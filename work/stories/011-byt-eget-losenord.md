---
story: 011
status: draft
issue: 27
---

# Story 011: Byt mitt eget lösenord

Etapp 1 av två. Story 012 bygger på den här.

## Användarvärde
Som inloggad användare vill jag kunna byta mitt lösenord inifrån appen, så att jag kan välja
ett nytt när jag misstänker att det läckt — utan att behöva be en administratör om hjälp.

## Bakgrund
Sedan story 006 äger appen sina egna konton: `AppUser` har `PasswordHash` och en
`SecurityStamp` som redan är dokumenterad som något som ska roteras "vid utloggning och
lösenordsbyte". Rotationen finns, men det finns ingen vy där en användare faktiskt kan byta
lösenord. Vi har medvetet ingen e-postkedja, så byte förutsätter att användaren kan logga in
— den som glömt sitt lösenord hanteras av story 012.

## Omfattning
- En vy där inloggad användare byter sitt eget lösenord.
- Nuvarande lösenord krävs för att bekräfta att det är rätt person vid tangentbordet.
- Samma längdregler som vid registrering (`MinimumPasswordLength` = 12, max 256).

Utanför omfattning: e-post, glömt lösenord, byte av alias eller e-postadress, tvåfaktor.

## Säkerhetskriterier
- [ ] Bytet kräver korrekt nuvarande lösenord. Ett fel här får inte avslöja något annat än
      att det var fel.
- [ ] Servern byter lösenord enbart på den inloggade användaren — användar-id får aldrig
      komma från klienten.
- [ ] `SecurityStamp` roteras vid lyckat byte, så att sessioner i andra webbläsare slutar
      gälla. Den egna, pågående sessionen ska fortsätta fungera utan omlogg.
- [ ] Upprepade misslyckade försök att ange nuvarande lösenord ska inte vara ett sätt att
      gissa sig fram obegränsat — samma spärrlogik som vid inloggning gäller.

## UX-acceptanskriterier
- [ ] Vyn har en enda primär handling: "Byt lösenord".
- [ ] Tre fält: nuvarande lösenord, nytt lösenord, upprepa nytt lösenord.
- [ ] Längdkravet står skrivet i vyn innan användaren skriver fel, inte bara efteråt.
- [ ] Nytt lösenord och upprepningen måste stämma överens; annars pekas felet ut vid rätt
      fält, inte som en allmän banner.
- [ ] Laddning: knappen visar att bytet pågår och går inte att trycka två gånger.
- [ ] Fel: fel nuvarande lösenord, för kort nytt lösenord och tekniskt fel ger tre skilda,
      begripliga meddelanden på svenska. Fälten för nytt lösenord töms vid fel så att
      användaren inte råkar spara ett halvt ifyllt försök.
- [ ] Lyckat byte ger en tydlig bekräftelse och lämnar användaren kvar inloggad.
- [ ] Tomt tillstånd: inte tillämpligt — vyn är ett formulär och nås bara inloggad.
- [ ] Vyn nås från en synlig plats i menyn för inloggad användare.
- [ ] Fälten är riktiga lösenordsfält (dolda tecken, `autocomplete` satt så att lösenordshanterare
      förstår vad som är gammalt och nytt).

## Flöde
1. Användaren öppnar sin kontosida från menyn.
2. Användaren fyller i nuvarande lösenord, nytt lösenord och upprepningen.
3. Användaren trycker "Byt lösenord".
4. Vid fel: felet pekas ut, användaren rättar och försöker igen.
5. Vid rätt: bekräftelse visas, användaren är kvar inloggad och andra sessioner har loggats ut.

## Wireframe

```
  ┌────────────────────────────────────────────┐
  │  Mitt konto                                │
  │  peter@exempel.se · Peter                  │
  │                                            │
  │  Byt lösenord                              │
  │  Nuvarande lösenord                        │
  │  [••••••••••••••••••••••••••••]            │
  │                                            │
  │  Nytt lösenord  (minst 12 tecken)          │
  │  [                            ]            │
  │  Upprepa nytt lösenord                     │
  │  [                            ]            │
  │    ↳ Lösenorden stämmer inte överens       │  <- fel vid fältet
  │                                            │
  │              [  Byt lösenord  ]            │  <- enda primära handlingen
  └────────────────────────────────────────────┘
```

Alias och e-post står som läsbar text överst; formuläret är sidans enda interaktiva del.

## Klar när
- En användare kan byta sitt lösenord och logga in igen med det nya.
- Fel nuvarande lösenord byter ingenting.
- En session i en andra webbläsare slutar gälla efter bytet, medan den egna fortsätter.
