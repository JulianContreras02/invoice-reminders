# ADR 0002: Minimal API en lugar de Controllers MVC

**Estado:** Aceptada · **Fecha:** 2026-10-21

## Contexto

La capa HTTP del sistema es pequeña y estable: cuatro endpoints de negocio y administración bajo `/api`, dos endpoints de salud, el documento OpenAPI con su documentación interactiva y el servido de la SPA de React. El equipo revisor evalúa Clean Code, SOLID e inyección de dependencias; la capa HTTP debe ser legible de un vistazo y no esconder lógica.

Requisitos que condicionan la elección:

- Dos operaciones (`POST /api/reminders/run` y `POST /api/admin/seed/reset`) deben serializarse en una sola instancia: una segunda llamada concurrente responde 409.
- Los errores se devuelven como ProblemDetails (RFC 9457) con `traceId`, y `CustomerNotFoundException` se traduce a 404.
- OpenAPI debe estar disponible siempre (`/openapi/v1.json` con documentación interactiva en `/scalar`) para que el revisor pueda ejecutar el proceso sin la UI.
- `Program.cs` debe mantenerse en torno a 60 líneas, con la composición delegada a `AddApplication()` y `AddInfrastructure(IConfiguration)`.

## Decisión

Se usa **Minimal API** de ASP.NET Core:

- `MapGroup("/api")` como raíz; los endpoints viven en clases estáticas por recurso en `src/InvoiceReminders.Api/Endpoints/`: `InvoiceEndpoints`, `ReminderEndpoints`, `AdminEndpoints`, `MetaEndpoints`.
- Endpoints reales: `GET /api/invoices?customerId=&status=` (filas filtradas, conteos por estado y resumen por cliente en `customers[]`), `POST /api/reminders/run` (todos los clientes) o `POST /api/reminders/run?customerId=cli-002` (uno, por query string), `POST /api/admin/seed/reset`, `GET /api/meta`, `GET /health/live`, `GET /health/ready`, `GET /openapi/v1.json` y `GET /scalar`. No hay `GET /api/customers` ni `GET /api/invoices/{id}`. Cualquier otra ruta bajo `/api` responde 404 (`MapFallback("/api/{**path}")`) antes del `MapFallbackToFile("index.html")` de la SPA.
- Respuestas con `TypedResults` sobre los DTOs de Application (`InvoiceSummaryDto`, `ReminderRunResult`), `SeedResult` de Infrastructure y un `MetaDto` propio de la Api; JSON en camelCase con enums como texto (`JsonStringEnumConverter`); errores en `application/problem+json`.
- Exclusión de operaciones simultáneas en `src/InvoiceReminders.Api/Concurrency`: `OperationGate` (singleton con un `SemaphoreSlim(1,1)`) y `SingleOperationFilter` (`IEndpointFilter`), aplicado con `AddEndpointFilter<SingleOperationFilter>()` a `POST /api/reminders/run` y `POST /api/admin/seed/reset`; si no entra en la puerta de inmediato devuelve 409 ProblemDetails "Operación en curso".
- `AddProblemDetails()` y `AddExceptionHandler<ApiExceptionHandler>()`: `CustomerNotFoundException` → 404, resto → 500 con `traceId`.
- OpenAPI nativo con `AddOpenApi()`/`MapOpenApi()` (paquete `Microsoft.AspNetCore.OpenApi` 10.0.13) y documentación interactiva con `Scalar.AspNetCore` 2.18.2 (`MapScalarApiReference()`, en `/scalar`).
- `ReminderEndpoints` pasa al caso de uso `IHostApplicationLifetime.ApplicationStopping` como token, no `HttpContext.RequestAborted` (ADR 0004).
- Los endpoints consumen `IInvoiceSummaryQueries` directamente; no hay clase intermedia sin lógica.
- Sin CORS: en desarrollo Vite hace proxy y en compose la UI es del mismo origen (ADR 0006).

## Alternativas consideradas

- **Controllers MVC.** Más familiar para equipos con historial en ASP.NET MVC y con binding por convención. Se descarta porque para seis endpoints añade ceremonia (atributos de ruta, clases base, `ApiController`), y porque los filtros de endpoint y ProblemDetails ya cubren lo que aquí se necesita sin filtros de acción ni convenciones.
- **Capa de mediación (MediatR, CQRS formal).** No resuelve ningún problema de este enunciado; los casos de uso son clases concretas con un método y se inyectan directamente en los endpoints.
- **Puerta de operación única como puerto de Application (`IOperationGate`).** Fue la versión anterior del diseño; se descarta porque el doble clic es una preocupación de la capa HTTP: `OperationGate` es una clase concreta de la Api, usada solo por `SingleOperationFilter`, sin puerto ni fake adicional.

## Consecuencias

Positivas:

- Capa HTTP mínima y explícita; cada archivo de `Endpoints/` se lee como la tabla de la API de `docs/ARQUITECTURA.md`.
- `OperationGate`, `SingleOperationFilter` y `ApiExceptionHandler` son piezas de una sola responsabilidad, fáciles de mostrar como evidencia de SOLID.
- Es el estilo por defecto de las plantillas actuales de Visual Studio y de la documentación de .NET 10.

Negativas y lo que se acepta a cambio:

- No hay binding por convención y los parámetros (`customerId`, `status`) se declaran en cada handler; se acepta porque el contrato es pequeño y se documenta en OpenAPI.
- La serialización de "run" y "reset" solo vale para una instancia del proceso; varias instancias quedan fuera de alcance (ADR 0004).
- La documentación interactiva (Scalar) está habilitada en todos los entornos, incluida la imagen de compose; se acepta porque es una demo local sin autenticación (puertos en loopback, ADR 0009).

## Referencias

- `docs/ARQUITECTURA.md`: contrato de la API y diagrama de secuencia.
- `README.md`: URL de `/scalar` y uso de `InvoiceReminders.Api.http`.
- ADR 0004 (token de apagado) y ADR 0006 (mismo origen, sin CORS).
