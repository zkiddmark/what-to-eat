---
story: 005
status: planned
issue: 12
---

# Story 005: Föräldralösa bilder ska inte följa med till SQLite

Prioritet: byggs **före** story 003.

## Användarvärde
Som ägare av appen vill jag inte släpa med bilder som ingen rätt pekar på, så att databasen
bara innehåller sådant som faktiskt används och backuperna inte växer av skräp.

I produktionsdatan är **9 av 24 bilder föräldralösa** — ingen rätt refererar dem. De kommer
från buggen i story 003: bilden laddades upp, men rätten fick aldrig kopplingen. De har aldrig
visats för någon användare och kan inte kopplas tillbaka till rätt rätt, för kopplingen har
aldrig funnits.

## Varför det inte blir en radering
Story 002:s migrering är mergad men **ännu inte driftsatt** — prod kör fortfarande LiteDB och
någon `whattoeat.sqlite` finns inte på servern. Det betyder att vi inte behöver radera något
någonstans: det räcker att migreringen låter bli att ta med de föräldralösa bilderna.

Det är den enkla och ofarliga vägen. Ingen fil raderas, inget skrivs till produktionsdatan,
och den gamla LiteDB-filen behålls som rollback med samtliga 24 bilder kvar. Blir det fel går
allt att köra om.

Detta ersätter kravet i story 002 om att föräldralösa bilder skulle följa med. Uppdatera den
formuleringen i `work/stories/002-migrera-litedb-till-sqlite.md` så att historiken hänger ihop.

## Omfattning
- `LiteDbToSqliteMigrator`: hoppa över bilder som ingen rätt refererar via `ImageId`.
- Rapportera antalet överhoppade bilder i sammanfattningen.

Utanför omfattning: att radera ur en redan migrerad SQLite-fil, och att röra produktionsfilen.

## UX-acceptanskriterier
- [ ] Migreringen mot en kopia av `WhatToEat-20260918.db` rapporterar `31 lästa, 31 skrivna`
      rätter och `24 lästa, 15 skrivna` bilder, plus `9 överhoppade (föräldralösa)`.
- [ ] Alla 15 rätter som har en `ImageId` visar fortfarande sin bild — ingen bild som används
      hoppas över.
- [ ] Källfilen är fortsatt orörd; checksumman är identisk före och efter.
- [ ] Körs migreringen mot data helt utan föräldralösa bilder rapporteras `0 överhoppade`.
- [ ] Sammanfattningen är begriplig på svenska och säger varför bilder hoppades över.

## Flöde
1. Driftsättningen kör migreringsverktyget.
2. Verktyget skriver ut hur många bilder som togs med och hur många som hoppades över.
3. Appen startar mot SQLite-filen och visar samma rätter och samma bilder som förut.

## Wireframe
Ingen layoutförändring. Enda ytan är konsolutskriften:

```
Migrerar WhatToEat.db -> whattoeat.sqlite
  Rätter:  31 lästa, 31 skrivna
  Bilder:  24 lästa, 15 skrivna, 9 överhoppade (ingen rätt pekar på dem)
  Klart. Originalfilen är orörd.
```

## Klar när
- Migreringen körd mot en kopia av prod-backupen med siffrorna ovan.
- De 15 använda bilderna verifierat identiska (md5) med originalen.
