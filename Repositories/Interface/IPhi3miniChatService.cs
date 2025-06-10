using OpenAI.Chat;
using System.Text.Json;
using Unanet_POC.Models.DTO;

namespace Unanet_POC.Repositories.Interface
{
    public interface IPhi3miniChatService
    {
        Task<chatResponse> UnifiedChatbotHandler(string userInput, JsonElement swaggerJson);
        Task<List<string>> generateActionList(JsonElement jsonElement);
        Task<string> GenerateTextFromImageUrl(string imageUrl);
        void SetQueryParameters(QueryParamsDto dto);
        void setHeaderParamValues(HeaderParamsDto dto);
        void setURLPath(string url);

    }
}
