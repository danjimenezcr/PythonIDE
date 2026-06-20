using System.Text.Json.Serialization;

namespace PyStudioDesktopSharp.Models;

public sealed class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public sealed class AuthData
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("user")]
    public UserDto? User { get; set; }
}

public sealed class UserDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }
}

public sealed class CourseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("access_code")]
    public string? AccessCode { get; set; }
}

public sealed class ActivityDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("course_id")]
    public int CourseId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("deadline")]
    public string? Deadline { get; set; }
}

public sealed class SubmissionData
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("submission_id")]
    public int SubmissionId { get; set; }

    [JsonPropertyName("submitted_at")]
    public string? SubmittedAt { get; set; }
}
