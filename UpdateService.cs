using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO.Compression;
using AssistantArsef.Core;

namespace AssistantArsef;

internal static class UpdateService
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/Theflash2004/diva-cartouche-assistant/releases/latest";
    private const string PackageName = "DivaCartoucheAssistant-win-x64.zip";

    public static async Task CheckAsync(ArsefForm owner)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DivaCartoucheAssistant", CurrentVersion.ToString()));
            var release = await client.GetFromJsonAsync<GitHubRelease>(LatestReleaseUrl);
            if (release is null || !Version.TryParse(release.TagName.TrimStart('v'), out var available) || available <= CurrentVersion)
                return;

            var asset = release.Assets.FirstOrDefault(x => x.Name.Equals(PackageName, StringComparison.OrdinalIgnoreCase));
            if (asset is null) return;
            if (MessageBox.Show(owner, $"Une mise à jour ({release.TagName}) est disponible. L'installer maintenant ?",
                    "Mise à jour disponible", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
                return;

            using var progress = new UpdateProgressDialog(owner);
            progress.Show(owner);
            try
            {
                var updateRoot = AppPaths.UpdatesRoot;
                Directory.CreateDirectory(updateRoot);
                var zipPath = Path.Combine(updateRoot, PackageName);
                progress.SetMessage("Téléchargement de la mise à jour…");
                await DownloadAsync(client, asset.DownloadUrl, zipPath, progress);

                progress.SetMessage("Vérification de l'intégrité du téléchargement…");
                var digest = asset.Digest;
                var checksumAsset = release.Assets.FirstOrDefault(x => x.Name.Equals("checksums.txt", StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrWhiteSpace(digest) && checksumAsset is not null)
                {
                    var checksumPath = Path.Combine(updateRoot, "checksums.txt");
                    await DownloadAsync(client, checksumAsset.DownloadUrl, checksumPath, progress);
                    digest = ReadChecksum(checksumPath, PackageName);
                    File.Delete(checksumPath);
                }
                Verify(zipPath, digest);

                progress.SetMessage("Installation sécurisée de la mise à jour…");
                var marker = Path.Combine(updateRoot, "update-" + Guid.NewGuid().ToString("N") + ".ok");
                var start = new ProcessStartInfo(Application.ExecutablePath)
                {
                    UseShellExecute = true,
                    WorkingDirectory = AppContext.BaseDirectory
                };
                start.ArgumentList.Add("--apply-update");
                start.ArgumentList.Add(zipPath);
                start.ArgumentList.Add(Environment.ProcessId.ToString());
                start.ArgumentList.Add(marker);
                Process.Start(start);
                owner.ExitForUpdate();
            }
            finally
            {
                if (!progress.IsDisposed) progress.Close();
            }
        }
        catch
        {
            // Updates are optional. A failed check or download never interrupts document creation.
        }
    }

    private static async Task DownloadAsync(HttpClient client, string url, string path, UpdateProgressDialog progress)
    {
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var totalBytes = response.Content.Headers.ContentLength;
        progress.BeginDownload(totalBytes);
        await using var source = await response.Content.ReadAsStreamAsync();
        var temporaryPath = path + ".download";
        try
        {
            await using var target = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None);
            var buffer = new byte[1024 * 128];
            long completedBytes = 0;
            int read;
            while ((read = await source.ReadAsync(buffer)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read));
                completedBytes += read;
                progress.ReportDownload(completedBytes, totalBytes);
            }
            File.Move(temporaryPath, path, true);
        }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); } catch { }
        }
    }

    private static string? ReadChecksum(string path, string fileName)
    {
        foreach (var line in File.ReadLines(path))
        {
            var parts = line.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && Path.GetFileName(parts[^1]).Equals(fileName, StringComparison.OrdinalIgnoreCase)) return parts[0];
        }
        return null;
    }

    private static void Verify(string path, string? digest)
    {
        if (string.IsNullOrWhiteSpace(digest)) throw new InvalidDataException("La mise à jour ne contient pas de preuve d'intégrité.");
        var expected = digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ? digest[7..] : digest;
        var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("La vérification de la mise à jour a échoué.");
    }

    private static Version CurrentVersion => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);

    private sealed record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("assets")] List<GitHubAsset> Assets);

    private sealed record GitHubAsset(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("browser_download_url")] string DownloadUrl,
        [property: JsonPropertyName("digest")] string? Digest);
}

internal static class UpdateInstaller
{
    public static int Run(string[] args)
    {
        if (args.Length < 4) return 2;
        var zipPath = Path.GetFullPath(args[1]);
        var marker = Path.GetFullPath(args[3]);
        if (!IsUnder(zipPath, AppPaths.UpdatesRoot)) return 3;

        if (int.TryParse(args[2], out var parentPid)) WaitForExit(parentPid);
        var stage = Path.Combine(AppPaths.UpdatesRoot, "stage-" + Guid.NewGuid().ToString("N"));
        var target = Path.Combine(AppContext.BaseDirectory, "DivaCartoucheAssistant.exe");
        var backup = target + ".rollback";
        try
        {
            Directory.CreateDirectory(AppPaths.UpdatesRoot);
            ExtractSafe(zipPath, stage);
            var stagedExe = Directory.EnumerateFiles(stage, "DivaCartoucheAssistant.exe", SearchOption.AllDirectories).SingleOrDefault();
            if (stagedExe is null) return 4;

            if (File.Exists(backup)) File.Delete(backup);
            if (File.Exists(target)) File.Move(target, backup);
            File.Copy(stagedExe, target, false);

            var start = Process.Start(new ProcessStartInfo(target, "--post-update \"" + marker + "\"")
            {
                UseShellExecute = true,
                WorkingDirectory = AppContext.BaseDirectory
            });
            var healthy = WaitForMarker(marker, 30_000);
            if (!healthy)
            {
                try { if (start is not null && !start.HasExited) start.Kill(true); } catch { }
                Restore(target, backup);
                return 5;
            }

            if (File.Exists(backup)) File.Delete(backup);
            return 0;
        }
        catch
        {
            Restore(target, backup);
            return 6;
        }
        finally
        {
            try { if (Directory.Exists(stage)) Directory.Delete(stage, true); } catch { }
            try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
        }
    }

    public static void MarkHealthy(string marker)
    {
        var full = Path.GetFullPath(marker);
        if (!IsUnder(full, AppPaths.UpdatesRoot)) return;
        Directory.CreateDirectory(AppPaths.UpdatesRoot);
        var temp = full + ".tmp";
        File.WriteAllText(temp, "ok");
        File.Move(temp, full, true);
    }

    private static void WaitForExit(int pid)
    {
        for (var i = 0; i < 120; i++)
        {
            try
            {
                using var process = Process.GetProcessById(pid);
                if (!process.HasExited) { Thread.Sleep(250); continue; }
            }
            catch { }
            return;
        }
    }

    private static bool WaitForMarker(string marker, int timeoutMs)
    {
        var until = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < until)
        {
            if (File.Exists(marker) && File.ReadAllText(marker).Equals("ok", StringComparison.Ordinal)) return true;
            Thread.Sleep(250);
        }
        return false;
    }

    private static void ExtractSafe(string zipPath, string destination)
    {
        using var archive = System.IO.Compression.ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            var relative = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(relative) || relative.Split(Path.DirectorySeparatorChar).Any(x => x == ".."))
                throw new InvalidDataException("Archive path is not safe.");
            var full = Path.GetFullPath(Path.Combine(destination, relative));
            if (!IsUnder(full, destination)) throw new InvalidDataException("Archive path is not safe.");
            if (string.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(full); continue; }
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            entry.ExtractToFile(full, true);
        }
    }

    private static void Restore(string target, string backup)
    {
        try { if (File.Exists(target)) File.Delete(target); } catch { }
        try { if (File.Exists(backup)) File.Move(backup, target, true); } catch { }
    }

    private static bool IsUnder(string path, string root)
    {
        var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }
}
