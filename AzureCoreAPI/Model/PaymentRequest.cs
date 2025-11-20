namespace AzureCoreAPI.Model
{
    public class PaymentRequest
    {
        public string? id { get; set; } = Guid.NewGuid().ToString();
        public bool isSelected { get; set; }
        public string phoneNumber { get; set; }
        public int amount { get; set; }
        public string? validationMessage { get; set; }
    }
}
