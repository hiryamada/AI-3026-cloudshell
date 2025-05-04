#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0110

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

class LabSelectionStragety : SelectionStrategy
{
    protected override async Task<Microsoft.SemanticKernel.Agents.Agent> SelectAgentAsync(
        IReadOnlyList<Microsoft.SemanticKernel.Agents.Agent> agents,
        IReadOnlyList<ChatMessageContent> history,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        ChatMessageContent last = history.Last();
        string agentName = (last.AuthorName == "DEVOPS_ASSISTANT" || last.Role == AuthorRole.User)
            ? "INCIDENT_MANAGER"
            : "DEVOPS_ASSISTANT";
        return agents.First(a => a.Name == agentName);
    }
}
