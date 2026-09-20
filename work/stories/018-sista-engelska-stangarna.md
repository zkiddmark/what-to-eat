---
story: 018
status: done
issue: 43
---

# Story 018: De sista engelska strängarna och en flikrubrik per vy

Efterskörd på story 016. Liten, men den stänger ett kriterium som inte blev uppfyllt.

## Användarvärde
Som användare vill jag att den svenska språkdräkten är hel, så att jag inte möts av engelska
just i det ögonblick jag lägger till en rätt — och så att jag kan skilja appens flikar åt när
jag har flera öppna.

## Bakgrund
Story 016 gjorde huvudarbetet och löste den svåra biten (veckodagarna) rätt. Men Reviewer
satte `request-changes` på PR #42 med två kvarvarande engelska strängar, och PR:en mergades
innan de åtgärdats. Jag har verifierat på `main` att de finns kvar:

| Var | Text |
|---|---|
| `Shared/Day/DayCardModal.razor:81` | `"Add a new delightful dish!"` — initialvärdet på `ModalHeaderText`, alltså modalens rubrik när man **skapar** en rätt |
| `Shared/PreviousDish/PreviousDishComponent.razor:87` och `:114` | `"Not scheduled"` — syns på varje rättkort i `/previousDishes` som inte är schemalagt |

Den första är särskilt olycklig: fälten under rubriken är svenska, rubriken över dem är
engelsk, och det är precis det flödessteg storyn själv beskrev.

Rebase-problemet som Reviewer också varnade för — att branchen utgick från före story 015 och
kunde riva rubrikhierarkin och paletten vid mergen — **gick bra**. Jag har kontrollerat main:
`DayCard.razor` har `<h2>`/`<h3>` från 015 *och* `ToSwedish()` från 016, och `site.css` har
sina 16 palettvariabler kvar. Konflikterna löstes rätt. Det behöver alltså ingen åtgärd.

Reviewer noterade dessutom en sak som inte var ett kriteriebrott: `Shared/MainLayout.razor:2`
sätter `<PageTitle>Veckans mat</PageTitle>` för **alla** Blazor-vyer. Fliken säger därför
"Veckans mat" även på `/previousDishes` och `/admin/anvandare`. Razor Pages-sidorna gör det
redan bättre med egen `ViewData["Title"]`. Jag tar med det här eftersom storyn ändå öppnar
samma filer.

## Omfattning
- De tre förekomsterna ovan översätts.
- Varje Blazor-vy får en egen `<PageTitle>`.

Utanför omfattning: allt annat. Ingen omdesign, ingen `.resx` — samma avgränsning som 016.

## UX-acceptanskriterier
- [ ] Modalens rubrik är svensk när jag skapar en rätt, och den säger vad jag håller på med.
- [ ] Ett rättkort som inte är schemalagt visar en svensk text. Den ska säga att rätten inte
      är inplanerad — inte se ut som ett fel eller som saknad data.
- [ ] Ingen användarsynlig engelsk text finns kvar i någon vy. Kriteriet är "ingen kvar",
      inte "dessa tre" — leta bredare än tabellen ovan.
- [ ] Varje vy ger sin egen flikrubrik. Med `/` och `/previousDishes` öppna i var sin flik
      går de att skilja åt.
- [ ] Rubriknivåerna från story 015 och paletten är orörda efteråt — `<h2>`/`<h3>` i
      `DayCard`, palettvariablerna i `site.css`.
- [ ] Inget beteende ändras. Det här är text.

## Flöde
1. Användaren trycker "lägg till rätt" — modalens rubrik är svensk.
2. Användaren öppnar `/previousDishes` och ser svensk text på de rätter som inte är inplanerade.
3. Användaren har två flikar öppna och ser på flikrubriken vilken som är vilken.

## Wireframe

```
  Flikar FÖRE                          Flikar EFTER
  ┌──────────────┬──────────────┐     ┌──────────────┬──────────────┐
  │ Veckans mat  │ Veckans mat  │     │ Veckans mat  │ Alla rätter  │
  └──────────────┴──────────────┘     └──────────────┴──────────────┘
      ↑ går inte att skilja åt

  Modalen FÖRE                         Modalen EFTER
  ┌────────────────────────────┐      ┌────────────────────────────┐
  │ Add a new delightful dish! │      │ Lägg till en rätt          │
  │ Rättens namn  [        ]   │      │ Rättens namn  [        ]   │
  │ Ingredienser  [        ]   │      │ Ingredienser  [        ]   │
  │  ↑ engelsk rubrik,         │      │                            │
  │    svenska fält            │      │                            │
  └────────────────────────────┘      └────────────────────────────┘

  Kortet:  "Not scheduled"   ->   "Inte inplanerad"
```

## Klar när
- `grep -rn "Not scheduled\|delightful" Pages Shared` ger noll träffar.
- Genomsökning av alla vyer i en körande instans utan engelsk text.
- `/` och `/previousDishes` har olika flikrubriker.
- `DayCard.razor` har kvar `<h2>`/`<h3>`, och `site.css` sina palettvariabler.
