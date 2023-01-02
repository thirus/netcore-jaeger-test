using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;

namespace demo
{

    public class Program
    {
            static String serviceName = "demo";
            static String serviceVersion = "1.0.0";
        public static async Task Main(String[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var builder = CreateHostBuilder(args);
            builder.Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
                  .ConfigureLogging(logging =>
      {
          // logging.ClearProviders();
          logging.AddOpenTelemetry(options =>
          {
              options.IncludeFormattedMessage = true;
              options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
              options.AddOtlpExporter();
              // options.AddConsoleExporter();
          });
      })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
    }
}
