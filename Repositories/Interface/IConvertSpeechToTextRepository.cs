using System.Threading.Tasks;

namespace Unanet_POC.Repositories.Interface
{
    public interface IConvertSpeechToTextRepository
    {
        Task<string> ConvertSpeechToText(string filePath);
    }
}
