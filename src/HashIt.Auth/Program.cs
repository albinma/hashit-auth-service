using Serilog;
using Serilog.Events;

// Setup logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Disable 'Server' in response header for security purposes
    builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

    // Configure logging
    builder.Host.UseSerilog();

    // Configure application
    var app = builder
        .ConfigureServices()
        .ConfigurePipeline();

    app.Run();

    return 0;
}
catch (Exception e) when (e is OperationCanceledException || e.GetType().Name == "StopTheHostException")
{
    Log.Information("Migration completed");
    return 0;
}
catch (Exception e)
{
    Log.Fatal(e, "Caught exception whilst building Host");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

