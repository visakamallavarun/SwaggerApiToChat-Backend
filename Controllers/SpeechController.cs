using Microsoft.AspNetCore.Mvc;
using Unanet_POC.Models.DTO;
using Unanet_POC.Repositories.Interface;

namespace Unanet_POC.Controllers
{
    [Route("api/speech")]
    [ApiController]
    public class SpeechController : ControllerBase
    {
        private readonly IConvertSpeechToTextRepository convertSpeechToTextRepository;
        private readonly IPhi3miniChatService phi3MiniChatService;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public SpeechController(IConvertSpeechToTextRepository convertSpeechToTextRepository,IPhi3miniChatService phi3MiniChatService, IConfiguration configuration, HttpClient httpClient)
        {
            this.convertSpeechToTextRepository = convertSpeechToTextRepository;
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


    }

}
