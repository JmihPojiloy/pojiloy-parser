using System.Diagnostics;
using System.Net.Mail;
using System.Reflection.Metadata;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using JPParser.Services;
using Microsoft.Extensions.Configuration;

namespace JPParser;

public static class Program
{
    private static IConfiguration? _configuration;
    private static readonly string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;

    public static async Task Main(string[] args)
    {
        // Настройка конфигурации
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        _configuration = builder.Build();

        // Инициализация
        int emailCount = 0;

        //string logFileName = _configuration["LogFileName"]!;

        var emailSettings = _configuration.GetSection("EmailSettings")!;

        var emailParameters = new Dictionary<string, string>();

        emailParameters.Add("Username", emailSettings["Username"]!);
        emailParameters.Add("Password", emailSettings["Password"]!);
        emailParameters.Add("FromEmail", emailSettings["FromEmail"]!);
        emailParameters.Add("ToEmail", emailSettings["ToEmail"]!);
        emailParameters.Add("SmtpHost", emailSettings["SmtpHost"]!);
        emailParameters.Add("EnableSsl", emailSettings["EnableSsl"]!);
        emailParameters.Add("SmtpPort", emailSettings["SmtpPort"]!);

        //var commands = _configuration.GetSection("Commands").GetChildren();
        string logparserPath = Path.Combine(projectDirectory, "parser", "LogParser.exe");

        var commandPrepare = _configuration.GetSection("report");
        
        var logFileName = commandPrepare["source-file"]!;
        var logFilePath = Path.Combine(projectDirectory, "parser", logFileName);
        var topCount = int.Parse(commandPrepare["top-count"]!);
        var commandsSection = commandPrepare.GetSection("report-sections");
        
        
        var xmlfiles = new List<string>();
        
        // Инициализация сервисов
        var logParser = new LogparserService(logparserPath);
        var commandPrepareService = new CommandPrepareService(logFilePath, topCount, commandsSection);
        var emailSenderService = new EmailSenderService(emailParameters);
        
        // System watcher
        using var _watcher = new FileSystemWatcher(projectDirectory, "*.xml");

        _watcher.NotifyFilter = NotifyFilters.Attributes
            | NotifyFilters.CreationTime
            | NotifyFilters.DirectoryName
            | NotifyFilters.FileName
            | NotifyFilters.LastAccess
            | NotifyFilters.LastWrite
            | NotifyFilters.Size;

        _watcher!.Created += (sender, args) =>
        {
            xmlfiles.Add(args.FullPath);
        };
        
        _watcher.EnableRaisingEvents = true;
        _watcher?.WaitForChanged(WatcherChangeTypes.All, 3000);

        // Действия

        var commands = commandPrepareService.GetCommands();

        foreach (var command in commands)
        {
            logParser.RunLogParser(command);
            await Task.Delay(1000);
        }

        var contentForSend = MergeXmlFiles(xmlfiles);

        var send = await emailSenderService.PrepareAndSendEmail(contentForSend, topCount);

        Console.WriteLine($"[DESCRIPTION] completed, send {emailCount} emails.");
    }
    
    private static XDocument MergeXmlFiles(IEnumerable<string> xmlFilePaths)
    {
        // Создаем корневой элемент для итогового XML документа
        var resultDocument = new XDocument(new XElement("Root"));

        var filePaths = xmlFilePaths.ToList();
        foreach (var filePath in filePaths)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    // Загружаем текущий XML файл
                    var xmlContent = XDocument.Load(filePath);

                    // Получаем имя файла без расширения для использования в качестве тега
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

                    // Создаем элемент с именем файла и добавляем содержимое файла
                    var fileElement = new XElement(fileNameWithoutExtension, xmlContent.Root);

                    // Добавляем элемент в итоговый документ
                    resultDocument.Root!.Add(fileElement);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке файла {filePath}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Файл {filePath} не существует.");
            }
        }
        DeleteFiles(filePaths);
        return resultDocument;
    }
    
    private static void DeleteFiles(IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            File.Delete(file);
        }
        File.Delete(Path.Combine(projectDirectory, "unique_ips.txt"));
    }
}