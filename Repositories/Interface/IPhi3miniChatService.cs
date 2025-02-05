using OpenAI.Chat;

namespace Unanet_POC.Repositories.Interface
{
    public interface IPhi3miniChatService
    {
        Task<String> selectProject(string speechText);
        Task<string> GetChatCompletion(string systemMessage, string userMessage);
    }
}
