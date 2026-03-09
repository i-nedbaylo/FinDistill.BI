FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
# Support both Railway (fixed 8080) and Render (dynamic $PORT)
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/FinDistill.Web/FinDistill.Web.csproj",           "src/FinDistill.Web/"]
COPY ["src/FinDistill.Application/FinDistill.Application.csproj", "src/FinDistill.Application/"]
COPY ["src/FinDistill.Infrastructure/FinDistill.Infrastructure.csproj", "src/FinDistill.Infrastructure/"]
COPY ["src/FinDistill.Domain/FinDistill.Domain.csproj",     "src/FinDistill.Domain/"]

RUN dotnet restore "src/FinDistill.Web/FinDistill.Web.csproj"

COPY . .

RUN dotnet publish "src/FinDistill.Web/FinDistill.Web.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FinDistill.Web.dll"]
