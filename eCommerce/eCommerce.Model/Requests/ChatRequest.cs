using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class ChatRequest
    {
        [Required]
        [MaxLength(4000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Optional prior turns so the assistant can follow the conversation.
        /// Only user/assistant roles are expected from the client; the server adds system context.
        /// </summary>
        public List<ChatMessageDto>? History { get; set; }
    }

    public class ChatMessageDto
    {
        /// <summary>"user" or "assistant".</summary>
        public string Role { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }
}
