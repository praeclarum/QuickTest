namespace QuickTest
{
	partial class TestWindow
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose (bool disposing)
		{
			if (disposing && (components != null)) {
				components.Dispose ();
			}
			base.Dispose (disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent ()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestWindow));
			this.Grid = new System.Windows.Forms.DataGridView();
			this.GridMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.deleteTestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.panel1 = new System.Windows.Forms.Panel();
			this.StatusButton = new System.Windows.Forms.Button();
			this.AllImages = new System.Windows.Forms.ImageList(this.components);
			this.Progress = new System.Windows.Forms.ProgressBar();
			this.StatusLabel = new System.Windows.Forms.Label();
			this.panel2 = new System.Windows.Forms.Panel();
			this.button1 = new System.Windows.Forms.Button();
			this.MemberBox = new System.Windows.Forms.ComboBox();
			((System.ComponentModel.ISupportInitialize)(this.Grid)).BeginInit();
			this.GridMenu.SuspendLayout();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// Grid
			// 
			this.Grid.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.Grid.BackgroundColor = System.Drawing.SystemColors.Window;
			this.Grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.Grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.Grid.ContextMenuStrip = this.GridMenu;
			this.Grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Grid.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(227)))), ((int)(((byte)(227)))), ((int)(((byte)(227)))));
			this.Grid.Location = new System.Drawing.Point(0, 53);
			this.Grid.Name = "Grid";
			this.Grid.RowHeadersWidth = 24;
			this.Grid.RowTemplate.Height = 20;
			this.Grid.Size = new System.Drawing.Size(322, 274);
			this.Grid.TabIndex = 2;
			this.Grid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.Grid_CellEndEdit);
			this.Grid.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.Grid_RowsAdded);
			this.Grid.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.Grid_RowsRemoved);
			this.Grid.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Grid_KeyDown);
			// 
			// GridMenu
			// 
			this.GridMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteTestToolStripMenuItem});
			this.GridMenu.Name = "GridMenu";
			this.GridMenu.Size = new System.Drawing.Size(153, 48);
			// 
			// deleteTestToolStripMenuItem
			// 
			this.deleteTestToolStripMenuItem.Name = "deleteTestToolStripMenuItem";
			this.deleteTestToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.deleteTestToolStripMenuItem.Text = "&Delete Test";
			this.deleteTestToolStripMenuItem.Click += new System.EventHandler(this.deleteTestToolStripMenuItem_Click);
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.SystemColors.Info;
			this.panel1.Controls.Add(this.StatusButton);
			this.panel1.Controls.Add(this.Progress);
			this.panel1.Controls.Add(this.StatusLabel);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel1.ForeColor = System.Drawing.SystemColors.InfoText;
			this.panel1.Location = new System.Drawing.Point(0, 30);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(322, 23);
			this.panel1.TabIndex = 8;
			// 
			// StatusButton
			// 
			this.StatusButton.FlatAppearance.BorderColor = System.Drawing.SystemColors.Info;
			this.StatusButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.StatusButton.ImageIndex = 4;
			this.StatusButton.ImageList = this.AllImages;
			this.StatusButton.Location = new System.Drawing.Point(0, 0);
			this.StatusButton.Name = "StatusButton";
			this.StatusButton.Size = new System.Drawing.Size(22, 22);
			this.StatusButton.TabIndex = 3;
			this.StatusButton.UseVisualStyleBackColor = false;
			// 
			// AllImages
			// 
			this.AllImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("AllImages.ImageStream")));
			this.AllImages.TransparentColor = System.Drawing.Color.Magenta;
			this.AllImages.Images.SetKeyName(0, "SychronizeList.bmp");
			this.AllImages.Images.SetKeyName(1, "OK.bmp");
			this.AllImages.Images.SetKeyName(2, "Critical.bmp");
			this.AllImages.Images.SetKeyName(3, "Serious.bmp");
			this.AllImages.Images.SetKeyName(4, "SuccessComplete.bmp");
			this.AllImages.Images.SetKeyName(5, "Information.bmp");
			this.AllImages.Images.SetKeyName(6, "NewDocument.bmp");
			// 
			// Progress
			// 
			this.Progress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.Progress.Location = new System.Drawing.Point(218, 4);
			this.Progress.Name = "Progress";
			this.Progress.Size = new System.Drawing.Size(100, 15);
			this.Progress.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
			this.Progress.TabIndex = 1;
			this.Progress.Visible = false;
			// 
			// StatusLabel
			// 
			this.StatusLabel.AutoSize = true;
			this.StatusLabel.Location = new System.Drawing.Point(23, 4);
			this.StatusLabel.Name = "StatusLabel";
			this.StatusLabel.Size = new System.Drawing.Size(106, 15);
			this.StatusLabel.TabIndex = 2;
			this.StatusLabel.Text = "Results: 0/0 passed";
			// 
			// panel2
			// 
			this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(188)))), ((int)(((byte)(199)))), ((int)(((byte)(216)))));
			this.panel2.Controls.Add(this.button1);
			this.panel2.Controls.Add(this.MemberBox);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel2.Location = new System.Drawing.Point(0, 0);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(322, 30);
			this.panel2.TabIndex = 9;
			// 
			// button1
			// 
			this.button1.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(188)))), ((int)(((byte)(199)))), ((int)(((byte)(216)))));
			this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.button1.ImageIndex = 0;
			this.button1.ImageList = this.AllImages;
			this.button1.Location = new System.Drawing.Point(4, 4);
			this.button1.Margin = new System.Windows.Forms.Padding(0);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(22, 22);
			this.button1.TabIndex = 2;
			this.button1.UseVisualStyleBackColor = false;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// MemberBox
			// 
			this.MemberBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MemberBox.FormattingEnabled = true;
			this.MemberBox.Location = new System.Drawing.Point(37, 3);
			this.MemberBox.Name = "MemberBox";
			this.MemberBox.Size = new System.Drawing.Size(281, 23);
			this.MemberBox.TabIndex = 1;
			// 
			// TestWindow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.Grid);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.panel2);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "TestWindow";
			this.Size = new System.Drawing.Size(322, 327);
			((System.ComponentModel.ISupportInitialize)(this.Grid)).EndInit();
			this.GridMenu.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataGridView Grid;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.ProgressBar Progress;
		private System.Windows.Forms.Label StatusLabel;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.ComboBox MemberBox;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.ImageList AllImages;
		private System.Windows.Forms.Button StatusButton;
		private System.Windows.Forms.ContextMenuStrip GridMenu;
		private System.Windows.Forms.ToolStripMenuItem deleteTestToolStripMenuItem;
	}
}
