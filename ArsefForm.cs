using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AssistantArsef.Core;

namespace AssistantArsef;

internal sealed class ArsefForm : Form
{
    private readonly ComboBox modelBox = new();
    private readonly ComboBox typeBox = new();
    private readonly ComboBox domainBox = new();
    private readonly ComboBox serviceBox = new();
    private readonly TextBox recipientBox = new();
    private readonly TextBox titleBox = new();
    private readonly TextBox codeWordBox = new();
    private readonly TextBox versionBox = new();
    private readonly TextBox authorBox = new();
    private readonly DateTimePicker dateBox = new();
    private readonly Label titleLabel = new();
    private readonly Label recipientLabel = new();
    private readonly Label codePreview = new();
    private readonly Label pathPreview = new();
    private readonly Label status = new();
    private string arsefRoot = string.Empty;
    private string settingsPath = string.Empty;
    private bool foldersPrepared;
    private FileStream? outputReservation;
    private readonly NotifyIcon trayIcon = new();
    private readonly ConcurrentDictionary<string, System.Threading.Timer> pdfRefreshTimers = new(StringComparer.OrdinalIgnoreCase);
    private FileSystemWatcher? pdfWatcher;
    private bool quitting;

    public ArsefForm()
    {
        Text = "Diva cartouche assistant";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 650);
        Size = new Size(820, 720);
        Font = new Font("Segoe UI", 10F);

        BuildUi();
        LoadSettings();
        ApplySelectionRules();
        UpdatePreview();
        InitializeTray();
        StartPdfWatcher();
        FormClosing += HandleFormClosing;
        Shown += (_, _) => _ = UpdateService.CheckAsync(this);
    }

    private void BuildUi()
    {
        var purple = Color.FromArgb(112, 48, 160);
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = 15,
            AutoScroll = true,
            BackColor = Color.FromArgb(248, 246, 251)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 225));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var banner = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0, 0, 0, 8)
        };
        banner.Controls.Add(new PictureBox
        {
            Image = LoadLogo(),
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(52, 52),
            Margin = new Padding(0, 0, 12, 0)
        });
        banner.Controls.Add(new Label
        {
            Text = "Diva cartouche assistant",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 16F, FontStyle.Bold),
            ForeColor = purple,
            Margin = new Padding(0, 12, 0, 0)
        });
        banner.Controls.Add(new Label
        {
            Text = "Créer • classer • suivre les PDF automatiquement",
            AutoSize = true,
            ForeColor = Color.FromArgb(95, 95, 95),
            Margin = new Padding(18, 17, 0, 0)
        });
        root.Controls.Add(banner, 0, 0);
        root.SetColumnSpan(banner, 2);

        AddRow(root, 1, "Modèle", modelBox);
        AddRow(root, 2, "Titre du document", titleBox, "Exemple : Codification des documents");
        AddRow(root, 3, "Type", typeBox);
        AddRow(root, 4, "Domaine", domainBox);
        AddRow(root, 5, "Service", serviceBox);
        AddRow(root, 6, "Mot-clé de codification", codeWordBox, "Libre : texte long accepté");
        AddRow(root, 7, "Version", versionBox);
        AddRow(root, 8, "Préparé par", authorBox);
        AddRow(root, 9, "Date de validité", dateBox);

        codePreview.AutoSize = true;
        codePreview.Font = new Font(Font, FontStyle.Bold);
        codePreview.ForeColor = Color.DarkGreen;
        root.Controls.Add(new Label { Text = "Code généré", AutoSize = true }, 0, 10);
        root.Controls.Add(codePreview, 1, 11);

        pathPreview.AutoSize = true;
        pathPreview.MaximumSize = new Size(520, 0);
        pathPreview.ForeColor = Color.FromArgb(70, 70, 70);
        root.Controls.Add(new Label { Text = "Dossier prévu", AutoSize = true }, 0, 11);
        root.Controls.Add(pathPreview, 1, 12);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        var create = Button("Créer et ouvrir le document", (_, _) => CreateNew());
        create.BackColor = Color.FromArgb(112, 48, 160);
        create.ForeColor = Color.White;
        create.UseVisualStyleBackColor = false;
        actions.Controls.Add(create);
        root.Controls.Add(actions, 0, 13);
        root.SetColumnSpan(actions, 2);

        status.AutoSize = true;
        status.MaximumSize = new Size(700, 0);
        status.ForeColor = Color.FromArgb(70, 70, 70);
        root.Controls.Add(status, 0, 14);
        root.SetColumnSpan(status, 2);

        var oldTitleLabel = root.GetControlFromPosition(0, 2);
        if (oldTitleLabel is not null) root.Controls.Remove(oldTitleLabel);
        titleLabel.Text = "Titre du document";
        titleLabel.AutoSize = true;
        titleLabel.Anchor = AnchorStyles.Left;
        titleLabel.Margin = new Padding(0, 8, 0, 4);
        root.Controls.Add(titleLabel, 0, 2);

        var oldCodeLabel = root.GetControlFromPosition(0, 10);
        var oldPathLabel = root.GetControlFromPosition(0, 11);
        if (oldCodeLabel is not null) root.Controls.Remove(oldCodeLabel);
        if (oldPathLabel is not null) root.Controls.Remove(oldPathLabel);
        recipientLabel.Text = "Destinataire";
        recipientLabel.AutoSize = true;
        recipientLabel.Anchor = AnchorStyles.Left;
        recipientLabel.Margin = new Padding(0, 8, 0, 4);
        recipientBox.Dock = DockStyle.Top;
        recipientBox.Margin = new Padding(0, 4, 0, 4);
        recipientBox.PlaceholderText = "Exemple : Madame Dupont ou service@exemple.fr";
        root.Controls.Add(recipientLabel, 0, 10);
        root.Controls.Add(recipientBox, 1, 10);
        root.Controls.Add(new Label { Text = "Code généré", AutoSize = true }, 0, 11);
        root.Controls.Add(new Label { Text = "Dossier prévu", AutoSize = true }, 0, 12);

        modelBox.DropDownStyle = ComboBoxStyle.DropDownList;
        typeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        domainBox.DropDownStyle = ComboBoxStyle.DropDownList;
        serviceBox.DropDownStyle = ComboBoxStyle.DropDownList;
        modelBox.DropDownWidth = 360;
        typeBox.DropDownWidth = 620;
        domainBox.DropDownWidth = 620;
        serviceBox.DropDownWidth = 420;
        modelBox.DataSource = TemplateCatalog.Models.ToList();
        typeBox.DataSource = ArsefRules.Types.ToList();
        domainBox.DataSource = ArsefRules.Domains.ToList();
        serviceBox.DataSource = ArsefRules.Services.ToList();
        modelBox.DisplayMember = nameof(ArsefTemplateModel.Label);
        typeBox.DisplayMember = nameof(ArsefOption.ChoiceLabel);
        domainBox.DisplayMember = nameof(ArsefOption.ChoiceLabel);
        serviceBox.DisplayMember = nameof(ArsefOption.ChoiceLabel);
        modelBox.SelectedIndexChanged += (_, _) => { ApplyModelRules(true); ApplySelectionRules(); UpdatePreview(); };
        typeBox.SelectedIndexChanged += (_, _) => { ApplySelectionRules(); UpdatePreview(); };
        domainBox.SelectedIndexChanged += (_, _) => { ApplySelectionRules(); UpdatePreview(); };
        serviceBox.SelectedIndexChanged += (_, _) => UpdatePreview();
        foreach (var control in new Control[] { titleBox, recipientBox, codeWordBox, versionBox, authorBox })
            control.TextChanged += (_, _) => UpdatePreview();
        authorBox.TextChanged += (_, _) => { if (settingsPath.Length > 0) SaveSettings(); };
        dateBox.ValueChanged += (_, _) => UpdatePreview();
        modelBox.SelectedIndex = 0;
        typeBox.SelectedIndex = 0;
        domainBox.SelectedIndex = 0;
        serviceBox.SelectedIndex = 0;
        versionBox.Text = "1";
        authorBox.Text = Environment.UserName;
        dateBox.Value = DateTime.Today;
    }

    private static void AddRow(TableLayoutPanel root, int row, string label, Control control, string? placeholder = null)
    {
        root.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.FromArgb(78, 35, 112), Margin = new Padding(0, 8, 0, 4) }, 0, row);
        control.Dock = DockStyle.Top;
        control.Margin = new Padding(0, 4, 0, 4);
        control.BackColor = Color.White;
        if (control is TextBox textBox && placeholder is not null)
            textBox.PlaceholderText = placeholder;
        root.Controls.Add(control, 1, row);
    }

    private static Button Button(string text, EventHandler click, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 36,
            Padding = new Padding(12, 0, 12, 0),
            Margin = new Padding(0, 0, 8, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(112, 48, 160) : Color.White,
            ForeColor = primary ? Color.White : Color.FromArgb(112, 48, 160),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(112, 48, 160);
        button.FlatAppearance.BorderSize = 1;
        button.Click += click;
        return button;
    }

    private void ApplySelectionRules()
    {
        var domain = domainBox.SelectedItem as ArsefOption;
        var type = typeBox.SelectedItem as ArsefOption;
        var rgpd = type?.Code == "RGPD";
        var soins = domain?.Code.Equals(ArsefRules.ServiceDomainCode, StringComparison.OrdinalIgnoreCase) == true;
        if (rgpd)
        {
            domainBox.SelectedItem = ArsefRules.Domains.FirstOrDefault(x => x.Code == "QUA") ?? ArsefRules.Domains.FirstOrDefault();
            domainBox.Enabled = false;
        }
        else
        {
            domainBox.Enabled = true;
        }

        serviceBox.Enabled = soins;
        serviceBox.Visible = soins;
        if (!soins) serviceBox.SelectedIndex = -1;
        else if (serviceBox.SelectedIndex < 0) serviceBox.SelectedIndex = 0;

        ApplyModelRules();
    }

    private void ApplyModelRules(bool resetRegisterDefaults = false)
    {
        var model = modelBox.SelectedItem as ArsefTemplateModel;
        var email = model?.Kind == ArsefTemplateKind.Plain;
        titleLabel.Text = email ? "Objet du document" : "Titre du document";
        titleBox.PlaceholderText = email ? "Exemple : Accusé de réception de votre réclamation" : "Exemple : Codification des documents";
        codeWordBox.PlaceholderText = "Mot-clé : décrire le document en 3 mots";
        recipientLabel.Visible = email;
        recipientBox.Visible = email;
        if (model?.DefaultTypeCode is { Length: > 0 } defaultTypeCode)
        {
            typeBox.SelectedItem = ArsefRules.GetType(defaultTypeCode);
            typeBox.Enabled = false;
        }
        else
        {
            typeBox.Enabled = true;
        }

        if (model?.DefaultDomainCode is { Length: > 0 } defaultDomainCode)
        {
            domainBox.SelectedItem = ArsefRules.GetDomain(defaultDomainCode);
            domainBox.Enabled = false;
        }

        if (model?.Kind == ArsefTemplateKind.Register && resetRegisterDefaults)
        {
            typeBox.SelectedItem = ArsefRules.Types.FirstOrDefault(x => x.Code == "REG") ?? ArsefRules.Types.FirstOrDefault();
            var registerDomain = model?.DefaultDomainCode;
            domainBox.SelectedItem = ArsefRules.Domains.FirstOrDefault(x => x.Code == registerDomain) ?? ArsefRules.Domains.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(titleBox.Text)) titleBox.Text = "Registre";
            if (string.IsNullOrWhiteSpace(codeWordBox.Text)) codeWordBox.Text = "REGISTRE";
            if (string.IsNullOrWhiteSpace(versionBox.Text)) versionBox.Text = "1";
        }
    }

    private void UpdatePreview()
    {
        try
        {
            var input = ReadInput();
            var plan = ArsefRules.CreatePlan(input, string.IsNullOrWhiteSpace(arsefRoot) ? DesktopArsefRoot() : arsefRoot);
            codePreview.Text = plan.Code;
            pathPreview.Text = plan.OutputFolder;
        }
        catch
        {
            codePreview.Text = "À compléter";
            pathPreview.Text = "À compléter";
        }
    }

    private ArsefInput ReadInput()
    {
        var model = modelBox.SelectedItem as ArsefTemplateModel;
        var type = (typeBox.SelectedItem as ArsefOption)?.Code ?? string.Empty;
        var domain = (domainBox.SelectedItem as ArsefOption)?.Code ?? string.Empty;
        var service = domain.Equals(ArsefRules.ServiceDomainCode, StringComparison.OrdinalIgnoreCase)
            ? (serviceBox.SelectedItem as ArsefOption)?.Code ?? string.Empty
            : string.Empty;
        return new ArsefInput(titleBox.Text, type, domain, service, codeWordBox.Text, versionBox.Text, authorBox.Text, dateBox.Value.Date)
        {
            EmailSubject = model?.Kind == ArsefTemplateKind.Plain ? titleBox.Text : string.Empty,
            EmailRecipient = model?.Kind == ArsefTemplateKind.Plain ? recipientBox.Text : string.Empty
        };
    }

    private bool ValidateInput(out ArsefInput input, out ArsefPlan plan)
    {
        input = ReadInput();
        plan = null!;
        var errors = ArsefRules.Validate(input, arsefRoot).ToList();
        if ((modelBox.SelectedItem as ArsefTemplateModel)?.Kind == ArsefTemplateKind.Plain && string.IsNullOrWhiteSpace(input.EmailRecipient))
            errors.Add("Le destinataire est obligatoire pour un modèle Email.");
        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, errors), "Vérification nécessaire", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        plan = ArsefRules.CreatePlan(input, arsefRoot);
        return true;
    }

    private void CreateNew()
    {
        if (!EnsureArsefRoot()) return;
        if (!ValidateInput(out var input, out var plan)) return;
        if (!PrepareFolders()) return;
        if (!ConfirmOutput(plan)) return;
        try
        {
            var model = SelectedModel();
            WordAutomation.CreateFromTemplate(TemplateCatalog.Extract(model), input, plan, model.Kind);
            Finish(plan);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            outputReservation?.Dispose();
            outputReservation = null;
        }
    }

    private bool ConfirmOutput(ArsefPlan plan)
    {
        if (Directory.Exists(plan.OutputFolder) && !Directory.EnumerateFileSystemEntries(plan.OutputFolder).Any())
        {
            try { Directory.Delete(plan.OutputFolder); } catch { }
        }

        if (Directory.Exists(plan.OutputFolder) || File.Exists(plan.DocxPath) || File.Exists(plan.PdfPath))
        {
            ShowCollision(plan.OutputFolder);
            return false;
        }

        try
        {
            var parent = Path.GetDirectoryName(plan.OutputFolder)!;
            var lockFolder = Path.Combine(AppPaths.DataRoot, "Locks");
            Directory.CreateDirectory(lockFolder);
            var lockName = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(parent)))[..16] + ".lock";
            // One local creator at a time for this parent; the handle also prevents stale lock files.
            outputReservation = new FileStream(Path.Combine(lockFolder, lockName), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return true;
        }
        catch (IOException)
        {
            MessageBox.Show(
                "Un autre document est déjà en cours de création dans ce dossier. Attendez quelques secondes, puis réessayez.",
                "Création en cours", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
    }

    private static void ShowCollision(string outputFolder)
    {

        MessageBox.Show(
            "Un dossier ou document portant exactement ce nom existe déjà :\r\n\r\n" +
            outputFolder + "\r\n\r\n" +
            "Si c'est une nouvelle version, changez le champ « Version ».\r\n" +
            "Si c'est un nouveau document, changez le « Mot-clé de codification ».\r\n\r\n" +
            "Aucun fichier n'a été remplacé.",
            "Nom déjà utilisé", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void Finish(ArsefPlan plan)
    {
        status.Text = "Terminé : le document Word et le PDF sont sur le Bureau.\r\n" + plan.OutputFolder;
        TryOpenFile(plan.DocxPath);
        OpenFolder(plan.OutputFolder);
    }

    private bool EnsureArsefRoot()
    {
        if (string.IsNullOrWhiteSpace(arsefRoot)) arsefRoot = DesktopArsefRoot();
        return !string.IsNullOrWhiteSpace(arsefRoot);
    }

    private bool PrepareFolders()
    {
        if (foldersPrepared) return true;
        try
        {
            ArsefRules.PrepareFixedFolders(arsefRoot);
            foldersPrepared = true;
            return true;
        }
        catch (Exception ex)
        {
            ShowError(ex);
            return false;
        }
    }

    private void InitializeTray()
    {
        trayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        trayIcon.Text = "Diva cartouche assistant - surveillance PDF";
        trayIcon.DoubleClick += (_, _) => ShowFromTray();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Ouvrir Diva", null, (_, _) => ShowFromTray());
        menu.Items.Add("Quitter", null, (_, _) => QuitApplication());
        trayIcon.ContextMenuStrip = menu;
        trayIcon.Visible = true;
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void HandleFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!quitting && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            trayIcon.ShowBalloonTip(2500, "Diva cartouche assistant", "Diva reste actif et met à jour les PDF automatiquement.", ToolTipIcon.Info);
            return;
        }

        StopPdfWatcher();
        trayIcon.Visible = false;
        trayIcon.Dispose();
    }

    private void QuitApplication()
    {
        quitting = true;
        Close();
    }

    private void StartPdfWatcher()
    {
        try
        {
            Directory.CreateDirectory(arsefRoot);
            pdfWatcher = new FileSystemWatcher(arsefRoot, "*.docx")
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            pdfWatcher.Changed += QueuePdfRefresh;
            pdfWatcher.Created += QueuePdfRefresh;
            pdfWatcher.Renamed += (_, e) => QueuePdfRefresh(this, new FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(e.FullPath) ?? arsefRoot, Path.GetFileName(e.FullPath)));
        }
        catch { }
    }

    private void StopPdfWatcher()
    {
        pdfWatcher?.Dispose();
        pdfWatcher = null;
        foreach (var timer in pdfRefreshTimers.Values) timer.Dispose();
        pdfRefreshTimers.Clear();
    }

    private void QueuePdfRefresh(object? sender, FileSystemEventArgs e)
    {
        if (!e.FullPath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)) return;
        var timer = pdfRefreshTimers.GetOrAdd(e.FullPath, path => new System.Threading.Timer(_ => RefreshPdf(path), null, Timeout.Infinite, Timeout.Infinite));
        timer.Change(2500, Timeout.Infinite);
    }

    private void RefreshPdf(string docxPath)
    {
        try
        {
            var pdfPath = Path.ChangeExtension(docxPath, ".pdf");
            for (var attempt = 0; attempt < 5; attempt++)
            {
                if (!File.Exists(docxPath)) return;
                if (File.Exists(pdfPath) && File.GetLastWriteTimeUtc(pdfPath) >= File.GetLastWriteTimeUtc(docxPath)) return;

                var firstLength = new FileInfo(docxPath).Length;
                Thread.Sleep(500);
                if (!File.Exists(docxPath) || new FileInfo(docxPath).Length != firstLength)
                {
                    Thread.Sleep(1500);
                    continue;
                }

                WordAutomation.UpdatePdf(docxPath, pdfPath);
                SetStatus("PDF mis à jour automatiquement : " + Path.GetFileName(pdfPath));
                return;
            }
        }
        catch
        {
            SetStatus("Le PDF sera réessayé au prochain enregistrement du document.");
        }
    }

    private void SetStatus(string text)
    {
        if (IsDisposed || !IsHandleCreated) return;
        try { BeginInvoke(new Action(() => status.Text = text)); } catch { }
    }

    private static void OpenFolder(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + path + "\"") { UseShellExecute = true });
        }
        catch { }
    }

    private static Image? LoadLogo()
    {
        using var source = typeof(ArsefForm).Assembly.GetManifestResourceStream("AssistantArsef.Assets.diva-cat-logo.png");
        if (source is null) return null;
        using var image = Image.FromStream(source);
        return new Bitmap(image);
    }

    private static void TryOpenFile(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch
        {
            // The document is already saved; opening it is only a convenience.
        }
    }

    private ArsefTemplateModel SelectedModel()
    {
        return modelBox.SelectedItem as ArsefTemplateModel
            ?? throw new InvalidOperationException("Aucun modèle n'est sélectionné.");
    }

    private void LoadSettings()
    {
        settingsPath = AppPaths.SettingsPath;
        try
        {
            if (File.Exists(settingsPath))
            {
                var settings = JsonSerializer.Deserialize<ArsefSettings>(File.ReadAllText(settingsPath));
                arsefRoot = settings?.ArsefRoot ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(settings?.Author)) authorBox.Text = settings.Author;
            }
        }
        catch { arsefRoot = string.Empty; }

        if (string.IsNullOrWhiteSpace(arsefRoot)) arsefRoot = DesktopArsefRoot();
    }

    private static string DesktopArsefRoot()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop)) desktop = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(desktop, ArsefRules.RootFolderName);
    }

    private void SaveSettings()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(settingsPath, JsonSerializer.Serialize(new ArsefSettings(arsefRoot, authorBox.Text.Trim())));
    }

    private void ShowError(Exception ex)
    {
        status.Text = "Échec. Aucun succès n'est annoncé tant que les fichiers ne sont pas vérifiés.";
        MessageBox.Show(ex.Message, "Diva – action impossible", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    internal void ExitForUpdate()
    {
        quitting = true;
        BeginInvoke(new Action(Close));
    }

    private sealed record ArsefSettings(string ArsefRoot, string Author = "");
}
