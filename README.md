# OpenTelemetry Setup Guide for Tracing Microservices Using Jaeger

> 📝 **Project Notes:**  
> Note that the we have some prerelease packages installed due to compatability issues with old systems like our LMS application (dotnet 5). The following Configuration should work with all ITworx Education projects.

---

## 📦 Required NuGet Packages

Install the following packages in the Web Layer:

```
dotnet add package opentelemetry
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package opentelemetry.instrumentation.process --prerelease
dotnet add package opentelemetry.instrumentation.aspnetcore
dotnet add package opentelemetry.instrumentation.http
dotnet add package --prerelease opentelemetry.instrumentation.sqlclient
dotnet add package --prerelease opentelemetry.instrumentation.entityframeworkcore
dotnet add package opentelemetry.instrumentation.grpcnetclient
dotnet add package opentelemetry.exporter.console
dotnet add package opentelemetry.exporter.opentelemetryprotocol
dotnet add package opentelemetry.exporter.jaeger
dotnet add package opentelemetry.exporter.zipkin
dotnet add package opentelemetry.metrics
dotnet add package opentelemetry.instrumentation.runtime
```

---

## 📚Folder Structure

Create a new Folder **OpenTelemery** and add it to your Web layer.
Add `OpenTelemetryConfiguration.cs` to that folder.

---

## 🔌 What This File Does

The `OpenTelemetryConfiguration.cs` file configures OpenTelemetry to:

- Automatically trace ASP.NET Core HTTP requests
- Trace outgoing HTTP client calls
- Instrument Entity Framework Core and raw SQL calls
- Export all collected telemetry to Jaeger
- Allow manual trace points via `ActivitySource`

---

## 📦 Service Configuration Example Breakdown

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder
            .SetResourceBuilder(resourceBuilder)
            .SetSampler(new TraceIdRatioBasedSampler(configuration.GetValue("OpenTelemetry:Sampling:Ratio", 1.0)))
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = (httpContext) =>
                {
                    var path = httpContext.Request.Path.Value?.ToLower();
                    return !path?.Contains("/health") == true &&
                           !path?.Contains("/assets") == true &&
                           !path?.Contains("/css") == true &&
                           !path?.Contains("/js") == true &&
                           !path?.Contains("/img") == true &&
                           !path?.Contains("/favicon") == true;
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
            .AddSource("LMS.*");

        builder.AddJaegerExporter(options =>
        {
            options.AgentHost = configuration.GetValue("OpenTelemetry:Exporters:Jaeger:AgentHost", "localhost");
            options.AgentPort = configuration.GetValue("OpenTelemetry:Exporters:Jaeger:AgentPort", 6831);
        });
    });
```
# 🧾 OPTIONAL: OpenTelemetry Tracing Setup — Line-by-Line Explanation

## 1. `services.AddOpenTelemetry()`
- Registers OpenTelemetry with the **dependency injection container**.

## 2. `.WithTracing(builder => { ... })`
- Configures **tracing** (spans/activities) for your application.

## 3. `.SetResourceBuilder(resourceBuilder)`
- Sets **metadata** for your service (e.g., name, version, environment).
- This metadata appears in Jaeger and helps identify trace origins.

## 4. `.SetSampler(new TraceIdRatioBasedSampler(...))`
- Controls the **percentage of traces** that are recorded and exported.
- Example: `1.0` = 100% sampling, `0.5` = 50%.

## 5. `.AddAspNetCoreInstrumentation(options => { ... })`
- Automatically creates spans for **incoming HTTP requests**.

### Options:
- `options.RecordException = true;`  
  ➤ Records unhandled exceptions in HTTP requests.
  
- `options.Filter = ...`  
  ➤ **Filters out noisy paths** like `/health`, `/assets`, `/favicon` to keep traces meaningful.

## 6. `.AddHttpClientInstrumentation(options => { ... })`
- Automatically traces **outgoing HTTP requests** made using `HttpClient`.

### Options:
- `options.RecordException = true;`  
  ➤ Captures exceptions that occur during HTTP calls.

## 7. `.AddEntityFrameworkCoreInstrumentation(options => { ... })`
- Traces **Entity Framework Core** database operations.

### Options:
- `options.SetDbStatementForText = true;`  
  ➤ Records raw SQL for LINQ-generated queries.

- `options.SetDbStatementForStoredProcedure = true;`  
  ➤ Records SQL for stored procedure calls.

## 8. `.AddSqlClientInstrumentation(options => { ... })`
- Traces low-level **ADO.NET SQL operations**.

### Options:
- `options.SetDbStatementForText = true;`  
  ➤ Records text-based SQL commands.

- `options.RecordException = true;`  
  ➤ Captures SQL command errors.

## 9. `.AddSource("LMS.*")`
- Registers custom **`ActivitySource`** names (e.g., `"LMS.Web.Core"`).
- Enables **manual tracing** of business logic via `ActivitySource`.

## 10. `builder.AddJaegerExporter(options => { ... })`
- Configures Jaeger as the **trace exporter**.

### Options:
- `options.AgentHost` and `options.AgentPort`  
  ➤ Define where to send the traces (default: `localhost:6831`).
  


# 🧾 What you need to know men el akher:

This is the constant configuration part across all appliactions:
```csharp
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
            .AddSource("YourApp.ActivitySourceName") // Use your ActivitySource name for custom spans in our case in the "LMS.*" which means it captures all custom activities you create in your codebase using any ActivitySource with a name starting with LMS..
            .AddJaegerExporter(options =>
            {
                options.AgentHost = "localhost"; // or your Jaeger agent host
                options.AgentPort = 6831;        // default Jaeger UDP port
            });
    });
```


Now add this to your `Startup.cs` in the `ConfigureServices` function.

```
            services.AddOpenTelemetryLMS(Configuration);
            services.AddSingleton<ILMSTelemetryService, LMSTelemetryService>();
```

then add the configurations to your `appsettings.json` file:

```json
{
  "OpenTelemetry": {
    "ServiceName": "LMS.Web.Core",
    "Exporters": {
      "Jaeger": {
        "Enabled": true,
        "AgentHost": "localhost",
        "AgentPort": 6831
      }
    },
    "Sampling": {
      "Ratio": 1.0
    }
  }
}
```
## ✅ What the Basic Configuration Does

### 1. 🔧 Automatic Instrumentation

Your configuration enables the following built-in instrumentations:

- **AspNetCore**  
  ➤ Traces all **incoming HTTP requests** to your application (e.g., controllers, API endpoints).

- **HttpClient**  
  ➤ Traces all **outgoing HTTP requests** made by your app (e.g., REST API calls to other services).

- **EntityFrameworkCore**  
  ➤ Traces all **database operations** performed via EF Core (queries, inserts, updates).

- **SqlClient**  
  ➤ Traces all direct **SQL operations using ADO.NET** (e.g., `SqlCommand` executions).

---

### 2. ❗ Exception Recording

- **Exceptions** thrown during:
  - Incoming HTTP requests  
  - Outgoing HTTP calls  
  - Database operations  
  ➤ Are automatically **recorded as events** in the relevant span.

---

### 3. 🧹 Filtering

- Requests for **static files**, **health checks**, and other noise (like `/css`, `/favicon`, `/assets`) are **excluded from tracing** using the configured filter.

---

### 4. 📡 Jaeger Export

- All collected **traces and spans** are **exported to Jaeger**.
- You can view them in the **Jaeger UI** (default: [http://localhost:16686](http://localhost:16686)).

---

## 🔍 What You Will See in Jaeger

- ✅ Each **incoming HTTP request** will appear as a top-level trace/span.
- ✅ Each **outgoing HTTP call** will appear as a **child span** of the parent request.
- ✅ Each **database query or command** (EF Core or SqlClient) will also be a child span.
- ✅ **Exceptions** will show as red events attached to the relevant span.
- ✅ **Service metadata** (name, version, environment) will be attached to each trace.

---

## 🚫 What You Will NOT See

- ❌ **Internal business logic** steps or custom operations inside your methods  
  (_Unless you add manual tracing with `ActivitySource`_)

- ❌ **Fine-grained tracing** of your code paths (e.g., method-level, conditional branches)  
  (_Requires custom spans to be added manually_)

---

# 🎯 OPTIONAL: Tracing Business Logic with ActivitySource

If you want to trace **business logic steps** inside a function in your LMS, you should use a custom `ActivitySource` to create spans for those steps. This allows you to see **detailed, step-by-step traces** in Jaeger — not just the automatic HTTP or database traces.

---

## 💡 Example: Tracing Business Logic Steps

Suppose you have a simple service method:

```csharp
public class EnrollmentService
{
    public void EnrollStudent(int studentId, int courseId)
    {
        // Step 1: Validate input
        // Step 2: Add enrollment to database
        // Step 3: Send notification
    }
}
```

---

## 🛠️ How to Add Business Logic Tracing

### 1. Define an `ActivitySource`

Usually declared as a static field in the class or a shared tracing utility:

```csharp
private static readonly ActivitySource ActivitySource = new("LMS.Web.Core");
```

---

### 2. Wrap Each Business Step in an Activity

```csharp
public void EnrollStudent(int studentId, int courseId)
{
    using (var activity = ActivitySource.StartActivity("EnrollStudent"))
    {
        activity?.SetTag("student.id", studentId);
        activity?.SetTag("course.id", courseId);

        // Step 1: Validate input
        using (var step = ActivitySource.StartActivity("ValidateInput"))
        {
            // ... validation logic ...
        }

        // Step 2: Add enrollment to database
        using (var step = ActivitySource.StartActivity("AddEnrollmentToDb"))
        {
            // ... DB logic ...
        }

        // Step 3: Send notification
        using (var step = ActivitySource.StartActivity("SendNotification"))
        {
            // ... notification logic ...
        }
    }
}
```

---

## 🔍 What You Will See in Jaeger

- ✅ A **top-level span** for `EnrollStudent`
- ✅ **Child spans** for:
  - `ValidateInput`
  - `AddEnrollmentToDb`
  - `SendNotification`
- ✅ Custom **tags** (like `student.id`, `course.id`)
- ✅ **Exception events** if any errors occur during those steps
- ✅ A **hierarchical breakdown** of your business logic execution

---

## 📌 Best Practices

- Use `activity?.SetTag("key", value)` to include meaningful metadata in each span.
- Use `activity?.AddEvent(new ActivityEvent("event-name"))` for key business milestones.
- Handle exceptions with:

```csharp
try
{
    // ... logic
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.RecordException(ex);
    throw;
}
```

- Note that if you are using custom activities you will need to add a project reference from your services layer to the web layer (Where openTelemetery is setup).

---

# 🚀 Final Steps: Running Jaeger and Launching Your Application

To start collecting and visualizing traces from your application, you need to **run Jaeger** locally and then start your app.

---

## 🐳 Step 1: Run Jaeger via Docker

Open your terminal and run the following command:

```bash
docker run -d --name jaeger -e COLLECTOR_ZIPKIN_HOST_PORT=:9411 -p 16686:16686 -p 6831:6831/udp -p 6832:6832/udp -p 5778:5778 -p 14268:14268 -p 14250:14250 -p 9411:9411 jaegertracing/all-in-one:1.57
```

### 📌 This will:
- Start a Jaeger container in the background
- Expose the Jaeger UI on [http://localhost:16686](http://localhost:16686)
- Open ports for trace collection (UDP & HTTP)

---

## 🟢 Step 2: Run Your Application

Once Jaeger is up, launch your application as usual.

```bash
dotnet run
```

## 🔍 Step 3: View Traces in Jaeger

1. Open your browser and go to: [http://localhost:16686](http://localhost:16686)
2. In the search bar:
   - Select your **service name** (e.g., `LMS.Web.Core`)
   - Click “Find Traces”
3. Click on any trace to see:
   - Span hierarchy (business logic steps if you used `ActivitySource`)
   - Tags and attributes
   - Duration and errors (if any)

---

## 🧼 Optional: Stop/Remove Jaeger

If you want to stop or clean up Jaeger later:

```bash
docker stop jaeger
docker rm jaeger
```
## 👉 Advanced: OpenTelemetry Middleware Setup for Distributed Tracing

To enable **full distributed tracing** in your .NET 5 LMS microservices using OpenTelemetry and Jaeger, you need to:

### 💪 Step 1: Middleware Configuration Explained

OpenTelemetry uses *instrumentation libraries* and *middleware* to automatically generate telemetry data (called spans) for:

* Incoming HTTP requests
* Outgoing HTTP client calls
* SQL commands and EF Core operations
* Custom application events (via `ActivitySource`)

These spans are collected and exported to Jaeger for visualization.

---

### 🛠️ Step 2: Modify `Startup.cs` to Use OpenTelemetry

Ensure you add the OpenTelemetry services in `Startup.cs` like this:

#### 🔧 `Startup.cs` → `ConfigureServices`

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddControllers();

    // Add OpenTelemetry Tracing
    services.AddOpenTelemetryLMS(Configuration);
}
```

> `AddOpenTelemetryLMS` is the extension method in `OpenTelemetryConfiguration.cs`.

---

#### ⚙️ `Startup.cs` → `Configure`

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseRouting();

    app.UseAuthorization();

    // Add middleware to auto-trace incoming HTTP requests
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

✅ The `AddAspNetCoreInstrumentation()` in your `OpenTelemetryConfiguration.cs` file automatically hooks into `UseRouting` and `UseEndpoints` to generate traces for each HTTP request.

---

### 🧠 Step 3: What Gets Traced Automatically

With your current setup, OpenTelemetry captures:

| Component         | Captured Automatically?                                    | Notes                                   |
| ----------------- | ---------------------------------------------------------- | --------------------------------------- |
| ASP.NET Core HTTP | ✅ Yes                                                      | Filters out `/health`, `/favicon`, etc. |
| `HttpClient`      | ✅ Yes                                                      | Records exceptions by default           |
| EF Core Queries   | ✅ With `EnrichWithIDbCommand`                              | Adds SQL as `db.statement`              |
| ADO.NET/SqlClient | ✅ With `Enrich`                                            | Adds SQL command and command type       |
| Custom Code       | ⚠️ You must call `ActivitySource.StartActivity()` manually |                                         |

---

## 🌐 Distributed Tracing Across Microservices

When your application is composed of **multiple services** (e.g., `LMS.Web.Core`, `LMS.EnrollmentService.API`, `LMS.NotificationsService`), you can track requests across all of them by enabling **distributed tracing** using OpenTelemetry.

This is done by **propagating trace context** (trace ID, span ID, etc.) across service boundaries using HTTP headers.

---

### ✅ Step 1: Add `OpenTelemetryPropagationHandler.cs`

Create this file in each microservice project (or share it in a common utility library):

```csharp
using OpenTelemetry.Context.Propagation;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

public class OpenTelemetryPropagationHandler : DelegatingHandler
{
    private static readonly ActivitySource ActivitySource = new("LMS.Web.Core");
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), request, InjectTraceContextIntoHeaders);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private static void InjectTraceContextIntoHeaders(HttpRequestMessage request, string key, string value)
    {
        if (!request.Headers.Contains(key))
        {
            request.Headers.Add(key, value);
        }
    }
}
```

> 🧠 This middleware ensures that all **outgoing HTTP requests** carry the current trace context.

---

### ✅ Step 2: Register the Handler in `Startup.cs`

In each microservice, register the handler and attach it to your `HttpClient`:

```csharp
services.AddTransient<OpenTelemetryPropagationHandler>();

services.AddHttpClient()
        .AddHttpMessageHandler<OpenTelemetryPropagationHandler>();
```

> ✅ You can also register it globally using `.AddHttpClient()` if you're not using named clients.

---


### ✅ Step 3: Ensure the Receiving Service is Also Instrumented

In each service **receiving** a request:

- Make sure `AddAspNetCoreInstrumentation()` is added in your `OpenTelemetryConfiguration.cs`
- This will **extract trace headers** from the incoming request and **continue the trace** from where the previous service left off.

---

## 🔍 What You’ll See in Jaeger

- ✅ A **single trace** spanning multiple services  
- ✅ Each service’s spans appear as **child spans** of the original trace  
- ✅ **Cross-service latency** is visible  
- ✅ **Exceptions** are visible across the full call chain  

---

## 📎 Summary Checklist for Each Microservice

- [x] Install OpenTelemetry NuGet packages  
- [x] Add `OpenTelemetryConfiguration.cs` and configure tracing  
- [x] Add `OpenTelemetryPropagationHandler.cs`  
- [x] Register and use `OpenTelemetryPropagationHandler` with `HttpClient`  
- [x] Ensure `appsettings.json` has a unique `"ServiceName"`  
- [x] Ensure `AddAspNetCoreInstrumentation()` is called in OpenTelemetry setup  
- [x] Verify Jaeger is running (`http://localhost:16686`)  
- [x] Launch the service and inspect traces in Jaeger UI  

---








