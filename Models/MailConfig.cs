namespace OpentubeAPI.Models;

public class MailConfig {
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string SMTPServer { get; set; }
    public int SMTPPort { get; set; }
}