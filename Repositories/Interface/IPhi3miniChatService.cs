using OpenAI.Chat;
using System.Text.Json;
using Unanet_POC.Models.DTO;

namespace Unanet_POC.Repositories.Interface
{
    public interface IPhi3miniChatService
    {
        Task<string> UnifiedChatbotHandler(string userInput, JsonElement swaggerJson);
    }
}
