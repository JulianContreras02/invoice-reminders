# ADR 0004: Orden envío → actualización con CAS, sin reintentos ni transacciones

**Estado:** Aceptada · **Fecha:** 2026-10-21

## Contexto

El enunciado fija un orden: el estado de la factura avanza **solo después** de enviar el correo. SMTP no es transaccional y MongoDB en compose corre como instancia única (standalone), sin replica set: el driver no reintenta escrituras y las transacciones multi-documento no están disponibles. Además, el revisor debe poder demostrar CA-08 (SMTP caído → estado intacto, fallo registrado, el lote continúa) con `docker compose stop mailpit`.

Hay tres fallos que distinguir: el correo no sale; el correo sale y otro actor movió la factura entre lectura y escritura; el correo sale y la escritura lanza una excepción.

## Decisión

- Orden por factura, con capturas de excepción acotadas al envío y a la actualización: `ReminderPolicy.TryGetTransition` → validación del correo del cliente (`Customer.HasDeliverableEmail()`) → `IReminderEmailComposer.Compose` → `IEmailSender.SendAsync` → `IInvoiceRepository.TryAdvanceStatusAsync`. `ProcessRemindersUseCase` es la única clase que conoce este orden.
- La actualización es un **compare-and-set (CAS)**: `MongoInvoiceRepository.TryAdvanceStatusAsync` ejecuta `UpdateOneAsync` con filtro `{_id, estado: expectedCurrent}` y `Update.Set(estado, next).Set(actualizadoEn, at).Push(historialEstados, {de, a, fecha, motivo, ejecucionId, mensajeId})`; devuelve `MatchedCount == 1`. No existe en la interfaz ningún `UpdateStatus(id, nuevo)` incondicional.
- `StatusChange(Guid RunId, string? MessageId, DateTimeOffset At, string Reason)` con `Reason = "recordatorio"` es lo que el CAS añade al historial; `ejecucionId` se persiste como string.
- Desenlaces por factura (`OutcomeKind`): `SentAndUpdated`, `EmailFailed` (estado intacto; el lote continúa con la siguiente factura), `UpdateConflict` (CAS devuelve `false`), `UpdateFailedAfterSend` (CAS lanza; log Error con `MessageId`), `SkippedNoEmail`. Una factura devuelta en un estado sin transición no es un desenlace: se registra un Error y se omite. Los contadores de `ReminderRunResult` cumplen `Candidates = EmailsSent + EmailFailed + Skipped` y `EmailsSent = Updated + UpdateConflicts + UpdateFailedAfterSend`; `UpdateConflicts` se muestra como aviso, no como fallo.
- **Sin reintento** del CAS y **sin transacciones** multi-documento: cada unidad es un documento y `$set` + `$push` son atómicos.
- **Corte del lote si MongoDB falla.** Si falla la lectura de las facturas de un cliente o la actualización tras un envío (`UpdateFailedAfterSend`), el lote se **detiene** y devuelve lo ya procesado: con la persistencia caída, cada correo adicional sería un duplicado seguro en la siguiente ejecución. Un `EmailFailed`, en cambio, no detiene el lote.
- El lote recibe `IHostApplicationLifetime.ApplicationStopping` como token, no `HttpContext.RequestAborted`; el apagado se atiende solo **entre** facturas y entre clientes. Cada factura es una unidad de trabajo: el envío (`SendAsync` con `CancellationToken.None`, acotado por el tiempo límite propio del SMTP) y el CAS (`CancellationToken.None`) no se interrumpen. Un `OperationCanceledException` por timeout del `SmtpClient` se traduce en `MailKitEmailSender` a `EmailDeliveryException`.
- `RunCompletion { Completed, Cancelled, StoppedPersistenceUnavailable }` (propiedad `Completion` de `ReminderRunResult`) dice cómo terminó la ejecución; `HasFailures` también es `true` con `StoppedPersistenceUnavailable`.
- El endpoint devuelve 200 con `ReminderRunResult` aunque haya fallos parciales o el lote se haya detenido (`HasFailures`, `Completion`); 4xx/5xx solo si el proceso no pudo iniciarse.

## Alternativas consideradas

- **Actualizar primero y luego enviar.** Contradice el orden literal del enunciado y dejaría facturas avanzadas sin correo. Descartada.
- **Transacciones multi-documento.** Exigirían replica set en compose (más configuración para el revisor) y no evitarían el problema real: el correo ya salió cuando falla la escritura. Descartadas.
- **`BoundedRetry` sobre el CAS.** Añadía clase, registro, `TimeProvider` adicional y pruebas sin cambiar el desenlace relevante; contra un Mongo local, un fallo de escritura tras el envío no mejora reintentando de inmediato. Eliminado en la revisión por sobreingeniería.
- **Outbox (registrar el envío antes de enviar).** Exigiría una colección y un reconciliador más, y tampoco eliminaría el duplicado: solo lo haría detectable antes. Se declara como evolución.
- **Lease distribuido en Mongo con TTL.** Solo necesario con varias instancias, que están fuera de alcance; documentado como solución futura.

## Consecuencias

Positivas:

- Idempotencia por diseño: la unidad de trabajo es (factura, transición) y el filtro por estado anterior es la única vía de cambio; una factura avanza exactamente un paso por ejecución y la tercera ejecución sobre la semilla es un no-op.
- Seguro ante carreras: el filtro por estado anterior es atómico en MongoDB, así que de dos CAS concurrentes sobre la misma factura solo uno encuentra el documento (`MatchedCount == 1`). El doble `InMemoryInvoiceRepository` reproduce esa semántica y `ProcessRemindersUseCaseTests` cubre el desenlace `UpdateConflict`; una prueba de concurrencia contra MongoDB real queda como evolución (ADR 0008).
- CA-08 demostrable sin código especial: con Mailpit detenido las candidatas quedan `EmailFailed`, `/health/ready` reporta `Degraded` y la UI muestra el chip "SMTP no disponible"; al levantarlo, la siguiente ejecución las procesa.
- Cerrar la pestaña del navegador no interrumpe un lote a medias, y un apagado ordenado espera a que termine la factura en curso: nunca deja un correo enviado sin su actualización.
- Con MongoDB caído el lote deja de enviar en cuanto lo detecta, en lugar de acumular correos que la siguiente ejecución repetiría.

Negativas y lo que se acepta a cambio:

- Los correos no son idempotentes. Si el proceso muere entre envío y actualización, la factura sigue en el estado anterior y la siguiente ejecución reenvía un correo con el mismo prefijo de `Message-ID` (`{numeroFactura}.{estadoAnterior}.…`) y otro `runId`. Se acepta: es rastreable en Mailpit, en `historialEstados`, en el log y en el resultado.
- Un corte por MongoDB caído deja sin revisar las facturas restantes hasta la siguiente ejecución; se acepta porque el resultado lo muestra (`StoppedPersistenceUnavailable`) y es preferible a enviar correos sin registro.
- Con varias instancias el CAS evita avanzar dos veces, pero podría duplicarse un correo. Fuera de alcance; mitigación futura con lease y TTL.
- Sin retroalimentación en tiempo real durante el lote: el resultado llega al terminar. Con 10 facturas es aceptable.

## Referencias

- `docs/ARQUITECTURA.md`: diagrama de secuencia y modelo de datos (`historialEstados`).
- `docs/VERIFICACION.md`: escenario CA-08 paso a paso.
- ADR 0003 (un correo por factura) y ADR 0008 (pruebas del orden y de los desenlaces).
