using GuideTestGUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using ICSharpCode.AvalonEdit;
using System.Xml;

namespace GUITest
{
    public partial class TestRunForm : Form
    {
        private ICSharpCode.AvalonEdit.TextEditor TextEditor;
        private ElementHost TextEditorHost;
        private string TextBoxCodeText { get => TextEditor.Text; set { TextEditor.Text = value; } }
        private bool TextBoxCodeEnabled { get => TextEditorHost.Enabled; set { TextEditorHost.Enabled = value; } }
        
        public GUITestRimWorldModelSetting Setting { get; set; }
        public event Action OnChangeSetting;
        public string _Folder { get; set; }
        public string Folder 
        {
            get { return _Folder; }
            set 
            {
                if (_Folder == value) return;
                _Folder = value; 
                SetFolder(); 
            }
        }
        public string FileName => Path.Combine(Folder, textBoxFileName.Text + ".src");
        public string SettingFileName => Path.Combine(Folder, "Setting.json");

        public TestRunForm()
        {
            InitializeComponent();

            //  http://avalonedit.net/
            //  https://stackoverflow.com/questions/14170165/how-can-i-add-this-wpf-control-into-my-winform
            TextEditor = new ICSharpCode.AvalonEdit.TextEditor();
            TextEditor.ShowLineNumbers = true;
            TextEditor.FontFamily = new System.Windows.Media.FontFamily("Consolas");
            TextEditor.FontSize = 12.75f;
            var modeFile = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "CSharp-Mode.xshd");
            if (File.Exists(modeFile))
            {
                Stream xshd_stream = File.OpenRead(modeFile);
                XmlTextReader xshd_reader = new XmlTextReader(xshd_stream);
                // Apply the new syntax highlighting definition.
                TextEditor.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(xshd_reader, ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance);
                xshd_reader.Close();
                xshd_stream.Close();
            }
            TextEditorHost = new ElementHost();
            TextEditorHost.Size = new Size(400, 400);
            TextEditorHost.Location = new Point(0, 0);
            TextEditorHost.Dock = DockStyle.Fill;
            TextEditorHost.Child = TextEditor;
            this.tabPage1.Controls.Add(TextEditorHost);

            TextEditor.AllowDrop = true;
            TextEditor.DragEnter += TextEditor_DragEnter;
            TextEditor.Drop += TextEditor_Drop;

            //Второй компонент в Справке
            var TextHelp = new ICSharpCode.AvalonEdit.TextEditor();
            TextHelp.ShowLineNumbers = true;
            TextHelp.FontFamily = new System.Windows.Media.FontFamily("Consolas");
            TextHelp.FontSize = 12.75f;

            TextHelp.SyntaxHighlighting = TextEditor.SyntaxHighlighting;

            var TextHelpHost = new ElementHost();
            TextHelpHost.Size = new Size(400, 400);
            TextHelpHost.Location = new Point(0, 0);
            TextHelpHost.Dock = DockStyle.Fill;
            TextHelpHost.Child = TextHelp;
            this.tabPage3.Controls.Add(TextHelpHost);

            TextHelp.Text = textBoxHelp.Text;
        }

        public TestRunForm(string folder)
            : this()
        {
            Folder = folder;
        }

        private void TextEditor_Drop(object sender, System.Windows.DragEventArgs e)
        {
            var index = TextEditor.SelectionStart;
            if (index < 0) index = 0;
            var txt = "";

            string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            int i;
            for (i = 0; i < s.Length; i++)
                txt += (i > 0 ? ", " : "") + "\"" + Path.GetFileNameWithoutExtension(s[i]) + "\"";

            TextEditor.Text = TextEditor.Text.Insert(index, txt);
            TextEditor.SelectionStart = index + txt.Length;
        }

        private void TextEditor_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = System.Windows.DragDropEffects.All;
            else
                e.Effects = System.Windows.DragDropEffects.None;
        }

        private void SetFolder()
        {
            textBoxFolder.Text = Folder;
            browser.Url = new Uri(Folder);
            if (!File.Exists(SettingFileName))
            {
                if (Setting == null) Setting = new GUITestRimWorldModelSetting();
            }
            else
            {
                var jsonString = File.ReadAllText(SettingFileName);
                var setting = JsonSerializer.Deserialize<GUITestRimWorldModelSetting>(jsonString);
                if (Setting != null) MessageBox.Show("Настройки загружены");
                Setting = setting;
            }
            if (OnChangeSetting != null) OnChangeSetting();
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxFileName.Text))
            {
                MessageBox.Show("Введите имя скрипта");
                return;
            }
            File.WriteAllText(FileName, TextBoxCodeText);
            MessageBox.Show("Сохранено " + FileName);
        }

        private void buttonFileName_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.InitialDirectory = Folder;
            ofd.Filter = "Script|*.src";
            if (ofd.ShowDialog() != DialogResult.Cancel && Path.GetExtension(ofd.FileName).ToLower() == ".src")
            {
                LoadFileScript(ofd.FileName);
            }
        }

        public void LoadFileScript(string fileName)
        {
            Folder = Path.GetDirectoryName(fileName);
            textBoxFileName.Text = Path.GetFileNameWithoutExtension(fileName);
            TextBoxCodeText = File.ReadAllText(fileName);
        }

        private void buttonSettings_Click(object sender, EventArgs e)
        {
            var form = new TestRunSettingForm<GUITestRimWorldModelSetting>(Setting);
            if (form.ShowDialog() == DialogResult.Cancel) return;

            Setting = form.Edit;
            var jsonText = JsonSerializer.Serialize(Setting, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(SettingFileName, jsonText);
            MessageBox.Show("Настройки сохранены");

            if (OnChangeSetting != null) OnChangeSetting();
        }

        private void buttonRunConnect_Click(object sender, EventArgs e)
        {
            var txt = buttonRunConnect.Text;
            buttonRunConnect.Text = "...";

            Run(true, (res) =>
            {
                buttonRunConnect.Text = txt;
            });
        }

        private void buttonRun_Click(object sender, EventArgs e)
        {
            var txt = buttonRun.Text;
            buttonRun.Text = "...";

            Run(false, (res) =>
            {
                buttonRun.Text = txt;
            });
        }

        private string OutputRuningText = "";
        private Thread TestThread = null;
        private Action<string> TestThreadFinish = null;
        private void Run(bool withConnect, Action<string> finish)
        {
            panelTop.Enabled = false;
            TextBoxCodeEnabled = false;
            buttonStop.Visible = true;
            buttonStopNorm.Visible = true;
            textBoxOutput.Text = "";
            tabControl1.SelectedIndex = 1;
            timer1.Enabled = true;
            TestThreadFinish = finish;
            OutputRuningText = "";
            var script = TextBoxCodeText;
            TestThread = new Thread(() =>
            {
                try
                {
                    GuideUI.DisableFinishProcess = false;

                    var fileOutput = Path.Combine(Folder, @"..\TestResult\output.txt");
                    var dirOutput = Path.GetDirectoryName(fileOutput);
                    if (withConnect)
                    {
                        //В этом случае сами удаляем папку с результатом
                        Directory.Delete(dirOutput, true);
                    }
                    Directory.CreateDirectory(dirOutput);

                    var result = new GUIDynamicTestRimWorld(Setting, Folder, script, withConnect
                        , (logMes) => OutputRuningText += logMes + Environment.NewLine).Exec();

                    File.WriteAllText(fileOutput, result);

                    this.Invoke((Action)(() =>
                    {
                        RunFinish(result);
                    }));
                }
                catch(Exception exp)
                {
                    MessageBox.Show(exp.ToString());
                }
                //GuideUI.DisableFinishProcess = false;
            });
            TestThread.IsBackground = true;
            TestThread.Start();
        }
        private void RunFinish(string result)
        { 
            TestThread = null;
            timer1.Enabled = false;

            panelTop.Enabled = true;
            TextBoxCodeEnabled = true;
            buttonStop.Visible = false;
            buttonStopNorm.Visible = false;

            for (int i = 1; i < tabControl1.TabPages.Count; i++)
            {
                var textBox = tabControl1.TabPages[i].Controls[0];
                var fn = GetFileNameOnTabControl(i);
                if (textBox is TextBox)
                    (textBox as TextBox).Text = ReadAllFile(fn);
                else
                    TextBoxCodeText = ReadAllFile(fn);
            }
            if (TestThreadFinish != null) TestThreadFinish(result);
        }

        private void buttonStopNorm_Click(object sender, EventArgs e)
        {
            if (TestThread == null) return;
            try
            {
                TestThread.Abort();
            }
            catch { }
            RunFinish("Abort run");
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (TestThread == null) return;
            try
            {
                GuideUI.DisableFinishProcess = true;
                TestThread.Abort();
            }
            catch { }
            RunFinish("Abort run");
        }

        private string ReadAllFile(string fn)
        {
            var txt = "";
            if (File.Exists(fn))
            {
                try
                {
                    txt = File.ReadAllText(fn);
                }
                catch
                {
                    Thread.Sleep(270);
                    txt = File.ReadAllText(fn);
                }
            }
            return txt;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            textBoxOutput.Text = OutputRuningText;

            for (int i = 2; i < tabControl1.TabPages.Count; i++)
            {
                var textBox = tabControl1.TabPages[i].Controls[0];
                var fn = GetFileNameOnTabControl(i);
                var txt = ReadAllFile(fn);
                if (textBox is TextBox)
                {
                    if ((textBox as TextBox).Text != txt) (textBox as TextBox).Text = txt;
                }
                else
                {
                    if (TextBoxCodeText != txt) TextBoxCodeText = txt;
                }
            }
        }

        private string GetFileNameOnTabControl(int index)
        {
            var textBox = tabControl1.TabPages[index].Controls[0];
            var textBoxName = (textBox as TextBox)?.Name;
            var fn = textBoxName == "textBoxOutput" ? Path.Combine(Folder, @"..\TestResult\output.txt")
                : textBoxName == "textBoxLogMod" ? Path.Combine(Folder, @"..\TestResult\clientModLog.txt")
                : textBoxName == "textBoxLogSrv" ? Path.Combine(Folder, @"..\TestResult\serverLog.txt")
                : textBoxName == "textBoxLogGame" ? Path.Combine(Folder, @"..\TestResult\gameLog.txt")
                : FileName;
            return fn;
        }

        private void buttonToFile_Click(object sender, EventArgs e)
        {
            var fn = GetFileNameOnTabControl(tabControl1.SelectedIndex);
            if (File.Exists(fn)) Process.Start("explorer", $" /select, \"{fn}\"");
        }

        private void buttonFileOpen_Click(object sender, EventArgs e)
        {
            var fn = GetFileNameOnTabControl(tabControl1.SelectedIndex);
            if (File.Exists(fn)) Process.Start(fn);
        }

        private void buttonBuf_Click(object sender, EventArgs e)
        {
            var textBox = tabControl1.TabPages[tabControl1.SelectedIndex].Controls[0];
            var textBoxText = (textBox as TextBox)?.Text ?? TextBoxCodeText;
            if (!string.IsNullOrEmpty(textBoxText)) Clipboard.SetText(textBoxText);
        }
    }
}
