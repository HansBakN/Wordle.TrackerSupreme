FROM mcr.microsoft.com/dotnet/sdk:10.0 AS migrator
WORKDIR /workspace

COPY Wordle.TrackerSupreme.sln ./
COPY Wordle.TrackerSupreme.Api/Wordle.TrackerSupreme.Api.csproj Wordle.TrackerSupreme.Api/
COPY Wordle.TrackerSupreme.Domain/Wordle.TrackerSupreme.Domain.csproj Wordle.TrackerSupreme.Domain/
COPY Wordle.TrackerSupreme.Infrastructure/Wordle.TrackerSupreme.Infrastructure.csproj Wordle.TrackerSupreme.Infrastructure/
COPY Wordle.TrackerSupreme.Migrations/Wordle.TrackerSupreme.Migrations.csproj Wordle.TrackerSupreme.Migrations/

RUN dotnet restore Wordle.TrackerSupreme.Migrations/Wordle.TrackerSupreme.Migrations.csproj

COPY . .

RUN dotnet tool install --global dotnet-ef --version 10.0.0
ENV PATH="${PATH}:/root/.dotnet/tools"

ENTRYPOINT ["dotnet","ef","database","update","--project","./Wordle.TrackerSupreme.Migrations/Wordle.TrackerSupreme.Migrations.csproj","--startup-project","./Wordle.TrackerSupreme.Api/Wordle.TrackerSupreme.Api.csproj","--context","WordleTrackerSupremeDbContext"]
