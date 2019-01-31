﻿using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace Prometheus
{
    /// <summary>
    /// Prometheus metrics export middleware for ASP.NET Core.
    /// 
    /// You should use IApplicationBuilder.UseMetricServer extension method instead of using this class directly.
    /// </summary>
    public sealed class MetricServerMiddleware
    {
        public MetricServerMiddleware(RequestDelegate next, Settings settings)
        {
            _next = next;

            _registry = settings.Registry ?? Metrics.DefaultRegistry;
        }

        public sealed class Settings
        {
            public CollectorRegistry Registry { get; set; }
        }

        private readonly RequestDelegate _next;

        private readonly CollectorRegistry _registry;

        public async Task Invoke(HttpContext context)
        {
            // We just handle the root URL (/metrics or whatnot).
            if (!string.IsNullOrWhiteSpace(context.Request.Path.Value.Trim('/')))
            {
                await _next(context);
                return;
            }

            var request = context.Request;
            var response = context.Response;

            response.ContentType = PrometheusConstants.ExporterContentType;

            MetricsSnapshot snapshot;

            try
            {
                snapshot = _registry.Collect();
            }
            catch (ScrapeFailedException ex)
            {
                response.StatusCode = StatusCodes.Status503ServiceUnavailable;

                if (!string.IsNullOrWhiteSpace(ex.Message))
                {
                    using (var writer = new StreamWriter(response.Body))
                        await writer.WriteAsync(ex.Message);
                }

                return;
            }

            response.StatusCode = StatusCodes.Status200OK;

            using (var outputStream = response.Body)
                snapshot.Serialize(outputStream);
        }
    }
}
