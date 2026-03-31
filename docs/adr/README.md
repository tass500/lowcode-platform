# Architecture Decision Records (ADR)

## Cél

Az **ADR** egy **egy döntés = egy fájl** konvenció (iparági gyakorlat, pl. Michael Nygard-féle forma). Segít:

- visszakövetni, **miért** választottunk egy technológiát vagy mintát;
- elkerülni a „régen már eldöltük chatben” információvesztést;
- új tagoknak **időrendben** értelmezni a platformot.

## Elnevezés

`NNN-rövid-cim-kebab-case.md`

- `NNN`: háromjegyű sorszám (001, 002, …).
- Példa: `001-use-main-as-default-branch.md`

## Sablon

Használd: [`template.md`](template.md)

## Viszony a többi doksihoz

- **`docs/00_truth_files_template/01_dontesek.md`**: részletes policy index és hosszú döntésnapló-formátum — **referencia**.
- **`docs/live/*`**: operatív állapot és feature — **nem** helyettesíti az ADR-t stratégiai döntéseknél.
- Új **szervezeti szintű** döntés: ADR fájl + szükség szerint egy sor a policy táblázatban (`01_dontesek`) vagy hivatkozás a `PROJECT_CONTEXT.md`-ből.

## Mikor írj ADR-t?

- Új auth modell, új adatbázis-provider, új integrációs minta.
- Breaking API vagy szándékos szerződés-változás.
- Olyan kompromisszum, amit 6 hónap múlva is meg kell tudni magyarázni.

## Mikor elég a PR leírás?

- Kisebb, reverzibilis változás, egyértelmű scope — a `DEVELOPMENT_WORKFLOW.md` szerinti PR összegzés + tesztterv.
