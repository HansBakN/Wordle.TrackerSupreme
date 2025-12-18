FROM mcr.microsoft.com/dotnet/sdk:10.0 AS migrator
WORKDIR /workspace

COPY src/Wordle.TrackerSupreme.sln ./src/
COPY src/Wordle.TrackerSupreme.Api/Wordle.TrackerSupreme.Api.csproj ./src/Wordle.TrackerSupreme.Api/
COPY src/Wordle.TrackerSupreme.Domain/Wordle.TrackerSupreme.Domain.csproj ./src/Wordle.TrackerSupreme.Domain/
COPY src/Wordle.TrackerSupreme.Infrastructure/Wordle.TrackerSupreme.Infrastructure.csproj ./src/Wordle.TrackerSupreme.Infrastructure/
COPY src/Wordle.TrackerSupreme.Migrations/Wordle.TrackerSupreme.Migrations.csproj ./src/Wordle.TrackerSupreme.Migrations/

RUN dotnet restore ./src/Wordle.TrackerSupreme.Migrations/Wordle.TrackerSupreme.Migrations.csproj

COPY src ./src

RUN dotnet tool install --global dotnet-ef --version 10.0.0
ENV PATH="${PATH}:/root/.dotnet/tools"

ENTRYPOINT ["dotnet","ef","database","update","--project","./src/Wordle.TrackerSupreme.Migrations/Wordle.TrackerSupreme.Migrations.csproj","--startup-project","./src/Wordle.TrackerSupreme.Api/Wordle.TrackerSupreme.Api.csproj","--context","WordleTrackerSupremeDbContext"]
