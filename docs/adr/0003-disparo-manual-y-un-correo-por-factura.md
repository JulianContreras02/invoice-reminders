# ADR 0003: Disparo manual del proceso y un correo por factura

**Estado:** Aceptada · **Fecha:** 2026-10-21

## Contexto

El proceso de recordatorios toma las facturas de un cliente en `primerrecordatorio` o `segundorecordatorio`, envía el correo correspondiente y, solo después, avanza el estado. El enunciado no define cuándo se ejecuta ni si los correos se agrupan por cliente.

Restricciones que pesan en la decisión:

- Lo que hace el revisor es clonar, `docker compose up --build`, abrir la UI y ver 10 facturas de 3 clientes **con la semilla intacta**, pulsar "Ejecutar recordatorios" y comprobar el antes y el después (7 correos, luego 4, luego 0).
- Los criterios de aceptación piden un paso por ejecución (CA-10) y que cada cliente reciba únicamente los correos de sus facturas (CA-11).
- El proceso debe servir tanto para un cliente concreto como para todos.

## Decisión

- **Disparo manual** mediante `POST /api/reminders/run` (todos los clientes) o `POST /api/reminders/run?customerId=cli-002` (un cliente, por query string; sin cuerpo), y el botón correspondiente en la UI ("Ejecutar recordatorios (todos)" o "Ejecutar solo para {cliente}"). **Nunca se ejecuta al arrancar** ni por temporizador.
- `ProcessRemindersUseCase.RunAsync(string? customerId, ct)` está parametrizado por cliente; sin `customerId` orquesta todos los clientes ordenados por Id, secuencialmente. Un `customerId` inexistente produce `CustomerNotFoundException` → 404.
- **Un correo por factura elegible.** Cada factura genera exactamente un `EmailMessage` compuesto por `ReminderEmailComposer`, un envío y, si el envío se acepta, una transición de estado con una entrada en `historialEstados`.
- Cada ejecución avanza una factura **exactamente un paso**: `primerrecordatorio → segundorecordatorio` o `segundorecordatorio → desactivado`, según `ReminderPolicy.Transitions`. No hay ventanas de tiempo entre recordatorios.
- Los estados no contemplados (`pagada`, `desactivado`, `pendiente` u otro literal) no entran en memoria: el filtro Mongo es `{ clienteId, estado: { $in: ["primerrecordatorio", "segundorecordatorio"] } }`.
- Para repetir la demo existe `POST /api/admin/seed/reset` y el botón "Reiniciar datos de demostración"; vaciar Mailpit es un paso documentado aparte (ADR 0007 y 0009).

## Alternativas consideradas

- **`BackgroundService` programado activo.** Ejecutaría el proceso periódicamente sin intervención. Se descarta porque alteraría los datos semilla antes de que el revisor los viera y haría la demo no determinista. Se describe como extensión del mismo caso de uso, deshabilitada.
- **Ejecución al arrancar la aplicación.** Mismo problema: el revisor nunca vería el estado inicial. Descartada.
- **Correo consolidado por cliente** (un mensaje con todas sus facturas). Reduce el número de correos, pero rompe la relación 1 correo = 1 transición = 1 entrada de historial y complica la regla "sin envío no hay cambio" cuando un mismo correo cubre facturas en estados distintos. Queda como mejora documentada.
- **Varios pasos por ejecución** (desactivar directamente una factura en primer recordatorio si ya venció hace tiempo). El enunciado no define ventanas de tiempo; se descarta por inventar reglas de negocio.

## Consecuencias

Positivas:

- Demo determinista y repetible: la secuencia de 7, 4 y 0 correos se narra en `docs/VERIFICACION.md` y se afirma en `DemoDataTests` (caso de uso real sobre los dobles en memoria con la semilla real).
- Trazabilidad completa: cada correo lleva `Message-ID` determinista `{numeroFactura}.{estadoAnterior}.{runId sin guiones}@invoice-reminders.test` (guardado sin corchetes angulares) y cabeceras `X-Invoice-Id`, `X-Reminder-Kind`, `X-Reminder-Run-Id`, y corresponde a una entrada de `historialEstados`.
- La regla "sin envío no hay cambio" es trivial de implementar y de probar por unidad (ADR 0004).
- Cambiar a disparo programado o a consolidación no toca `ReminderPolicy` ni los puertos: es otra forma de invocar el mismo caso de uso.

Negativas y lo que se acepta a cambio:

- Un cliente con varias facturas elegibles recibe varios correos en la misma ejecución; la semilla lo hace visible a propósito (cli-002 recibe dos correos en la primera ejecución). Se acepta como lectura literal del enunciado.
- No hay automatización: en un entorno real haría falta un planificador externo o el `BackgroundService` descrito como extensión.
- El lote corre dentro de la petición HTTP; con 10 facturas es inmediato, pero a escala exigiría un trabajo en segundo plano con seguimiento por `runId`.

## Referencias

- `docs/ARQUITECTURA.md`: diagrama de estados y de secuencia.
- `docs/VERIFICACION.md`: guion 7 → 4 → 0 y reinicio de la demo.
- ADR 0004 (orden envío → actualización) y ADR 0009 (semilla y reinicio).
