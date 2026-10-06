using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TANREN_Metsuke.Services;

// Hashes decide which files changed, timestamps are kept as part of the transfer contract.
public sealed record SyncFileMetadata(string Filename, long Modified, string Hash);
// Deleted counts planned removals, live files remain untouched until completion.
public sealed record SyncManifestResult(string SessionId, string[] Needed, int Deleted);

// Used under the server's request gate. Files remain staged until every requested hash has arrived.
public sealed class SyncBatchStore(string folder) : IDisposable
{
    private Dictionary<string, string>? expected;
    private readonly HashSet<string> received = new(StringComparer.Ordinal);
    private int deleted;
    private string? sessionId;
    private bool committed;
    private readonly string transaction = WorkoutStorage.TransactionFolder(folder);

    public SyncManifestResult Begin(IReadOnlyList<SyncFileMetadata> files)
    {
        // Reject an invalid manifest before discarding an existing staging batch.
        var manifest = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            if (file == null || !WorkoutJson.IsSyncableFilename(file.Filename) || file.Modified < 0 ||
                file.Hash == null || file.Hash.Length != 64 || !file.Hash.All(Uri.IsHexDigit) || !manifest.TryAdd(file.Filename, file.Hash.ToLowerInvariant()))
                throw new InvalidDataException("Invalid or duplicate manifest entry.");
        }
        lock (WorkoutStorage.Gate)
        {
            Abort();
            Directory.CreateDirectory(folder);
            Directory.CreateDirectory(Path.Combine(transaction, "uploads"));
            expected = manifest;
            committed = false;
            sessionId = Guid.NewGuid().ToString("N");
            received.Clear();
            foreach (var (name, hash) in manifest)
            {
                var local = Path.Combine(folder, name);
                if (File.Exists(local) && ComputeHash(local) == hash)
                {
                    // Matching bytes still need to be structurally valid.
                    WorkoutJson.Validate(name, File.ReadAllText(local));
                    received.Add(name);
                }
            }
            deleted = Directory.EnumerateFiles(folder, "*.json")
                .Count(f => WorkoutJson.IsSyncableFilename(Path.GetFileName(f)) && !manifest.ContainsKey(Path.GetFileName(f)));
            var needed = manifest.Keys.Where(name => !received.Contains(name)).ToArray();
            return new SyncManifestResult(sessionId, needed, deleted);
        }
    }

    public void Upload(string id, string filename, JsonElement content)
    {
        if (!WorkoutJson.IsSyncableFilename(filename))
            throw new InvalidDataException("Invalid filename.");
        lock (WorkoutStorage.Gate)
        {
            CheckSession(id);
            if (committed || !expected!.TryGetValue(filename, out var hash))
                throw new InvalidOperationException("Send a manifest before uploading requested files.");
            if (content.ValueKind == JsonValueKind.Undefined)
                throw new InvalidDataException("Missing file content.");
            // Hash the exact JSON content bytes sent by the phone, formatting affects the hash.
            var json = content.GetRawText();
            WorkoutJson.Validate(filename, json);
            var bytes = Encoding.UTF8.GetBytes(json);
            if (Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() != hash)
                throw new InvalidDataException("File content does not match the manifest hash. Restart sync with a fresh manifest.");
            // A repeated accepted upload is safe when the phone retries after losing a response.
            if (received.Contains(filename))
                return;
            var destination = Path.Combine(transaction, "uploads", filename);
            var temporary = destination + ".tmp";
            // Publish the staged file only after its temporary write succeeds.
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, destination, overwrite: true);
            received.Add(filename);
        }
    }

    public bool Complete(string id)
    {
        lock (WorkoutStorage.Gate)
        {
            CheckSession(id);
            // Completion retries must not replace the data or trigger another reload.
            if (committed)
                return false;
            if (received.Count != expected!.Count)
                throw new InvalidOperationException("The transfer is incomplete. Upload every needed file before completing it.");
            // Recheck staged and unchanged local files in case their bytes changed during transfer.
            foreach (var (name, hash) in expected)
            {
                var staged = Path.Combine(transaction, "uploads", name);
                var path = File.Exists(staged) ? staged : Path.Combine(folder, name);
                if (!File.Exists(path) || ComputeHash(path) != hash)
                    throw new InvalidDataException("Files changed during sync. Send a fresh manifest.");
            }
            var changed = Commit();
            committed = true;
            return changed;
        }
    }

    private void CheckSession(string id)
    {
        if (expected == null || string.IsNullOrEmpty(id) || id != sessionId)
            throw new InvalidOperationException("Unknown or expired sync session. Send a fresh manifest.");
    }

    private bool Commit()
    {
        var uploads = Path.Combine(transaction, "uploads");
        var changed = Directory.EnumerateFiles(uploads, "*.json").Any() || deleted > 0;
        if (!changed)
            return false;
        var next = Path.Combine(transaction, "next");
        if (Directory.Exists(next))
            Directory.Delete(next, recursive: true);
        // Prepare the full replacement first, preserving files outside the sync contract.
        CopyDirectory(folder, next);
        foreach (var path in Directory.EnumerateFiles(next, "*.json"))
            if (WorkoutJson.IsSyncableFilename(Path.GetFileName(path)) && !expected!.ContainsKey(Path.GetFileName(path)))
                File.Delete(path);
        foreach (var path in Directory.EnumerateFiles(uploads, "*.json"))
            File.Copy(path, Path.Combine(next, Path.GetFileName(path)), overwrite: true);

        // Readers share the storage lock, recovery can restore the old folder if the swap is interrupted.
        var previous = Path.Combine(transaction, "previous");
        Directory.Move(folder, previous);
        try { Directory.Move(next, folder); }
        catch
        {
            Directory.Move(previous, folder);
            throw;
        }
        // Retain the whole prior snapshot. If archiving fails, Recover archives it on the next load/start.
        try { WorkoutStorage.ArchivePrevious(folder, previous); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return true;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var item in new DirectoryInfo(source).EnumerateFileSystemInfos())
        {
            // Avoid following links into folders outside the workout snapshot.
            if (item.Attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new InvalidDataException("The workout directory cannot contain symbolic links.");
            var target = Path.Combine(destination, item.Name);
            if (item is DirectoryInfo)
                CopyDirectory(item.FullName, target);
            else
                File.Copy(item.FullName, target);
        }
    }

    private static string ComputeHash(string path)
    {
        using var file = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant();
    }

    private void Abort()
    {
        // Restore or archive any retained live snapshot before removing temporary transfer files.
        WorkoutStorage.Recover(folder);
        if (Directory.Exists(transaction))
            Directory.Delete(transaction, recursive: true);
        expected = null;
        received.Clear();
    }

    public void Dispose()
    {
        lock (WorkoutStorage.Gate)
            Abort();
    }
}
