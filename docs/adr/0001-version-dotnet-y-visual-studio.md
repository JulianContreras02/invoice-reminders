# ADR 0001: Versión de .NET y Visual Studio

**Estado:** Aceptada · **Fecha:** 2026-10-21

## Contexto

La prueba técnica exige desarrollo en Visual Studio con .NET y C#, y el equipo revisor evalúa, entre otros criterios, el stack: la solución debe compilar y depurar en Visual Studio (CA-14). Al mismo tiempo, el despliegue para el revisor es un único comando (`docker compose up --build`) que no depende del Visual Studio que tenga instalado.

Restricciones reales en la fecha de la decisión:

- .NET 8 y .NET 9 pierden soporte el 10 de noviembre de 2026, tres semanas después de la entrega. Entregar sobre un runtime a punto de vencer es una señal negativa ante un revisor.
- .NET 10 es LTS con soporte hasta el 14 de noviembre de 2028.
- Apuntar a `net10.0` solo está soportado oficialmente en Visual Studio 18.0 o superior. Visual Studio 2022 17.14 con el SDK 10 abre y compila la solución con una advertencia, sin soporte.
- Varios paquetes del ecosistema (`xunit.v3`, Microsoft.Testing.Platform) cambian de comportamiento con el SDK 10; el proyecto se queda con el `dotnet test` clásico (VSTest), ver ADR 0008.

## Decisión

- Target framework **.NET 10 LTS** (`net10.0`) para todos los proyectos, fijado una sola vez en `Directory.Build.props` junto con `LangVersion latest`, `Nullable`, `ImplicitUsings`, `AnalysisLevel latest` y `EnforceCodeStyleInBuild`.
- `global.json` con `{"sdk": {"version": "10.0.100", "rollForward": "latestFeature"}}`; no configura runner de pruebas (ADR 0008).
- Solución en formato **`.slnx`** (`InvoiceReminders.slnx`, el formato XML de solución de Visual Studio 2026) con los proyectos Domain, Application, Infrastructure, Api y `tests/InvoiceReminders.UnitTests`, más carpetas de solución con los archivos de configuración y los documentos.
- IDE de desarrollo: **Visual Studio 2026 (18.x) Community**, carga de trabajo "ASP.NET y desarrollo web". El candidato ya lo tiene instalado.
- Imágenes base del Dockerfile: `mcr.microsoft.com/dotnet/sdk:10.0` y `mcr.microsoft.com/dotnet/aspnet:10.0` (Ubuntu 24.04, no Alpine), con tag de versión menor para recibir parches. Verificado con el SDK 10.0.402 (runtime 10.0.13).
- La primera línea del README indica la versión de Visual Studio requerida y aclara que el despliegue por compose no depende de ella.
- Versiones de paquetes centralizadas en `Directory.Packages.props` (Central Package Management, versiones exactas): `MongoDB.Driver 3.12.0`, `MailKit 4.18.1`, `Scalar.AspNetCore 2.18.2`, `Microsoft.AspNetCore.OpenApi 10.0.13`, los `Microsoft.Extensions.*` en `10.0.13`, `xunit.v3.mtp-off 4.0.2` (variante oficial de xunit.v3 sin Microsoft Testing Platform), `xunit.runner.visualstudio 4.0.1` y `Microsoft.NET.Test.Sdk 18.11.0`.

## Alternativas consideradas

- **.NET 8 LTS + Visual Studio 2022.** Es la combinación más extendida y la que con mayor probabilidad tiene el revisor. Se descarta porque su soporte termina el 10 de noviembre de 2026, justo después de la entrega; el riesgo de que el revisor solo tenga VS 2022 se mitiga con la pregunta al equipo revisor, la nota del README y el hecho de que compose no necesita Visual Studio.
- **.NET 9 STS.** Mismo fin de soporte que .NET 8 y sin la etiqueta LTS; no aporta nada frente a .NET 10.
- **Imágenes Alpine de .NET.** Más pequeñas, pero activan `InvariantGlobalization` y rompen el formato `es-CO` de los montos en los correos.

## Consecuencias

Positivas:

- Runtime con soporte durante toda la vida útil razonable de la entrega y de la entrevista.
- Las plantillas y herramientas actuales de Visual Studio 2026 (Minimal API, xunit.v3, contenedores) se usan sin adaptaciones.
- Una sola fuente de verdad para el target framework y las versiones de paquetes.

Negativas y lo que se acepta a cambio:

- Un revisor con Visual Studio 2022 verá una advertencia de framework no soportado al abrir la solución, y necesita una versión que soporte el formato `.slnx`. Se acepta porque la evaluación funcional se hace con `docker compose up --build`, y se declara en el README.
- El modo Microsoft.Testing.Platform de `dotnet test` en el SDK 10 cambia la sintaxis del comando; se evita usando el `dotnet test` clásico (VSTest), de modo que `dotnet test` funciona sin argumentos especiales (ADR 0008).
- Riesgo de que un cambio de feature band de `sdk:10.0` o un advisory NuGet rompa el build del revisor: se mitiga con `TreatWarningsAsErrors` solo cuando se compila con `ContinuousIntegrationBuild=true` y `WarningsNotAsErrors NU1901;NU1902;NU1903;NU1904` (ADR 0008 y 0009).

**Plan B documentado:** volver a `net8.0` es una línea en `Directory.Build.props` más el cambio de los tags de imagen (`sdk:8.0`, `aspnet:8.0`). Se activaría solo si el equipo revisor indicara expresamente que revisa con Visual Studio 2022 y prefiere .NET 8.

## Referencias

- `docs/ARQUITECTURA.md`: capas y tabla de paquetes por proyecto.
- `README.md`: primera línea con la versión de Visual Studio y requisito único de Docker Desktop.
- ADR 0008 (pruebas y runner) y ADR 0009 (despliegue).
