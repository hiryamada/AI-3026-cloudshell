
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

        // Display the data to be analyzed
        var data = File.ReadAllText("data.txt");
        Console.WriteLine(data);

        // Connect to the Azure AI Foundry project
        AgentsClient client = new(connectionString, new DefaultAzureCredential());

        // Upload the data file and create a CodeInterpreterTool
        AgentFile file = await client.UploadFileAsync(
            filePath: "data.txt",
            purpose: AgentFilePurpose.Agents
        );
        CodeInterpreterToolResource codeInterpreter = new();
        codeInterpreter.FileIds.Add(file.Id);

        // Define an agent that uses the CodeInterpreterTool
        Agent agent = await client.CreateAgentAsync(
            model: deployName,
            name: "data-agent",
            instructions: "You are an AI agent that analyzes the data in the file that has been uploaded. If the user requests a chart, create it and save it as a .png file.",
            tools: [new CodeInterpreterToolDefinition()],
            toolResources: new ToolResources() { CodeInterpreter = codeInterpreter }
        );
        Console.WriteLine($"Using agent: {agent.Name}");

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
            var attachment = new MessageAttachment(
                fileId: file.Id, 
                tools: [new CodeInterpreterToolDefinition()]
            );
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

        // Get any generated files
        var fileIds = textItems
            .SelectMany(item => item.Annotations.OfType<MessageTextFilePathAnnotation>())
            .Select(annotation => annotation.FileId);
        foreach (var fileId in fileIds)
        {
            AgentFile chartFile = await client.GetFileAsync(fileId);
            BinaryData fileContent = await client.GetFileContentAsync(fileId);
            string fileName = Path.GetFileName(chartFile.Filename);
            await File.WriteAllBytesAsync(fileName, fileContent.ToArray());
            Console.WriteLine($"File saved as {fileName}");
        }

        // Clean up
        await client.DeleteAgentAsync(agent.Id);
        await client.DeleteThreadAsync(thread.Id);
    }
}
