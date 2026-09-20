---
story: 020
status: in-review
issue: 48
---

# Story 020: Veckoplaneringen är min egen

## Användarvärde
Som användare vill jag ha min egen veckoplanering, så att det jag planerat att äta är mitt —
och jag vill kunna planera in vilken rätt som helst i huset, även en som någon annan lagt in,
eftersom vi delar receptsamling men inte middagsbord.

## Bakgrund
Beslut av människan. Idag är veckoplaneringen **global och gemensam**: `Entities/Dish.cs` har
fältet `When`, alltså en enda dag per rätt för hela appen. `GetTodaysDish` hämtar
`dishes.LastOrDefault(x => x.When.Date == day...)` utan att fråga vem som tittar. Alla ser
samma vecka.

Det ger tre konkreta fel, och det tredje är en riktig bugg:

1. **En rätt kan bara ligga på en dag i hela huset.** Planerar Peter kålpudding på måndag kan
   Boss inte ha samma rätt på tisdag.
2. **Att planera skriver över någon annans dag.** `Pages/PreviousDishes.razor:58-61`: den rätt
   som redan låg på dagen får `When = DateTimeOffset.MinValue`. Boss planerar tisdag och
   Peters tisdag försvinner, utan förvarning och utan att någon får veta det.
3. **Att planera in någon annans rätt går inte alls.** Schemaläggningen sker genom
   `UpdateDishAsync` (`PreviousDishes.razor:62,66`), som anropar `EnsureMayEdit`. Försöker
   Peter planera in en rätt som Boss äger kastas `ForbiddenException` — "Du får inte ändra
   den här rätten." Det du beskriver som önskat beteende är alltså inte bara ogjort, det är
   spärrat. Ägarskapet från story 008 skyddar rätten, men schemaläggningen råkade åka med.

Roten är att *när jag ska äta något* lagras som en egenskap på *rätten*. Det är två olika
saker: rätten är delad, planeringen är personlig.

## Omfattning
- Veckoplaneringen blir personlig: varje användare ser och styr sin egen vecka.
- Planeringen flyttas från `Dish.When` till en egen post per användare, dag och rätt.
- Vem som helst kan planera in vilken rätt som helst, oavsett vem som äger den. Att planera
  är inte att ändra, och ska inte längre gå via `UpdateDishAsync`.
- Två användare kan ha samma rätt på samma dag utan att påverka varandra.
- Migrering av befintlig planering.

Utanför omfattning: att se någon annans vecka, delad hushållsplanering, planering längre än
innevarande vecka, inköpslistor, att kopiera någons vecka.

## Produktbeslut

**Min vecka är min egen — inte synlig för andra.** Du bad om att planeringen ska följa
användaren, inte om ett fönster in i andras middagar. Enklast och minst överraskande är att
veckovyn visar precis mitt, och att rätterna förblir det gemensamma. Vill vi senare kunna
kika på varandras veckor är det en egen story.

**`Dish.When` ska bort, inte bli kvar vid sidan av.** Två källor till samma sanning blir fel
inom en månad.

## Migrering

De rätter som idag har ett `When` är någons planering — men ingen vet vems, eftersom fältet
aldrig burit en användare. Rimligaste tolkningen: **planeringen tillfaller rättens ägare**.
Det stämmer med hur data faktiskt ser ut, eftersom ägarmigreringen i story 008 gav de
befintliga rätterna till Peter.

- [ ] Varje rätt med ett `When` som är dagens datum eller senare blir en planeringspost för
      rättens ägare på den dagen.
- [ ] `When`-värden i det förflutna migreras inte — de är historik, inte planering.
- [ ] `DateTimeOffset.MinValue` betyder "inte inplanerad" och migreras inte.
- [ ] Ingen rätt tas bort, och ingen rätt byter ägare.

## Säkerhetskriterier
- [ ] Planeringen knyts till den inloggade användaren på servern. Användar-id får aldrig
      komma från klienten.
- [ ] Jag kan inte lägga något i, eller ta bort något ur, någon annans vecka.
- [ ] Att planera in en rätt kräver **inte** att man äger den — men att *ändra* rätten kräver
      det fortfarande, precis som idag. De två får inte glida ihop.
- [ ] Att ta bort en rätt ur min vecka raderar inte rätten.
- [ ] Raderas en rätt som ligger i någons vecka ska den försvinna ur veckan utan att lämna
      en trasig post efter sig.

## UX-acceptanskriterier
- [ ] Veckovyn visar min planering. Loggar Boss in ser hen sin egen, inte min.
- [ ] Jag kan planera in en rätt som någon annan äger, utan felmeddelande.
- [ ] Planerar jag in en rätt på en dag som redan har en rätt hos mig, ersätts min dag — och
      jag ser att det är det som händer, innan det sker.
- [ ] Att planera påverkar aldrig någon annans vecka.
- [ ] Jag kan ta bort en rätt från en dag utan att rätten försvinner ur samlingen.
- [ ] I `/previousDishes` betyder "Inplanerad <datum>" *min* planering. En rätt som bara
      Boss planerat in visas som inte inplanerad för mig.
- [ ] Tomt tillstånd: en dag utan planerad rätt ser likadan ut som idag och bjuder in till
      att planera. En helt tom vecka känns inte som ett fel.
- [ ] Laddning: när jag planerar visas att det pågår, och dubbelklick skapar inte två poster.
- [ ] Fel: om planeringen inte kan sparas står dagen kvar som den var och jag får veta det —
      vyn ska aldrig visa något som inte sparats.
- [ ] Sorteringen i `/previousDishes` är fortfarande entydig. **Observera:** dagens sortering
      använder `.ThenByDescending(x => x.When)` som skiljetecken (`DishService.cs:156`). När
      `When` försvinner måste det ersättas, annars bryts story 004 — samma betyg ska ge samma
      ordning varje gång.

## Flöde
1. Peter loggar in och ser sin vecka.
2. Peter öppnar `/previousDishes` och väljer "Ät igen" på Boss kålpudding, måndag.
3. Rätten hamnar på Peters måndag. Boss måndag är orörd.
4. Boss loggar in, ser sin egen vecka, och planerar in samma kålpudding på sin onsdag.
5. Båda har kålpudding planerad, på var sin dag, utan att veta om varandra.
6. Peter tar bort rätten från sin måndag — rätten finns kvar i samlingen och på Boss onsdag.

## Wireframe

```
  Peter loggar in                      Boss loggar in
  ┌────────────────────────┐          ┌────────────────────────┐
  │ Veckans mat            │          │ Veckans mat            │
  │ ┌────────────────────┐ │          │ ┌────────────────────┐ │
  │ │ Måndag             │ │          │ │ Måndag             │ │
  │ │ Kålpudding         │ │          │ │ Ingen rätt         │ │
  │ │ av Boss            │ │          │ │ planerad än        │ │
  │ │ [Ändra rätten]     │ │          │ │ [Lägg till rätt]   │ │
  │ │  ↑ grå — Peter     │ │          │ └────────────────────┘ │
  │ │    äger den inte,  │ │          │ ┌────────────────────┐ │
  │ │    men får planera │ │          │ │ Onsdag             │ │
  │ │    in den          │ │          │ │ Kålpudding         │ │
  │ └────────────────────┘ │          │ │ av Boss            │ │
  └────────────────────────┘          │ └────────────────────┘ │
                                      └────────────────────────┘
      samma rätt, två veckor, ingen påverkar den andra
```

Kortet visar fortfarande vem som äger rätten ("av Boss") — det är information om receptet.
Att den ligger i min vecka är information om mig, och syns på att den är där.

## Klar när
- Två användare kan ha var sin veckoplanering samtidigt, verifierat med två inloggningar.
- Peter kan planera in en rätt Boss äger, utan `ForbiddenException`.
- Att planera hos den ene ändrar ingenting hos den andre.
- `Dish.When` finns inte kvar i entiteten.
- Migrering körd mot en kopia av produktionsdatan: varje framtida `When` har blivit en
  planeringspost för rättens ägare, inget `MinValue` har migrerats, ingen rätt förändrad.
- Sorteringen i `/previousDishes` är entydig utan `When` — samma lista i samma ordning
  två anrop i rad.
- Att radera en rätt som ligger i en vecka lämnar ingen trasig post.
