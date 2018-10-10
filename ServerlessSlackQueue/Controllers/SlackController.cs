using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Amazon.SQS;
using Microsoft.Extensions.Options;
using ServerlessSlackQueue.Helpers;
using ServerlessSlackQueue.Models;

namespace ServerlessSlackQueue.Controllers
{
    [Route("api/[controller]")]
    public class SlackController : Controller
    {
        private readonly IAmazonSQS _sqs;
        private readonly ILogger _logger;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IVerifier _verifier;

        public SlackController(
            IVerifier verifier,
            IOptions<AppSettings> appSettings,
            ILogger<SlackController> logger, 
            IAmazonSQS sqs)
        {
            _appSettings = appSettings;
            _logger = logger;
            _sqs = new AmazonSQSClient();
            _verifier = verifier;
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult ReceiveCommand([FromForm] SlackSlashCommand form)
        {
            string body;
            using (var ms = new MemoryStream())
            {
                Request.Body.Seek(0, SeekOrigin.Begin);
                Request.Body.CopyTo(ms);
                body = System.Text.Encoding.Default.GetString(ms.ToArray());
            }

            var timestamp = Request.Headers["X-Slack-Request-Timestamp"];
            var signature = Request.Headers["X-Slack-Signature"];
            if (!_verifier.IsValid(timestamp, "v0", body, _appSettings.Value.SigningSecret, signature))
            {
                _logger.LogCritical($"Signature verification failed. {_verifier.ErrorMessage}");
                return Ok();
            }
            return Ok("Request received");
        }
    }
}