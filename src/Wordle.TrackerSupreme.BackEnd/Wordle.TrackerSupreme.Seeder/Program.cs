using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Services.Game;
using Wordle.TrackerSupreme.Infrastructure;
using Wordle.TrackerSupreme.Seeder;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddWordleTrackerSupremeInfrastructure(context.Configuration);
        services.Configure<GameOptions>(context.Configuration.GetSection(GameOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<GameOptions>>().Value);
        services.Configure<SeederOptions>(context.Configuration.GetSection(SeederOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<SeederOptions>>().Value);
        services.AddSingleton<IWordSelector, WordSelector>();
        services.AddSingleton<WordListValidator>();
        services.AddSingleton<IWordValidator>(sp => sp.GetRequiredService<WordListValidator>());
        services.AddSingleton<IWordListProvider>(sp => sp.GetRequiredService<WordListValidator>());
        services.AddSingleton<IGuessEvaluationService, GuessEvaluationService>();
        services.AddScoped<PasswordHasher<Player>>();
        services.AddScoped<SeedDataGenerator>();
        services.AddScoped<SeederRunner>();
    });

using var host = builder.Build();

using var scope = host.Services.CreateScope();
await scope.ServiceProvider.GetRequiredService<SeederRunner>().SeedAsync();
