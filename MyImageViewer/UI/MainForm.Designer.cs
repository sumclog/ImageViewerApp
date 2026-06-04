namespace MyImageViewer.UI
{
    partial class MainForm
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            statusBar = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            toolStripStatusLabel2 = new ToolStripStatusLabel();
            splitContainer1 = new SplitContainer();
            splitContainer2 = new SplitContainer();
            treeFolders = new TreeView();
            pictureBoxPreview = new PictureBox();
            splitContainer3 = new SplitContainer();
            panelSearch = new Panel();
            btnClearSearch = new Button();
            txtSearch = new TextBox();
            lblSearch = new Label();
            listThumbs = new ListView();
            imgListThumbs = new ImageList(components);
            thumbWorker = new System.ComponentModel.BackgroundWorker();
            statusBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer3).BeginInit();
            splitContainer3.Panel1.SuspendLayout();
            splitContainer3.Panel2.SuspendLayout();
            splitContainer3.SuspendLayout();
            panelSearch.SuspendLayout();
            SuspendLayout();
            // 
            // statusBar
            // 
            statusBar.ImageScalingSize = new Size(20, 20);
            statusBar.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1, toolStripStatusLabel2 });
            statusBar.Location = new Point(0, 727);
            statusBar.Name = "statusBar";
            statusBar.Size = new Size(1382, 26);
            statusBar.TabIndex = 2;
            statusBar.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(52, 20);
            toolStripStatusLabel1.Text = "lblInfo";
            // 
            // toolStripStatusLabel2
            // 
            toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            toolStripStatusLabel2.Size = new Size(53, 20);
            toolStripStatusLabel2.Text = "lblSize";
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(splitContainer2);
            splitContainer1.Panel1.Paint += splitContainer1_Panel1_Paint;
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(splitContainer3);
            splitContainer1.Panel2.Paint += splitContainer1_Panel2_Paint;
            splitContainer1.Size = new Size(1382, 727);
            splitContainer1.SplitterDistance = 413;
            splitContainer1.TabIndex = 3;
            splitContainer1.SplitterMoved += splitContainer1_SplitterMoved;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            splitContainer2.Orientation = Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(treeFolders);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(pictureBoxPreview);
            splitContainer2.Size = new Size(413, 727);
            splitContainer2.SplitterDistance = 374;
            splitContainer2.TabIndex = 1;
            // 
            // treeFolders
            // 
            treeFolders.Dock = DockStyle.Fill;
            treeFolders.Location = new Point(0, 0);
            treeFolders.Name = "treeFolders";
            treeFolders.Size = new Size(413, 374);
            treeFolders.TabIndex = 0;
            treeFolders.AfterSelect += treeFolders_AfterSelect;
            // 
            // pictureBoxPreview
            // 
            pictureBoxPreview.Dock = DockStyle.Fill;
            pictureBoxPreview.Location = new Point(0, 0);
            pictureBoxPreview.Name = "pictureBoxPreview";
            pictureBoxPreview.Size = new Size(413, 349);
            pictureBoxPreview.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxPreview.TabIndex = 0;
            pictureBoxPreview.TabStop = false;
            // 
            // splitContainer3
            // 
            splitContainer3.Dock = DockStyle.Fill;
            splitContainer3.Location = new Point(0, 0);
            splitContainer3.Name = "splitContainer3";
            splitContainer3.Orientation = Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            splitContainer3.Panel1.Controls.Add(panelSearch);
            // 
            // splitContainer3.Panel2
            // 
            splitContainer3.Panel2.Controls.Add(listThumbs);
            splitContainer3.Size = new Size(965, 727);
            splitContainer3.SplitterDistance = 79;
            splitContainer3.TabIndex = 2;
            // 
            // panelSearch
            // 
            panelSearch.Controls.Add(btnClearSearch);
            panelSearch.Controls.Add(txtSearch);
            panelSearch.Controls.Add(lblSearch);
            panelSearch.Dock = DockStyle.Fill;
            panelSearch.Location = new Point(0, 0);
            panelSearch.Name = "panelSearch";
            panelSearch.Size = new Size(965, 79);
            panelSearch.TabIndex = 1;
            // 
            // btnClearSearch
            // 
            btnClearSearch.Location = new Point(492, 12);
            btnClearSearch.Name = "btnClearSearch";
            btnClearSearch.Size = new Size(40, 23);
            btnClearSearch.TabIndex = 2;
            btnClearSearch.Text = "×";
            btnClearSearch.UseVisualStyleBackColor = true;
            btnClearSearch.Click += btnClearSearch_Click;
            // 
            // txtSearch
            // 
            txtSearch.Location = new Point(71, 10);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new Size(415, 27);
            txtSearch.TabIndex = 1;
            txtSearch.TextChanged += txtSearch_TextChanged;
            // 
            // lblSearch
            // 
            lblSearch.AutoSize = true;
            lblSearch.Location = new Point(10, 10);
            lblSearch.Name = "lblSearch";
            lblSearch.Size = new Size(55, 20);
            lblSearch.TabIndex = 0;
            lblSearch.Text = "Поиск:";
            // 
            // listThumbs
            // 
            listThumbs.Dock = DockStyle.Fill;
            listThumbs.LargeImageList = imgListThumbs;
            listThumbs.Location = new Point(0, 0);
            listThumbs.Name = "listThumbs";
            listThumbs.Size = new Size(965, 644);
            listThumbs.TabIndex = 0;
            listThumbs.UseCompatibleStateImageBehavior = false;
            listThumbs.SelectedIndexChanged += listThumbs_SelectedIndexChanged;
            listThumbs.DoubleClick += listThumbs_DoubleClick;
            // 
            // imgListThumbs
            // 
            imgListThumbs.ColorDepth = ColorDepth.Depth32Bit;
            imgListThumbs.ImageSize = new Size(96, 96);
            imgListThumbs.TransparentColor = Color.Transparent;
            // 
            // thumbWorker
            // 
            thumbWorker.WorkerReportsProgress = true;
            thumbWorker.WorkerSupportsCancellation = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1382, 753);
            Controls.Add(splitContainer1);
            Controls.Add(statusBar);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Image Viewer";
            statusBar.ResumeLayout(false);
            statusBar.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).EndInit();
            splitContainer3.Panel1.ResumeLayout(false);
            splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer3).EndInit();
            splitContainer3.ResumeLayout(false);
            panelSearch.ResumeLayout(false);
            panelSearch.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private StatusStrip statusBar;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripStatusLabel toolStripStatusLabel2;
        private SplitContainer splitContainer1;
        private Panel panelSearch;
        private Button btnClearSearch;
        private TextBox txtSearch;
        private Label lblSearch;
        private TreeView treeFolders;
        private ListView listThumbs;
        private System.ComponentModel.BackgroundWorker thumbWorker;
        private ImageList imgListThumbs;
        private SplitContainer splitContainer2;
        private SplitContainer splitContainer3;
        private PictureBox pictureBoxPreview;
    }
}