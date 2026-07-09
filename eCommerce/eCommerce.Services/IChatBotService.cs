using eCommerce.Model.Requests;
using eCommerce.Model.Responses;

namespace eCommerce.Services
{
    public interface IChatBotService
    {
        /// <summary>
        /// Sends the user message (and optional history) to OpenAI with a live cinema-data context snapshot.
        /// </summary>
        Task<ChatResponse> ChatAsync(ChatRequest request, string userRole);
    }
}
