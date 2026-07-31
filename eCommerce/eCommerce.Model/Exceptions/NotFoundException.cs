namespace eCommerce.Model.Exceptions
{
    /// <summary>
    /// Resource was not found. The WebAPI <c>ExceptionFilter</c> maps this to HTTP 404.
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {
        }

        public NotFoundException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
