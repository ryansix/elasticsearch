using ElasticWithOpentelemetrySampleApi;
using ElasticWithOpentelemetrySampleApi.BackgroundServices;
using System.Diagnostics; 

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
var source = ActivityHelper.GeneralActivitySource;
var listener = new ActivityListener
{
    ActivityStarted = activity => { },
    ActivityStopped = activity => { },
    ShouldListenTo = _ => true,
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
};

ActivitySource.AddActivityListener(listener);
Activity? act;


act= source?.StartActivity("name", ActivityKind.Internal);
act?.Start();
var cu = Activity.Current;
using var activ = source?.StartActivity("myname");
var cu2 = Activity.Current;
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAllElasticApm(); // Enable Elastic APM

builder.Services.AddHostedService<MetricsBackgroundService>()
    .AddHostedService<MessagingBackgroundService>();
var app = builder.Build();

long requestNumber = 0;
app.MapGet("/hello", async () =>
{
    using var activity = ActivityHelper.GeneralActivitySource.StartActivity("ProcessRequestA", ActivityKind.Server);
    if (activity != null)
    {
        Interlocked.Increment(ref requestNumber);
        activity.AddTag("http.method", "GET");
        activity.AddTag("http.url", "/");
        activity.SetTag("request.number", requestNumber);
        activity?.SetTag("request.payload", "hello,world!");
        await Task.Delay(100);
        activity?.AddEvent(new ActivityEvent("ProcessingComplete"));
        Console.WriteLine($"==========ProcessingComplete{requestNumber}==========");
    }
    return $"Hello, World{requestNumber}!";
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
