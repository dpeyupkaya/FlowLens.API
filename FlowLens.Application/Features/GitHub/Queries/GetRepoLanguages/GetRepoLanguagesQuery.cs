using MediatR;
using System.Collections.Generic;

namespace FlowLens.Application.Features.GitHub.Queries.GetRepoLanguages;

public record GetRepoLanguagesQuery(string RepoUrl) : IRequest<Dictionary<string, int>>;
