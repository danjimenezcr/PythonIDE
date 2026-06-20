using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PyStudioDesktopSharp.Models;

namespace PyStudioDesktopSharp.Services;

public sealed class ApiException : Exception
{
    public ApiException(string message) : base(message) { }
}

public sealed class ApiClient
{
    private readonly HttpClient _http = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string BaseUrl { get; private set; } = "http://localhost:8000/api";
    public string? Token { get; private set; }
    public UserDto? User { get; private set; }

    public void SetBaseUrl(string baseUrl)
    {
        BaseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? "http://localhost:8000/api"
            : baseUrl.Trim().TrimEnd('/');
    }

    public async Task<UserDto> RegisterStudentAsync(string fullName, string email, string password)
    {
        var data = await RequestAsync<UserDto>(HttpMethod.Post, "/auth/register", new
        {
            full_name = fullName,
            email,
            password,
            role = "student"
        });

        return data;
    }

    public async Task<AuthData> LoginAsync(string email, string password)
    {
        var data = await RequestAsync<AuthData>(HttpMethod.Post, "/auth/login", new
        {
            email,
            password
        });

        Token = data.Token;
        User = data.User;
        return data;
    }

    public async Task<List<CourseDto>> GetCoursesAsync()
    {
        return await RequestAsync<List<CourseDto>>(HttpMethod.Get, "/courses");
    }

    public async Task<CourseDto> EnrollCourseAsync(string accessCode)
    {
        return await RequestAsync<CourseDto>(HttpMethod.Post, "/courses/enroll", new
        {
            access_code = accessCode
        });
    }

    public async Task<List<ActivityDto>> GetActivitiesAsync(int courseId)
    {
        return await RequestAsync<List<ActivityDto>>(HttpMethod.Get, $"/courses/{courseId}/activities");
    }

    public async Task<SubmissionData> SubmitScriptAsync(int activityId, string scriptPath)
    {
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException("No existe el script seleccionado.", scriptPath);

        string encoded = Convert.ToBase64String(await File.ReadAllBytesAsync(scriptPath));
        return await RequestAsync<SubmissionData>(HttpMethod.Post, "/submissions", new
        {
            activity_id = activityId,
            files = new[]
            {
                new
                {
                    file_name = Path.GetFileName(scriptPath),
                    file_content_base64 = encoded
                }
            }
        });
    }

    private async Task<T> RequestAsync<T>(HttpMethod method, string endpoint, object? body = null)
    {
        using var request = new HttpRequestMessage(method, BaseUrl + endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);

        if (body is not null)
            request.Content = JsonContent.Create(body, options: _jsonOptions);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception ex)
        {
            throw new ApiException($"No se pudo conectar al backend: {ex.Message}");
        }

        string raw = await response.Content.ReadAsStringAsync();
        ApiResponse<T>? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<ApiResponse<T>>(raw, _jsonOptions);
        }
        catch (JsonException)
        {
            throw new ApiException($"El backend respondió algo que no es JSON: {raw[..Math.Min(raw.Length, 160)]}");
        }

        if (!response.IsSuccessStatusCode || parsed is null || !parsed.Success)
        {
            string message = parsed?.Message ?? raw;
            throw new ApiException(string.IsNullOrWhiteSpace(message) ? "Error desconocido del backend." : message);
        }

        if (parsed.Data is null)
            throw new ApiException("La respuesta del backend no contiene data.");

        return parsed.Data;
    }
}
