using System.Threading.Tasks;

namespace Elisoft.Notificator.Twilio.Services
{
    public interface ITwilioNotificator
    {
        Task<bool> SendSmsAsync(string accountSid, string authToken, string fromNumber, string toNumber, string messageText);
    }
}