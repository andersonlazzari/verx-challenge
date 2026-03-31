FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . . 
RUN dotnet restore "src/services/Consolidado.Worker/Consolidado.Worker.csproj"
RUN dotnet publish "src/services/Consolidado.Worker/Consolidado.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Consolidado.Worker.dll"]