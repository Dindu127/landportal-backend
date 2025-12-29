using System.Net.Http;

namespace LandPortal.Api.Services
{
    public class SmsService
    {
        private readonly IConfiguration _config;

        public SmsService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpAsync(string phone, string otp)
        {
            var apiKey = _config["Fast2Sms:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("Fast2SMS API key missing");

            var message = $"Your OTP is {otp}. Valid for 10 minutes.";

            var url =
                $"https://www.fast2sms.com/dev/bulkV2" +
                $"?route=transactional" +
                $"&message={Uri.EscapeDataString(message)}" +
                $"&numbers={phone}";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Clear();

            // ⚠️ MUST BE LOWERCASE AND RAW KEY
            client.DefaultRequestHeaders.Add("authorization", apiKey);

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"SMS failed: {content}");
            }
        }
    }
}
