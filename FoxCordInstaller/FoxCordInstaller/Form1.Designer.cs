namespace FoxCordInstaller
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
            cancel = new Button();
            progressBar = new ProgressBar();
            pictureBox1 = new PictureBox();
            installlab = new Label();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // cancel
            // 
            cancel.Cursor = Cursors.Hand;
            cancel.Font = new Font("Segoe UI", 10.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            cancel.ForeColor = SystemColors.ControlDarkDark;
            cancel.Location = new Point(338, 387);
            cancel.Name = "cancel";
            cancel.Size = new Size(135, 51);
            cancel.TabIndex = 0;
            cancel.Text = "Cancel";
            cancel.UseVisualStyleBackColor = true;
            cancel.Click += cancel_Click;
            // 
            // progressBar
            // 
            progressBar.Location = new Point(12, 351);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(776, 30);
            progressBar.TabIndex = 1;
            progressBar.Click += progressBar_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = FoxCordInstaller.Properties.Resources.app1;
            pictureBox1.Location = new Point(338, 117);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(132, 135);
            pictureBox1.TabIndex = 2;
            pictureBox1.TabStop = false;
            pictureBox1.Click += pictureBox1_Click;
            // 
            // installlab
            // 
            installlab.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            installlab.Location = new Point(12, 297);
            installlab.Name = "installlab";
            installlab.Size = new Size(776, 25);
            installlab.TabIndex = 3;
            installlab.Text = "label1";
            installlab.TextAlign = ContentAlignment.TopCenter;
            installlab.Click += installlab_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlLightLight;
            ClientSize = new Size(800, 450);
            Controls.Add(installlab);
            Controls.Add(pictureBox1);
            Controls.Add(progressBar);
            Controls.Add(cancel);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "Form1";
            Text = "Installer";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button cancel;
        private ProgressBar progressBar;
        private PictureBox pictureBox1;
        private Label installlab;
    }
}
