# Teamprotokoll — koordinering via artefakter

All kommunikation mellan roller sker genom markdown-artefakter (och i github-läge git/GitHub). Inga direktmeddelanden mellan agenter.

## Lägen

Din `CLAUDE.md` börjar med **Läge: github** eller **Läge: local**. Det avgör var artefakterna ligger och vad du får göra med git:

| | github | local |
|---|---|---|
| Artefakter (`work/`) | i produktrepot; committas och pushas till origin | i workspacens `work/` (sökvägen står i din CLAUDE.md); bara sparas, ingen git |
| Issue och labels | ja, en `story:*`-label per issue | nej; `issue:` lämnas tomt |
| Coder | branch `story/NNN-slug`, push, PR mot main | lokal branch `story/NNN-slug` från integrationsbranchen, **ingen push, ingen PR** |
| Reviewer | granskar PR:en | granskar `git diff <integrationsbranch>...story/NNN-slug` |
| "Mergad" betyder | PR:en är mergad | `git branch --merged <integrationsbranch>` listar story-branchen |
| Overseer mergar med | GitHub eller `npm run merge NNN` | `npm run merge NNN` |
| Regel | pusha bara till origin | **kör aldrig `git push`**, rör aldrig andra brancher än din story-branch |

Allt nedan gäller båda lägena om inget annat sägs.

## Artefakter

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
status: draft | planned | in-progress | in-review | approved | done
issue: <GitHub issue-nummer, tomt i local-läge>
---
```

`status` i storyn är den enda sanningskällan för var i flödet en story befinner sig.

## Flöde

1. **Product Owner** skapar en story (`status: draft`). github: skapar en issue (`gh issue create`), sätter label `story:draft`, committar och pushar. local: sparar filen. När storyn är redo för arkitektur sätts `status: draft` → PO signalerar klart genom att lämna den i draft; Architect plockar upp drafts.
2. **Architect** hittar en story med `status: draft` utan plan, skriver `work/plans/NNN.md`, ändrar storyns status till `planned`. github: label `story:planned`, committar, pushar.
3. **Coder** hittar en story med `status: planned`, sätter `status: in-progress` (github: + label), skapar branch `story/NNN-slug`, implementerar enligt planen. github: pushar branchen, öppnar PR mot main. local: committar bara lokalt. Sätter sedan `status: in-review` (github: + label).
4. **Reviewer** hittar en story med `status: in-review`, granskar ändringen (github: PR:en; local: diffen mot integrationsbranchen) mot kod och UX-acceptanskriterier, skriver `work/reviews/NNN.md` med verdict `approve` eller `request-changes` (github: kommenterar på PR:en). Vid `request-changes` sätts status tillbaka till `in-progress`. Vid `approve` sätts `status: approved` (github: + label).
5. **Overseer** (människan) granskar och mergar stories i `approved` (github: PR:en; local: `npm run merge NNN`). Kolumnen `approved` på boardet är Overseers att-göra-lista; inget hamnar där utan Reviewerns godkännande.
6. **Product Owner** ser att Overseer mergat, sätter `status: done` (github: + label `story:done`, stänger issuen).

## Regler för alla roller

- Arbeta ENDAST i din egen worktree. Rör aldrig en annan rolls artefakter.
- github: innan varje handling `git fetch origin && git pull --rebase origin main` (Coder rebasar sin storybranch); efter varje handling committa med prefix `[<roll>]` och pusha (`git push origin HEAD:main`, Coder `git push origin HEAD:story/NNN-slug`). Spegla status till GitHub: exakt en `story:*`-label per issue (`gh issue edit N --add-label story:X --remove-label story:Y`).
- local: artefakter sparas direkt i `work/`. Coder committar med prefix `[coder]` på sin story-branch och rebasar den mot integrationsbranchen innan `in-review`. Ingen annan roll gör git-skrivningar.
- Poll-loop: kontrollera var 30:e sekund om en artefakt i ditt input-state finns. Om inte — vänta tyst, skriv ingenting.
- Fråga aldrig Overseer om lov för handlingar protokollet redan tillåter. Overseer är människan som ger idéer och mergar; tilltala hen så när du behöver hens beslut.
