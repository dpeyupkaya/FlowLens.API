using System.Text.Json.Serialization;

namespace FlowLens.Application.Interfaces.External
{
    public record GitHubRepoResponse(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("html_url")] string HtmlUrl,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("language")] string? Language
    );
}