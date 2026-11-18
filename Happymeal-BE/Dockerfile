FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first, so the restore layer stays cached while the code changes
COPY global.json ./
COPY src/Happymeal.API/Happymeal.API.csproj src/Happymeal.API/
COPY src/Happymeal.Application/Happymeal.Application.csproj src/Happymeal.Application/
COPY src/Happymeal.Common/Happymeal.Common.csproj src/Happymeal.Common/
COPY src/Happymeal.Domain/Happymeal.Domain.csproj src/Happymeal.Domain/
COPY src/Happymeal.Infrastructure/Happymeal.Infrastructure.csproj src/Happymeal.Infrastructure/
RUN dotnet restore src/Happymeal.API/Happymeal.API.csproj

COPY .editorconfig ./
COPY src/ src/
RUN dotnet publish src/Happymeal.API/Happymeal.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Happymeal.API.dll"]
