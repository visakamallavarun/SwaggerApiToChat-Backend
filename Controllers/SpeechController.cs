using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Unanet_POC.Models.DTO;
using Unanet_POC.Repositories.Interface;

namespace Unanet_POC.Controllers
{
    [Route("api/speech")]
    [ApiController]
    public class SpeechController : ControllerBase
    {
        private readonly IPhi3miniChatService phi3MiniChatService;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public SpeechController(IPhi3miniChatService phi3MiniChatService, IConfiguration configuration, HttpClient httpClient)
        {
            this.phi3MiniChatService = phi3MiniChatService;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        [HttpGet("get-speech-token")]
        public async Task<IActionResult> GetSpeechToken()
        {
            string speechKey = _configuration["AzureAI:SpeechApiKey"];
            string speechRegion = _configuration["AzureAI:Region"];

            if (string.IsNullOrWhiteSpace(speechKey) || string.IsNullOrWhiteSpace(speechRegion))
            {
                return BadRequest("You forgot to add your speech key or region to the configuration.");
            }

            try
            {
                _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", speechKey);
                var response = await _httpClient.PostAsync($"https://{speechRegion}.api.cognitive.microsoft.com/sts/v1.0/issueToken", null);

                if (response.IsSuccessStatusCode)
                {
                    var token = await response.Content.ReadAsStringAsync();
                    return Ok(new { token, region = speechRegion });
                }
                else
                {
                    return Unauthorized("There was an error authorizing your speech key.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        [HttpGet("ImageURLToText")]
        public async Task<IActionResult> ImageURLToText([FromQuery] string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return BadRequest("Please provide a valid image URL.");
            }
            var result = await phi3MiniChatService.GenerateTextFromImageUrl(imageUrl);
            return Ok(result);
        }

        [HttpPost("UnifiedChatbotHandler")]
        public async Task<IActionResult> UnifiedChatbotHandler([FromBody] SwaggerChatRequest swaggerChatRequest)
        {
            if (swaggerChatRequest.Text == null || string.IsNullOrWhiteSpace(swaggerChatRequest.Text))
            {
                return BadRequest("Please provide a valid text.");
            }
            var result = await phi3MiniChatService.UnifiedChatbotHandler(swaggerChatRequest.Text, swaggerChatRequest.SwaggerJson);
            return Ok(result);
        }

        [HttpPost("Actions")]
        public async Task<IActionResult> GetAllActions([FromBody] JsonElement SwaggerJson)
        {
            if (SwaggerJson.ValueKind == JsonValueKind.Undefined ||
        SwaggerJson.ValueKind == JsonValueKind.Null ||
        (SwaggerJson.ValueKind == JsonValueKind.Object && SwaggerJson.EnumerateObject().Count() == 0))
            {
                return BadRequest("Swagger JSON is empty or invalid.");
            }

            var result = await phi3MiniChatService.generateActionList(SwaggerJson);
            return Ok(result);
        }

        [HttpPost("store-query-params")]
        public IActionResult StoreQueryParams([FromBody] QueryParamsDto dto)
        {
            if (dto == null || dto.Params == null)
            {
                return BadRequest("Invalid query parameters.");
            }

            // Replace or add new values
            phi3MiniChatService.SetQueryParameters(dto);

            return Ok();
        }

        [HttpPost("store-header-params")]
        public IActionResult StoreHeaderParams([FromBody] HeaderParamsDto dto)
        {
            if (dto == null || dto.Params == null)
            {
                return BadRequest("Invalid query parameters.");
            }

            // Replace or add new values
            phi3MiniChatService.setHeaderParamValues(dto);

            return Ok();
        }

        [HttpPost("store-url-path")]
        public IActionResult StoreURLPath([FromBody] string URL)
        {
            // Replace or add new values
            phi3MiniChatService.setURLPath(URL);

            return Ok();
        }
    }
}
