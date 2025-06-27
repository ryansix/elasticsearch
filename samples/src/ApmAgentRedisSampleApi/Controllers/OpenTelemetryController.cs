using Elastic.Apm.Api;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Diagnostics;

namespace ApmAgentRedisSampleApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OpenTelemetryController : ControllerBase
    {
        private ActivitySource ActivitySource = Activity.Current?.Source ?? new ActivitySource(nameof(OpenTelemetryController));
        private readonly ConnectionMultiplexer _connectionMultiplexer;
        public OpenTelemetryController(ConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }
        [HttpGet("[action]")]
        public async Task<string> GetKey()
        {
            // Manual span creation via ActivitySource


            var activity = ActivitySource?.StartActivity("MyMethod");

            activity?.SetTag("custom.tag", "value");

            var database = _connectionMultiplexer.GetDatabase();
            var randomNumber = new Random().Next(1, 100);
            var key = $"redis:string:{randomNumber}";
            string value = string.Empty;
            if (await database.KeyExistsAsync(key))
            {
                var redisValue = await database.StringGetAsync(key);
                if (redisValue.HasValue)
                    value = redisValue.ToString();
            }
            else
            {
                value = $"{randomNumber}";
                await database.StringSetAsync(key, value);
            }


            activity?.SetTag("db.statement", key);

            return value;
        }
    }
}
