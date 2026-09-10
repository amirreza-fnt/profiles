##
# Multi-stage build (optional). Offline AlmaLinux deploy uses publish/ + systemd.
##

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/ProfileService.Domain/ProfileService.Domain.csproj src/ProfileService.Domain/
COPY src/ProfileService.Application/ProfileService.Application.csproj src/ProfileService.Application/
COPY src/ProfileService.Infrastructure/ProfileService.Infrastructure.csproj src/ProfileService.Infrastructure/
COPY src/ProfileService.Api/ProfileService.Api.csproj src/ProfileService.Api/
RUN dotnet restore src/ProfileService.Api/ProfileService.Api.csproj

COPY src/ src/
RUN dotnet publish src/ProfileService.Api/ProfileService.Api.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5027
EXPOSE 5027

ENTRYPOINT ["dotnet", "ProfileService.Api.dll"]
