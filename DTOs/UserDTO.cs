using OpentubeAPI.Models;

namespace OpentubeAPI.DTOs;

public class UserDTO(User user) {
    public Guid Id { get; set; } = user.Id;
    public string DisplayName { get; set; } = user.DisplayName;
    public string Username { get; set; } = user.Username;
    public string? ProfilePictureUrl { get; set; } = user.ProfilePicture == string.Empty ? null : $"/cdn/images/{user.ProfilePicture}";
    public bool Verified { get; set; } = user.Verified;
    public string Role { get; set; } = user.Role.ToString();

    public static explicit operator UserDTO(User user) {
        return new UserDTO(user);
    }
}