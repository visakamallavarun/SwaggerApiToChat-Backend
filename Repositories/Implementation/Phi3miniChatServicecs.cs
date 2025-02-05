using Azure.AI.OpenAI;
using Azure;
using OpenAI.Chat;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Unanet_POC.Repositories.Interface;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Nodes;
using System.Text.Json;
using Unanet_POC.DTO;
using Azure.Identity;
using Azure.AI.Inference;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using System.Net;

namespace Unanet_POC.Repositories.Implementation
{
    public class Phi3miniChatServicecs: IPhi3miniChatService
    {
        private readonly string _modelName;
        private readonly IConfiguration _configuration;
        private readonly Uri endpointURI;
        private readonly AzureKeyCredential credential;

        public Phi3miniChatServicecs(IConfiguration configuration)
        {
            _configuration = configuration;
            string key = configuration["AzureAI:ApiKey"];
            string endpoint = configuration["AzureAI:EndpointUrl"];
            _modelName = configuration["AzureAI:ModelName"];
            endpointURI = new Uri(endpoint);
            credential = new AzureKeyCredential(key);
        }

        public async Task<string> GetChatCompletion(string systemMessage, string userMessage)
        {

            var client = new ChatCompletionsClient(endpointURI, credential, new AzureAIInferenceClientOptions());
            var requestOptions = new ChatCompletionsOptions()
            {
                Messages =
    {
        new ChatRequestSystemMessage(systemMessage),
        new ChatRequestUserMessage(userMessage),
    },Model = _modelName
            };
            Response<ChatCompletions> response = await client.CompleteAsync(requestOptions);
            return response.Value.Content;
        }

        public async Task<string> selectProject(string speechText)
        {
            var projects = _configuration.GetSection("Projects").Get<List<Project>>();
            string projectsString = JsonSerializer.Serialize(projects, new JsonSerializerOptions { WriteIndented = true });
            string systemMessage = "You are an assistant that extracts multiple projects from a user's sentence and assigns the specific hours mentioned for each project. The response should be in JSON format as a list, where each object contains 'id', 'name', 'category', and 'hours'. The 'hours' field represents the number of hours the user mentioned for that project.\n\nYou will be given a predefined list of projects, and the user's input will contain project names along with hours worked. Match the mentioned projects and their corresponding hours to generate the response.\n\nIf no projects from the list match the user's sentence, return the string:\n\"No matches found\"\n\nExample project list:\n[\n  { \"id\": 1, \"name\": \"Library Management System\", \"category\": \"Web Development\" },\n  { \"id\": 2, \"name\": \"E-Commerce Website\", \"category\": \"Web Development\" }\n]\n\nExample user input:\n'I worked on the Library Management System for 5 hours and spent 3 hours developing an E-Commerce Website.'\n\nExpected response when matches are found:\n[\n  {\n    \"id\": 1,\n    \"name\": \"Library Management System\",\n    \"category\": \"Web Development\",\n    \"hours\": 5\n  },\n  {\n    \"id\": 2,\n    \"name\": \"E-Commerce Website\",\n    \"category\": \"Web Development\",\n    \"hours\": 3\n  }\n]\n\nExample user input with no matches:\n'I worked on a Machine Learning model for 5 hours.'\n\nExpected response when no matches are found:\n\"No matches found\" . This is the project list which is in json format you have to choose from here"+projectsString;
            string userMeassage = speechText;
            var completion = await GetChatCompletion(systemMessage, userMeassage);
            return completion;
        }
    }
}
