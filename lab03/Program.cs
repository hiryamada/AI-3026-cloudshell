// Add references
using System.Text.Json;
using Azure.AI.Projects;
using Azure.Identity;
using dotenv.net;
using dotenv.net.Utilities;

class Program
{
    static async Task Main()
    {
        // Clear the console
        Console.Clear();

        // Load environment variables from .env file
        DotEnv.Load();
        var connectionString = EnvReader.GetStringValue("AZURE_AI_AGENT_PROJECT_CONNECTION_STRING");
        var deployName = EnvReader.GetStringValue("AZURE_AI_AGENT_MODEL_DEPLOYMENT_NAME");

        // Connect to the Azure AI Foundry project
        AgentsClient client = new(connectionString, new DefaultAzureCredential());

        // Define an agent that can use the custom functions
        var instructions = """
            You are a technical support agent.
            When a user has a technical issue, you get their email address and a description of the issue.
            Then you use those values to submit a support ticket using the function available to you.
            If a file is saved, tell the user the file name.
            """;
        Agent agent = await client.CreateAgentAsync(
            model: deployName,
            name: "support-agent",
            instructions: instructions,
            tools: [UserFunctions.SubmitSupportTicketTool]
        );
        Console.WriteLine($"You're chatting with: {agent.Name} ({agent.Id})");

        // Create a thread for the conversation
        AgentThread thread = await client.CreateThreadAsync();

        // Loop until the user types 'quit'
        while (true)
        {
            Console.Write("Enter a prompt (or type 'quit' to exit): ");
            string userPrompt = Console.ReadLine() ?? string.Empty;
            if (userPrompt.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
            {
                break;
            }
            if (userPrompt.Length == 0)
            {
                Console.WriteLine("Please enter a prompt.");
                continue;
            }

            // Send a prompt to the agent
            await client.CreateMessageAsync(
                thread.Id,
                MessageRole.User,
                content: userPrompt
            );

            // Check the run status for failures
            ThreadRun run = await client.CreateRunAsync(thread.Id, agent.Id);
            if (run.Status == RunStatus.Failed)
            {
                Console.WriteLine($"Run failed: {run.LastError}");
                continue;
            }
            do
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                run = await client.GetRunAsync(thread.Id, run.Id);
                if (run.Status == RunStatus.RequiresAction
                    && run.RequiredAction is SubmitToolOutputsAction submitToolOutputsAction)
                {
                    List<ToolOutput> toolOutputs = [];
                    foreach (RequiredToolCall toolCall in submitToolOutputsAction.ToolCalls)
                    {
                        toolOutputs.Add(await GetResolvedToolOutput(toolCall));
                    }
                    run = await client.SubmitToolOutputsToRunAsync(run, toolOutputs);
                }
            }
            while (run.Status == RunStatus.Queued
                || run.Status == RunStatus.InProgress);

            // Show the latest response from the agent
            (await client.GetMessagesAsync(thread.Id, run.Id)).Value
                .Reverse()
                .Where(msg => msg.Role == MessageRole.Agent)
                .SelectMany(msg => msg.ContentItems.OfType<MessageTextContent>()
                .Select(content => content.Text))
                .ToList().ForEach(Console.WriteLine);

        }

        // Get the conversation history
        Console.WriteLine("\nConversation Log:\n");

        var textItems = (await client.GetMessagesAsync(thread.Id)).Value
            .Reverse()
            .SelectMany(msg => msg.ContentItems.OfType<MessageTextContent>());
        textItems.Select(content => content.Text)
            .ToList().ForEach(Console.WriteLine);

        // Clean up
        await client.DeleteAgentAsync(agent.Id);
        await client.DeleteThreadAsync(thread.Id);
    }

    private static async Task<ToolOutput> GetResolvedToolOutput(RequiredToolCall toolCall)
    {
        if (toolCall is RequiredFunctionToolCall functionToolCall)
        {
            using JsonDocument argumentsJson = JsonDocument.Parse(functionToolCall.Arguments);
            if (nameof(UserFunctions.SubmitSupportTicket).Equals(functionToolCall.Name, StringComparison.OrdinalIgnoreCase))
            {
                string emailAddressArgument = argumentsJson.RootElement.GetProperty("emailAddress").GetString() ?? string.Empty;
                string descriptionArgument = argumentsJson.RootElement.GetProperty("description").GetString() ?? string.Empty;
                return new ToolOutput(functionToolCall, await UserFunctions.SubmitSupportTicket(emailAddressArgument, descriptionArgument));
            }
        }
        throw new NotImplementedException($"Tool {toolCall} is not implemented.");
    }
}
