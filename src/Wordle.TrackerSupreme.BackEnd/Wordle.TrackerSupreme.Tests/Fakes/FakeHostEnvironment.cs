using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Wordle.TrackerSupreme.Tests.Fakes;

public class FakeHostEnvironment : IHostEnvironment
{
    public string ApplicationName { get; set; } = "Wordle.TrackerSupreme.Tests";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

    public string EnvironmentName { get; set; } = Environments.Development;
}
