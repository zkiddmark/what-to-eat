---
story: 010
status: in-progress
issue: 25
---

# Story 010: Admin-kontot måste seedas innan ägarmigreringen — appen startar inte i produktion

## Användarvärde
Som ägare av produktionsinstansen vill jag att appen startar och migrerar färdigt även när
första starten skedde utan `ADMIN_INITIAL_PASSWORD`, så att jag inte låses ute från min egen
app av en startordning jag inte kan påverka utifrån.

## Bakgrund
Program.cs seedar admin-kontot endast när `AddAppUser` ligger i *pending*-listan. Efter en
första start utan `ADMIN_INITIAL_PASSWORD` är den migreringen redan applicerad men
användartabellen tom — då hoppas seedningen över, `AddDishOwner` sätter alla `OwnerId` till
`00000000-...`, och främmande nyckeln mot `Users` fäller starten:

```
SQLite Error 19: 'FOREIGN KEY constraint failed'
at Program.<Main>$(String[] args) in /src/Program.cs:line 68
```

Appen kraschloopar. Att sätta variabeln i efterhand hjälper inte — seedningen på rad 71 nås
aldrig. Databasen är intakt (allt utom `PRAGMA foreign_keys` rullades tillbaka i transaktion).

## Acceptanskriterier
- [ ] Admin seedas när användartabellen **finns och är tom** — inte bara när migreringen är pending.
- [ ] En instans som redan står på `AddAppUser` med tom användartabell startar färdigt när
      `ADMIN_INITIAL_PASSWORD` sätts och containern startas om; befintliga rätter får admin som ägare.
- [ ] En instans som redan kört hela vägen med data påverkas inte (seedningen är fortsatt no-op
      när användare finns).
- [ ] Saknas `ADMIN_INITIAL_PASSWORD` fortfarande: appen ska **inte** kraschloopa i en migrering.
      Antingen stoppa tidigt med ett begripligt felmeddelande som namnger variabeln, eller låta
      ägarmigreringen klara sig utan admin. Arkitekten väljer — men tyst krasch i EF är inte ett
      alternativ.
- [ ] Fel: loggraden vid utebliven seedning ska säga vad människan ska göra, inte bara vad som hände.

## Flöde (drift)
1. Människan sätter `ADMIN_INITIAL_PASSWORD` i `docker-compose-wte.yml` (12–256 tecken).
2. `docker compose up -d`.
3. Appen migrerar fram till användartabellen, seedar admin, kör resten av migreringarna.
4. Rätterna pekar på admin. Inloggning fungerar.

## Wireframe
Ingen UI-yta. Ytan är containerloggen: en rad, i klartext, som säger vilken variabel som saknas
och att omstart krävs.

## Noterat men utanför scope
- `ASPNETCORE_URLS` pekar på 5258 medan compose mappar 8080→80 och dockerfilen exponerar 80/443.
- `UseHttpsRedirection()` är på utan konfigurerad HTTPS-port i containern.
