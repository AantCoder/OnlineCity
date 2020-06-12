using System;
using System.Windows.Forms;

namespace OC.Chat
{
    public partial class Form1 : Form
    {
        ChatMan Chat;
        public Form1()
        {
            InitializeComponent();
            SCC.Init();
        }

        private void UpdateChats()
        {
            textBoxChat.Text = Chat.GetChat(0);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Chat = new ChatMan();
            var msg = Chat.Login(textBox3.Text, textBox1.Text, textBox2.Text);
            if (msg == null)
            {
                this.groupBox1.Visible = false;
                UpdateChats();

                return;
            }

            MessageBox.Show(msg);           
        }

        private void button3_Click(object sender, EventArgs e)
        {
            UpdateChats();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Chat.Send(0, textBox4.Text);
            UpdateChats();
        }
    }
}
