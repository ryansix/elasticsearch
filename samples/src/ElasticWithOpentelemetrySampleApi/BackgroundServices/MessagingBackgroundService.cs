
using Elastic.Apm;
using Elastic.Apm.Api;
using System.Diagnostics;
using System.Transactions;

namespace ElasticWithOpentelemetrySampleApi.BackgroundServices
{
    public class MessagingBackgroundService : BackgroundService
    {
        private readonly IApmAgent _apmAgent;
        public MessagingBackgroundService(IApmAgent apmAgent)
        {
            _apmAgent = apmAgent;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Here is define a transaction type to messaging via ITrancer. 
                var traceTransaction = _apmAgent.Tracer.StartTransaction("MessageConsuming", ApiConstants.TypeMessaging);
                ActivityContext? activityContext = Activity.Current?.Context;
                var activitySource = ActivityHelper.GeneralActivitySource;
                // can start a transaction here then inherit the parent activity to see if it works
                //using (var activity = activityContext==null ?  activitySource.StartActivity("ProcessMessageTransaction6", ActivityKind.Server):
                //    activitySource.StartActivity("ProcessMessageTransaction",ActivityKind.Server, activityContext.Value)
                //    )
                using (var activity = activitySource.StartActivity("RabitMQMessageConsuming", ActivityKind.Server))
                {
                    if (activity != null)
                    {
                        var transaction = Agent.Tracer.StartTransaction(activity.DisplayName, ApiConstants.TypeMessaging);
                        activity.SetTag("transaction.type", ApiConstants.TypeMessaging);
                        activity.SetTag("messaging.system", "rabbitmq");
                        activity.SetTag("messaging.destination", "myqueue");


                        using (var spanActivity = ActivityHelper.GeneralActivitySource.StartActivity("SendMessage", ActivityKind.Producer))
                        {
                            //activity.SetTag("service.type", "messaging");
                            if (spanActivity != null)
                            {
                                await Task.Delay(500);
                                spanActivity.AddEvent(new ActivityEvent("MessageSent"));
                            }
                        }
                        await Task.Delay(500);
                    }
                }

                traceTransaction.Result = "success";

                traceTransaction.End();
            }
        }
    }
}
