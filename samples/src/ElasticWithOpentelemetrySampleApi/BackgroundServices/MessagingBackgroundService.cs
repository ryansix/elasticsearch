
using Elastic.Apm;
using Elastic.Apm.Api;
using System.Diagnostics;

namespace ElasticWithOpentelemetrySampleApi.BackgroundServices
{
    public class MessagingBackgroundService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // can start a transaction here then inherit the parent activity to see if it works
                using (var activity = ActivityHelper.GeneralActivitySource.StartActivity("ProcessMessageTransaction6", ActivityKind.Server
                     // , tags: new[] { new KeyValuePair<string, object?>("transaction.type", "messaging") }
                    ))
                {
                    if (activity != null)
                    {
                        var transaction = Agent.Tracer.StartTransaction(activity.DisplayName, ApiConstants.TypeMessaging);
                        activity.SetTag("transaction.type", ApiConstants.TypeMessaging);
                        activity.SetTag("messaging.system", "rabbitmq");
                        activity.SetTag("messaging.destination", "myqueue");
                        //activity.AddTag("transaction.type", "messaging");

                        //using (var spanActivity = ActivityHelper.GeneralActivitySource.StartActivity("SendMessage", ActivityKind.Producer))
                        //{
                        //    //activity.SetTag("service.type", "messaging");
                        //    if (spanActivity != null)
                        //    {
                        //        await Task.Delay(500);
                        //        spanActivity.AddEvent(new ActivityEvent("MessageSent"));
                        //    }
                        //}
                        await Task.Delay(500);
                    }
                }
            }
        }
    }
}
