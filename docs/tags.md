# What goes in a name, and what does not

The name is the expensive channel. Every character lands in every list, tooltip
and inventory grid the name appears in, and StarStrings shortens names for a
reason. The description is the cheap channel: it already averages 261
characters and nobody is squinting at a grid cell to read it.

So the rule is: **the name carries at most six characters, separator and
brackets included. Everything else goes in the description.**

## The budget is measured

Across the 9,575 item names in the 4.10 table:

| | characters |
|---|---|
| median | 21 |
| 75th percentile | 26 |
| 90th percentile | 32 |
| 99th percentile | 44 |
| longest real name | 61 |

30.8% of names are already longer than 24 characters. The widest suffix Gloss
produces is ` [S4*]`, six characters, putting a median name at 27 — just past
the game's own 75th percentile and well inside its 90th. It renders as well as
the third of names CIG already ship longer than that. Anything more is guesswork
about a UI we cannot measure.

Two of those six are the brackets, and they earn their place: they say at a
glance that a suffix came from Gloss rather than from CIG or another text mod,
they make every addition greppable in a 10 MB file, and a reader who does not
know what `H` means can still tell it was added by something.

## The marks

```
Omnisky IX Cannon [S4*]
                   ││
                   │└─ nothing known to sell it
                   └── size, when size means something

Berserker Helmet [H*]       heavy armour, and nothing sells it
Ace Interceptor Suit [U]    an undersuit
```

Everything Gloss adds is inside square brackets. Two of the game's own 9,575
names already end in one — `WCPR-Made XIAN Nox Cooler Name [PH]` and its
powerplant twin, both obvious CIG placeholders — so "ends with a bracket" is
99.98% ours rather than all of it. The tag *vocabulary* is exact: `L`, `M`, `H`,
`U`, `S` followed by a number, and `*`. Nothing else appears between our
brackets, and `[PH]` is not in that set.

**Armour carries its weight class** as one letter: `L`, `M`, `H`, or `U` for an
undersuit. Light, Medium and Heavy are the only classes the data has — there is
no super-heavy anywhere in it, so none is invented. The rest of the vocabulary
is not a weight at all (`Helmet` is a slot, `Personal` is a backpack,
`UNDEFINED` is nothing) and gets no letter rather than a guessed one. Undersuits
come from the item's type instead, since almost all of them carry `UNDEFINED`.

Armour and ship components are disjoint, so a name never carries both a class
and a size.

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
