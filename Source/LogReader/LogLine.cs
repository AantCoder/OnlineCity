using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogReader
{
    internal class LogLine
    {
        public int Thread;
        public DateTime Time;
        public string Prefix;
        public string Content;
        public ContextConnect Context;
        public bool WithException;
        public string LogLevel;
        public DateTime TimeLocal => Context?.LocalTime != default ? Context.LocalTime + Time.TimeOfDay : Time;

        public override string ToString()
        {
            return $"{TimeLocal} |{Thread.ToString().PadLeft(3, ' ')} | [{LogLevel}] {Prefix + (string.IsNullOrEmpty(Prefix) ? "" : " ")}{Content}";
        }
    }
}
