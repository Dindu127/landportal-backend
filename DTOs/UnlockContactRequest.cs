namespace LandPortal.Api.DTOs
{
    public class UnlockContactRequest
    {
        // Accept optional payment token/provider data
        public string? PaymentToken { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; } = "INR";
    }
}
