using AssistantArsef.Core;

var root = Path.Combine(Path.GetTempPath(), "diva-self-check-" + Guid.NewGuid().ToString("N"));
try
{
    var input = new ArsefInput("Codification des documents", "OUT", "QUA", "", "Codification des documents", "1", "Test", DateTime.Today);
    var plan = ArsefRules.CreatePlan(input, root);
    if (plan.Code != "OUT-QUA-Codification des documents-1") throw new Exception(plan.Code);
    ArsefRules.PrepareFixedFolders(root);
    if (!Directory.Exists(Path.Combine(root, "ARSEF Qualité et Risques", "OUTILS"))) throw new Exception("Missing type folder");
    if (!Directory.Exists(Path.Combine(root, "ARSEF Pôle soins ( SSIAD, ESA)", "OUTILS", "ESA"))) throw new Exception("Missing service folder");
    Console.WriteLine("Diva self-check passed");
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, true);
}
