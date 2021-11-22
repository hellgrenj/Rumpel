using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
public static class Server
{
    public static async Task Start(Func<HttpContext, Task> requestHandler)
    {
        var port = Environment.GetEnvironmentVariable("RUMPEL_PORT") != null ? Convert.ToInt32(Environment.GetEnvironmentVariable("RUMPEL_PORT")) : 8181;
        Printer.PrintInfo($"rumpel up and listening on port {port}");
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddCors(o => o.AddPolicy("corsPolicy", builder =>
             {
                 builder.AllowAnyOrigin()
                         .AllowAnyMethod()
                         .AllowAnyHeader();
             }));
        builder.Services.AddLogging(config => config.ClearProviders());
        builder.WebHost.ConfigureKestrel(ko =>
        {
            ko.Limits.MinRequestBodyDataRate = null;
            ko.ListenAnyIP(port);
        });
        var app = builder.Build();
        app.UseCors("corsPolicy");
        app.Use((context, next) =>
        {
            context.Request.EnableBuffering();
            return next();
        });
        app.Map("{**path}", async context => await requestHandler(context));
        await app.RunAsync();
    }
}