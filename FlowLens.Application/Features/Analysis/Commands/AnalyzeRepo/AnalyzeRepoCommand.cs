using FlowLens.Application.Features.Analysis.DTOs;
using MediatR;

namespace FlowLens.Application.Features.Analysis.Commands.AnalyzeRepo;

public record AnalyzeRepoCommand(string RepoUrl, Guid UserId) : IRequest<AnalysisReportDto>;