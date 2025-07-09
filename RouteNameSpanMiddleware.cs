using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LMS.Web.Core.Infrastructure.OpenTelemetry
{
    public class RouteNameSpanMiddleware
    {
        private readonly RequestDelegate _next;

        public RouteNameSpanMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                // Wait until routing happens
                await _next(context);

                var endpoint = context.GetEndpoint();
                var routePattern = (endpoint as RouteEndpoint)?.RoutePattern?.RawText;

                if (!string.IsNullOrEmpty(routePattern))
                {
                    activity.DisplayName = routePattern;
                    activity.SetTag("http.route", routePattern); // Also tags it for filtering
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}
