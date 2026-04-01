using FlowLens.Application.Features.Analysis.DTOs;
using MediatR;

namespace FlowLens.Application.Features.Analysis.Commands.AnalyzeRepo;

public record AnalyzeRepoCommand(string RepoUrl, string AccessToken) : IRequest<AnalysisReportDto>;