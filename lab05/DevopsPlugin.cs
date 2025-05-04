using Microsoft.SemanticKernel;
using System.ComponentModel;

class DevopsPlugin
{
    private static void AppendToLogFile(string filepath, string content)
    {
        using StreamWriter writer = new(filepath, true);
        writer.WriteLine(content.Trim());
    }
    [KernelFunction]
    [Description("A function that restarts the named service")]
    public static string RestartService(
        [Description("the name of the service to restart")]
        string serviceName = "", 
        [Description("the log file to write to")]
        string logfile = "")
    {
        string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ALERT  DevopsAssistant: Multiple failures detected in {serviceName}. Restarting service.\n" +
                            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO  {serviceName}: Restart initiated.\n" +
                            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO  {serviceName}: Service restarted successfully.";
        AppendToLogFile(logfile, logMessage);
        return $"Service {serviceName} restarted successfully.";
    }
    [KernelFunction]
    [Description("A function that rolls back the transaction")]
    public static string RollbackTransaction(
        [Description("the log file to write to")]
        string logfile = ""
        )
    {
        string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ALERT  DevopsAssistant: Transaction failure detected. Rolling back transaction batch.\n" +
                            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO   TransactionProcessor: Rolling back transaction batch.\n" +
                            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO   Transaction rollback completed successfully.";
        AppendToLogFile(logfile, logMessage);
        return "Transaction rolled back successfully.";
    }
    [KernelFunction]
    [Description("A function that redeploys the named resource")]
    public static string RedeployResource(
        [Description("the resource name to redeploy")]
        string resourceName = "", 
        [Description("the log file to write to")]
        string logfile = ""
    )
    {
        string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ALERT  DevopsAssistant: Resource deployment failure detected in '{resourceName}'. Redeploying resource.\n" +
                            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO   DeploymentManager: Redeployment request submitted.\n" +
                            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO   DeploymentManager: Service successfully redeployed, resource '{resourceName}' created successfully.";
        AppendToLogFile(logfile, logMessage);
        return $"Resource '{resourceName}' redeployed successfully.";
    }
    [KernelFunction]
    [Description("A function that increases the quota")]
    public static string IncreaseQuota(
        [Description("the log file to write to")]
        string logfile = ""
    )
    {
        string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ALERT  DevopsAssistant: High request volume detected. Increasing quota.\n" +
                            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO   APIManager: Quota increase request submitted.\n" +
                            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO   APIManager: Quota successfully increased to 150% of previous limit.";
        AppendToLogFile(logfile, logMessage);
        return "Successfully increased quota.";
    }
    [KernelFunction]
    [Description("A function that escalates the issue")]
    public static string EscalateIssue(
        [Description("the log file to write to")]
        string logfile = ""
    )
    {
        string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ALERT  DevopsAssistant: Cannot resolve issue.\n" +
                            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ALERT  DevopsAssistant: Requesting escalation.";
        AppendToLogFile(logfile, logMessage);
        return "Submitted escalation request.";
    }
}
