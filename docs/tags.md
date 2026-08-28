# What goes in a name, and what does not

The name is the expensive channel. Every character lands in every list, tooltip
and inventory grid the name appears in, and StarStrings shortens names for a
reason. The description is the cheap channel: it already averages 261
characters and nobody is squinting at a grid cell to read it.

So the rule is: **the name carries at most four characters, separator included.
Everything else goes in the description.**

## The budget is measured

Across the 9,575 item names in the 4.10 table:

| | characters |
|---|---|
| median | 21 |
| 75th percentile | 26 |
| 90th percentile | 32 |
| 99th percentile | 44 |
| longest real name | 61 |

30.8% of names are already longer than 24 characters. Four characters put a
median name at 25 — still inside the game's own 75th percentile, so it renders
exactly as well as a third of the names CIG already ship. Anything longer is
guesswork about a UI we cannot measure.

## The two marks

```
Omnisky IX Cannon S4*
                  ││
                  │└─ nothing known to sell it
                  └── size, when size means something
```

**`*` — nothing known to sell it.** A floor, never a rarity rating. See
[sources.md](sources.md): absence of a price is mostly absence of data.

**`S<n>` — the size**, and only where it carries information. Size is shown
only when it *varies within the item's own type*, which is a property of the
data rather than a list somebody has to maintain:

| type | sizes seen | shown? |
|---|---|---|
| ManneuverThruster | 1,2,3,4,5 | yes |
| WeaponPersonal | 1,2,3,4,5 | yes |
| WeaponAttachment | 0,1,2,3,4,5 | yes |
| Turret | 1,2,3,4,5,6 | yes |
| Char_Armor_Helmet | 1 | no — always 1 |
| Char_Clothing_Legs | 1 | no — always 1 |
| Paints | 1 | no — always 1 |

Only 38 of 5,073 sized items name their size today, so this is close to
entirely new information. But "2Tuf Gloves S1" tells nobody anything, and a
mark that appears everywhere stops being read. If every item of a type is the
same size, the size is not a fact about the item.

## What never goes in the name

Prices. The table is read once at startup, so a price is as old as the session
and says nothing about it. It belongs in the description, where there is room
to say when it was true.
