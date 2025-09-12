using Microsoft.AspNetCore.Mvc;

namespace OpentubeAPI.DTOs;

public class Result {
    private object? Value { get; }
    private bool Success { get; }
    public int StatusCode { get; set; }
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