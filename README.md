# Gloss

Notes written into Star Citizen's own text.

A *gloss* is a note inserted into a text to explain it. Star Citizen reads every
name it shows from one file at startup, so a note added to that file is a note
the game itself displays — while you are standing over the thing it describes,
rather than on a second screen you are not looking at.

```
Gamma Duo (2x Holographic)             you can buy another whenever you like
Gamma Duo LL (2x Holographic) [*]      nothing known to sell this one
REP-VS EMP Generator [S4]              a size 4 component
Berserker Helmet [H*]                  heavy armour, and nothing sells it
Ace Interceptor Racing Suit [U*]       an undersuit, same
```

Everything Gloss adds sits inside square brackets, so it is obvious what came
from here and what is the game's own text.

It writes text. No code injection, no hooking, no game binary touched, and
`gloss remove` puts back exactly what was there.

## Using it

```
gloss sync                       fetch acquisition facts — slow, do it when a patch lands
gloss build                      write the annotated table to out\global.ini
gloss install                    copy it into the game, keeping a backup
gloss remove                     put back whatever install displaced
```

Only `install` and `remove` write outside the tool's own folder. `build` prints
what it would change and touches nothing, because the thing being edited is
your game.

```
--path <dir>        Star Citizen channel folder (default: auto-detect)
--facts <file>      fact file (default: facts.json)
--out <dir>         where build writes (default: out)
--page-size <n>     items per API request while syncing (default: 100)
--sold <file>       item classes you know are sold, one per line
```

**`--sold` is the good one.** If you have a record of what you have actually
bought at a kiosk, feed it in: a receipt cannot be wrong, and it overrides every
other source. Quantum Wake can produce that list from your logs.

Restart the game for a change to take effect — the table is read once at
startup.

## What the marks mean

**`*` — nothing known to sell it.** A floor, not a rarity rating. Two sources
say a thing is sold: your receipts, and UEX by way of the wiki API. Neither
enumerates what shops actually stock, so an unmarked item means *nobody has told
us otherwise*, never "confirmed common".

The mark is a **positive** claim — an item is marked only when the game's own
data says it can be looted or crafted *and* nothing is known to sell it. Marking
on a missing price alone put a mark on three quarters of all gear, which says
nothing at all; requiring the positive flag halves it.

**`L` `M` `H` `U` — the armour class**: light, medium, heavy, or undersuit.
Light, Medium and Heavy are the only classes the data carries; there is no
super-heavy in it, so none is invented, and pieces with no class get no letter.

**`S4` — the component size**, for ship components only, and only where size
varies within its type. Every helmet is size 1, so the number would be noise;
thrusters and turrets run S1–S6, where it is the number you actually want. A
scope already says `2x` in its name and does not need `S1` argued next to it.

Names carry at most **six characters including the separator and brackets**.
That budget is measured: the median item name is 21 characters and 30.8% are
already over 24, so the widest tag leaves a median name at 27 — just past the
game's own 75th percentile and well inside its 90th. See
[docs/tags.md](docs/tags.md).

## Living with other text mods

Only one loose `global.ini` can win, so Gloss layers rather than replaces.

- If **StarStrings** or another mod is installed, `build` uses *their* file as
  its base, so their work survives underneath the marks.
- If the file is **Gloss's own**, `build` reaches past it to whatever it
  displaced. Otherwise a second build would apply every mark twice.
- `remove` refuses if the file is no longer the one Gloss wrote — a patch or
  another mod has been over it since, and restoring our backup would undo their
  work rather than ours.

It knows which is which by hashing what it wrote, so nothing depends on a marker
left inside somebody's game text.

## What it does not do

**Prices.** The table is read once at startup, so a price would be as old as
your session with nothing on screen admitting it. "Is this sold at all" barely
moves between patches; a price moves hourly.

**Rarity.** It reports what two incomplete sources know about how a thing is
obtained. That is a useful floor, not a rating.

## Documentation

- [docs/tags.md](docs/tags.md) — what goes in a name, what does not, and the
  measured budget
- [docs/sources.md](docs/sources.md) — the three sources, their measured
  accuracy, and the traps in each
- [docs/results.md](docs/results.md) — what it actually did against one real
  install, including the two defects the install loop found
- [docs/datacore.md](docs/datacore.md) — what is inside Game2.dcb, and what it
  would take to read it properly
- [CREDITS.md](CREDITS.md) — MrKraken's StarStrings is the idea this is built
  on, and none of it is redistributed here

## Status

Early. Nothing is published, and the install path has been exercised against one
4.10 install only.
