using System.Text.Json.Serialization;

namespace MediaServer;

public class CredUser
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";
    [JsonPropertyName("password")]
    public string Password { get; set; } = "";
}