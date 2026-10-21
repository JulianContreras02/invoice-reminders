# ADR 0010: Repositorio privado y confidencialidad del enunciado

**Estado:** Aceptada · **Fecha:** 2026-10-21

## Contexto

La entrega consiste en un repositorio en GitHub más un documento con arquitectura, decisiones, progreso y pasos de despliegue. La prueba técnica está sujeta a una cláusula de confidencialidad: el enunciado no puede reproducirse y la empresa evaluadora no debe aparecer asociada a la descripción de la prueba en un lugar público.

Al mismo tiempo, el equipo revisor debe poder clonar el repositorio y seguir el README sin fricción, y el candidato quiere que el proyecto sea presentable como trabajo propio.

## Decisión

- **Repositorio privado en GitHub con invitación a los revisores.** Se pregunta al equipo revisor a qué usuarios invitar; si prefieren un repositorio público, se publica sin cambiar nada de su contenido.
- **Nombre neutral:** `invoice-reminders`. Título neutral también en el documento de entrega y en el README.
- **Nunca el enunciado literal.** El problema se describe con palabras propias en el README y en `docs/ENTREGA.md`; la lectura de los requisitos se parafrasea. Sin PDF ni capturas del enunciado en el repositorio.
- **Nunca el nombre de la empresa junto a la descripción de la prueba**, ni en código, commits, documentación ni nombres de archivo. Se habla de "la prueba técnica" y "el equipo revisor".
- **Sin credenciales reales:** el SMTP real se configura solo por variables de entorno (`Smtp__Username`, `Smtp__Password`) que no se versionan; la semilla usa el dominio reservado `.test`.
- **Sin datos reales:** clientes y facturas de la semilla son ficticios (`Pérez & Asociados S.A.S.`, `Rodríguez Abogados Ltda.`, `Consultorio Jurídico Gómez`).
- El documento de entrega omite el "nivel de confianza" interno de la lectura de requisitos.
- Entrega final sobre un tag (`v1.0.0`) con historial de commits legible, sin placeholders ni TODO.

## Alternativas consideradas

- **Repositorio público desde el inicio.** Facilita el acceso y sirve de portafolio, pero asociaría públicamente el proyecto a un proceso de selección confidencial. Se descarta como opción por defecto; sigue siendo viable si el equipo revisor lo prefiere, porque el contenido ya cumple las reglas anteriores.
- **Incluir el enunciado como anexo "para contexto".** Violaría la cláusula; se sustituye por la paráfrasis y la tabla requisito → evidencia.
- **Repositorio privado sin invitaciones, compartiendo un ZIP.** Perdería el historial de commits y la posibilidad de verificar el guion desde un clon limpio. Descartado.

## Consecuencias

Positivas:

- Cumplimiento de la confidencialidad pactada con independencia de la visibilidad final del repositorio.
- El repositorio puede hacerse público más adelante sin reescribir nada.
- La paráfrasis obliga a explicar el problema con criterio propio, lo que suma al criterio de documentación.

Negativas y lo que se acepta a cambio:

- Un paso administrativo adicional: obtener los usuarios de GitHub de los revisores e invitarlos antes de enviar el correo de entrega.
- Si el equipo revisor no responde, el supuesto por defecto es mantenerlo privado e invitar a quien figure en el correo de entrega; público con nombre neutral y sin el enunciado queda como segunda opción.
- Todo el equipo que colabore en el repositorio debe conocer la regla; se recuerda en `docs/DECISIONES.md` y en la sección "Lo que no va en ninguno" de la estructura documental.

## Referencias

- `README.md`: descripción neutral en dos líneas.
- `docs/ENTREGA.md`: encabezado neutral, enlace al repositorio y tag evaluado.
- `docs/DECISIONES.md`: índice de ADR.
