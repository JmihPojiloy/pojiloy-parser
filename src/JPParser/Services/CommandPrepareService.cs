using Microsoft.Extensions.Configuration;

namespace JPParser.Services;

public class CommandPrepareService
{
    private readonly string? _logFile;
    private readonly int _topCount;
    private readonly IConfigurationSection _commandSection;
    
    public CommandPrepareService(string logFile, int topCount, IConfigurationSection commandSection)
    {
        _logFile = logFile;
        _topCount = topCount;   
        _commandSection = commandSection;
    }

    public List<string> GetCommands()
    {
        var commands = new List<string>();
        var commandsDictionary = GetCommandsFromConfiguration();
        foreach (var command in commandsDictionary)
        {
            var totalCommand = 
                string.Format(
                    "\"SELECT COUNT(*) AS total_visits INTO {0}_t.xml FROM {1} WHERE cs-uri-stem IN ('{2}'; '{3}')\" -i:W3C -o:XML",
                    command.Key, _logFile, command.Value[0], command.Value[1]);
            
            commands.Add(totalCommand);

            // receive int
            var uniqCommand =
                string.Format(
                    "\"SELECT DISTINCT [X-Forwarded-For] INTO unique_ips.txt FROM {0} WHERE cs-uri-stem IN ('{1}'; '{2}')\" -i:W3C -o:CSV", 
                    _logFile, command.Value[0], command.Value[1]);

            commands.Add(uniqCommand);
            
            var uniqAdditionalCommand = 
                string.Format(
                    "\"SELECT COUNT(*) AS unique_ips INTO {0}_u.xml FROM unique_ips.txt\" -i:CSV -o:XML",
                    command.Key);
            
            commands.Add(uniqAdditionalCommand);
            
            // receive int
            var topCommand =
                string.Format(
                    "\"SELECT TOP 20 [X-Forwarded-For] AS clientip, COUNT(*) AS hits INTO {0}.xml FROM {1} WHERE cs-uri-stem IN ('{2}'; '{3}') GROUP BY [X-Forwarded-For] ORDER BY Hits DESC\" -i:W3C -o:XML", 
                    command.Key, _logFile, command.Value[0], command.Value[1], command.Value[0], command.Value[1]);

            commands.Add(topCommand);
        }
        return commands;
    }
    
    private Dictionary<string, List<string>> GetCommandsFromConfiguration()
    {
        Dictionary<string, List<string>> commands = new();
        var sections = _commandSection.GetChildren();

        foreach (var section in sections)
        {
            var title = section["title"];
            
            var urlsSection = section.GetSection("urls");
            var urls = urlsSection.GetChildren().Select(s => s.Value).ToList();

            if (urls != null && urls.Count > 0)
            {
                commands.Add(title, urls);
            }
        }

        return commands;
    }
}