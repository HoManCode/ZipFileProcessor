
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ZipFileProcessor.Services.Notification;
using ZipFileProcessor.Services.Processor;
using ZipFileProcessor.Services.Validator;


namespace ZipFileProcessor
{
    public class Program
    {
        static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            var configuration = host.Services.GetService(typeof(IConfiguration)) as IConfiguration;
            
            var processor = host.Services.GetRequiredService<IProcessor>();

            processor.Process(configuration?["ZipFileLoc"]);
            
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
                            path: context.Configuration["LogFileLoc"] + "log_.txt",
                            rollingInterval: RollingInterval.Day
                        );
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddTransient<IValidator, XmlValidator>();
                    services.AddTransient<INotification, EmailNotification>();
                    services.AddTransient<IProcessor, ZipProcessor>();
                });
        }
            
    }
    
}

