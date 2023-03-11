using GuideTestGUI;
using Sidekick.Sidekick.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GUITest
{
    public partial class UtilityGorTestForm : Form
    {
        public string WindowsTitle = null; // new GUITestRimWorldModelSetting().WindowsTitle;// "RimWorld by Ludeon Studios";
        public OCVImage Template { get; private set; }
        public GuideUI Game { get; private set; }
        public Point FindedPos { get; private set; }
        public Point Offset { get; private set; }
        public TestRunForm CodeForm { get; set; }

        public UtilityGorTestForm()
        {
            InitializeComponent();
            Text = Path.Combine(Directory.GetCurrentDirectory(), "Resource");
            CodeForm = new TestRunForm(Text);
            CodeForm.OnChangeSetting += () => ChangeSetting();
            ChangeSetting(true);
        }
        public UtilityGorTestForm(string fileScript)
            : this()
        {
            if (fileScript == null) return;
            CodeForm.Show();
            CodeForm.Activate();
            var fi = new FileInfo(fileScript);
            if (fi.Exists)
            {
                CodeForm.LoadFileScript(fi.FullName);
            }
            this.WindowState = FormWindowState.Minimized;
        }

        private void ChangeSetting(bool update = false)
        {
            var newTitle = WindowsTitle;
            if (!string.IsNullOrEmpty(CodeForm.Setting?.WindowsTitle)) newTitle = CodeForm.Setting.WindowsTitle;
            if (newTitle != WindowsTitle)
            {
                WindowsTitle = newTitle;
                Game = new GuideUI();
                Game.ConnectProcess(WindowsTitle);
                ClearFinded();
            }
        }

        private void buttonUpdateScreenshot_Click(object sender, EventArgs e)
        {
            if (Game.WorkProcess == null)
            {
                Game = new GuideUI();
                if (Game.ConnectProcess(WindowsTitle) == null)
                {
                    MessageBox.Show("Запустите " + WindowsTitle);
                    return;
                }
            }
            var txt = buttonUpdateScreenshot.Text;
            buttonUpdateScreenshot.Text = "...";
            this.Refresh();
            Game.Graphics.CheckForegroundWindow();
            Game.Graphics.UpdateScreenshot();
            buttonUpdateScreenshot.Text = txt;
            this.Activate();
            pictureBoxMain.Image = Game.Graphics.Screenshot.Image;
        }

        private void buttonFileName_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.InitialDirectory = Text;
            ofd.Filter = "Images|*.png";
            if (ofd.ShowDialog() != DialogResult.Cancel && Path.GetExtension(ofd.FileName).ToLower() == ".png")
            {
                Text = Path.GetDirectoryName(ofd.FileName);
                CodeForm.Folder = Text;
                if (!string.IsNullOrEmpty(CodeForm.Setting?.WindowsTitle)) WindowsTitle = CodeForm.Setting.WindowsTitle;
                textBoxFileName.Text = Path.GetFileNameWithoutExtension(ofd.FileName);
                pictureBoxImage.Image = new Bitmap(Path.Combine(Text, textBoxFileName.Text + ".png"));
            }
        }

        private void ClearFinded()
        {
            textBoxFileName.Text = "";
            pictureBoxImage.Image = null;
            Template = null;
            FindedPos = PointExt.Null;
            Offset = new Point();
            textBoxOffset.Text = "";
            checkBoxCenter.Checked = false;
            checkBoxCenter.Enabled = false;
            RepaintUp();
        }

        private void buttonClearTemplate_Click(object sender, EventArgs e)
        {
            ClearFinded();
        }

        private void buttonFind_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxFileName.Text) || !File.Exists(Path.Combine(Text, textBoxFileName.Text + ".png")))
            {
                buttonFileName_Click(sender, e);
                return;
            }
            Template = new OCVImage() { Image = new Bitmap(Path.Combine(Text, textBoxFileName.Text + ".png")) };
            pictureBoxImage.Image = Template.Image;
            FindedPos = Game.Graphics.Find(Template, center: false);
            Offset = new Point();
            textBoxOffset.Text = "";
            if (!checkBoxCenter.Enabled)
            {
                checkBoxCenter.Checked = true;
                checkBoxCenter.Enabled = true;
            }
            RepaintUp();
        }

        private void RepaintUp()
        {
            if (Game.Graphics?.Screenshot == null) return;

            var image = new Bitmap(pictureBoxMain.Width, pictureBoxMain.Height);
            Graphics GH = Graphics.FromImage(image);
            GH.DrawImage(Game.Graphics.Screenshot.Image, 0, 0);

            if (!FindedPos.IsNull())
            {
                var width = 10;
                GH.DrawRectangle(new Pen(Color.FromArgb(100, 200, 200, 200), width)
                    , FindedPos.X - 1 - width / 2, FindedPos.Y - 1 - width / 2, Template.Width + 1 + width, Template.Height + 1 + width);

                GH.DrawRectangle(new Pen(Color.Red)
                    , FindedPos.X - 1, FindedPos.Y - 1, Template.Width + 1, Template.Height + 1);


                pictureBoxImage.Image = Template.Image;

                textBoxFind.Text = $"{FindedPos.X}, {FindedPos.Y}  {Template.Width}, {Template.Height}";
            }
            else
                textBoxFind.Text = "Нет";

            if (!Offset.IsEmpty)
            {
                var len = 10;
                var point = FindedPos.IsNull() ? new Point() : FindedPos;
                GH.DrawLine(new Pen(Color.Red)
                    , point.X + Offset.X, point.Y + Offset.Y - len, point.X + Offset.X, point.Y + Offset.Y + len);
                GH.DrawLine(new Pen(Color.Red)
                    , point.X + Offset.X - len, point.Y + Offset.Y, point.X + Offset.X + len, point.Y + Offset.Y);
            }

            pictureBoxMain.Image = image;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (Template == null) return;

            var offset = checkBoxCenter.Checked
                ? new Point(Offset.X - (Template.Width >> 1), Offset.Y - (Template.Height >> 1))
                : Offset;
            textBoxOffset.Text = $"{offset.X}, {offset.Y}";

            RepaintUp();
        }

        private void buttonCreate_Click(object sender, EventArgs e)
        {
            if (Template == null) return;

            var sfd = new SaveFileDialog();
            sfd.Filter = "Images|*.png";
            sfd.InitialDirectory = Text;
            sfd.AddExtension = true;
            sfd.DefaultExt = ".png";
            sfd.FileName = textBoxFileName.Text + ".png";
            if (sfd.ShowDialog() != DialogResult.Cancel && Path.GetExtension(sfd.FileName).ToLower() == ".png")
            {
                Text = Path.GetDirectoryName(sfd.FileName);
                CodeForm.Folder = Text;
                if (!string.IsNullOrEmpty(CodeForm.Setting?.WindowsTitle)) WindowsTitle = CodeForm.Setting.WindowsTitle;
                textBoxFileName.Text = Path.GetFileNameWithoutExtension(sfd.FileName);

                Template.Image.Save(sfd.FileName);
            }
        }

        private bool MouseIsDown = false;
        private Point DownMousePos;
        private Point CurrentMousePos;

        private void pictureBoxMain_MouseDown(object sender, MouseEventArgs e)
        {
            DownMousePos = e.Location;
            MouseIsDown = true;
        }

        private void pictureBoxMain_MouseMove(object sender, MouseEventArgs e)
        {
            const int zoomOutWidth = 24;
            CurrentMousePos = e.Location;
            if (Game?.Graphics?.Screenshot != null)
            {
                if (!pictureBoxZoom.Visible) pictureBoxZoom.Visible = true;

                var zoomSelect = new Rectangle(CurrentMousePos.X - zoomOutWidth / 2, CurrentMousePos.Y - zoomOutWidth / 2, zoomOutWidth, zoomOutWidth);
                var zoomEnd = new Rectangle(0, 0, pictureBoxZoom.Width, pictureBoxZoom.Height);
                var image = new Bitmap(pictureBoxZoom.Width, pictureBoxZoom.Height);

                Graphics GH = Graphics.FromImage(image);
                GH.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                GH.DrawImage(Game.Graphics.Screenshot.Image, zoomEnd, zoomSelect, GraphicsUnit.Pixel);

                GH.DrawLine(new Pen(Color.White), pictureBoxZoom.Width / 2, 0, pictureBoxZoom.Width / 2, pictureBoxZoom.Height);
                GH.DrawLine(new Pen(Color.White), 0, pictureBoxZoom.Height / 2, pictureBoxZoom.Width, pictureBoxZoom.Height / 2);
                GH.DrawRectangle(new Pen(Color.Black), 0, 0, pictureBoxZoom.Width - 1, pictureBoxZoom.Height - 1);
                GH.DrawRectangle(new Pen(Color.White), 1, 1, pictureBoxZoom.Width - 3, pictureBoxZoom.Height - 3);

                if (pictureBoxZoom.Top > pictureBoxMain.Height / 2
                    && CurrentMousePos.Y > pictureBoxMain.Height / 2)
                    pictureBoxZoom.Top = 10;
                if (pictureBoxZoom.Top < pictureBoxMain.Height / 2
                    && CurrentMousePos.Y < pictureBoxMain.Height / 2)
                    pictureBoxZoom.Top = pictureBoxMain.Height - pictureBoxZoom.Height - 10;
                if (pictureBoxZoom.Left > pictureBoxMain.Width / 2
                    && CurrentMousePos.X > pictureBoxMain.Width / 2)
                    pictureBoxZoom.Left = 10;
                if (pictureBoxZoom.Left < pictureBoxMain.Width / 2
                    && CurrentMousePos.X < pictureBoxMain.Width / 2)
                    pictureBoxZoom.Left = pictureBoxMain.Width - pictureBoxZoom.Width - 10;

                pictureBoxZoom.Image = image;
            }

            if (MouseIsDown)
            {
                NewSelect();
            }
        }
        private void pictureBoxMain_MouseUp(object sender, MouseEventArgs e)
        {
            if (!MouseIsDown) return;
            CurrentMousePos = e.Location;
            MouseIsDown = false;
            if (!NewSelect())
            {
                //если это не выделение, а клик, то это установка смещения
                if (!FindedPos.IsNull() && Template != null)
                {
                    Offset = new Point(CurrentMousePos.X - FindedPos.X, CurrentMousePos.Y - FindedPos.Y);

                    var offset = checkBoxCenter.Checked
                        ? new Point(Offset.X - (Template.Width >> 1), Offset.Y - (Template.Height >> 1))
                        : Offset;
                    textBoxOffset.Text = $"{offset.X}, {offset.Y}";
                }
                else
                {
                    //если шаблона нет, то смещение задается от угла изображения (просто координаты)
                    Offset = new Point(CurrentMousePos.X, CurrentMousePos.Y);

                    textBoxOffset.Text = $"{Offset.X}, {Offset.Y}";
                }

                RepaintUp();
            }
        }

        private Rectangle GetSelectArea()
        {
            var res = new Rectangle();
            if (CurrentMousePos.X < DownMousePos.X)
            {
                res.X = CurrentMousePos.X;
                res.Width = DownMousePos.X - CurrentMousePos.X + 1;
            }
            else
            {
                res.X = DownMousePos.X;
                res.Width = CurrentMousePos.X - DownMousePos.X + 1;
            }
            if (CurrentMousePos.Y < DownMousePos.Y)
            {
                res.Y = CurrentMousePos.Y;
                res.Height = DownMousePos.Y - CurrentMousePos.Y + 1;
            }
            else
            {
                res.Y = DownMousePos.Y;
                res.Height = CurrentMousePos.Y - DownMousePos.Y + 1;
            }
            return res;
        }

        private bool NewSelect()
        {
            if (Game.Graphics?.Screenshot == null) return false;

            var select = GetSelectArea();
            if (select.Width <= 1 || select.Height <= 1) return false;

            var image = new Bitmap(select.Width, select.Height);
            Graphics GH = Graphics.FromImage(image);
            GH.DrawImage(Game.Graphics.Screenshot.Image, new Rectangle(new Point(), image.Size), select, GraphicsUnit.Pixel);

            Template = new OCVImage() { Image = image };
            FindedPos = new Point(select.X, select.Y);
            Offset = new Point();
            textBoxOffset.Text = "";
            if (!checkBoxCenter.Enabled)
            {
                checkBoxCenter.Checked = true;
                checkBoxCenter.Enabled = true;
            }
            RepaintUp();

            return true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CodeForm.Show();
            CodeForm.Activate();
        }

    }
}
