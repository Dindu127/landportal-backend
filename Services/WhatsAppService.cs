using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class WhatsAppService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    public WhatsAppService(IConfiguration config, HttpClient http)
    {
        _config = config;
        _http = http;
    }

    public async Task SendOtpAsync(string phone, string otp)
    {
        var token = _config["WhatsApp:AccessToken"];
        var phoneId = _config["WhatsApp:PhoneNumberId"];

        var url = $"https://graph.facebook.com/v19.0/{phoneId}/messages";

        var body = new
        {
            messaging_product = "whatsapp",
            to = phone, // 91XXXXXXXXXX (no +)
            type = "template",
            template = new
            {
                name = "otp_login",
                language = new { code = "en" },
                components = new[]
                {
                new
                {
                    type = "body",
                    parameters = new[]
                    {
                        new { type = "text", text = otp }
                    }
                }
            }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _http.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"WhatsApp error: {result}");
    }


}
