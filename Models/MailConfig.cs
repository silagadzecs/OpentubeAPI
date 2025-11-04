namespace OpentubeAPI.Models;

public class MailConfig {
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string SMTPServer { get; init; }
    public int SMTPPort { get; init; }
}