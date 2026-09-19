---
story: 004
status: in-review
issue: 11
---

# Story 004: Stabil sortering när rätter har samma betyg

## Användarvärde
Som användare vill jag att listan "Tidigare rätter" har en förutsägbar ordning även när flera
rätter har samma betyg, så att listan ser likadan ut varje gång jag öppnar den och rätter inte
tycks hoppa omkring.

## Bakgrund
`GetAllDishes` sorterar bara på `OrderByDescending(x => x.Rating)`. Rätter med samma betyg får
ingen definierad inbördes ordning. Vid migreringen från LiteDB till SQLite (story 002) bytte
just sådana rätter plats — ordningen är stabil mellan anrop, så paginering hoppar inte över
eller dubblerar rätter, men den är godtycklig och kan ändras av framtida ändringar.

Prioriterad lågt: ingen data går förlorad och ingen användare är blockerad.

## Omfattning
- Lägg till en sekundär sortering i `GetAllDishes`, förslagsvis `When` fallande, så att den
  senast lagade rätten kommer först bland lika betygsatta.
- Samma sortering ska gälla i pagineringens alla sidor.

Utanför omfattning: valbar sortering i UI:t, ny sorteringsordning för betyget självt.

## UX-acceptanskriterier
- [ ] Rätter med samma betyg visas i fallande datumordning — senast lagad först.
- [ ] Listan ser identisk ut vid upprepade besök och sidladdningar.
- [ ] Att bläddra framåt och bakåt genom sidorna visar varje rätt exakt en gång — ingen rätt
      dubbleras eller hoppas över vid sidgränserna.
- [ ] Tomt tillstånd och laddning oförändrade.
- [ ] Ingen förändring för rätter med unika betyg — de ligger kvar där de låg.

## Flöde
1. Användaren öppnar "Tidigare rätter".
2. Listan visar högst betygsatta först, och inom samma betyg senast lagad först.
3. Användaren bläddrar till sida 2 och tillbaka till sida 1 — samma rätter, samma ordning.

## Wireframe
Ingen layoutförändring. Endast radordningen inom en betygsgrupp ändras.
