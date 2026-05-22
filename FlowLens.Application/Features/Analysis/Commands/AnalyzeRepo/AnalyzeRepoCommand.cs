using FlowLens.Application.Features.Analysis.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace FlowLens.Application.Features.Analysis.Commands.AnalyzeRepo;

public record AnalyzeRepoCommand(
    string RepoUrl,
    List<string> IgnoredFolders,
    int? MaxDepth,
    string AnalysisId,
    int TimezoneOffsetMinutes = 0 
) : IRequest<AnalysisReportDto>;