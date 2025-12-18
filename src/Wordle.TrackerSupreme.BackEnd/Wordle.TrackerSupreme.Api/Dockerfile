FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /workspace

COPY Wordle.TrackerSupreme.sln ./
COPY Wordle.TrackerSupreme.Api/Wordle.TrackerSupreme.Api.csproj Wordle.TrackerSupreme.Api/
COPY Wordle.TrackerSupreme.Domain/Wordle.TrackerSupreme.Domain.csproj Wordle.TrackerSupreme.Domain/
COPY Wordle.TrackerSupreme.Infrastructure/Wordle.TrackerSupreme.Infrastructure.csproj Wordle.TrackerSupreme.Infrastructure/
COPY Wordle.TrackerSupreme.Migrations/Wordle.TrackerSupreme.Migrations.csproj Wordle.TrackerSupreme.Migrations/

RUN dotnet restore Wordle.TrackerSupreme.Api/Wordle.TrackerSupreme.Api.csproj

COPY . .

RUN dotnet publish Wordle.TrackerSupreme.Api/Wordle.TrackerSupreme.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Wordle.TrackerSupreme.Api.dll"]
