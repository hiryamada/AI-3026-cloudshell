#pragma warning disable SKEXP0110

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Chat;

class ApprovalTerminationStragety : TerminationStrategy
{
    protected override async Task<bool> ShouldAgentTerminateAsync(
        Microsoft.SemanticKernel.Agents.Agent agent,
        IReadOnlyList<ChatMessageContent> history,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        var last = history.LastOrDefault();
        return last != null && last.Content != null && last.Content.EndsWith("no action needed.", StringComparison.OrdinalIgnoreCase);
    }
}
