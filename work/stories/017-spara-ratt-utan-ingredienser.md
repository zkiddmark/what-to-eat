---
story: 017
status: planned
issue: 41
---

# Story 017: Det ska gå att spara en rätt utan ingredienser

## Användarvärde
Som användare vill jag kunna lägga in en rätt med bara ett namn, så att jag snabbt kan skriva
upp "Pizza" eller "Rester" utan att först hitta på en ingredienslista jag inte behöver.

## Bakgrund
Anmäld av Reviewer i issue #40, upptäckt vid regressionsvarvet kring story 015 och
reproducerad på `main` i en körande instans — alltså ingen följd av någon pågående story.

Att skapa en ny rätt utan att lägga till minst en ingrediens kraschar sparningen:

```
SQLite Error 19: 'NOT NULL constraint failed: Dishes.Ingredients'
```

Jag har verifierat orsaken i koden. `Shared/Day/DayCardModal.razor:80`:

```csharp
public IList<string> IngredientList { get; set; } = null!;
```

`EditOrCreateState()` på rad 92 fyller listan **bara** i ändra-grenen (`if (Dish != null)`).
Skapar man en ny rätt förblir `IngredientList` null och skickas vidare till en `NOT NULL`-kolumn.
Därför fungerar det att redigera en befintlig rätt men inte att skapa en ny utan ingredienser.

Värt att notera: `OnCloseHandler` på rad 113 har redan `?? new List<string>()`. Någon har
alltså sett null-risken på ett ställe men inte i initieringen.

Användaren får idag meddelandet *"Rätten kunde inte sparas. Ingenting har ändrats — försök
igen."* Det är fel sorts meddelande: felet går inte över av att man försöker igen, och
texten säger inte vad som saknas. Användaren lämnas att gissa.

## Produktbeslut

Reviewer ställde två frågor. Mina svar:

**1. Ska en rätt utan ingredienser gå att spara? Ja.** Ingredienslistan är ett stöd, inte ett
krav. Att tvinga fram en ingrediens för "Rester" eller "Pizza" är friktion utan värde, och
rätten har redan allt appen behöver: ett namn och en dag. Lösningen är alltså att initiera
listan till en tom lista — inte att lägga till en valideringsspärr.

**2. Felmeddelandet.** Ett fel som användaren själv kan åtgärda och ett tillfälligt tekniskt
fel ska inte låta likadant. "Försök igen" ska bara stå där det faktiskt är meningsfullt att
försöka igen.

## Omfattning
- En ny rätt går att spara utan ingredienser.
- Ingredienslistan är tom, inte null, i skapa-läget.
- Felmeddelandet vid sparning skiljer på fel användaren kan åtgärda och tillfälliga fel.
- Ett kort för en rätt utan ingredienser visar ett vettigt tomt tillstånd.

Utanför omfattning: obligatoriska fält i övrigt, redigering av ingredienser i listvyn,
omdesign av modalen.

## UX-acceptanskriterier
- [ ] Jag kan skapa en rätt med bara en titel och spara — rätten skapas och syns på dagen.
- [ ] Jag kan skapa en rätt med ingredienser precis som idag; inget av det beteendet ändras.
- [ ] Jag kan redigera en befintlig rätt och ta bort alla dess ingredienser och spara. Det
      ska fungera lika bra som att aldrig ha lagt till några.
- [ ] Tomt tillstånd: en rätt utan ingredienser visar ingen tom rubrik "Ingredienser" med
      inget under — antingen döljs sektionen, eller så bjuder den in till att lägga till.
- [ ] Fel: ett fel som användaren kan åtgärda säger vad som saknas och vad hen ska göra. Ett
      tillfälligt tekniskt fel får behålla "försök igen". Samma text används inte för båda.
- [ ] Vid fel står modalen kvar med användarens ifyllda värden — inget skrivet går förlorat.
- [ ] Laddning: sparaknappen visar att det pågår och går inte att trycka två gånger.
- [ ] Titel är fortfarande det enda som faktiskt krävs, och saknad titel fångas i formuläret
      innan sparning, inte av databasen.

## Flöde
1. Användaren trycker "lägg till rätt" på en dag.
2. Användaren skriver bara en titel och sparar.
3. Rätten skapas, modalen stängs, dagen visar rätten.
4. Användaren öppnar rätten igen och lägger till ingredienser i efterhand — det fungerar.

## Wireframe

```
  FÖRE                                     EFTER
  ┌──────────────────────────────┐        ┌──────────────────────────────┐
  │ Lägg till rätt               │        │ Lägg till rätt               │
  │ Namn:  [ Pizza          ]    │        │ Namn:  [ Pizza          ]    │
  │ Ingredienser:                │        │ Ingredienser (valfritt):     │
  │   (inget tillagt)            │        │   (inget tillagt)            │
  │                              │        │                              │
  │        [ Spara ]             │        │        [ Spara ]             │
  │                              │        │                              │
  │ ⚠ Rätten kunde inte sparas.  │        │ ✓ sparad                     │
  │   Ingenting har ändrats —    │        │                              │
  │   försök igen.               │        │                              │
  │   ↑ olösligt, säger inget    │        │                              │
  └──────────────────────────────┘        └──────────────────────────────┘

  Kortet för en rätt utan ingredienser:
  ┌─────────────────────┐
  │ [bild]              │
  │ Pizza               │
  │                     │   <- ingen tom "Ingredienser"-rubrik
  │ [Ändra rätten]      │
  └─────────────────────┘
```

## Klar när
- Stegen i issue #40 följda i en körande instans: rätten skapas, ingen `DbUpdateException`.
- Redigera en rätt och ta bort alla ingredienser — sparas utan fel.
- Loggen fri från `NOT NULL constraint failed: Dishes.Ingredients`.
- Ett framkallat tillfälligt fel visar fortfarande "försök igen"; ett åtgärdbart fel gör det inte.
