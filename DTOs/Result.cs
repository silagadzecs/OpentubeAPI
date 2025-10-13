using Microsoft.AspNetCore.Mvc;

namespace OpentubeAPI.DTOs;

public class Result {
    public object? Value { get; private set; }
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    private Error[] Errors { get; } = [];

    public Result(object value, int statusCode = 200) {
        Success = true;
        StatusCode = statusCode;
        Value = value;
    }

    public Result(params Error[] errors) {
        Success = false;
        StatusCode = 400;
        Errors = errors;
    }
    public IActionResult ToActionResult() {
        if (Success) {
            return new ObjectResult(Value) {
                StatusCode = StatusCode
            };
        } else {
            var errors = Errors.ToDictionary(item => item.Name, item => item.Errors);
            return new ObjectResult(errors) {
                StatusCode = StatusCode
            };
        }
    }
}