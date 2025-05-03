using System.Text.Json;
using Azure.AI.Projects;


class UserFunctions
{
    // Create a function to submit a support ticket
    public static async Task<string> SubmitSupportTicket(string emailAddress, string description)
    {
        string ticketNumber = Guid.NewGuid().ToString("N").Substring(0, 6);
        var fileName = $"ticket-{ticketNumber}.txt";
        var text = $"""
            Support ticket: {ticketNumber}
            Submitted by: {emailAddress}
            Description: {description}
            """;
        var messageJson = $$"""
            {"message": f"Support ticket {{ticketNumber}} submitted. The ticket file is saved as {{fileName}}"}
            """;
        await File.WriteAllTextAsync(fileName, text);
        return messageJson;
    }
    
    // Define a set of callable functions
    public static FunctionToolDefinition SubmitSupportTicketTool = new(
        name: "submitSupportTicket",
        description: "Submits Support Ticket.",
        parameters: BinaryData.FromObjectAsJson(
            new
            {
                Type = "object",
                Properties = new
                {
                    EmailAddress = new
                    {
                        Type = "string",
                        Description = "User's email address",
                    },
                    Description = new
                    {
                        Type = "string",
                        Description = "User's issue description",
                    },
                },
                Required = new[] { "emailAddress", "description" },
            },
            new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        )
    );
}