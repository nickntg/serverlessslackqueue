using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
            _sqs = sqs;
            _verifier = verifier;
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> ReceiveCommand([FromForm] SlackSlashCommand form)
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

            var command = form.Command.Substring(1);

            var sendRequest = new SendMessageRequest
            {
                QueueUrl = $"{_appSettings.Value.SqsUrlPrefix}slack_{command}",
                MessageBody = JsonConvert.SerializeObject(form)
            };

            try
            {
                await _sqs.SendMessageAsync(sendRequest);
                return Ok("Request received");
            }
            catch (AmazonSQSException e)
            {
                if (e.ErrorCode == "AWS.SimpleQueueService.NonExistentQueue")
                {
                    // Queue does not exist, let's create it.
                    var createRequest = new CreateQueueRequest($"slack_{command}")
                    {
                        Attributes =
                        {
                            ["MessageRetentionPeriod"] = _appSettings.Value.NewQueueMessageRetentionPeriod.ToString(),
                            ["VisibilityTimeout"] = _appSettings.Value.NewQueueVisibilityTimeout.ToString()
                        }
                    };
                    await _sqs.CreateQueueAsync(createRequest);
                    return Ok($"Command [{command}] was used for the first time and you caught me setting up the pipes. Please send it again.");
                }

                _logger.LogError(e.ToString());
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw;
            }           
        }
    }
}