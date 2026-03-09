FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app
ENV DOTNET_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/FinDistill.Worker/FinDistill.Worker.csproj",           "src/FinDistill.Worker/"]
COPY ["src/FinDistill.Application/FinDistill.Application.csproj", "src/FinDistill.Application/"]
COPY ["src/FinDistill.Infrastructure/FinDistill.Infrastructure.csproj", "src/FinDistill.Infrastructure/"]
COPY ["src/FinDistill.Domain/FinDistill.Domain.csproj",           "src/FinDistill.Domain/"]

RUN dotnet restore "src/FinDistill.Worker/FinDistill.Worker.csproj"

COPY . .

RUN dotnet publish "src/FinDistill.Worker/FinDistill.Worker.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FinDistill.Worker.dll"]
