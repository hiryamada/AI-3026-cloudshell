#pragma warning disable SKEXP0110

// Add references
using dotenv.net;
using dotenv.net.Utilities;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Azure.AI.Projects;
using Azure.Identity;

class Program
{
    static async Task Main()
    {
        // Clear the console
        Console.Clear();

        // Get the log files
        Directory.CreateDirectory("logs");
        foreach (var logFile in Directory.EnumerateFiles("sample_logs", "*"))
        {
            File.Copy(logFile, Path.Combine("logs", Path.GetFileName(logFile)), true);
        }

        // Get the Azure AI Agent settings
        DotEnv.Load();
        var connectionString = EnvReader.GetStringValue("AZURE_AI_AGENT_PROJECT_CONNECTION_STRING");
        var deployName = EnvReader.GetStringValue("AZURE_AI_AGENT_MODEL_DEPLOYMENT_NAME");

        AIProjectClient client = AzureAIAgent.CreateAzureAIClient(connectionString, new DefaultAzureCredential());
        AgentsClient agentsClient = client.GetAgentsClient();

        // Create the incident manager agent on the Azure AI agent service
        var incidentManagerInstructions = """
            Analyze the given log file or the response from the devops assistant.
            Recommend which one of the following actions should be taken:

            Restart service {service_name}
            Rollback transaction
            Redeploy resource {resource_name}
            Increase quota

            If there are no issues or if the issue has already been resolved, respond with "INCIDENT_MANAGER > No action needed."
            If none of the options resolve the issue, respond with "Escalate issue."

            RULES:
            - Do not perform any corrective actions yourself.
            - Read the log file on every turn.
            - Prepend your response with this text: "INCIDENT_MANAGER > {logfilepath} | "
            - Only respond with the corrective action instructions.
            """;
        Azure.AI.Projects.Agent azureAIProjectIncidentManagerAgent = agentsClient.CreateAgent(
            model: deployName,
            name: "INCIDENT_MANAGER",
            instructions: incidentManagerInstructions
        );

        // Create a Semantic Kernel agent for the Azure AI incident manager agent
        var logFilePlugin = KernelPluginFactory.CreateFromType<LogFilePlugin>();
        AzureAIAgent incidentManagerAgent = new(azureAIProjectIncidentManagerAgent, agentsClient, plugins: [logFilePlugin]);

        // Create the devops agent on the Azure AI agent service
        var devopsManagerInstructions = """
            Read the instructions from the INCIDENT_MANAGER and apply the appropriate resolution function. 
            Return the response as "{function_response}"
            If the instructions indicate there are no issues or actions needed, 
            take no action and respond with "No action needed."

            RULES:
            - Use the instructions provided.
            - Do not read any log files yourself.
            - Prepend your response with this text: "DEVOPS_ASSISTANT > "
            """;

        Azure.AI.Projects.Agent azureAIProjectDevopsManagerAgent = agentsClient.CreateAgent(
            model: deployName,
            name: "DEVOPS_ASSISTANT",
            instructions: devopsManagerInstructions
        );

        // Create a Semantic Kernel agent for the devops Azure AI agent
        var devopsPlugin = KernelPluginFactory.CreateFromType<DevopsPlugin>();
        AzureAIAgent devopsManagerAgent = new(azureAIProjectDevopsManagerAgent, agentsClient, plugins: [devopsPlugin]);

        // Process log files
        foreach (var logFile in Directory.EnumerateFiles("logs", "*").Order())
        {

            // Add the agents to a group chat with a custom termination and selection strategy
            AgentGroupChat chat = new(incidentManagerAgent, devopsManagerAgent)
            {
                ExecutionSettings = new()
                {
                    SelectionStrategy = new LabSelectionStragety(),
                    TerminationStrategy = new ApprovalTerminationStragety()
                }
            };

            ChatHistory chatHistory = [];
            var userMessage = $"USER > {logFile}";
            Console.WriteLine(userMessage);

            Console.WriteLine($"\nReady to process log file: {logFile}");

            // Append the current log file to the chat
            chatHistory.AddUserMessage(userMessage);
            chat.AddChatMessages(chatHistory);

            // invoke a response from the agents
            var result = chat.InvokeAsync();
            await foreach (var message in result)
            {
                Console.WriteLine(message.Content);
                chatHistory.Add(message);
            }

            Console.WriteLine();
        }
    }
}

