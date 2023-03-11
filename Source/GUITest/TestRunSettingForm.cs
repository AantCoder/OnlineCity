using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GUITest
{
    public partial class TestRunSettingForm<T> : Form
        where T : ICloneable
    {
        public T Edit;
        private T Original;

        public TestRunSettingForm()
        {
            InitializeComponent();
        }

        public TestRunSettingForm(T edit)
            : this()
        {
            Original = edit;
            Edit = (T)edit.Clone();
            propertyGrid1.SelectedObject = Edit;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            Edit = Original;
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
