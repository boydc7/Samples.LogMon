using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DdLogMon.Helpers;
using DdLogMon.Interfaces;
using DdLogMon.Models;
using DdLogMon.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;

namespace DdLogMon
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var defaultFileToTail = args.FirstOrDefault();

            // Configure app, logging, options...
            var hostBuilder = new HostBuilder().UseContentRoot(Directory.GetCurrentDirectory())
                                               .ConfigureHostConfiguration(b => BuildConfiguration(b))
                                               .ConfigureAppConfiguration((wc, conf) => BuildConfiguration(conf, wc.HostingEnvironment.EnvironmentName))
                                               .ConfigureServices((hostContext, services) =>
                                                                  {   // Setup IoC container (just use default container for this)
                                                                      services.AddLogging();
                                                                      services.Configure<ConfigSettings>(hostContext.Configuration.GetSection("ConfigSettings"));

                                                                      services.AddTransient<Func<TailFileType, IFileLineProcessor>>(s => t => FileLineProcessorFactory.Instance.ResolveProcessor(t, s));
                                                                      services.AddTransient<Func<TailFileInfo, IFileSystemTailDelegate>>(s => t => new FileSystemTailFileDelegate(t,
                                                                                                                                                                                  s.GetRequiredService<ITailFileFailureDelegate>(),
                                                                                                                                                                                  s.GetRequiredService<IOptions<ConfigSettings>>(),
                                                                                                                                                                                  s.GetRequiredService<ILogger<FileSystemTailFileDelegate>>()));

                                                                      services.AddSingleton<HttpAccessLogFileLineProcessor>();

                                                                      services.AddSingleton<IHttpAccessLogLineParser, NaiveHttpAccessLogLineParser>();

                                                                      services.AddSingleton<ITailFileFailureDelegate, RetryBackoffTailFileFailureDelegate>();
                                                                      services.AddSingleton<ITailFileService, FileSystemTailFileService>();

                                                                      // Host a file tail service for the default/specified file by default
                                                                      services.AddTransient<IHostedService, DefaultHttpAccessLogHostedTail>(s => new DefaultHttpAccessLogHostedTail(s.GetRequiredService<ITailFileService>(),
                                                                                                                                                                                    s.GetRequiredService<IOptions<ConfigSettings>>(),
                                                                                                                                                                                    defaultFileToTail));

                                                                      // Stat summary processing service
                                                                      services.AddHostedService<StatSummaryService>();

                                                                      // Storage service (simple in-memory for now)
                                                                      services.AddSingleton<IHttpAccessLogStorageService>(s =>
                                                                                                                          {
                                                                                                                              var settings = s.GetRequiredService<IOptions<ConfigSettings>>();
                                                                                                                              return new InMemoryHttpAccessLogStorageService(settings.Value.TotalTrafficIntervalMinutes.Gz(2) + 1);
                                                                                                                          });
                                                                  })
                                               .ConfigureLogging((h, b) =>
                                                                 {
                                                                     b.ClearProviders();
                                                                     b.SetMinimumLevel(LogLevel.Debug);
                                                                     b.AddNLog();
                                                                 })
                                               .UseConsoleLifetime() // For purposes of this app, app lifetime is tied to the console
                                               .Build();

            await hostBuilder.RunAsync();
        }

        // The configuration produced by this method is used for both the host and app configurations.
        private static void BuildConfiguration(IConfigurationBuilder conf, string envName = null)
        {
            var defaultEnv = "Development";

#if RELEASE
    defaultEnv = "Production";
#endif

            conf.AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{envName ?? defaultEnv}.json", true,
                             true);
        }
    }
}
