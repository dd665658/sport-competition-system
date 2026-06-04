FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# копируем csproj из подпапки
COPY WebApplication1/WebApplication1.csproj WebApplication1/

WORKDIR /app/WebApplication1
RUN dotnet restore WebApplication1.csproj

# копируем весь код
COPY WebApplication1/ ./

RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /out ./

ENTRYPOINT ["dotnet", "WebApplication1.dll"]