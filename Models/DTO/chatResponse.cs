namespace Unanet_POC.Models.DTO
{
    public class chatResponse
    {
        public string response { get; set; }
        public string? speachResponse { get; set; }
        public string? debugerResponse { get; set; }

        public chatResponse(string response, string? speachResponse =null, string? debugerResponse = null)
        {
            this.response = response;
            this.speachResponse = speachResponse;
            this.debugerResponse = debugerResponse;
        }
    }
}
