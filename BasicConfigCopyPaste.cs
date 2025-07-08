services.AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName, serviceVersion)
            )
            .SetSampler(new TraceIdRatioBasedSampler(1.0)) // 100% sampling; adjust as needed
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                // Optionally filter out static files, health checks, etc.
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddEntityFrameworkCoreInstrumentation()
            .AddSqlClientInstrumentation()
            .AddSource("YourApp.ActivitySourceName") // Use your ActivitySource name for custom spans
            .AddJaegerExporter(options =>
            {
                options.AgentHost = "localhost"; // or your Jaeger agent host
                options.AgentPort = 6831;        // default Jaeger UDP port
            });
    });
