using AssistantArsef.Core;

var root = Path.Combine(Path.GetTempPath(), "diva-self-check-" + Guid.NewGuid().ToString("N"));
try
{
    var input = new ArsefInput("Codification des documents", "OUT", "GEN", "", "Codification des documents", "1", "Test", DateTime.Today);
    var plan = ArsefRules.CreatePlan(input, root);
    if (plan.Code != "OUT-GEN-Codification des documents-1") throw new Exception(plan.Code);
    ArsefRules.PrepareFixedFolders(root);
    if (!Directory.Exists(Path.Combine(root, "GEN", "OUTILS"))) throw new Exception("Missing type folder");
    if (!Directory.Exists(Path.Combine(root, "OPS", "OUTILS", "SERVICE-A"))) throw new Exception("Missing service folder");
    Console.WriteLine("Diva self-check passed");
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, true);
}
