
namespace GUITest
{
    partial class UtilityGorTestForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonUpdateScreenshot = new System.Windows.Forms.Button();
            this.buttonFind = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxFileName = new System.Windows.Forms.TextBox();
            this.textBoxFind = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxOffset = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.pictureBoxZoom = new System.Windows.Forms.PictureBox();
            this.pictureBoxMain = new System.Windows.Forms.PictureBox();
            this.buttonFileName = new System.Windows.Forms.Button();
            this.pictureBoxImage = new System.Windows.Forms.PictureBox();
            this.buttonCreate = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.checkBoxCenter = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.buttonClearTemplate = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxZoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxMain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxImage)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonUpdateScreenshot
            // 
            this.buttonUpdateScreenshot.Location = new System.Drawing.Point(12, 12);
            this.buttonUpdateScreenshot.Name = "buttonUpdateScreenshot";
            this.buttonUpdateScreenshot.Size = new System.Drawing.Size(84, 23);
            this.buttonUpdateScreenshot.TabIndex = 0;
            this.buttonUpdateScreenshot.Text = "Взять скрин";
            this.buttonUpdateScreenshot.UseVisualStyleBackColor = true;
            this.buttonUpdateScreenshot.Click += new System.EventHandler(this.buttonUpdateScreenshot_Click);
            // 
            // buttonFind
            // 
            this.buttonFind.Location = new System.Drawing.Point(117, 12);
            this.buttonFind.Name = "buttonFind";
            this.buttonFind.Size = new System.Drawing.Size(124, 23);
            this.buttonFind.TabIndex = 0;
            this.buttonFind.Text = "Найти изображение";
            this.buttonFind.UseVisualStyleBackColor = true;
            this.buttonFind.Click += new System.EventHandler(this.buttonFind_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(102, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(9, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "|";
            // 
            // textBoxFileName
            // 
            this.textBoxFileName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.textBoxFileName.Location = new System.Drawing.Point(247, 12);
            this.textBoxFileName.Name = "textBoxFileName";
            this.textBoxFileName.Size = new System.Drawing.Size(223, 21);
            this.textBoxFileName.TabIndex = 2;
            // 
            // textBoxFind
            // 
            this.textBoxFind.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.textBoxFind.Location = new System.Drawing.Point(594, 12);
            this.textBoxFind.Name = "textBoxFind";
            this.textBoxFind.Size = new System.Drawing.Size(187, 21);
            this.textBoxFind.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(787, 17);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(9, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "|";
            // 
            // textBoxOffset
            // 
            this.textBoxOffset.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.textBoxOffset.Location = new System.Drawing.Point(953, 12);
            this.textBoxOffset.Name = "textBoxOffset";
            this.textBoxOffset.Size = new System.Drawing.Size(100, 21);
            this.textBoxOffset.TabIndex = 2;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.pictureBoxZoom);
            this.panel1.Controls.Add(this.pictureBoxMain);
            this.panel1.Location = new System.Drawing.Point(12, 41);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1215, 583);
            this.panel1.TabIndex = 3;
            // 
            // pictureBoxZoom
            // 
            this.pictureBoxZoom.Location = new System.Drawing.Point(10, 10);
            this.pictureBoxZoom.Name = "pictureBoxZoom";
            this.pictureBoxZoom.Size = new System.Drawing.Size(240, 240);
            this.pictureBoxZoom.TabIndex = 1;
            this.pictureBoxZoom.TabStop = false;
            this.pictureBoxZoom.Visible = false;
            // 
            // pictureBoxMain
            // 
            this.pictureBoxMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBoxMain.Location = new System.Drawing.Point(0, 0);
            this.pictureBoxMain.Name = "pictureBoxMain";
            this.pictureBoxMain.Size = new System.Drawing.Size(1215, 583);
            this.pictureBoxMain.TabIndex = 0;
            this.pictureBoxMain.TabStop = false;
            this.pictureBoxMain.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMain_MouseDown);
            this.pictureBoxMain.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMain_MouseMove);
            this.pictureBoxMain.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMain_MouseUp);
            // 
            // buttonFileName
            // 
            this.buttonFileName.BackColor = System.Drawing.Color.Transparent;
            this.buttonFileName.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonFileName.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.buttonFileName.Location = new System.Drawing.Point(470, 12);
            this.buttonFileName.Margin = new System.Windows.Forms.Padding(0);
            this.buttonFileName.Name = "buttonFileName";
            this.buttonFileName.Size = new System.Drawing.Size(27, 21);
            this.buttonFileName.TabIndex = 0;
            this.buttonFileName.Text = "...";
            this.buttonFileName.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.buttonFileName.UseVisualStyleBackColor = false;
            this.buttonFileName.Click += new System.EventHandler(this.buttonFileName_Click);
            // 
            // pictureBoxImage
            // 
            this.pictureBoxImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBoxImage.Location = new System.Drawing.Point(528, 0);
            this.pictureBoxImage.Name = "pictureBoxImage";
            this.pictureBoxImage.Size = new System.Drawing.Size(60, 40);
            this.pictureBoxImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxImage.TabIndex = 0;
            this.pictureBoxImage.TabStop = false;
            // 
            // buttonCreate
            // 
            this.buttonCreate.Location = new System.Drawing.Point(1074, 12);
            this.buttonCreate.Name = "buttonCreate";
            this.buttonCreate.Size = new System.Drawing.Size(72, 23);
            this.buttonCreate.TabIndex = 0;
            this.buttonCreate.Text = "Сохранить";
            this.buttonCreate.UseVisualStyleBackColor = true;
            this.buttonCreate.Click += new System.EventHandler(this.buttonCreate_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(1059, 17);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(9, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "|";
            // 
            // checkBoxCenter
            // 
            this.checkBoxCenter.AutoSize = true;
            this.checkBoxCenter.Checked = true;
            this.checkBoxCenter.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxCenter.Enabled = false;
            this.checkBoxCenter.Location = new System.Drawing.Point(872, 16);
            this.checkBoxCenter.Name = "checkBoxCenter";
            this.checkBoxCenter.Size = new System.Drawing.Size(75, 17);
            this.checkBoxCenter.TabIndex = 4;
            this.checkBoxCenter.Text = "от центра";
            this.checkBoxCenter.UseVisualStyleBackColor = true;
            this.checkBoxCenter.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(802, 17);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(64, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "Смещение:";
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(1155, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(72, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "К тестам";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // buttonClearTemplate
            // 
            this.buttonClearTemplate.BackColor = System.Drawing.Color.Transparent;
            this.buttonClearTemplate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonClearTemplate.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.buttonClearTemplate.Location = new System.Drawing.Point(497, 12);
            this.buttonClearTemplate.Margin = new System.Windows.Forms.Padding(0);
            this.buttonClearTemplate.Name = "buttonClearTemplate";
            this.buttonClearTemplate.Size = new System.Drawing.Size(27, 21);
            this.buttonClearTemplate.TabIndex = 0;
            this.buttonClearTemplate.Text = "X";
            this.buttonClearTemplate.UseVisualStyleBackColor = false;
            this.buttonClearTemplate.Click += new System.EventHandler(this.buttonClearTemplate_Click);
            // 
            // UtilityGorTestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1239, 636);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.checkBoxCenter);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.pictureBoxImage);
            this.Controls.Add(this.buttonClearTemplate);
            this.Controls.Add(this.buttonFileName);
            this.Controls.Add(this.textBoxFind);
            this.Controls.Add(this.textBoxOffset);
            this.Controls.Add(this.textBoxFileName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.buttonCreate);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonFind);
            this.Controls.Add(this.buttonUpdateScreenshot);
            this.Name = "UtilityGorTestForm";
            this.Text = "Утилита для тестов GUI";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxZoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxMain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonUpdateScreenshot;
        private System.Windows.Forms.Button buttonFind;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxFileName;
        private System.Windows.Forms.TextBox textBoxFind;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxOffset;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button buttonFileName;
        private System.Windows.Forms.PictureBox pictureBoxImage;
        private System.Windows.Forms.PictureBox pictureBoxMain;
        private System.Windows.Forms.Button buttonCreate;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkBoxCenter;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.PictureBox pictureBoxZoom;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button buttonClearTemplate;
    }
}