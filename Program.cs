using AssistantArsef.Core;

namespace AssistantArsef;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length > 0 && args[0].Equals("--apply-update", StringComparison.OrdinalIgnoreCase))
        {
            Environment.ExitCode = UpdateInstaller.Run(args);
            return;
        }

        var postUpdateMarker = args.Length > 1 && args[0].Equals("--post-update", StringComparison.OrdinalIgnoreCase)
            ? args[1]
            : null;
        var schema = DivaSchema.Load(AppPaths.SchemaPath);
        ArsefRules.Configure(schema);
        TemplateCatalog.Configure(schema);
        ApplicationConfiguration.Initialize();
        using var form = new ArsefForm();
        if (postUpdateMarker is not null)
        {
            UpdateInstaller.MarkHealthy(postUpdateMarker);
            form.SetStartupStatus("Mise à jour installée avec succès.");
            form.Shown += (_, _) => MessageBox.Show(form,
                "Diva Cartouche Assistant a été mis à jour avec succès.",
                "Application mise à jour", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        Application.Run(form);
    }
}
