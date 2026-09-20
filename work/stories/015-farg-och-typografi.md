---
story: 015
status: done
issue: 35
---

# Story 015: Färg och typografi som ett system

Följer på story 014. Den storyn ger appen ett gemensamt skal; den här ger den ett gemensamt
utseende inuti skalet. Bygg 014 först — det är enklare att sätta typografi i en layout som
står stilla.

## Användarvärde
Som användare vill jag att appen ser medvetet gjord ut och att jag kan läsa den utan
ansträngning, så att jag snabbt hittar det viktiga på en sida i stället för att alla element
skriker lika högt.

## Bakgrund
Rapporterat av människan som andra halvan av designöversynen. Appen kör Bootstrap 5.1.0 med
standardtemat och har ingen egen palett eller typografisk skala. Vad jag hittade:

- **Motstridiga färgklasser på samma element.** `Index.razor:4` och `PreviousDishes.razor:5`
  har båda `class="text-primary text-muted"` på sidans `<h1>`. `text-muted` vinner, så
  `text-primary` är död kod — sidrubriken är alltså grå, sannolikt utan att någon valt det.
- **`text-muted` är appens de facto designsystem.** Den förekommer **32 gånger** och sitter
  på nästan varje rubrik. När allt är dämpat finns ingen hierarki kvar: sidrubrik,
  sektionsrubrik och hjälptext har samma tyngd.
- **Rubriknivåerna följer inte innehållet.** Sidans rubrik är `<h1>` på startsidan och
  `/previousDishes`, men `<h3>` på login, register, konto, tvingat byte och adminvyn. Nivån
  har valts efter önskad storlek, inte efter struktur — det är också ett
  tillgänglighetsproblem för den som navigerar med skärmläsare.
- **Färger hårdkodade vid sidan av Bootstrap.** `site.css` sätter `#0071c1` för länkar och
  `#1b6ec2`/`#1861ac` för `.btn-primary` — nära, men inte lika med, Bootstraps egna värden.
  Samtidigt sätts länkfärg inline som `color: var(--bs-secondary)` på tre ställen i
  `NavMenu.razor`. Det finns alltså tre konkurrerande källor till vad appens färg är.
- **Typsnittet krockar med sig självt.** `site.css` sätter `'Helvetica Neue', Helvetica,
  Arial` på `html, body`, medan Bootstrap sätter sin egen systemstack. Vilken som vinner
  beror på laddningsordning, inte på ett beslut.
- **Navigationen är en rad likadana `btn-outline-secondary`.** Inget säger vilken vy man är
  i, och ingen knapp är viktigare än någon annan.

## Omfattning
- En palett definierad **en gång** som CSS-variabler: en primärfärg, en textfärg, en dämpad
  textfärg, en bakgrund, en fara-färg. Bootstraps variabler sätts om till dessa i stället för
  att skrivas över på enskilda element.
- En typografisk skala: sidrubrik, sektionsrubrik, brödtext, hjälptext — var och en med
  bestämd storlek och tyngd.
- Rubrikelementen rättas så att nivån följer strukturen: en `<h1>` per sida, på alla sidor.
  Storleken kommer från skalan, inte från vilken nivå man råkat välja.
- `text-muted` behålls bara där texten verkligen är sekundär, inte som standardfärg för allt.
- Aktiv vy markeras i navigationen.
- De hårdkodade hex-värdena i `site.css` och de inline-satta färgerna i `NavMenu.razor` tas
  bort till förmån för paletten.

Utanför omfattning: nytt komponentbibliotek, byte bort från Bootstrap, mörkt läge, nya
ikoner, omskrivning av texter. Layout och bredd hör till story 014.

## UX-acceptanskriterier
- [ ] Varje sida har en synlig rubrik som är tydligt störst på sidan, och det är sidans enda
      `<h1>`.
- [ ] Sidrubrik, sektionsrubrik, brödtext och hjälptext går att skilja åt på storlek och
      tyngd — inte bara på position.
- [ ] Inget element har två motstridiga färgklasser. `text-primary text-muted` finns inte kvar.
- [ ] Appens primärfärg används till det som är den primära handlingen på sidan, och inte
      till annat. Farliga handlingar (radera) behåller sin egen, tydligt skilda färg.
- [ ] Brödtext mot bakgrund når kontrastkravet WCAG AA (4,5:1), och dämpad text likaså.
      Dämpad text får vara svagare än brödtext, men inte oläslig.
- [ ] Jag ser i navigationen vilken vy jag befinner mig i.
- [ ] Ett typsnitt gäller i hela appen, och vilket det är beror inte på laddningsordning.
- [ ] Färgerna är definierade på ett ställe. En sökning efter hex-värden och `color:` i
      `.razor`/`.cshtml` ger inga träffar kvar.
- [ ] Färg är aldrig den enda bäraren av information — ett fel ska kännas igen på text, inte
      bara på att något blivit rött.
- [ ] Tomt tillstånd, laddning och fel: samma innehåll som idag, men de använder paletten och
      skalan i stället för egna avvikande färger.
- [ ] Ingenting i appen ändrar beteende — det här är utseende, inte funktion.

## Flöde
1. Användaren öppnar en vy och ser direkt vad sidan heter och vad den primära handlingen är.
2. Användaren navigerar till en annan vy — samma typsnitt, samma färger, och det syns i menyn
   var hen är.
3. Användaren läser hjälptext och felmeddelanden utan att kisa.

## Wireframe

```
  FÖRE                                 EFTER
  ┌────────────────────────────┐      ┌────────────────────────────┐
  │ [Hem][Alla][Konto][Logga ut]│      │ [Hem]│Alla│[Konto] [Logga ut]│
  │  alla lika, ingen aktiv     │      │        ↑ aktiv vy markerad  │
  │                             │      │                             │
  │ All dishes, Wow!   (grå h1) │      │ Alla rätter        (h1, mörk,│
  │                             │      │                     störst)  │
  │ Kålpudding         (grå h5) │      │ Kålpudding      (h2, tydlig) │
  │ av Peter           (grå)    │      │ av Peter        (dämpad)     │
  │ Anteckningar...    (grå)    │      │ Anteckningar... (brödtext)   │
  │   ↑ allt samma gråa tyngd   │      │   ↑ tre nivåer syns          │
  │                             │      │                             │
  │ [Eat again!] [✎] [🗑]       │      │ [Eat again!]  [✎]      [🗑]  │
  │  alla grå-outline           │      │  primär       neutral   fara │
  └────────────────────────────┘      └────────────────────────────┘
```

Poängen: samma innehåll, men ögat får en ordning att läsa i. Det mest dämpade i "före" är
sidans rubrik — det ska vara det minst dämpade.

## Klar när
- Paletten och skalan är definierade som CSS-variabler på ett ställe och används överallt.
- Varje sida har exakt en `<h1>`.
- `grep -rE "#[0-9a-fA-F]{3,6}|color:" Pages Shared --include='*.razor' --include='*.cshtml'`
  ger noll träffar.
- Kontrasten mätt och godkänd mot WCAG AA för brödtext och dämpad text.
- Appen granskad i de tre bredderna från story 014 — färg och typografi håller i alla tre.
