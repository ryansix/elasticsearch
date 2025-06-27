using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.NetCoreAll;
using Elastic.Apm.Report;
using Elastic.Apm.StackExchange.Redis;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAllElasticApm();

var connectionString = builder.Configuration.GetConnectionString("Redis");
ArgumentNullException.ThrowIfNull(connectionString);
var connection = await ConnectionMultiplexer.ConnectAsync(connectionString).ConfigureAwait(false);
//var connection = ConnectionMultiplexer.Connect(connectionString);
builder.Services.AddSingleton(sp => connection);
connection.UseElasticApm();


// #Configure OpenTelemetry 
//builder.Services.AddOpenTelemetry()
//    .WithTracing(tracerProviderBuilder =>
//    {
//    tracerProviderBuilder
//        .AddSource("ElasticApmDemo")
//        .SetResourceBuilder(ResourceBuilder.CreateDefault()
//        .AddService(builder.Configuration.GetValue("ApmSettings:ServiceName")))
//        .AddAspNetCoreInstrumentation()
//        .AddHttpClientInstrumentation()
//        .AddEntityFrameworkCoreInstrumentation()
//        .AddConsoleExporter();
    // For debugging, replace with Elastic exporter in production });

    //// 集成OpenTelemetry
    //builder.Services.AddOpenTelemetry()
    //.WithTracing(tracerProviderBuilder =>
    //{

    //    tracerProviderBuilder

    //        .AddSource(configuration.GetSection("ElasticApm:ServiceName").Get<string>()) // 自定义ActivitySource
    //        .SetResourceBuilder(
    //            ResourceBuilder
    //                .CreateDefault() 
    //                .AddService(configuration.GetSection("ElasticApm:ServiceName").Get<string>())
    //            )
    //        .AddAspNetCoreInstrumentation()
    //        .AddConsoleExporter()
              
    //        //.AddHttpClientInstrumentation

    //        //.AddElasticApmInstrumentation() // APM与OTel桥接
    //        //.AddAspNetCoreInstrumentation(options =>
    //        //{
    //        //    options.RecordException = true;
    //        //    options.EnableGrpcAspNetCoreSupport = true;
    //        //    // 捕获请求和响应Body
    //        //    options.EnrichWithHttpRequest = (activity, request) =>
    //        //    {
    //        //        if (request.ContentLength > 0 && request.Body.CanRead)
    //        //        {
    //        //            request.EnableBuffering(); // 允许重复读取Body
    //        //            using var reader = new StreamReader(request.Body, leaveOpen: true);
    //        //            var body = reader.ReadToEndAsync().Result;
    //        //            activity.SetTag("http.request.body", body);
    //        //            request.Body.Position = 0; // 重置流位置
    //        //        }
    //        //    };
    //        //})
    //        ; // 可选：导出到OTLP Collector
    //});

var app = builder.Build();
// using with Elastic.Apm 1.22.0
//app.UseAllElasticApm(builder.Configuration);
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();




// Start a new activity (span)
ActivitySource activitySource = new ActivitySource("MyApp");
using (var activity = activitySource?.StartActivity("ProcessRequest"))
{
    // Access SpanId and TraceId
    var spanId = activity?.SpanId.ToString(); // Unique ID for this span
    var traceId = activity?.TraceId.ToString(); // Shared across all spans in the trace
    var parentSpanId = activity?.ParentSpanId.ToString(); // Parent span's ID (if any)

    Console.WriteLine($"TraceId: {traceId}, SpanId: {spanId}, ParentSpanId: {parentSpanId}");
}

// filter 
var apmAgent = app.Services.GetRequiredService<IApmAgent>();
if(apmAgent.PayloadSender is IPayloadSenderWithFilters apmPayloadFilters)
{
    apmPayloadFilters.AddFilter((ISpan span) =>
    {
        Console.WriteLine("name:" + span.Name);
        if (span.Name.Contains("swagger.json")) return null;
        return span;
    });

    apmPayloadFilters.AddFilter((Elastic.Apm.Api.ITransaction transaction) =>
    {
        //if (payload is HttpRequest request &&
        //    (request.Path.StartsWithSegments("/debug") ||
        //     request.Path.StartsWithSegments("/admin")))
        //{
        //    return null; // 丢弃匹配的请求
        //}
        //return payload;
        Console.WriteLine("transaction name:" + transaction.Name);
        return transaction;
    });
}
app.Run();
