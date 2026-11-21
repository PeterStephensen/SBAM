# SBAM

Tests of SBAM, the algorithm that decides who forms a couple with whom in SMILE, DREAM's
microsimulation model of the Danish population.

SBAM (sparse biproportionate adjustment matching) is described in Stephensen and Markeprand (2013),
which is included here as [`SBAM.pdf`](SBAM.pdf). The algorithm adjusts a table of historical
couples to the men and women who are to form couples in a given year, and forms the couples from
the adjusted table.

The repository contains two studies. Both run the matching code in a small stand-alone program on
an artificial marriage market of the Choo and Siow (2006) type, where the correct couple table is
known. SMILE itself can only be run on DREAM's closed server at Statistics Denmark, so no real data
are used here.

| Folder | Question | Paper |
|---|---|---|
| [`CodeTest`](CodeTest) | Does the SBAM code in SMILE do what SBAM is supposed to do, and can it be improved? | [Improving the SBAM implementation in the SMILE model](CodeTest/doc/SBAM_test_en.pdf) |
| [`CompareStocastic`](CompareStocastic) | How does the improved SBAM compare with the stochastic matching used in other microsimulation models? | [SBAM and stochastic matching in dynamic microsimulation](CompareStocastic/doc/SBAM_vs_stochastic_matching.pdf) |

## CodeTest: the SBAM code in SMILE

The model's `Matching.cs` is run unchanged on the artificial market and compared with the correct
answer. Main findings:

- The code forms the right number of couples, but it pairs too many people who do not resemble each
  other. With the model's scale-up of the pool, 61.5 per cent of the couples are from the same
  region and 46.8 per cent have the same education, against 76.7 and 55.4 per cent in the correct
  answer.
- The bias arises in the last step, where the computed couple table is turned into couples. From
  the model's second year the code no longer uses the historical table.
- Five changes are tried in a copy, `Matching_test.cs`. With all five the method hits the correct
  answer, and a call of the matching takes 16-36 ms in the cases tested, against 26-965 ms for the
  existing code.

What is in the folder:

| Path | Contents |
|---|---|
| `Matching.cs` | Copy of the model's file. Parts that need the rest of the model are switched off with `#define SBAM_TEST` |
| `SmileFiles/` | Copies of the other model files that `Matching.cs` uses |
| `Matching_test.cs`, `RASMatrix_test.cs` | The modified method. The changes are marked `//TEST0` to `//TEST4` and can be switched on and off separately |
| `Test/` | The test's own code: the artificial market, the test program and the comparison of the variants |
| `data/`, `output/` | Generated test data and the printouts behind the paper |
| `doc/` | The paper in English (`SBAM_test_en`) and in Danish (`SBAM_test`) |

The program's printouts and command-line options are in Danish.

## CompareStocastic: SBAM against stochastic matching

The improved SBAM (`Matching_test.cs` with all changes on) is compared with the matching rules of
DYNASIM, CORSIM/DYNACAN and CBOLT, as described by Perese (2002). All methods run inside the same
program on the same simulated persons. The two things measured are precision and the running time
of one call. Main findings:

- With five years of historical data, SBAM is the most precise method that uses only the historical
  table. Its systematic error is 4.0 per cent of the couples, against 2.9 for a random draw from
  the true model and 4.7-16.0 for stochastic rules that use the same table.
- For the stochastic rules, the compatibility index matters more than the rule. With a
  nine-parameter logit the error is 8.6 or more. With a well-specified logit the CORSIM rule
  reaches 3.1.
- A call of SBAM takes 24 ms for a pool of 72,000 persons. The CBOLT and CORSIM rules take 0.8 and
  2.2 seconds when implemented person by person, and 17 and 49 ms when implemented on types of
  persons, which gives the same couples.
- SBAM fails when the historical data are thin. With 3,624 historical couples its error is 27.7,
  and it leaves 1.5 per cent of the couples unformed. A logit index is unaffected.

The artificial market is of the kind SBAM assumes, and the running times are from one laptop. The
paper's discussion lists these and the other limitations.

What is in the folder:

| Path | Contents |
|---|---|
| `Program.cs` | The experiments, the measures and the timing |
| `StochasticMatching.cs` | The DYNASIM, CORSIM and CBOLT rules, by person and by type |
| `Compatibility.cs` | The compatibility indices: distance function, two logit models, table index and oracle index |
| `Market.cs` | The artificial marriage market |
| `data/`, `output/` | Generated historical data and the result files behind the paper |
| `doc/` | The paper |

The SBAM code is not copied. The project links `Matching_test.cs`, `RASMatrix_test.cs` and the
model files from `CodeTest`, so both folders must be present.

## Running the programs

Both projects need the .NET SDK and build for .NET 8. NuGet packages are restored on the first
build.

The test of the model's code, from the folder `CodeTest`:

```
dotnet run                                              (factor 1.0, types by province)
dotnet run -- --scale 1.3                               (the model's scale-up of the pool)
dotnet run -- --kommune                                 (types by municipality)
dotnet run -c Release -- --sammenlign --koersler 10     (Matching.cs against the variants in Matching_test.cs)
```

The comparison with stochastic matching, from the folder `CompareStocastic`:

```
dotnet run -c Release -- --experiment baseline
dotnet run -c Release -- --experiment all --runs 10
```

| Option | Meaning |
|---|---|
| `--experiment` | `baseline`, `change` (a changed population), `size` (pool size), `history` (amount of historical data) or `all` |
| `--runs` | Number of pools per setting (default 10) |
| `--seed` | First seed for the pools and the matching (default 1). The historical data have a fixed seed |
| `--methods` | Comma-separated method keys, for example `SBAM,COR-L,CBO-T`, to run only some of the methods |

Each experiment writes `results_<experiment>.txt` and `.tsv` to `output/` and overwrites the files
that are there. The precision figures are the same in every run with the same seed. The running
times depend on the machine.

The papers are built with pdfLaTeX from the `.tex` files in the `doc` folders.

## Other files

- [`SBAM.pdf`](SBAM.pdf): Stephensen and Markeprand (2013), the paper that describes SBAM.
- [`SBAM_papir_vs_kode.md`](SBAM_papir_vs_kode.md): a comparison of that paper with the code in
  SMILE, made by reading the code (in Danish).

## References

- Choo, E. and A. Siow (2006). Who marries whom and why. *Journal of Political Economy*, 114(1),
  175-201.
- Perese, K. (2002). Mate matching for microsimulation models. Technical Paper 2002-3, Long-Term
  Modeling Group, Congressional Budget Office, Washington, DC.
- Stephensen, P. and T. Markeprand (2013). SBAM: An algorithm for pair matching. DREAM Working
  Paper 2013:3.
