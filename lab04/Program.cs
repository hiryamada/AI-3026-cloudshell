#pragma warning disable SKEXP0110

// Add references
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using dotenv.net;
using dotenv.net.Utilities;

class Program
{
    static async Task Main()
    {
        // Clear the console
        Console.Clear();

        // Get configuration settings
        DotEnv.Load();
        var connectionString = EnvReader.GetStringValue("AZURE_AI_AGENT_PROJECT_CONNECTION_STRING");
        var deployName = EnvReader.GetStringValue("AZURE_AI_AGENT_MODEL_DEPLOYMENT_NAME");

        // Connect to the Azure AI Foundry project
        AIProjectClient client = AzureAIAgent.CreateAzureAIClient(connectionString, new DefaultAzureCredential());
        AgentsClient agentsClient = client.GetAgentsClient();

        // Define an Azure AI agent that sends an expense claim email
        var instructions = """
            You are an AI assistant for expense claim submission.
            When a user submits expenses data and requests an expense claim, 
            use the plug-in function to send an email to expenses@contoso.com 
            with the subject 'Expense Claim`and a body that contains itemized expenses with a total.
            Then confirm to the user that you've done so.
            """;
        Agent azureAIProjectAgent = agentsClient.CreateAgent(
            model: deployName,
            name: "expenseAgent",
            instructions: instructions
        );

        // Create a semantic kernel agent
        var emailPlugin = KernelPluginFactory.CreateFromType<EmailPlugin>();
        AzureAIAgent agent = new(azureAIProjectAgent, agentsClient, plugins: [emailPlugin]);

        // Use the agent to process the expenses data
        Microsoft.SemanticKernel.Agents.AgentThread agentThread = new AzureAIAgentThread(agent.Client);
        ChatHistory chatHistory = [];

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
            chatHistory.AddUserMessage(userPrompt);
            var agentResponse = agent.InvokeAsync(chatHistory, agentThread);

            // Show the latest response from the agent
            await foreach (ChatMessageContent message in agentResponse)
            {
                Console.WriteLine(message.Content);
                chatHistory.Add(message);
            }
        }

        // Get the conversation history
        Console.WriteLine("\nConversation Log:\n");

        foreach (var message in chatHistory)
        {
            Console.WriteLine($"{message.Role}: {message.Content}");
        }
    }
}
