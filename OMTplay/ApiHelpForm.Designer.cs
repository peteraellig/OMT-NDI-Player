namespace OMTplay;

partial class ApiHelpForm
{
    private TextBox helpText = null!;
    private Button closeButton = null!;

    private void InitializeComponent()
    {
        helpText = new TextBox();
        closeButton = new Button();
        SuspendLayout();
        helpText.Location = new Point(16, 16);
        helpText.Size = new Size(728, 580);
        helpText.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        helpText.Multiline = true;
        helpText.ReadOnly = true;
        helpText.ScrollBars = ScrollBars.Vertical;
        helpText.BackColor = SystemColors.Window;
        helpText.Font = new Font("Segoe UI", 10F);
        helpText.TabIndex = 0;
        closeButton.Location = new Point(634, 610);
        closeButton.Size = new Size(110, 32);
        closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        closeButton.Text = "Close";
        closeButton.DialogResult = DialogResult.Cancel;
        closeButton.TabIndex = 1;
        CancelButton = closeButton;
        AutoScaleDimensions = new SizeF(7, 15);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(760, 658);
        MinimumSize = new Size(500, 400);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        Text = "OMTplay – API Help";
        Controls.Add(helpText);
        Controls.Add(closeButton);
        ResumeLayout(false);
        PerformLayout();
    }
}
