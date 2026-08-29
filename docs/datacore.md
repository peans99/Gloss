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
