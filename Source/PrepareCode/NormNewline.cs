
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PrepareCode
{
    internal class NormNewline
	{
		private static List<string> Files = new List<string> { ".cs", ".txt", ".xml", ".props", ".config" };

		public static int Run(string directoryName)
		{
			try
			{
				Console.WriteLine("Path: " + directoryName);
				Console.WriteLine("Press Enter for start...");
				Console.ReadKey();
				Console.WriteLine();
				EditFiles(directoryName, "*.*", delegate (string fileName, Func<string> getContent)
				{
					if (!Files.Any((string ff) => fileName.ToLower().EndsWith(ff)))
					{
						return null;
					}
					bool flag = false;
					string text = getContent();
					string text2 = NormalizeNewline(text);
					if (text != text2)
					{
						flag = true;
						text = text2;
					}
					if (!flag)
					{
						return null;
					}
					return text;
				});
				Console.WriteLine("Ready");
				Console.ReadKey();
				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception! " + ex.Message);
				Console.ReadLine();
				return -1;
			}
		}

		private static void EditFiles(string sourcePath, string mask, Func<string, Func<string>, string> editFile)
		{
			string[] files = Directory.GetFiles(sourcePath, mask, SearchOption.AllDirectories);
			foreach (string newPath in files)
			{
				string textFile = null;
				string text = editFile(newPath, () => textFile = File.ReadAllText(newPath));
				if (text != null)
				{
					Console.WriteLine(newPath);
					if (text != string.Empty)
					{
						File.WriteAllText(newPath, text, Encoding.UTF8);
					}
					else if (File.Exists(newPath))
					{
						File.Delete(newPath);
					}
				}
				else if (textFile != null)
				{
					var preamble = Encoding.UTF8.GetPreamble();
					if (new FileInfo(newPath).Length >= 3)
					{
						var buf = new byte[preamble.Length];
						using (var f = File.OpenRead(newPath))
						{
							f.Read(buf, 0, 3);
						}
						if (preamble[0] != buf[0]
							|| preamble[1] != buf[1]
							|| preamble[2] != buf[2])
						{
							Console.WriteLine(newPath);
							File.WriteAllText(newPath, textFile, Encoding.UTF8);
						}
					}
				}
			}
		}


		private static Regex _crlfRegex = new Regex("\\r\\n|\\n\\r|\\n|\\r");
		private static string NormalizeNewline(string str)
		{
			return _crlfRegex.Replace(str, "\r\n");
		}
	}
}
