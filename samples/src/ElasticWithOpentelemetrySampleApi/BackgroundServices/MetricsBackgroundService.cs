
using Elastic.Apm;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ElasticWithOpentelemetrySampleApi.BackgroundServices
{
    public class MetricsBackgroundService : BackgroundService
    {
        //private static readonly ActivitySource _activitySource = new ActivitySource("ElasticWithOpentelemetrySampleApi.Metrics",);
        private static readonly Meter _meter = new Meter("ElasticWithOpentelemetrySampleApi.Metrics", "1.0.0");
        private readonly Counter<long> _requestCounter;
        private readonly Histogram<double> _processingDuration;
        private readonly ILogger<MetricsBackgroundService> _logger;
        public MetricsBackgroundService(ILogger<MetricsBackgroundService> logger)
        {
            _logger = logger;
            _requestCounter = _meter.CreateCounter<long>("ElasticWithOpentelemetrySampleApi.requests.total", "Requests", "Total number of processed requests");
            _processingDuration = _meter.CreateHistogram<double>("ElasticWithOpentelemetrySampleApi.processing.duration", "ms", "Duration of request processing");
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task.Factory.StartNew(async (obj) =>
            {
                if (obj == null) return;
                if (obj is CancellationToken ct)
                {
                    _logger.LogInformation("Metrics Background Service started.");
                    while (!ct.IsCancellationRequested)
                    {
                        using (var activity = ActivityHelper.GeneralActivitySource.StartActivity("ProcessMetrics4", ActivityKind.Server))
                        {
                            try
                            {
                                var stopwatch = Stopwatch.StartNew();

                                // Simulate processing tasks (such as HTTP requests or data processing)
                                await Task.Delay(100, ct); // Simulate work
                                _requestCounter.Add(1, new KeyValuePair<string, object?>("operation", "process"));

                                // Record processing time
                                stopwatch.Stop();
                                _processingDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("operation", "process"));

                                // Adding Tracking Events
                                activity?.SetTag("request.payload", "test");
                                activity?.SetTag("request.header", "jwt");
                                activity?.SetTag("service.name", nameof(MetricsBackgroundService)); //
                                activity?.SetTag("http.method", "GET");
                                activity?.SetTag("http.url", "/metrics");

                                activity?.SetTag("messaging.system", "rabbitmq"); // 
                                activity?.SetTag("messaging.destination", "myqueue"); // 
                                activity?.SetTag("messaging.operation", "process"); //
                                activity?.AddEvent(new ActivityEvent("MetricsProcessed"));
                                
                           
                                //_logger.LogInformation("Processed metric: RequestCount={Count}, Duration={Duration}ms",
                                //    _requestCounter.ToString(), stopwatch.ElapsedMilliseconds);
                            }
                            catch (Exception ex)
                            {
                                // Catch exceptions and log them Elastic APM
                                //RecordException(activity, ex);
                                _logger.LogError(ex, "Error processing metrics");
                            }
                        }

                        // Excute every 5 seconds 
                        await Task.Delay(TimeSpan.FromSeconds(2), ct);
                    }
                    _logger.LogInformation("Metrics Background Service stopped.");
                }
            }, stoppingToken);
            return Task.CompletedTask;
        }

        private void RecordException(Activity? activity , Exception ex)
        {
            if(activity!=null && ex != null)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                ActivityTagsCollection activityTagsCollection = new ActivityTagsCollection()
                {
                    {
                        "exception.type",
                        ex.GetType().FullName
                    },
                    {
                        "exception.stacktrace",
                        ex.ToString()
                    },
                    {
                        "exception.message",
                        ex.Message.ToString()
                    }
                };

                activity.AddEvent(new ActivityEvent("exception", default, activityTagsCollection));
                activity.SetTag("exception.details", ex.ToString());
                activity.SetTag("recordedexception.elapsedMs", stopwatch.Elapsed.TotalMilliseconds);
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            }
        }
    }
}
