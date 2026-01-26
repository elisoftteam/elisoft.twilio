using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Elisoft.Notificator.Twilio.Services
{
    public class TwilioNotificator : ITwilioNotificator
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TwilioNotificator> _logger;

        private const string TwilioApiUrlFormat = "https://api.twilio.com/2010-04-01/Accounts/{0}/Messages.json";

        public TwilioNotificator(HttpClient httpClient, ILogger<TwilioNotificator> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> SendSmsAsync(string accountSid, string authToken, string fromNumber, string toNumber, string messageText)
        {
            if (string.IsNullOrWhiteSpace(accountSid)) throw new ArgumentNullException(nameof(accountSid));
            if (string.IsNullOrWhiteSpace(authToken)) throw new ArgumentNullException(nameof(authToken));

            var url = string.Format(TwilioApiUrlFormat, accountSid);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            var authenticationString = $"{accountSid}:{authToken}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("From", fromNumber),
                new KeyValuePair<string, string>("To", toNumber),
                new KeyValuePair<string, string>("Body", messageText)
            });

            try
            {
                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error Twilio Api ({StatusCode}): {Error}", response.StatusCode, error);
                    return false;
                }

                _logger.LogInformation("SMS sent successfully to {ToNumber}.", toNumber);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception when communicating with Twilio.");
                return false;
            }
        }
    }
}