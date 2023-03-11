using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogReader
{
    internal class ContextConnect
    {
        public int Thread;
        public string IP;
        public string Key;
        public string Login;
        public DateTime Begin;
        public DateTime End;
        public DateTime LocalTime;
        public DateTime BeginLocal => LocalTime == default ? Begin : LocalTime + Begin.TimeOfDay;
        public DateTime EndLocal => LocalTime == default ? End : LocalTime + End.TimeOfDay;

        public ContextConnect(LogLine line)
        {
            Thread = line.Thread;
            Begin = line.Time;
        }

        public override string ToString()
        {
            return $"{Login} | {BeginLocal} - {EndLocal} | {IP} | {Key}";
        }
    }
}
