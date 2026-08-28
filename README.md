# Gloss

Notes written into Star Citizen's own text.

A *gloss* is a note inserted into a text to explain it. Star Citizen reads every
name and description it shows from one file at startup, so a note added to that
file is a note the game itself displays — while you are standing over the thing
it describes, rather than on a second screen you are not looking at.

Gloss builds that file. It does not inject code, hook the process, or touch a
game binary. It writes text, and it can be removed.

## What it answers

The question that started this: *is this worth carrying, or can I buy another
whenever I like?* Two facts turn out to matter, and both are cheap to show:

- **Whether anything sells it.** A thing no shop stocks is worth the trip home.
- **What size a component is.** The one number you need at the moment you are
  deciding, and it is currently only in the description.

Neither is a price. A price is stale the moment the game launches — the text
file is read once at startup and never again — while "is this sold at all"
barely moves between patches.

## Two channels, deliberately different

**Names** carry a mark of at most **four characters, separator included.** That
budget is measured, not guessed: across 9,575 item names in the 4.10 table the
median is 21 characters, the 90th percentile 32, and 30.8% are already over 24.
A four-character suffix puts a median name at 25 — inside the game's own 75th
percentile, so it renders whatever the UI does today. StarStrings shortens names
for a reason, and Gloss must not undo that work.

**Descriptions** carry the detail. They average 261 characters and already open
with a structured block:

```
Manufacturer: Behring
Item Type: Laser Cannon
Size: 4

Behring's M6A is a versatile high velocity energy autocannon…
```

One more line in that block costs nothing and can say what a single character
cannot — including how sure it is. `*` cannot say "no shop known to stock it";
a line can.

## Where the facts come from

In confidence order. Nothing here enumerates what shops actually stock, so every
claim of absence is a floor and has to be worded as one.

| source | strength | weakness |
|---|---|---|
| Your own kiosk receipts | The game charged you. Cannot be wrong. | Only what you personally bought |
| Star Citizen Wiki API | `is_lootable` / `is_craftable`, game-derived. 100% catalogue coverage in testing | ~10% false negatives once food is excluded |
| UEX | Broad on common goods | Knows 27 of 362 personal weapons; 0 of 51 radars |

The measurement that shaped this: of 109 items known to have been looted, the
wiki API knew all 109 and marked 93 lootable, while UEX had prices for 64. Of
106 items a player's logs prove were bought at a kiosk, UEX listed 29 as sold
nowhere. Absence of a price is not evidence of rarity — which is why the mark
keys on a positive, game-derived flag instead.

## Status

Early. Nothing is published and nothing installs yet.
