using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogReader
{
    public partial class LogReaderForm : Form
    {
        private OpenFileDialog FileDialog = new OpenFileDialog();

        private LogServer LogData;

        private List<string> Logins;

        private ContextConnect[] SelectConnects;

        private string TextView;

        public LogReaderForm()
        {
            InitializeComponent();
            checkedListBoxAddFilter.SetItems(new List<string>
            {
                "key=",
                "Exception",
                "kill",
                "PlayInfo",
                "PostingChat",
                "[EE]",
                "[WW]",
                "[--]",
                "[EH]",
                "[RG]",
                "[LG]",
                "[GE]",
            });
            checkedListBoxAddFilter.Changed += _ => UpdateTextLaze();

            checkedListBoxAddExclude.SetItems(new List<string>
            {
                "Network",
                "CheckFiles",
                "PlayInfo",
                "UpdateChat",
                "CheckTicksAdd",
                "[EE]",
                "[WW]",
                "[--]",
                "[EH]",
                "[RG]",
                "[LG]",
                "[GE]",
            });
            checkedListBoxAddExclude.Changed += _ => UpdateTextLaze();
        }

        private void UpdateTextLaze()
        {
            new Thread(() => this.Invoke((Action)(() => UpdateText()))).Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // C:\W\OnlineCity\Разное\Логи 2022-01-04\main
            FileDialog.Multiselect = true;
            if (FileDialog.ShowDialog() == DialogResult.Cancel) return;
            Text = FileDialog.FileName;

            var content = FileDialog.FileNames
                .Select(fn =>
                {
                    DateTime time;
                    try
                    {
                        using (var f = File.OpenText(fn))
                        {
                            var str1 = f.ReadLine();
                            if (string.IsNullOrWhiteSpace(str1)) str1 = f.ReadLine();
                            str1 = str1.Trim();
                            str1 = str1.Substring(0, str1.IndexOf('|')).Trim();
                            LogServer.ParceTime(str1, out time);
                        }
                    }
                    catch(Exception exc)
                    {
                        MessageBox.Show("Check file log " + fn + Environment.NewLine + Environment.NewLine + exc.Message);
                        return null;
                    }
                    return new { fn = fn, time = time };
                })
                .Where(a => a != null)
                .OrderBy(a => a.time)
                .Select(a => File.ReadAllText(a.fn))
                .Aggregate(new StringBuilder(), (sb, fc) => sb.AppendLine(fc));

            LogData = new LogServer(content.ToString());
            LogData.Analize();

            Logins = LogData.Connects.Where(c => !string.IsNullOrEmpty(c.Login)).Select(c => c.Login).Distinct().ToList();

            checkedListBox2.Items.Clear();
            checkedListBox2.Items.Add("<Without login>");
            checkedListBox2.Items.AddRange(Logins.ToArray());
            for (int i = 0; i < checkedListBox2.Items.Count; i++) checkedListBox2.SetItemChecked(i, true);

            UpdateCheckedListBox1();
            //SelectConnects = LogData.Connects.ToArray();
            //checkedListBox1.Items.Clear();
            //checkedListBox1.Items.Add("<General messages>");
            //checkedListBox1.Items.AddRange(SelectConnects);
            //textBox1.Text = TextView = "";
        }

        private void checkedListBox2_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (LogData == null || LogData.Connects == null || checkedListBox1.Items.Count == 0) return;

            Logins = new List<string>();
            for (int i = 0; i < checkedListBox2.Items.Count; i++)
            {
                var check = checkedListBox2.GetItemChecked(i);
                if (e.Index == i) check = e.NewValue == CheckState.Checked;
                if (!check) continue;

                Logins.Add(checkedListBox2.Items[i].ToString());
            }

            UpdateCheckedListBox1();
        }

        private void checkBoxWithException_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCheckedListBox1();
        }

        private void UpdateCheckedListBox1()
        {
            var withException = checkBoxWithException.Checked;

            SelectConnects = LogData.Connects
                .Where(c => string.IsNullOrEmpty(c.Login) && Logins.Contains("<Without login>")
                    || Logins.Contains(c.Login))
                .Where(c => !withException || LogData.Lines.Any(l => l.Context == c && l.WithException))
                .ToArray();

            checkedListBox1.Items.Clear();
            checkedListBox1.Items.Add("<General messages>");
            checkedListBox1.Items.AddRange(SelectConnects);
            textBox1.Text = TextView = "";
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            UpdateText(e.Index, e.NewValue == CheckState.Checked);
        }

        private bool NoUpdate = false;

        private void UpdateText(int indexChanging = -1, bool newValue = false)
        { 
            if (NoUpdate || LogData == null || LogData.Connects == null || checkedListBox1.Items.Count == 0) return;
            if (!int.TryParse(textBox2.Text, out var maxCountLines))
            {
                textBox2.Text = (maxCountLines = 1000).ToString();
            }
            if (!int.TryParse(textBox3.Text, out var skipCountLines))
            {
                textBox3.Text = (skipCountLines = 0).ToString();
            }

            var checkGeneral = checkedListBox1.GetItemChecked(0);
            if (indexChanging == 0) checkGeneral = newValue;

            var connects = new HashSet<ContextConnect>();
            for (int i = 1; i < checkedListBox1.Items.Count; i++)
            {
                var check = checkedListBox1.GetItemChecked(i);
                if (indexChanging == i) check = newValue;
                if (!check) continue;

                connects.Add(SelectConnects[i - 1]);
            }


            var q = LogData.Lines
                .Where(l => string.IsNullOrEmpty(l.Context?.Login) && checkGeneral
                    || connects.Contains(l.Context));

            var filter = checkedListBoxAddFilter.GetSelected();
            if (filter.Count > 0) q = q.Where(l => filter.Any(
                f => l.Content?.Contains(f) == true || f[0] == '[' && f.Length == 4 && l.LogLevel?.Contains(f.Substring(1, 2)) == true));

            var exclude = checkedListBoxAddExclude.GetSelected();
            if (exclude.Count > 0) q = q.Where(l => !exclude.Any(
                f => l.Content?.Contains(f) == true || f[0] == '[' && f.Length == 4 && l.LogLevel?.Contains(f.Substring(1, 2)) == true));

            label4.Text = q.Count().ToString();
            TextView = q
                .Skip(skipCountLines)
                .Take(maxCountLines)
                .Aggregate(new StringBuilder(), (sb, l) => sb.AppendLine(l.ToString()))
                .ToString();
            textBox1.Text = TextView;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            NoUpdate = true;
            var check = checkBox1.Checked;
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, check);
            }
            NoUpdate = false;
            UpdateText();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            NoUpdate = true;
            var check = checkBox2.Checked;
            for (int i = 0; i < checkedListBox2.Items.Count; i++)
            {
                checkedListBox2.SetItemChecked(i, check);
            }
            NoUpdate = false;
            UpdateText();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            UpdateText();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox2.Text, out var maxCountLines))
            {
                textBox2.Text = (maxCountLines = 1000).ToString();
            }
            if (!int.TryParse(textBox3.Text, out var skipCountLines))
            {
                textBox3.Text = (skipCountLines = 0).ToString();
            }
            skipCountLines -= maxCountLines;
            if (skipCountLines < 0) skipCountLines = 0;
            textBox3.Text = skipCountLines.ToString();
            UpdateText();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox2.Text, out var maxCountLines))
            {
                textBox2.Text = (maxCountLines = 1000).ToString();
            }
            if (!int.TryParse(textBox3.Text, out var skipCountLines))
            {
                textBox3.Text = (skipCountLines = 0).ToString();
            }
            skipCountLines += maxCountLines;
            if (skipCountLines < 0) skipCountLines = 0;
            textBox3.Text = skipCountLines.ToString();
            UpdateText();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(TextView);
        }
    }
}
