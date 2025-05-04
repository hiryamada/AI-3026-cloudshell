using Microsoft.SemanticKernel;
using System.ComponentModel;

class LogFilePlugin
{
    [KernelFunction]
    [Description("Accesses the given file path string and returns the file contents as a string.")]
    public static string ReadLogFile(
        [Description("the path of the log file to read.")]
        string filePath
    ) => File.ReadAllText(filePath);
}
