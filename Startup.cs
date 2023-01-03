using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;

namespace demo
{
    public class Startup
    {
        static readonly ActivitySource a = new ActivitySource("demo");
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationDbContext>(options =>
                                                        {
                                                            // options.UseNpgsql(connectionString);
                                                        }
                );
            services.AddControllers();
            services.AddAuthorization();
            services.AddHealthChecks();
            // services.AddEndpointsApiExplorer();
            var serviceName = "demo";
            var serviceVersion = "1.0.0";
            services.AddOpenTelemetry()
                .WithTracing(builder => builder
                    .AddSource(serviceName)
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
                    // .AddConsoleExporter()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation(o =>
                    {
                        o.EnrichWithHttpRequest = async (activity, httpRequest) =>
                        {
                            var r = a.StartActivity("request/response", ActivityKind.Consumer);
                            httpRequest.HttpContext.Items["__r"] = r;
                            if (httpRequest.Method == "POST")
                            {
                                var body = await new StreamReader(httpRequest.Body)
                                                                    .ReadToEndAsync();
                                // activity.SetTag("requestBody", body);
                                var tags = new ActivityTagsCollection {
                                  {"body", body},
                                };
                                // activity.AddEvent(new ActivityEvent("request", DateTime.UtcNow, tags));
                                //("request.body", body));
                                r?.AddEvent(new ActivityEvent("request", DateTime.UtcNow, tags));
                            }
                        };
                        o.EnrichWithHttpResponse = async (activity, httpResponse) =>
                        {
                            // activity.SetTag("responseLength", httpResponse.ContentLength);
                            string body = "NA";
                            // httpResponse.HttpContext.Request.EnableRewind()
                            if (httpResponse.Body.CanRead)
                            {
                                body = await new StreamReader(httpResponse.Body).ReadToEndAsync();
                            }
                            var tags = new ActivityTagsCollection {
                                  {"body", body},
                                  {"length", httpResponse.ContentLength?.ToString()},
                                };
                            // activity.AddEvent(new ActivityEvent("response", DateTime.UtcNow, tags));
                            // using var r = a.StartActivity("request/response", ActivityKind.Consumer);
                            var r = httpResponse.HttpContext.Items["__r"] as Activity;
                            r?.AddEvent(new ActivityEvent("response", DateTime.UtcNow, tags));
                            r?.Dispose();
                        };
                        o.EnrichWithException = (activity, exception) =>
                        {
                            activity.SetTag("http.error", true);
                            activity.SetTag("exceptionType", exception.GetType().ToString());
                        };
                    })
                    .AddSqlClientInstrumentation(o =>
                    {
                        o.SetDbStatementForText = true;
                        o.EnableConnectionLevelAttributes = true;
                        o.SetDbStatementForStoredProcedure = true;
                    })
                     // .AddJaegerExporter()
                     .AddConsoleExporter()
                     .AddEntityFrameworkCoreInstrumentation(o =>
                     {
                         o.SetDbStatementForText = true;
                         o.SetDbStatementForStoredProcedure = true;
                     })
                     .AddOtlpExporter(o =>
                     {
                         o.Protocol = OtlpExportProtocol.HttpProtobuf;
                     })
            )
        .StartWithHost();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "aspnetcoreapp", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "aspnetcoreapp v1"));
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // app.UseAuthorization();

            app.UseMiddleware<Logger>();
            app.UseHealthChecks("/healthz");
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

        }
    }
}
