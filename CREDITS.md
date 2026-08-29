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

Gloss is moving to the player's own game files plus UEX, so these are listed by
what they are becoming rather than by what they were.

| Source | What Gloss takes |
|---|---|
| [UEX](https://uexcorp.space) | **Whether anything sells an item** — the one fact that is not in the game files at all, because stock and prices are server-side. Crowd-sourced by players and their datarunners, and the reason a "nothing sells this" mark is possible. |
| [Star Citizen Wiki API](https://api.star-citizen.wiki) | `is_lootable`, `is_craftable`, `size`, `class`, `grade`, `type` and `classification`. **This is what made the whole idea work** — absence of a price says almost nothing, and these flags say something. Being downstream of the game files, it is becoming a fallback rather than the primary source, but the design was found here, not in the blob. |
| [starcitizen.tools](https://starcitizen.tools) | The same project's wiki. Its Buy / Loot / Craft / Pledge summary is how a mark that looks wrong gets checked by hand. |
| [scunpacked-data](https://github.com/StarCitizenWiki/scunpacked-data) / [ScDataDumper](https://github.com/octfx/ScDataDumper) | StarCitizenWiki and octfx. The loader that turns the game files into the data the API serves — which is to say, the thing Gloss is learning to do for itself. |

Reading the game files directly is not a reason to owe these projects less. The
schema was legible because their output showed what was in there and what it was
called, and the wiki API is what proved the marks were worth making before a
single byte of `Game2.dcb` had been parsed.

## File formats

**`Data.p4k`** — the game's localisation table, and now `Game2.dcb`, are read out
of the archive. It is a ZIP64 container whose entries use ZStd under method 100,
which standard ZIP readers refuse. That detail is community knowledge
established by the reverse-engineering behind
[scdatatools](https://github.com/ventorvar/scdatatools) and the `unp4ck` tools
before it. The reader here is ported from
[Quantum Wake](https://github.com/peans99/QuantumWake)'s, which is our own
implementation of that knowledge — no third-party extraction tool is involved,
and nothing is unpacked to disk.

**`Game2.dcb`** — the DataCore blob, where the game keeps its records: entities,
missions, loot tables, crafting, reputation. The format is undocumented by CIG
and legible only because scdatatools and the DataForge lineage worked it out and
published what they found. **This debt grows rather than shrinks as Gloss reads
the file directly** — every struct and property definition it walks is a shape
somebody else identified first. Nothing of theirs is vendored; the reader is
ours, written from their published understanding.

## Packages

| Package | Why |
|---|---|
| [ZstdSharp.Port](https://github.com/oleg-st/ZstdSharp) | Oleg Stepanischev's managed ZStd decoder. Without it the archive cannot be read at all from .NET. |

## Star Citizen

Star Citizen®, Roberts Space Industries® and Cloud Imperium® are registered
trademarks of Cloud Imperium Rights LLC. This is an unofficial fan tool, not
affiliated with or endorsed by Cloud Imperium Games.
