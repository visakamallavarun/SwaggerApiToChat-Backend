using System.Text.Json;

namespace Unanet_POC.Models.DTO
{
    public class SwaggerRequest
    {
        public JsonElement SwaggerJson { get; set; }
        public string EndpointName { get; set; }
    }
}
