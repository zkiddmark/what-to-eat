---
story: 003
status: planned
issue: 10
---

# Story 003: Bilden tappas när en ny rätt skapas

## Användarvärde
Som användare vill jag att bilden jag laddar upp på en ny rätt faktiskt visas på rätten,
så att jag inte tror att jag sparat en bild som i själva verket försvann.

Idag laddas bilden upp och sparas i databasen, men rätten får aldrig någon koppling till
den. Bilden finns, men ingen ser den någonsin. Användaren får ingen felindikation.

## Bevis
I `Services/Dish/DishService.cs`:

```csharp
var newDish = dishDto.MapToNewDish();
if (dishDto.Image is not null)
{
    var imageId = await AddImageFromFileAsync(dishDto.Image, dishDto.Title);
    newDish.ImageId = imageId;          // sätts på newDish ...
}
db.Dishes.Add(dishDto.MapToNewDish());  // ... men här skapas ett NYTT objekt utan ImageId
```

`newDish` används aldrig. Bugg-fingeravtrycket finns i produktionsdatan: **9 av 24 bilder är
föräldralösa** — ingen rätt pekar på dem. Det är uppladdningar användaren tror finns.

Buggen fanns i LiteDB-versionen och bevarades medvetet i story 002 i stället för att tyst
fixas, vilket var rätt — men den ska åtgärdas.

## Omfattning
- Lägg till den bild-id-satta rätten i stället för ett nytt, tomt objekt.
- Utanför omfattning: att rädda de 9 redan föräldralösa bilderna (egen fråga, se nedan).

## UX-acceptanskriterier
- [ ] En ny rätt som skapas med bild visar bilden direkt i listan efter att den sparats.
- [ ] Rätten visar samma bild efter omladdning av sidan.
- [ ] En ny rätt som skapas **utan** bild fungerar som tidigare och får ingen bild.
- [ ] Redigera-flödet påverkas inte — utbytt bild tar fortfarande bort den gamla.
- [ ] Fel: om bilduppladdningen misslyckas sparas inte rätten tyst utan bild; användaren får
      veta att bilden inte kunde sparas.
- [ ] Inga nya föräldralösa bilder skapas när en rätt sparas med bild.

## Flöde
1. Användaren väljer "lägg till rätt", fyller i titel och väljer en bild.
2. Användaren sparar.
3. Rätten dyker upp i listan **med** bilden.
4. Användaren laddar om sidan — bilden är kvar.

## Wireframe
Ingen layoutförändring. Skillnaden är att bildrutan på den nyskapade rätten innehåller en
bild i stället för att vara tom.

## Öppen fråga till människan
De 9 föräldralösa bilderna i produktionsdatan går inte att koppla tillbaka till rätt rätt
automatiskt — kopplingen har aldrig funnits. Vill du att de får ligga kvar (kostar utrymme,
syns inte), eller ska de städas bort i en egen story?
