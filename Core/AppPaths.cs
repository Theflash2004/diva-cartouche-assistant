namespace AssistantArsef.Core;

internal static class AppPaths
{
    public static string DataRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DivaCartoucheAssistant");

    public static string SettingsPath => Path.Combine(DataRoot, "settings.json");
    public static string ActiveSessionPath => Path.Combine(DataRoot, "active-session.json");
    public static string SchemaPath => Path.Combine(DataRoot, "private-schema.json");
    public static string TemplatesRoot => Path.Combine(DataRoot, "Templates");
    public static string UpdatesRoot => Path.Combine(DataRoot, "Updates");
}
