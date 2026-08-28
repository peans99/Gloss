using System.Security.Cryptography;
using System.Text.Json;

namespace Gloss;

/// <summary>Who wrote the loose table currently in the game folder.</summary>
public enum Provenance
{
    /// <summary>No loose table; the game is reading its own out of Data.p4k.</summary>
    None,

    /// <summary>Ours, unchanged since we wrote it.</summary>
    Gloss,

    /// <summary>Another text mod, recognised by what it does to the text.</summary>
    StarStrings,

    /// <summary>Somebody's, but not one we can name.</summary>
    Unknown,
}

/// <summary>What an install put where, and what it displaced.</summary>
/// <param name="Hash">
/// SHA-256 of the file as written. It is how a later build tells our own output
/// from somebody else's mod, which matters because building on our own output
/// would mark everything twice.
/// </param>
/// <param name="WroteUserCfg">
/// True when there was no user.cfg and we made one. Only then may removing
/// delete it: an existing file is the player's, and may hold settings that have
/// nothing to do with us.
/// </param>
public sealed record Installed(
    string GameRoot,
    string BackupDirectory,
    string Hash,
    DateTimeOffset At,
    string LayeredOver,
    bool WroteUserCfg = false)
{
    private static string Path => System.IO.Path.Combine(AppContext.BaseDirectory, "installed.json");

    public static Installed? Load()
    {
        try
        {
            return File.Exists(Path)
                ? JsonSerializer.Deserialize<Installed>(File.ReadAllText(Path))
                : null;
        }
        catch (Exception e) when (e is IOException or JsonException)
        {
            return null;
        }
    }

    public void Save() => File.WriteAllText(Path, JsonSerializer.Serialize(this));

    public static void Forget()
    {
        if (File.Exists(Path)) File.Delete(Path);
    }

    public static string HashOf(string text) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));

    /// <summary>
    /// Works out who wrote the loose table, if there is one.
    /// </summary>
    /// <remarks>
    /// Ours is recognised by an exact hash rather than by a marker in the file:
    /// a marker would be one more thing altering somebody's game text, and a
    /// hash cannot be wrong. If the file has since been edited by hand or
    /// replaced by a patch the hash stops matching, which is the right answer -
    /// it is no longer ours to reason about.
    ///
    /// StarStrings is recognised by what it visibly does. It tags contracts that
    /// award blueprints with [BP] and writes the reputation a contract pays into
    /// its title, and neither appears in the game's own table. That is a
    /// heuristic and is treated as one: getting it wrong costs a wrong label in
    /// one line of output, never a wrong base, because either way the file is
    /// layered on rather than replaced.
    /// </remarks>
    public static Provenance Identify(string? looseTable, Installed? record)
    {
        if (looseTable is null)
            return Provenance.None;

        if (record is not null && HashOf(looseTable) == record.Hash)
            return Provenance.Gloss;

        if (looseTable.Contains("[BP]", StringComparison.Ordinal)
            || looseTable.Contains(" Rep]", StringComparison.Ordinal))
        {
            return Provenance.StarStrings;
        }

        return Provenance.Unknown;
    }
}
