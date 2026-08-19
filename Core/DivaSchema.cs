using System.Text.Json;

namespace AssistantArsef.Core;

public sealed class DivaSchema
{
    public string RootFolderName { get; set; } = "DivaDocuments";
    public string LetterPlace { get; set; } = "Place";
    public string ServiceDomainCode { get; set; } = "OPS";
    public string RegisterCodeMarker { get; set; } = "REGISTER-CODE-1";
    public string RegisterVersionMarker { get; set; } = "Version 1";
    public string RegisterTitleMarker { get; set; } = "REGISTER TITLE";
    public List<DivaOptionDefinition> Types { get; set; } = new();
    public List<DivaOptionDefinition> Domains { get; set; } = new();
    public List<DivaOptionDefinition> Services { get; set; } = new();
    public List<DivaTemplateDefinition> Templates { get; set; } = new();

    public static DivaSchema Load(string path)
    {
        if (!File.Exists(path)) return CreateDefault();
        try
        {
            var schema = JsonSerializer.Deserialize<DivaSchema>(File.ReadAllText(path));
            return schema ?? CreateDefault();
        }
        catch
        {
            return CreateDefault();
        }
    }

    public static DivaSchema CreateDefault() => new()
    {
        Types =
        [
            new("MAN", "MAN - Manuel", "MANUAL", "MAN - Manuel (Instructions, chartes, règles générales)"),
            new("PROC", "PROC - Procédure", "PROCEDURES", "PROC - Procédure (Étapes à suivre pour réaliser une activité)"),
            new("PROT", "PROT - Protocole", "PROTOCOLES", "PROT - Protocole (Consignes détaillées)"),
            new("OUT", "OUT - Outil / Guide", "OUTILS", "OUT - Outil / Guide (Support pratique)"),
            new("ENR", "ENR - Enregistrement", "ENREGISTREMENTS", "ENR - Enregistrement (Document à signer ou compléter)"),
            new("REG", "REG - Registre", "REGISTRES", "REG - Registre (Tableau de suivi)"),
            new("RGPD", "RGPD - Conformité", "CONFORMITE", "RGPD - Conformité (Données personnelles)")
        ],
        Domains =
        [
            new("GEN", "GEN - Général", "GEN", "GEN - Général (Documents transversaux)"),
            new("DIR", "DIR - Direction", "DIR", "DIR - Direction (Décisions et correspondances)"),
            new("RH", "RH - Ressources humaines", "RH", "RH - Ressources humaines (Personnel et formation)"),
            new("OPS", "OPS - Opérations", "OPS", "OPS - Opérations (Activités quotidiennes)"),
            new("SI", "SI - Système d'information", "SI", "SI - Système d'information (Logiciels et sécurité)")
        ],
        Services = [new("SERVICE-A", "Service A", "SERVICE-A"), new("SERVICE-B", "Service B", "SERVICE-B")],
        Templates =
        [
            new("CARTOUCHE", "Document (cartouche)", "Cartouche.dotm", "Cartouche"),
            new("EMAIL", "Email", "Email.dotm", "Plain", "ENR", "GEN"),
            new("REGISTRE", "Registre (tableau vide)", "Registre.docx", "Register", "REG", "GEN")
        ]
    };
}

public sealed record DivaOptionDefinition(string Code, string Label, string Folder, string? DisplayText = null);

public sealed record DivaTemplateDefinition(
    string Code,
    string Label,
    string FileName,
    string Kind,
    string? DefaultTypeCode = null,
    string? DefaultDomainCode = null);
