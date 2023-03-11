using OCUnion;
using OCUnion.Transfer.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorldOnlineCity.Model
{
    internal class ClientHashCheckerResult
    {
        /// <summary>
        /// Отличающиеся файлы, которые были переписаны
        /// </summary>
        public List<string> ReplaceFiles { get; set; } = new List<string>();
        /// <summary>
        /// Отличающиеся файлы, которые нельзя заменить
        /// </summary>
        public List<string> DifferentFiles { get; set; } = new List<string>();

        private bool MarkExist { get; set; }
        private string MarkFileName => Loger.PathLog + "FileChecking.txt";
        private string ReportFileName => Loger.PathLog + "FileChecked.txt";
        private string ModsConfigByStart { get; }

        public ClientHashCheckerResult()
        {
            if (MarkExist = File.Exists(MarkFileName)) File.Delete(MarkFileName);
            ModsConfigByStart = GetModsConfigContent();
        }

        private string GetModsConfigContent() => File.ReadAllText(Path.Combine(GenFilePaths.ConfigFolderPath, "ModsConfig.xml"));

        private List<string> GetListLi(string text)
        {
            var result = new List<string>();
            int pos = 0;
            while(true)
            {
                pos = text.IndexOf("<li>", pos);
                if (pos < 0) break;
                pos += 4;
                var e = text.IndexOf("</li>", pos);
                if (e < 0) break;
                result.Add(text.Substring(pos, e - pos));
            }
            return result;
        }

        public string ReportComplete()
        {
            if (DifferentFiles.Count > 0 || ReplaceFiles.Count > 0)
            {
                File.CreateText(MarkFileName).Close();
            }
            if (!MarkExist && DifferentFiles.Count == 0)
            {
                if (ReplaceFiles.Count == 0) return null;
                return "OCity_SessionCC_FilesUpdated".Translate();
            }

            string result = null;

            var modsConfigByServer = GetModsConfigContent();
            if (ModsConfigByStart.ToLower() != modsConfigByServer.ToLower())
            {
                var verG = GameXMLUtils.GetByTag(ModsConfigByStart, "version");
                var verS = GameXMLUtils.GetByTag(modsConfigByServer, "version");
                if (verG.ToLower() != verS.ToLower()) result += Environment.NewLine
                        + "OC_HashCheckerResult_VersionErr".Translate(verG, verS);

                var modsG = GetListLi(ModsConfigByStart).Select(s => s.ToLower()).Distinct().ToHashSet();
                var modsS = GetListLi(modsConfigByServer).Select(s => s.ToLower()).Distinct().ToHashSet();

                var modsNeed = new HashSet<string>(modsS);
                modsNeed.ExceptWith(modsG);

                var modsLeft = new HashSet<string>(modsG);
                modsLeft.ExceptWith(modsS);

                if (modsNeed.Count > 0)
                {
                    result += Environment.NewLine
                        + "OC_HashCheckerResult_NeedMods".Translate() + " "
                        + modsNeed.Aggregate("", (r, i) => r + Environment.NewLine + i);
                }
                if (modsLeft.Count > 0)
                {
                    result += Environment.NewLine
                        + "OC_HashCheckerResult_ExcessMods".Translate() + " "
                        + modsLeft.Aggregate("", (r, i) => r + Environment.NewLine + i);
                }

                if (result == null)
                {
                    result += Environment.NewLine + "OC_HashCheckerResult_UnexpectedDiff".Translate();
                }
            }

            if (DifferentFiles.Count > 0)
            {
                result += Environment.NewLine
                    + "OC_HashCheckerResult_DiffFiles".Translate();
            }

            if (ReplaceFiles.Count > 0)
            {
                var cf = ReplaceFiles
                    //.Select(fn => new { fn = fn, ix = fn.ToLower().IndexOf("mods\\") })
                    //.Where(a => a.ix >= 0)
                    //.Select(a => a.fn.Substring(a.ix + 5))
                    .Where(fn => fn.Contains("\\"))
                    .Select(fn => fn.Substring(0, fn.IndexOf("\\")))
                    .Distinct()
                    .ToList();

                if (cf.Count > 0)
                    result += Environment.NewLine
                        + "OC_HashCheckerResult_ChangedDir".Translate() + " "
                        + cf.Aggregate("", (r, i) => r + Environment.NewLine + i);

                result += Environment.NewLine
                    + Environment.NewLine
                    + "OC_HashCheckerResult_ChangedFiles".Translate() + " "
                    + ReplaceFiles.Aggregate("", (r, i) => r + Environment.NewLine + i);
            }

            if (DifferentFiles.Count > 0)
            {
                result += Environment.NewLine
                    + Environment.NewLine
                    + "OC_HashCheckerResult_CriticalDiff".Translate() + " "
                    + DifferentFiles.Aggregate("", (r, i) => r + Environment.NewLine + i);
            }

            if (!string.IsNullOrEmpty(result))
            {
                try
                {
                    File.WriteAllText(ReportFileName, result);
                    Process.Start("notepad", ReportFileName);
                }
                catch
                { }
            }
            return result;
        }

        internal void FileSynchronization(List<ModelFileInfo> files)
        {
            foreach (var f in files)
            {
                //Loger.Log("ReportFileSynchronization " + f.FileName);
                if (f.NeedReplace) ReplaceFiles.Add(f.FileName);
                else DifferentFiles.Add(f.FileName);
            }
        }
    }
}
