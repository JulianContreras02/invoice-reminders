# ADR 0010: Repositorio público y confidencialidad del enunciado

**Estado:** Aceptada · **Fecha:** 2026-10-21 (revisada: sustituye la decisión inicial de repositorio privado)

## Contexto

La entrega consiste en un repositorio en GitHub más un documento con arquitectura, decisiones, progreso y pasos de despliegue. La prueba técnica está sujeta a una cláusula de confidencialidad: el enunciado no puede reproducirse y la empresa evaluadora no debe aparecer asociada a la descripción de la prueba.

Al mismo tiempo, el equipo revisor debe poder clonar el repositorio y seguir el README sin fricción, y el candidato quiere que el proyecto sea presentable como trabajo propio.

La decisión inicial fue un repositorio privado con invitación a los revisores. Se revisó al publicar el repositorio: la confidencialidad depende de lo que el repositorio contiene, no de quién puede verlo, y el contenido ya cumplía todas las reglas de esta decisión.

## Decisión

- **Repositorio público en GitHub** (`invoice-reminders`). El equipo revisor accede sin invitaciones ni cuentas adicionales, y puede ver el historial de commits y las ejecuciones del CI.
- **La confidencialidad se garantiza por el contenido**, con estas reglas:
  - **Nombre neutral:** `invoice-reminders`, también en el README y en el documento de entrega.
  - **Nunca el enunciado literal.** El problema se describe con palabras propias en el README y en `docs/ENTREGA.md`. Sin PDF ni capturas del enunciado.
  - **Nunca el nombre de la empresa evaluadora**, ni en código, commits, documentación ni nombres de archivo. Se habla de "la prueba técnica" y "el equipo revisor".
  - **El material interno de planeación no se publica:** `docs/planeacion/` está en `.gitignore`.
- **Sin credenciales reales:** el SMTP real se configura solo por variables de entorno (`Smtp__Username`, `Smtp__Password`) que no se versionan; la semilla usa el dominio reservado `.test`.
- **Sin datos reales:** clientes y facturas de la semilla son ficticios (`Pérez & Asociados S.A.S.`, `Rodríguez Abogados Ltda.`, `Consultorio Jurídico Gómez`).
- Entrega final sobre un tag (`v1.0.0`) con historial de commits legible, sin placeholders ni TODO.

## Verificación

Antes de dar por buena la visibilidad pública se revisó el historial completo con `git log --all -p`: ningún commit menciona a la empresa evaluadora ni reproduce el enunciado, y `docs/planeacion/` nunca se versionó. La misma revisión se repite antes de crear el tag de entrega.

## Alternativas consideradas

- **Repositorio privado con invitación a los revisores** (decisión inicial). Restringe la visibilidad, pero añade un paso administrativo (conseguir los usuarios de GitHub e invitarlos) y no aporta confidencialidad adicional si el contenido ya cumple las reglas. Sustituida.
- **Incluir el enunciado como anexo "para contexto".** Violaría la cláusula; se sustituye por la paráfrasis y la tabla requisito → evidencia.
- **Compartir un ZIP en lugar de un repositorio.** Perdería el historial de commits y el CI que verifica el despliegue en una máquina limpia. Descartado.

## Consecuencias

Positivas:

- El revisor clona y verifica sin pasos previos; el CI en verde es visible para cualquiera.
- El proyecto sirve como portafolio sin reescribir nada.
- La paráfrasis obliga a explicar el problema con criterio propio, lo que suma al criterio de documentación.

Negativas y lo que se acepta a cambio:

- Cualquier persona puede ver la solución. Se acepta porque no contiene el enunciado ni identifica el proceso de selección.
- Un descuido futuro (un commit con el nombre de la empresa o el enunciado) quedaría publicado en el historial. Mitigación: la revisión del historial antes del tag y la regla recordada en `docs/DECISIONES.md`.

## Referencias

- `README.md`: descripción neutral.
- `docs/ENTREGA.md`: encabezado neutral, enlace al repositorio y tag evaluado.
- `.gitignore`: exclusión de `docs/planeacion/`.
