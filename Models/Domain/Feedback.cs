namespace Unanet_POC.Models.Domain
{
    public class Feedback
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Comment { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}