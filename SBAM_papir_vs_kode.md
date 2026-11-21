# SBAM: papirets beskrivelse sammenlignet med koden

Sammenligning af `SBAM.pdf` (Stephensen & Markeprand, 31. oktober 2013) med koden i `C:\psp\MS_SMILE_2026`, udført 20. november 2026.

## Grundlag og forbehold

- Gennemlæst: `MicroSim\Demographics\Matching.cs`, `Numerical\RASMatrix.cs`, `Matching2019.cs`, `Matching2026.cs`, `MatchingFactory.cs` samt de steder i `Demographics.cs`, `Household.cs`, `Person.cs`, `Simulation.cs`, `Statistics.cs` og `Program.cs`, der kalder eller opsætter matchingen.
- Intet er kørt, da modellen mangler data. Størrelsen af de beskrevne effekter er derfor ikke målt.
- Indholdet af datafilen `SBAM_initfile_nyePar_*.txt` er ikke set. Kodningen af `landsdel_primo`, `oprindelse` og aldersfordelingen i filen er derfor ikke kontrolleret.
- `C:\psp\MS_SMILE_2026` er ikke et git-repository, så der er ingen historik for, hvornår eller hvorfor de enkelte linjer er ændret. Vurderingerne bygger på den nuværende kode og kommentarerne i den.
- Ingen kode er ændret.

## Konklusion

Kernen i koden svarer til papiret: symmetrisk datamatrix, sparse matrix med linkede lister, RAS-tilpasning og pardannelse celle for celle. Der er to sandsynlige fejl og fire steder, hvor koden bevidst gør noget andet end papiret beskriver.

## Det der stemmer

| Papir | Kode |
|---|---|
| 2.1: symmetrisk datamatrix, x0(i,j) og x0(j,i) tælles op for hvert par | To `RASMatrixCell` pr. par, `(j0,j1)` og `(j1,j0)`; ens typer giver værdien 2 i diagonalen |
| 2.2: RAS med skiftevis række- og søjleskalering mod N | `RASAdjust(target, target)`, rækker først, så søjler |
| 2.3: sparse matrix med 2T linkede lister ("SBAMMatrix") | `RASMatrix` med `_rows`/`_cols` og `NextCol`/`NextRow` |
| 4.3: par dannes celle for celle for i ≤ j fra tilfældige køer | Matricen foldes til en trekant, typelisterne shuffles, og par tages forfra |
| 5: puljen består af enlige, nyligt opløste par og udeflyttede børn | `InMatchingPool` sættes ved ShackUp, Divorce2Matching og LeaveHome2Matching |

## Sandsynlige fejl

### 1. RAS-iterationen stopper altid efter højst to gennemløb

`Numerical\RASMatrix.cs`, linje 521-528:

```csharp
        err = (r_ssq + c_ssq) / n;
        //if (ConsoleMessage) Console.WriteLine("--> {0}", err);
        double err0 = err;
        if (err < errOK) break;
        if (itt > ittMax) break;
        if (itt > 0 && err - err0 < 0.00001) break;
        itt++;
      }
```

`err0` sættes lig `err` lige før sammenligningen, så `err - err0` altid er 0. Løkken brydes derfor, så snart `itt > 0`, altså i andet gennemløb. Papirets "repeat these two operations until a new stable matrix has emerged" er ikke opfyldt.

Da `ConsoleMessage` er `false`, udskrives iterationstallet aldrig, så det opdages ikke under en kørsel.

Efter to gennemløb passer søjlesummerne præcist, mens rækkesummerne kun passer tilnærmelsesvist. Foldningen til en trekant lægger (i,j) og (j,i) sammen, så afvigelsen pr. type bliver halveret.

### 2. Aldersindekset i data er forskudt i forhold til simulationen

`MicroSim\Demographics\Matching.cs`, linje 117-121:

```csharp
                            if (a[i] < AgeMin) a[i] = AgeMin;
                            if (a[i] > AgeMax) a[i] = AgeMax;
                            // Aggregeres og gøres 0-baserede
                            //TMA (2/10/2019): alder ultimo-ficeres
                            a[i] = a[i] - AgeMin + 1;
```

Med `MatchingMinAge = 15` og `MatchingMaxAge = 65` er der 51 alderstrin med gyldigt indeks 0..50. Data-alderen afgrænses til [15, 65] og får derefter lagt 1 til, så indekset bliver 1..51. Simulationen bruger `p.Age - MatchingMinAge`, altså 0..50.

- **Overløb:** Indeks 51 løber over i næste køn. En mand på 65+ tælles som en 15-årig kvinde med samme uddannelse, og en kvinde på 65+ som en 15-årig mand ét uddannelsestrin højere.
- **15-årige:** Indeks 0 får kun data fra overløbet. Simulationens 15-årige matches derfor enten efter de ældres mønster eller slet ikke.
- **Mulig crash:** Den allersidste type (kvinde, 65+, lang uddannelse, sidste region, sidste herkomstgruppe) ville give et indeks uden for matricen.
- **Betingelse:** Det rammer kun, hvis filen indeholder personer med `alder` ≥ 65. Afgrænsningen til `AgeMax` tyder på, at det er tilfældet, men det er ikke kontrolleret.

Selve forskydningen med +1 ser ud til at være tilsigtet: `alder` i filen er primo-alder, og simulationen matcher efter `UpdateAge()`, altså på ultimo-alder. Fejlen er, at afgrænsningen ikke blev flyttet med.

"Grass sow"-blokken (linje 157-189) bruger indeks uden +1 og er dermed inkonsistent med indlæsningen, men den er slået fra (`SBAMMunicipalityGrassSow = false`).

## Bevidste afvigelser fra papiret

### 3. Ikke alle i puljen matches

Papiret danner N/2 par af N personer. Koden puster puljen op med `MatchingScaleUp` og stopper ved `targetAntalPar`.

`MicroSim\Demographics\Matching.cs`, linje 380-390:

```csharp
            int antalPar = nHusk / 2;
            //int targetAntalPar = (int)(antalPar / MSSettings.MatchingScaleUp);
            double targetAntalPar_tmp = 0;
            for (int k = 0; k < MSSettings.MUNICIPALITY; k++)
            {
                double antalParMun = nHuskMun[k] / 2;

                targetAntalPar_tmp += antalParMun / MSSettings.MatchingScaleUp[k];
            }

            int targetAntalPar = (int)targetAntalPar_tmp;
```

- **Faktorer:** 1,3 generelt; 1,05 for København og Frederiksberg; 1,15 for Odense og Aarhus; 1,2 for Aalborg (`Matching2019.cs`). Sandsynlighederne for at komme i puljen ganges med samme faktor i `Demographics.cs`.
- **Konsekvens:** RAS-målet er hele den oppustede pulje, så papirets ligning (1) kun holder op til en faktor på ca. 1/1,3 for de realiserede par. Ca. 23 pct. af puljen forbliver enlige.
- **Detalje:** `nHuskMun[k] / 2` er heltalsdivision, så der tabes et halvt par pr. kommune med ulige antal.

### 4. Pardannelsen er stokastisk og sker i flere gennemløb

Papiret tager x(i,j) par pr. celle i ét gennemløb og siger intet om afrunding af ikke-heltallige værdier. Koden (PSP1-ændringen) tager højst ét par pr. celle pr. gennemløb plus et lodtræk på resten, i op til 1000 gennemløb.

`MicroSim\Demographics\Matching.cs`, linje 484-505:

```csharp
                    int n = 1;  // Antal par der skal laves
                    if (c.Value >= 2.0)
                        c.Value -= 2.0;
                    else
                        n = 0;

                    //PSP1
                    if (c.Value < 0)
                        throw new Exception("ups!");

                    // Det gælder at c.Value < 2.0 på grund af ovenstående.
                    // Med ssh'en (c.Value/2.0) tilføjer vi 1 par
                    // Det giver den korrekte forventede værdi
                    //PSP1
                    if (c.Value < 2.0)
                    {
                        if (Simulation.Random.NextEvent(c.Value / 2.0))
                        {
                            n++;  //n = 1
                            c.Value = 0;
                        }
                    }
```

- **Kommentaren holder ikke længere:** En rest, der ikke udløses, beholdes og får et nyt lodtræk i næste gennemløb. Kommentaren "korrekte forventede værdi" gælder kun med ét gennemløb, som i den udkommenterede oprindelige version.
- **Retning af skævheden:** Store celler beskæres, når løkken stopper ved målet, mens små celler overrepræsenteres. Sjældne kombinationer får dermed for stor vægt i forhold til den RAS-tilpassede matrix. Hvis næsten alle celler er under ét par, nås målet i første gennemløb, og udtyndingen er tilfældig og dermed uskæv.
- **Tabte par:** Løber en typeliste tør, er cellens værdi allerede trukket ned, og parret tabes uden omfordeling (tælles som `udeladt`).
- **Overflødig kode:** `rasAdj` sorteres efter værdi og shuffles umiddelbart efter. `n_target` beregnes, men bruges ikke.

### 5. Langt flere typer end i papiret

- **Papiret:** køn × alder (15-65) × 5 uddannelser × 11 landsdele = 5.500 typer. Papiret regner med 50 alderstrin; 15-65 er 51.
- **Koden, landsdelsmodel:** herkomst (5) er tilføjet, hvilket giver 51 × 2 × 5 × 11 × 5 = 28.050 typer.
- **Koden, kommunemodel:** 98 kommuner i stedet for 11 landsdele, hvilket giver 249.900 typer.
- **Typer uden data:** Typer i puljen uden observationer i data får målet sat til 0 i `RASAdjust` og kan ikke matches. Papiret forudsætter, at der ikke er nulrækker. Advarslen lægges i `_warnings`, som aldrig skrives ud; forskellen kan kun ses indirekte i testfilen ("Antal personer i rasAdj" mod puljens størrelse).
- **Samkønnede par:** Papirets afsnit 3 bruger samkønnede par som ventil. I koden findes de kun i celler, der er observeret i data, da RAS bevarer nuller.

### 6. Dataperioden er 2013-2017, også i 2026-versionen

`MicroSim\Demographics\MatchingFactory.cs`, linje 12-15:

```csharp
        public Matching ChooseModel(DwellingCharacteristics dwelChar)
        {
            //NBNBNBNNNBN: foreløbig
            return new Matching2019(dwelChar);
```

- `MatchingFactory` returnerer altid `Matching2019`, så `Matching2026` bruges aldrig. Resten af metoden er uopnåelig kode.
- `Matching2019` og `Matching2026` er identiske, og begge vælger 2013-2017.
- Med Baseline2024 læses filen `SBAM_initfile_nyePar_2007_2023.txt`, men kun 2013-2017 bruges.
- Papirets figurer bygger på data fra 2008; basisklassen `Matching` vælger 2008-2012.

## Mindre ting

- **Bopæl:** Data typeinddeles efter `landsdel_primo`/`kommune_primo`, mens simulationen bruger bopælen på matchingtidspunktet, altså efter en eventuel udeflytning eller parsplit samme år.
- **Symmetri:** Papirets 50 pct. besparelse fra symmetri udnyttes ikke. RAS kører på hele matricen og foldes først bagefter.
- **Sidste celle:** Konstruktøren i `RASMatrix` løber kun til `n - 1`, så den sidste celle kan falde ud af række- eller søjlelisten, hvis den står alene i sin række eller søjle.
- **Landsdelskodning:** I landsdelsmodellen bruges `landsdel_primo` råt som indeks (kaldet til `Mappings.Region11` er udkommenteret), så filen skal være kodet 0-10.
- **Aldersgrænse i puljen:** Husholdninger, hvis første voksne er uden for 15-65 år, tages ud af matchingen, selvom de er markeret til puljen.
