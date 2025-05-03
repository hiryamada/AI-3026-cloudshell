using Microsoft.SemanticKernel;

// Create a Plugin for the email functionality
class EmailPlugin
{
    [KernelFunction("SendEmail")]
    public static void SendEmail(string to, string subject, string body)
    {
        // Simulate sending an email
        Console.WriteLine($"""
            to: {to}
            Subject: {subject}
            {body}
        """
        );
    }
}