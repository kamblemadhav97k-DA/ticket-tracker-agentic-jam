# syntax=docker/dockerfile:1
# Build context is the repository root (see docker-compose.yml) so that
# project references across the solution resolve during restore/build.

# ---- Build stage ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (layer-cached on csproj/sln changes only).
COPY TicketTracker.sln ./
COPY src/TicketTracker.API/TicketTracker.API.csproj                   src/TicketTracker.API/
COPY src/TicketTracker.Application/TicketTracker.Application.csproj     src/TicketTracker.Application/
COPY src/TicketTracker.Domain/TicketTracker.Domain.csproj             src/TicketTracker.Domain/
COPY src/TicketTracker.Infrastructure/TicketTracker.Infrastructure.csproj src/TicketTracker.Infrastructure/
RUN dotnet restore src/TicketTracker.API/TicketTracker.API.csproj

# Publish.
COPY . .
RUN dotnet publish src/TicketTracker.API/TicketTracker.API.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "TicketTracker.API.dll"]
