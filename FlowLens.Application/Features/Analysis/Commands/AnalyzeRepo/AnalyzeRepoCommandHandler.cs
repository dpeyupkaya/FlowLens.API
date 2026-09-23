using FlowLens.Application.Common.Interfaces;
using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Application.Interfaces;
using FlowLens.Application.Interfaces.External;
using FlowLens.Application.Interfaces.Infrastructure;
using FlowLens.Domain.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlowLens.Application.Features.Analysis.Commands.AnalyzeRepo
{
    public class AnalyzeRepoCommandHandler : IRequestHandler<AnalyzeRepoCommand, AnalysisReportDto>
    {
        private readonly IGitHubService _gitHubService;
        private readonly IEnumerable<IProjectAnalyzerStrategy> _analyzers;
        private readonly IAnalysisProgressService _progressService;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserService _currentUserService;

        private const int MAX_DAILY_ANALYSIS_LIMIT = 5;

        public AnalyzeRepoCommandHandler(
            IGitHubService gitHubService,
            IEnumerable<IProjectAnalyzerStrategy> analyzers,
            IAnalysisProgressService progressService,
            IUserRepository userRepository,
            ICurrentUserService currentUserService)
        {
            _gitHubService = gitHubService;
            _analyzers = analyzers;
            _progressService = progressService;
            _userRepository = userRepository;
            _currentUserService = currentUserService;
        }

        public async Task<AnalysisReportDto> Handle(AnalyzeRepoCommand request, CancellationToken cancellationToken)
        {
            await _progressService.NotifyAsync(request.AnalysisId, "Analiz motoru hazırlık süreci başladı.");

            var userIdString = _currentUserService.UserId;

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userGuid))
            {
                throw new UnauthorizedAccessException("Oturum bilgisi doğrulanamadı. Lütfen giriş yaptığınızdan emin olun.");
            }

            var user = await _userRepository.GetByIdAsync(userGuid);

            if (user.DailyAnalysisCount >= MAX_DAILY_ANALYSIS_LIMIT)
            {
                throw new InvalidOperationException($"Günlük analiz limitinize ({MAX_DAILY_ANALYSIS_LIMIT}/{MAX_DAILY_ANALYSIS_LIMIT}) ulaştınız. Limitleriniz gece 00:00'da sıfırlanacaktır.");
            }

            await _progressService.NotifyAsync(request.AnalysisId, "Depo erişim yetkileri doğrulanıyor...");
            var (isAccessible, isPrivate) = await _gitHubService.VerifyRepoAccessAsync(request.RepoUrl, user.GitHubAccessToken);

            if (!isAccessible)
            {
                throw new InvalidOperationException("Güvenlik İhlali: Bu depoya erişim sağlanamadı. Depo mevcut olmayabilir veya (Private ise) görüntüleme yetkiniz bulunmuyor olabilir. Lütfen geçerli ve yetkiniz olan bir bağlantı girin.");
            }
            if (isPrivate)
            {
                await _progressService.NotifyAsync(request.AnalysisId, "Özel (Private) depo algılandı. Güvenli analiz ortamı hazırlanıyor...");
            }

            await _progressService.NotifyAsync(request.AnalysisId, "Proje meta verileri ve dili tespit ediliyor...");
            var repoStats = await _gitHubService.GetRepoStatsAsync(request.RepoUrl, user.GitHubAccessToken);

            var targetLangs = request.TargetLanguages != null && request.TargetLanguages.Any() 
                ? request.TargetLanguages 
                : new List<string> { repoStats.PrimaryLanguage ?? "" };

            var activeStrategies = new List<IProjectAnalyzerStrategy>();
            var allExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var lang in targetLangs)
            {
                string mappedLanguage = lang.ToLower() switch
                {
                    "c#" or "csharp" => "CSharp",
                    "python" => "Python",
                    "go" => "Go",
                    "javascript" or "typescript" => "JavaScript",
                    "html" or "css" => "HTML_CSS",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(mappedLanguage))
                {
                    var strategy = _analyzers.FirstOrDefault(a => string.Equals(a.SupportedLanguage, mappedLanguage, StringComparison.OrdinalIgnoreCase));
                    if (strategy != null && !activeStrategies.Contains(strategy))
                    {
                        activeStrategies.Add(strategy);
                        var exts = mappedLanguage switch {
                            "Python" => new[] { "*.py" },
                            "Go" => new[] { "*.go" },
                            "JavaScript" => new[] { "*.js", "*.jsx", "*.ts", "*.tsx", "*.mjs", "*.cjs" },
                            "CSharp" => new[] { "*.cs" },
                            "HTML_CSS" => new[] { "*.html", "*.htm", "*.css", "*.scss", "*.sass", "*.less" },
                            _ => Array.Empty<string>()
                        };
                        foreach (var ext in exts) allExtensions.Add(ext);
                    }
                }
            }

            if (!activeStrategies.Any())
            {
                throw new InvalidOperationException("Seçilen diller için uygun analiz motoru bulunamadı.");
            }

            user.DailyAnalysisCount++;
            user.LastAnalysisDate = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            var workspaceId = Guid.NewGuid().ToString();
            var tempPath = Path.Combine(Path.GetTempPath(), "FlowLens", workspaceId);
            bool isAnalysisSuccessful = false;

            try
            {
                var dbSettings = user.Settings?.Analysis;

                var finalIgnoredFolders = request.IgnoredFolders != null && request.IgnoredFolders.Any()
                    ? request.IgnoredFolders
                    : dbSettings?.ExcludedFolders ?? new List<string>();

                var finalMaxDepth = request.MaxDepth ?? dbSettings?.MaxDepth ?? 3;

                Directory.CreateDirectory(tempPath);

                await _progressService.NotifyAsync(request.AnalysisId, "Kaynak kod deposu indirme ve dışa aktarma işlemi yürütülüyor.");
                await _gitHubService.DownloadAndExtractRepoAsync(request.RepoUrl, user.GitHubAccessToken, tempPath, cancellationToken);

                await _progressService.NotifyAsync(request.AnalysisId, "Dosya meta verileri ve kod metrikleri hesaplanıyor.");

                var allFiles = new List<string>();
                foreach (var ext in allExtensions)
                {
                    allFiles.AddRange(Directory.GetFiles(tempPath, ext, SearchOption.AllDirectories));
                }
                
                var targetFiles = allFiles.Distinct().Where(file =>
                {
                    var normalizedPath = file.Replace("\\", "/");
                    return !finalIgnoredFolders.Any(folder => normalizedPath.Contains($"/{folder}/", StringComparison.OrdinalIgnoreCase));
                }).ToArray();

                int totalLines = 0;
                foreach (var file in targetFiles)
                {
                    var lines = await File.ReadAllLinesAsync(file, cancellationToken);
                    totalLines += lines.Length;
                }

                await _progressService.NotifyAsync(request.AnalysisId, $"Statik tarama sonucunda {targetFiles.Length} dosya ve {totalLines} satır kod analiz kapsamına alındı.");

                var mergedNodes = new List<NodeDto>();
                var mergedEdges = new List<EdgeDto>();

                foreach (var strategy in activeStrategies)
                {
                    var graph = await strategy.AnalyzeStructureAsync(request.AnalysisId, tempPath, finalIgnoredFolders, finalMaxDepth, dbSettings);
                    mergedNodes.AddRange(graph.Nodes);
                    mergedEdges.AddRange(graph.Edges);
                }

                var distinctNodes = mergedNodes.DistinctBy(n => n.Id).ToList();
                var distinctEdges = mergedEdges.DistinctBy(e => new { e.Source, e.Target, e.RelationType }).ToList();
                var codeGraph = new CodeGraphDto(distinctNodes, distinctEdges);

                await _progressService.NotifyAsync(request.AnalysisId, "Proje yapısal analizi ve haritalama işlemi başarıyla tamamlandı.");

                isAnalysisSuccessful = true;

                return new AnalysisReportDto(
                    RepoUrl: request.RepoUrl,
                    TotalFilesScanned: targetFiles.Length,
                    TotalLinesOfCode: totalLines,
                    Graph: codeGraph,
                    Issues: new List<string>
                    {
                        "Analiz süreci yürütme hatası alınmadan tamamlanmıştır.",
                        $"Analiz kapsamı: {codeGraph.Nodes.Count} yapısal birim haritalandı. (Derinlik: {finalMaxDepth})",
                        $"Kalan Günlük Analiz Hakkı: {MAX_DAILY_ANALYSIS_LIMIT - user.DailyAnalysisCount}"
                    },
                    RepoStats: repoStats
                );
            }
            catch (Exception ex)
            {
                await _progressService.NotifyAsync(request.AnalysisId, $"[HATA] Analiz süreci başarısız oldu: {ex.Message}");
                throw;
            }
            finally
            {
                if (!isAnalysisSuccessful)
                {
                    user.DailyAnalysisCount--;
                    await _userRepository.UpdateAsync(user);
                    await _progressService.NotifyAsync(request.AnalysisId, "Analiz başarısız olduğu için hakkınız iade edildi.");
                }

                if (Directory.Exists(tempPath))
                {
                    await _progressService.NotifyAsync(request.AnalysisId, "Geçici çalışma dizini ve ilgili kaynaklar temizleniyor.");
                    Directory.Delete(tempPath, true);
                }
            }
        }
    }
}