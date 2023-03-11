
namespace GUITest
{
    partial class TestRunForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestRunForm));
            this.buttonFileName = new System.Windows.Forms.Button();
            this.textBoxFileName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonSettings = new System.Windows.Forms.Button();
            this.textBoxFolder = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.browser = new System.Windows.Forms.WebBrowser();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.buttonStop = new System.Windows.Forms.Button();
            this.buttonStopNorm = new System.Windows.Forms.Button();
            this.tabPageOutput = new System.Windows.Forms.TabPage();
            this.textBoxOutput = new System.Windows.Forms.TextBox();
            this.tabPageLogMod = new System.Windows.Forms.TabPage();
            this.textBoxLogMod = new System.Windows.Forms.TextBox();
            this.tabPageLogSrv = new System.Windows.Forms.TabPage();
            this.textBoxLogSrv = new System.Windows.Forms.TextBox();
            this.tabPageLogGame = new System.Windows.Forms.TabPage();
            this.textBoxLogGame = new System.Windows.Forms.TextBox();
            this.tabControl2 = new System.Windows.Forms.TabControl();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.label3 = new System.Windows.Forms.Label();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.textBoxHelp = new System.Windows.Forms.TextBox();
            this.buttonRun = new System.Windows.Forms.Button();
            this.buttonRunConnect = new System.Windows.Forms.Button();
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonBuf = new System.Windows.Forms.Button();
            this.buttonToFile = new System.Windows.Forms.Button();
            this.buttonFileOpen = new System.Windows.Forms.Button();
            this.panelTop = new System.Windows.Forms.Panel();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPageOutput.SuspendLayout();
            this.tabPageLogMod.SuspendLayout();
            this.tabPageLogSrv.SuspendLayout();
            this.tabPageLogGame.SuspendLayout();
            this.tabControl2.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.panelTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonFileName
            // 
            this.buttonFileName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFileName.BackColor = System.Drawing.Color.Transparent;
            this.buttonFileName.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonFileName.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.buttonFileName.Location = new System.Drawing.Point(950, 11);
            this.buttonFileName.Margin = new System.Windows.Forms.Padding(0);
            this.buttonFileName.Name = "buttonFileName";
            this.buttonFileName.Size = new System.Drawing.Size(27, 21);
            this.buttonFileName.TabIndex = 3;
            this.buttonFileName.Text = "...";
            this.buttonFileName.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.buttonFileName.UseVisualStyleBackColor = false;
            this.buttonFileName.Click += new System.EventHandler(this.buttonFileName_Click);
            // 
            // textBoxFileName
            // 
            this.textBoxFileName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxFileName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.textBoxFileName.Location = new System.Drawing.Point(728, 11);
            this.textBoxFileName.Name = "textBoxFileName";
            this.textBoxFileName.Size = new System.Drawing.Size(222, 21);
            this.textBoxFileName.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(85, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Рабочая папка:";
            // 
            // buttonSettings
            // 
            this.buttonSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSettings.BackColor = System.Drawing.Color.Transparent;
            this.buttonSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonSettings.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.buttonSettings.Location = new System.Drawing.Point(986, 11);
            this.buttonSettings.Margin = new System.Windows.Forms.Padding(0);
            this.buttonSettings.Name = "buttonSettings";
            this.buttonSettings.Size = new System.Drawing.Size(109, 22);
            this.buttonSettings.TabIndex = 3;
            this.buttonSettings.Text = "Настройки папок";
            this.buttonSettings.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.buttonSettings.UseVisualStyleBackColor = false;
            this.buttonSettings.Click += new System.EventHandler(this.buttonSettings_Click);
            // 
            // textBoxFolder
            // 
            this.textBoxFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxFolder.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.textBoxFolder.Location = new System.Drawing.Point(105, 11);
            this.textBoxFolder.Name = "textBoxFolder";
            this.textBoxFolder.Size = new System.Drawing.Size(565, 21);
            this.textBoxFolder.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(676, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Скрипт:";
            // 
            // browser
            // 
            this.browser.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.browser.Location = new System.Drawing.Point(4, 19);
            this.browser.MinimumSize = new System.Drawing.Size(20, 20);
            this.browser.Name = "browser";
            this.browser.Size = new System.Drawing.Size(277, 658);
            this.browser.TabIndex = 7;
            this.browser.Url = new System.Uri("", System.UriKind.Relative);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(3, 64);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tabControl1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tabControl2);
            this.splitContainer1.Size = new System.Drawing.Size(1096, 706);
            this.splitContainer1.SplitterDistance = 799;
            this.splitContainer1.TabIndex = 8;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPageOutput);
            this.tabControl1.Controls.Add(this.tabPageLogMod);
            this.tabControl1.Controls.Add(this.tabPageLogSrv);
            this.tabControl1.Controls.Add(this.tabPageLogGame);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(799, 706);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.buttonStop);
            this.tabPage1.Controls.Add(this.buttonStopNorm);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(791, 680);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Код скрипта";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(33, 48);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(222, 23);
            this.buttonStop.TabIndex = 9;
            this.buttonStop.Text = "Прерывать оставив работающим";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Visible = false;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // buttonStopNorm
            // 
            this.buttonStopNorm.Location = new System.Drawing.Point(33, 19);
            this.buttonStopNorm.Name = "buttonStopNorm";
            this.buttonStopNorm.Size = new System.Drawing.Size(222, 23);
            this.buttonStopNorm.TabIndex = 9;
            this.buttonStopNorm.Text = "Прерывать выполнение";
            this.buttonStopNorm.UseVisualStyleBackColor = true;
            this.buttonStopNorm.Visible = false;
            this.buttonStopNorm.Click += new System.EventHandler(this.buttonStopNorm_Click);
            // 
            // tabPageOutput
            // 
            this.tabPageOutput.Controls.Add(this.textBoxOutput);
            this.tabPageOutput.Location = new System.Drawing.Point(4, 22);
            this.tabPageOutput.Name = "tabPageOutput";
            this.tabPageOutput.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageOutput.Size = new System.Drawing.Size(791, 680);
            this.tabPageOutput.TabIndex = 4;
            this.tabPageOutput.Text = "Результат скрипта";
            this.tabPageOutput.UseVisualStyleBackColor = true;
            // 
            // textBoxOutput
            // 
            this.textBoxOutput.AllowDrop = true;
            this.textBoxOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxOutput.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.textBoxOutput.Location = new System.Drawing.Point(3, 3);
            this.textBoxOutput.Multiline = true;
            this.textBoxOutput.Name = "textBoxOutput";
            this.textBoxOutput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxOutput.Size = new System.Drawing.Size(785, 674);
            this.textBoxOutput.TabIndex = 2;
            this.textBoxOutput.WordWrap = false;
            // 
            // tabPageLogMod
            // 
            this.tabPageLogMod.Controls.Add(this.textBoxLogMod);
            this.tabPageLogMod.Location = new System.Drawing.Point(4, 22);
            this.tabPageLogMod.Name = "tabPageLogMod";
            this.tabPageLogMod.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageLogMod.Size = new System.Drawing.Size(791, 680);
            this.tabPageLogMod.TabIndex = 1;
            this.tabPageLogMod.Text = "Результат - лог мода";
            this.tabPageLogMod.UseVisualStyleBackColor = true;
            // 
            // textBoxLogMod
            // 
            this.textBoxLogMod.AllowDrop = true;
            this.textBoxLogMod.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxLogMod.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.textBoxLogMod.Location = new System.Drawing.Point(3, 3);
            this.textBoxLogMod.Multiline = true;
            this.textBoxLogMod.Name = "textBoxLogMod";
            this.textBoxLogMod.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxLogMod.Size = new System.Drawing.Size(785, 674);
            this.textBoxLogMod.TabIndex = 1;
            this.textBoxLogMod.WordWrap = false;
            // 
            // tabPageLogSrv
            // 
            this.tabPageLogSrv.Controls.Add(this.textBoxLogSrv);
            this.tabPageLogSrv.Location = new System.Drawing.Point(4, 22);
            this.tabPageLogSrv.Name = "tabPageLogSrv";
            this.tabPageLogSrv.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageLogSrv.Size = new System.Drawing.Size(791, 680);
            this.tabPageLogSrv.TabIndex = 2;
            this.tabPageLogSrv.Text = "Результат - лог сервера";
            this.tabPageLogSrv.UseVisualStyleBackColor = true;
            // 
            // textBoxLogSrv
            // 
            this.textBoxLogSrv.AllowDrop = true;
            this.textBoxLogSrv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxLogSrv.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.textBoxLogSrv.Location = new System.Drawing.Point(3, 3);
            this.textBoxLogSrv.Multiline = true;
            this.textBoxLogSrv.Name = "textBoxLogSrv";
            this.textBoxLogSrv.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxLogSrv.Size = new System.Drawing.Size(785, 674);
            this.textBoxLogSrv.TabIndex = 1;
            this.textBoxLogSrv.WordWrap = false;
            // 
            // tabPageLogGame
            // 
            this.tabPageLogGame.Controls.Add(this.textBoxLogGame);
            this.tabPageLogGame.Location = new System.Drawing.Point(4, 22);
            this.tabPageLogGame.Name = "tabPageLogGame";
            this.tabPageLogGame.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageLogGame.Size = new System.Drawing.Size(791, 680);
            this.tabPageLogGame.TabIndex = 3;
            this.tabPageLogGame.Text = "Результат - лог игры";
            this.tabPageLogGame.UseVisualStyleBackColor = true;
            // 
            // textBoxLogGame
            // 
            this.textBoxLogGame.AllowDrop = true;
            this.textBoxLogGame.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxLogGame.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.textBoxLogGame.Location = new System.Drawing.Point(3, 3);
            this.textBoxLogGame.Multiline = true;
            this.textBoxLogGame.Name = "textBoxLogGame";
            this.textBoxLogGame.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxLogGame.Size = new System.Drawing.Size(785, 674);
            this.textBoxLogGame.TabIndex = 1;
            this.textBoxLogGame.WordWrap = false;
            // 
            // tabControl2
            // 
            this.tabControl2.Controls.Add(this.tabPage2);
            this.tabControl2.Controls.Add(this.tabPage3);
            this.tabControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl2.Location = new System.Drawing.Point(0, 0);
            this.tabControl2.Name = "tabControl2";
            this.tabControl2.SelectedIndex = 0;
            this.tabControl2.Size = new System.Drawing.Size(293, 706);
            this.tabControl2.TabIndex = 8;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.label3);
            this.tabPage2.Controls.Add(this.browser);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(285, 680);
            this.tabPage2.TabIndex = 0;
            this.tabPage2.Text = "Ресурсы";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 3);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(235, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Перетащите для вставки в позицию курсора";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.textBoxHelp);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(285, 680);
            this.tabPage3.TabIndex = 1;
            this.tabPage3.Text = "Справка";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // textBoxHelp
            // 
            this.textBoxHelp.AllowDrop = true;
            this.textBoxHelp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxHelp.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.textBoxHelp.Location = new System.Drawing.Point(3, 3);
            this.textBoxHelp.Multiline = true;
            this.textBoxHelp.Name = "textBoxHelp";
            this.textBoxHelp.ReadOnly = true;
            this.textBoxHelp.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxHelp.Size = new System.Drawing.Size(279, 674);
            this.textBoxHelp.TabIndex = 3;
            this.textBoxHelp.Text = resources.GetString("textBoxHelp.Text");
            this.textBoxHelp.Visible = false;
            this.textBoxHelp.WordWrap = false;
            // 
            // buttonRun
            // 
            this.buttonRun.Location = new System.Drawing.Point(14, 36);
            this.buttonRun.Name = "buttonRun";
            this.buttonRun.Size = new System.Drawing.Size(174, 23);
            this.buttonRun.TabIndex = 9;
            this.buttonRun.Text = "Выполнить с запуском";
            this.buttonRun.UseVisualStyleBackColor = true;
            this.buttonRun.Click += new System.EventHandler(this.buttonRun_Click);
            // 
            // buttonRunConnect
            // 
            this.buttonRunConnect.Location = new System.Drawing.Point(194, 36);
            this.buttonRunConnect.Name = "buttonRunConnect";
            this.buttonRunConnect.Size = new System.Drawing.Size(174, 23);
            this.buttonRunConnect.TabIndex = 9;
            this.buttonRunConnect.Text = "Выполнить с присоединением";
            this.buttonRunConnect.UseVisualStyleBackColor = true;
            this.buttonRunConnect.Click += new System.EventHandler(this.buttonRunConnect_Click);
            // 
            // buttonSave
            // 
            this.buttonSave.Location = new System.Drawing.Point(374, 36);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(76, 23);
            this.buttonSave.TabIndex = 9;
            this.buttonSave.Text = "Сохранить";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // buttonBuf
            // 
            this.buttonBuf.BackColor = System.Drawing.Color.Transparent;
            this.buttonBuf.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonBuf.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.buttonBuf.Location = new System.Drawing.Point(720, 62);
            this.buttonBuf.Margin = new System.Windows.Forms.Padding(0);
            this.buttonBuf.Name = "buttonBuf";
            this.buttonBuf.Size = new System.Drawing.Size(69, 21);
            this.buttonBuf.TabIndex = 3;
            this.buttonBuf.Text = "В буфер";
            this.buttonBuf.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.buttonBuf.UseVisualStyleBackColor = false;
            this.buttonBuf.Click += new System.EventHandler(this.buttonBuf_Click);
            // 
            // buttonToFile
            // 
            this.buttonToFile.BackColor = System.Drawing.Color.Transparent;
            this.buttonToFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonToFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.buttonToFile.Location = new System.Drawing.Point(645, 62);
            this.buttonToFile.Margin = new System.Windows.Forms.Padding(0);
            this.buttonToFile.Name = "buttonToFile";
            this.buttonToFile.Size = new System.Drawing.Size(69, 21);
            this.buttonToFile.TabIndex = 3;
            this.buttonToFile.Text = "К файлу";
            this.buttonToFile.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.buttonToFile.UseVisualStyleBackColor = false;
            this.buttonToFile.Click += new System.EventHandler(this.buttonToFile_Click);
            // 
            // buttonFileOpen
            // 
            this.buttonFileOpen.BackColor = System.Drawing.Color.Transparent;
            this.buttonFileOpen.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonFileOpen.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.buttonFileOpen.Location = new System.Drawing.Point(570, 62);
            this.buttonFileOpen.Margin = new System.Windows.Forms.Padding(0);
            this.buttonFileOpen.Name = "buttonFileOpen";
            this.buttonFileOpen.Size = new System.Drawing.Size(69, 21);
            this.buttonFileOpen.TabIndex = 3;
            this.buttonFileOpen.Text = "Открыть";
            this.buttonFileOpen.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.buttonFileOpen.UseVisualStyleBackColor = false;
            this.buttonFileOpen.Click += new System.EventHandler(this.buttonFileOpen_Click);
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.buttonSave);
            this.panelTop.Controls.Add(this.buttonRunConnect);
            this.panelTop.Controls.Add(this.buttonRun);
            this.panelTop.Controls.Add(this.buttonSettings);
            this.panelTop.Controls.Add(this.textBoxFileName);
            this.panelTop.Controls.Add(this.label2);
            this.panelTop.Controls.Add(this.textBoxFolder);
            this.panelTop.Controls.Add(this.label1);
            this.panelTop.Controls.Add(this.buttonFileName);
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1119, 60);
            this.panelTop.TabIndex = 10;
            // 
            // timer1
            // 
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // TestRunForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1103, 771);
            this.Controls.Add(this.buttonToFile);
            this.Controls.Add(this.buttonBuf);
            this.Controls.Add(this.buttonFileOpen);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.splitContainer1);
            this.Name = "TestRunForm";
            this.Text = "Скрипты";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPageOutput.ResumeLayout(false);
            this.tabPageOutput.PerformLayout();
            this.tabPageLogMod.ResumeLayout(false);
            this.tabPageLogMod.PerformLayout();
            this.tabPageLogSrv.ResumeLayout(false);
            this.tabPageLogSrv.PerformLayout();
            this.tabPageLogGame.ResumeLayout(false);
            this.tabPageLogGame.PerformLayout();
            this.tabControl2.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonFileName;
        private System.Windows.Forms.TextBox textBoxFileName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonSettings;
        private System.Windows.Forms.TextBox textBoxFolder;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.WebBrowser browser;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button buttonRun;
        private System.Windows.Forms.Button buttonRunConnect;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPageLogMod;
        private System.Windows.Forms.TextBox textBoxLogMod;
        private System.Windows.Forms.TabPage tabPageLogSrv;
        private System.Windows.Forms.TextBox textBoxLogSrv;
        private System.Windows.Forms.TabPage tabPageLogGame;
        private System.Windows.Forms.TextBox textBoxLogGame;
        private System.Windows.Forms.Button buttonBuf;
        private System.Windows.Forms.Button buttonToFile;
        private System.Windows.Forms.TabPage tabPageOutput;
        private System.Windows.Forms.TextBox textBoxOutput;
        private System.Windows.Forms.Button buttonFileOpen;
        private System.Windows.Forms.TabControl tabControl2;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TextBox textBoxHelp;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Button buttonStopNorm;
    }
}