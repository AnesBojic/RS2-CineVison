using System.Threading.Tasks;
using CineVision.Model.Messages;

namespace CineVision.Services
{
    public interface IEmailService
    {
        /// <summary>Publishes an email message to the configured RabbitMQ queue for asynchronous delivery.</summary>
        Task QueueEmailAsync(EmailMessage message);
    }
}
