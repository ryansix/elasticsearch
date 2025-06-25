using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ApmAgentSampleApi.Options
{
    public class ApmOptions
    {
        public const string SectionName = "ElasticApm";

        [Required]
        public  string ServerUrl { get; set; }
        [Required]
        public  string ServiceName { get; set; }
        public string? SecretToken { get; set; }
        public string Environment { get; set; } = "Production";
        public bool CaptureRequestBody { get; set; } = true; // Enable capture request payload
        public bool OpenTelemetryBridgeEnabled { get; set; } = true; // Enable OTel adapter
    }
}
