using System.Diagnostics;
using System.Text.Json;
using AssistantArsef.Core;

namespace AssistantArsef;

internal sealed record DocumentHistoryEntry(
    string Code,
    string Title,
    string DocxPath,
    string PdfPath,
    DateTime StartedAt,
    DateTime? FinishedAt,
    bool IncludedInManagement);

internal static class DocumentHistory
{
    public static IReadOnlyList<DocumentHistoryEntry> Load()
    {
        try
        {
            if (!File.Exists(AppPaths.HistoryPath)) return [];
            return JsonSerializer.Deserialize<List<DocumentHistoryEntry>>(File.ReadAllText(AppPaths.HistoryPath)) ?? [];
        }
        catch { return []; }
    }

    public static void Started(DocumentHistoryEntry session)
    {
        try
        {
            var entries = Load().Where(x => !x.Code.Equals(session.Code, StringComparison.OrdinalIgnoreCase)).ToList();
            entries.Insert(0, new DocumentHistoryEntry(session.Code, session.Title, session.DocxPath, session.PdfPath, DateTime.Now, null, false));
            Save(entries);
        }
        catch { }
    }

    public static void Finished(string code, bool includedInManagement)
    {
        try
        {
            var entries = Load().ToList();
            var index = entries.FindIndex(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (index < 0) return;
            var entry = entries[index];
            entries[index] = entry with { FinishedAt = DateTime.Now, IncludedInManagement = includedInManagement };
            Save(entries);
        }
        catch { }
    }

    private static void Save(List<DocumentHistoryEntry> entries)
    {
        Directory.CreateDirectory(AppPaths.DataRoot);
        var temporary = AppPaths.HistoryPath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(entries.Take(100), new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, AppPaths.HistoryPath, true);
    }
}

internal sealed class DocumentHistoryDialog : Form
{
    private readonly ListBox list = new();
    private readonly IReadOnlyList<DocumentHistoryEntry> entries;

    public DocumentHistoryDialog(IReadOnlyList<DocumentHistoryEntry> entries)
    {
        this.entries = entries;
        Text = "Historique des documents Diva";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 420);
        Size = new Size(900, 520);
        Font = new Font("Segoe UI", 10F);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), RowCount = 2, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        list.Dock = DockStyle.Fill;
        list.HorizontalScrollbar = true;
        foreach (var entry in entries)
        {
            var state = entry.FinishedAt is null ? "En cours" : "Terminé";
            list.Items.Add($"{state} — {entry.Code} — {entry.Title} — {entry.DocxPath}");
        }
        list.DoubleClick += (_, _) => OpenDocument();
        layout.Controls.Add(list, 0, 0);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
        actions.Controls.Add(MakeButton("Fermer", (_, _) => Close()));
        actions.Controls.Add(MakeButton("Ouvrir le dossier", (_, _) => OpenFolder()));
        actions.Controls.Add(MakeButton("Ouvrir le document", (_, _) => OpenDocument()));
        layout.Controls.Add(actions, 0, 1);
    }

    private Button MakeButton(string text, EventHandler click)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 34 };
        button.Click += click;
        return button;
    }

    private DocumentHistoryEntry? Selected() => list.SelectedIndex >= 0 && list.SelectedIndex < entries.Count ? entries[list.SelectedIndex] : null;

    private void OpenDocument()
    {
        var entry = Selected();
        if (entry is null) return;
        TryStart(entry.DocxPath);
    }

    private void OpenFolder()
    {
        var entry = Selected();
        if (entry is null) return;
        TryStart(Path.GetDirectoryName(entry.DocxPath)!);
    }

    private static void TryStart(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); } catch { }
    }
}
