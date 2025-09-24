using RecallAI.Api.Models.Completion;

namespace RecallAI.Api.Interfaces;

public interface ICompletionPipelineService
{
    IAsyncEnumerable<string> ProcessCompletionAsync(CompletionRequest request, Guid userId, CancellationToken cancellationToken = default);
}