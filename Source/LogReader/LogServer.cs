using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogReader
{
    internal class LogServer
    {
        public string Content { get; set; }
        public List<LogLine> Lines { get; set; }
        public List<ContextConnect> Connects { get; set; }
        public DateTime LocalTime { get; set; }

        /// <summary>
        /// Copy with OCUnion.Loger.Culture
        /// </summary>
        public static CultureInfo Culture = CultureInfo.GetCultureInfo("ru-RU");

        public static bool ParceTime(string str, out DateTime time)
        {
            return DateTime.TryParse(str, Culture, DateTimeStyles.None, out time);
        }

        public LogServer(string content)
        {
            Content = content;
        }

        public void Analize()
        {
            //13.11.2021 9:39:57 | 5108 | 25 | Server Koshken_Osken PlayInfo dMS = 4732,6452 dTicks = 319 dValue = 0 dPawn = 0
            //19:31:40.2212 | 4996 | 10 | [--] Server 222   PlayInfo dMS = 4995.5686 dTicks = 48 dValue = 0 dPawn = 0 dBalance = 0 dStorage = 0
            //0123456789 123456789 123456789 123456789

            Connects = new List<ContextConnect>();
            ContextConnects = new Dictionary<int, ContextConnect>();
            Lines = new List<LogLine>();
            var linesRaw = Content.Split('\n');
            LogLine current = null;
            for (int i = 0; i < linesRaw.Length; i++)
            {
                var line = linesRaw[i];
                if (line.EndsWith("\r")) line = line.Remove(line.Length - 1);
                var splitPos = line.IndexOf('|');
                DateTime time;
                if (splitPos < 7 || splitPos > 25
                    || !ParceTime(line.Substring(0, splitPos), out time))
                {
                    if (current != null) current.Content += Environment.NewLine + line;
                    continue;
                }
                if (current != null) AddLine(current);

                current = new LogLine();
                current.Time = time;

                var splitPos1 = line.IndexOf('|', splitPos + 1);
                var splitPos2 = line.IndexOf('|', splitPos1 + 1);

                current.Thread = int.Parse(line.Substring(splitPos1 + 1, splitPos2 - splitPos1 - 1).Trim());

                if (line.Length <= splitPos2 + 2) continue;
                if (line[splitPos2 + 1] == ' ') splitPos2++;

                if (line.Length >= splitPos2 + 5
                    && line[splitPos2 + 1] == '['
                    && line[splitPos2 + 4] == ']')
                {
                    current.LogLevel = line.Substring(splitPos2 + 2, 2);
                    splitPos2 += 5;
                }
                else
                    current.LogLevel = "--";

                if (line.Length <= splitPos2 + 2) continue;
                if (line[splitPos2 + 1] == ' ') splitPos2++;

                if (line.Length > splitPos2 + 8 && line.Substring(splitPos2 + 1, 7) == "Server ")
                {
                    current.Prefix = "Server";
                    splitPos2 += 7;
                }

                current.Content = line.Substring(splitPos2 + 1);
            }
            if (current != null) AddLine(current);
        }


        //Сразу после порождения нити нового подключения пишется один из логов: New connect или Abort connect BanIP
        //на основе этого запоминаем контекст игрока, пока не возникнет новое подключение
        private Dictionary<int, ContextConnect> ContextConnects;

        private void AddLine(LogLine line)
        {
            if (!string.IsNullOrEmpty(line.Content))
            {
                //------------------------------
                // Context Thread Begin End
                var content = line.Content.Trim();
                var newConnect = content.StartsWith("New connect") || content.StartsWith("Abort connect BanIP");
                if (newConnect || !ContextConnects.ContainsKey(line.Thread))
                {
                    var c = new ContextConnect(line) { LocalTime = LocalTime };
                    ContextConnects[line.Thread] = c;
                    if (newConnect) Connects.Add(c);
                }
                line.Context = ContextConnects[line.Thread];
                line.Context.End = line.Time;

                //------------------------------
                // WithException
                if (content.Contains(Environment.NewLine + "   at "))
                    line.WithException = true;

                //------------------------------
                // IP
                if (content.StartsWith("New connect"))
                {
                    var s1 = content.Substring("New connect".Length).Trim();
                    line.Context.IP = s1.Substring(0, s1.IndexOf(' '));
                }
                if (content.StartsWith("Abort connect BanIP"))
                {
                    var s1 = content.Substring("Abort connect BanIP".Length).Trim();
                    line.Context.IP = s1;
                }

                //------------------------------
                // Key Login 
                if (content.StartsWith("Checked") && content.Contains(" key="))
                {
                    var s1 = content.Substring("Checked".Length).Trim();
                    var i1 = s1.IndexOf(" key=");
                    if (i1 > 0)
                    {
                        line.Context.Login = s1.Substring(0, i1);
                        line.Context.Key = s1.Substring(i1 + " key=".Length);
                    }
                    else
                    {
                        line.Context.Key = s1.Replace("key=", "").Trim();
                    }
                }

                //------------------------------
                // Login
                if (content.Contains(" ServerInformation"))
                {
                    var s1 = content.Trim();
                    var i1 = s1.IndexOf(" ServerInformation");
                    if (i1 > 0)
                        line.Context.Login = s1.Substring(0, i1);
                }

                //------------------------------
                // local time: 2022-04-06 08:35:52.3964
                if (content.Contains("local time: "))
                {
                    var s1 = content.Trim();
                    var i1 = s1.IndexOf("local time: ");
                    if (i1 >= 0)
                    {
                        i1 += 12;
                        var date = s1.Substring(i1, s1.IndexOf(" ", i1) - i1);
                        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out line.Context.LocalTime))
                            line.Context.LocalTime = default;
                        else
                            LocalTime = line.Context.LocalTime;
                    }
                }
            }
            Lines.Add(line);
        }

    }
}
