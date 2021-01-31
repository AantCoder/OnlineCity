using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCUnion.Common
{
    public static class ChatUtils
    {
        public static void ParceCommand(string chatLine, out string command, out List<string> argsM)
        {
            var s = chatLine.Split(new char[] { ' ' }, 2);
            command = s[0].Trim().ToLower();
            var args = s.Length == 1 ? "" : s[1];
            //разбираем аргументы в кавычках '. Удвоенная кавычка указывает на её символ.
            argsM = SplitBySpace(args);
        }

        public static List<string> SplitBySpace(string args)
        {
            int i = 0;
            var argsM = new List<string>();
            while (i + 1 < args.Length)
            {
                if (args[i] == '\'')
                {
                    int endK = i;
                    bool exit;
                    do //запускаем поиск след кавычки снова, если после найденной ещё одна
                    {
                        exit = true;
                        endK = args.IndexOf('\'', endK + 1);
                        if (endK >= 0 && endK + 1 < args.Length && args[endK + 1] == '\'')
                        {
                            //это двойная кавычка - пропускаем её
                            endK++;
                            exit = false;
                        }
                    }

                    while (!exit);

                    if (endK >= 0)
                    {
                        argsM.Add(args.Substring(i + 1, endK - i - 1).Replace("''", "'"));
                        i = endK + 1;
                        continue;
                    }
                }

                var ni = args.IndexOf(" ", i);
                if (ni >= 0)
                {
                    //условие недобавления для двойного пробела
                    if (ni > i) argsM.Add(args.Substring(i, ni - i));
                    i = ni + 1;
                    continue;
                }
                else
                {
                    break;
                }
            }
            if (i < args.Length)
            {
                argsM.Add(args.Substring(i));
            }

            return argsM;
        }
    }
}
