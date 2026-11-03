namespace OMTplay
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            flowLayoutPanel1 = new FlowLayoutPanel();
            _buttonSettings = new Panel();
            lblbwidth = new Label();
            help = new Label();
            label3 = new Label();
            _lblAudiochannels = new Label();
            label2 = new Label();
            button1 = new Button();
            _btnFullscreen = new Button();
            _btnExit = new Button();
            _lblStatus = new Label();
            _btnConnect = new Button();
            label1 = new Label();
            _cbAudio = new ComboBox();
            _cbSources = new ComboBox();
            _lblTimestamp = new Label();
            _lblResolution = new Label();
            _lblConnected = new Label();
            _btnRefresh = new Button();
            _btnDisconnect = new Button();
            _audioMeter = new AudioLevelMeter();
            _videoBox = new PictureBox();
            linkLabel1 = new LinkLabel();
            linklabel2 = new LinkLabel();
            flowLayoutPanel1.SuspendLayout();
            _buttonSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_videoBox).BeginInit();
            SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.BackColor = Color.Brown;
            flowLayoutPanel1.Controls.Add(_buttonSettings);
            flowLayoutPanel1.Location = new Point(2, -1);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(1192, 192);
            flowLayoutPanel1.TabIndex = 0;
            // 
            // _buttonSettings
            // 
            _buttonSettings.Anchor = AnchorStyles.None;
            _buttonSettings.BackColor = Color.DimGray;
            _buttonSettings.Controls.Add(lblbwidth);
            _buttonSettings.Controls.Add(help);
            _buttonSettings.Controls.Add(label3);
            _buttonSettings.Controls.Add(_lblAudiochannels);
            _buttonSettings.Controls.Add(label2);
            _buttonSettings.Controls.Add(button1);
            _buttonSettings.Controls.Add(_btnFullscreen);
            _buttonSettings.Controls.Add(_btnExit);
            _buttonSettings.Controls.Add(_lblStatus);
            _buttonSettings.Controls.Add(_btnConnect);
            _buttonSettings.Controls.Add(label1);
            _buttonSettings.Controls.Add(_cbAudio);
            _buttonSettings.Controls.Add(_cbSources);
            _buttonSettings.Controls.Add(_lblTimestamp);
            _buttonSettings.Controls.Add(_lblResolution);
            _buttonSettings.Controls.Add(_lblConnected);
            _buttonSettings.Controls.Add(_btnRefresh);
            _buttonSettings.Controls.Add(_btnDisconnect);
            _buttonSettings.Location = new Point(0, 0);
            _buttonSettings.Margin = new Padding(0);
            _buttonSettings.Name = "_buttonSettings";
            _buttonSettings.Size = new Size(1194, 190);
            _buttonSettings.TabIndex = 0;
            // 
            // lblbwidth
            // 
            lblbwidth.BackColor = SystemColors.Control;
            lblbwidth.Font = new Font("Segoe UI", 11.25F);
            lblbwidth.Location = new Point(175, 46);
            lblbwidth.Name = "lblbwidth";
            lblbwidth.Size = new Size(152, 30);
            lblbwidth.TabIndex = 26;
            lblbwidth.Text = "Bandwidth";
            lblbwidth.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // help
            // 
            help.ForeColor = Color.White;
            help.Location = new Point(361, 155);
            help.Name = "help";
            help.Size = new Size(347, 35);
            help.TabIndex = 25;
            help.Text = "N: next source    P: previous source\r\nF: fullscreen on/off    Esc: leave fullscreen";
            help.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            label3.BackColor = Color.Black;
            label3.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.ForeColor = Color.White;
            label3.Location = new Point(-2, 0);
            label3.Name = "label3";
            label3.Size = new Size(1196, 35);
            label3.TabIndex = 24;
            label3.Text = "OMT / NDI Player";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _lblAudiochannels
            // 
            _lblAudiochannels.BackColor = SystemColors.Control;
            _lblAudiochannels.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            _lblAudiochannels.Location = new Point(8, 118);
            _lblAudiochannels.Name = "_lblAudiochannels";
            _lblAudiochannels.Size = new Size(319, 30);
            _lblAudiochannels.TabIndex = 22;
            _lblAudiochannels.Text = "Audio Channels (max. 8)";
            _lblAudiochannels.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            label2.ForeColor = Color.White;
            label2.Location = new Point(865, 155);
            label2.Name = "label2";
            label2.Size = new Size(183, 23);
            label2.TabIndex = 17;
            label2.Text = "label2";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // button1
            // 
            button1.BackColor = Color.LimeGreen;
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Location = new Point(1094, 159);
            button1.Name = "button1";
            button1.Size = new Size(85, 24);
            button1.TabIndex = 16;
            button1.Text = "settings";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // _btnFullscreen
            // 
            _btnFullscreen.BackColor = SystemColors.Control;
            _btnFullscreen.FlatAppearance.BorderSize = 0;
            _btnFullscreen.FlatStyle = FlatStyle.Flat;
            _btnFullscreen.Location = new Point(865, 55);
            _btnFullscreen.Name = "_btnFullscreen";
            _btnFullscreen.Size = new Size(183, 90);
            _btnFullscreen.TabIndex = 1;
            _btnFullscreen.Text = "Fullscreen / →Esc returns";
            _btnFullscreen.UseVisualStyleBackColor = false;
            // 
            // _btnExit
            // 
            _btnExit.BackColor = Color.FromArgb(192, 0, 0);
            _btnExit.FlatAppearance.BorderSize = 0;
            _btnExit.FlatStyle = FlatStyle.Flat;
            _btnExit.ForeColor = Color.White;
            _btnExit.Location = new Point(1094, 58);
            _btnExit.Name = "_btnExit";
            _btnExit.Size = new Size(90, 90);
            _btnExit.TabIndex = 0;
            _btnExit.Text = "Exit";
            _btnExit.UseVisualStyleBackColor = false;
            // 
            // _lblStatus
            // 
            _lblStatus.BackColor = SystemColors.Control;
            _lblStatus.Font = new Font("Segoe UI", 11.25F);
            _lblStatus.Location = new Point(361, 82);
            _lblStatus.Name = "_lblStatus";
            _lblStatus.Size = new Size(347, 30);
            _lblStatus.TabIndex = 5;
            _lblStatus.Text = "Status:";
            _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _btnConnect
            // 
            _btnConnect.BackColor = Color.PowderBlue;
            _btnConnect.FlatAppearance.BorderSize = 0;
            _btnConnect.FlatStyle = FlatStyle.Flat;
            _btnConnect.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            _btnConnect.Location = new Point(725, 53);
            _btnConnect.Name = "_btnConnect";
            _btnConnect.Size = new Size(90, 90);
            _btnConnect.TabIndex = 3;
            _btnConnect.Text = "Connect";
            _btnConnect.UseVisualStyleBackColor = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 11.25F);
            label1.ForeColor = Color.White;
            label1.Location = new Point(8, 158);
            label1.Name = "label1";
            label1.Size = new Size(52, 20);
            label1.TabIndex = 14;
            label1.Text = "Audio:";
            // 
            // _cbAudio
            // 
            _cbAudio.AccessibleName = "Audio channel selection";
            _cbAudio.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbAudio.FlatStyle = FlatStyle.Flat;
            _cbAudio.Font = new Font("Segoe UI", 11.25F);
            _cbAudio.Items.AddRange(new object[] { "Stereo 1/2", "Stereo 3/4", "Stereo 5/6", "Stereo 7/8", "Mono 1 → L+R", "Mono 2 → L+R", "Mono 3 → L+R", "Mono 4 → L+R", "Mono 5 → L+R", "Mono 6 → L+R", "Mono 7 → L+R", "Mono 8 → L+R" });
            _cbAudio.Location = new Point(72, 155);
            _cbAudio.Name = "_cbAudio";
            _cbAudio.Size = new Size(255, 28);
            _cbAudio.TabIndex = 10;
            _cbAudio.SelectedIndexChanged += AudioSelectionChanged;
            // 
            // _cbSources
            // 
            _cbSources.DropDownWidth = 600;
            _cbSources.FlatStyle = FlatStyle.Flat;
            _cbSources.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            _cbSources.FormattingEnabled = true;
            _cbSources.Location = new Point(453, 122);
            _cbSources.Name = "_cbSources";
            _cbSources.Size = new Size(255, 23);
            _cbSources.TabIndex = 9;
            _cbSources.Text = "select source";
            // 
            // _lblTimestamp
            // 
            _lblTimestamp.BackColor = SystemColors.Control;
            _lblTimestamp.Font = new Font("Segoe UI", 11.25F);
            _lblTimestamp.Location = new Point(8, 82);
            _lblTimestamp.Name = "_lblTimestamp";
            _lblTimestamp.Size = new Size(319, 30);
            _lblTimestamp.TabIndex = 8;
            _lblTimestamp.Text = "Timestamp ";
            _lblTimestamp.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _lblResolution
            // 
            _lblResolution.BackColor = SystemColors.Control;
            _lblResolution.Font = new Font("Segoe UI", 11.25F);
            _lblResolution.Location = new Point(8, 46);
            _lblResolution.Name = "_lblResolution";
            _lblResolution.Size = new Size(152, 30);
            _lblResolution.TabIndex = 7;
            _lblResolution.Text = "Resolution";
            _lblResolution.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _lblConnected
            // 
            _lblConnected.BackColor = SystemColors.Control;
            _lblConnected.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            _lblConnected.ForeColor = Color.Red;
            _lblConnected.Location = new Point(361, 46);
            _lblConnected.Name = "_lblConnected";
            _lblConnected.Size = new Size(347, 30);
            _lblConnected.TabIndex = 6;
            _lblConnected.Text = "Disconnected";
            _lblConnected.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _btnRefresh
            // 
            _btnRefresh.BackColor = SystemColors.Control;
            _btnRefresh.FlatAppearance.BorderSize = 0;
            _btnRefresh.FlatStyle = FlatStyle.Flat;
            _btnRefresh.Font = new Font("Segoe UI", 11.25F);
            _btnRefresh.Location = new Point(361, 117);
            _btnRefresh.Name = "_btnRefresh";
            _btnRefresh.Size = new Size(86, 30);
            _btnRefresh.TabIndex = 4;
            _btnRefresh.Text = "refresh";
            _btnRefresh.UseVisualStyleBackColor = false;
            // 
            // _btnDisconnect
            // 
            _btnDisconnect.BackColor = Color.LightCoral;
            _btnDisconnect.FlatAppearance.BorderSize = 0;
            _btnDisconnect.FlatStyle = FlatStyle.Flat;
            _btnDisconnect.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            _btnDisconnect.Location = new Point(725, 54);
            _btnDisconnect.Name = "_btnDisconnect";
            _btnDisconnect.Size = new Size(90, 90);
            _btnDisconnect.TabIndex = 2;
            _btnDisconnect.Text = "Disconnect";
            _btnDisconnect.UseVisualStyleBackColor = false;
            // 
            // _audioMeter
            // 
            _audioMeter.AccessibleName = "Input audio channels 1 to 8, dBFS";
            _audioMeter.BackColor = Color.DimGray;
            _audioMeter.ForeColor = Color.White;
            _audioMeter.ImeMode = ImeMode.Off;
            _audioMeter.Location = new Point(3, 193);
            _audioMeter.Name = "_audioMeter";
            _audioMeter.PeakHoldMilliseconds = 1500;
            _audioMeter.Size = new Size(147, 565);
            _audioMeter.TabIndex = 19;
            _audioMeter.TabStop = false;
            // 
            // _videoBox
            // 
            _videoBox.BorderStyle = BorderStyle.FixedSingle;
            _videoBox.Location = new Point(159, 193);
            _videoBox.Name = "_videoBox";
            _videoBox.Size = new Size(1030, 565);
            _videoBox.SizeMode = PictureBoxSizeMode.Zoom;
            _videoBox.TabIndex = 10;
            _videoBox.TabStop = false;
            // 
            // linkLabel1
            // 
            linkLabel1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            linkLabel1.BackColor = SystemColors.ControlDark;
            linkLabel1.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            linkLabel1.ForeColor = Color.Black;
            linkLabel1.LinkArea = new LinkArea(29, 11);
            linkLabel1.Location = new Point(3, 760);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(609, 24);
            linkLabel1.TabIndex = 20;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "Open Media Transport (OMT) · MIT License";
            linkLabel1.TextAlign = ContentAlignment.MiddleLeft;
            linkLabel1.UseCompatibleTextRendering = true;
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // linklabel2
            // 
            linklabel2.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            linklabel2.BackColor = SystemColors.ControlDark;
            linklabel2.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            linklabel2.ForeColor = Color.Black;
            linklabel2.LinkArea = new LinkArea(48, 18);
            linklabel2.Location = new Point(603, 760);
            linklabel2.Name = "linklabel2";
            linklabel2.Size = new Size(591, 24);
            linklabel2.TabIndex = 21;
            linklabel2.TabStop = true;
            linklabel2.Text = "NDI® is a registered trademark of Vizrt NDI AB · https://ndi.video/";
            linklabel2.TextAlign = ContentAlignment.MiddleRight;
            linklabel2.UseCompatibleTextRendering = true;
            linklabel2.LinkClicked += linklabel2_LinkClicked;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(1196, 784);
            Controls.Add(_audioMeter);
            Controls.Add(_videoBox);
            Controls.Add(linklabel2);
            Controls.Add(linkLabel1);
            Controls.Add(flowLayoutPanel1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(1036, 716);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            Load += Form1_Load;
            flowLayoutPanel1.ResumeLayout(false);
            _buttonSettings.ResumeLayout(false);
            _buttonSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_videoBox).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private FlowLayoutPanel flowLayoutPanel1;
        private Panel _buttonSettings;
        private ComboBox _cbSources;
        private ComboBox _cbAudio;
        private AudioLevelMeter _audioMeter;
        private Label _lblTimestamp;
        private Label _lblResolution;
        private Label _lblConnected;
        private Label _lblStatus;
        private Button _btnRefresh;
        private Button _btnConnect;
        private Button _btnDisconnect;
        private Button _btnFullscreen;
        private Button _btnExit;
        private PictureBox _videoBox;
        private Label label1;
        private Button button1;
        private Label label2;
        private LinkLabel linkLabel1;
        private LinkLabel linklabel2;
        private Label _lblAudiochannels;
        private Label label3;
        private Label help;
        private Label lblbwidth;
    }
}
