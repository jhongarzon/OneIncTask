FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY nuget.config ./
COPY src/OneIncTask.Domain/OneIncTask.Domain.csproj src/OneIncTask.Domain/
COPY src/OneIncTask.Application/OneIncTask.Application.csproj src/OneIncTask.Application/
COPY src/OneIncTask.Infrastructure/OneIncTask.Infrastructure.csproj src/OneIncTask.Infrastructure/
COPY src/OneIncTask.Api/OneIncTask.Api.csproj src/OneIncTask.Api/
RUN dotnet restore src/OneIncTask.Api/OneIncTask.Api.csproj --configfile nuget.config

COPY src/ src/
RUN dotnet publish src/OneIncTask.Api/OneIncTask.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

RUN groupadd -r appuser && useradd -r -g appuser appuser
USER appuser

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OneIncTask.Api.dll"]
