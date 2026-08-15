# Hekser & Diamanter – simulator

En tekstbasert Monte Carlo-simulator for brettspillet **Hekser & Diamanter**. Simulatoren bruker virtuelle, ubegrensede komponentlagre og måler hvor mange komponenter spillerne har ute samtidig. Det gjør det mulig å estimere en billigere, men robust fysisk komponentliste.

## Kjøring

Start fra prosjektmappen for å få en nummerert meny med de medfølgende profilene:

```bash
dotnet run --project HekserOgDiamanter
```

Vis profilene uten å starte en simulering:

```bash
dotnet run --project HekserOgDiamanter -- --list-configs
```

Kjør en bestemt profil direkte, for eksempel i et skript eller en automatisert jobb:

```bash
dotnet run --project HekserOgDiamanter -- --config HekserOgDiamanter/Configs/stress-gold.json
```

Ved omdirigert input må `--config` brukes, siden menyen krever et interaktivt valg. Hele [`Configs`](HekserOgDiamanter/Configs)-mappen kopieres til build- og publish-output.

De medfølgende filene er:

- `stress-clear-diamond.json`, `stress-gold.json`, `stress-pickaxe.json`, `stress-shovel.json` og `stress-money.json`: målrettede stresstester med 2, 3 og 4 spillere
- `random.json`: tilfeldig strategi og stokking med 2, 3 og 4 spillere
- `example.json`: en pedagogisk blanding av tilfeldige spill og diamant-stresstest

Hver profil skriver til sin egen mappe under `simulation-results/`. Relative resultatmapper tolkes fra arbeidsmappen der programmet startes, ikke fra `bin/` eller konfigurasjonsfilens mappe.

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

For hver komponent vises 95-, 99- og 99,9-percentil samt observert maksimum. 99,9-percentilen for alle spillerne samlet brukes som anbefalt antall og sammenlignes med dagens komponentliste. Avbrutte spill rapporteres separat og inngår ikke i anbefalingen.

`summary.csv` har nøyaktig én rad per scenario og ressurstype. Kolonnene `AllPlayers...` beskriver fysisk totalbehov, mens `SinglePlayerP99.9` og `SinglePlayerMaximum` viser største samtidige beholdning hos én spiller. En profil med tre scenarioer gir derfor 27 datarader. Navngitte spillerstatistikker lagres ikke.

Sett `writeDetailedCsv` til `true` for også å få `games.csv` med én rad per spill. Den inneholder total- og enkeltspillertopper, utdelinger, poeng og vinnere, men ingen navngitte spillertopper.

Mynter veksles fritt og representeres alltid med flest mulig 10-kroner, deretter 5- og 1-kroner. Fargede diamanter og lanternen er faste enkeltkomponenter.

## Tester

```bash
dotnet test hekserogdiamanter.slnx -m:1
```
