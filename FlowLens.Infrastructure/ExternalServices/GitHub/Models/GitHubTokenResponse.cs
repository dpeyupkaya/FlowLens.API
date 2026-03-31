using System.Text.Json.Serialization;


namespace FlowLens.Infrastructure.ExternalServices.GitHub.Models
{
    public record GitHubTokenResponse(
       [property: JsonPropertyName("access_token")] string AccessToken,
       [property: JsonPropertyName("token_type")] string TokenType,
       [property: JsonPropertyName("scope")] string Scope
   );
}
