using System.Net.Mail;
using System.Text;
using System.Xml.Linq;

namespace JPParser.Services;

public class EmailSenderService
{
    private readonly Dictionary<string, string> _parameters;
    
    public EmailSenderService(Dictionary<string, string> parameters)
    {
        _parameters = parameters;
    }
    
    public async Task<int> PrepareAndSendEmail(XDocument document, int topCount)
    {
        int maxRetries = 3; // Максимальное количество попыток
        int retryCount = 0;
        bool emailSent = false;
        while (retryCount < maxRetries && !emailSent)
        {
            try
            {
                // Подготовка EML файла
                string emlFilePath = Path.Combine(Directory.GetCurrentDirectory(), "result.eml");

                XDocument xmldoc = document;

                string emlBody = GenerateContentFromMergedXml(xmldoc, topCount);

                // Формирование EML с заголовками
                var emlContent = new StringBuilder();
                emlContent.AppendLine("Date: " + DateTime.Now.ToString("r"));
                emlContent.AppendLine("From: " + _parameters["FromEmail"]);
                emlContent.AppendLine("To: " + _parameters["ToEmail"]);
                emlContent.AppendLine("Subject: Mail from JP_BOT");
                emlContent.AppendLine("MIME-Version: 1.0");
                emlContent.AppendLine("Content-Type: text/html; charset=UTF-8");
                emlContent.AppendLine();
                emlContent.AppendLine(emlBody);

                byte[] emlBytes = Encoding.UTF8.GetBytes(emlContent.ToString());

                // Сохраняем EML файл
                await using (var fs = new FileStream(emlFilePath, FileMode.Create))
                {
                    await fs.WriteAsync(emlBytes);
                }

                // Отправка EML файла
                using MailMessage message = new MailMessage();
                using SmtpClient smtp = new SmtpClient();

                message.From = new MailAddress(_parameters["FromEmail"]);
                message.To.Add(new MailAddress(_parameters["ToEmail"]));
                message.Subject = "Mail from JP_BOT";

                // Добавляем вложение EML файла
                Attachment attachment = new Attachment(emlFilePath);
                message.Attachments.Add(attachment);

                smtp.Host = _parameters["SmtpHost"];
                smtp.Port = int.Parse(_parameters["SmtpPort"]);
                smtp.EnableSsl = bool.Parse(_parameters["EnableSsl"]);
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new System.Net.NetworkCredential(_parameters["Username"], _parameters["Password"]);

                // Отправляем email
                await smtp.SendMailAsync(message);
                Console.WriteLine($"File: {emlFilePath} - email sent successfully!");
                emailSent = true;
                return 1;
            }
            catch (IOException ex) when (ex.Message.Contains("because it is being used by another process"))
            {
                retryCount++;
                Console.WriteLine($"Error preparing or sending email: {ex.Message}. Retry {retryCount} of {maxRetries}");
                await Task.Delay(1000); // Задержка перед повторной попыткой
            }
            catch (Exception ex)
            {
                retryCount++;
                Console.WriteLine($"Error preparing or sending email: {ex.Message}");
                //emailSent = true;
                return 0;
            }
        }
        return emailSent ? 1 : 0;
    }
    private string GenerateContentFromMergedXml(XDocument mergedXml, int topCount)
    {
        var contentBuilder = new StringBuilder();
        contentBuilder.AppendLine("<div><div style='font-size: 9pt;'>");
        int uniqueIps = 0;
        int totalVisits = 0;
        int count = 0;
        var pagename = string.Empty;
        var body = new StringBuilder();
        
        // Проходим по каждому узлу в корневом элементе (узлы с названиями файлов)
        foreach (var fileElement in mergedXml.Root!.Elements())
        {
            // Имя страницы (название тега)
            var elementName = fileElement.Name.LocalName;


            if (elementName.Contains("_u"))
            {
                // Извлекаем количество уникальных IP из секции
                var root = fileElement.Element("ROOT");
                var row = root.Element("ROW");
                uniqueIps = int.Parse(row.Element("unique_ips")?.Value.Trim()!);
                
            }
            else if (elementName.Contains("_t"))
            {
                // Извлекаем общее количество посещений из секции
                var root = fileElement.Element("ROOT");
                var row = root.Element("ROW");
                totalVisits = int.Parse(row.Element("total_visits")?.Value.Trim()!);
            }
            else
            {
                body = new StringBuilder();  
                // Проходим по каждому элементу "ROW" внутри текущего файла для вывода IP и хитов
                foreach (var row in fileElement.Descendants("ROW"))
                {
                    string ip = row.Element("clientip")?.Value.Trim()!;
                    int hits = int.Parse(row.Element("hits")?.Value.Trim()!);
                    body.Append($"<p>{ip} - {hits}</p>");
                }

                pagename = elementName;
            }

            count++;
            if (count == 3)
            {
                // Формируем заголовок с указанием статистики
                string header = 
                    $"<p style='font-size: 12pt;'>[{pagename.ToUpper()}] >> [total-hits: {totalVisits}, unique-callers: {uniqueIps}] >> top {topCount} caller details:</p>";
                // Формируем весь контент для данного раздела
                var yellowZoneContent = header + body;

                // Добавляем оформленный блок контента
                contentBuilder.AppendLine(
                    "<div style='margin: 10px 0; padding: 8px; box-shadow: 0 0 8px rgba(0, 0, 0, 0.1); background-color: #FFFFEE; border: 1px solid #DFDF00; border-radius: 4px;'>");
                contentBuilder.Append(yellowZoneContent);
                contentBuilder.AppendLine("</div>");
                count = 0;
            }
        }

        contentBuilder.AppendLine("</div></div>");
        return contentBuilder.ToString();
    }
    
}