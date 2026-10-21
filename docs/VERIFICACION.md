# Guía de verificación para el equipo revisor

**Proyecto:** `invoice-reminders` · **Fecha:** 2026-10-21 · Primer build: 3-6 minutos; el resto del guion no requiere esperas.

Esta guía recorre, paso a paso y con resultados esperados, todo lo que el sistema promete. La arquitectura está en [ARQUITECTURA.md](ARQUITECTURA.md); las decisiones en [DECISIONES.md](DECISIONES.md). Los lugares marcados `[CAPTURA: …]` llevarán capturas de pantalla en la versión final.

---

## 1. Requisito previo

Solo **Docker Desktop** (con WSL2 en Windows). No hace falta .NET, Node ni Visual Studio para desplegar y verificar; solo se necesitan para las secciones 8 y 9 (pruebas y depuración).

## 2. Arranque con un comando

Desde la raíz del clon:

```
docker compose up --build
```

El primer build tarda 3-6 minutos (compila el frontend React y la API dentro de la imagen); los siguientes, segundos. Levanta tres contenedores: `app`, `mongo` y `mailpit`. La aplicación espera a Mongo, crea los índices y siembra los datos de demostración antes de abrir el puerto. `app` espera también a que Mailpit esté sano, así que el chip de salud arranca en verde.

Para apagar: `Ctrl+C` y `docker compose down`. Para borrar también los datos: `docker compose down -v`.

## 3. URLs

| Qué | URL |
|---|---|
| Interfaz (resumen de facturas) | `http://localhost:8080` |
| Documentación de la API (Scalar) | `http://localhost:8080/scalar` (documento OpenAPI en `/openapi/v1.json`) |
| Salud (JSON por check) | `http://localhost:8080/health/ready` |
| Correos capturados (Mailpit) | `http://localhost:8025` |

Si algún puerto está ocupado, vea la sección 11.

## 4. Qué debe ver al abrir la UI

Abra `http://localhost:8080`. Pantalla única "Resumen de facturas":

- Chip de salud "API, MongoDB y SMTP listos" y enlace "Ver correos enviados (Mailpit)".
- Tarjetas: **Primer recordatorio 4 · Segundo recordatorio 3 · Desactivadas 1 · Pagadas 2 · Otros estados 0 · Total 10**.
- Resumen por cliente con **tres clientes** (`cli-001 Pérez & Asociados S.A.S.`, `cli-002 Rodríguez Abogados Ltda.`, `cli-003 Consultorio Jurídico Gómez`), cada uno con su email `.test` y sus conteos por estado.
- Tabla con **10 facturas** (`FAC-2026-0101` … `FAC-2026-0304`) con número de factura, cliente, correo, monto en COP, emisión, vencimiento, estado y fecha de actualización.
- Filtros por cliente y por estado; botones "Ejecutar recordatorios (todos)" y "Reiniciar datos de demostración".

`[CAPTURA: UI inicial con tarjetas 4/3/1/2, resumen de los tres clientes y las 10 facturas]`

Mailpit (`http://localhost:8025`) debe estar vacío.

## 5. Guion de ejecución: 7 → 4 → 0

### Ejecución 1

Pulse **"Ejecutar recordatorios (todos)"**. En el panel de resultado:

- Candidatas 7 · Correos enviados 7 · Actualizadas 7 · Correos fallidos 0 · Conflictos 0 · Enviadas sin actualizar 0 · Omitidas 0, con el `runId` y la duración.
- Tabla por factura con 7 filas "Enviado y actualizado" (`SentAndUpdated`), con el estado antes y después.
- Las tarjetas pasan a **Primer 0 · Segundo 4 · Desactivadas 4 · Pagadas 2**.

En Mailpit: **7 correos**, **3 destinatarios** y **2 asuntos** distintos:

| Destinatario | Correos | Asuntos |
|---|---|---|
| contacto@perez-asociados.test | 2 | `Factura FAC-2026-0101: ha pasado a segundo recordatorio` · `Factura FAC-2026-0102: aviso de desactivación del servicio` |
| facturacion@rodriguez-abogados.test | 2 | `Factura FAC-2026-0201: …segundo recordatorio` · `Factura FAC-2026-0202: …segundo recordatorio` |
| admin@gomez-juridico.test | 3 | `FAC-2026-0301` y `FAC-2026-0302`: aviso de desactivación · `FAC-2026-0304`: segundo recordatorio |

Abra el correo de `FAC-2026-0102`: el nombre `Pérez & Asociados S.A.S.` se muestra correctamente en la vista HTML, y en las cabeceras aparecen `X-Invoice-Id`, `X-Reminder-Kind`, `X-Reminder-Run-Id` y un `Message-ID` con la forma `<FAC-2026-0102.segundorecordatorio.{runId sin guiones}@invoice-reminders.test>` (en `historialEstados[].mensajeId` y en la respuesta de la API el mismo valor se guarda sin los corchetes angulares).

`[CAPTURA: panel de resultado de la ejecución 1]` · `[CAPTURA: Mailpit con 7 mensajes y los dos asuntos]`

### Ejecución 2

Pulse de nuevo. Resultado: Candidatas 4 · Correos enviados 4 · Actualizadas 4. Tarjetas: **Primer 0 · Segundo 0 · Desactivadas 8 · Pagadas 2**. Mailpit acumula **11** correos; los 4 nuevos son todos "aviso de desactivación del servicio" (`0101`, `0201`, `0202`, `0304`).

### Ejecución 3

Pulse una vez más. Resultado: Candidatas 0 · Correos enviados 0 ("No había facturas en primer ni en segundo recordatorio"). Nada cambia en la tabla ni en Mailpit. Las facturas `pagada` (`0103`, `0303`) y la `desactivado` original (`0203`) nunca recibieron correo ni cambiaron.

| Momento | primer | segundo | desactivado | pagada | Correos en la ejecución |
|---|---|---|---|---|---|
| Inicial | 4 | 3 | 1 | 2 | — |
| Ejecución 1 | 0 | 4 | 4 | 2 | 7 (4 segundo recordatorio + 3 desactivación), 3 destinatarios |
| Ejecución 2 | 0 | 0 | 8 | 2 | 4 (desactivación) |
| Ejecución 3 | 0 | 0 | 8 | 2 | 0 |

**Ejecución por cliente (opcional):** filtre por `cli-002` y pulse "Ejecutar solo para Rodríguez Abogados Ltda."; solo se procesan sus facturas. Equivale a `POST /api/reminders/run?customerId=cli-002` (parámetro de query string) desde `/scalar` o con `curl -X POST "http://localhost:8080/api/reminders/run?customerId=cli-002"`.

### Verificación automática del guion

El mismo recorrido de esta sección está automatizado en `.github/scripts/compose-smoke.sh`, que el CI ejecuta en cada push. Con el compose levantado, el revisor puede correrlo con `bash .github/scripts/compose-smoke.sh` (requiere `curl` y `jq`). Vacía Mailpit al empezar y deja los datos reiniciados al terminar.

## 6. Escenario "SMTP caído" (CA-08: sin correo no hay cambio de estado)

Parta de los datos iniciales (sección 7 si ya ejecutó el guion anterior).

1. En otra terminal: `docker compose stop mailpit`.
2. Refresque la UI: el chip pasa a **"SMTP no disponible"**; `http://localhost:8080/health/ready` muestra el check `smtp` en `Degraded` y `mongo` en `Healthy`.
3. Pulse "Ejecutar recordatorios (todos)". Resultado: **Candidatas 7 · Correos enviados 0 · Correos fallidos 7**; las 7 filas son "Correo fallido (estado intacto)" (`EmailFailed`) con el error "No se pudo entregar el correo a {to} vía mailpit:1025: …". Las tarjetas siguen en **4/3/1/2**: ningún estado cambió.
4. `docker compose start mailpit`. El chip vuelve a verde.
5. Pulse de nuevo: 7 enviados y 7 actualizadas; Mailpit recibe los 7 correos.

`[CAPTURA: chip "SMTP no disponible" y panel con 7 EmailFailed y tarjetas intactas]`

## 7. Repetir la demo

1. Pulse **"Reiniciar datos de demostración"** y confirme (equivale a `POST /api/admin/seed/reset`; restaura los mismos 3 clientes y 10 facturas con los mismos `_id`). Las tarjetas vuelven a 4/3/1/2.
2. **Vacíe Mailpit**: botón *Delete all* en `http://localhost:8025` o `DELETE http://localhost:8025/api/v1/messages`. El reinicio de datos no vacía la bandeja a propósito; sin este paso, la siguiente ronda mostraría los correos acumulados (7 + 4 + 7).

## 8. Cómo correr las pruebas

Requiere el SDK de .NET 10 (`global.json` fija `10.0.100` con `rollForward: latestFeature`). Sin Docker ni red, desde la raíz del clon:

```
dotnet test
```

(o `dotnet test tests/InvoiceReminders.UnitTests` para el proyecto de pruebas). Es el `dotnet test` clásico (VSTest): el proyecto usa `xunit.v3.mtp-off` 4.0.2, la variante oficial de xunit.v3 que desactiva Microsoft Testing Platform, y se ejecuta a través de `xunit.runner.visualstudio` y `Microsoft.NET.Test.Sdk`.

Esperado: 42 casos de prueba en verde (36 métodos `[Fact]`/`[Theory]`; los dos `[Theory]` de `ReminderPolicyTests` aportan 8 casos `[InlineData]`) en menos de 3 segundos: política de transiciones y literales de estado, compositor de correo, caso de uso con sus invariantes y caminos de fallo, semilla (7 → 4 → 0) y grafo de DI. Las pruebas usan dobles escritos a mano (`tests/InvoiceReminders.UnitTests/Fakes`), sin MongoDB ni SMTP.

No hay pruebas de integración ni pruebas del frontend; el frontend se valida con la compilación estricta de TypeScript (`npm run build` = `tsc && vite build`, que se ejecuta dentro del build de Docker).

`[CAPTURA: salida de dotnet test en verde]`

## 9. Cómo abrir en Visual Studio 2026

Versión: Visual Studio 2026 (18.x) Community, carga de trabajo "ASP.NET y desarrollo web". `net10.0` solo está soportado oficialmente en VS 18.0+; VS 2022 17.14 con el SDK 10 abre y compila con advertencia, sin soporte. Abra `InvoiceReminders.slnx` (formato XML de solución de Visual Studio 2026; proyectos Domain, Application, Infrastructure, Api y `tests/InvoiceReminders.UnitTests`, más carpetas de solución con los archivos de configuración y los documentos). El Explorador de pruebas descubre las pruebas xunit.v3 (`xunit.v3.mtp-off` + `xunit.runner.visualstudio`).

**API local con la infraestructura en Docker:**
1. `docker compose up -d mongo mailpit` (solo la infraestructura; si se olvida, `MongoStartupWaiter` lo indica en el log tras 15 intentos).
2. Proyecto de inicio `InvoiceReminders.Api`, perfil "http" (`http://localhost:5080`) o "https" (`https://localhost:7080`); ambos abren `/scalar`. `appsettings.Development.json` apunta a `localhost:27017` y `localhost:1025` con `Seed:Mode=IfEmpty` y `Seed:AllowReset=true`.
3. Puntos de interrupción en `ProcessRemindersUseCase` y Hot Reload funcionan. Para la UI en este modo: `cd web && npm ci && npm run dev` (Vite en `http://localhost:5173` hace proxy de `/api` y `/health` a `http://localhost:5080`).

No hay proyecto `docker-compose.dcproj`: depurar con F5 dentro de Docker Compose queda como evolución posible.

`[CAPTURA: Test Explorer en verde]`

## 10. Checklist de criterios de aceptación

| | Criterio | Cómo se verifica |
|---|---|---|
| [ ] | **CA-01** Conexión | La API arranca y la UI muestra facturas. Negativo: arranque la API local desde Visual Studio sin `docker compose up -d mongo mailpit`: el log muestra "Esperando MongoDB (intento i/15): …" y el proceso termina con código 1 y un error explícito; `docker compose logs app` nunca muestra un bucle de reinicio. |
| [ ] | **CA-02** Extracción por cliente | Filtro de cliente `cli-002` (o `GET /api/invoices?customerId=cli-002`): exactamente sus 3 facturas. "Ejecutar solo para {cliente}" procesa únicamente las suyas. |
| [ ] | **CA-03** Filtro por estado | Tras cualquier ejecución, `0103` y `0303` (`pagada`) y `0203` (`desactivado`) no tienen correo en Mailpit ni cambio de estado. |
| [ ] | **CA-04** Correo de primer recordatorio | Ejecución 1: 4 correos con asunto `Factura {numero}: ha pasado a segundo recordatorio`, cada uno al email del cliente dueño. |
| [ ] | **CA-05** Correo de segundo recordatorio | Ejecución 1: 3 correos con asunto `Factura {numero}: aviso de desactivación del servicio`. |
| [ ] | **CA-06** Transición 1→2 | `FAC-2026-0101` pasa a `segundorecordatorio` tras la ejecución 1 (UI, o en Mongo: `docker compose exec mongo mongosh invoice_reminders --quiet --eval "db.facturas.find({}, {estado: 1})"`). |
| [ ] | **CA-07** Transición 2→desactivado | `FAC-2026-0102` pasa a `desactivado` tras la ejecución 1; `historialEstados` tiene una entrada nueva con `motivo: "recordatorio"`, `ejecucionId` y `mensajeId`. |
| [ ] | **CA-08** Orden correo → actualización | Sección 6: con Mailpit detenido, 7 `EmailFailed` y tarjetas 4/3/1/2 intactas; el lote no se interrumpe. |
| [ ] | **CA-09** Estados no contemplados intactos | Tras las tres ejecuciones, la tarjeta "Pagadas" sigue en 2 y `0203` sigue siendo la única `desactivado` sin correo. |
| [ ] | **CA-10** Un paso por ejecución | `0101`: `primerrecordatorio` → `segundorecordatorio` (ejecución 1) → `desactivado` (ejecución 2); nunca dos saltos en una ejecución. |
| [ ] | **CA-11** Tres clientes | Mailpit: 3 destinatarios distintos; cada correo nombra en el asunto una factura del cliente destinatario (tabla de la sección 5). Resumen por cliente en la UI. |
| [ ] | **CA-12** Resumen en UI | Tabla con las 10 facturas (cliente, número, estado), tarjetas por estado y resumen de los 3 clientes con nombre, email y conteos; se actualiza tras cada ejecución. |
| [ ] | **CA-13** Datos reproducibles | En un clon limpio, `docker compose up --build` deja 3 clientes y 10 facturas sin intervención manual; `GET /api/invoices` devuelve `total: 10` y 3 `customers`. |
| [ ] | **CA-14** Stack exigido | `InvoiceReminders.slnx` compila y depura en Visual Studio 2026 con .NET 10 y C#; `web/` es React (Vite + TypeScript) y consume `/api`. |
| [ ] | **CA-15** Inyección de dependencias | `Application/DependencyInjection.cs` e `Infrastructure/DependencyInjection.cs`; el caso de uso recibe `ICustomerRepository`, `IInvoiceRepository`, `IReminderEmailComposer`, `IEmailSender` por constructor; `ProcessRemindersUseCaseTests` lo construye con dobles; `DependencyInjectionTests` valida el grafo real. |
| [ ] | **CA-16** Pruebas unitarias | `dotnet test` en verde sin Mongo ni SMTP (42 casos); cubre ambas transiciones, selección de plantilla, exclusión de estados, no actualización si falla el envío, tres clientes sin cruces e invariantes de contadores. |
| [ ] | **CA-17** Documentación y entrega | `README.md` operativo; `docs/ARQUITECTURA.md`, `docs/DECISIONES.md` + ADR, esta guía y `docs/ENTREGA.md`; el repositorio no reproduce el enunciado de la prueba técnica. |

## 11. Troubleshooting

| Síntoma | Causa y solución |
|---|---|
| `docker compose up` falla con "port is already allocated" (8080, 8025, 1025 o 27017) | Copie `.env.example` a `.env` y cambie `APP_PORT`, `MAILPIT_PORT`, `SMTP_PORT` o `MONGO_PORT`. El compose funciona sin `.env`; solo hace falta si un puerto está ocupado. Si cambia `APP_PORT` o `MAILPIT_PORT`, use esos puertos en las URLs de la sección 3. |
| El build se detiene o Docker Desktop reporta falta de memoria / errores de WSL2 | Asigne más memoria a Docker Desktop (o a WSL2), cierre otros contenedores y repita `docker compose up --build`. El primer build descarga las imágenes `sdk:10.0`, `aspnet:10.0`, `node:22-alpine`, `mongo:8.0` y `axllent/mailpit:v1.31.6`. |
| El build falla descargando paquetes (`npm install`/`npm ci` o `dotnet restore`) detrás de un proxy corporativo | Configure el proxy en la configuración de Docker Desktop para que las etapas `web` (npm) y `build` (NuGet) del Dockerfile tengan salida a internet, y repita el build. |
| El contenedor `app` aparece como `Exited` y la UI no responde | No hay `restart` a propósito: `docker compose logs app` muestra la causa en la última línea. Casos típicos: variable de configuración inválida (el mensaje cita la propiedad y la variable, p. ej. `Smtp__Host`), Mongo no disponible tras 15 intentos, o error al sembrar. Corrija y vuelva a levantar. |
| `mongo` se reinicia o termina al arrancar en un equipo x86-64 antiguo o una VM | MongoDB ≥ 5.0 exige AVX en x86-64; `mongo:8.0` no arranca sin él (ARM64/Apple Silicon no se ve afectado). No hay tag alternativo soportado: use otra máquina o habilite AVX en la VM. |

Diagnóstico general: `docker compose ps` (estado y healthchecks), `docker compose logs app`, `http://localhost:8080/health/ready`.
