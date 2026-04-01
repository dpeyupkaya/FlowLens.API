using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowLens.Application.Features.Analysis.DTOs
{
    public record CodeGraphDto(
     List<NodeDto> Nodes, 
     List<EdgeDto> Edges
 );

    public record NodeDto(string Id, string Name, string Type, int Size);
    public record EdgeDto(string Source, string Target, string RelationType);
}
