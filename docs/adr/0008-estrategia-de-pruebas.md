# ADR 0008: Estrategia de pruebas

**Estado:** Aceptada · **Fecha:** 2026-10-21

## Contexto

Las pruebas unitarias son criterio de entrega (CA-16): `dotnet test` debe pasar sin Mongo ni SMTP y cubrir ambas transiciones, la selección del mensaje, la exclusión de estados no contemplados, la no actualización cuando falla el envío, tres clientes sin cruces y las invariantes de contadores. El revisor también puede abrirlas en el Explorador de pruebas de Visual Studio 2026.

Condiciones del ecosistema en la fecha: xUnit v2 figura como deprecado en NuGet (solo parches de seguridad); xunit.v3 es estable desde 4.0 (14-ago-2026). El SDK 10 permite ejecutar `dotnet test` sobre Microsoft.Testing.Platform (MTP), pero ese modo cambia la sintaxis del comando y tiene fricción conocida con el Explorador de pruebas. Además, se trata de una prueba técnica: la estrategia debe ser proporcional al tamaño del problema, y el único comando del revisor (`docker compose up --build`) no puede romperse por advisories NuGet o analizadores nuevos.

## Decisión

- **Un solo proyecto de pruebas:** `tests/InvoiceReminders.UnitTests` (`OutputType Exe`, `IsTestProject`). No hay proyecto de pruebas de integración ni biblioteca compartida de dobles.
- **Framework y runner:** `xunit.v3.mtp-off 4.0.2` (la variante oficial de xunit.v3 que desactiva Microsoft Testing Platform) + `xunit.runner.visualstudio 4.0.1` + `Microsoft.NET.Test.Sdk 18.11.0`, ejecutados con el **`dotnet test` clásico (VSTest)**. `global.json` no configura Microsoft.Testing.Platform. Comando: `dotnet test` desde la raíz (usa `InvoiceReminders.slnx`) o `dotnet test tests/InvoiceReminders.UnitTests`. El Explorador de pruebas de Visual Studio las descubre gracias a `xunit.runner.visualstudio`.
- **Dobles escritos a mano** en `tests/InvoiceReminders.UnitTests/Fakes`, sin librería de mocks:
  - `InMemoryInvoiceRepository`: filtra por cliente y estado, y avanza el estado **solo si la factura sigue en el estado esperado** (la misma semántica compare-and-set que `MongoInvoiceRepository`). Permite simular un cambio concurrente (`SimulateConcurrentChange`), hacer fallar las actualizaciones (`FailUpdatesWith`) o la lectura de las facturas de un cliente (`FailReadsFor`), consultar el historial registrado (`Changes`) y comprobar que el CAS se invocó con un token no cancelable. Respeta el `CancellationToken` recibido.
  - `InMemoryCustomerRepository`.
  - `RecordingEmailSender`: guarda los correos enviados, falla con `EmailDeliveryException` para direcciones concretas (como un SMTP caído), respeta el `CancellationToken`, registra si algún envío recibió un token cancelable (`AnySendTokenCouldBeCancelled`) y expone un gancho `AfterSend` con el que las pruebas simulan un apagado justo después de un envío.
  - `OperationJournal`: registro ordenado compartido por los dobles; verifica el orden envío → actualización (`send:FAC-1` antes de `update:FAC-1`, y ningún `update` si el envío falla).
  - `FixedTimeProvider`: reloj fijo derivado de `TimeProvider`.
- **Cobertura** (48 métodos de prueba, 62 casos contando cada `[InlineData]` y cada entrada de `[MemberData]`; < 3 s, sin Docker ni red):
  - `ReminderPolicyTests` (dos `[Theory]` con 8 casos y un `[Fact]`) e `InvoiceStatusTests` (3): máquina de estados y literales del contrato.
  - `CustomerTests` (dos `[Theory]` con 6 casos): `HasDeliverableEmail` acepta direcciones simples y rechaza vacías, inválidas, con nombre (`"Juan Pérez <juan@perez.test>"`) o con espacios.
  - `ReminderEmailComposerTests` (7): destinatario con nombre y dirección separados, asuntos de ambas plantillas, formato `es-CO`, `Pérez & Asociados S.A.S.` → `&amp;` en HTML y `&` en texto, cabeceras y `Message-ID` determinista, fallo inmediato si falta una plantilla.
  - `ProcessRemindersUseCaseTests` (17): ambas transiciones, estados ignorados, fallo de envío sin cambio de estado (y el lote sigue), conflicto CAS, fallo de actualización tras el envío (con el `Message-ID` y sin detalles internos en el error), corte del lote cuando falla la actualización tras enviar, corte cuando falla la lectura de un cliente (conservando lo ya hecho), cliente sin correo válido, cliente inexistente, tres clientes sin cruces (CA-11), un solo cliente, tres ejecuciones consecutivas, apagado entre facturas (ni el envío ni el CAS reciben un token cancelable), apagado antes del siguiente cliente (resultado parcial sin leerlo), historial con `runId` y `Message-ID`, invariantes de contadores.
  - `DemoDataTests` (5): forma de la semilla y la historia 7 → 4 → 0 ejecutando el caso de uso real sobre los dobles.
  - `DependencyInjectionTests` (4): grafo real con `ValidateOnBuild` y `ValidateScopes`, `OptionsValidationException` que cita `Mongo__ConnectionString` y `Smtp__Username`, semilla deshabilitada por defecto.
  - `DocumentMappersTests` (3): ida y vuelta dominio → documento → dominio sin cambios, fechas guardadas como medianoche UTC y estado como literal exacto, y un documento sin estado que se mapea a un estado desconocido en lugar de fallar.
  - `MailKitEmailSenderTests` (un `[Theory]` con 5 entradas de `[MemberData]` y tres `[Fact]`): la regla `IsDeliveryFailure` trata como fallo de entrega los problemas de transporte y el timeout propio del cliente, pero no una cancelación pedida por quien llama ni los errores de programación.
  - Nombres `Metodo_Escenario_Resultado`, AAA, un comportamiento por prueba.
- **Sin pruebas de integración:** no se usa Testcontainers. `MongoInvoiceRepository`, `MongoInvoiceSummaryQueries`, el seeder, los health checks y el envío real de `MailKitEmailSender` se validan extremo a extremo con `docker compose` y la guía `docs/VERIFICACION.md`.
- **Frontend:** sin pruebas de componentes; la verificación es la compilación estricta de TypeScript en `npm run build` (ADR 0006).
- **Rigor del compilador:** `TreatWarningsAsErrors` **solo** con `ContinuousIntegrationBuild=true`; `WarningsNotAsErrors NU1901;NU1902;NU1903;NU1904`. La regla de dependencias se comprueba con `dotnet list src/InvoiceReminders.Application package --include-transitive` (no debe contener `MongoDB`, `MailKit`, `MimeKit` ni `Microsoft.AspNetCore`); la ejecuta el trabajo `backend` del CI (`.github/workflows/ci.yml`) en cada push, junto con la compilación con `ContinuousIntegrationBuild=true` y `dotnet test` (ADR 0009).

## Alternativas consideradas

- **xunit 2.9.3.** Sin fricción de runner, pero deprecado en NuGet; entregar sobre un framework de pruebas deprecado es la misma señal negativa que un runtime a punto de vencer. Descartado.
- **Microsoft.Testing.Platform como runner** (paquete `xunit.v3` con `"test": { "runner": "Microsoft.Testing.Platform" }` en `global.json`). Cambia la sintaxis de `dotnet test` (`--project`/`--solution`, sin `--logger trx`) y añade riesgo con el Explorador de pruebas; con VSTest, `dotnet test` funciona sin argumentos especiales. Descartado por ahora; migrar más adelante es cambiar `xunit.v3.mtp-off` por `xunit.v3` y no exige cambiar las pruebas.
- **NSubstitute y `Microsoft.Extensions.TimeProvider.Testing`.** Descartados por proporcionalidad: los dobles a mano son cinco clases pequeñas, evitan dos dependencias y el doble del repositorio **documenta el contrato del CAS** (un mock solo afirmaría que hubo una llamada). El orden envío → actualización se verifica con `OperationJournal` en lugar de `Received.InOrder`.
- **Testcontainers con una suite de contrato compartida** (biblioteca de dobles + proyecto de integración que ejecute el mismo contrato contra `InMemoryInvoiceRepository` y `MongoInvoiceRepository`). Verificaría el CAS real y la paridad del doble con MongoDB, pero añade dos proyectos, dependencia de Docker en las pruebas y tiempo de ejecución. Desproporcionado para una prueba técnica; queda como evolución.
- **`DependencyRulesTests` por reflexión.** `GetReferencedAssemblies()` solo ve ensamblados usados en IL y daría falsa garantía; se sustituye por las referencias de `.csproj` y el comando `dotnet list`.
- **Warnings como errores siempre o en Release.** Expondría el `dotnet publish` del Dockerfile a advisories NuGet o analizadores ajenos al código. Descartado.

## Consecuencias

Positivas:

- `ReminderPolicyTests` actúa como especificación ejecutable de la máquina de estados.
- `dotnet test` funciona tal cual, desde la CLI y desde el Explorador de pruebas.
- Pocas dependencias de prueba: los tres paquetes de xUnit/VSTest más `Microsoft.Extensions.Configuration` y `Microsoft.Extensions.DependencyInjection` para `DependencyInjectionTests`.
- Los dobles son legibles y dejan escrito qué espera el caso de uso de cada puerto.
- El build del revisor no depende del estado del ecosistema NuGet el día de la revisión.

Negativas y lo que se acepta a cambio:

- La semántica CAS del doble no se verifica contra MongoDB real: si `MongoInvoiceRepository` divergiera, las pruebas unitarias no lo detectarían. Se mitiga con el trabajo `compose-smoke` del CI (7 → 4 → 0 contra MongoDB real) y la guía manual (incluido el escenario de SMTP caído); las pruebas con Testcontainers quedan como evolución.
- Los adaptadores de Infrastructure (repositorios Mongo, envío MailKit, health checks) no tienen pruebas unitarias; de Infrastructure solo se prueban por unidad el mapeo `DocumentMappers` y la regla `IsDeliveryFailure`. El despliegue real lo recorre el trabajo `compose-smoke` del CI.
- `UnitTests` referencia `Infrastructure` solo para `DemoDataTests`, `DependencyInjectionTests`, `DocumentMappersTests` y `MailKitEmailSenderTests`; las pruebas del caso de uso no tocan ningún tipo de Infrastructure.

## Referencias

- `docs/ARQUITECTURA.md`: tabla DI, capas y sección 13.1 (evolución).
- `README.md`: comandos de prueba.
- ADR 0001 (SDK 10), ADR 0004 (contrato del CAS) y ADR 0009 (CI con prueba de humo del compose).
