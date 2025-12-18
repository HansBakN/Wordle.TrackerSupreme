FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /workspace

COPY src/Wordle.TrackerSupreme.sln ./src/
COPY src/Wordle.TrackerSupreme.Api/Wordle.TrackerSupreme.Api.csproj ./src/Wordle.TrackerSupreme.Api/
COPY src/Wordle.TrackerSupreme.Domain/Wordle.TrackerSupreme.Domain.csproj ./src/Wordle.TrackerSupreme.Domain/
COPY src/Wordle.TrackerSupreme.Infrastructure/Wordle.TrackerSupreme.Infrastructure.csproj ./src/Wordle.TrackerSupreme.Infrastructure/
COPY src/Wordle.TrackerSupreme.Migrations/Wordle.TrackerSupreme.Migrations.csproj ./src/Wordle.TrackerSupreme.Migrations/

RUN dotnet restore ./src/Wordle.TrackerSupreme.Api/Wordle.TrackerSupreme.Api.csproj

COPY src ./src

RUN dotnet publish ./src/Wordle.TrackerSupreme.Api/Wordle.TrackerSupreme.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Wordle.TrackerSupreme.Api.dll"]
