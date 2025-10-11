namespace OpentubeAPI.DTOs;

public class Error(string name, params string[] errors) {
    public string Name { get; init; } = name;
    public string[] Errors { get; init; } = errors;
}