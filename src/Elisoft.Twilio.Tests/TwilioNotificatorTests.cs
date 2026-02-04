using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Elisoft.Notificator.Twilio.Services;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Shouldly;

namespace Elisoft.Notificator.Twilio.Tests
{
    [TestFixture]
    public class TwilioNotificatorTests
    {
        private Fixture _fixture;
        private ILogger<TwilioNotificator> _logger;

        [SetUp]
        public void SetUp()
        {
            _fixture = new Fixture();
            _logger = A.Fake<ILogger<TwilioNotificator>>();
        }

        [Test]
        public async Task SendSmsAsync_AccountSidIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            var httpClient = CreateHttpClient(HttpStatusCode.OK);
            var sut = new TwilioNotificator(httpClient, _logger);
            var authToken = _fixture.Create<string>();

            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(async () =>
                await sut.SendSmsAsync(null!, authToken, "+1234567890", "+1987654321", "msg"));
        }

        [Test]
        public async Task SendSmsAsync_AuthTokenIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            var httpClient = CreateHttpClient(HttpStatusCode.OK);
            var sut = new TwilioNotificator(httpClient, _logger);
            var accountSid = _fixture.Create<string>();

            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(async () =>
                await sut.SendSmsAsync(accountSid, null!, "+1234567890", "+1987654321", "msg"));
        }

        [Test]
        public async Task SendSmsAsync_ResponseIsSuccess_ReturnsTrue()
        {
            // Arrange
            var httpClient = CreateHttpClient(HttpStatusCode.OK);
            var sut = new TwilioNotificator(httpClient, _logger);

            var accountSid = _fixture.Create<string>();
            var authToken = _fixture.Create<string>();
            var from = "+15005550006"; 
            var to = "+15005550007";   
            var msg = _fixture.Create<string>();

            // Act
            var result = await sut.SendSmsAsync(accountSid, authToken, from, to, msg);

            // Assert
            result.ShouldBeTrue();
        }

        [Test]
        public async Task SendSmsAsync_ResponseIsFailure_ReturnsFalse()
        {
            // Arrange
            var httpClient = CreateHttpClient(HttpStatusCode.BadRequest);
            var sut = new TwilioNotificator(httpClient, _logger);

            var accountSid = _fixture.Create<string>();
            var authToken = _fixture.Create<string>();
            var from = "+15005550006"; 
            var to = "+15005550007";   
            var msg = _fixture.Create<string>();

            // Act
            var result = await sut.SendSmsAsync(accountSid, authToken, from, to, msg);

            // Assert
            result.ShouldBeFalse();
        }

        [Test]
        public async Task SendSmsAsync_HttpClientThrowsException_ReturnsFalse()
        {
            // Arrange
            var handler = A.Fake<HttpMessageHandler>();

            A.CallTo(handler)
                .Where(call => call.Method.Name == "SendAsync")
                .WithReturnType<Task<HttpResponseMessage>>()
                .ThrowsAsync(new HttpRequestException());

            var httpClient = new HttpClient(handler);
            var sut = new TwilioNotificator(httpClient, _logger);

            var accountSid = _fixture.Create<string>();
            var authToken = _fixture.Create<string>();
            var from = "+15005550006"; 
            var to = "+15005550007";   
            var msg = _fixture.Create<string>();

            // Act
            var result = await sut.SendSmsAsync(accountSid, authToken, from, to, msg);

            // Assert
            result.ShouldBeFalse();
        }

        private static HttpClient CreateHttpClient(HttpStatusCode statusCode)
        {
            var handler = A.Fake<HttpMessageHandler>();

            A.CallTo(handler)
                .Where(call => call.Method.Name == "SendAsync")
                .WithReturnType<Task<HttpResponseMessage>>()
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent("{\"status\": \"mock_response\"}", Encoding.UTF8, "application/json")
                }));

            return new HttpClient(handler);
        }
    }
}