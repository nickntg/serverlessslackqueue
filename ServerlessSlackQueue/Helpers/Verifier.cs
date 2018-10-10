using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ServerlessSlackQueue.Helpers
{
    public interface IVerifier
    {
        string ErrorMessage { get; set; }
        bool IsValid(string timestamp, string versionNumber, string requestBody, string sharedSecret, string signature);
    }

    public class Verifier : IVerifier
    {
        public string ErrorMessage { get; set; }

        public bool IsValid(string timestamp, string versionNumber, string requestBody, string sharedSecret, string signature)
        {
            var time = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(Convert.ToInt64(timestamp));
            var difference = time.Subtract(DateTime.UtcNow);
            if (Math.Abs(difference.TotalSeconds) > 60 * 5)
            {
                ErrorMessage = "Clocks differ too much. Replay attack?";
                return false;
            }

            var key = Encoding.Default.GetBytes(sharedSecret);
            var input = Encoding.Default.GetBytes($"{versionNumber}:{timestamp}:{requestBody}");
            using (var crypto = new HMACSHA256(key))
            {
                var hash = $"{versionNumber}=" + crypto.ComputeHash(input).Select(b => b.ToString("x2")).Aggregate(new StringBuilder(),
                    (current, next) => current.Append(next), current => current.ToString());
                if (signature != hash)
                {
                    ErrorMessage = $"Calculated {hash} but was {signature}";
                }

                return signature == hash;
            }
        }
    }
}