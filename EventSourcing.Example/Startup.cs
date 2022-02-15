using EventSourcing.Core;
using EventSourcing.Cosmos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace EventSourcing.Example;

public class Startup
{
  private IConfigurationRoot Configuration { get; }

  public Startup(IHostEnvironment env)
  {
    Configuration = new ConfigurationBuilder()
      .SetBasePath(env.ContentRootPath)
      .AddJsonFile("appsettings.local.json", true)
      .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
      .AddEnvironmentVariables()
      .Build();
  }

  // This method gets called by the runtime. Use this method to add services to the container.
  public void ConfigureServices(IServiceCollection services)
  {
    services.AddControllers();
    services.AddSwaggerGen(c =>
    {
      c.SwaggerDoc("v1", new OpenApiInfo { Title = "EventSourcing.Example", Version = "v1" });
    });
      
    // Configure Cosmos connections
    services.Configure<CosmosEventStoreOptions>(Configuration.GetSection("Cosmos"));
    services.AddSingleton<IEventStore, CosmosEventStore>();
    // Configure AggregateService
    services.AddScoped<IAggregateService, AggregateService>();
  }

  // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
  public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
  {
    if (env.IsDevelopment())
    {
      app.UseDeveloperExceptionPage();
      app.UseSwagger();
      app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "EventSourcing.Example v1"));
    }

    app.UseHttpsRedirection();

    app.UseRouting();

    app.UseAuthorization();

    app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
  }
}