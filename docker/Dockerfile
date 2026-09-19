# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ATMCashForecasting.sln .
COPY src/ATMCashForecasting.Domain/ATMCashForecasting.Domain.csproj src/ATMCashForecasting.Domain/
COPY src/ATMCashForecasting.Application/ATMCashForecasting.Application.csproj src/ATMCashForecasting.Application/
COPY src/ATMCashForecasting.Infrastructure/ATMCashForecasting.Infrastructure.csproj src/ATMCashForecasting.Infrastructure/
COPY src/ATMCashForecasting.Web/ATMCashForecasting.Web.csproj src/ATMCashForecasting.Web/
COPY tests/ATMCashForecasting.Tests/ATMCashForecasting.Tests.csproj tests/ATMCashForecasting.Tests/

RUN dotnet restore src/ATMCashForecasting.Web/ATMCashForecasting.Web.csproj

COPY src/ src/
COPY tests/ tests/

RUN dotnet build tests/ATMCashForecasting.Tests/ATMCashForecasting.Tests.csproj -c Release --no-restore
RUN dotnet test tests/ATMCashForecasting.Tests/ATMCashForecasting.Tests.csproj -c Release --no-build --logger "console;verbosity=normal"

RUN dotnet publish src/ATMCashForecasting.Web/ATMCashForecasting.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN addgroup --system --gid 1000 atmcash && adduser --system --uid 1000 --ingroup atmcash atmcash
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
USER atmcash

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ATMCashForecasting.Web.dll"]
