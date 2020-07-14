using Microsoft.AspNetCore.Http;
using Prometheus;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Web
{
    public class MetricsMiddleware : IMiddleware
    {
        private Histogram _responseTimeHistogram;

        public MetricsMiddleware()
        {
            _responseTimeHistogram = Metrics.CreateHistogram(
                "http_request_duration_seconds",
                "how long a request took",
                new HistogramConfiguration
                {
                    LabelNames = new string[] { "code", "method" }
                });
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var sw = Stopwatch.StartNew();
            await next(context);
            sw.Stop();

            var code = context.Response.StatusCode;
            var method = context.Request.Method;

            _responseTimeHistogram.WithLabels(code.ToString(CultureInfo.InvariantCulture), method).Observe(sw.Elapsed.TotalSeconds);
        }
    }
}
