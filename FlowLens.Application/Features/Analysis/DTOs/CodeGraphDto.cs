namespace FlowLens.Application.Features.Analysis.DTOs
{
    public record CodeGraphDto(List<NodeDto> Nodes, List<EdgeDto> Edges);

    public record NodeDto
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public string Type { get; init; }
        public int Size { get; init; }
        public Dictionary<string, string>? Metadata { get; set; }

        public NodeDto(string id, string name, string type, int size, Dictionary<string, string>? metadata = null)
        {
            Id = id;
            Name = name;
            Type = type;
            Size = size;
            Metadata = metadata;
        }
    }

    public record EdgeDto(string Source, string Target, string RelationType);
}