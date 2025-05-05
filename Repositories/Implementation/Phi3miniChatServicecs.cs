using Azure;
using Unanet_POC.Repositories.Interface;
using System.Text.Json;
using Azure.AI.Inference;
using Unanet_POC.Models.DTO;
using System.Text;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.IdentityModel.Tokens;
using Unanet_POC.Models;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.PortableExecutable;

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

        private string urlPath { get; set; } = "https://api.pipedrive.com/v1";

        private string Params { get; set; }
        //api_token=cd37c62ccaa1c36afc7a22b8edc929b79e8cc57d

        private Dictionary<string, string> Headers { get; set; } = new();

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

        public void SetQueryParameters(QueryParamsDto dto)
        {
            Params = string.Join("&", dto.Params.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        }

        public void setHeaderParamValues(HeaderParamsDto dto)
        {
            Headers = dto.Params;
        }

        public void setURLPath(string url)
        {
            this.urlPath = url;
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
            var url = $"{urlPath}{path}/?{Params}";

            var request = new HttpRequestMessage(new HttpMethod(method), url);

            foreach (var header in Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

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
                        - If the user is asking for information, set `intent` = "info".
                        - If the user wants to execute an API call *and all required data is provided*, set `intent` = "action".
                        - If the user wants to execute an API call *but required parameters are missing*, treat it as `"info"` and ask follow-up questions.

                    - When `intent` is "info":
                        - If it’s a question about the API, extract the relevant details from the Swagger document and include them in the `info` field.
                        - If the user is trying to take action but hasn’t provided enough input, check for data in the chat history and perform that action if not present include a natural follow-up question in the `info` field.
                        - Set `method`, `path`, and `payload` to null.
                        - Provide a concise summary in the `Speach` field that reflects either the explanation or the follow-up prompt.

                    - When `intent` is "action":
                        - Determine the correct method and path from Swagger.
                        - Extract all required data from the prompt.
                        - Set `method`, `path`, and `payload` appropriately.
                        - Set `info` to null.
                        - Provide a clear, action-based summary in the `Speach` field describing what operation is being performed (e.g., "Book created successfully", "User updated", "Invoice submitted"). The Speach should reflect the outcome of the action being performed, not a confirmation or request.

                    Response JSON format:
                    {
                        "intent": "info" | "action",
                        "method": "POST" | "GET" | "PUT" | "DELETE" | "PATCH" | null,
                        "path": "/full/path/from/swagger" | null,
                        "payload": { ... } | null,
                        "info": "string" | null,
                        "Speach": "string" | null
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

            _chatHistory.Add(new ChatRequestUserMessage(response));

            return llmResponse.Value.Content;
        }

        public async Task<List<string>> generateActionList(JsonElement jsonElement)
        {
            string systemPrompt = @"
                You are a smart assistant that analyzes Swagger (OpenAPI 3.0) specifications and generates clear, user-friendly action descriptions based on the available API operations and metadata.

                Your task is to:
                - Understand the structure of the Swagger JSON, including paths, operations (GET, POST, PUT, DELETE), and general API metadata (title, version, authentication, etc.).
                - Generate a list of natural language prompts that describe what users can do with the API, such as 'Create a new lead' or 'Get details of a lead by ID'.
                - Include both operation-based actions (like creating or updating resources) and general information actions (like asking about the API version or authentication method).
                - Keep the descriptions simple, concise, and helpful — avoid technical jargon where possible.
                - Only include actions that can be directly derived from the Swagger JSON (do not infer or invent new capabilities).

                Format your response as a raw JSON array of strings — no objects, no wrapping keys, just the array.
                This format must match exactly what can be deserialized into a C# List<string>.

                Response JSON format:
                {
                    ""Action"": [
                    ""Create a new lead"",
                    ""List all leads"",
                    ""What is this API about?"",
                    ""Show the API version""
                ]
                }
                

                Now, given the following Swagger JSON, generate the appropriate list of actions:
                <insert swagger JSON here>
                ";

            var requestOptions = new ChatCompletionsOptions()
            {
                Messages =
                    {
                        new ChatRequestSystemMessage(systemPrompt),
                        new ChatRequestUserMessage($"Swagger JSON: {jsonElement}")
                    },
                Model = _modelName,
                Temperature = 0.0f,
                NucleusSamplingFactor = 1.0f,
                FrequencyPenalty = 0.0f,
                PresencePenalty = 0.0f,
                ResponseFormat = new ChatCompletionsResponseFormatJSON()
            };

            Response<ChatCompletions> llmResponse = await client.CompleteAsync(requestOptions);
            string jsonContent = llmResponse.Value.Content;

            ActionResponse? actions;
            try
            {
                actions = JsonSerializer.Deserialize<ActionResponse>(jsonContent);
            }
            catch (Exception ex)
            {
                return new List<string>();
            }

            return actions.Action ?? new List<string>();

        }

    }
}




