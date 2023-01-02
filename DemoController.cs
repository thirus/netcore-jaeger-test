using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace demo
{

    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private static readonly ActivitySource MyActivitySource = new("OpenTelemetry.Demo.Jaeger");
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpPost(Name = "TestPost")]
        public async Task<string> Post() {
            _logger.LogInformation("hello log");
            using var client = new HttpClient();
            await client.GetStringAsync("https://httpstat.us/200");
            return  await Task.FromResult("Okay");
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            using var parent = MyActivitySource.StartActivity("Main");

            using (var client = new HttpClient())
            {
                using (var slow = MyActivitySource.StartActivity("slow"))
                {
                    await client.GetStringAsync("https://httpstat.us/200?sleep=1000");
                    await client.GetStringAsync("https://httpstat.us/200?sleep=1000");
                }

                using (var fast = MyActivitySource.StartActivity("fast"))
                {
                    await client.GetStringAsync("https://httpstat.us/301");
                    await client.GetStringAsync("https://httpstat.us/200");
                }
            }


            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = 10,
                Summary = Summaries[1]
            })
            .ToArray();
        }
    }

}
