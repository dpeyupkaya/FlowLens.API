using FlowLens.Application.Features.Analysis.DTOs;
using MediatR;
using System;
using System.Collections.Generic; 

namespace FlowLens.Application.Features.Analysis.Commands.AnalyzeRepo;

public record AnalyzeRepoCommand(
    string RepoUrl,
    Guid UserId,
    List<string> IgnoredFolders,
    int? MaxDepth
) : IRequest<AnalysisReportDto>;