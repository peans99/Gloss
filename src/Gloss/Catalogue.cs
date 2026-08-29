using System.Text;
using System.Text.RegularExpressions;

namespace Gloss;

/// <summary>
/// The game's own catalogue: what a thing is called, by the id the logs carry.
/// </summary>
/// <remarks>
/// <para>
/// This is what makes a 110 MB community download unnecessary for naming. The
/// chain is entirely local: a logged GUID is a DataCore record hash, the record
/// carries a <c>displayName</c> localisation key, and <c>global.ini</c> holds
/// the English behind it.
/// </para>
/// <para>
/// Measured against that download on this install: all 203 of its commodities
/// are named, 185 word for word. The 18 that differ are presentation rather than
/// disagreement — the dataset writes "Agricium (Ore)" where the game says
/// "Ore Agricium" — and none is wrong.
/// </para>
/// </remarks>
public sealed partial class Catalogue
{
    private readonly DataCore _core;
    private readonly Dictionary<string, string> _text;
    private readonly Dictionary<Guid, DataRecord> _byId = [];

    public Catalogue(DataCore core, string globalIni)
    {
        _core = core;
        _text = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in globalIni.Split('\n'))
        {
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;

            var value = line[(eq + 1)..].TrimEnd('\r').Trim();
            if (value.Length > 0) _text.TryAdd(line[..eq].TrimStart('﻿').Trim(), value);
        }

        foreach (var record in core.Records()) _byId.TryAdd(record.Hash, record);
    }

    /// <summary>How many records carry an id.</summary>
    public int Count => _byId.Count;

    /// <summary>
    /// What the game calls the thing with this id, or null if it has no record.
    /// </summary>
    /// <remarks>
    /// Falls back to the record's own class name, spaced at word boundaries,
    /// when the localisation key has no English behind it. That is not a guess:
    /// 25 of this install's commodities — Iron among them — have a well-formed
    /// key that CIG have simply not filled in, and the class name is the game's
    /// own word for the thing rather than one we invented.
    /// </remarks>
    public string? Name(Guid id)
    {
        if (!_byId.TryGetValue(id, out var record)) return null;

        if (_core.TextProperty(record, "displayName") is { Length: > 0 } key
            && _text.TryGetValue(key.TrimStart('@'), out var text))
        {
            return text;
        }

        var bare = record.Name.Contains('.')
            ? record.Name[(record.Name.LastIndexOf('.') + 1)..]
            : record.Name;

        return bare.Length > 0 ? Spaced(bare) : null;
    }

    /// <summary>The record behind an id, for callers wanting more than a name.</summary>
    public DataRecord? Record(Guid id) => _byId.GetValueOrDefault(id);

    /// <summary><c>Ore_Agricium</c> becomes <c>Ore Agricium</c>.</summary>
    private static string Spaced(string className) =>
        WordBoundary().Replace(className.Replace('_', ' '), " ").Trim();

    [GeneratedRegex("(?<=[a-z])(?=[A-Z])")]
    private static partial Regex WordBoundary();
}
