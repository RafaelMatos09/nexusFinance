FROM node:22-alpine AS frontend
WORKDIR /src
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/src/NexusFinance.Domain/ backend/src/NexusFinance.Domain/
COPY backend/src/NexusFinance.Application/ backend/src/NexusFinance.Application/
COPY backend/src/NexusFinance.Infrastructure/ backend/src/NexusFinance.Infrastructure/
COPY backend/src/NexusFinance.Api/ backend/src/NexusFinance.Api/
COPY --from=frontend /src/dist backend/src/NexusFinance.Api/wwwroot
RUN dotnet publish backend/src/NexusFinance.Api/NexusFinance.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "NexusFinance.Api.dll"]
