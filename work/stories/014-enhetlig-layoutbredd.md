---
story: 014
status: done
issue: 34
---

# Story 014: Enhetlig layoutbredd i alla vyer

## Användarvärde
Som användare vill jag att appen ser ut att vara *en* app oavsett vilken vy jag är i, och att
den är läsbar både på min telefon och på en bred skärm, så att innehållet inte hoppar i sidled
när jag navigerar och inte sträcks ut till oläsliga rader när fönstret är stort.

## Bakgrund
Rapporterat av människan. Jag har mätt upp vad som faktiskt gäller idag — varje vy har sin
egen breddregel, och ingen av dem känner till de andra:

| Vy | Breddregel idag |
|---|---|
| `Shared/NavMenu.razor:5` | `max-width: 520px` inline |
| `Pages/Index.razor:5` | `max-width: 1040px` inline, satt på `.row` |
| `Pages/PreviousDishes.razor:8` | `max-width: 1040px` inline, satt på `.row` |
| `Pages/Login`, `Register`, `Account`, `ForcedPasswordChange` (alla `:20`) | `col-sm-9 col-lg-6`, ingen maxbredd |
| `Pages/Admin/Users.razor` | ingen breddbegränsning alls |

Konsekvenserna:

- **Menyn står inte över innehållet.** Navbaren är 520 px, innehållet 1040 px. Menyn ser
  felcentrerad ut i förhållande till det den navigerar mellan.
- **Formulären växer obegränsat.** `col-lg-6` är *halva fönstret*, inte en maxbredd. På en
  ultrabred skärm blir inloggningsrutan omkring 1500 px — ett fält man drar musen längs.
- **Adminvyn tar hela bredden** och delar därmed inte utseende med någon annan vy.
- **Inget sidoutrymme på mobil.** `site.css` sätter `.content { padding: 0px; margin: 0px; }`,
  så innehållet ligger kant i kant med skärmen på en telefon.
- Breddvärdena står som **inline-style på fyra ställen** — det finns ingen plats att ändra
  bredden på, bara fyra ställen att glömma.

## Omfattning
- Ett gemensamt layoutskal som alla vyer sitter i: samma maxbredd, samma sidopadding, samma
  centrering. Navmenyn följer samma bredd som innehållet.
- Maxbredden definieras **en gång** (CSS-variabel eller klass i `site.css`), inte som inline-style
  per vy. De fyra inline-`max-width` tas bort.
- Rimlig läsbredd på stor skärm: innehållet slutar växa och centreras i stället.
- Formulärvyerna (login, register, konto, tvingat byte) får en egen, smalare maxbredd — ett
  formulär ska inte vara lika brett som en lista med rätter — men centreras i samma skal.
- Fungerande sidopadding på små skärmar, så inget innehåll klistrar i skärmkanten.

Utanför omfattning: nya färger, ny typografi, nytt komponentbibliotek, omdesign av enskilda
kort eller modaler. Det här handlar om skalet och bredden, inte om utseendet inuti.

## UX-acceptanskriterier
- [ ] Alla vyer — startsidan, `/previousDishes`, `/admin/anvandare`, login, register, konto,
      tvingat byte — har samma vänster- och högerkant. Navigerar jag mellan dem hoppar
      innehållet inte i sidled.
- [ ] Navmenyn har samma bredd och kanter som innehållet under sig.
- [ ] På en bred skärm (≥ 2560 px) slutar innehållet växa vid en rimlig maxbredd och
      centreras. Radlängden förblir läsbar.
- [ ] På telefon (360 px) finns luft mellan innehåll och skärmkant i alla vyer, och inget
      går utanför skärmen i sidled — ingen horisontell scroll någonstans.
- [ ] Formulärvyerna är smalare än listvyerna, men centrerade i samma skal och med samma
      kanter som varandra.
- [ ] Rättkorten och veckovyn ligger kvar i sitt rutnät och bryter till färre kolumner när
      det blir trångt, i stället för att krympa till oläslighet.
- [ ] Maxbredden är definierad på ett ställe i CSS. En sökning efter `max-width` i
      `.razor`/`.cshtml` ger inga träffar kvar.
- [ ] Inget befintligt beteende går förlorat: alla knappar, dialoger och modaler går att nå
      och använda i alla tre bredderna nedan.
- [ ] Tomt tillstånd, laddning och fel: oförändrade i innehåll, men de ska ligga inom samma
      skal och kanter som övrigt innehåll — inga felbanners i full skärmbredd.

## Flöde
1. Användaren öppnar appen på sin telefon och ser innehållet med luft mot kanterna.
2. Användaren växlar mellan startsidan, listan och sitt konto — kanterna ligger stilla.
3. Samma användare öppnar appen på en bred skärm: innehållet centreras i stället för att
   sträckas ut.
4. Användaren ändrar fönsterbredden mellan dessa lägen — layouten byter kolumnantal utan att
   något hamnar utanför skärmen.

## Wireframe

```
  Telefon (360px)            Laptop (1440px)                Ultrabred (2560px)
  ┌──────────────┐    ┌───────────────────────────┐   ┌──────────────────────────────────┐
  │▏ [nav]     ▕│    │      ▏  [nav]          ▕   │   │        ▏   [nav]           ▕     │
  │▏          ▕│    │      ▏                 ▕   │   │        ▏                   ▕     │
  │▏ ┌────────┐▕│    │      ▏ ┌─────┐ ┌─────┐ ▕   │   │        ▏ ┌─────┐ ┌─────┐   ▕     │
  │▏ │ rätt   │▕│    │      ▏ │rätt │ │rätt │ ▕   │   │        ▏ │rätt │ │rätt │   ▕     │
  │▏ └────────┘▕│    │      ▏ └─────┘ └─────┘ ▕   │   │        ▏ └─────┘ └─────┘   ▕     │
  │▏ ┌────────┐▕│    │      ▏ ┌─────┐         ▕   │   │        ▏ ┌─────┐           ▕     │
  │▏ │ rätt   │▕│    │      ▏ │rätt │         ▕   │   │        ▏ │rätt │           ▕     │
  │▏ └────────┘▕│    │      ▏ └─────┘         ▕   │   │        ▏ └─────┘           ▕     │
  └──────────────┘    └───────────────────────────┘   └──────────────────────────────────┘
    en kolumn,          två kolumner,                   samma maxbredd som laptop,
    luft mot kant       innehållet fyller skalet        bara mer tomrum runt om

  ▏ ▕ = samma kanter i alla vyer, nav inräknad
```

Formulärvyerna (login, konto, …) använder samma yttre skal men ett smalare innehållsfält,
centrerat:

```
  ┌───────────────────────────┐
  │      ▏  [nav]         ▕   │
  │      ▏   ┌─────────┐  ▕   │
  │      ▏   │ formulär│  ▕   │   <- smalare än listvyerna,
  │      ▏   └─────────┘  ▕   │      men samma yttre kanter
  └───────────────────────────┘
```

## Klar när
- Appen granskad i tre bredder: 360 px, 1440 px och 2560 px.
- Alla sju vyer har samma kanter i alla tre bredderna.
- Ingen horisontell scroll i någon vy vid 360 px.
- `grep -rE "max-width" Pages Shared --include='*.razor' --include='*.cshtml'` ger noll träffar.
- Maxbredden går att ändra på ett ställe och slår igenom i hela appen.
