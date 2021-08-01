using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public static class Server
{
    public static async Task Start(Func<HttpContext, Task> requestHandler)
    {
        var port = Environment.GetEnvironmentVariable("RUMPEL_PORT") != null ? Convert.ToInt32(Environment.GetEnvironmentVariable("RUMPEL_PORT")) : 8181;
    
        Printer.PrintInfo($"rumpel up and listening on port {port}");
        await Host.CreateDefaultBuilder()

                  .ConfigureServices((hostContext, services) =>
                  {
                      services.AddCors(o => o.AddPolicy("corsPolicy", builder =>
                      {
                          builder.AllowAnyOrigin()
                                  .AllowAnyMethod()
                                  .AllowAnyHeader();
                      }));
                      services.AddRouting();
                      services.AddLogging(config => config.ClearProviders());
                  })
                  .ConfigureWebHost(webBuilder =>
                   {
                       
                       webBuilder.UseKestrel(options =>
                       {
                           options.Limits.MinRequestBodyDataRate = null;
                       }).ConfigureKestrel(options => options.ListenAnyIP(port))
                       .Configure(app =>
                       {
                           app.UseCors("corsPolicy");
                           app.UseRouting();
                           app.Use((context, next) =>
                           {
                               context.Request.EnableBuffering();
                               return next();
                           });
                           app.UseEndpoints(e => e.Map("{**path}", async context => await requestHandler(context)));
                       });
                   })
                   .RunConsoleAsync();
    }
}