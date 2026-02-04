using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Elisoft.Notificator.Twilio.Services
{
    public class TwilioNotificator : ITwilioNotificator
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TwilioNotificator> _logger;

        private const string TwilioApiUrlFormat = "https://api.twilio.com/2010-04-01/Accounts/{0}/Messages.json";

        private const int MaxMessageLength = 1600;
        private const int MaxPhoneNumberLength = 16;

        private const string PhoneNumberRegexPattern = @"^\+?[0-9]+$";

        public TwilioNotificator(HttpClient httpClient, ILogger<TwilioNotificator> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> SendSmsAsync(string accountSid, string authToken, string fromNumber, string toNumber, string messageText)
        {
            ValidateArguments(accountSid, authToken, fromNumber, toNumber, messageText);

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

        private void ValidateArguments(string accountSid, string authToken, string fromNumber, string toNumber, string messageText)
        {
            if (accountSid is null) throw new ArgumentNullException(nameof(accountSid));
            if (string.IsNullOrWhiteSpace(accountSid))
                throw new ArgumentException("Account SID cannot be empty.", nameof(accountSid));

            if (authToken is null) throw new ArgumentNullException(nameof(authToken));
            if (string.IsNullOrWhiteSpace(authToken))
                throw new ArgumentException("Auth Token cannot be empty.", nameof(authToken));

            if (fromNumber is null) throw new ArgumentNullException(nameof(fromNumber));
            if (string.IsNullOrWhiteSpace(fromNumber))
                throw new ArgumentException("From number cannot be empty.", nameof(fromNumber));

            if (fromNumber.Length > MaxPhoneNumberLength)
                throw new ArgumentException($"From number is too long. Max length is {MaxPhoneNumberLength}.", nameof(fromNumber));

            if (!Regex.IsMatch(fromNumber, PhoneNumberRegexPattern))
                throw new ArgumentException("From number contains invalid characters. Only digits and leading '+' are allowed (E.164 standard).", nameof(fromNumber));

            if (toNumber is null) throw new ArgumentNullException(nameof(toNumber));
            if (string.IsNullOrWhiteSpace(toNumber))
                throw new ArgumentException("To number cannot be empty.", nameof(toNumber));

            if (toNumber.Length > MaxPhoneNumberLength)
                throw new ArgumentException($"To number is too long. Max length is {MaxPhoneNumberLength}.", nameof(toNumber));

            if (!Regex.IsMatch(toNumber, PhoneNumberRegexPattern))
                throw new ArgumentException("To number contains invalid characters. Only digits and leading '+' are allowed.", nameof(toNumber));

            if (messageText is null) throw new ArgumentNullException(nameof(messageText));
            if (string.IsNullOrWhiteSpace(messageText))
                throw new ArgumentException("Message text cannot be empty.", nameof(messageText));

            if (messageText.Length > MaxMessageLength)
                throw new ArgumentException($"Message text exceeds the limit of {MaxMessageLength} characters.", nameof(messageText));
        }
    }
}