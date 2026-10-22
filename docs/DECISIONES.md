# Decisiones de arquitectura (índice de ADR)

Cada ADR (*Architecture Decision Record*) documenta en una página una decisión de diseño del proyecto: contexto, decisión, alternativas consideradas, consecuencias y referencias. Los ADR viven en `docs/adr/` con numeración única y no se reescriben una vez aceptados; un cambio posterior se registra con un ADR nuevo que reemplaza al anterior.
Convención de estados: **Propuesta** (en discusión), **Aceptada** (vigente), **Reemplazada por ADR NNNN** (sustituida por una decisión posterior) y **Obsoleta** (ya no aplica y no tiene sustituta). Todos los ADR de esta lista fueron confirmados el 2026-10-21.

| # | Decisión | Estado | Resumen |
|---|---|---|---|
| 0001 | [Versión de .NET y Visual Studio](adr/0001-version-dotnet-y-visual-studio.md) | Aceptada | .NET 10 LTS con Visual Studio 2026 (18.x); plan B `net8.0` en una línea de `Directory.Build.props` más tags de imagen. |
| 0002 | [Minimal API](adr/0002-minimal-api.md) | Aceptada | Minimal API con `MapGroup("/api")`, `TypedResults`, `OperationGate` + `SingleOperationFilter` (409), OpenAPI nativo y Scalar en `/scalar`; sin Controllers. |
| 0003 | [Disparo manual y un correo por factura](adr/0003-disparo-manual-y-un-correo-por-factura.md) | Aceptada | `POST /api/reminders/run` y botón; nunca al arrancar ni por temporizador; un correo y un paso de estado por factura y ejecución. |
| 0004 | [Orden envío → actualización con CAS](adr/0004-orden-envio-actualizacion-cas.md) | Aceptada | Enviar y luego `UpdateOneAsync` filtrando por estado anterior (`MatchedCount == 1`) más `$push` de historial; sin reintentos, transacciones ni outbox; token de apagado, no del request, atendido solo entre facturas; el lote se detiene si MongoDB falla (`RunCompletion`). |
| 0005 | [Contrato de datos e idioma](adr/0005-contrato-de-datos-e-idioma.md) | Aceptada | Código en inglés; colecciones, campos y literales de estado en español; `_id` de negocio en texto; `ejecucionId` como string; frontera en `DocumentMappers`. |
| 0006 | [Frontend React servido desde la API](adr/0006-frontend-react-servido-desde-la-api.md) | Aceptada | React 19 + Vite 8 + TypeScript strict, compilado en el Dockerfile y servido desde `wwwroot`; un origen, sin CORS, el revisor no necesita Node. |
| 0007 | [MailKit y Mailpit](adr/0007-mailkit-y-mailpit.md) | Aceptada | Transporte MailKit 4.18.1 y capturador Mailpit v1.31.6; sin emisor simulado; plantillas por interpolación con `HtmlEncode`. |
| 0008 | [Estrategia de pruebas](adr/0008-estrategia-de-pruebas.md) | Aceptada | Un solo proyecto de pruebas unitarias con `xunit.v3.mtp-off` 4.0.2 (variante oficial de xunit.v3 sin Microsoft Testing Platform) y `dotnet test` clásico (VSTest); dobles escritos a mano en `Fakes/` (sin NSubstitute); sin Testcontainers ni pruebas de integración; warnings como errores solo con `ContinuousIntegrationBuild=true`. |
| 0009 | [Despliegue con compose y semilla](adr/0009-despliegue-compose-y-semilla.md) | Aceptada | `docker compose up --build` con app + mongo + mailpit en loopback y sin `restart`; semilla opt-in en la aplicación (`MongoDemoDataSeeder`); CI en GitHub Actions que compila, prueba y recorre la guía de verificación contra el compose real. |
| 0010 | [Repositorio público y confidencialidad](adr/0010-repositorio-publico-y-confidencialidad.md) | Aceptada (revisada) | Repositorio público; la confidencialidad se garantiza por el contenido: nombre neutral, nunca el enunciado ni la empresa evaluadora, planeación interna fuera de git. Sustituye la decisión inicial de repositorio privado. |

## Supuestos que no son ADR

Van a la sección "Alcance y supuestos" del documento de entrega (`docs/ENTREGA.md`), no a un ADR propio:

- La fuente de datos son documentos de MongoDB creados y sembrados por el proyecto; no hay ingesta de archivos. Punto de extensión: `IInvoiceRepository`.
- "Cliente" es el registro deudor dueño de las facturas, con un correo de contacto único; "tres clientes" son tres registros de cliente en una misma base.
- Las facturas en estados distintos de `primerrecordatorio` y `segundorecordatorio` se ignoran: ni correo ni cambio de estado; se muestran en la UI.
- No hay ventanas de tiempo entre recordatorios: una factura avanza un paso por ejecución.
- La demo no tiene autenticación en la API, en Mongo ni en Mailpit; los puertos se publican solo en loopback.
