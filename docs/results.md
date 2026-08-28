# What it actually did

Every number here came from running the tool against one real 4.10 install
(`Data.p4k` build 12519617, game version 4.10.191.2241). One install is not a
sample, and nothing below should be read as more than that.

## The sync

12,296 items in 123 pages, 1.3 MB on disk.

| | |
|---|---|
| lootable | 3,691 |
| sold somewhere | 2,748 |
| `Ship.*` classification | 3,011 |
| `FPS.*` classification | 5,126 |

## The build

With 147 item classes of kiosk receipts layered on top:

| | |
|---|---|
| marked rare | **1,482** |
| given a size | 574 |
| left alone | 2,886 |
| no facts, left alone | 4,694 |

**The positive flag is what makes it usable.** Marking on a missing price alone
put a mark on 3,116 of 4,047 gear names — three quarters of them, which says
nothing at all. Requiring `lootable || craftable` halves it, and every mark is
then a claim the game's own data makes rather than a hole in UEX's.

The 4,694 with no facts are not gear: 975 are paints and liveries, and most of
the rest are Idris internals, flair mugs and crafting stations. Of 109 item
classes this install had actually looted, **0** were missing.

## File integrity

| | |
|---|---|
| lines in | 90,364 |
| lines out | **90,364** |
| lines ending in the mark | 1,565 |
| marks applied | 1,482 |

The 83-line difference is CIG's own: they use asterisks for emphasis in rule
board text (`***the vault opens***`). Gloss never touches those, and the numbers
reconcile exactly. The longest resulting name is still CIG's own 261-character
one, where a description has been pasted into a name key.

## The install loop

Run against the real install, in order:

```
install   Layered over the game's own text
rebuild   built on the game's own text   1,482 / 574   <- identical, no stacking
remove    Put back the game's own text
remove    No install recorded, so there is nothing to put back
```

The rebuild is the one that matters: it proves Gloss recognises its own output
and reaches past it, rather than reading the marks back in and applying them
twice.

Two defects the loop found, both since fixed:

- `user.cfg` was left behind on removal. It is now deleted, but only when there
  was none before and Gloss created it.
- `remove` would have restored its backup over a file a patch had since
  replaced. It now refuses and says where the backup is.

An empty `data/localization/english/` tree is left behind. The game ignores it,
and deleting directories inside somebody's install is not worth getting wrong.

## Spot-checks against the wiki

The four scopes the build marked, checked by hand against
`starcitizen.tools`, which renders a Buy / Loot / Craft / Pledge summary:

| item | wiki says | Gloss |
|---|---|---|
| Gamma Duo (2x Holographic) | Buy=Yes Loot=Yes | left alone ✓ |
| Gamma Duo LL (2x Holographic) | Loot=yes, no Buy | marked ✓ |
| Gamma LL (1x Holographic) | Loot=yes, no Buy | marked ✓ |
| Theta Pro LL (8x Telescopic) | Loot=yes, no Buy | marked ✓ |

Worth recording because it nearly read as a failure: the plain Gamma Duo is sold
at 13 terminals and was never marked, while the marked line was the `_LAMP`
low-light variant — a separate entry with a nearly identical name.
