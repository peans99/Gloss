using System.Text;
using Gloss;

// Gloss - notes written into Star Citizen's own text.
//
// Four commands, and only two of them touch the game folder.

var command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";
var options = Options.Parse(args);

return command switch
{
    "sync" => await Sync(options),
    "build" => Build(options),
    "install" => Install(options),
    "remove" => Remove(options),
    _ => Help(),
};

static int Help()
{
    Console.WriteLine("""
        Gloss - notes written into Star Citizen's own text.

          gloss sync             Fetch acquisition facts (slow; do it when a patch lands)
          gloss build            Write the annotated table to out\global.ini
          gloss install          Copy the built table into the game, keeping a backup
          gloss remove           Put back whatever install displaced

        Options
          --path <dir>           Star Citizen channel folder (default: auto-detect)
          --facts <file>         Fact file (default: facts.json)
          --out <dir>            Where build writes (default: out)
          --page-size <n>        Items per API request while syncing (default: 100)
          --sold <file>          Extra item classes known to be sold, one per line.
                                 Kiosk receipts cannot be wrong, so they win.
          --from-game            Ignore any installed text mod and build on the
                                 game's own table. For producing a file for
                                 someone else.

        Only 'install' and 'remove' write outside this folder.
        """);

    return 0;
}

static async Task<int> Sync(Options o)
{
    Console.WriteLine("Fetching acquisition facts. This is the slow part - the API has no bulk");
    Console.WriteLine("endpoint and ignores sparse fieldsets, so it is one page at a time.");
    Console.WriteLine();

    var facts = await Facts.FetchAsync(o.PageSize, (page, last) =>
        Console.Write($"\r  page {page} of {last}   "));

    Console.WriteLine();
    Facts.Save(facts, o.Facts);

    Console.WriteLine();
    Console.WriteLine($"  {facts.Items.Count:N0} items written to {o.Facts}");
    Console.WriteLine($"  lootable: {facts.Items.Values.Count(f => f.Lootable):N0}"
        + $"   sold somewhere: {facts.Items.Values.Count(f => f.Sold):N0}");

    return 0;
}

static int Build(Options o)
{
    if (Facts.Load(o.Facts) is not { } facts)
    {
        Console.Error.WriteLine($"No fact file at {o.Facts}. Run 'gloss sync' first.");
        return 1;
    }

    if (o.ResolveInstall() is not { } root)
    {
        Console.Error.WriteLine("No Star Citizen install found. Pass --path.");
        return 1;
    }

    var (baseIni, over) = ReadBaseTable(root, o.FromGame);

    if (baseIni is null)
    {
        Console.Error.WriteLine("Could not read the game's text table out of Data.p4k.");
        return 1;
    }

    var extra = LoadSold(o.Sold);
    var built = Table.Run(baseIni, facts.Items, extra);

    Directory.CreateDirectory(o.Out);
    var target = Path.Combine(o.Out, "global.ini");
    File.WriteAllText(target, built.Content, new UTF8Encoding(false));

    // The game only reads a loose table when told which language to use.
    File.WriteAllText(Path.Combine(o.Out, "user.cfg"), "g_language = english\n", new UTF8Encoding(false));

    Console.WriteLine($"facts from {facts.Source}, built {facts.BuiltAt:yyyy-MM-dd}");
    Console.WriteLine($"built on {over}");
    if (extra.Count > 0) Console.WriteLine($"plus {extra.Count} item classes you have receipts for");
    Console.WriteLine();
    Console.WriteLine($"  marked rare   : {built.Marked:N0}");
    Console.WriteLine($"  given a size  : {built.Sized:N0}");
    Console.WriteLine($"  armour classed: {built.Classed:N0}");
    Console.WriteLine($"  left alone    : {built.Untouched:N0}");
    Console.WriteLine($"  not in facts  : {built.Unknown:N0}   (left alone - unknown is not rare)");
    Console.WriteLine();

    foreach (var c in built.Samples.Take(12))
        Console.WriteLine($"  {Trim(c.Was, 36),-36} -> {Trim(c.Becomes, 40),-40} {c.Why}");

    Console.WriteLine();
    Console.WriteLine($"written: {target}");
    Console.WriteLine("Nothing has touched the game. 'gloss install' does that.");

    return 0;
}

static string Trim(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";

static int Install(Options o)
{
    if (o.ResolveInstall() is not { } root)
    {
        Console.Error.WriteLine("No Star Citizen install found. Pass --path.");
        return 1;
    }

    var source = Path.Combine(o.Out, "global.ini");

    if (!File.Exists(source))
    {
        Console.Error.WriteLine($"Nothing built at {source}. Run 'gloss build' first.");
        return 1;
    }

    var target = Path.Combine(root, "data", "localization", "english", "global.ini");
    var cfg = Path.Combine(root, "user.cfg");
    var backups = Path.Combine(AppContext.BaseDirectory, "backup",
        DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss"));

    // Named before anything is written, because afterwards the evidence is gone.
    var displaced = Installed.Identify(
        File.Exists(target) ? File.ReadAllText(target) : null, Installed.Load()) switch
    {
        Provenance.Gloss => "an earlier Gloss build",
        Provenance.StarStrings => "StarStrings",
        Provenance.Unknown => "another text mod",
        _ => "the game's own text",
    };

    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
    Directory.CreateDirectory(backups);

    // Whatever is there is copied aside before anything is written, and that
    // copy is what 'remove' reads back. It may be another mod's file.
    foreach (var (path, name) in new[] { (target, "global.ini"), (cfg, "user.cfg") })
    {
        if (File.Exists(path))
            File.Copy(path, Path.Combine(backups, name), overwrite: true);
    }

    File.Copy(source, target, overwrite: true);

    var wroteCfg = !File.Exists(cfg);

    if (wroteCfg)
        File.Copy(Path.Combine(o.Out, "user.cfg"), cfg, overwrite: true);
    else if (!File.ReadAllText(cfg).Contains("g_language", StringComparison.OrdinalIgnoreCase))
        Console.WriteLine("Note: your user.cfg has no g_language line, so the game may ignore this.");
    new Installed(root, backups, Installed.HashOf(File.ReadAllText(source)),
        DateTimeOffset.UtcNow, displaced, wroteCfg).Save();

    Console.WriteLine($"Installed into {root}");
    Console.WriteLine($"Layered over {displaced}");
    Console.WriteLine($"What was there is in {backups}");
    Console.WriteLine("Restart Star Citizen to see it.");

    return 0;
}

static int Remove(Options o)
{
    if (Installed.Load() is not { } record)
    {
        Console.Error.WriteLine("No install recorded, so there is nothing to put back.");
        return 1;
    }

    var target = Path.Combine(record.GameRoot, "data", "localization", "english", "global.ini");
    var saved = Path.Combine(record.BackupDirectory, "global.ini");
    var now = File.Exists(target) ? File.ReadAllText(target) : null;

    // If the file is no longer the one we wrote, a patch or another mod has been
    // over it since. Restoring our backup would undo their work rather than ours.
    if (now is not null && Installed.HashOf(now) != record.Hash)
    {
        Console.Error.WriteLine("The text file is not the one Gloss wrote - a patch or another mod");
        Console.Error.WriteLine("has replaced it since. Nothing was changed. The backup is still at");
        Console.Error.WriteLine($"  {record.BackupDirectory}");
        Installed.Forget();
        return 1;
    }

    if (File.Exists(saved))
        File.Copy(saved, target, overwrite: true);
    else if (File.Exists(target))
        File.Delete(target);

    // Only the one we created. An existing user.cfg is the player's.
    if (record.WroteUserCfg)
    {
        var cfg = Path.Combine(record.GameRoot, "user.cfg");
        if (File.Exists(cfg)) File.Delete(cfg);
    }

    Installed.Forget();
    Console.WriteLine($"Put back {record.LayeredOver}. Restart Star Citizen.");

    return 0;
}

/// <summary>
/// The table to build on, and a phrase describing it.
/// </summary>
/// <remarks>
/// A loose table on disk is usually another mod's work and is exactly what we
/// want underneath us, so that installing this does not silently revert theirs.
/// The exception is our own output: building on that would apply every mark a
/// second time, so when the file is ours we reach past it to whatever it
/// displaced, which is the copy taken at install.
/// </remarks>
static (string? Ini, string Over) ReadBaseTable(string root, bool fromGame)
{
    // A file built for other people has to come from the game's own text. What
    // happens to be installed on the machine cutting the release is that
    // person's business and must not travel.
    var loose = fromGame
        ? null
        : Path.Combine(root, "data", "localization", "english", "global.ini");
    var record = Installed.Load();
    var text = loose is not null && File.Exists(loose) ? File.ReadAllText(loose) : null;

    switch (Installed.Identify(text, record))
    {
        case Provenance.Gloss:
            // Past our own file to what was under it, or the game's if nothing was.
            var displaced = Path.Combine(record!.BackupDirectory, "global.ini");

            if (File.Exists(displaced))
                return (File.ReadAllText(displaced), $"the table Gloss displaced ({record.LayeredOver})");

            break;

        case Provenance.StarStrings:
            return (text, "StarStrings, which will survive this");

        case Provenance.Unknown:
            return (text, "a text mod already installed, which will survive this");
    }

    var raw = new P4kArchive(P4kArchive.PathFor(root))
        .TryRead(Path.Combine("Data", "Localization", "english", "global.ini"));

    return raw is null ? (null, "") : (Encoding.UTF8.GetString(raw), "the game's own text");
}

static HashSet<string> LoadSold(string? path)
{
    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    if (path is not null && File.Exists(path))
    {
        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0) set.Add(trimmed);
        }
    }

    return set;
}

/// <summary>Command-line options, with the game folder found the usual way.</summary>
internal sealed record Options(
    string? Path, string Facts, string Out, int PageSize, string? Sold, bool FromGame)
{
    public static Options Parse(string[] args) => new(
        Value(args, "--path"),
        Value(args, "--facts") ?? "facts.json",
        Value(args, "--out") ?? "out",
        int.TryParse(Value(args, "--page-size"), out var n) && n > 0 ? n : 100,
        Value(args, "--sold"),
        args.Contains("--from-game"));

    private static string? Value(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    /// <summary>The channel folder, given or discovered.</summary>
    public string? ResolveInstall()
    {
        if (Path is { Length: > 0 } given)
            return Directory.Exists(given) ? given : null;

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady) continue;

            foreach (var relative in new[]
            {
                System.IO.Path.Combine("rsi", "StarCitizen"),
                System.IO.Path.Combine("Program Files", "Roberts Space Industries", "StarCitizen"),
                System.IO.Path.Combine("Roberts Space Industries", "StarCitizen"),
            })
            {
                var candidate = System.IO.Path.Combine(drive.RootDirectory.FullName, relative);
                if (!Directory.Exists(candidate)) continue;

                foreach (var channel in Directory.EnumerateDirectories(candidate))
                {
                    if (P4kArchive.Exists(channel)) return channel;
                }
            }
        }

        return null;
    }
}
