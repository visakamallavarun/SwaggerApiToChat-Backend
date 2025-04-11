using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using Unanet_POC.Repositories.Interface;

namespace Unanet_POC.Repositories.Implementation
{
    public class ConvertSpeechToTextRepository : IConvertSpeechToTextRepository
    {
        private readonly string _speechKey;
        private readonly string _speechRegion;

        public ConvertSpeechToTextRepository(IConfiguration configuration)
        {
            _speechKey = configuration["AzureAI:SpeechApiKey"];
            _speechRegion = configuration["AzureAI:Region"];
        }
        public async Task<string> ConvertSpeechToText(string filePath)
        {
            var config = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
            using var audioConfig = AudioConfig.FromWavFileInput(filePath);
            using var recognizer = new SpeechRecognizer(config, audioConfig);

            var result = await recognizer.RecognizeOnceAsync();

            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                return result.Text;
            }
            else if (result.Reason == ResultReason.NoMatch)
            {
                return "No speech could be recognized.";
            }
            else
            {
                return $"Speech recognition failed: {result.Reason}";
            }
        }
    }
}
