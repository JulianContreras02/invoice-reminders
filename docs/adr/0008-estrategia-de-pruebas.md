# ADR 0008: Estrategia de pruebas

**Estado:** Aceptada · **Fecha:** 2026-10-21

## Contexto

Las pruebas unitarias son criterio de entrega (CA-16): `dotnet test` debe pasar sin Mongo ni SMTP y cubrir ambas transiciones, la selección del mensaje, la exclusión de estados no contemplados, la no actualización cuando falla el envío, tres clientes sin cruces y las invariantes de contadores. El revisor también puede abrirlas en el Explorador de pruebas de Visual Studio 2026.

Condiciones del ecosistema en la fecha: xUnit v2 figura como deprecado en NuGet (solo parches de seguridad); xunit.v3 es estable desde 4.0 (14-ago-2026). El SDK 10 permite ejecutar `dotnet test` sobre Microsoft.Testing.Platform (MTP), pero ese modo cambia la sintaxis del comando y tiene fricción conocida con el Explorador de pruebas. Además, se trata de una prueba técnica: la estrategia debe ser proporcional al tamaño del problema, y el único comando del revisor (`docker compose up --build`) no puede romperse por advisories NuGet o analizadores nuevos.

## Decisión

- **Un solo proyecto de pruebas:** `tests/InvoiceReminders.UnitTests` (`OutputType Exe`, `IsTestProject`). No hay proyecto de pruebas de integración ni biblioteca compartida de dobles.
- **Framework y runner:** `xunit.v3.mtp-off 4.0.2` (la variante oficial de xunit.v3 que desactiva Microsoft Testing Platform) + `xunit.runner.visualstudio 4.0.1` + `Microsoft.NET.Test.Sdk 18.11.0`, ejecutados con el **`dotnet test` clásico (VSTest)**. `global.json` no configura Microsoft.Testing.Platform. Comando: `dotnet test` desde la raíz (usa `InvoiceReminders.slnx`) o `dotnet test tests/InvoiceReminders.UnitTests`. El Explorador de pruebas de Visual Studio las descubre gracias a `xunit.runner.visualstudio`.
- **Dobles escritos a mano** en `tests/InvoiceReminders.UnitTests/Fakes`, sin librería de mocks:
  - `InMemoryInvoiceRepository`: filtra por cliente y estado, y avanza el estado **solo si la factura sigue en el estado esperado** (la misma semántica compare-and-set que `MongoInvoiceRepository`). Permite simular un cambio concurrente (`SimulateConcurrentChange`), hacer fallar las actualizaciones (`FailUpdatesWith`), consultar el historial registrado (`Changes`) y comprobar que el CAS se invocó con un token no cancelable.
  - `InMemoryCustomerRepository`.
  - `RecordingEmailSender`: guarda los correos enviados, falla con `EmailDeliveryException` para direcciones concretas (como un SMTP caído) y expone un gancho `AfterSend`.
  - `OperationJournal`: registro ordenado compartido por los dobles; verifica el orden envío → actualización (`send:FAC-1` antes de `update:FAC-1`, y ningún `update` si el envío falla).
  - `FixedTimeProvider`: reloj fijo derivado de `TimeProvider`.
- **Cobertura** (36 métodos de prueba, 42 casos contando cada `[InlineData]`; < 3 s, sin Docker ni red):
  - `ReminderPolicyTests` (dos `[Theory]` con 8 casos y un `[Fact]`) e `InvoiceStatusTests` (3): máquina de estados y literales del contrato.
  - `ReminderEmailComposerTests` (7): destinatario con nombre y dirección separados, asuntos de ambas plantillas, formato `es-CO`, `Pérez & Asociados S.A.S.` → `&amp;` en HTML y `&` en texto, cabeceras y `Message-ID` determinista, fallo inmediato si falta una plantilla.
  - `ProcessRemindersUseCaseTests` (14): ambas transiciones, estados ignorados, fallo de envío sin cambio de estado, conflicto CAS, fallo de actualización tras el envío, cliente sin correo válido, cliente inexistente, tres clientes sin cruces (CA-11), un solo cliente, tres ejecuciones consecutivas, cancelación entre facturas con el CAS en `CancellationToken.None`, historial con `runId` y `Message-ID`, invariantes de contadores.
  - `DemoDataTests` (5): forma de la semilla y la historia 7 → 4 → 0 ejecutando el caso de uso real sobre los dobles.
  - `DependencyInjectionTests` (4): grafo real con `ValidateOnBuild` y `ValidateScopes`, `OptionsValidationException` que cita `Mongo__ConnectionString` y `Smtp__Username`, semilla deshabilitada por defecto.
  - Nombres `Metodo_Escenario_Resultado`, AAA, un comportamiento por prueba.
- **Sin pruebas de integración:** no se usa Testcontainers. `MongoInvoiceRepository`, `MongoInvoiceSummaryQueries`, el seeder, los health checks y `MailKitEmailSender` se validan extremo a extremo con `docker compose` y la guía `docs/VERIFICACION.md`.
- **Frontend:** sin pruebas de componentes; la verificación es la compilación estricta de TypeScript en `npm run build` (ADR 0006).
- **Rigor del compilador:** `TreatWarningsAsErrors` **solo** con `ContinuousIntegrationBuild=true`; `WarningsNotAsErrors NU1901;NU1902;NU1903;NU1904`. La regla de dependencias se comprueba con `dotnet list src/InvoiceReminders.Application package --include-transitive` (no debe contener `MongoDB`, `MailKit`, `MimeKit` ni `Microsoft.AspNetCore`); hoy es una comprobación manual porque no hay CI (ADR 0009).

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

- La semántica CAS del doble no se verifica contra MongoDB real: si `MongoInvoiceRepository` divergiera, las pruebas unitarias no lo detectarían. Se mitiga con la guía manual (7 → 4 → 0 y escenario de SMTP caído); las pruebas con Testcontainers quedan como evolución.
- Los adaptadores de Infrastructure (Mongo, MailKit, health checks) no tienen pruebas automatizadas.
- `UnitTests` referencia `Infrastructure` solo para `DemoDataTests` y `DependencyInjectionTests`; las pruebas del caso de uso no tocan ningún tipo de Infrastructure.

## Referencias

- `docs/ARQUITECTURA.md`: tabla DI, capas y sección 13.1 (evolución).
- `README.md`: comandos de prueba.
- ADR 0001 (SDK 10), ADR 0004 (contrato del CAS) y ADR 0009 (CI opcional, no incluido).
