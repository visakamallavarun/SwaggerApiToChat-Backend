using System.Text.Json;

namespace Unanet_POC.Models.DTO
{
    public class SwaggerChatRequest
    {
        public string Text { get; set; }
        public JsonElement SwaggerJson { get; set; }
    }
}
