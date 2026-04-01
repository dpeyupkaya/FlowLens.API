using FlowLens.Application.Interfaces.External;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowLens.Application.Features.GitHub.Queries.GetCSharpRepos
{
    public class GetCSharpReposQuery : IRequest<List<GitHubRepoResponse>>
    {
        public string AccessToken { get; set; }

        public GetCSharpReposQuery(string accessToken)
        {
            AccessToken = accessToken;
        }
    }
}
