---
story: 016
status: planned
issue: 38
---

# Story 016: En språkdräkt — allt i gränssnittet på svenska

## Användarvärde
Som användare vill jag att hela appen talar samma språk, så att jag slipper läsa "Eat again!"
bredvid "Din röst" och kan lita på att texterna är skrivna för mig.

## Bakgrund
Rapporterat av människan. Appen talar två språk, och gränsen går rakt genom historien: allt
som byggts från story 006 och framåt (inloggning, konto, roller, admin, röstning) är svenskt,
medan den ursprungliga appen är engelsk. Resultatet är att en och samma vy blandar båda.

Engelska strängar jag hittat:

| Var | Text |
|---|---|
| `Pages/Index.razor:4` | "What to eat" (sidrubrik) |
| `Pages/PreviousDishes.razor:5` | "All dishes, Wow!" (sidrubrik) |
| `Shared/Day/DayCard.razor` | "Update the dish", "Add a dish" |
| `Shared/Day/DayCardModal.razor` | "Name of the dish", "Notes", "Url to a tasty image", "Close" |
| `Shared/PreviousDish/PreviousDishComponent.razor` | "Eat again!" |
| `Shared/Ingredients/AddIngredientsComponent.razor` | "Ingredients", "Add" |
| `Shared/UploadImage/UploadFileComponent.razor` | "Upload an image" |
| `Shared/ImageFromDb/ImageFromDbComponent.razor` | "Loading image" |
| `Shared/ConfirmDialog/ConfirmDialogComponent.razor` | "Yes" |
| `Pages/_Layout.cshtml:37` | "Reload" (felraden längst ned) |
| `Pages/Error.cshtml` | "Error" |

Dessutom: **veckodagarna visas på engelska.** `Enums/Days.cs` har `Monday`–`Sunday`, och
`DayCard.razor:13` renderar `@Day.ToString()` rakt ut, liksom dagvalslistan i
`PreviousDishComponent.razor:45`. Det är appens mest synliga text på startsidan.

Två saker värda att veta för den som bygger:

- Enum-värdena är lagrade som heltal (`Monday = 1`), så visningsnamnet kan bytas utan att
  röra data. Men koden matchar `Enum.TryParse` mot `DateTimeOffset.DayOfWeek.ToString()`,
  som alltid ger engelska namn — så själva enum-medlemmarna bör **behålla sina engelska
  namn** och översättas vid visning. Byter man namnen går dagberäkningen sönder tyst.
- "Yes" i bekräftelsedialogen har ingen synlig motpart i samma kodrad; kontrollera att
  avbryt-knappen finns och också är svensk.

## Omfattning
- Allt användarsynligt i gränssnittet skrivs på svenska.
- Veckodagar visas på svenska, utan att enum-medlemmarnas namn ändras.
- Sidtiteln i webbläsarfliken ("WhatToEatApp") och felsidan ingår.
- Texterna får vara hårdkodade på svenska, precis som de engelska är idag.

**Utanför omfattning: lokaliseringsinfrastruktur.** Ingen `.resx`, inga resursfiler, ingen
språkväljare, ingen `IStringLocalizer`. Appen har en användargrupp som talar ett språk;
att bygga ett översättningslager för det vore maskineri utan användare. Om appen någon gång
ska tala flera språk är det en egen story då.

Utanför omfattning också: kod, klassnamn, kommentarer, commit-meddelanden, loggtexter och
`Error.cshtml`:s "Development Mode"-block (det är utvecklarens vy, inte användarens).

## UX-acceptanskriterier
- [ ] Ingen användarsynlig engelsk text finns kvar i någon vy.
- [ ] Veckodagarna visas på svenska både på startsidans kort och i "Ät igen"-listan, och rätt
      dag är fortfarande rätt dag — dagberäkningen får inte påverkas.
- [ ] Knapptexter är verb i samma form genom hela appen; "Spara" och "Avbryt" heter likadant
      överallt de förekommer.
- [ ] Bekräftelsedialogen har svenska svarsalternativ, och de säger vad som händer ("Ta bort"
      / "Avbryt") hellre än "Ja" / "Nej".
- [ ] Webbläsarfliken visar en svensk titel.
- [ ] Felsidan och felraden längst ned ("Reload") är svenska.
- [ ] Laddningstexter är svenska.
- [ ] Tomma tillstånd är svenska.
- [ ] Texterna är skrivna som svenska, inte ordagrant översatta: sidrubrikerna "What to eat"
      och "All dishes, Wow!" ska ersättas med det de faktiskt betyder för användaren, inte
      med "Vad äta" och "Alla rätter, Wow!".
- [ ] Ingen funktion ändras — det här är enbart text.

## Flöde
1. Användaren öppnar startsidan och ser svenska veckodagar och svensk rubrik.
2. Användaren lägger till en rätt — modalens fält och knappar är svenska.
3. Användaren går till listan, väljer "Ät igen" och får en svensk dagvalslista.
4. Användaren raderar en rätt och möts av en svensk bekräftelsedialog.

## Wireframe

```
  FÖRE                                  EFTER
  ┌─────────────────────────┐          ┌─────────────────────────┐
  │ What to eat             │          │ Veckans mat             │
  │                         │          │                         │
  │ ┌─────────────────────┐ │          │ ┌─────────────────────┐ │
  │ │ Monday    (2/10)    │ │          │ │ Måndag    (2/10)    │ │
  │ │ [bild]              │ │          │ │ [bild]              │ │
  │ │ Kålpudding          │ │          │ │ Kålpudding          │ │
  │ │ Ingredients         │ │          │ │ Ingredienser        │ │
  │ │ [Update the dish]   │ │          │ │ [Ändra rätten]      │ │
  │ └─────────────────────┘ │          │ └─────────────────────┘ │
  └─────────────────────────┘          └─────────────────────────┘

  Bekräftelsedialog:
  FÖRE:  Ta bort rätten?  [Yes]  [?]    EFTER:  Ta bort rätten?  [Ta bort] [Avbryt]
         ↑ engelskt ja, oklart nej              ↑ knappen säger vad som händer
```

Rubrikförslag att förhålla sig till, inte att följa slaviskt: "Veckans mat" för startsidan,
"Alla rätter" för `/previousDishes`. Den som bygger får föreslå bättre.

## Klar när
- Genomgång av alla sju vyer utan att någon engelsk sträng syns.
- Veckodagarna svenska, och en rätt som schemaläggs på en dag hamnar fortfarande på rätt dag.
- `Enums/Days.cs` har kvar sina engelska medlemsnamn och `ResolveDayOfWeek` fungerar oförändrat.
- Ingen `.resx`-fil har tillkommit.
