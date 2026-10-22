# invoice-reminders

> El código apunta a **.NET 10** y la solución requiere **Visual Studio 2026 (18.x)** para soporte oficial (VS 2022 17.14 con el SDK 10 abre y compila con warning, sin soporte). El despliegue con `docker compose` **no depende de Visual Studio**.

Proceso de recordatorios de cobro sobre facturas almacenadas en MongoDB: por cada factura en primer o segundo recordatorio envía un correo al cliente y, solo si el envío fue aceptado, avanza su estado al siguiente escalón (segundo recordatorio o desactivado).
Incluye una API Minimal (.NET 10), una interfaz React de una pantalla con el resumen de todas las facturas y un `docker compose` con la aplicación, MongoDB y un capturador de correo (Mailpit).

[![CI](https://github.com/JulianContreras02/invoice-reminders/actions/workflows/ci.yml/badge.svg)](https://github.com/JulianContreras02/invoice-reminders/actions/workflows/ci.yml)

![Resumen de facturas tras la primera ejecución: 7 correos enviados y 7 facturas actualizadas](docs/img/02-resultado-ejecucion-1.png)

Más capturas en la [guía de verificación](docs/VERIFICACION.md). El documento de entrega está en [docs/ENTREGA.md](docs/ENTREGA.md) y en [PDF](docs/ENTREGA.pdf).

## Requisito previo

- **Docker Desktop** (con WSL2 en Windows). Es el único requisito para desplegar y verificar.
- **No** hace falta instalar .NET ni Node: el frontend se compila dentro de la imagen y la API se publica en una etapa del Dockerfile.

## Inicio rápido

```bash
git clone https://github.com/JulianContreras02/invoice-reminders.git
cd invoice-reminders
docker compose up --build
```

El primer build tarda entre 3 y 6 minutos; los siguientes, segundos. Al arrancar, la aplicación espera a MongoDB, crea los índices y siembra los datos de demostración (3 clientes y 10 facturas) si la base está vacía.

| Recurso | URL |
|---|---|
| Interfaz (resumen de facturas) | http://localhost:8080 |
| Documentación de la API (Scalar) | http://localhost:8080/scalar (documento OpenAPI en `/openapi/v1.json`) |
| Salud | http://localhost:8080/health/ready (`/health/live` para vida) |
| Correos capturados (Mailpit) | http://localhost:8025 |
| MongoDB (opcional, para Compass) | `mongodb://localhost:27017/invoice_reminders` |

Todos los puertos se publican solo en `127.0.0.1`.

## Ejecutar el proceso de recordatorios

1. Abra http://localhost:8080. Verá 10 facturas de 3 clientes, las tarjetas de conteo por estado y el resumen por cliente.
2. Pulse **Ejecutar recordatorios (todos)** (o **Ejecutar solo para {cliente}** si hay un cliente filtrado). El panel de resultado muestra los contadores, el `runId`, si la ejecución fue interrumpida (apagado de la aplicación) o detenida (MongoDB no respondió) y una fila por factura con el desenlace.
3. Abra http://localhost:8025 (o el enlace **Ver correos enviados (Mailpit)** del encabezado). Con los datos iniciales la secuencia es determinista:

| Ejecución | Correos | Qué cambia |
|---|---|---|
| 1.ª | 7 (4 "ha pasado a segundo recordatorio" + 3 "aviso de desactivación"), 3 destinatarios | 4 facturas pasan a `segundorecordatorio`, 3 a `desactivado` |
| 2.ª | 4 (desactivación) | las 4 restantes pasan a `desactivado` |
| 3.ª | 0 | nada: no quedan facturas elegibles |

Las facturas en `pagada` o `desactivado` nunca reciben correo ni cambian de estado.

Desde la línea de comandos:

```bash
curl -X POST http://localhost:8080/api/reminders/run                      # todos los clientes
curl -X POST "http://localhost:8080/api/reminders/run?customerId=cli-002"   # un solo cliente
```

Si ya hay una ejecución o un reinicio en curso, la API responde `409` ("Operación en curso"). Si el `customerId` no existe, responde `404`.

## Reiniciar los datos (y vaciar Mailpit)

- Botón **Reiniciar datos de demostración** en la interfaz, o `POST http://localhost:8080/api/admin/seed/reset` (responde `403` si `Seed__AllowReset=false`).
- El reinicio restaura MongoDB pero **no** vacía Mailpit. Para repetir la historia 7 → 4 → 0 vacíe la bandeja con el botón *Delete all* de Mailpit o:

```bash
curl -X DELETE http://localhost:8025/api/v1/messages
```

- Borrado total (contenedores y volumen de datos):

```bash
docker compose down -v
```

## Variables de entorno

Valores por defecto tal como los fija `docker-compose.yml`. El formato `Seccion__Clave` corresponde a `Seccion:Clave` en `appsettings*.json`.

| Variable | Valor en compose | Notas |
|---|---|---|
| `Mongo__ConnectionString` | `mongodb://mongo:27017` | Obligatoria. Sin ella la aplicación termina al arrancar con un error que la nombra. |
| `Mongo__Database` | `invoice_reminders` | Colecciones `clientes` y `facturas`. |
| `Smtp__Host` | `mailpit` | Obligatoria. |
| `Smtp__Port` | `1025` | 1-65535. Valor por defecto en código: 1025. |
| `Smtp__Security` | `None` | `None`, `StartTls`, `SslOnConnect`, `Auto`. Para un SMTP real: `587` + `StartTls` o `465` + `SslOnConnect`. |
| `Smtp__FromAddress` | `recordatorios@invoice-reminders.test` | Obligatoria; debe ser una dirección válida. |
| `Smtp__FromName` | `Recordatorios de facturación` | |
| `Smtp__Username` / `Smtp__Password` | (vacíos) | Opcionales; van juntos. Nunca se versionan credenciales. |
| `Smtp__TimeoutSeconds` | `10` | Valor por defecto en código. |
| `Seed__Mode` | `IfEmpty` | `IfEmpty`, `Always`, `Never`. Por defecto en código: `Never` (la semilla es opt-in). |
| `Seed__AllowReset` | `true` | Habilita `POST /api/admin/seed/reset`. Por defecto en código: `false`. |
| `Links__MailpitUrl` | `http://localhost:8025` | URL que la interfaz muestra en "Ver correos enviados". Llega al front solo por `GET /api/meta`. |

Puertos del host (archivo `.env`, opcional; copie `.env.example` solo si necesita cambiarlos):

| Variable | Valor por defecto | Servicio |
|---|---|---|
| `APP_PORT` | `8080` | Interfaz y API |
| `MAILPIT_PORT` | `8025` | Interfaz de Mailpit |
| `SMTP_PORT` | `1025` | SMTP de Mailpit |
| `MONGO_PORT` | `27017` | MongoDB |

## Pruebas

Las pruebas unitarias no necesitan Docker ni red y tardan menos de 3 segundos. Requieren el SDK de .NET 10 (versión fijada en `global.json`).

```bash
dotnet test                                   # desde la raíz (InvoiceReminders.slnx)
dotnet test tests/InvoiceReminders.UnitTests  # solo el proyecto de pruebas
```

El proyecto `tests/InvoiceReminders.UnitTests` usa `xunit.v3.mtp-off` 4.0.2, la variante oficial de xunit.v3 que desactiva Microsoft Testing Platform, con el `dotnet test` clásico (VSTest, vía `xunit.runner.visualstudio` y `Microsoft.NET.Test.Sdk`), así que también aparece en el Explorador de pruebas de Visual Studio. Son 48 métodos de prueba (62 casos contando cada `[InlineData]` y cada entrada de `[MemberData]`) sobre el dominio, el caso de uso, el compositor de correos, la semilla, el contenedor de dependencias, el mapeo a documentos de MongoDB y la regla de fallos de entrega de `MailKitEmailSender`, con dobles escritos a mano en `tests/InvoiceReminders.UnitTests/Fakes`.

No hay pruebas de integración contra MongoDB/SMTP reales ni pruebas del frontend; el frontend se valida con `tsc` en modo estricto al compilar (`npm run build`). Ver [ADR 0008](docs/adr/0008-estrategia-de-pruebas.md).

## Integración continua

`.github/workflows/ci.yml` se ejecuta en cada push y pull request a `main` con tres trabajos: compilación y pruebas del backend con advertencias como errores, compilación del frontend, y una prueba de humo que levanta este mismo `docker compose` y recorre el guion de la guía de verificación (7, 4 y 0 correos). La prueba de humo también se puede correr en local con Docker, `curl` y `jq`:

```bash
docker compose up -d --build --wait
```

```bash
bash .github/scripts/compose-smoke.sh
```

## Abrir en Visual Studio 2026 y depurar

Abra `InvoiceReminders.slnx` (formato XML de solución de Visual Studio 2026; carga de trabajo "ASP.NET y desarrollo web").

**API local con la infraestructura en Docker:**

1. Levante solo la infraestructura:
   ```bash
   docker compose up -d mongo mailpit
   ```
2. Proyecto de inicio `InvoiceReminders.Api`, perfil **http** (`http://localhost:5080`) o **https** (`https://localhost:7080`, también escucha en `http://localhost:5080`); ambos con entorno `Development` y abren `/scalar`. `appsettings.Development.json` apunta a `localhost:27017` y `localhost:1025` con `Seed:Mode=IfEmpty` y `Seed:AllowReset=true`.
3. F5. Puntos de interrupción en `ProcessRemindersUseCase` y Hot Reload funcionan. Si olvidó el paso 1, el log de `MongoStartupWaiter` lo indica.
4. Para la interfaz en desarrollo: `cd web && npm ci && npm run dev` (Node 22, ≥ 22.12). Vite (puerto 5173) hace proxy de `/api` y `/health` a `http://localhost:5080`, por lo que no hay CORS. Con F5 la API no sirve la interfaz: `wwwroot/` solo se genera dentro de la imagen Docker.

El repositorio no incluye un proyecto de orquestación de Docker Compose para Visual Studio (`docker-compose.dcproj`); depurar la aplicación dentro de contenedores con F5 queda como evolución posible.

Peticiones de ejemplo listas para ejecutar en `src/InvoiceReminders.Api/InvoiceReminders.Api.http`.

## Estructura del repositorio

```
invoice-reminders/
├─ InvoiceReminders.slnx · global.json · Directory.Build.props · Directory.Packages.props
├─ docker-compose.yml · .env.example · nuget.config
├─ .github/         workflows/ci.yml · scripts/compose-smoke.sh (prueba de humo del compose)
├─ docs/            ARQUITECTURA.md · DECISIONES.md · adr/0001…0010 · VERIFICACION.md · ENTREGA.md
├─ src/
│  ├─ InvoiceReminders.Domain/          Customer, Invoice, InvoiceStatus, ReminderPolicy
│  ├─ InvoiceReminders.Application/     puertos, ProcessRemindersUseCase, ReminderEmailComposer, plantillas, DTOs
│  ├─ InvoiceReminders.Infrastructure/  Mongo (repositorios, semilla, índices), MailKit, health checks, options
│  └─ InvoiceReminders.Api/             Program.cs, Endpoints/, Concurrency/, Errors/, Health/, Dockerfile
│                                       (wwwroot/ con el build de React solo existe dentro de la imagen)
├─ tests/
│  └─ InvoiceReminders.UnitTests/       xunit.v3 (xunit.v3.mtp-off), sin Docker ni red; dobles a mano en Fakes/
└─ web/                                 React 19 + Vite 8 + TypeScript (npm run dev para desarrollo)
```

## Documentación

- [docs/ARQUITECTURA.md](docs/ARQUITECTURA.md): capas, diagramas (componentes, estados, secuencia), modelo de datos, plantillas de correo, tabla de inyección de dependencias.
- [docs/DECISIONES.md](docs/DECISIONES.md): índice de los 10 ADR en `docs/adr/`.
- [docs/VERIFICACION.md](docs/VERIFICACION.md): checklist del revisor con capturas, incluido el escenario de SMTP caído.

## Troubleshooting

| Síntoma | Qué hacer |
|---|---|
| `port is already allocated` o `address already in use` al levantar | Copie `.env.example` a `.env` y cambie `APP_PORT`, `MAILPIT_PORT`, `SMTP_PORT` o `MONGO_PORT`. |
| Docker Desktop sin memoria o WSL2 lento (build que se cuelga, `mongo` nunca sano) | Asigne al menos 4 GB a WSL2 (`.wslconfig`), reinicie Docker Desktop y repita `docker compose up --build`. |
| Proxy corporativo: falla `npm install` (o `npm ci`) o `dotnet restore` dentro del build | Configure el proxy en Docker Desktop (Settings > Resources > Proxies) y, si aplica, los certificados de la empresa. |
| El contenedor `app` termina (`Exited`) | `docker compose logs app`: la última línea nombra la causa (variable faltante o inválida, MongoDB inaccesible, error al sembrar). No hay `restart`, por lo que el error no se oculta en un bucle. |
| La interfaz no refleja un cambio tras actualizar la imagen | El navegador cacheó la SPA: recargue sin caché (Ctrl+F5). |
| `mongo` se reinicia o muere al instante en un equipo x86 antiguo o una VM | MongoDB 5.0 y posteriores exigen AVX en x86-64 (`mongo:8.0` incluido). No hay tag alternativo soportado: use otra máquina o habilite AVX en la VM. ARM64/Apple Silicon no se ve afectado. |
