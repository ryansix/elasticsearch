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

        public RedisController(
            ITracer tracer,
            ConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _tracer = tracer;
        }
        [HttpGet(Name = "GetKey")]
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

    }
}
