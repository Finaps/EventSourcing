using System;
using System.IO;
using System.Reflection;
using Finaps.EventSourcing.Example;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace EventSourcing.Example.Tests.Cosmos;

public static class CosmosTestServer
{
    public static TestServer GetServer()
    {
      Environment.SetEnvironmentVariable("UseCosmos", "true");
      var path = Assembly.GetAssembly(typeof(CosmosTestServer))?.Location;
      var hostBuilder = new WebHostBuilder()
        .UseContentRoot(Path.GetDirectoryName(path)!)
        .ConfigureAppConfiguration(builder => builder
          .AddJsonFile("appsettings.local.json", true)
          .AddEnvironmentVariables())
        .UseStartup<Startup>();
      return new TestServer(hostBuilder);
    }
}