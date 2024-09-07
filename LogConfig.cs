using Microsoft.Extensions.Configuration;
using Serilog;

namespace ZipFileProcessor;

public class LogConfig
{
    public static void SerilogConfig(IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Console()
            .WriteTo.File(configuration["LogFileLocation"] + "log_.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}