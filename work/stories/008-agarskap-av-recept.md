---
story: 008
status: done
issue: 18
---

# Story 008: Ägarskap av recept

Etapp 3 av fyra. Bygger på story 007.

## Användarvärde
Som användare vill jag kunna skapa och redigera mina egna recept och läsa allas, så att vi kan
dela en gemensam samling utan att någon råkar skriva om det jag lagt in.

Som admin vill jag kunna redigera vad som helst, så att jag kan städa när det behövs.

## Omfattning
- Recept får en ägare. De 31 befintliga recepten får `peter@stjern.se` som ägare.
- Den som skapar ett recept blir dess ägare.
- Ägaren och admin kan redigera och ta bort receptet. Alla inloggade kan läsa alla recept.
- Receptet visar vems det är.

Utanför omfattning: att lämna över ägarskap, delade recept, röstning (009).

## Säkerhetskriterier
- [ ] Behörighetskontrollen sker på servern, i det lager som utför ändringen — inte bara
      genom att dölja knappar.
- [ ] Ett försök att redigera eller ta bort någon annans recept nekas även om anropet
      konstrueras för hand med ett giltigt recept-id.
- [ ] Ägaren sätts av servern utifrån vem som är inloggad, aldrig av något klienten skickar.

## UX-acceptanskriterier
- [ ] Alla inloggade ser samma lista med alla recept — ingen samling blir osynlig.
- [ ] Varje recept visar vems det är, med ägarens alias.
- [ ] Redigera och ta bort visas bara på recept användaren får ändra.
- [ ] Ett nytt recept får inloggad användare som ägare utan att hen behöver välja något.
- [ ] Admin ser redigera och ta bort på alla recept.
- [ ] Fel: nekas en ändring visas ett begripligt meddelande — inte en tom sida eller en
      stacktrace.
- [ ] Tomt tillstånd: en användare utan egna recept ser fortfarande hela den gemensamma
      listan, inte en tom vy.
- [ ] De 31 befintliga recepten ser oförändrade ut, nu märkta med Peter som ägare.

## Flöde
1. Användaren loggar in och ser hela receptlistan.
2. På egna recept finns redigera och ta bort; på andras finns de inte.
3. Användaren skapar ett nytt recept och blir automatiskt dess ägare.
4. Användaren öppnar någon annans recept, läser det, men kan inte ändra det.

## Wireframe

Receptrad i listan, tillägget är ägarraden och att knapparna villkoras:

```
  ┌────────────────────────────────────────────┐
  │ [bild]  Köttfärslimpa med potatis          │
  │         ★★★★★   av Peter                   │
  │                            [Redigera] [🗑]  │  <- bara för ägare/admin
  └────────────────────────────────────────────┘
  ┌────────────────────────────────────────────┐
  │ [bild]  Chili con carne                    │
  │         ★★★★☆   av Alex                    │
  │                                             │  <- inga knappar
  └────────────────────────────────────────────┘
```

Ägaren skrivs diskret under titeln, underordnad betyget. Ingen ny vy behövs.

## Klar när
- Migrering satt Peter som ägare på samtliga 31 befintliga recept.
- Verifierat att en vanlig användare inte kan ändra någon annans recept, inte heller genom
  ett handgjort anrop.
- Admin kan redigera ett recept hen inte äger.
