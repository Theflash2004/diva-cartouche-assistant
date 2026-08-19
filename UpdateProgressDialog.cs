namespace AssistantArsef;

internal sealed class UpdateProgressDialog : Form
{
    private readonly Label message = new();
    private readonly Label percentage = new();
    private readonly ProgressBar progress = new();

    public UpdateProgressDialog(Form owner)
    {
        Text = "Mise à jour de Diva";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ControlBox = false;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(520, 155);
        Font = new Font("Segoe UI", 10F);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        message.Text = "Préparation du téléchargement…";
        message.AutoSize = true;
        message.ForeColor = Color.FromArgb(78, 35, 112);
        layout.Controls.Add(message, 0, 0);
        layout.SetColumnSpan(message, 2);

        progress.Dock = DockStyle.Fill;
        progress.Minimum = 0;
        progress.Maximum = 100;
        progress.Style = ProgressBarStyle.Marquee;
        progress.MarqueeAnimationSpeed = 25;
        progress.Margin = new Padding(0, 16, 12, 0);
        layout.Controls.Add(progress, 0, 1);

        percentage.Text = "…";
        percentage.AutoSize = true;
        percentage.TextAlign = ContentAlignment.MiddleRight;
        percentage.Margin = new Padding(0, 16, 0, 0);
        layout.Controls.Add(percentage, 1, 1);

        var note = new Label
        {
            Text = "Ne fermez pas Diva pendant l'installation.",
            AutoSize = true,
            ForeColor = Color.FromArgb(95, 95, 95),
            Margin = new Padding(0, 12, 0, 0)
        };
        layout.Controls.Add(note, 0, 2);
        layout.SetColumnSpan(note, 2);
    }

    public void SetMessage(string text)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(new Action(() => SetMessage(text))); return; }
        message.Text = text;
        percentage.Text = "…";
    }

    public void BeginDownload(long? totalBytes)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(new Action(() => BeginDownload(totalBytes))); return; }
        if (totalBytes is > 0)
        {
            progress.Style = ProgressBarStyle.Continuous;
            progress.Value = 0;
            percentage.Text = "0 %";
        }
        else
        {
            progress.Style = ProgressBarStyle.Marquee;
            percentage.Text = "…";
        }
    }

    public void ReportDownload(long completedBytes, long? totalBytes)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(new Action(() => ReportDownload(completedBytes, totalBytes))); return; }
        if (totalBytes is not > 0) return;
        var value = (int)Math.Clamp(completedBytes * 100L / totalBytes.Value, 0, 100);
        progress.Value = value;
        percentage.Text = value + " %";
    }
}
