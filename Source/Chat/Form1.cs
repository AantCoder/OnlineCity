using RimWorldOnlineCity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Chat
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
            if (Chat.Login(textBox3.Text, textBox1.Text, textBox2.Text) == null)
            {
                this.groupBox1.Visible = false;
                UpdateChats();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            UpdateChats();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Chat.Send(0, textBox4.Text, UpdateChats);
        }
    }
}
