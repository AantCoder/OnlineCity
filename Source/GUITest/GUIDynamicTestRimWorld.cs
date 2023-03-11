using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GUITest
{
    public class GUIDynamicTestRimWorld
    {
        public GUITestRimWorldModelSetting Setting { get; }
        public string FolderResource { get; }
        public string ExecText { get; }
        public bool WithConnect { get; }
        public static Action<string> LogRuning { get; private set; }

        private string TemplareRun = @"//copy from GUITestRimWorld
using GUITest;
using GuideTestGUI;
using Sidekick.Sidekick.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

public class DynClass##0##
{
	public GUITest.GUITestRimWorldModelSetting Setting;

    public string ResourceFolder = @""##2##"";
    public string ResultFolder = @""##2##\..\TestResult"";

	public string DynMethod(ref GUITest.GUITestRimWorldModelSetting setting)
	{
		Setting = setting;
        string LogText = null;
        Action<string> Log = (m) => 
        {
            LogText += m + Environment.NewLine;
            GUIDynamicTestRimWorld.LogRuning(m);
        };
			
		var result = StartRimWorld((game, server, clientModLog) =>
		{
			//После запуска игры выполняются команды ниже:
            try
            {
##1##
			    return LogText ?? ""Finish"";
            }
            catch (Exception expp)
            {
                var expperr = expp.Message + Environment.NewLine
                    + (expp.InnerException == null ? """" : expp.InnerException.Message + Environment.NewLine)
                    + Environment.NewLine
                    + expp.ToString();
                return LogText + Environment.NewLine + Environment.NewLine
                    + ""Exception: "" + expperr;
            }
		});
		return result;
	}
	
	public string StartRimWorld(Func<GuideUI, GuideUI, GuideUI, string> test)
	{
        if (Directory.Exists(ResultFolder)) Directory.Delete(ResultFolder, true);
        Directory.CreateDirectory(ResultFolder);

		string result = """";
		using (var loaderImage = new LoaderImage(ResourceFolder + @""\{0}.png""))
		using (var server = new GuideUI())
		{
			server.LoaderImage = loaderImage;
			server.EnvironmentSourceFolderName = Setting.ServerFolder;
			server.EnvironmentWorkFolderName = Setting.TempFolder + @""\Server"";
			server.EnvironmentBackupFolderName = null;
            server.ResultTestLogFileName = Path.Combine(ResultFolder, ""serverLog.txt"");
			server.NeedRecoveryWorkFolder = false;
			if (##3##) 
            {
                server.StartEnvironment();
                server.StartProcess(server.EnvironmentWorkFolderName + @""\Server.exe"", false);
            }

			using (var game = new GuideUI())
			{
				game.LoaderImage = loaderImage;
				game.EnvironmentSourceFolderName = Setting.TestConfigFolder;
				game.EnvironmentWorkFolderName = Setting.GameConfigFolder;
				game.EnvironmentBackupFolderName = Setting.TempFolder + @""\BackupConfig"";
                game.ResultTestLogFileName = Path.Combine(ResultFolder, ""gameLog.txt"");
				game.NeedRecoveryWorkFolder = true;
				game.StartEnvironment();
                if (##3##) game.StartProcess(Setting.GameExec);
                else game.ConnectProcess(Setting.WindowsTitle);
				game.LogFileName = Setting.GameLogFile;

                Thread.Sleep(5000);

                server.SetLogFileFromFolder(server.EnvironmentWorkFolderName + @""\World"");

                var clientModLog = new GuideUI(); //сокращенный запуск только для логов
                clientModLog.ResultTestLogFileName = Path.Combine(ResultFolder, ""clientModLog.txt"");
                clientModLog.SetLogFileFromFolder(Setting.ModLogFolder);

                result = test(game, server, clientModLog);

                clientModLog.GetLogNewText();
                game.GetLogNewText();
                server.GetLogNewText();
            }
        }
        return result;
    }
}
";

        public GUIDynamicTestRimWorld(GUITestRimWorldModelSetting setting, string folderResource, string execText, bool withConnect, Action<string> logRuning)
        {
            Setting = setting;
            FolderResource = string.IsNullOrEmpty(folderResource) ? "." : folderResource;
            ExecText = execText;
            WithConnect = withConnect;
            LogRuning = logRuning;
        }

        private static int CountExec = 0;
        public string Exec()
        {
            try
            {
                var countExec = (CountExec++).ToString();
                var execText = TemplareRun
                    .Replace("##0##", countExec)
                    .Replace("##1##", ExecText)
                    .Replace("##2##", FolderResource)
                    .Replace("##3##", WithConnect ? "false" : "true");

                return EvalCode("DynClass" + countExec, "DynMethod", execText, Setting);
            }
            catch (Exception exp)
            {
                return exp.Message 
                    + (exp.InnerException == null ? "" : Environment.NewLine + exp.InnerException.Message);
            }
        }


        //Code by https://habr.com/ru/post/110999/
        private string EvalCode(string typeName, string methodName, string sourceCode, object argument, List<string> skipDLL = null)
        {
            string output = ":)";
            var compiler = CodeDomProvider.CreateProvider("CSharp");
            var parameters = new CompilerParameters
            {
                CompilerOptions = "/t:library",
                GenerateInMemory = true,
                IncludeDebugInformation = true
            };

            //parameters.ReferencedAssemblies.Add(@"System.dll");
            //parameters.ReferencedAssemblies.Add(@"System.Core.dll");
            //parameters.ReferencedAssemblies.Add(@"System.Drawing.dll");
            //parameters.ReferencedAssemblies.Add(@"System.Windows.Forms.dll");
            //parameters.ReferencedAssemblies.Add(@"C:\W\OnlineCity\SVNSrv\Source\GUITest\bin\Release\GUITest.exe");
            //parameters.ReferencedAssemblies.Add(@"C:\W\OnlineCity\SVNSrv\Source\GUITest\bin\Release\GuideTestGUI.dll");


            var aaa = string.Join(Environment.NewLine, AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).Select(a => a.Location).ToArray());
            //MessageBox.Show(aaa);
            var assemblies = AppDomain.CurrentDomain
                            .GetAssemblies()
                            .Where(a => !a.IsDynamic)
                            .Select(a => a.Location)
                            .Where(a => skipDLL == null || !skipDLL.Any(s => a.EndsWith(s, StringComparison.InvariantCultureIgnoreCase)))
                            //.Where(a => !a.StartsWith(@"C:\Windows"))
                            //.Where(a => !a.Contains(@"\mscorlib."))
                            //.Where(a => !a.Contains(@"c:\program files"))
                            //.Where(a => !a.Contains(@"resources.dll"))
                            .Where(a => !a.EndsWith(@".winmd"))
                            .Select(a => a.ToLower().StartsWith(@"c:\windows") ? Path.GetFileName(a) : a)
                            .ToArray();
            //MessageBox.Show(string.Join(Environment.NewLine, assemblies));
            parameters.ReferencedAssemblies.AddRange(assemblies);

            var results = compiler.CompileAssemblyFromSource(parameters, sourceCode);

            if (!results.Errors.HasErrors)
            {
                var assembly = results.CompiledAssembly;
                var evaluatorType = assembly.GetType(typeName);
                var evaluator = Activator.CreateInstance(evaluatorType);

                output = (string)evaluatorType.InvokeMember(methodName, System.Reflection.BindingFlags.InvokeMethod, null, evaluator, new object[] { argument });

                return output;
            }

            //Заплатка на кривое подключение библиотек: находим ошибки с неудачной загрузкой сборок, исключаем и пробуем ещё раз
            if (skipDLL == null)
            {
                skipDLL = results.Errors.Cast<CompilerError>()
                    .Where(ce => !ce.IsWarning && ce.Line == 0 && ce.ErrorText.EndsWith(".dll\""))
                    .Select(ce => ce.ErrorText.Substring(ce.ErrorText.IndexOf("\"")).Replace("\"", ""))
                    .ToList();

                if (skipDLL.Count > 0)
                {
                    return EvalCode(typeName, methodName, sourceCode, argument, skipDLL);
                }
            }

            output = "Compile error!";

            //находим номер строки с которой вставляли код пользователя, чтобы отображать номер строки в его данных, а не в TemplareRun
            var templareRunLines = TemplareRun.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            int lineStartCode = 0;
            while (lineStartCode < templareRunLines.Length && !templareRunLines[lineStartCode].Contains("##1##")) lineStartCode++;
            if (lineStartCode == templareRunLines.Length) lineStartCode = 0;

            //получаем массив исходника для вытаскивания строк с ошибками
            var sourceCodeLines = sourceCode.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            //Ошибка появляется постоянно только при наличии ошибок компиляции, поэтому не показываем её: 
            // "Заранее определенный тип "System.ObsoleteAttribute" определен в нескольких сборках в глобальном псевдониме; используется определение из "c:\Windows\Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll""
            var resError = results.Errors.Cast<CompilerError>()
                .Where(ce => !ce.ErrorText.Contains("System.ObsoleteAttribute") || results.Errors.Count == 1)
                .Where(ce => !ce.IsWarning)
                .Aggregate(output, (current, ce) => current + 
                    string.Format("\r\n{0}{1}\r\n{2}"
                        , (ce.Line > 0 ? $"line {ce.Line - lineStartCode}: " : "")
                        , (ce.Line > 0 && ce.Line - 1 < sourceCodeLines.Length ? sourceCodeLines[ce.Line - 1] : "")
                        , ce.ErrorText));

            return resError;
        }

    }
}
