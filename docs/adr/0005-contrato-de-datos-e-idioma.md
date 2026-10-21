# ADR 0005: Contrato de datos e idioma

**Estado:** Aceptada · **Fecha:** 2026-10-21

## Contexto

El enunciado impone un contrato visible en la base: el campo `estado` y sus valores (`primerrecordatorio`, `segundorecordatorio`, `desactivado`) son lo que el equipo revisor consultará en MongoDB para verificar CA-06 y CA-07. No se entregó esquema ni datos de ejemplo: el candidato crea la base y la semilla.

A la vez, el criterio de Clean Code se evalúa sobre el código fuente, y la convención profesional en .NET es código, namespaces y pruebas en inglés. Hace falta decidir dónde está la frontera entre ambos idiomas y cómo se identifican las facturas.

## Decisión

- **Código, namespaces y pruebas en inglés.** Nombres como `Invoice`, `Customer`, `InvoiceStatus`, `ReminderPolicy`, `ProcessRemindersUseCase`, `MongoInvoiceRepository`; pruebas con nombres `Metodo_Escenario_Resultado`.
- **Mongo en español**: base `invoice_reminders`, colecciones `clientes` y `facturas`, campos `nombre`, `email`, `creadoEn`, `clienteId`, `monto`, `moneda`, `fechaEmision`, `fechaVencimiento`, `estado`, `historialEstados` (`de`, `a`, `fecha`, `motivo`, `ejecucionId`, `mensajeId`), `actualizadoEn`.
- **Literales de estado exactos**: minúsculas, sin espacios, comparación ordinal. Centralizados en `readonly record struct InvoiceStatus` (constructor privado, propiedad `Value`) con los estáticos `FirstReminder`, `SecondReminder`, `Deactivated`, `Paid`, `Pending`; `From(string)` conserva cualquier literal desconocido sin normalizar. Es el único lugar del código con las cadenas del contrato.
- **`_id` de negocio en texto**: el `_id` de la factura **es** su número (`FAC-2026-0102`) y el del cliente es `cli-001`; no existe campo `numero` ni índice único sobre él. El dominio expone `Invoice.Id` sin campo `Number` redundante.
- `ejecucionId` se persiste como **string** (`runId.ToString("D")`); no se serializa ningún `Guid` y no hace falta `GuidSerializer`.
- Fechas en UTC; `monto` como `Decimal128` (declarado con `[BsonRepresentation(BsonType.Decimal128)]`).
- **Frontera de traducción explícita** en Infrastructure: `InvoiceDocument`, `CustomerDocument` y `StatusChangeDocument` (`internal`, con `[BsonElement("estado")]` y similares) más `DocumentMappers`. El dominio no lleva atributos Bson.
- **UI y correos en español** (etiquetas legibles en `statusLabels.ts`, montos en `es-CO`, plantillas A y B en español). API JSON en camelCase con nombres en inglés (`countsByStatus`, `emailsSent`).
- Índices declarativos: `facturas {clienteId:1, estado:1}` (`ix_clienteId_estado`) y `clientes {email:1} unique` (`ux_email`), creados de forma idempotente por `MongoIndexInitializer`.

## Alternativas consideradas

- **Todo en español** (código incluido). Evitaría la frontera de traducción, pero va contra la convención profesional y dificulta el uso de los tipos del framework. Descartada.
- **Todo en inglés** (incluidos campos y valores de Mongo). Rompería el contrato visible: el revisor buscaría `estado: "segundorecordatorio"` y no lo encontraría. Descartada.
- **`ObjectId` como `_id` más campo `numero` con índice único.** Es el patrón por defecto del driver, pero añade un índice único y un identificador opaco sin beneficio a esta escala; complica la semilla determinista y la lectura en la UI, en Mailpit y en Compass. Descartada.
- **Normalizar literales de estado** (trim, minúsculas) al leer. Ocultaría datos inconsistentes; se prefiere conservar el literal y tratar lo desconocido como no elegible, visible en la UI en gris.

## Consecuencias

Positivas:

- Lo que el revisor ve en Mongo coincide exactamente con el enunciado; lo que ve en el código sigue la convención del sector.
- Cero cadenas mágicas fuera de `InvoiceStatus`; `ReminderPolicyTests` e `InvoiceStatusTests` fijan el contrato.
- `_id` legible simplifica la semilla, la UI, los asuntos de correo y el `Message-ID` determinista (`FAC-2026-0102.segundorecordatorio.{runId sin guiones}@invoice-reminders.test`).
- Una futura ingesta de facturas desde otra fuente escribiría en la misma colección a través de otra `IInvoiceRepository` sin tocar el proceso.

Negativas y lo que se acepta a cambio:

- Mapeo manual en `DocumentMappers` que hay que mantener en paralelo con los records del dominio; se acepta porque son tres documentos y el dominio queda libre de atributos de persistencia.
- Mezcla de idiomas visible al leer la capa Infrastructure; se mitiga concentrándola en un solo directorio y explicándola en `docs/ARQUITECTURA.md`.
- Un `_id` de negocio obliga a que la semilla y cualquier ingesta futura garanticen unicidad del número de factura por sí mismas.

## Referencias

- `docs/ARQUITECTURA.md`: modelo de datos, índices y diagrama de estados.
- `README.md`: variables de entorno `Mongo__ConnectionString` y `Mongo__Database`.
- ADR 0003 (un correo por factura) y ADR 0009 (semilla).
