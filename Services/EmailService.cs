using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace LandPortal.Api.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpAsync(string toEmail, string otp)
        {
            var host = _config["Email:SmtpHost"];
            var port = int.Parse(_config["Email:SmtpPort"]!);
            var from = _config["Email:From"];
            var appPassword = _config["Email:Password"]; // Gmail App Password

            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(appPassword))
                throw new Exception("Email configuration missing");

            using var client = new SmtpClient(host, port)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(from, appPassword),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 10000
            };

            var mail = new MailMessage
            {
                From = new MailAddress(from, "LandPortal"),
                Subject = "LandPortal OTP Verification",
                Body = $"Your OTP is: {otp}\n\nValid for 10 minutes.\n\nDo not share this code.",
                IsBodyHtml = false
            };

            mail.To.Add(toEmail);

            await client.SendMailAsync(mail);
        }
    }
}
