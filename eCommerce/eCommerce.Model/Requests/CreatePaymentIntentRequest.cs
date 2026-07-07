namespace eCommerce.Model.Requests;

public class CreatePaymentIntentRequest
{
    public List<CheckoutLineRequest> Items { get; set; } = new();
}
