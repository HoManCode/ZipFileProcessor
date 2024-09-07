
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using ZipFileProcessor.Services.Notification;
using ZipFileProcessor.Services.Validator;


namespace ZipFileProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            
            var validator = host.Services.GetRequiredService<IValidator>();

            var emailSender = host.Services.GetRequiredService<INotification>();

            //emailSender.SendNotification("hello", "world");

            //bool valid = validator.Validate("./", "{}");

            //logger.LogInformation($"Processing ZIP file: ");

            //logger.LogError("file did not find");

        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .UseSerilog((context, loggerConfig) =>
                {
                    loggerConfig
                        .ReadFrom.Configuration(context.Configuration)
                        .WriteTo.Console()
                        .WriteTo.File(
                            path: context.Configuration["LogFileLocation"] + "log_.txt",
                            rollingInterval: RollingInterval.Day
                        );
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IValidator, XmlValidator>();
                    services.AddTransient<INotification, EmailNotification>();
                });
        }
            
    }
    
}

