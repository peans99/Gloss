using System.Text;

namespace Gloss;

/// <summary>One name the build would change.</summary>
public sealed record Change(string ItemClass, string Was, string Becomes, string Why);

/// <summary>What a build changed, and the file it produced.</summary>
public sealed record Build(
    int Marked,
    int Sized,
    int Classed,
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
    /// Everything Gloss adds sits inside these, and nothing the game ships does.
    /// </summary>
    /// <remarks>
    /// The brackets are the point, not decoration. They say at a glance that a
    /// suffix came from here rather than from CIG or another text mod, they make
    /// the additions greppable in a 10 MB file, and they mean a reader who does
    /// not know what "H" means can at least tell it was added. They cost two
    /// characters against a four-character budget, which is why the budget moved
    /// to six - see docs/tags.md.
    /// </remarks>
    private const char Open = '[';
    private const char Close = ']';

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
        ISet<string>? alsoSold = null,
        bool componentsAlreadyLabelled = false)
    {
        var sizeSpeaks = TypesWhereSizeVaries(facts);
        var lines = baseIni.Split('\n');
        var output = new StringBuilder(baseIni.Length + lines.Length);
        var samples = new List<Change>();

        int marked = 0, sized = 0, classed = 0, untouched = 0, unknown = 0;

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
            var showSize = ShowsSize(fact, sizeSpeaks);

            // Armour and ship components are disjoint, so at most one of these
            // ever contributes.
            if (ArmourClass(fact) is { } armour)
                suffix.Append(armour);
            else if (showSize && !componentsAlreadyLabelled)
                suffix.Append(Component(fact));

            if (rare)
                suffix.Append(RareMark);

            if (suffix.Length == 0)
            {
                untouched++;
                Emit(line);
                continue;
            }

            if (rare) marked++;
            if (showSize && !componentsAlreadyLabelled) sized++;
            if (ArmourClass(fact) is not null) classed++;

            var tag = string.Concat(Open, suffix.ToString(), Close);
            var becomes = name + " " + tag;

            if (samples.Count < 30)
            {
                var why = rare
                    ? (fact.Lootable ? "loot only" : "craft only")
                    : ArmourClass(fact) is not null ? "armour class" : "component size";

                samples.Add(new Change(itemClass, name, becomes, why));
            }

            Emit(key + "=" + value + " " + tag);
            continue;

            void Emit(string text)
            {
                output.Append(text);
                if (!last) output.Append('\n');
            }
        }

        return new Build(marked, sized, classed, untouched, unknown, samples, output.ToString());
    }

    /// <summary>
    /// A ship component as class, size and grade - "Mil2B".
    /// </summary>
    /// <remarks>
    /// The three facts anyone fitting a ship wants, and none of them is in the
    /// name. The abbreviations are three letters because Civilian and
    /// Competition share an initial, which is the same reason StarStrings picked
    /// Civ and Cmp; matching their vocabulary costs nothing and means a reader
    /// who knows one tool can read the other.
    ///
    /// Suppressed entirely when a text mod already labels components - see
    /// componentsAlreadyLabelled. StarStrings writes "Mil/1/C Bracer" as a
    /// prefix, and adding "[Mil1C]" after it would say the same thing twice in
    /// two notations.
    /// </remarks>
    private static string Component(Fact fact)
    {
        var cls = fact.Class switch
        {
            "Military" => "Mil",
            "Civilian" => "Civ",
            "Industrial" => "Ind",
            "Competition" => "Cmp",
            "Stealth" => "Sth",
            _ => "",
        };

        // Grade is a single letter A-D when present, and worth having even when
        // the class is not: "S2B" still says more than "S2".
        var grade = fact.Grade is { Length: 1 } g ? g : "";

        return cls + "S" + fact.Size + grade;
    }

    /// <summary>
    /// The weight class of a piece of armour, as one letter.
    /// </summary>
    /// <remarks>
    /// Light, Medium and Heavy are the only classes the data carries. There is
    /// no super-heavy anywhere in it, so none is invented. The rest of the
    /// vocabulary is not a weight at all - "Helmet" is a slot, "Personal" is a
    /// backpack, "UNDEFINED" is nothing - and those get no letter rather than a
    /// guessed one.
    ///
    /// Undersuits are taken from the type instead: almost all of them carry
    /// UNDEFINED as their sub-type, and "undersuit" is the useful fact about
    /// one anyway.
    /// </remarks>
    private static string? ArmourClass(Fact fact)
    {
        if (!fact.Classification.StartsWith("FPS.Armor", StringComparison.OrdinalIgnoreCase))
            return null;

        if (fact.Type.EndsWith("Undersuit", StringComparison.OrdinalIgnoreCase))
            return "U";

        return fact.SubType switch
        {
            "Light" or "LightArmor" => "L",
            "Medium" => "M",
            "Heavy" => "H",
            _ => null,
        };
    }

    /// <summary>
    /// Whether the size is worth the characters it costs.
    /// </summary>
    /// <remarks>
    /// Ship components only. Size is the number you want when fitting a cooler
    /// or a turret, and it is nowhere in the name. On personal gear it is either
    /// meaningless - every helmet is size 1 - or already said better by the name
    /// itself: a scope called "Gamma Duo LL (2x Holographic)" has told you the
    /// magnification, and appending S1 only adds a second, different number to
    /// argue with. The API's classification separates the two cleanly:
    /// <c>Ship.Cooler</c> against <c>FPS.WeaponAttachment.IronSight</c>.
    /// </remarks>
    private static bool ShowsSize(Fact fact, HashSet<string> sizeVaries) =>
        fact.Size > 0
        && fact.Classification.StartsWith("Ship.", StringComparison.OrdinalIgnoreCase)
        && sizeVaries.Contains(fact.Type);

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
