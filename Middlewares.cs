using System.Net.Http;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.IO;
using System;

namespace demo
{
    public class Logger
    {
        RequestDelegate next;

        public Logger(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var log = new Log
            {
                Path = context.Request.Path,
                Method = context.Request.Method,
                QueryString = context.Request.QueryString.ToString()
            };

            // check if the Request is a POST call
            // since we need to read from the body
            if (context.Request.Method == "POST")
            {
                context.Request.EnableBuffering();
                var body = await new StreamReader(context.Request.Body)
                                                    .ReadToEndAsync();
                context.Request.Body.Position = 0;
                log.Payload = body;
            }

            log.RequestedOn = DateTime.Now;
            await next.Invoke(context);
        }
    }
}
