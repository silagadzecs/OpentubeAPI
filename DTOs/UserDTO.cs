using OpentubeAPI.Models;

namespace OpentubeAPI.DTOs;

public class UserDTO {
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
    public string Username { get; set; }
    public string ProfilePictureUrl { get; set; }
    public bool Verified { get; set; }
    public string Role { get; set; }

    public UserDTO(User user) {
        Id = user.Id;
        DisplayName = user.DisplayName;
        Username = user.Username;
        Role = user.Role.ToString();
        Verified = user.Verified;
        
    }

    public static explicit operator UserDTO(User user) {
        return new UserDTO(user);
    }
}