FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY LogService.API/*.csproj ./LogService.API/
RUN dotnet restore ./LogService.API/LogService.API.csproj

COPY . .
WORKDIR /src/LogService.API
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "LogService.API.dll"]
