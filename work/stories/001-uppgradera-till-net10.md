---
story: 001
status: done
issue: 6
---

# Story 001: Lyft appen till senaste .NET (net8.0 → net10.0)

## Användarvärde
Som användare av What To Eat vill jag att appen körs på en aktuell, supportad .NET-version
så att den fortsätter vara säker, snabb och möjlig att vidareutveckla — utan att något i
appens beteende förändras för mig.

Som utvecklare vill jag ha en modern runtime och aktuella paket så att framtida stories
kan byggas utan att först röja teknisk skuld.

## Omfattning
- `WhatToEatApp.csproj`: `TargetFramework` net8.0 → net10.0.
- `dockerfile`: base- och sdk-images till `10.0`.
- NuGet-paket (LiteDB 5.0.17) till senaste stabila version som fungerar på net10.0.
- Åtgärda alla nya varningar/fel som uppgraderingen orsakar — inga andra ändringar.

Utanför omfattning: ny funktionalitet, redesign, refaktorering av kod som redan fungerar.

## UX-acceptanskriterier
- [ ] Appen startar och alla befintliga vyer (inloggning, listor, val av maträtt) fungerar
      exakt som före uppgraderingen — ingen synlig förändring för användaren.
- [ ] Inloggning/auth-flödet fungerar oförändrat; en redan inloggad användare kastas inte ut.
- [ ] Befintlig LiteDB-data läses utan migrering — inga maträtter eller listor försvinner.
- [ ] Tomt tillstånd: vyer utan data visar samma tomtillståndstext som tidigare.
- [ ] Laddning: inga nya, längre eller "hängande" laddningstillstånd introduceras vid start
      eller navigering.
- [ ] Fel: om något går fel visas samma felhantering som tidigare — inga råa stacktraces
      eller ASP.NET-standardfelsidor exponeras för användaren.
- [ ] Tillgänglighet: tangentbordsnavigering och fokusordning är oförändrade.
- [ ] Docker-imagen byggs och containern startar på nya basimagen.

## Flöde
1. Användaren öppnar appen på samma URL som tidigare.
2. Användaren loggar in (eller är redan inloggad) — samma formulär, samma fel vid fel lösen.
3. Användaren navigerar mellan sidorna och ser sin befintliga data.
4. Användaren utför det den brukar (lägga till/välja maträtt) och resultatet sparas.
5. Användaren märker ingen skillnad. Det är målet.

## Wireframe
Ingen layoutförändring. Alla vyer behåller exakt nuvarande struktur, hierarki och primär
handling. Verifiering sker genom före/efter-jämförelse av samma skärmar.

## Klar när
- Bygget är grönt på net10.0 utan nya varningar.
- Docker-bygget går igenom.
- Manuell genomgång av flödet ovan gjord mot befintlig databasfil.
