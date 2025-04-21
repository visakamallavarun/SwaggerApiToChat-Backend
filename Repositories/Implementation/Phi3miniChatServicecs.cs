using Azure;
using Unanet_POC.Repositories.Interface;
using System.Text.Json;
using Azure.AI.Inference;
using Unanet_POC.Models.DTO;
using System.Text;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.IdentityModel.Tokens;

namespace Unanet_POC.Repositories.Implementation
{
    public class Phi3miniChatServicecs: IPhi3miniChatService
    {
        private readonly string _modelName;
        private readonly IConfiguration _configuration;
        private readonly Uri endpointURI;
        private readonly AzureKeyCredential credential;
        private readonly ChatCompletionsClient client;
        private readonly HttpClient _httpClient;

        private readonly List<ChatRequestMessage> _chatHistory = new();


        // Add a message history to store previous interactions
        public Phi3miniChatServicecs(IConfiguration configuration, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            string key = configuration["AzureAI:ModelApiKey"];
            string endpoint = configuration["AzureAI:ModelEndpointUrl"];
            _modelName = configuration["AzureAI:ModelName"];
            endpointURI = new Uri(endpoint);
            credential = new AzureKeyCredential(key);
            client = new ChatCompletionsClient(endpointURI, credential, new AzureAIInferenceClientOptions());
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
            var url = $"https://api.pipedrive.com/v1{path}/?api_token=cd37c62ccaa1c36afc7a22b8edc929b79e8cc57d";

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

        public async Task<chatResponse> UnifiedChatbotHandler(string userInput, JsonElement swaggerJson)
        {
            if (_chatHistory.Count == 0)
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
                    - Extract the relevant details from the Swagger document and return them in the `info` field. The `info` should be detailed, explaining endpoint names, methods, and any relevant fields or descriptions in full.
                    - Set `method`, `path`, and `payload` to null.
                    - Place a short and concise summary of the details in the `Speach` field, making sure it's easy for the bot to say out loud.
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
                    "Speach":"string" | null
                }

                Only return the JSON object.
                """
            ;
                _chatHistory.Add(new ChatRequestSystemMessage(systemMessage));
                _chatHistory.Add(new ChatRequestUserMessage($"Swagger JSON: {swaggerJson}"));
            }

            _chatHistory.Add(new ChatRequestUserMessage(userInput));

            var requestOptions = new ChatCompletionsOptions
            {
                Messages = _chatHistory,
                Model = _modelName,
                Temperature = 0.0f,
                NucleusSamplingFactor = 1.0f,
                FrequencyPenalty = 0.0f,
                PresencePenalty = 0.0f,
                ResponseFormat = new ChatCompletionsResponseFormatJSON()
            };

            Response<ChatCompletions> response = await client.CompleteAsync(requestOptions);
            var llmJson = response.Value.Content;

            _chatHistory.Add(new ChatRequestAssistantMessage(llmJson));

            LLMApiCall? apiCall;
            try
            {
                apiCall = JsonSerializer.Deserialize<LLMApiCall>(llmJson);
            }
            catch (Exception ex)
            { 
                return new chatResponse($"Error parsing model output: {ex.Message}");
            }

            if (apiCall == null || string.IsNullOrWhiteSpace(apiCall.intent))
                return new chatResponse("Invalid response from LLM.");

            if (apiCall.intent == "info")
            {
                return new chatResponse(apiCall.info ??  "No information returned.", apiCall.Speach);
            }

            if (string.IsNullOrWhiteSpace(apiCall.method) || string.IsNullOrWhiteSpace(apiCall.path))
                return new chatResponse("Missing method or path for action intent.");

            string methodAndPath = $"{apiCall.method} {apiCall.path}";
            string? payload = apiCall.payload?.ToString();

            var httpResponse = await SendRequestToApi(methodAndPath, payload);
            var prettyHttpResponse = await httpResponseConverter(httpResponse);

            return new chatResponse(prettyHttpResponse,apiCall.Speach,httpResponse);
        }

        private async Task<string> httpResponseConverter(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                return "Empty Response";
            }
            string systemPrompt = @"
                    You are a smart assistant that transforms raw JSON API responses into clear, natural-language summaries. When given a JSON object, your task is to:

                    - Understand the structure and contents of the JSON data, including nested arrays and objects.
                    - Extract key information that would be meaningful to a human reader.
                    - Present the information in a readable, organized, and friendly format using full sentences, bullet points, or tables when appropriate.
                    - Convert dates, timestamps, and boolean values into human-friendly terms (e.g., true → ""Yes"", ""2025-04-21T12:00:00Z"" → ""April 21, 2025, at 12:00 PM UTC"").
                    - If the JSON includes a list of items (e.g., users, orders, logs), summarize each item with relevant details, omitting overly technical or redundant data unless it's useful context.
                    - Maintain accuracy and avoid guessing—only include what’s present in the data.
                    - If possible, organize the summary into sections for better readability.

                    Respond as if you're explaining this information to a non-technical stakeholder or user.
            ";

            var requestOptions = new ChatCompletionsOptions()
            {
                Messages =
                    {
                        new ChatRequestSystemMessage(systemPrompt),
                        new ChatRequestUserMessage(response)
                    },
                Model = _modelName,
                Temperature = 0.0f,
                NucleusSamplingFactor = 1.0f,
                FrequencyPenalty = 0.0f,
                PresencePenalty = 0.0f,
            };

            Response<ChatCompletions> llmResponse = await client.CompleteAsync(requestOptions);

            return llmResponse.Value.Content;
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
    public string? Speach { get; set; } // for descriptive response when intent is "info"
}


