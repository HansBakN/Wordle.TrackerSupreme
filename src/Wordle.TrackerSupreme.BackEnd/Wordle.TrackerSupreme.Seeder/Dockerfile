FROM mcr.microsoft.com/dotnet/sdk:10.0.101 AS seeder
WORKDIR /workspace

COPY Wordle.TrackerSupreme.sln ./
COPY Wordle.TrackerSupreme.Application/Wordle.TrackerSupreme.Application.csproj Wordle.TrackerSupreme.Application/
COPY Wordle.TrackerSupreme.Domain/Wordle.TrackerSupreme.Domain.csproj Wordle.TrackerSupreme.Domain/
COPY Wordle.TrackerSupreme.Infrastructure/Wordle.TrackerSupreme.Infrastructure.csproj Wordle.TrackerSupreme.Infrastructure/
COPY Wordle.TrackerSupreme.Seeder/Wordle.TrackerSupreme.Seeder.csproj Wordle.TrackerSupreme.Seeder/

RUN dotnet restore Wordle.TrackerSupreme.Seeder/Wordle.TrackerSupreme.Seeder.csproj

COPY . .

ENTRYPOINT ["dotnet","run","--project","Wordle.TrackerSupreme.Seeder/Wordle.TrackerSupreme.Seeder.csproj"]
