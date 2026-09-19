---
story: 006
status: done
issue: 16
---

# Story 006: Egen inloggning och registrering — Firebase bort

Etapp 1 av fyra. Efterföljs av 007 (roller), 008 (ägarskap) och 009 (röstning).

## Användarvärde
Som användare vill jag kunna skapa ett konto och logga in med e-post och lösenord direkt i
appen, så att jag kommer åt receptsamlingen utan att appen är beroende av en extern tjänst.

Som ägare av appen vill jag att inloggningen verifieras av min egen server, så att jag vet
vad som skyddar den och kan lita på att den som säger sig vara inloggad faktiskt är det.

## Varför nu — dagens lösning håller inte
`FirebaseService.Authenticate` tar emot ett användarobjekt **från JavaScript** och servern
bygger en `ClaimsPrincipal` av det rakt av, medan `GetAuthenticationStateAsync` alltid
returnerar en oautentiserad användare. Servern verifierar alltså aldrig några uppgifter själv
— den litar på vad klienten påstår sig vara. Det är inte en teoretisk svaghet.

En sidovinst: när Firebase är borta behövs inte `ExcludedSecrets/firebaseConfig.json` för att
starta appen. Idag går appen inte att köra lokalt utan den filen, vilket är skälet till att
bilduppladdningen i story 003 aldrig kunnat klickas igenom. Det blir testbart igen.

## Omfattning
- Användarkonto: alias/namn, e-post (inloggningsidentitet), lösenord.
- Registrering, inloggning, utloggning — allt validerat på servern.
- Nya konton hamnar i **väntar på godkännande** och kan inte logga in förrän de godkänts.
  Godkännandevyn byggs i story 007; fram till dess godkänns konton direkt i databasen.
- `peter@stjern.se` finns som godkänt konto från start, så appen aldrig är utelåst.
  Lösenordet sätts vid första start och får inte ligga i repot.
- Ta bort Firebase helt: `Auth/FirebaseService.cs`, `Auth/FirebaseAuthStateProvider.cs`,
  `wwwroot/js/firebaseAuth.js`, paket, konfiguration och läsningen av `ExcludedSecrets`.

Utanför omfattning: roller och behörigheter (007), ägarskap av recept (008), röstning (009),
e-postutskick, glömt lösenord, tvåfaktor.

## Säkerhetskriterier
Arkitekten väljer tekniken; detta är vad lösningen ska uppfylla.

- [ ] Lösenord lagras med en modern lösenordshash med salt och inställbar arbetsfaktor.
      Aldrig klartext, aldrig en rå SHA/MD5.
- [ ] Inloggningsuppgifter verifieras **på servern**. Ingenting som klienten skickar avgör
      vem användaren är.
- [ ] Sessionen bärs av en cookie som är `HttpOnly`, `Secure` och `SameSite`.
- [ ] Utloggning gör sessionen obrukbar — att återanvända den gamla cookien fungerar inte.
- [ ] Misslyckad inloggning ger samma svar oavsett om e-posten finns eller inte, och tar
      ungefär lika lång tid, så inloggningssidan inte kan användas för att kartlägga vilka
      adresser som är registrerade.
- [ ] Upprepade misslyckade försök mot samma konto bromsas eller spärras tillfälligt.
- [ ] Minsta lösenordslängd 12 tecken, ingen övre gräns som avvisar långa lösenord, inga
      påtvingade teckenklasser.
- [ ] Inga hemligheter i repot. `ExcludedSecrets`-beroendet är borta.
- [ ] Sidor som kräver inloggning går inte att nå utloggad — kontrollen sitter på servern,
      inte bara i vyn.

## UX-acceptanskriterier
- [ ] Utloggad besökare som öppnar appen hamnar på inloggningssidan, inte på en tom vy.
- [ ] Inloggningssidan har en länk till registrering och tvärtom.
- [ ] Registrering kräver alias, e-post och lösenord. Fälten valideras innan skicka.
- [ ] Efter registrering visas en tydlig bekräftelse om att kontot väntar på godkännande —
      användaren lämnas inte i tron att hen kan logga in direkt.
- [ ] Inloggningsförsök med konto som väntar på godkännande säger det rent ut, inte
      "fel lösenord".
- [ ] Fel: fel e-post eller lösenord ger ett gemensamt, begripligt felmeddelande. Formuläret
      behåller det som skrivits utom lösenordet.
- [ ] Laddning: knappen visar pågående tillstånd och går inte att trycka två gånger.
- [ ] Inloggad användare ser sitt alias någonstans i gränssnittet och kan logga ut.
- [ ] Copy skrivs på svenska. Befintlig engelsk text på inloggningssidan byts ut; övriga
      vyer översätts inte i denna story.
- [ ] Tillgänglighet: fälten har riktiga `label`, felmeddelanden kopplas till fältet, hela
      flödet går att genomföra med tangentbord.

## Flöde

Registrering:
1. Besökaren öppnar appen och hamnar på inloggningssidan.
2. Besökaren väljer "Skapa konto".
3. Besökaren fyller i alias, e-post och lösenord och skickar.
4. Bekräftelse: kontot är skapat och väntar på godkännande.

Inloggning:
1. Användaren fyller i e-post och lösenord.
2. Vid korrekta uppgifter och godkänt konto landar användaren på startsidan, inloggad.
3. Vid fel visas felmeddelandet utan att sidan laddas om.
4. Utloggning tar användaren tillbaka till inloggningssidan.

## Wireframe

Inloggning — en centrerad ruta, samma placering som dagens:

```
            Logga in
  ┌────────────────────────────┐
  │ E-post                     │
  │ [__________________]       │
  │ Lösenord                   │
  │ [__________________]       │
  │                            │
  │ [       Logga in       ]   │  <- primär handling
  │                            │
  │ Har du inget konto?        │
  │ Skapa konto                │  <- textlänk, tydligt underordnad
  └────────────────────────────┘
```

Registrering — samma ram, ett fält mer:

```
            Skapa konto
  ┌────────────────────────────┐
  │ Namn                       │
  │ E-post                     │
  │ Lösenord  (minst 12 tecken)│
  │                            │
  │ [      Skapa konto     ]   │
  │ Tillbaka till logga in     │
  └────────────────────────────┘
```

Efter registrering ersätts formuläret av ett lugnt bekräftelsemeddelande i samma ram: kontot
är skapat och väntar på godkännande. Ingen knapp som lockar till ett inloggningsförsök som
ändå kommer nekas.

## Klar när
- Registrering och inloggning fungerar mot en tom databas.
- Utloggning verifierat: den gamla sessionen går inte att återanvända.
- Inga referenser till Firebase kvar i koden, konfigurationen eller `wwwroot`.
- Appen startar lokalt utan `ExcludedSecrets`.
- De 31 befintliga recepten är oförändrade och syns för en inloggad användare.

## Verifierat efter merge (2026-09-19, mergad main `b243de7`)

Kört mot appen, inte bara läst koden — vilket blev möjligt först nu när Firebase är borta:

- Utloggad `GET /` → 302 till `/login`. Kontrollen sitter i pipelinen.
- Inloggning med rätt uppgifter → 302, därefter `GET /` → 200.
- Fel lösenord och okänd e-post ger **exakt samma** meddelande: "Fel e-post eller lösenord."
- Sessionscookien sätts med `secure; samesite=lax; httponly`.
- Sex felaktiga försök sätter `LockedUntil`; därefter nekas även rätt lösenord.
- Registrering skapar konto med status `Pending` och visar "Kontot är skapat och väntar på
  godkännande". Inloggningsförsök med det kontot säger det rent ut.
- Utloggning ligger medvetet bara på POST — en GET som muterar tillstånd går att utlösa från
  en annan sajt. Efter POST-utloggning nekas en **sparad kopia** av den gamla cookien.
- Inga referenser till Firebase kvar. Appen startar utan `ExcludedSecrets`.
- Admin-seedningen läser lösenordet ur `ADMIN_INITIAL_PASSWORD` och skapar hellre inget konto
  alls än använder ett standardlösenord.

Not: `Microsoft.AspNetCore.Identity` används för `IPasswordHasher<AppUser>`, men **inget nytt
paket** har lagts till — typen ligger i det delade ramverket. Det är alltså ramverkets
beprövade PBKDF2-hashning utan att hela Identity dras in.
