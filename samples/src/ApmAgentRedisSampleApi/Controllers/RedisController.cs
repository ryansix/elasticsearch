using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using StackExchange.Redis;
using StackExchange.Redis.Profiling;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ApmAgentRedisSampleApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RedisController : ControllerBase
    {
        private readonly ConnectionMultiplexer _connectionMultiplexer;
        private readonly ITracer _tracer;
        private readonly IApmAgent _apmAgent;

        public RedisController(
            IApmAgent apmAgent, 
            ConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _apmAgent = apmAgent;
            _tracer = apmAgent.Tracer;
        }
        [HttpGet("[action]")]
        public async Task<string> GetKey()
        {
            // use Elastic APM to create a Span 
            var span = _tracer.CurrentTransaction?.StartSpan(nameof(GetKey), "method");

            var database = _connectionMultiplexer.GetDatabase();
            var randomNumber = new Random().Next(1, 100);
            var key = $"redis:string:{randomNumber}";
            string value = string.Empty;
            if (await database.KeyExistsAsync(key))
            {
               var redisValue= await database.StringGetAsync(key);
                if (redisValue.HasValue)
                    value = redisValue.ToString();
            }
            else
            {
                value = $"{randomNumber}";
                await database.StringSetAsync(key, value);
            }

            if (span != null)
            {
                span.Context.Db = new Database
                {
                    Instance = "Test",
                    Statement = key,
                    Type = ApiConstants.SubTypeRedis
                };
            }
            span?.End();
            return value;
        }

        [HttpGet("[action]")]
        public async Task<string> CreateNewTraceAsync()
        { 
            var tracer = _apmAgent.Tracer;
            // start 
            var transaction = tracer.StartTransaction("OrderProcessing", "request");
            // Span 1: validate order
            var span1 = transaction.StartSpan("ValidateOrder", "validation");
            ValidateOrder();
            span1?.SetLabel("order.valid", true);
            span1?.End();

            // Span 2: handle pay
            var span2 = transaction.StartSpan("ProcessPayment", "payment");

            ProcessPayment();
            span2?.SetLabel("payment.method", "credit_card");
            span2?.End();

            // Span 3: update inventory（sub-nested Span）
            var span3 = transaction.StartSpan("UpdateInventory", "database");
            UpdateInventory(span3);
            span3?.End();

            transaction.Result = "success";

            transaction.End();
            
            return "okay";
        }

        [HttpGet("[action]")]
        public async Task<string> DistributedTraceAsync(string orderno)
        {
            var tracer = _apmAgent.Tracer;
            // start 
            var transaction = tracer.CurrentTransaction ?? tracer.StartTransaction("OrderProcessing", "request");
            
           // Span 1: validate order
           var span1 =  transaction.StartSpan("ValidateOrder", "");
            span1.Context.Db = new Database()
            {
                Statement = $"order:{orderno}"
            };
            ValidateOrder();
            span1?.SetLabel("order.valid", true);
            span1?.End();

            // Span 2: handle pay
            var span2 = transaction.StartSpan("ProcessPayment", ApiConstants.TypeMessaging);

            ProcessPayment();
            span2?.SetLabel("payment.method", "credit_card");
            span2?.End();

            // Span 3: update inventory（sub-nested Span）
            var span3 = transaction.StartSpan("UpdateInventory", ApiConstants.TypeDb);
            UpdateInventory(span3);
            span3?.End();

            transaction.Result = "success";

            transaction.End();
            //// start 
            //  tracer.CaptureTransaction(nameof(CaptureTraceAsync), "request", transaction =>
            //{
            //    transaction.CaptureSpan("OrderProcessing", "request", span =>
            //   {
            //       ValidateOrder();
            //       span?.SetLabel("order.valid", true);
            //   });

            //    // Span 2: handle pay
            //     transaction.CaptureSpan("ProcessPayment", "payment", span =>
            //     {
            //         ProcessPayment();
            //         span?.SetLabel("payment.method", "credit_card");
            //         span?.End();
            //     });

            //    // Span 3: update inventory（sub-nested Span）
            //    transaction.CaptureSpan("UpdateInventory", "database", span =>
            //    {
            //        UpdateInventory(span);
            //    });
            //    transaction.Result = "success";
            //});
            return "okay";
        }

        void ValidateOrder() { }
        void UpdateInventory(ISpan parentSpan)
        {
            // sub-span：database operation
            var dbSpan = parentSpan.StartSpan("DB_Query", "db.mysql");
            dbSpan?.SetLabel("query", "UPDATE stock SET quantity = quantity - 1");
            // simulate database calling
            Thread.Sleep(50);

        }
        void ProcessPayment() { }

    }
}
