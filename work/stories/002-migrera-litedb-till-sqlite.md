---
story: 002
status: draft
issue: 7
---

# Story 002: Migrera från LiteDB till SQLite med EF Core

Beroende: bygger på story 001 (net10.0). Bör göras efter att 001 är mergad.

## Användarvärde
Som användare vill jag att all min befintliga data — maträtter, betyg, ingredienser,
anteckningar och uppladdade bilder — finns kvar oförändrad efter bytet av databas,
så att jag inte förlorar något jag lagt in genom åren.

Som utvecklare vill jag ha SQLite via EF Core istället för LiteDB, så att datamodellen
har ett standardiserat, verktygsstött persistenslager med migrations och riktiga queries.

## Omfattning
- Dra in `Microsoft.EntityFrameworkCore.Sqlite` + design-time-paket.
- `AppDbContext` med `Dish` som entitet; EF Core-migration som skapar schemat.
- Ersätt `ILiteDbService`/`LiteDbService` med DbContext; skriv om `DishService` mot EF Core.
- Byt nyckeltyp: LiteDB:s `ObjectId` försvinner ur `Entities/Dish`, `DishDto` och
  `Shared/PreviousDish/PreviousDishEventArgs`. Ny nyckel (t.ex. `Guid`) beslutas i planen.
- `Ingredients` (`IList<string>`) måste få en representation i SQLite — värdekonverterare
  till JSON-kolumn eller egen tabell. Beslutas i planen.
- Bildlagring: idag LiteDB FileStorage (`wteImages`/`wteChunks`). SQLite saknar motsvarighet.
  Architect väljer i planen mellan **BLOB-kolumn** (allt kvar i en fil, minst kodändring,
  databasen växer) och **filer på disk med sökväg i databasen** (liten databas, men två
  saker att backa upp och Docker-volymen måste täcka båda). Motivera valet i planen.
- **Migreringsverktyg**: en engångskörning som läser befintlig `WhatToEat.db` (LiteDB) och
  skriver över allt till SQLite — inklusive bilderna. Måste vara körbar mot produktionsfilen.
- `appsettings.json`: ny connection string; Docker-volymen ska fortsatt peka på `/app/data`.

Utanför omfattning: ny funktionalitet, UI-förändringar, omdesign av datamodellen utöver
vad bytet kräver.

## UX-acceptanskriterier
- [ ] Efter migrering visar "Tidigare rätter" exakt samma antal rätter som före, med samma
      titlar, betyg, anteckningar, ingredienslistor, länkar och datum.
- [ ] Alla bilder som fanns före migreringen visas efter migreringen — ingen trasig bildruta.
- [ ] Sorteringen i listan är oförändrad (fallande betyg) och pagineringen ger samma sidor.
- [ ] "Dagens rätt" väljer samma rätt för samma veckodag som före migreringen.
- [ ] Lägga till, redigera och ta bort en rätt fungerar som tidigare, inklusive att en
      utbytt bild tar bort den gamla bilden.
- [ ] Tomt tillstånd: en tom databas ger samma tomtillståndsvy som idag — inte ett fel.
- [ ] Laddning: listan känns inte långsammare än tidigare vid normal datamängd.
- [ ] Fel: om databasen saknas eller inte går att öppna visas ett begripligt felmeddelande
      på svenska med vad användaren kan göra — inte en stacktrace eller vit sida.
- [ ] Migreringen är körbar mot en kopia och **rör aldrig originalfilen** — den läses bara.
- [ ] Rullar man tillbaka till föregående version fungerar appen fortfarande mot den gamla
      LiteDB-filen (den raderas inte som en del av storyn).

## Flöde
1. Driftsättningen kör migreringsverktyget mot befintlig `WhatToEat.db`.
2. Verktyget rapporterar hur många rätter och hur många bilder som flyttats.
3. Appen startar mot den nya SQLite-filen.
4. Användaren öppnar appen och ser sin lista precis som förut.
5. Användaren öppnar en gammal rätt med bild — bilden visas.
6. Användaren lägger till en ny rätt med bild — den sparas i SQLite och visas i listan.

## Wireframe
Ingen layoutförändring. Enda nya "yta" är migreringsverktygets konsolutskrift, som ska
sluta med en tydlig sammanfattning:

```
Migrerar WhatToEat.db -> whattoeat.sqlite
  Rätter:  <n> lästa, <n> skrivna
  Bilder:  <n> lästa, <n> skrivna
  Klart. Originalfilen är orörd.
```

Vid fel: vilken rätt som fallerade och varför, och att inget skrivits (allt-eller-inget).

## Klar när
- Migreringen körd mot en kopia av produktionsdatan med noll tappade rätter och bilder.
- Före/efter-jämförelse av listvy och en bildrätt gjord.
- Inga referenser till LiteDB kvar i koden utom i migreringsverktyget.
