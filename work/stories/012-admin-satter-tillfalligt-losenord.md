---
story: 012
status: planned
issue: 28
---

# Story 012: Admin sätter tillfälligt lösenord — användaren tvingas byta vid nästa inloggning

Etapp 2 av två. Bygger på story 011.

## Användarvärde
Som administratör vill jag kunna sätta ett tillfälligt lösenord åt en användare som låst ute
sig, så att hen kan komma in igen — och som användare vill jag tvingas välja ett eget
lösenord direkt vid nästa inloggning, så att det tillfälliga inte blir kvar som ett riktigt.

## Bakgrund
Vi har medvetet ingen e-postkedja: det finns ingen återställningslänk att skicka. Den som
glömt sitt lösenord kan alltså inte hjälpa sig själv. Admin finns redan (story 007) med en
vy på `/admin/anvandare` där konton godkänns och avslås — det är där den här handlingen hör
hemma. Överlämningen av det tillfälliga lösenordet sker utanför appen: admin läser upp eller
skriver det i valfri kanal. Därför måste det tillfälliga lösenordet vara kortlivat och
oanvändbart till annat än att sätta ett nytt.

Det kräver ett nytt tillstånd på kontot — att lösenordet är tillfälligt och måste bytas.
Hur det lagras är arkitektens beslut; storyn kräver bara att tillståndet överlever omstart
och utloggning.

## Omfattning
- Admin sätter ett tillfälligt lösenord på ett godkänt konto och ser det en gång på skärmen.
- Kontot markeras som "måste byta lösenord".
- Vid nästa inloggning hamnar användaren i ett tvingat byte och kommer ingen annanstans
  förrän det är gjort.

Utanför omfattning: e-post, självbetjänad återställning, att admin får se befintliga
lösenord (omöjligt — de är hashade), engångslänkar.

## Säkerhetskriterier
- [ ] Endast admin kan sätta tillfälligt lösenord. Kontrollen sker på servern, inte genom
      att knappen döljs.
- [ ] Admin kan inte sätta tillfälligt lösenord på sitt eget konto — den vägen går via
      story 011.
- [ ] Det tillfälliga lösenordet genereras av servern med kryptografiskt säker slump. Admin
      får inte hitta på det själv.
- [ ] Det tillfälliga lösenordet visas exakt en gång och lagras aldrig i klartext, inte
      heller i loggar.
- [ ] Att sätta tillfälligt lösenord roterar `SecurityStamp` — användarens eventuella
      pågående sessioner slutar gälla omedelbart.
- [ ] En användare i tillståndet "måste byta lösenord" når ingen annan sida i appen, och
      inget API-anrop som ändrar data, förrän bytet är gjort. Kontrollen ligger på servern.
- [ ] Det tvingade bytet kräver det tillfälliga lösenordet och rensar tillståndet först när
      det nya lösenordet sparats.
- [ ] Ett tillfälligt lösenord som inte använts inom 24 timmar slutar gälla; kontot förblir
      spärrat tills admin sätter ett nytt.

## UX-acceptanskriterier
- [ ] I användarlistan har varje godkänd användare utom en själv handlingen "Sätt tillfälligt
      lösenord". Den är sekundär — godkänn/avslå är fortfarande vyns huvudsak.
- [ ] Handlingen kräver bekräftelse i dialog som säger vad som händer: användaren loggas ut
      överallt och måste byta vid nästa inloggning.
- [ ] Efter bekräftelse visas det tillfälliga lösenordet stort och läsbart, med en tydlig
      text om att det bara visas nu och måste lämnas över utanför appen. Kopiera-knapp.
- [ ] Listan visar vilka konton som väntar på ett tvingat byte, så admin ser vem som ännu
      inte kommit in.
- [ ] Användaren loggar in med det tillfälliga lösenordet och landar direkt i vyn "Välj ett
      nytt lösenord" — utan att först se listan med rätter.
- [ ] Den vyn förklarar varför den visas ("En administratör har satt ett tillfälligt lösenord
      åt dig"), har inga navigationsvägar bort och en enda primär handling.
- [ ] Laddning: både admins knapp och användarens bytesknapp visar pågående arbete och kan
      inte tryckas två gånger.
- [ ] Fel: utgånget tillfälligt lösenord säger att det gått ut och att admin behöver sätta
      ett nytt — inte bara "fel lösenord". För kort nytt lösenord och tekniskt fel har egna
      meddelanden.
- [ ] Tomt tillstånd: finns inga konton som väntar på byte visas ingen sektion om det alls.
- [ ] När bytet är klart får användaren en bekräftelse och fortsätter direkt in i appen,
      inloggad, utan ny inloggning.

## Flöde

**Admin**
1. Admin öppnar `/admin/anvandare`.
2. Admin väljer "Sätt tillfälligt lösenord" på en användare.
3. Dialogen förklarar konsekvensen; admin bekräftar.
4. Det tillfälliga lösenordet visas en gång; admin kopierar och lämnar över det utanför appen.

**Användaren**
5. Användaren loggar in med sin e-post och det tillfälliga lösenordet.
6. Appen visar "Välj ett nytt lösenord" — ingen annan väg finns.
7. Användaren anger nytt lösenord två gånger och sparar.
8. Tillståndet rensas, användaren är inloggad och fortsätter till listan med rätter.

## Wireframe

**Admin — användarlistan**
```
  ┌──────────────────────────────────────────────────────┐
  │ Användare (4)                                        │
  │ ────────────────────────────────────────────────     │
  │ Anna   anna@exempel.se    user   [Sätt tillf. lösen] │
  │ Björn  bjorn@exempel.se   user   väntar på byte      │  <- status, ingen knapp-spam
  │ Peter  peter@exempel.se   admin  (du)                │
  └──────────────────────────────────────────────────────┘
```

**Admin — efter bekräftelse**
```
  ┌──────────────────────────────────────────────────────┐
  │  Tillfälligt lösenord för Anna                       │
  │                                                      │
  │        K7m-våg-9tRx-blå-22                    [Kopiera] │
  │                                                      │
  │  Visas bara nu. Lämna över det till Anna direkt.     │
  │  Anna måste välja ett eget lösenord vid inloggning.  │
  │  Gäller i 24 timmar.                                 │
  │                                     [  Klart  ]      │
  └──────────────────────────────────────────────────────┘
```

**Användaren — tvingat byte**
```
  ┌────────────────────────────────────────────┐
  │  Välj ett nytt lösenord                    │
  │  En administratör har satt ett tillfälligt │
  │  lösenord åt dig. Välj ett eget för att    │
  │  fortsätta.                                │
  │                                            │
  │  Nytt lösenord  (minst 12 tecken)          │
  │  [                            ]            │
  │  Upprepa nytt lösenord                     │
  │  [                            ]            │
  │                                            │
  │           [  Spara och fortsätt  ]         │
  └────────────────────────────────────────────┘
```
Ingen meny, ingen "hoppa över", ingen väg bakåt utom att logga ut.

## Klar när
- Admin kan sätta tillfälligt lösenord på en annan användare och ser det en gång.
- Användaren kommer in med det, tvingas byta och kan inte nå någon annan sida dessförinnan.
- Efter bytet fungerar bara det nya lösenordet; det tillfälliga är dött.
- Ett tillfälligt lösenord äldre än 24 timmar avvisas med eget felmeddelande.
- Admin kan inte sätta tillfälligt lösenord på sig själv.
