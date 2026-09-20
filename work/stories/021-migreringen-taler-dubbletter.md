---
story: 021
status: planned
issue: 50
---

# Story 021: Migreringen till MealPlans måste tåla dubbletter

**Brådskande. Bygg denna före nästa produktionsdeploy.** Story 020 är mergad med ett känt
stoppfel i migreringen: en produktionsstart kan hamna i kraschloop.

## Användarvärde
Som ägare av appen vill jag att uppgraderingen till personlig veckoplanering går igenom
oavsett vad som råkar ligga i databasen, så att produktionen inte står still efter en deploy.

## Bakgrund
Reviewer satte `request-changes` på PR #49 för den här punkten. PR:en mergades innan den
åtgärdades, och jag har kontrollerat `Migrations/20260920121425_AddMealPlan.cs` på main:
**felet är kvar.**

Migreringen skapar det unika indexet `(UserId, Date)` på rad 44-48 och kopierar därefter in
rader utan att hantera att flera rätter kan ha samma ägare och samma datum:

```sql
INSERT INTO MealPlans (Id, UserId, DishId, Date)
SELECT ..., OwnerId, Id, date("When")
FROM ( SELECT ... FROM Dishes WHERE date("When") >= date('now') );
```

Finns två sådana rätter fälls INSERT:en:

```
SQLite Error 19: 'UNIQUE constraint failed: MealPlans.UserId, MealPlans.Date'
EXIT=134 — appen startar inte
```

Databasen överlever (transaktionen rullas tillbaka — `Dishes` orörd, `When` kvar, migreringen
oregistrerad), så det är ingen dataförlust. Men appen kommer inte upp, och ingen efterhandsfix
hjälper. Det är samma sorts fel som story 010 handlade om.

**Det är inte ett konstruerat fall.** Reviewer framkallade läget genom appens eget
gränssnitt på main:

1. Öppna `/previousDishes` med fler än tio rätter, alltså två sidor.
2. Planera in en rätt från **sida 2** på måndag.
3. Gå till **sida 1** och planera in en annan rätt på måndag.

Båda blir kvar på måndag. Orsaken är `PreviousDishes.razor:58`:
`Dishes.SingleOrDefault(x => x.When.Date == e.DtWhen.Date)` — `Dishes` är bara den laddade
sidan, så en rätt på en annan sida hittas aldrig och får aldrig sitt `MinValue`. Det är
punkt 2 i story 020:s egen bakgrund, i en variant som lämnar spår i datan.

Vår produktionsdatabas hade 31 rätter vid story 009. Den har alltså mer än två sidor, och
den har levt med den här buggen hela tiden.

## Omfattning
- Migreringen kopierar högst en rad per `(OwnerId, datum)`.
- Inget annat. Story 020:s funktion är verifierad och ska inte röras.

Utanför omfattning: sidbrytningsbuggen i `PreviousDishes.razor:58` — den upphör att existera
i och med story 020, eftersom planeringen inte längre går via `Dish.When`. Det är historisk
data den lämnat efter sig som är problemet, och det är migreringens sak att hantera.

## Lösningsriktning
Reviewers förslag, som jag ställer mig bakom men inte låser utformningen på: behåll raden med
högst `rowid` per `(OwnerId, date("When"))`. Gamla `GetTodaysDish` använde `LastOrDefault`,
så den rätt användaren faktiskt *såg* på dagen var den sista — `max(rowid)` bevarar alltså
det som visades, vilket är rätt tolkning av användarens avsikt.

Migreringen ska klara vilken produktionsdatabas som helst. Den får inte förutsätta ett
villkor som aldrig har varit garanterat.

## Acceptanskriterier
- [ ] Migreringen går igenom mot en databas där två rätter har samma ägare och samma datum.
- [ ] Den rad som behålls är den som appen visade före migreringen — den sista.
- [ ] Migreringen ger fortfarande samma resultat som idag på data utan dubbletter:
      framtida `When` migreras, förflutna och `MinValue` gör det inte, ingen rätt ändras.
- [ ] Appen startar efter migreringen, verifierat genom att appen faktiskt körs.
- [ ] Ingen rätt går förlorad — `Dishes` har lika många rader efter som före.
- [ ] Den planering som behålls är synlig i veckovyn efteråt, alltså läsbar för EF.

## Klar när
- Migreringen körd mot en kopia av produktionsdatabasen, och appen startar.
- Migreringen körd mot en databas som framkallats via stegen ovan (två rätter samma dag),
  och appen startar.
- Ingen `UNIQUE constraint failed: MealPlans.UserId, MealPlans.Date` i loggen.
