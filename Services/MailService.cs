using System.Net;
using System.Net.Mail;
using System.Text;
using OpentubeAPI.Models;

namespace OpentubeAPI.Services;

public class MailService(MailConfig config) {
    public async Task SendCode(string code, string recipient) {
        var client = new SmtpClient(config.SMTPServer, config.SMTPPort);
        var creds = new NetworkCredential(config.Username, config.Password);
        client.UseDefaultCredentials = false;
        client.EnableSsl = true;
        client.DeliveryMethod = SmtpDeliveryMethod.Network;
        client.Credentials = creds;
        var from = new MailAddress(config.Username, "no-reply");
        var to = new MailAddress(recipient);
        var message = new MailMessage(from, to);
        message.IsBodyHtml = true;
        message.BodyEncoding = Encoding.UTF8;
        message.Subject = "Your Opentube verification code";
        message.Body = $"<h2>Your code is</h2>\r\n" +
                       $"<h1>{code}</h1>\r\n" +
                       $"<h3>Expires in 1 hour</h3>";
        await client.SendMailAsync(message);
    }
}