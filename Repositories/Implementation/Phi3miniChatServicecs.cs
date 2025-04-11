using Azure;
using Unanet_POC.Repositories.Interface;
using System.Text.Json;
using Azure.AI.Inference;
using Unanet_POC.Models.DTO;
using System.Text;

namespace Unanet_POC.Repositories.Implementation
{
    public class Phi3miniChatServicecs: IPhi3miniChatService
    {
        private readonly string _modelName;
        private readonly IConfiguration _configuration;
        private readonly Uri endpointURI;
        private readonly AzureKeyCredential credential;
        private readonly HttpClient _httpClient;

        public Phi3miniChatServicecs(IConfiguration configuration, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            string key = configuration["AzureAI:ModelApiKey"];
            string endpoint = configuration["AzureAI:ModelEndpointUrl"];
            _modelName = configuration["AzureAI:ModelName"];
            endpointURI = new Uri(endpoint);
            credential = new AzureKeyCredential(key);
        }

        private async Task<string> SendRequestToApi(string endpointWithMethod, string? payload = null)
        {
            if (string.IsNullOrWhiteSpace(endpointWithMethod))
                return "Invalid input: endpoint and method required.";

            var parts = endpointWithMethod.Split(' ', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                return "Invalid format. Expected: METHOD /path";

            var method = parts[0].ToUpperInvariant();
            var path = parts[1];
            var url = $"https://fakerestapi.azurewebsites.net{path}";

            var request = new HttpRequestMessage(new HttpMethod(method), url);

            if (payload != null && method is "POST" or "PUT" or "PATCH")
            {
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            }

            try
            {
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                return response.IsSuccessStatusCode
                    ? content
                    : $"Error: {response.StatusCode} - {content}";
            }
            catch (Exception ex)
            {
                return $"Exception during request: {ex.Message}";
            }
        }

        public async Task<string> UnifiedChatbotHandler(string userInput, JsonElement swaggerJson)
        {
            string systemMessage = """
                You are an intelligent Swagger-based API assistant.

                Context:
                - You are provided with a full Swagger JSON document that includes all endpoints, HTTP methods, and schemas.
                - Users can ask for:
                    1. **Information** about the API (e.g., available endpoints, method descriptions, field meanings).
                    2. **Execute** an API call (e.g., "Create a new book with title X").

                Instructions:
                - Analyze the user prompt and decide the `intent`:
                    - If the user is asking for info, set `intent` = "info".
                    - If the user wants to trigger an API call, set `intent` = "action".
                - When `intent` is "info":
                    - Extract relevant details from the Swagger and return them in the `info` field.
                    - Set `method`, `path`, and `payload` to null.
                - When `intent` is "action":
                    - Determine correct method and path from Swagger.
                    - Extract data from prompt and fill the `payload` (or null if GET/DELETE).
                    - Return structured response for execution.

                Response JSON format:
                {
                    "intent": "info" | "action",
                    "method": "POST" | "GET" | "PUT" | "DELETE" | "PATCH" | null,
                    "path": "/full/path/from/swagger" | null,
                    "payload": { ... } | null,
                    "info": "string" | null
                }

                Only return the JSON object.
                """;

            var client = new ChatCompletionsClient(endpointURI, credential, new AzureAIInferenceClientOptions());
            var requestOptions = new ChatCompletionsOptions()
            {
                    Messages =
                    {
                        new ChatRequestSystemMessage(systemMessage),
                        new ChatRequestUserMessage($"Swagger JSON: {swaggerJson}"),
                        new ChatRequestUserMessage(userInput)
                    },
                Model = _modelName,
                Temperature = 0.0f,
                NucleusSamplingFactor = 1.0f,
                FrequencyPenalty = 0.0f,
                PresencePenalty = 0.0f,
                ResponseFormat = new ChatCompletionsResponseFormatJSON()
            };

            Response<ChatCompletions> response = await client.CompleteAsync(requestOptions);
            var llmJson = response.Value.Content;

            LLMApiCall? apiCall;
            try
            {
                apiCall = JsonSerializer.Deserialize<LLMApiCall>(llmJson);
            }
            catch (Exception ex)
            {
                return $"Error parsing model output: {ex.Message}";
            }

            if (apiCall == null || string.IsNullOrWhiteSpace(apiCall.intent))
                return "Invalid response from LLM.";

            if (apiCall.intent == "info")
            {
                return apiCall.info ?? "No information returned.";
            }

            if (string.IsNullOrWhiteSpace(apiCall.method) || string.IsNullOrWhiteSpace(apiCall.path))
                return "Missing method or path for action intent.";

            string methodAndPath = $"{apiCall.method} {apiCall.path}";
            string? payload = apiCall.payload?.ToString();

            return await SendRequestToApi(methodAndPath, payload);
        }

    }
}

public class LLMApiCall
{
    public string intent { get; set; } = default!; // "info" or "action"
    public string? method { get; set; }
    public string? path { get; set; }
    public JsonElement? payload { get; set; }
    public string? info { get; set; } // for descriptive response when intent is "info"
}

