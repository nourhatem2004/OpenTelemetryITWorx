public static class OpenTelemetryConfiguration
{
    public static void AddOpenTelemetryLMS(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration.GetValue<string>("OpenTelemetry:ServiceName") ?? "MyApp";
        var serviceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService(serviceName, serviceVersion)
                    )
                    .SetSampler(new TraceIdRatioBasedSampler(
                        configuration.GetValue("OpenTelemetry:Sampling:Ratio", 1.0)))
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext =>
                        {
                            var path = httpContext.Request.Path.Value?.ToLower();
                            return !path?.Contains("/health") == true &&
                                   !path?.Contains("/favicon") == true &&
                                   !path?.Contains("/assets") == true &&
                                   !path?.Contains("/css") == true &&
                                   !path?.Contains("/js") == true &&
                                   !path?.Contains("/img") == true;
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.SetDbStatementForStoredProcedure = true;
                    })
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.RecordException = true;
                    })
                    .AddSource("LMS.*") // This enables all custom ActivitySources starting with LMS.
                    .AddJaegerExporter(options =>
                    {
                        options.AgentHost = configuration.GetValue("OpenTelemetry:Exporters:Jaeger:AgentHost", "localhost");
                        options.AgentPort = configuration.GetValue("OpenTelemetry:Exporters:Jaeger:AgentPort", 6831);
                    });
            });
    }
}
