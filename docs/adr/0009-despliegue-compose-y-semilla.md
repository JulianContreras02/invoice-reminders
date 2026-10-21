# ADR 0009: Despliegue con docker compose y semilla en la aplicación

**Estado:** Aceptada · **Fecha:** 2026-10-21

## Contexto

El guion del revisor es: clonar, ejecutar un único comando, abrir la UI y ver 3 clientes con 10 facturas sin intervención manual (CA-13). El requisito previo debe ser solo Docker Desktop: ni .NET ni Node. La demo corre sin autenticación en Mongo ni en Mailpit, en la máquina del revisor. Un error de configuración debe ser visible en el log, no un bucle de reinicios.

Además, la misma semilla debe servir en compose, en F5 desde Visual Studio (API local), en el endpoint de reinicio y en las pruebas, y nunca debe sembrar un entorno real por accidente.

## Decisión

- **Comando único:** `docker compose up --build` desde la raíz. `docker-compose.yml` es canónico y autosuficiente con tres servicios: `mongo` (`mongo:8.0`), `mailpit` (`axllent/mailpit:v1.31.6`) y `app` (imagen `invoice-reminders-app:local`). URLs: UI `http://localhost:8080`, documentación de la API `/scalar` (OpenAPI en `/openapi/v1.json`), salud `/health/ready`, correos `http://localhost:8025`. Borrado total: `docker compose down -v`; diagnóstico: `docker compose logs app`.
- **Puertos en loopback:** `127.0.0.1:${APP_PORT:-8080}:8080`, `127.0.0.1:${MAILPIT_PORT:-8025}:8025`, `127.0.0.1:${SMTP_PORT:-1025}:1025`, `127.0.0.1:${MONGO_PORT:-27017}:27017`. `.env.example` con esas cuatro variables es lo único que el revisor tocaría si un puerto está ocupado.
- **Sin `restart`** en `app`: con configuración inválida el contenedor sale con código ≠ 0 y se queda parado para que la causa aparezca en la última línea del log.
- `depends_on`: `mongo` con `condition: service_healthy` (healthcheck con `mongosh` y `db.adminCommand('ping').ok`); `mailpit` también con `service_healthy` (su imagen declara `HEALTHCHECK` con `/mailpit readyz`). Healthcheck de `app` con `bash -c '</dev/tcp/127.0.0.1/8080'` (sin `apt-get` ni `curl`).
- **Dockerfile** (`src/InvoiceReminders.Api/Dockerfile`, contexto raíz), etapas en este orden: `base` (`aspnet:10.0` Ubuntu 24.04, `USER $APP_UID`, `EXPOSE 8080`) primero, siguiendo la convención del modo rápido de contenedores de Visual Studio; `web` (`node:22-alpine`, `npm ci` con el lock versionado y `npm run build`); `build` (`sdk:10.0`), que copia `global.json`, los props y los `.csproj` **uno por `COPY` preservando rutas**, **no** copia la `.slnx`, hace `dotnet restore src/InvoiceReminders.Api/InvoiceReminders.Api.csproj` y `dotnet publish -c Release --no-restore -o /out`; `final` (`FROM base`) copia `/out` y `/web/dist` a `wwwroot`. `.dockerignore` excluye `**/bin`, `**/obj`, `**/.vs`, `**/node_modules`, `**/*.user`, `web/dist`, `src/InvoiceReminders.Api/wwwroot`, `.git`, `.github`, `docs`, `tests`, `*.md` y `.env`.
- **Semilla en la aplicación:** `MongoDemoDataSeeder` (Infrastructure/Seed) con `DemoData` estático, `enum SeedMode { Never, IfEmpty, Always }` y `SeedResult(Mode, Applied, Customers, Invoices)`. La extensión `InitializeInfrastructureAsync` (`Infrastructure/DependencyInjection.cs`), llamada desde `Program.cs`, ejecuta esperar Mongo (`MongoStartupWaiter`, 15 intentos con 2 s entre ellos) → índices (`MongoIndexInitializer`) → semilla **antes** de abrir el puerto; si algo falla (incluido un error al sembrar), el proceso registra el error y termina con código 1.
- **Opt-in:** `SeedOptions { Mode = Never, AllowReset = false }` por defecto; `Seed__Mode: IfEmpty` y `Seed__AllowReset: "true"` solo en compose y en `appsettings.Development.json`. `POST /api/admin/seed/reset` (modo `Always`: borra e inserta los mismos `_id`) responde 403 si `AllowReset=false`.
- Variables de `app` en compose: `Mongo__ConnectionString: mongodb://mongo:27017`, `Mongo__Database: invoice_reminders`, `Smtp__Host: mailpit`, `Smtp__Port: "1025"`, `Smtp__Security: None`, `Smtp__FromAddress`, `Smtp__FromName`, `Links__MailpitUrl`.
- No hay `docker-compose.vs.yml` ni `docker-compose.dcproj`: depurar con F5 dentro de Docker Compose en Visual Studio queda como evolución; hoy se depura la API local con `docker compose up -d mongo mailpit`. Un `docker-compose.override.yml` generado por el asistente de Visual Studio está en `.gitignore`.
- **CI en GitHub Actions** (`.github/workflows/ci.yml`) en cada push y pull request a `main`: `backend` compila con `ContinuousIntegrationBuild=true`, ejecuta `dotnet test` y comprueba con `dotnet list` que Application no depende de paquetes de infraestructura; `frontend` ejecuta `npm ci` y `npm run build`; `compose-smoke` hace `docker compose up -d --build --wait` y ejecuta `.github/scripts/compose-smoke.sh`, que comprueba salud, 10 facturas de 3 clientes, la historia 7 → 4 → 0 contra la API y la bandeja de Mailpit, el reinicio de datos, el 404 de `/api/*`, la SPA y el documento OpenAPI. No añade pasos al revisor; el mismo script sirve en local con Docker.

## Alternativas consideradas

- **Scripts en `docker-entrypoint-initdb.d` de la imagen de Mongo.** Solo corren con el volumen vacío, no sirven para F5 ni para el endpoint de reinicio, y duplican el conocimiento del esquema fuera del código. Descartados.
- **`restart: unless-stopped`.** Ocultaría errores de configuración tras un bucle de `Restarting`. Descartado.
- **Puertos en todas las interfaces.** Expondría un Mongo sin autenticación y un Mailpit a la red local de la máquina del revisor. Descartado; quien necesite acceso remoto lo cambia en su `.env`.
- **Restaurar la `.slnx` dentro del Dockerfile.** Fallaría por el `.csproj` de `tests/`, excluido por `.dockerignore`. Descartado.
- **Imágenes Alpine de .NET.** Rompen `es-CO` (ADR 0001).
- **Sin CI.** Era la opción inicial por ser una capa opcional. Se incluyó porque el entorno de desarrollo no tenía Docker: el trabajo `compose-smoke` es la única verificación automática del despliegue real en una máquina limpia, sin añadir pasos al revisor.
- **Carpeta `scripts/` con envoltorios.** "Un solo comando" no necesita seis scripts. Eliminada.

## Consecuencias

Positivas:

- El mismo código de semilla sirve en compose, F5, reinicio y pruebas, y funciona aunque el volumen ya exista.
- Un fallo de configuración se diagnostica con `docker compose logs app` en una línea.
- El build del revisor no depende de `tests/`, de la `.slnx` ni de warnings nuevos.
- La historia 7 → 4 → 0 es reproducible con "Reiniciar datos" más vaciar Mailpit.

Negativas y lo que se acepta a cambio:

- Primer build de 3-6 minutos; los siguientes tardan segundos. Se ensaya en un clon limpio sin .NET ni Node antes de entregar.
- Mongo ≥ 5.0 exige AVX en x86-64: no se promete un tag alternativo; se documenta en el troubleshooting del README.
- La compatibilidad MongoDB.Driver 3.12.0 con servidor 8.0 se verifica en cada push con el trabajo `compose-smoke` del CI (misma imagen `mongo:8.0` que usa el revisor) y con la guía manual de `docs/VERIFICACION.md`; no hay pruebas de integración con Testcontainers.
- Imágenes con tag de versión menor (`sdk:10.0`, `aspnet:10.0`, `node:22-alpine`, `mongo:8.0`) para recibir parches; Mailpit fijado a `v1.31.6`.
- Sin autenticación en la demo: decisión declarada como "solo demo local".

## Referencias

- `docs/ARQUITECTURA.md`: diagrama de contenedores.
- `README.md`: inicio rápido, tabla de URLs, variables de entorno y troubleshooting.
- `docs/VERIFICACION.md`: guion del revisor.
- ADR 0001 (imágenes base), ADR 0007 (Mailpit) y ADR 0008 (pruebas y rigor del compilador).
