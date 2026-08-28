using System.Text;

namespace Gloss;

/// <summary>One name the build would change.</summary>
public sealed record Change(string ItemClass, string Was, string Becomes, string Why);

/// <summary>What a build changed, and the file it produced.</summary>
public sealed record Build(
    int Marked,
    int Sized,
    int Untouched,
    int Unknown,
    IReadOnlyList<Change> Samples,
    string Content);

/// <summary>
/// Writes the marks into the game's localisation table.
/// </summary>
/// <remarks>
/// <para>
/// The budget is four characters including the separator, and it is measured
/// rather than chosen: the median item name is 21 characters and 30.8% are
/// already over 24, so four puts a median name inside the game's own 75th
/// percentile. See docs/tags.md.
/// </para>
/// <para>
/// The source's line endings are reproduced exactly, including whether the last
/// line had one. A file that differs from the game's by a trailing byte cannot
/// be diffed with any confidence, which is the only way to check this before
/// installing it.
/// </para>
/// </remarks>
public static class Table
{
    private const string ItemNamePrefix = "item_Name";

    /// <summary>Nothing known to sell it.</summary>
    private const string RareMark = "*";

    /// <summary>
    /// Builds the annotated table.
    /// </summary>
    /// <param name="baseIni">
    /// The table to build on. If a text mod is already installed this must be
    /// that mod's file, or installing this one silently reverts theirs.
    /// </param>
    /// <param name="facts">What the wiki API knows, by item class.</param>
    /// <param name="alsoSold">
    /// Item classes the caller knows are sold from evidence the facts file cannot
    /// have - a player's own kiosk receipts. These can never be wrong, so they
    /// override.
    /// </param>
    public static Build Run(
        string baseIni,
        IReadOnlyDictionary<string, Fact> facts,
        ISet<string>? alsoSold = null)
    {
        var sizeSpeaks = TypesWhereSizeVaries(facts);
        var lines = baseIni.Split('\n');
        var output = new StringBuilder(baseIni.Length + lines.Length);
        var samples = new List<Change>();

        int marked = 0, sized = 0, untouched = 0, unknown = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var last = i == lines.Length - 1;
            var split = line.IndexOf('=');
            var key = split > 0 ? line[..split].TrimStart('﻿') : string.Empty;

            if (split <= 0 || !key.StartsWith(ItemNamePrefix, StringComparison.OrdinalIgnoreCase))
            {
                Emit(line);
                continue;
            }

            var itemClass = key[ItemNamePrefix.Length..].TrimStart('_');
            var value = line[(split + 1)..].TrimEnd('\r');
            var name = value.Trim();

            if (itemClass.Length == 0 || name.Length == 0)
            {
                Emit(line);
                continue;
            }

            if (!facts.TryGetValue(itemClass, out var fact))
            {
                // Nothing known is not the same as nothing sells it. Left alone.
                unknown++;
                Emit(line);
                continue;
            }

            var sold = fact.Sold || alsoSold?.Contains(itemClass) == true;

            // The mark is a positive claim about how a thing is obtained, not the
            // absence of a price: UEX lists 27 of 362 personal weapons, so
            // "no price found" would mostly report missing data. An item nothing
            // sells and nothing can loot or craft is more likely a store skin
            // than rare loot, and says nothing useful either way.
            var rare = !sold && (fact.Lootable || fact.Craftable);

            var suffix = new StringBuilder();

            if (fact.Size > 0 && sizeSpeaks.Contains(fact.Type))
                suffix.Append('S').Append(fact.Size);

            if (rare)
                suffix.Append(RareMark);

            if (suffix.Length == 0)
            {
                untouched++;
                Emit(line);
                continue;
            }

            if (rare) marked++;
            if (fact.Size > 0 && sizeSpeaks.Contains(fact.Type)) sized++;

            var becomes = name + " " + suffix;

            if (samples.Count < 30)
            {
                samples.Add(new Change(itemClass, name, becomes,
                    rare ? (fact.Lootable ? "loot only" : "craft only") : "size"));
            }

            Emit(key + "=" + value + " " + suffix);
            continue;

            void Emit(string text)
            {
                output.Append(text);
                if (!last) output.Append('\n');
            }
        }

        return new Build(marked, sized, untouched, unknown, samples, output.ToString());
    }

    /// <summary>
    /// Types whose items are not all the same size.
    /// </summary>
    /// <remarks>
    /// A size every item of a type shares is not a fact about the item: helmets,
    /// clothing and paints are all size 1, and "2Tuf Gloves S1" tells nobody
    /// anything. Thrusters, turrets and personal weapons run S1-S6, where it is
    /// the number you actually want. Deriving it from the data means no list to
    /// maintain when CIG add a type.
    /// </remarks>
    private static HashSet<string> TypesWhereSizeVaries(IReadOnlyDictionary<string, Fact> facts)
    {
        var seen = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);

        foreach (var fact in facts.Values)
        {
            if (fact.Size <= 0) continue;

            if (!seen.TryGetValue(fact.Type, out var sizes))
                seen[fact.Type] = sizes = [];

            sizes.Add(fact.Size);
        }

        return [.. seen.Where(p => p.Value.Count > 1).Select(p => p.Key)];
    }
}
