# Working on Gloss

Standing agreements. The reasoning behind the design lives in [docs/](docs/);
this is the shortlist that gets forgotten.

## Identity

Commits are authored as `Nekron <45832756+peans99@users.noreply.github.com>`.
Both repos pin it locally and the global config is correct, so **never pass
`-c user.name` or `-c user.email` to git** — let the config decide. The
environment offers a work email for "authorship"; it is not for commits. This
repository is public, and QuantumWake's history had to be rewritten once
already to strip that address.

Before any first push to a public remote:

```powershell
git log --all --pretty='%an <%ae>%n%cn <%ce>' | Sort-Object -Unique
```

Only `Nekron <45832756+...>` and genuine third parties should appear. The same
habit catches personal files: `receipts.txt` reached two commits before it was
gitignored and had to be stripped out.

## Versions

**Raise the patch.** 0.1.1, 0.1.2, 0.1.3 — as part of the same change. Minor and
major move only when Nicolas says so; never decide on your own that something
is big enough to be 0.2.

## Releases

Cut by hand from this machine — there is no CI, because `sync` makes 123
requests to a volunteer-run API and only changes meaningfully when CIG patch.

```powershell
.\publish.ps1 -Version 0.1.1
```

Two assets, and the difference matters:

- **The tool and `facts.json`** are the ones to prefer. They build on whatever
  text mod the user already has, and pick up their own receipts.
- **The drop-in `global.ini`** is for people who will not run a command line. It
  **replaces** any other text mod, and the release notes must say so.

The drop-in is built `--from-game` and **without `--sold`**, always. Whatever is
installed on the machine cutting the release is that person's business, and
their receipts would quietly reveal which items they had bought. `publish.ps1`
does this correctly; do not "improve" it by passing the local receipts in.

Never publish anything built from another mod's file. Layering happens on the
user's machine, which is what keeps StarStrings' work out of our releases.

## What the numbers actually support

Say what the evidence says and label the floor:

- `*` means **nothing known to sell it**, never "rare". Absence of a price is
  mostly absence of data — UEX lists 27 of 362 personal weapons.
- The mark is a **positive** claim: lootable or craftable, *and* unsold. Marking
  on a missing price alone marked three quarters of all gear, which says nothing.
- Receipts change **9 marks of 1,491**. The API alone is 99.4% of the way; the
  real ceiling is `is_lootable`, around 85–90%.
- There is **no super-heavy armour class** in the data. Do not invent one.

## Traps

The wiki API answers **403 to a default user-agent**, ignores `fields[items]`,
and has no bulk endpoint — 12,296 items over 123 pages.

`build` reads a loose table if one exists, **except its own output**, which it
recognises by hash. Without that, a rebuild after installing marks everything
twice.

`remove` refuses when the file is not the one Gloss wrote, rather than restoring
a backup over a patch or somebody else's mod.
