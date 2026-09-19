---
story: 007
status: planned
issue: 17
---

# Story 007: Roller och godkännande av nya konton

Etapp 2 av fyra. Bygger på story 006.

## Användarvärde
Som admin vill jag kunna godkänna eller avslå dem som registrerat sig, så att bara personer
jag släppt in kan använda appen.

Som admin vill jag ha en roll som skiljer mig från vanliga användare, så att framtida
behörigheter har något att hänga på.

## Omfattning
- Två roller: `admin` och `user`. `peter@stjern.se` är admin från start, alla andra blir
  `user`.
- Adminvy som listar konton som väntar på godkännande, med möjlighet att godkänna eller avslå.
- Adminvy som listar befintliga användare och deras roll.
- Rollen bärs i användarens session och kontrolleras på servern.

Utanför omfattning: fler roller än två, att ändra roll på befintliga användare via
gränssnittet, ägarskap av recept (008), röstning (009).

## Säkerhetskriterier
- [ ] Rollkontrollen sker på servern. Att dölja en länk i vyn räcker inte — adminvyn går
      inte att nå genom att skriva in adressen som vanlig användare.
- [ ] En användare kan inte ändra sin egen roll, varken via gränssnittet eller genom att
      manipulera det som skickas.
- [ ] Ett avslaget konto kan inte logga in.
- [ ] Admin kan inte råka ta bort eller avslå sitt eget konto och låsa ute sig själv.

## UX-acceptanskriterier
- [ ] Adminvyn nås från en tydlig plats i navigationen — men bara för admin.
- [ ] Väntande konton visas med alias, e-post och när de registrerade sig.
- [ ] Godkänn och avslå är två skilda handlingar; avslå kräver en bekräftelse eftersom den
      inte går att ångra.
- [ ] Tomt tillstånd: när inga konton väntar står det att inget väntar — inte en tom yta.
- [ ] Laddning: listan visar ett laddningstillstånd, och knapparna låses medan handlingen
      pågår.
- [ ] Fel: om en handling misslyckas står kontot kvar i listan med ett felmeddelande. Inget
      försvinner tyst.
- [ ] Efter godkännande kan användaren logga in utan att något mer behöver göras.
- [ ] En vanlig användare ser ingen antydan om att adminvyn finns.
- [ ] Copy på svenska.

## Flöde
1. Någon registrerar sig (story 006) och hamnar i väntläge.
2. Admin loggar in och ser att det finns konton att ta ställning till.
3. Admin öppnar adminvyn och ser listan över väntande.
4. Admin godkänner ett konto. Raden flyttas från väntande till användarlistan.
5. Den godkända användaren loggar in och kommer åt appen.

## Wireframe

```
  Användare                                    [admin]

  Väntar på godkännande (2)
  ┌──────────────────────────────────────────────┐
  │ Kim Larsson    kim@exempel.se     19 sep      │
  │                        [Godkänn]  [Avslå]     │
  ├──────────────────────────────────────────────┤
  │ Sam Eriksson   sam@exempel.se     18 sep      │
  │                        [Godkänn]  [Avslå]     │
  └──────────────────────────────────────────────┘

  Användare (3)
  ┌──────────────────────────────────────────────┐
  │ Peter    peter@stjern.se          admin       │
  │ Alex     alex@exempel.se          user        │
  └──────────────────────────────────────────────┘
```

Primär handling per rad är "Godkänn"; "Avslå" är visuellt underordnad och öppnar en
bekräftelsedialog. När inget väntar ersätts den övre listan av en rad text: "Inga konton
väntar på godkännande."

## Klar när
- Ett nyregistrerat konto kan godkännas och logga in.
- Ett avslaget konto kan inte logga in.
- Adminvyn ger nekad åtkomst för en vanlig användare som skriver in adressen direkt.
