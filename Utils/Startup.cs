using Serilog;
using Serilog.Extensions.Hosting;
using System.Reflection;
using ILogger = Serilog.ILogger;

namespace Utils;

public class StartUp
{
    private readonly string[] args;
    private static readonly string appName = Assembly.GetEntryAssembly()?.GetName()?.Name ?? "The app with no name";

    public StartUp(string[] args)
    {
        this.args = args;

        Logger = CreateBootstrapLogger();
        Logger.Information(
            "Hello! It's a me, {AppName} : v{Version}",
            appName,
            Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "UnknownVersion");
    }

    public ILogger Logger { get; }

    public Action<WebApplicationBuilder> AddServices { get; set; } = _ => { };

    public Action<WebApplication> ConfigureRequestPipeline { get; set; } = _ => { };

    public int Run()
    {
        try
        {
            Logger.Information("Initialising");

            var app = Build();
            Configure(app);
            Run(app);

            Logger.Information("I love you buhbye!");

            return 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occured; Terminating");

            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private void Configure(WebApplication app)
    {
        Logger.Information("Configuring the request pipeline");
        ConfigureRequestPipeline(app);
    }

    private WebApplication Build()
    {
        var builder = WebApplication.CreateBuilder(args);

        Logger.Information("Building for {Env}", builder.Environment.EnvironmentName);

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .Enrich.WithProperty("AppName", appName)
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services);
        });

        Logger.Information("Adding services");
        AddServices(builder);

        return builder.Build();
    }

    private static ReloadableLogger CreateBootstrapLogger()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var bootstrapLoggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithProperty("AppName", appName);

        if (environment == "Development")
        {
            bootstrapLoggerConfig.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
            );
        }
        else
        {
            bootstrapLoggerConfig.WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter());
        }

        return bootstrapLoggerConfig.CreateBootstrapLogger();
    }

    private void Run(WebApplication app)
    {
        Logger.Information("Cocked, locked and ready to rock!");
        app.Run();
    }
}