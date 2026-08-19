FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/GeminiWeatherApi/GeminiWeatherApi.csproj src/GeminiWeatherApi/
RUN dotnet restore src/GeminiWeatherApi/GeminiWeatherApi.csproj

COPY . .
RUN dotnet publish src/GeminiWeatherApi/GeminiWeatherApi.csproj     --configuration Release     --output /app/publish     --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GeminiWeatherApi.dll"]
