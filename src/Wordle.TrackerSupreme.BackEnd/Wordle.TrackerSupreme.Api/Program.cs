using Wordle.TrackerSupreme.Infrastructure;
using Wordle.TrackerSupreme.Migrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddWordleTrackerSupremeInfrastructure(builder.Configuration, configureNpgsql: npgsql => npgsql.AllowMigrationManagement());
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
