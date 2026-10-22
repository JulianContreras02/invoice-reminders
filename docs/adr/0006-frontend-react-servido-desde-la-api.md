# ADR 0006: Frontend en React servido desde la imagen de la API

**Estado:** Aceptada · **Fecha:** 2026-10-21

## Contexto

La prueba técnica pide una interfaz gráfica con un resumen de todas las facturas; Angular se cita como ejemplo y los criterios de evaluación aceptan Angular, React o Flutter. La pantalla necesaria es una sola: tarjetas por estado, resumen por cliente (evidencia de los tres clientes, CA-11 y CA-12), tabla con filtros, botón de ejecución, panel de resultado y reinicio de datos.

Restricciones de despliegue: el revisor levanta todo con `docker compose up --build` y no debe necesitar Node ni ejecutar npm. En desarrollo, el candidato depura la API local en Visual Studio (con MongoDB y Mailpit en Docker) y necesita un camino de desarrollo del frontend que no dependa de MSBuild.

## Decisión

- **React 19 + Vite 8 + TypeScript strict**: React 19.3.1, Vite 8.3.5, `@vitejs/plugin-react` 6.1.2 y TypeScript 5.9.3 con `strict`, `noUnusedLocals`, `noUnusedParameters` y `noUncheckedIndexedAccess`. Node ≥ 22.12 (Vite 8 es solo ESM) declarado en `engines` de `package.json`; el Dockerfile usa `node:22-alpine`. `npm run build` es `tsc && vite build`.
- Sin router, sin gestor de estado global, sin librería de componentes: CSS plano con variables, tabla semántica, botones con texto y foco visible. Sin ESLint y sin pruebas de componentes (Vitest no está instalado): la verificación del frontend es la compilación estricta de `tsc`.
- Estructura por features en `web/src/`: `main.tsx`, `styles.css`, `api/{client.ts,types.ts,errors.ts}`, `features/invoices/{InvoicesPage,InvoiceTable,SummaryCards,CustomerSummary,InvoiceFilters,StatusBadge,statusLabels.ts,format.ts,useInvoiceData.ts,useMeta.ts,useOperation.ts}`, `features/reminders/RunResultPanel.tsx`, `features/health/HealthChip.tsx`. Las acciones (ejecutar, ejecutar por cliente, reiniciar) son botones de `InvoicesPage`; el estado y las llamadas viven en hooks: `useInvoiceData` (descarta respuestas obsoletas si cambia el filtro y recarga con el filtro vigente), `useMeta` y `useOperation` (una operación a la vez; un 409 se muestra como aviso). Las etiquetas de estado salen de una única lista, `KNOWN_STATUSES` en `statusLabels.ts`.
- Los datos vienen del backend: conteos por estado y por cliente de `GET /api/invoices`, salud de `/health/ready` (JSON por check) y la URL de Mailpit **solo** de `/api/meta` (ninguna variable `VITE_*`). El front no recalcula conteos; solo agrupa en la tarjeta "Otros estados" los conteos que el backend devuelve para estados fuera de `KNOWN_STATUSES`.
- Accesibilidad básica: botones y filtros deshabilitados durante una operación, `aria-busy` en la página, región `aria-live` montada desde el inicio para anunciar cada resultado y el detalle de salud como texto visible.
- **Servido desde la API**: la etapa `web` del Dockerfile (`node:22-alpine`) ejecuta `npm ci` con el `package-lock.json` versionado y `npm run build`, y la etapa `final` copia `/web/dist` a `wwwroot`. `Program.cs` usa `UseDefaultFiles`, `UseStaticFiles` y `MapFallbackToFile("index.html")`. Un contenedor, un puerto (`8080`), un origen.
- **Sin CORS**: en compose la UI y la API comparten origen; en desarrollo `vite.config.ts` (puerto 5173) hace proxy de `/api` y `/health` a `http://localhost:5080` (perfil `http` de la API).
- **Sin target MSBuild que ejecute npm** durante el build de Visual Studio: el desarrollo del front es `npm run dev`; el build del front ocurre solo en Docker (o a mano con `npm run build`). `wwwroot/` se genera dentro de la imagen y se ignora en git.
- `web/` no forma parte de `InvoiceReminders.slnx` (ni como carpeta ni como `.esproj`); se trabaja con `npm` desde su carpeta.

## Alternativas consideradas

- **Angular.** Es el ejemplo citado y posiblemente el framework más familiar para el equipo revisor. Se descarta por razones técnicas: una pantalla no necesita su sistema de DI, módulos ni router; el build es más pesado dentro del Dockerfile; y el código resultante en React con tipos explícitos y estructura por features es pequeño y legible para quien conozca Angular.
- **Flutter.** Aceptado por los criterios, pero orientado a aplicaciones móviles/multiplataforma; para una tabla de resumen en navegador añade toolchain sin beneficio.
- **Contenedor nginx aparte para la SPA.** Separa responsabilidades, pero añade un contenedor, un puerto y CORS o un proxy inverso que el revisor tendría que entender. Descartado.
- **Target MSBuild que ejecute `npm run build`.** Frágil: depende de Node en el PATH, falla en el modo rápido de contenedores y se comporta distinto entre Visual Studio, CLI y Docker. Descartado.
- **Variables `VITE_*` para la URL de Mailpit.** Se hornean en el build y duplican la configuración; `/api/meta` es la única fuente de verdad en runtime.

## Consecuencias

Positivas:

- El revisor no necesita Node: el frontend llega compilado dentro de la imagen de la API.
- Un contenedor y un puerto menos; sin CORS que configurar ni explicar.
- La UI no decide transiciones ni conoce Mongo: toda la lógica de negocio queda en el backend, probada por unidad; por eso se aceptó no tener pruebas de componentes por ahora (Vitest + React Testing Library quedan como evolución).

Negativas y lo que se acepta a cambio:

- El evaluador puede estar más familiarizado con Angular; se compensa con estructura por features, tipos explícitos y un alcance deliberadamente pequeño.
- Con F5 en Visual Studio la API no sirve la interfaz (`wwwroot/` solo existe en la imagen): la UI se sirve con `npm run dev` y `/scalar` funciona siempre.
- El primer `docker compose up --build` incluye la instalación de dependencias de npm y `npm run build` (3-6 minutos en total); las siguientes construcciones se cachean.
- El navegador puede cachear la SPA tras una reconstrucción; se incluye en el troubleshooting del README.

## Referencias

- `docs/ARQUITECTURA.md`: diagrama de componentes (UI → API por HTTP `/api`).
- `README.md`: inicio rápido, URL de la UI y comandos del frontend.
- ADR 0002 (API) y ADR 0009 (Dockerfile y compose).
