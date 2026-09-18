# Teamprotokoll — koordinering via git

All kommunikation mellan roller sker genom git och GitHub. Ingen direktmeddelanden mellan agenter.

## Artefakter

Produktrepot innehåller:

```
work/
├── stories/NNN-slug.md   # skrivs av Product Owner
├── plans/NNN.md          # skrivs av Architect
└── reviews/NNN.md        # skrivs av Reviewer
```

Varje artefakt har YAML-frontmatter:

```yaml
---
story: NNN
status: draft | planned | in-progress | in-review | done
issue: <GitHub issue-nummer>
---
```

`status` i storyn är den enda sanningskällan för var i flödet en story befinner sig.

## Flöde

1. **Product Owner** skapar en story (`status: draft`), skapar en GitHub-issue (`gh issue create`), sätter label `story:draft`, committar och pushar. När storyn är redo för arkitektur sätts `status: draft` → PO signalerar klart genom att lämna den i draft; Architect plockar upp drafts.
2. **Architect** hittar en story med `status: draft` utan plan, skriver `work/plans/NNN.md`, ändrar storyns status till `planned`, uppdaterar issue-label till `story:planned`, committar, pushar.
3. **Coder** hittar en story med `status: planned`, sätter `status: in-progress` + label, skapar branch `story/NNN-slug`, implementerar enligt planen, pushar branchen, öppnar PR mot main, sätter `status: in-review` + label.
4. **Reviewer** hittar en story med `status: in-review`, granskar PR:en (kod + UX-acceptanskriterier), skriver `work/reviews/NNN.md` med verdict `approve` eller `request-changes`, kommenterar på PR:en. Vid `request-changes` sätts status tillbaka till `in-progress`.
5. **Människan** mergar godkända PR:ar. Efter merge sätter PO `status: done` + label `story:done` och stänger issuen.

## Regler för alla roller

- Arbeta ENDAST i din egen worktree. Rör aldrig en annan rolls artefakter.
- Innan varje handling: `git fetch origin && git pull --rebase origin main` (Coder rebasar sin storybranch).
- Efter varje handling: committa med prefix `[<roll>]` i meddelandet och pusha. Din worktree är detached — pusha till main med `git push origin HEAD:main` (Coder pushar sin storybranch med `git push origin HEAD:story/NNN-slug`).
- Spegla status till GitHub: exakt en `story:*`-label per issue (`gh issue edit N --add-label story:X --remove-label story:Y`).
- Poll-loop: kontrollera var 30:e sekund om en artefakt i ditt input-state finns. Om inte — vänta tyst, skriv ingenting.
- Fråga aldrig människan om lov för handlingar protokollet redan tillåter.
