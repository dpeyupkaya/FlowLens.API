using FlowLens.Application.Interfaces.External;
using MediatR;
using System;
using System.Collections.Generic;

namespace FlowLens.Application.Features.GitHub.Queries.GetCSharpRepos;

public class GetCSharpReposQuery : IRequest<List<GitHubRepoResponse>>
{
    public Guid UserId { get; set; }

    public GetCSharpReposQuery(Guid userId)
    {
        UserId = userId;
    }
}