# API de Dentissa: imagen de Render (Production; escucha en $PORT) y de la demo local (docker-compose.demo.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilar
WORKDIR /src
COPY global.json ./
COPY backend/backend.csproj backend/
RUN dotnet restore backend/backend.csproj
COPY backend/ backend/
RUN dotnet publish backend/backend.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=compilar /app ./
# Sin privilegios de administrador dentro del contenedor
USER $APP_UID
# Production por defecto (Render). El compose de la demo local lo cambia a Development.
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "backend.dll"]
