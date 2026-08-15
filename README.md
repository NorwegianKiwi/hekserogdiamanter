# Hekser & Diamanter – simulator

En tekstbasert Monte Carlo-simulator for brettspillet **Hekser & Diamanter**. Simulatoren bruker virtuelle, ubegrensede komponentlagre og måler hvor mange komponenter spillerne har ute samtidig. Det gjør det mulig å estimere en billigere, men robust fysisk komponentliste.

## Kjøring

Kjør standardsenarioene fra prosjektmappen:

```bash
dotnet run --project HekserOgDiamanter
```

Bruk en annen konfigurasjon:

```bash
dotnet run --project HekserOgDiamanter -- --config /full/sti/til/config.json
```

Standardfilen er [`HekserOgDiamanter/simulation-config.json`](HekserOgDiamanter/simulation-config.json). Rapporten skrives til `simulation-results/` ved siden av konfigurasjonsfilen. `summary.csv` inneholder aggregert statistikk; sett `writeDetailedCsv` til `true` for også å få én rad per spill i `games.csv`.

## Konfigurasjon

En fil kan inneholde flere scenarioer. Hvert scenario velger:

- 2–4 spillere med individuelt startoppsett og strategi
- antall spill og maksimalt antall turer
- `Shuffled`, `ColoredDiamondLast` eller `Explicit` kortrekkefølge
- eventuell tur-for-tur-logging

Startoppsettene er `Standard` (2 kr og én klar diamant), `GoldVariant` (8 kr og ett gull) og `Custom`. En `startingResources`-blokk kan overstyre enkeltverdier i et hvilket som helst oppsett.

Strategiene er `Random` og `ResourceHoarding`. Sistnevnte krever ett `target`: `ClearDiamond`, `Gold`, `Pickaxe`, `Shovel` eller `Money`.

Ved `Explicit` må `explicitDeckOrder` inneholde alle seks skattebunkene og heksebunken. Programmet avviser ordren dersom korttypene eller antallene avviker fra standardfordelingen.

## Resultater

For hver komponent vises 95-, 99- og 99,9-percentil samt observert maksimum. 99,9-percentilen brukes som anbefalt antall og sammenlignes med dagens komponentliste. Avbrutte spill rapporteres separat og inngår ikke i anbefalingen.

Mynter veksles fritt og representeres alltid med flest mulig 10-kroner, deretter 5- og 1-kroner. Fargede diamanter og lanternen er faste enkeltkomponenter.

## Tester

```bash
dotnet test hekserogdiamanter.slnx -m:1
```
