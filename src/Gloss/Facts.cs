using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gloss;

/// <summary>What is known about one item class.</summary>
/// <param name="Lootable">The game's own flag: can be found rather than bought.</param>
/// <param name="Craftable">The game's own flag: can be made.</param>
/// <param name="Sold">True when at least one terminal is reported to stock it.</param>
/// <param name="Size">Component size, or 0 when the item has none.</param>
/// <param name="Type">The item's type, used to decide whether size means anything.</param>
/// <param name="Classification">
/// Dotted class, e.g. <c>Ship.Cooler</c> or <c>FPS.WeaponAttachment.IronSight</c>.
/// The leading segment is what separates a ship component from personal gear.
/// </param>
/// <param name="SubType">
/// For armour, the weight class - Light, Medium or Heavy. Also carries slot
/// words and UNDEFINED for pieces that have no class, which are not shown.
/// </param>
public sealed record Fact(
    bool Lootable, bool Craftable, bool Sold, int Size, string Type, string Classification, string SubType);

/// <summary>The whole fact table, as published.</summary>
public sealed record FactFile(
    string Source,
    DateTimeOffset BuiltAt,
    IReadOnlyDictionary<string, Fact> Items);

/// <summary>
/// Pulls acquisition facts from the Star Citizen Wiki API.
/// </summary>
/// <remarks>
/// <para>
/// This is the slow half and the reason Gloss is a separate tool: the API has
/// 12,283 items, ignores <c>fields[items]</c> so every request returns all 37
/// fields, and has no bulk endpoint. It is a job to run once when a patch lands,
/// not something to ask of anyone opening a dashboard.
/// </para>
/// <para>
/// Two things bite whoever writes this next. The API answers <c>403</c> to a
/// default .NET or Python user-agent, so a real one is required. And
/// <c>shops</c> is present on every item and always empty - the only
/// purchasability signal it carries is <c>uex_prices</c>, which is a UEX
/// passthrough rather than an independent source.
/// </para>
/// </remarks>
public static class Facts
{
    private const string Endpoint = "https://api.star-citizen.wiki/api/items";

    /// <summary>Identifies the tool. A default agent is refused with a 403.</summary>
    private const string Agent = "Gloss/0.1 (+https://github.com/peans99) star-citizen text annotator";

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Fetches every item, reporting progress as it goes.</summary>
    public static async Task<FactFile> FetchAsync(
        int pageSize, Action<int, int> progress, CancellationToken token = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd(Agent);
        http.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        var items = new Dictionary<string, Fact>(StringComparer.OrdinalIgnoreCase);
        var page = 1;
        var lastPage = 1;

        while (page <= lastPage)
        {
            var url = $"{Endpoint}?page%5Bsize%5D={pageSize}&page%5Bnumber%5D={page}";

            using var response = await http.GetAsync(url, token);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
            var root = doc.RootElement;

            if (root.TryGetProperty("meta", out var meta)
                && meta.TryGetProperty("last_page", out var last)
                && last.ValueKind == JsonValueKind.Number)
            {
                lastPage = last.GetInt32();
            }

            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in data.EnumerateArray())
                    Absorb(entry, items);
            }

            progress(page, lastPage);
            page++;
        }

        return new FactFile("api.star-citizen.wiki", DateTimeOffset.UtcNow, items);
    }

    private static void Absorb(JsonElement entry, Dictionary<string, Fact> into)
    {
        if (Str(entry, "class_name") is not { Length: > 0 } cls)
            return;

        // uex_prices.purchase is the only stock signal the API carries; the
        // shops array is present on every item and always empty.
        var sold = entry.TryGetProperty("uex_prices", out var prices)
            && prices.TryGetProperty("purchase", out var purchase)
            && purchase.ValueKind == JsonValueKind.Array
            && purchase.GetArrayLength() > 0;

        into[cls] = new Fact(
            Bool(entry, "is_lootable"),
            Bool(entry, "is_craftable"),
            sold,
            entry.TryGetProperty("size", out var size) && size.ValueKind == JsonValueKind.Number
                ? size.GetInt32()
                : 0,
            Str(entry, "type") ?? "?",
            Str(entry, "classification") ?? "?",
            Str(entry, "sub_type") ?? "");
    }

    private static string? Str(JsonElement element, string name) =>
        element.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static bool Bool(JsonElement element, string name) =>
        element.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

    public static void Save(FactFile facts, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(facts, Json));
    }

    public static FactFile? Load(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            return JsonSerializer.Deserialize<FactFile>(File.ReadAllText(path), Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
