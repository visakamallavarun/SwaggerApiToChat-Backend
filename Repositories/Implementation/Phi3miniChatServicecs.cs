using Azure;
using Unanet_POC.Repositories.Interface;
using System.Text.Json;
using Unanet_POC.DTO;
using Azure.AI.Inference;

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
            string exampleText = @"
                    📝 Example User Inputs & Expected Outputs:
                    ✅ Case 1: Matches Found (Multiple Projects)
                    🔹 User Input:""I worked on the Library Management System for 5 hours and spent 3 hours on the E-Commerce Website.""
                    ✅ Expected JSON Output:
                    [
                        {
                        ""id"": 1,
                        ""name"": ""Library Management System"",
                        ""category"": ""Web Development"",
                        ""hours"": 5
                        },
                        {
                        ""id"": 2,
                        ""name"": ""E-Commerce Website"",
                        ""category"": ""Web Development"",
                        ""hours"": 3
                        }
                    ]
                    ✅ Case 2: Matches Found (Single Project)
                    🔹 User Input:
                    ""Today, I spent 6 hours working on the AI Chatbot.""
                    ✅ Expected JSON Output:
                    [
                        {
                        ""id"": 5,
                        ""name"": ""AI Chatbot"",
                        ""category"": ""Artificial Intelligence"",
                        ""hours"": 6
                        }
                    ]
                    ❌ Case 3: No Matches Found
                    🔹 User Input:
                    ""I worked on a Machine Learning model for 4 hours.""
                    ✅ Expected JSON Output:
                    []
                    ✅ Case 4: Partial Matches (Only Recognized Projects Are Included)
                    🔹 User Input:
                    ""I spent 7 hours on the Mobile Banking App and 5 hours on a Data Science project.""
                    ✅ Expected JSON Output:
                    [
                        {
                        ""id"": 3,
                        ""name"": ""Mobile Banking App"",
                        ""category"": ""Mobile Development"",
                        ""hours"": 7
                        }
                    ]
                    ✅ Case 5: Different Wording for the Same Projects
                    🔹 User Input:
                    ""Worked on the Inventory Management System for about 8 hours today.""
                    ✅ Expected JSON Output:
                    [
                        {
                        ""id"": 4,
                        ""name"": ""Inventory Management System"",
                        ""category"": ""Software Development"",
                        ""hours"": 8
                        }
                    ]
                    ✅ Case 6: Projects Mentioned Without Hours
                    🔹 User Input:
                    ""I was involved in the Library Management System and the AI Chatbot today.""
                    ✅ Expected JSON Output:
                    []
                    (Since no hours are mentioned, return an empty list.)
                    ✅ Case 7: Hours Given Before Project Names
                    🔹 User Input:
                    ""Spent 5 hours today on the E-Commerce Website and 3 hours on the AI Chatbot.""
                    ✅ Expected JSON Output:
                    [
                        {
                        ""id"": 2,
                        ""name"": ""E-Commerce Website"",
                        ""category"": ""Web Development"",
                        ""hours"": 5
                        },
                        {
                        ""id"": 5,
                        ""name"": ""AI Chatbot"",
                        ""category"": ""Artificial Intelligence"",
                        ""hours"": 3
                        }
                    ] 
                    ";
            String systemMessage = @"
                    You are an AI assistant that extracts multiple projects from a user's sentence and assigns the specific hours mentioned for each project.  
                    ### Instructions:  
                    - You will receive a **predefined JSON list of projects**.  
                    - Match the mentioned projects from the list and extract the corresponding **hours**.  
                    - If multiple projects are mentioned, return all matched projects with their **hours**.  
                    - If no projects from the list match, return an **empty JSON list (`[]`)**.  
                    - The response must **always** be in **JSON format**.  
                    ### Project List (Use This for Matching):  
                    ```json
                    "+projectsString+"```"+
                    @"### Example Text:"+
                    exampleText;
            

            string userMeassage = speechText;
            var completion = await GetChatCompletion(systemMessage, userMeassage);
            return completion;
        }
    }
}
