# Credits and third-party resources

Gloss is built by **nekron**. Everything below came from someone else, and this
page says exactly what was taken from where.

The rule: if a line of logic, a data field or a package came from outside this
repository, it is named here — even when it was re-typed rather than copied,
because the *knowledge* was still someone else's work.

---

## The idea

| Project | Author | What Gloss owes it |
|---|---|---|
| [StarStrings](https://github.com/MrKraken/StarStrings) | MrKraken | **The whole approach.** That Star Citizen's English text can be usefully rewritten — that a note added to the game's own strings is worth more than the same note on a second screen — is MrKraken's idea, demonstrated years before this existed. Gloss is a different tool doing a different job, but it is standing on that. **Nothing of theirs is vendored, modified or redistributed here.** If StarStrings is installed, Gloss builds on top of the file it finds so their work survives; it never bundles or republishes it. |

## Data

| Source | What Gloss takes |
|---|---|
| [Star Citizen Wiki API](https://api.star-citizen.wiki) | `is_lootable`, `is_craftable`, `size`, `type` and `classification`, derived from the game's own data. This is the source that made the marks defensible: absence of a price says almost nothing, and these flags say something. |
| [starcitizen.tools](https://starcitizen.tools) | The same project's wiki. Used for spot-checking a mark that looks wrong — its Buy / Loot / Craft / Pledge summary is an independent reading of the same underlying data. |
| [UEX](https://uexcorp.space) | Prices and stock listings, reaching Gloss through the wiki API's `uex_prices` rather than directly. Crowd-sourced by players and their datarunners. |
| [scunpacked-data](https://github.com/StarCitizenWiki/scunpacked-data) / [ScDataDumper](https://github.com/octfx/ScDataDumper) | StarCitizenWiki and octfx. Not consumed by Gloss directly, but the wiki data it does consume exists because of this loader. |

## File formats

**`Data.p4k`** — the game's localisation table is read out of the archive. It is
a ZIP64 container whose entries use ZStd under method 100, which standard ZIP
readers refuse. That detail is community knowledge established by the
reverse-engineering behind [scdatatools](https://github.com/ventorvar/scdatatools)
and the `unp4ck` tools before it. The reader here is ported from
[Quantum Wake](https://github.com/peans99/QuantumWake)'s, which is our own
implementation of that knowledge — no third-party extraction tool is involved,
and nothing is unpacked to disk.

## Packages

| Package | Why |
|---|---|
| [ZstdSharp.Port](https://github.com/oleg-st/ZstdSharp) | Oleg Stepanischev's managed ZStd decoder. Without it the archive cannot be read at all from .NET. |

## Star Citizen

Star Citizen®, Roberts Space Industries® and Cloud Imperium® are registered
trademarks of Cloud Imperium Rights LLC. This is an unofficial fan tool, not
affiliated with or endorsed by Cloud Imperium Games.
