using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Diagnostics;
using Unanet_POC.Domain;
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
            string speechKey = _configuration["AzureAI:ApiKey"];
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


        [HttpPost("transcribe")]
        public async Task<IActionResult> TranscribeAudio(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please upload a valid .wav file.");

            if (Path.GetExtension(file.FileName).ToLower() != ".wav")
                return BadRequest("Only .wav files are supported.");

            // Save the uploaded file to a temporary location
            var filePath = Path.GetTempFileName();
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            try
            {
                var result = await convertSpeechToTextRepository.ConvertSpeechToText(filePath);
                var selectedProject = await phi3MiniChatService.selectProject(result);
                return Ok( selectedProject );
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing audio: {ex.Message}");
            }
            finally
            {
                // Clean up temp file
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
        }

        [HttpPost("selectProject")]
        public async Task<IActionResult> SelectProject([FromBody] Text request)
        {
            if(request == null || string.IsNullOrWhiteSpace(request.SpeechValue))
            {
                return BadRequest("Please provide a valid text.");
            }
           
            var result = await phi3MiniChatService.selectProject(request.SpeechValue);

            return Ok(result);
        }


    }

}
