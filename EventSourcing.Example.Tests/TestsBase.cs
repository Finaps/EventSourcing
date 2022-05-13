using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Finaps.EventSourcing.Example.Tests;

public class TestsBase : IAsyncLifetime
{
  private readonly TestServer Server;

  protected readonly HttpClient Client;
  protected TService? GetService<TService>() => Server.Services.GetService<TService>();
  // Control the number of concurrent Tests
  private static readonly SemaphoreSlim Semaphore = new(8);

  protected TestsBase(TestServer server)
  {
    Server = server;
    Client = Server.CreateClient();
  }

  public async Task InitializeAsync() => await Semaphore.WaitAsync();
  public Task DisposeAsync() => Task.FromResult(Semaphore.Release());
}