using System.Text.Json.Serialization;


namespace FlowLens.Application.Interfaces.External
{
    public record GitHubUserResponse(
       [property: JsonPropertyName("id")] long Id,
       [property: JsonPropertyName("login")] string Login,
       [property: JsonPropertyName("avatar_url")] string AvatarUrl,
       [property: JsonPropertyName("email")] string? Email,
       [property: JsonPropertyName("name")] string? Name
   );
}
