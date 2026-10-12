using System.Text.Json.Serialization;

namespace MediaServer;

public class RegisterNewUser
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";
    [JsonPropertyName("password")]
    public string Password { get; set; } = "";
}