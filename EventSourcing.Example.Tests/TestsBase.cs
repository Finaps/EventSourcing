using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EventSourcing.Example.Tests;

public class TestsBase : IAsyncLifetime
{
  // Control the number of concurrent Tests
  private static readonly SemaphoreSlim Semaphore = new(8);
  public async Task InitializeAsync() => await Semaphore.WaitAsync();
  public Task DisposeAsync() => Task.FromResult(Semaphore.Release());
  public static readonly TestServer Server = GetServer();
  protected static TService? GetService<TService>() => Server.Services.GetService<TService>();
  private static TestServer GetServer()
  {
    var path = Assembly.GetAssembly(typeof(Startup))?.Location;
    var hostBuilder = new WebHostBuilder()
      .UseContentRoot(Path.GetDirectoryName(path)!)
      .ConfigureAppConfiguration(builder => builder.AddJsonFile("appsettings.local.json", true))
      .UseStartup<Startup>();
    return new TestServer(hostBuilder);
  }
}