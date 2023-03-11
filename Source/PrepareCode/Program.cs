using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PrepareCode
{
    internal class Program
    {
        static private string SourcePath;
        static private string ProjectName;
        static private string RootPath;
        static private string PublicSourcePath;
        static private string PublicBinPath;
        static private List<string> SavedDir = new List<string>()
        {
            ".svn",
            ".git",
        };
        static private List<string> IgnoreDir = new List<string>()
        {
            "bin",
            "obj",
            ".svn",
            ".git",
            ".vs",
            "packages",
            "BuildScripts",
            "BuildOutput",
            "TestResults",
        };

        static int Main(string[] args)
        {
            try
            {
                var dir = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
                var diri = dir.IndexOf("\\Source\\");
                if (diri == -1)
                {
                    Console.WriteLine("Error path: " + dir);
                    return -1;
                }
                SourcePath = dir.Substring(0, diri + "\\Source\\".Length - 1);
                dir = Path.GetDirectoryName(SourcePath);
                ProjectName = Path.GetFileName(dir);
                RootPath = Path.GetDirectoryName(dir);
                PublicSourcePath = Path.Combine(RootPath, ProjectName + "Public");
                PublicBinPath = Path.Combine(SourcePath, "BuildOutput\\OnlineCity");

                Console.WriteLine("Path: " + RootPath); // c:\W\OnlineCity
                Console.WriteLine("Public source: " + PublicSourcePath); // c:\W\OnlineCity\OnlineCityPublic
                Console.WriteLine("Public bin: " + PublicBinPath); // c:\W\OnlineCity\OnlineCity\Source\BuildOutput\OnlineCity
                Console.WriteLine("Press Enter for start...");
                Console.ReadLine();
                Console.WriteLine("Copy...");

                if (!Directory.Exists(PublicSourcePath)) Directory.CreateDirectory(PublicSourcePath);
                else DeleteFiles(PublicSourcePath, SavedDir);
                CopyFiles(Path.Combine(RootPath, ProjectName), PublicSourcePath, IgnoreDir);

                Console.WriteLine("Press Enter for continue...");
                Console.ReadLine();

                if (Directory.Exists(PublicBinPath)) Directory.Delete(PublicBinPath, true);
                Directory.CreateDirectory(PublicBinPath);
                CopyFiles(Path.Combine(RootPath, ProjectName), PublicBinPath, IgnoreDir.Concat(new List<string>() { "Source" }));

                Console.WriteLine("Prepare...");

                EditFiles(PublicSourcePath, "*.cs", (fileName, getContent) =>
                {
                    bool needRecord = false;
                    var content = getContent();
                    var newc = NormalizeNewline(content);
                    if (content != newc)
                    {
                        needRecord = true;
                        content = newc;
                    }


                if (!needRecord) return null;
                    return content;
                });

                Console.WriteLine("Ready");
                Console.ReadLine();
                return 0;
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception! " + e.Message);
                Console.ReadLine();
                return -1;
            }
        }



        private static void EditFiles(string sourcePath, string mask, Func<string, Func<string>, string> editFile)
        {
            foreach (string newPath in Directory.GetFiles(sourcePath, mask, SearchOption.AllDirectories))
            {
                var newContent = editFile(newPath, () => File.ReadAllText(newPath));
                if (newContent != null)
                {
                    if (newContent != string.Empty)
                        File.WriteAllText(newPath, newContent);
                    else
                        if (File.Exists(newPath)) File.Delete(newPath);
                }
            }
        }

        private static void CopyFiles(string sourcePath, string targetPath, IEnumerable<string> ignoreDir)
        {
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                if (ignoreDir.Any(i => (dirPath + "\\").Contains($"\\{i}\\"))) continue;
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                if (ignoreDir.Any(i => newPath.Contains($"\\{i}\\"))) continue;
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        private static void DeleteFiles(string targetPath, IEnumerable<string> ignoreDir)
        {
            //директории ignoreDir не будут удалены только в корне targetPath! Вложенные удаляются
            foreach (string dirPath in Directory.GetDirectories(targetPath, "*", SearchOption.TopDirectoryOnly))
            {
                if (ignoreDir.Any(i => (dirPath + "\\").Contains($"\\{i}\\"))) continue;
                Directory.Delete(dirPath, true);
            }

            foreach (string newPath in Directory.GetFiles(targetPath, "*.*", SearchOption.TopDirectoryOnly))
            {
                if (ignoreDir.Any(i => newPath.Contains($"\\{i}\\"))) continue;
                File.Delete(newPath);
            }
        }

        private static Regex _crlfRegex = new Regex(@"\r\n|\n\r|\n|\r");
        public static string NormalizeNewline(string str)
        {
            return _crlfRegex.Replace(str, "\r\n");
        }

    }
}
