
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ZipFileProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = Services();
            
            var logger = services.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation($"Processing ZIP file: ");
            
            logger.LogError("file did not find");
            
        }

        private static ServiceProvider Services()
        {
            var serviceCollection = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            
            LogConfig.SerilogConfig(configuration);
            
            serviceCollection
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(configure => configure.AddSerilog())
                .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Information);
            
            return serviceCollection.BuildServiceProvider();
        }
    }
}

