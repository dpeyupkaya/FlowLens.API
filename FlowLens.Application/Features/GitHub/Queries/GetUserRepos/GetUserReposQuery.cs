using FlowLens.Application.Interfaces.External;
using MediatR;
using System;
using System.Collections.Generic;

namespace FlowLens.Application.Features.GitHub.Queries.GetUserRepos;

public class GetUserReposQuery : IRequest<List<GitHubRepoResponse>>
{
    public Guid UserId { get; set; }
    public string Language { get; set; }

    public GetUserReposQuery(Guid userId, string language = "All")
    {
        UserId = userId;
        Language = language;
    }
}