using System;
using System.IO;
using System.Reflection;
using Finaps.EventSourcing.Example;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace EventSourcing.Example.Tests.Postgres;

public static class PostgresTestServer
{
    public static TestServer GetServer()
    {
        Environment.SetEnvironmentVariable("UseCosmos", "false");
        var path = Assembly.GetAssembly(typeof(PostgresTestServer))?.Location;
        var hostBuilder = new WebHostBuilder()
            .UseContentRoot(Path.GetDirectoryName(path)!)
            .ConfigureAppConfiguration(builder => builder
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("appsettings.local.json", true)
                .AddEnvironmentVariables())
            .UseStartup<Startup>();
        return new TestServer(hostBuilder);
    }
}