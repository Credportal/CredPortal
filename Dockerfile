FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Cred.csproj", "./"]
RUN dotnet restore "Cred.csproj"
COPY . .
RUN dotnet publish "Cred.csproj" -c Release -o /app/publish

# Fase de Execução
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Cred.dll"]