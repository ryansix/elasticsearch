using System.Diagnostics;
using System.Reflection;

namespace ElasticWithOpentelemetrySampleApi
{
    public static class ActivityHelper
    {
        private static readonly string Name = Assembly.GetExecutingAssembly().GetName().ToString();
        private static readonly string? Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        public static readonly ActivitySource GeneralActivitySource = new ActivitySource("ElasticWithOpentelemetrySampleApi.Messaging", Version);
    }
}
