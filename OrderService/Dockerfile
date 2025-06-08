FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Копируем сразу всё
COPY . .

# Переходим в OrderService и делаем restore + publish
WORKDIR /src/OrderService
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "OrderService.dll"]
