---
story: 002
status: done
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
- [ ] Migreringen går igenom mot en kopia av `WhatToEat-20260918.db` med exit-kod 0 och
      rapporterar `31 lästa, 31 skrivna` rätter och `24 lästa, 24 skrivna` bilder.
- [ ] Migreringen är körbar mot en kopia och **rör aldrig originalfilen** — den läses bara.
- [ ] Rullar man tillbaka till föregående version fungerar appen fortfarande mot den gamla
      LiteDB-filen (den raderas inte som en del av storyn).

## Testdata

En verifierad kopia av produktionsdatabasen finns lokalt, utanför repot:

```
~/what-to-eat/prod-backup/WhatToEat-20260918.db   (8,6 MB, LiteDB v5)
```

Hämtad från `peter@webserver01:~/what_to_eat/WhatToEat.db` 2026-09-18, md5 verifierad mot
originalet (`7c462fc6...`) och satt read-only. Migreringen ska verifieras mot **en kopia av
denna fil**, inte mot filen på servern och inte mot originalkopian.

Filen får aldrig committas — den innehåller produktionsdata.

### Vad produktionsdatan faktiskt innehåller (mätt 2026-09-18)

31 rätter, 24 bilder. Fält som saknas eller är null i BSON — det är dessa migreringen
måste tåla, och det är här den seedade testfilen ljög:

| Fält | saknad nyckel | null | konsekvens |
|---|---|---|---|
| `Notes` | 10 | 5 | **15 av 31 rätter fäller `NOT NULL`-villkoret** |
| `ImgUrl` | 8 | 6 | ok, nullbar kolumn |
| `RecipeUrl` | 7 | 6 | ok, nullbar kolumn |
| `Ingredients` | 3 | 0 | ok, `IsArray`-kontrollen ger tom lista |
| `ImageId` | 16 | 0 | ok, nullbar kolumn |
| `Title`, `Rating`, `When` | 0 | 0 | alltid satta i denna datamängd |

Dessutom: 9 av 24 bilder är föräldralösa (ingen rätt pekar på dem) och alla bild-id:n är
giltiga Guid:er. Föräldralösa bilder ska följa med — de finns i LiteDB idag.

`Notes` ska mappas till `string.Empty` när nyckeln saknas eller är null, vilket är samma
värde som `Entities/Dish` redan har som default och samma tomma fält som användaren ser idag.

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
- Migreringen körd mot en kopia av `WhatToEat-20260918.db` med noll tappade rätter och bilder.
- Före/efter-jämförelse av listvy och en bildrätt gjord.
- Inga referenser till LiteDB kvar i koden utom i migreringsverktyget.

## Verifierat efter merge (2026-09-18, mergad main `0bfea30`)

Kört mot en kopia av prod-backupen:

```
  Rätter:  31 lästa, 31 skrivna
  Bilder:  24 lästa, 24 skrivna
```

Maskinell fält-för-fält-jämförelse mellan LiteDB (läst via `BsonMapper`, som appen läste den)
och SQLite: **noll skillnader** i `Title`, `Notes`, `ImgUrl`, `RecipeUrl`, `Ingredients`,
`Rating`, `ImageId`, i `When`-instanten, i det visade datumet, och i bildernas md5.
`docker build` grönt. Källfilens checksumma oförändrad före/efter.

`AsDateTime` visade sig ge `Kind=Local`, så `ToLocalOffset` blir en korrekt rundtur — inte
den timmes förskjutning som riskerade att uppstå.

Kvar, otestat: bilduppladdning genom UI:t. Appen kan inte startas lokalt utan
`ExcludedSecrets/firebaseConfig.json`, som inte finns i repot.

Mindre avvikelse, inte åtgärdad: rätter med **samma betyg** sorteras i annan inbördes ordning
än före migreringen, eftersom `OrderByDescending(Rating)` saknar sekundär sortering. Ordningen
är stabil mellan anrop, så paginering hoppar inte över eller dubblerar rätter — men listan ser
inte exakt ut som förut. Se story om sekundär sortering.
