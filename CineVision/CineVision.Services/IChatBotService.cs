using CineVision.Model.Requests;
using CineVision.Model.Responses;

namespace CineVision.Services
{
    public interface IChatBotService
    {
        /// <summary>
        /// Sends the user message (and optional history) to OpenAI with a live cinema-data context snapshot.
        /// </summary>
        Task<ChatResponse> ChatAsync(ChatRequest request, string userRole);
    }
}
