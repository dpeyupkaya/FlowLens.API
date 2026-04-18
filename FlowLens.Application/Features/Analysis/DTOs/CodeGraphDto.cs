using System.Collections.Generic;

namespace FlowLens.Application.Features.Analysis.DTOs
{
    public record CodeGraphDto(List<NodeDto> Nodes, List<EdgeDto> Edges);

    public record NodeDto
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public string Type { get; init; }
        public int Size { get; init; }

        public Dictionary<string, object> Metadata { get; set; } = new();

        public NodeDto(string id, string name, string type, int size, Dictionary<string, object>? metadata = null)
        {
            Id = id;
            Name = name;
            Type = type;
            Size = size;
            if (metadata != null) Metadata = metadata;
        }
    }

    public record EdgeDto(string Source, string Target, string RelationType);

    public record MethodInfoDto(string Name, string ReturnType, List<string> Parameters, string AccessModifier);
    public record PropertyInfoDto(string Name, string Type, string AccessModifier);
}