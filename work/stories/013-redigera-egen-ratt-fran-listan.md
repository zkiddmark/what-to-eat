---
story: 013
status: in-progress
issue: 31
---

# Story 013: Redigera min egen rätt var jag än ser den

## Användarvärde
Som användare vill jag kunna redigera en rätt jag äger direkt från listan över rätter, så att
jag kan rätta ett stavfel eller justera ingredienser utan att först behöva schemalägga rätten
på en dag den här veckan.

## Bakgrund
Rapporterat av människan: "logik för att ändra en befintlig rätt ser ut att ha försvunnit".

Jag har grävt i koden. Den *är* inte borttagen — men den har aldrig funnits där man letar:

- `DayCard.razor` har knappen "Update the dish" bakom `TodaysDish.CanEdit`. Den öppnar
  `DayCardModal`, som är den enda redigeringsvyn i appen. Den syns bara för en rätt som
  ligger på en dag i den aktuella veckan.
- `PreviousDishComponent.razor` — kortet i `/previousDishes`, där alla rätter listas — har
  bara "Eat again!" och en **raderaknapp** bakom `CanEdit`. Ingen redigera-knapp har någonsin
  funnits där (`git log` på filen: knappen saknas även före story 008).

Resultatet: äger man en rätt som inte råkar vara schemalagd just nu kan man radera den, men
inte ändra den. Det är en obehaglig asymmetri — den destruktiva handlingen finns, den
ofarliga saknas.

Servern är redan redo. `DishService.UpdateDishAsync` anropar `EnsureMayEdit`, och
`DishDto.CanEdit` sätts i `DecorateAsync` för alla listade rätter. Det som saknas är vägen in
från listan.

## Omfattning
- Redigera-knapp på rättkortet i `/previousDishes`, för den som äger rätten (och för admin).
- Knappen öppnar samma redigeringsmodal som `DayCard` använder, så en rätt redigeras likadant
  oavsett varifrån man kommer.
- Listan uppdateras när ändringen sparats.

Utanför omfattning: att ändra vem som äger en rätt, massredigering, ny redigeringsvy — modalen
som finns ska återanvändas, inte dubbleras.

## Säkerhetskriterier
- [ ] Redigera-knappen visas bara när `CanEdit` är sant — men det är enbart kosmetik.
      Servern avgör fortfarande via `EnsureMayEdit`, och ett anrop på någon annans rätt ska
      avvisas även om knappen tvingas fram i klienten.
- [ ] Ägarskapet får inte kunna ändras via redigeringen: en sparad ändring behåller
      rättens ursprungliga `OwnerId`.

## UX-acceptanskriterier
- [ ] På varje rätt jag äger i `/previousDishes` finns en redigera-knapp, placerad intill
      raderaknappen men tydligt skild från den — den ofarliga handlingen ska inte se ut som
      den farliga.
- [ ] På rätter jag inte äger visas varken redigera eller radera, precis som idag.
- [ ] Knappen öppnar samma modal som från veckovyn, ifylld med rättens nuvarande värden.
- [ ] När jag sparat syns ändringen direkt i listan, utan att jag behöver ladda om sidan
      eller tappa min plats i pagineringen.
- [ ] Avbryter jag modalen ändras ingenting.
- [ ] Laddning: sparaknappen visar att det pågår och går inte att trycka två gånger.
- [ ] Fel: om sparandet nekas av servern får jag veta att jag inte får ändra rätten, på
      svenska, och modalen stänger inte som om det gått bra. Tekniskt fel har eget meddelande.
- [ ] Tomt tillstånd: oförändrat — listans befintliga tomma tillstånd berörs inte.
- [ ] Knappen har läsbar etikett för skärmläsare; en ikon ensam räcker inte.

## Flöde
1. Användaren öppnar `/previousDishes`.
2. På en rätt hen äger syns en redigera-knapp.
3. Användaren trycker på den — redigeringsmodalen öppnas med rättens värden.
4. Användaren ändrar och sparar.
5. Modalen stängs, kortet i listan visar de nya värdena.
6. Vid avbryt eller fel: kortet är oförändrat och användaren får veta varför.

## Wireframe

```
  ┌────────────────────────────────────────────────────────┐
  │ [bild]  Klassisk kålpudding                            │
  │         ★★★★☆ 4,3 (3 röster)   av Peter                │
  │         Din röst: ☆ ☆ ★ ★ ★                            │
  │                                                        │
  │         Anteckningar...                                │
  │                                                        │
  │  [ Eat again! ▾ ]                      [✎]      [🗑]   │
  │                                         ↑        ↑     │
  │                              redigera (neutral)  radera │
  │                              — båda bara om jag äger   │
  └────────────────────────────────────────────────────────┘
```

Redigera ligger före radera i läsordningen och har neutral, inte röd, färg. Radera behåller
sin bekräftelsedialog; redigera behöver ingen.

## Klar när
- Ägaren kan redigera en rätt från `/previousDishes` utan att schemalägga den först.
- Samma modal används från både veckovyn och listan.
- En användare kan inte redigera någon annans rätt, varken via knapp eller direkt anrop.
- En sparad ändring behåller rättens ursprungliga ägare.
