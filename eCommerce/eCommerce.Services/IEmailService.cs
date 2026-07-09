using System.Threading.Tasks;
using eCommerce.Model.Messages;

namespace eCommerce.Services
{
    public interface IEmailService
    {
        /// <summary>Publishes an email message to the configured RabbitMQ queue for asynchronous delivery.</summary>
        Task QueueEmailAsync(EmailMessage message);
    }
}
