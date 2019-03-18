using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace RegHtml
{
    class Program
    {
        public static string GetStatus(string html)
        {
            // Функция получения тегов определенного типа
            Func<string, string, List<string>> GetTags = (content, name) =>
            {
                var retval = new List<string>();
                var TAGS = Regex.Matches(content, $"<{ name }.*?>(?'inner'[\\s\\S\\w\\W]*?)</{ name }>");
                for (int i = 0; i < TAGS.Count; i++) retval.Add(Regex.Replace(TAGS[i].Groups["inner"].Value, "[\r\n]", ""));
                return retval;
            };

            // Получение строк из таблицы
            Func<string, List<string[]>> GetRows = (content) =>
            {
                var TRS = GetTags(content, "tr");
                var Result = TRS.Select((tr) => GetTags(tr, "td").ToArray()).Where(tr => tr != null && tr.Length == 2).ToList();
                return Result;
            };

            // Получение строк
            var ROWS = GetRows(html);

            var status = ROWS.Select((row) =>
            {
                var date = Regex.Match(row[0], @"(?'date'[\d]{2}[.][\d]{2}[.][\d]{4})")?.Groups["date"]?.Value;
                var time = Regex.Match(row[0], @"(?'time'[\d]{2}[:][\d]{2})")?.Groups["time"]?.Value;
                if (time == "" || time == null) time = "00:00";
                if (date == "") return null;
                
                try
                {
                    var datetime = DateTime.ParseExact($"{date} {time}", "dd.MM.yyyy HH:mm", null);
                    var stat = Regex.Match(row[1], @"«(?'status'[^\d][\s\S\w\W]+?)»").Groups["status"].Value;
                    if (stat == null || stat == "") return null;
                    return new Tuple<DateTime, string>(datetime, stat);
                }
                catch
                {
                    return null;
                }
            })
            .Where(n => n != null)
            .ToList()
            .First();

            return (status != null)? status.Item2 : null;
        }
        
        static void Main(string[] args)
        {
            string url = "http://zakupki.gov.ru/223/contract/public/contract/view/journal.html?id=6604187&viewMode=FULL";
            string content = "";

            var t = Task.Factory.StartNew<string>(() => 
            {
                System.Net.WebClient client = new System.Net.WebClient();
                client.Encoding = Encoding.UTF8;

                content = client.DownloadString(url as string);
                return content;
            });

            Console.WriteLine("Ожидание скачивания страницы...");
            content = t.Result;

            var status = GetStatus(content);
            
            
            Console.ReadKey();
        }
    }
}