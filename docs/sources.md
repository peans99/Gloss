# Where the facts come from

Three sources, in confidence order. None of them enumerates what shops actually
stock, so every claim of absence is a floor and has to be worded as one
everywhere it appears.

## 1. Your own receipts

The game writes a line when a kiosk charges you:

```
client_price[485.000000] itemClassGUID[396ccb0d-…] itemName[grin_multitool_01] quantity[1]
```

First-party and exact. It cannot be wrong about whether a thing is sold, because
the game took the money. Its weakness is the opposite one: it only ever covers
what one player personally bought.

**The price is the line total, not the unit price.** A stack of 41 MedPens reads
as 10,865 rather than 265, and taking that as a unit price values the item forty
times over. Divide by `quantity` first — doing so takes agreement with UEX from
52% to 81%.

## 2. Star Citizen Wiki API

`https://api.star-citizen.wiki/api/items` — `is_lootable` and `is_craftable`,
derived from the game's own data.

Tested against 109 items a player's logs prove were looted:

| | |
|---|---|
| found in the API | **109 of 109** |
| `is_lootable: true` | 93 (85%) |
| neither flag set | 15 (14%) |

The true answer for that set is 100%, so 85% is the floor of its accuracy. The
misses concentrate in food and drink — things bought at a counter and carried,
which the wiki reasonably does not call loot — so excluding food takes it to
roughly 90%.

Notes for whoever wires it up:

- It **403s a default `User-Agent`**. Send a real one.
- `fields[items]` is ignored; every request returns all 37 fields.
- 12,283 items over `page[size]`/`page[number]`. This is why the sync runs
  somewhere once rather than on every player's machine.
- `shops` is present on every item and **always empty**. `uex_prices` is a UEX
  passthrough, not an independent source.

## 3. UEX

Broad on common goods, thin everywhere else, and crowd-sourced rather than
extracted.

| | |
|---|---|
| items a player's logs prove were bought | 106 |
| of those, UEX lists as sold nowhere | **29 (27%)** |
| personal weapons UEX knows | **27 of 362** |
| radars UEX knows | **0 of 51** |

This is the measurement that shaped the whole design. A mark meaning "no price
found" would have been wrong on more than a quarter of the things that player
had personally bought, including a P4-AR they were carrying at the time. Absence
of a price is not evidence of rarity — so the mark keys on a positive,
game-derived flag and treats a price as corroboration.
