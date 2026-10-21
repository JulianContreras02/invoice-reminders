# Imagen única de la aplicación: API .NET 10 + frontend React compilado en wwwroot.
# Contexto de construcción: la raíz del repositorio (ver docker-compose.yml).

# --- Etapa base: runtime de ASP.NET. Va primero por la convención de las herramientas de contenedores de Visual Studio.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
USER $APP_UID

# --- Frontend: build de React con Vite. El revisor no necesita Node instalado.
FROM node:22-alpine AS web
WORKDIR /web
COPY web/package*.json ./
RUN if [ -f package-lock.json ]; then npm ci; else npm install; fi
COPY web/ ./
RUN npm run build

# --- Backend: restaurar por proyecto (mejor caché) y publicar.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/InvoiceReminders.Domain/InvoiceReminders.Domain.csproj src/InvoiceReminders.Domain/
COPY src/InvoiceReminders.Application/InvoiceReminders.Application.csproj src/InvoiceReminders.Application/
COPY src/InvoiceReminders.Infrastructure/InvoiceReminders.Infrastructure.csproj src/InvoiceReminders.Infrastructure/
COPY src/InvoiceReminders.Api/InvoiceReminders.Api.csproj src/InvoiceReminders.Api/
RUN dotnet restore src/InvoiceReminders.Api/InvoiceReminders.Api.csproj
COPY src/ src/
RUN dotnet publish src/InvoiceReminders.Api/InvoiceReminders.Api.csproj -c Release --no-restore -o /out -p:UseAppHost=false

# --- Imagen final: runtime + binarios + frontend.
FROM base AS final
COPY --from=build /out .
COPY --from=web /web/dist ./wwwroot
ENTRYPOINT ["dotnet", "InvoiceReminders.Api.dll"]
