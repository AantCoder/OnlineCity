using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogReader
{
    public partial class CheckedListBoxAdd : UserControl
    {
        public List<string> Items { get; private set; }

        public string Tile { get => label1.Text; set { label1.Text = value; } }

        public event Action<List<string>> Changed;

        private bool Changing = false;

        public CheckedListBoxAdd()
        {
            InitializeComponent();
            Items = new List<string>();
        }

        public void SetItems(List<string> items)
        {
            Items = items;
            UpdateItems();
        }

        public void UpdateItems()
        {
            Changing = true;
            checkedListBox1.Items.Clear();
            checkedListBox1.Items.AddRange(Items.ToArray());
            Changing = false;

            if (Changed == null) return;
            Changed(GetSelected());
        }

        public List<string> GetSelected()
        {
            return GetSelected(-1, false);
        }

        private List<string> GetSelected(int index, bool newValue)
        {
            var res = new List<string>();
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                var check = checkedListBox1.GetItemChecked(i);
                if (index == i) check = newValue;
                if (!check) continue;

                res.Add(checkedListBox1.Items[i].ToString());
            }
            return res;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text)) return;
            Items.Add(textBox1.Text);
            checkedListBox1.Items.Add(textBox1.Text);
        }


        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (Changing || Changed == null) return;
            Changed(GetSelected(e.Index, e.NewValue == CheckState.Checked));
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            Changing = true;
            var check = checkBox2.Checked;
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, check);
            }
            Changing = false;

            if (Changed == null) return;
            Changed(GetSelected());
        }
    }
}
