using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Unanet_POC.Data;
using Unanet_POC.Models.Domain;
using Unanet_POC.Models.DTO;

namespace Unanet_POC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FeedbackController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackRequest feedback)
        {
            if (feedback == null || string.IsNullOrWhiteSpace(feedback.Username) || string.IsNullOrWhiteSpace(feedback.Comment))
            {
                return BadRequest(new { Message = "Username and Comment are required." });
            }

            var feedbackEntity = new Feedback
            {
                Username = feedback.Username,
                Comment = feedback.Comment,
                SubmittedAt = DateTime.UtcNow
            };

            _context.Feedbacks.Add(feedbackEntity);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Thank you for your feedback!",
                SubmittedBy = feedback.Username,
                Feedback = feedback.Comment
            });
        }
    }
}
