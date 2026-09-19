---
story: 009
status: in-review
issue: 19
---

# Story 009: En röst per användare

Etapp 4 av fyra. Bygger på story 008.

## Användarvärde
Som användare vill jag sätta mitt eget betyg på en rätt och se vad alla tycker i snitt, så att
listan speglar hela hushållets smak i stället för den som råkade rösta sist.

## Bakgrund
Idag är `Rating` ett enda tal på rätten som vem som helst kan skriva över. Det finns ingen
koppling till vem som tyckt vad, och den som ändrar sist raderar de andras åsikt utan att
någon märker det.

I produktionsdatan har **15 av 31 rätter betyg 0**, vilket rimligen betyder "aldrig satt"
snarare än "underkänd". De ska inte dras in i ett snitt som om någon aktivt gett dem noll.

## Omfattning
- En röst per användare och rätt, ändringsbar.
- Rätten visar snittet av rösterna och hur många som röstat.
- Listan sorteras på snittet, med samma entydiga ordning som story 004 införde.
- Migrering av de 31 befintliga betygen: ett satt betyg blir Peters röst. Betyg 0 tolkas som
  "ingen röst" och migreras inte.

Utanför omfattning: kommentarer, att se vem som röstat vad, viktning.

## Säkerhetskriterier
- [ ] Rösten knyts till inloggad användare på servern. En användare kan inte rösta i någon
      annans namn eller lägga fler än en röst på samma rätt.
- [ ] Att rösta på ett recept man inte äger är tillåtet — men att ändra själva receptet är
      det fortfarande inte.

## UX-acceptanskriterier
- [ ] Användaren ser sin egen röst tydligt skild från snittet.
- [ ] Att ändra sin röst uppdaterar snittet direkt, utan omladdning.
- [ ] Rätten visar antal röster, så ett snitt byggt på en röst inte ser ut som en sanning.
- [ ] Tomt tillstånd: en rätt utan röster visar att ingen röstat än och bjuder in till att
      rösta — den visas inte som betyg noll.
- [ ] Laddning: betygsraden visar att rösten sparas, och dubbelklick skapar inte två röster.
- [ ] Fel: om rösten inte kan sparas återställs den synliga rösten och användaren får veta
      det — snittet ska aldrig visa något som inte sparats.
- [ ] De 15 rätter som idag har betyg 0 visas efter migreringen som "inga röster än", inte
      som noll stjärnor.
- [ ] Sorteringen är fortfarande entydig: rätter med samma snitt hamnar i samma ordning
      varje gång.

## Flöde
1. Användaren öppnar listan och ser varje rätts snittbetyg och antal röster.
2. Användaren sätter sitt betyg på en rätt.
3. Snittet uppdateras och användarens egen röst markeras.
4. Användaren ändrar sig och sätter ett annat betyg — snittet räknas om, ingen extra röst.

## Wireframe

```
  ┌──────────────────────────────────────────────┐
  │ [bild]  Klassisk kålpudding                  │
  │         ★★★★☆ 4,3  (3 röster)   av Peter     │
  │                                               │
  │         Din röst:  ☆ ☆ ★ ★ ★                 │  <- interaktiv, tydligt egen
  └──────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────┐
  │ [bild]  Spaghetti med korv i tomatsås        │
  │         Inga röster än            av Peter    │
  │         Din röst:  ☆ ☆ ☆ ☆ ☆                 │
  └──────────────────────────────────────────────┘
```

Snittet är det stora, lugna talet; den egna rösten är den enda interaktiva delen av raden.

## Klar när
- Migrering körd mot en kopia av produktionsdatan: 16 rätter med satt betyg får Peters röst,
  15 rätter med betyg 0 får ingen röst.
- Två användare kan rösta olika på samma rätt och snittet stämmer.
- En användare kan inte lägga två röster på samma rätt.
