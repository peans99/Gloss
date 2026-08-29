# Game2.dcb — what is actually in there

Explored 2026-08-28 against build 12519617 (4.10.191.2241). Nothing here is
implemented; this is a survey so the next person does not have to start cold.

## Getting it out

It is one entry in `Data.p4k`, and the existing reader handles it unchanged:

| | |
|---|---|
| entry | `Data\Game2.dcb` |
| method | 100 — ZStd, same as everything else |
| compressed | 29,718,272 (28.3 MB) |
| uncompressed | **331,435,556 (316.1 MB)** |

The ZIP64 sentinel reads 4,096 MB, which is what made this look prohibitive at
first glance. It is not: 316 MB extracts in seconds and fits in memory.

## What it contains

Roughly 505,000 printable strings. The readable skeleton is a set of record
paths, `libs/foundry/records/<area>/...`, and the areas say what is modelled:

| area | records | why it matters |
|---|---|---|
| entities | 26,777 | every item, ship and prop |
| ui | 5,835 | |
| **missionbroker** | **2,584** | the missions, keyed by debug name |
| actor | 2,508 | |
| **missiondata** | **2,471** | |
| starmap | 2,073 | |
| **crafting** | **1,892** | what `is_craftable` is derived from |
| scitemmanufacturer | 1,161 | |
| harvestable | 899 | |
| **contracts** | **654** | |
| inventorycontainers | 586 | |
| **reputation** | **530** | standings and scopes |
| **lootgeneration** | **528** | the loot tables themselves |

`ShopLayout`, `ShopCatalog` and `shopinventory` appear as type names, so shops
are modelled — but whether the per-terminal stock lists are in here or served
from CIG's backend is unresolved, and it is the single most valuable open
question. If they are here, purchasability stops being a floor over UEX and
becomes a fact.

## The mission join, which is why this was opened

The API gives `reputation_amount` and `has_blueprints` for 1,786 missions but
nothing to tie them to the game's text. Matching on title gets **1 of 300**;
matching on `debug_name` gets **0 exact, 13 partial**, and those land on
description text rather than titles.

The blob has the missing side. `PU_Delivery_Local_DrugProduction_Stanton4_KlimIntro`
is present, as `MissionBrokerEntry.<debug_name>`. Its *title* is not — because
titles are localisation references, so the record points at a key and
`global.ini` holds the English. That is the chain a reputation tag needs:

```
MissionBrokerEntry.<debug_name>  ->  title reference  ->  global.ini key  ->  the text
                                 ->  reputation amount
```

This is how StarStrings can tag contracts with `[150 Rep]` and we cannot: they
work from extracted game data, we were working from an API that had the numbers
but not the join.

## What it would take

String-searching gets the skeleton. Reading a *value* — a reputation amount, a
shop's stock list — needs the whole format: header counts, struct definitions,
property definitions, enum tables, the typed value arrays, and the record
instances that index into them. The format is documented by community work
(scdatatools, DataForge); it is a known job, not a research project, but it is
a few hundred lines before anything useful comes out, and it moves with game
versions.

## The decision, 2026-08-28

Game files plus UEX, and nothing else.

Shop stock settles it. It is **not** in the game files: `SCShop_*` - the shop
names `Game.log` records - appear zero times, and `ShopLayout`, `ShopCatalog`
and `ShopInventory` exist only as type and UI names with no records behind
them. The shop hits are NPC shopkeeper actors. Stock is server-side, like
prices, which is why UEX exists at all.

So "is it sold" is permanently an outside fact, and everything else comes from
the installed game:

| | from the wiki API | from game files + UEX |
|---|---|---|
| requests per sync | 123, to a volunteer project | **1**, to `items_prices_all` |
| structural data | a derived copy | the installed patch |
| staleness | until the next release | none |
| wiki disappears | dead | unaffected |

The heavy dependency is the one that goes: 12,296 full item records, 37 fields
each, from a project with no bulk endpoint that refuses a default user-agent.
What remains is a single call to the one service built to answer it, which
Quantum Wake already makes.

Without any network it still works, on loot, craft, size, class and grade. Only
the `*` needs UEX, and a player's own receipts can stand in for it.

Extraction is a release-time job: `gloss extract` reads the local install, the
result is published as `facts.json`, and users who would rather not extract get
the published file.

## Reading it: where the strings are

Finding the text does not require trusting a guessed header layout, which is
just as well - assuming the text follows the header directly finds nothing,
because the definition arrays sit in between.

Scanning for contiguous runs of printable-or-null bytes finds exactly two
regions over 100 KB, and both contain known item classes and record paths:

| offset | bytes |
|---|---|
| `0x002C59A6A` | 14,950,072 |
| `0x0023613CA` | 9,406,110 |

Header words 28 and 29 hold 17,165,925 and 7,190,252 - the declared lengths of
two text sections, near enough to the scanned regions to be the cross-check a
real reader should use rather than scanning.

Validate against something known rather than against the format: this install
has looted `gmni_lmg_ballistic_01`, so a parser that cannot find that string has
not found the text section. The 109 looted classes and 147 bought ones are the
test set for everything built on top.

### Solved: the header, and why guessing failed

Guessing the layout gets nowhere and fails in a way that looks like success —
offsets land *inside* strings and produce plausible fragments. Requiring a name
to start at a string boundary took a search that scored 40/40 down to zero
matches, which is how the guesses were caught.

The format is published. unp4k's `unforge` carries it, and the header decodes
this file exactly:

| | |
|---|---|
| file version | **8** — so record definitions are 36 bytes, not 32 |
| struct / property / enum / record | 6,694 / 23,788 / 774 / 116,921 |
| text | 17,165,925 bytes at `0x23613CD` |
| blob | 7,190,252 bytes at `0x33C0232` |

Sections start at `0x78` and run struct, property, enum, mapping, record, then
the typed value arrays, then text, then blob. **Two traps, both silent:**

1. **There are two string tables.** Names come from the *blob*; file paths come
   from the *text*. Reading a name out of the text table lands mid-string.
2. **The header declares value counts in a different order from the one the
   sections are stored in.** Booleans are declared sixth and stored ninth. Get
   that wrong and every offset after it shifts.

### Verified

`DataCore.cs` reads it, and the check that matters is against this install
rather than against the format:

| | |
|---|---|
| records read | **116,921** |
| **looted classes resolved** | **109 of 109** |
| struct names | real type names — `ActivityBehaviorRequestCondition` |
| commodity records | 135, as `EntityClassDefinition` under `entities/commodities/` |

Record areas: 27,127 entities, 24,024 dialoguecontextbank, 18,878 tagdatabase,
2,584 missionbroker, 2,471 missiondata.

Still ahead: property values. Reading *which* struct a record is and where its
fields live is done; reading the fields themselves needs the typed value arrays
and the data-mapping table.

## Why Quantum Wake might want it too

The same file would answer questions that project currently answers with a
110 MB opt-in download, or cannot answer at all:

- **Resource GUID to commodity name.** The logs carry GUIDs; only the community
  dataset maps them. This is where that mapping comes from originally.
- **Item and ship reference data** without the dataset dependency.
- **Loot tables**, which is better evidence than the wiki's `is_lootable` flag
  and would lift Gloss's ~85% accuracy ceiling.

The cost is a parser that has to be re-verified every patch, against a format
CIG do not document. The dataset dependency is somebody else maintaining that
burden, which is worth something.
