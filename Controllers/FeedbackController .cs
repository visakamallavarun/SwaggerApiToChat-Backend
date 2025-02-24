using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Unanet_POC.DTO;

namespace Unanet_POC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        [HttpPost]
        public IActionResult SubmitFeedback([FromBody] FeedbackRequest feedback)
        {
            if (feedback == null || string.IsNullOrWhiteSpace(feedback.Username) || string.IsNullOrWhiteSpace(feedback.Comment))
            {
                return BadRequest(new { Message = "Username and Comment are required." });
            }

            return Ok(new
            {
                Message = "Thank you for your feedback!",
                SubmittedBy = feedback.Username,
                Feedback = feedback.Comment
            });
        }
    }
}
