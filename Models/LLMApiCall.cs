using System.Text.Json;

namespace Unanet_POC.Models
{
    public class LLMApiCall
    {
        public string intent { get; set; } = default!; // "info" or "action"
        public string? method { get; set; }
        public string? path { get; set; }
        public JsonElement? payload { get; set; }
        public string? info { get; set; } // for descriptive response when intent is "info"
        public string? Speach { get; set; } // for descriptive response when intent is "info"
    }
}
