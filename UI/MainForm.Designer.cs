namespace ServiceWatcher.UI;

partial class MainForm
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
        this.lblStatus = new System.Windows.Forms.Label();
        this.btnStart = new System.Windows.Forms.Button();
        this.btnStop = new System.Windows.Forms.Button();
        this.lblTitle = new System.Windows.Forms.Label();
        this.SuspendLayout();
        // 
        // lblStatus
        // 
        this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
        this.lblStatus.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
        this.lblStatus.Location = new System.Drawing.Point(12, 415);
        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Size = new System.Drawing.Size(776, 23);
        this.lblStatus.TabIndex = 0;
        this.lblStatus.Text = "準備完了";
        this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // btnStart
        // 
        this.btnStart.Location = new System.Drawing.Point(12, 60);
        this.btnStart.Name = "btnStart";
        this.btnStart.Size = new System.Drawing.Size(150, 40);
        this.btnStart.TabIndex = 1;
        this.btnStart.Text = "監視開始";
        this.btnStart.UseVisualStyleBackColor = true;
        this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
        // 
        // btnStop
        // 
        this.btnStop.Location = new System.Drawing.Point(168, 60);
        this.btnStop.Name = "btnStop";
        this.btnStop.Size = new System.Drawing.Size(150, 40);
        this.btnStop.TabIndex = 2;
        this.btnStop.Text = "監視停止";
        this.btnStop.UseVisualStyleBackColor = true;
        this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
        // 
        // lblTitle
        // 
        this.lblTitle.AutoSize = true;
        this.lblTitle.Font = new System.Drawing.Font("メイリオ", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        this.lblTitle.Location = new System.Drawing.Point(12, 15);
        this.lblTitle.Name = "lblTitle";
        this.lblTitle.Size = new System.Drawing.Size(264, 28);
        this.lblTitle.TabIndex = 3;
        this.lblTitle.Text = "Windowsサービス監視ツール";
        // 
        // MainForm
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Controls.Add(this.lblTitle);
        this.Controls.Add(this.btnStop);
        this.Controls.Add(this.btnStart);
        this.Controls.Add(this.lblStatus);
        this.MinimumSize = new System.Drawing.Size(600, 400);
        this.Name = "MainForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "ServiceWatcher - Windowsサービス監視";
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Label lblStatus;
    private System.Windows.Forms.Button btnStart;
    private System.Windows.Forms.Button btnStop;
    private System.Windows.Forms.Label lblTitle;
}
