using System.ComponentModel;

namespace WinNTC1750;

partial class frmMain
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private IContainer components = null;
    
    private System.Windows.Forms.Label lblMode;
    private System.Windows.Forms.Label lblIncline;
    private System.Windows.Forms.Label lblDistance;
    private System.Windows.Forms.Label lblSpeed;
    private System.Windows.Forms.Button btnStart;
    private System.Windows.Forms.Button btnStop;
    private System.Windows.Forms.Label lblStatus;
    

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
        lblMode = new System.Windows.Forms.Label();
        lblSpeed = new System.Windows.Forms.Label();
        lblIncline = new System.Windows.Forms.Label();
        lblDistance = new System.Windows.Forms.Label();
        btnStart = new System.Windows.Forms.Button();
        btnStop = new System.Windows.Forms.Button();
        lblStatus = new System.Windows.Forms.Label();
        SuspendLayout();
        // 
        // lblMode
        // 
        lblMode.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)0));
        lblMode.Location = new System.Drawing.Point(9, 9);
        lblMode.Margin = new System.Windows.Forms.Padding(0);
        lblMode.Name = "lblMode";
        lblMode.Size = new System.Drawing.Size(300, 50);
        lblMode.TabIndex = 2;
        lblMode.Text = "Mode:";
        // 
        // lblSpeed
        // 
        lblSpeed.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)0));
        lblSpeed.Location = new System.Drawing.Point(9, 59);
        lblSpeed.Margin = new System.Windows.Forms.Padding(0);
        lblSpeed.Name = "lblSpeed";
        lblSpeed.Size = new System.Drawing.Size(300, 50);
        lblSpeed.TabIndex = 3;
        lblSpeed.Text = "Speed:";
        // 
        // lblIncline
        // 
        lblIncline.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)0));
        lblIncline.Location = new System.Drawing.Point(11, 109);
        lblIncline.Margin = new System.Windows.Forms.Padding(0);
        lblIncline.Name = "lblIncline";
        lblIncline.Size = new System.Drawing.Size(300, 50);
        lblIncline.TabIndex = 4;
        lblIncline.Text = "Incline:";
        // 
        // lblDistance
        // 
        lblDistance.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)0));
        lblDistance.Location = new System.Drawing.Point(9, 159);
        lblDistance.Margin = new System.Windows.Forms.Padding(0);
        lblDistance.Name = "lblDistance";
        lblDistance.Size = new System.Drawing.Size(300, 50);
        lblDistance.TabIndex = 5;
        lblDistance.Text = "Distance:";
        // 
        // btnStart
        // 
        btnStart.BackColor = System.Drawing.Color.LightGreen;
        btnStart.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)0));
        btnStart.Location = new System.Drawing.Point(9, 198);
        btnStart.Margin = new System.Windows.Forms.Padding(0);
        btnStart.Name = "btnStart";
        btnStart.Size = new System.Drawing.Size(143, 66);
        btnStart.TabIndex = 6;
        btnStart.Text = "Start";
        btnStart.UseVisualStyleBackColor = false;
        btnStart.Click += btnStart_Click;
        // 
        // btnStop
        // 
        btnStop.BackColor = System.Drawing.Color.Firebrick;
        btnStop.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)0));
        btnStop.Location = new System.Drawing.Point(164, 198);
        btnStop.Margin = new System.Windows.Forms.Padding(0);
        btnStop.Name = "btnStop";
        btnStop.Size = new System.Drawing.Size(145, 66);
        btnStop.TabIndex = 7;
        btnStop.Text = "Stop";
        btnStop.UseVisualStyleBackColor = false;
        btnStop.Click += btnStop_Click;
        // 
        // lblStatus
        // 
        lblStatus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)0));
        lblStatus.Location = new System.Drawing.Point(9, 280);
        lblStatus.Margin = new System.Windows.Forms.Padding(0);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new System.Drawing.Size(300, 50);
        lblStatus.TabIndex = 8;
        lblStatus.Text = "Status";
        lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // frmMain
        // 
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
        BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
        ClientSize = new System.Drawing.Size(318, 335);
        Controls.Add(lblStatus);
        Controls.Add(btnStop);
        Controls.Add(btnStart);
        Controls.Add(lblDistance);
        Controls.Add(lblIncline);
        Controls.Add(lblSpeed);
        Controls.Add(lblMode);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        Margin = new System.Windows.Forms.Padding(2);
        Text = "WinNTC1750 Controller";
        Load += frmMain_Load;
        Leave += frmMain_Leave;
        ResumeLayout(false);
    }

    #endregion
}