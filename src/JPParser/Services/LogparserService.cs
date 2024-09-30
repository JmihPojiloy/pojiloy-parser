using System.Diagnostics;

namespace JPParser.Services;

public class LogparserService
{
    private readonly string? _path;
    
    public LogparserService(string path) => _path = path;

    public void RunLogParser(string command)
    {
        var process = LogparserStart(_path, command);

        process.Start();
        process.WaitForExit();

        Console.WriteLine("LogParser command completed with output format.");
    }
    
    private Process LogparserStart(string logparserPath, string infoString)
    {
        var logparser = new Process();
        ProcessStartInfo info = new ProcessStartInfo
        {
            FileName = logparserPath,
            Arguments = infoString,
            UseShellExecute = false
        };
        logparser.StartInfo = info;
        
        return logparser;
    }
}