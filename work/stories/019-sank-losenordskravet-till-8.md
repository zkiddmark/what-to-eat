---
story: 019
status: draft
issue: 46
---

# Story 019: Sänk minsta lösenordslängd från 12 till 8 tecken

## Användarvärde
Som användare vill jag kunna välja ett lösenord på åtta tecken, så att jag kommer in i en
matplaneringsapp utan samma krav som på min bank.

## Bakgrund
Beslut av människan: appen planerar veckans middagar åt ett hushåll. Tolv tecken är ett krav
taget från en hotbild appen inte har, och friktionen kostar mer än den skyddar.

Kravet infördes i story 006 som `UserService.MinimumPasswordLength = 12`. Konstanten används
korrekt av all serverlogik — registrering (`UserService.cs:112`), eget lösenordsbyte (`:321`)
och tvingat byte (`:429`) — och av admin-seedningen i `UserSeeder.cs:42`. Att ändra siffran
där är alltså allt som krävs för att *beteendet* ska ändras.

**Men texterna följer inte med.** Siffran 12 är hårdkodad i sex användarsynliga strängar:

| Var | Text |
|---|---|
| `Pages/Register.cshtml:43` | "Minst 12 tecken." |
| `Pages/Account.cshtml:36` | "Minst 12 tecken." |
| `Pages/ForcedPasswordChange.cshtml:24` | "Minst 12 tecken." |
| `Pages/Register.cshtml.cs:40` | "Lösenordet måste vara minst 12 tecken." |
| `Pages/Account.cshtml.cs:47` | "Det nya lösenordet måste vara minst 12 tecken." |
| `Pages/ForcedPasswordChange.cshtml.cs:35` | "Det nya lösenordet måste vara minst 12 tecken." |

Ändras bara konstanten kommer appen att acceptera åtta tecken medan den på sex ställen
påstår att den kräver tolv. Det är värre än dagens läge: en regel som ljuger.

Det här är samma anmärkning som Reviewer gjorde i review 011 och igen i review 012 — att
siffran spred sig i stället för att rättas. Den här storyn är tillfället att göra det, för nu
blir texterna *fel* om ingen gör det.

En not till den som bygger, så tiden inte går åt till att upptäcka det: de tre `ErrorMessage`
ovan sitter i `StringLength`-attribut, och C# tillåter bara konstanta uttryck där — därför
går det inte att stoppa in konstanten med stränginterpolation. Samtidigt finns det redan en
andra, interpolerad validering bredvid varje attribut (`Register.cshtml.cs:75`,
`Account.cshtml.cs:88`, `ForcedPasswordChange.cshtml.cs:84`) som formulerar samma regel
korrekt utifrån konstanten. Välj en väg och låt den vara den enda — dubbel validering med två
olika texter är en del av problemet.

## Omfattning
- `MinimumPasswordLength` sänks från 12 till 8.
- Alla användarsynliga texter om lösenordslängd härleds ur konstanten i stället för att
  upprepa siffran.

Utanför omfattning: komplexitetskrav (stora bokstäver, siffror, tecken), maxlängden (256
står kvar), lösenordsstyrkemätare, tvåfaktor. Och ingenting görs åt befintliga konton — ett
lösenord som redan finns fortsätter att fungera.

## Säkerhetskriterier
- [ ] Gränsen kontrolleras fortfarande på servern, inte bara i formuläret. Ett anrop förbi
      gränssnittet med sju tecken ska avvisas.
- [ ] Samma gräns gäller i alla tre flödena: registrering, eget byte, tvingat byte.
- [ ] Inga befintliga lösenord påverkas, och ingen tvingas byta.
- [ ] Admin-seedningen accepterar nu ett åttateckenslösenord — det är avsikten, men det ska
      verifieras att seedningen fortfarande vägrar starta på något kortare.

## UX-acceptanskriterier
- [ ] Jag kan registrera mig med ett lösenord på åtta tecken.
- [ ] Jag kan byta till ett lösenord på åtta tecken, både frivilligt och vid tvingat byte.
- [ ] Sju tecken avvisas, och felmeddelandet säger åtta — inte tolv.
- [ ] Hjälptexten under fältet säger åtta i alla tre vyerna, innan jag skrivit något.
- [ ] Samma regel uttrycks likadant överallt; inget ställe säger en annan siffra än ett annat.
- [ ] Ändras konstanten igen följer alla sex texterna med utan att någon behöver leta.
- [ ] Ingenting annat i inloggnings- eller kontoflödena ändrar beteende.

## Flöde
1. En ny användare öppnar registreringen och ser "minst 8 tecken".
2. Användaren väljer ett åttateckenslösenord och kontot skapas.
3. En befintlig användare loggar in med sitt gamla, längre lösenord — oförändrat.
4. Samma användare byter till ett kortare lösenord om hen vill.

## Wireframe

```
  FÖRE                                  EFTER
  ┌──────────────────────────────┐     ┌──────────────────────────────┐
  │ Lösenord                     │     │ Lösenord                     │
  │ [••••••••]                   │     │ [••••••••]                   │
  │ Minst 12 tecken.             │     │ Minst 8 tecken.              │
  │   ↳ Lösenordet måste vara    │     │                              │
  │     minst 12 tecken.         │     │        [ Skapa konto ]       │
  │        [ Skapa konto ]       │     │                              │
  └──────────────────────────────┘     └──────────────────────────────┘
        ↑ åtta tecken avvisas               ↑ åtta tecken går igenom

  Siffran står på ett ställe i koden och syns på sex ställen i appen.
```

## Klar när
- Registrering, eget byte och tvingat byte accepterar åtta tecken och avvisar sju.
- `grep -rn "12 tecken" Pages Services` ger noll träffar.
- Ett befintligt konto med långt lösenord loggar in oförändrat.
- Admin-seedningen startar med ett åttateckenslösenord och vägrar med ett på sju.
