// Comparison of Matching.cs with the variants in Matching_test.cs on the artificial market.
// All variants get the same historical data, the same pool and the same seed for Simulation.Random.
//
// Usage:  dotnet run -- --sammenlign [--koersler 5] [--seed 1] [--faktorer 1.0,1.05,1.3]
//                                    [--pulje 1.0] [--skaev 0.0] [--forskyd 0.0] [--kommune]
//   --koersler  Number of pools per factor (seed, seed+1, ...). The tables show averages over the runs.
//   --faktorer  The values of MatchingScaleUp that are run (the same value in all municipalities).
//   --pulje     Multiplies the probability of entering the pool. 0.1 gives a pool of one tenth, so that
//               there are few persons of each type, as there are in the model.
//   --skaev     Share of the men who are removed from the pool after it has been made equally large for the two genders.
//   --forskyd   Shifts the pool towards older and longer educated persons, so that it does not resemble the historical data.
//               The probability of entering the pool is multiplied by (1 + f*(edu-2)) * (1 + f*(age-37)/17).
//   --kommune   The types are divided by municipality instead of province.
// The printout is also saved in output\sammenligning*.txt. The options and the printouts are in Danish.

using System.Diagnostics;
using System.Globalization;
using DreamSMILE.MicroSim;
using DreamSMILE.Numerical;

namespace SbamTest;

/// <summary>Gives the test access to the protected field _testFile in Matching_test.</summary>
class TestMatching_test : Matching_test
{
    public TestMatching_test(DwellingCharacteristics dwelChar) : base(dwelChar) { }
    public void FlushTestFile() { _testFile.Flush(); }
}

static partial class Program
{
    sealed class Variant
    {
        public string Name;
        public bool Original;              // true: the class Matching (Matching.cs). false: Matching_test with the settings below
        public bool SecondCall;            // Matching.cs: second call of matching() on the same object (as in the model's second year onwards)
        public int Regression;             // 1 or 2: Matching_test with everything switched off; the couples must be the same as in A1 and A2 respectively
        public bool Reset = true;          // TEST0
        public bool Ras, TargetAsCouples;  // TEST1 and TEST2
        public EPairingMethod Method;      // TEST3
        public int Rounds = 1;
        public int MaxPasses = 200;        // cap on RAS passes (only with TEST1)
        public ERasMethod RasMethod = ERasMethod.Cells;   // TEST4: 4a = Factors, 4a+4b = Symmetric
        public bool ActiveOnly;            // TEST4: 4c
        public string SameAs;              // the letter of an earlier variant that this one must form exactly the same couples as
        public int SameCouples;            // number of runs in which the couples were the same as in SameAs

        // Times in milliseconds for each run: the whole of matching(), of which RAS adjustment and of which pairing
        public List<double> Ms = new List<double>(), MsRas = new List<double>(), MsPairing = new List<double>();

        // Sums over the runs
        public double Couples, Target, GapMean, GapSd, Extreme, SameEdu, SameReg, Passes, RoundsUsed, DPool, Omitted, NotConverged;
        public double[] DTrue = new double[4], DRef = new double[4];
        public double[,] SumTable = new double[Market.C, Market.C];
    }

    static int Compare(string[] args)
    {
        int runs = 5, seed = 1;
        double poolScale = 1.0, skew = 0.0, shift = 0.0;
        bool byMunicipality = false;
        double[] factors = { 1.0, 1.05, 1.3 };
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--sammenlign") continue;
            else if (args[i] == "--koersler") runs = int.Parse(args[++i]);
            else if (args[i] == "--seed") seed = int.Parse(args[++i]);
            else if (args[i] == "--pulje") poolScale = double.Parse(args[++i], CultureInfo.InvariantCulture);
            else if (args[i] == "--skaev") skew = double.Parse(args[++i], CultureInfo.InvariantCulture);
            else if (args[i] == "--forskyd") shift = double.Parse(args[++i], CultureInfo.InvariantCulture);
            else if (args[i] == "--faktorer") factors = args[++i].Split(',').Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
            else if (args[i] == "--kommune") byMunicipality = true;
            else throw new ArgumentException("Ukendt argument: " + args[i]);
        }

        string root = ProjectDir();
        string dataDir = Path.Combine(root, "data"), outDir = Path.Combine(root, "output");
        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(outDir);

        string name = "sammenligning";
        if (poolScale != 1.0) name += "_pulje" + poolScale.ToString(CultureInfo.InvariantCulture);
        if (skew != 0.0) name += "_skaev" + skew.ToString(CultureInfo.InvariantCulture);
        if (shift != 0.0) name += "_forskyd" + shift.ToString(CultureInfo.InvariantCulture);
        if (byMunicipality) name += "_kommune";
        using var file = new StreamWriter(Path.Combine(outDir, name + ".txt"));
        void Print(string s = "") { Console.WriteLine(s); file.WriteLine(s); }

        // Market and historical data: the same as in the single run (Main)
        var market = new Market();
        const int C = Market.C;
        var dataRng = new Random(DataSeed);
        string histFile = Path.Combine(dataDir, $"SBAM_testdata_nyePar_{FirstYear}_{FirstYear + Years - 1}.txt");
        WriteSingles(market, Path.Combine(dataDir, "SBAM_testdata_enlige.txt"));
        int[,] hist = WriteHistory(market, dataRng, histFile);
        double[,] seed0 = ToDouble(hist);

        MSSettings.OutputDir = outDir;
        MSSettings.ProjectionID = "sammenlign";
        MSSettings.InputDataMatching = histFile;
        MSSettings.MatchingMinAge = 15;
        MSSettings.MatchingMaxAge = 65;
        MSSettings.UseMunicipalityDependentSBAM = byMunicipality;
        MSSettings.SBAMMunicipalityGrassSow = false;
        MSSettings.MatchingScaleUp = Enumerable.Repeat(1.0, MSSettings.MUNICIPALITY).ToArray();

        var sim = Simulation.Create(FirstYear + Years, seed);
        var test = new TestMatching_test(new DwellingCharacteristics());
        Console.WriteLine();

        Print("Sammenligning af Matching.cs og varianterne i Matching_test.cs");
        Print($"Kørsler pr. faktor: {runs} (seed {seed}-{seed + runs - 1}), typeopdeling: {(byMunicipality ? "kommune" : "landsdel")}, pulje x {poolScale}, andel af mændene fjernet: {skew}");
        Print("Ændringer: 0 = Reset() gendanner de historiske data, 1 = RAS til konvergens, 2 = tabellen summerer til antal par,");
        Print("           3 = pardannelse i ét gennemløb");
        Print("           4 = hurtigere beregning af RAS: 4a faktorer i arrays, 4b én faktor pr. type (symmetri), 4c kun typer med personer i puljen");
        Print("A1 er det første kald af matching() på et nyt Matching-objekt, A2 det andet kald på samme objekt (modellens andet år og frem).");
        Print("A1 viser også, hvad Matching.cs giver i alle år, når kun ændring 0 er lavet.");
        if (shift != 0.0)
        {
            Print($"Forskudt pulje (--forskyd {shift}): sandsynligheden for at komme i puljen er ganget med");
            Print($"(1 + {shift} * (udd - 2)) * (1 + {shift} * (alder - 37) / 17), så puljen har flere ældre og flere med lang uddannelse end de historiske data.");
            Print("Den sande model gælder ikke for en forskudt pulje. Kun sammenligningen med reference-SBAM kan bruges.");
        }
        Print();

        var truth = market.Describe(market.Mu);
        bool ok = true;
        ok &= RasSelfTest(Print);
        Print();

        foreach (double factor in factors)
        {
            MSSettings.MatchingScaleUp = Enumerable.Repeat(factor, MSSettings.MUNICIPALITY).ToArray();

            var variants = new List<Variant>
            {
                new Variant { Name = "A1 Matching.cs, første år", Original = true },
                new Variant { Name = "A2 Matching.cs, senere år", Original = true, SecondCall = true },
                // Matching_test with everything switched off: first call on the historical data (as A1), second call after Reset() as in the model (as A2).
                // The second call has Reset = true, so that the matrix again holds the historical data when the variants below are run.
                new Variant { Name = "R1", Regression = 1, Reset = false, Method = EPairingMethod.Original },
                new Variant { Name = "R2", Regression = 2, Method = EPairingMethod.Original },
                new Variant { Name = "B  0+1", Ras = true, Method = EPairingMethod.Original },
                new Variant { Name = "C  0+1+2", Ras = true, TargetAsCouples = true, Method = EPairingMethod.Original },
                new Variant { Name = "D  0+1+2+3 uafhængig afrunding", Ras = true, TargetAsCouples = true, Method = EPairingMethod.Independent },
                new Variant { Name = "E  0+1+2+3 systematisk, typeorden", Ras = true, TargetAsCouples = true, Method = EPairingMethod.SystematicTypeOrder },
                new Variant { Name = "F  0+1+2+3 systematisk, hierark.", Ras = true, TargetAsCouples = true, Method = EPairingMethod.SystematicHierarchical },
                new Variant { Name = "G  F med opsamlingsrunder", Ras = true, TargetAsCouples = true, Method = EPairingMethod.SystematicHierarchical, Rounds = 5 },
                new Variant { Name = "H  F uden 1 (0+2+3)", TargetAsCouples = true, Method = EPairingMethod.SystematicHierarchical },
                new Variant { Name = "I  F uden 2 (0+1+3)", Ras = true, Method = EPairingMethod.SystematicHierarchical },
                new Variant { Name = "J  F uden 1 og 2 (0+3)", Method = EPairingMethod.SystematicHierarchical },
                new Variant { Name = "K  F med højst 10 RAS-gennemløb", Ras = true, TargetAsCouples = true, Method = EPairingMethod.SystematicHierarchical, MaxPasses = 10 },
                new Variant { Name = "L  G med højst 10 RAS-gennemløb", Ras = true, TargetAsCouples = true, Method = EPairingMethod.SystematicHierarchical, Rounds = 5, MaxPasses = 10 },
                // TEST4: the same adjustment computed faster. M must form exactly the same couples as L
                new Variant { Name = "M  L + 4a (faktorer i arrays)", Ras = true, TargetAsCouples = true, Method = EPairingMethod.SystematicHierarchical, Rounds = 5, MaxPasses = 10,
                              RasMethod = ERasMethod.Factors, SameAs = "L" },
                new Variant { Name = "N  L + 4a+4b (symmetri)", Ras = true, TargetAsCouples = true, Method = EPairingMethod.SystematicHierarchical, Rounds = 5, MaxPasses = 10,
                              RasMethod = ERasMethod.Symmetric },
                new Variant { Name = "O  L + 4a+4b+4c (aktive typer)", Ras = true, TargetAsCouples = true, Method = EPairingMethod.SystematicHierarchical, Rounds = 5, MaxPasses = 10,
                              RasMethod = ERasMethod.Symmetric, ActiveOnly = true },
                new Variant { Name = "P  O, RAS til konvergens", Ras = true, TargetAsCouples = true, Method = EPairingMethod.SystematicHierarchical, Rounds = 5,
                              RasMethod = ERasMethod.Symmetric, ActiveOnly = true },
                new Variant { Name = "Q  P, én runde", Ras = true, TargetAsCouples = true, Method = EPairingMethod.SystematicHierarchical,
                              RasMethod = ERasMethod.Symmetric, ActiveOnly = true },
                new Variant { Name = "S  O, én runde", Ras = true, TargetAsCouples = true, Method = EPairingMethod.SystematicHierarchical, MaxPasses = 10,
                              RasMethod = ERasMethod.Symmetric, ActiveOnly = true },
            };
            var shown = variants.Where(v => v.Regression == 0).ToList();

            // Basis of comparison: the reference SBAM (expected table) and one Poisson draw from it with the same number of couples as the target
            var reference = new Variant { Name = "Reference-SBAM, forventet" };
            var noise = new Variant { Name = "Reference-SBAM, ét træk" };
            double[,] sumRef = new double[C, C];
            double totalPool = 0, totalMen = 0, totalWomen = 0;
            bool sameCouples = true, checksOk = true;

            for (int k = 0; k < runs; k++)
            {
                int s = seed + k;

                // Pool as in Main: each single enters the pool with the historical match rate of his or her type times the factor
                var poolRng = new Random(s + 1000);
                int[] nA = new int[C], nB = new int[C];
                for (int x = 0; x < C; x++)
                {
                    long row = 0, col = 0;
                    for (int y = 0; y < C; y++) { row += hist[x, y]; col += hist[y, x]; }
                    // Shift of the pool towards older and longer educated persons (1 without --forskyd)
                    double w = poolScale * factor * (1 + shift * (market.Edu[x] - 1)) * (1 + shift * (market.Age[x] - 37) / 17.0);
                    nA[x] = Market.Binomial(poolRng, market.MA[x], Math.Min(1, w * row / (double)(Years * market.MA[x])));
                    nB[x] = Market.Binomial(poolRng, market.MB[x], Math.Min(1, w * col / (double)(Years * market.MB[x])));
                }
                int surplus = nA.Sum() - nB.Sum();
                if (surplus > 0) CutBack(poolRng, nA, surplus);
                if (surplus < 0) CutBack(poolRng, nB, -surplus);
                if (skew > 0) CutBack(poolRng, nA, (int)(skew * nA.Sum()));

                sim.Households.Clear();
                for (int x = 0; x < C; x++)
                {
                    for (int i = 0; i < nA[x]; i++) sim.Households.Add(NewSingle(market, x, EGender.Male));
                    for (int i = 0; i < nB[x]; i++) sim.Households.Add(NewSingle(market, x, EGender.Female));
                }
                totalPool += sim.Households.Count; totalMen += nA.Sum(); totalWomen += nB.Sum();

                // The target for the number of couples, computed as in Matching.matching()
                int[] poolByMunicipality = new int[MSSettings.MUNICIPALITY];
                foreach (Household h in sim.Households) poolByMunicipality[(int)h.Dwelling.DwellingMunicipality]++;
                double targetTmp = 0;
                for (int m = 0; m < MSSettings.MUNICIPALITY; m++) targetTmp += (poolByMunicipality[m] / 2) / MSSettings.MatchingScaleUp[m];
                int targetPairs = (int)targetTmp;

                // Reference SBAM: RAS to convergence on men x women. If the pool is lopsided, the large side is scaled down
                double[] rowTot = nA.Select(v => (double)v).ToArray(), colTot = nB.Select(v => (double)v).ToArray();
                double common = Math.Min(rowTot.Sum(), colTot.Sum());
                double fr = common / rowTot.Sum(), fc = common / colTot.Sum();
                for (int x = 0; x < C; x++) { rowTot[x] *= fr; colTot[x] *= fc; }
                var refTable = Ipf(seed0, rowTot, colTot).Table;
                double refSum = 0;
                foreach (double v in refTable) refSum += v;
                double[,] refTarget = new double[C, C];      // the reference scaled to the number of couples to be formed
                for (int x = 0; x < C; x++)
                    for (int y = 0; y < C; y++)
                    {
                        refTarget[x, y] = refTable[x, y] * targetPairs / refSum;
                        sumRef[x, y] += refTarget[x, y];
                    }
                Accumulate(reference, market, refTarget, refTable, targetPairs);
                Accumulate(noise, market, PoissonDraw(refTarget, new Random(s + 2000)), refTable, targetPairs);

                List<(Household, Household)> couplesA1 = null, couplesA2 = null;
                var couplesOf = new Dictionary<string, List<(Household, Household)>>();   // the couples in the variants that others are to be compared with
                TestMatching original = null;
                foreach (Variant v in variants)
                {
                    sim.Statistics.MatchedPairs.Clear();
                    Simulation.Random = new RandomEvent(s);

                    TextWriter console = Console.Out;
                    Console.SetOut(TextWriter.Null);           // Matching writes status lines to the console
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        if (v.Original && !v.SecondCall)
                        {
                            original = new TestMatching(new DwellingCharacteristics());   // new object: the historical data are read in again
                            sw.Restart();
                        }

                        if (v.Original)
                            original.matching();
                        else
                        {
                            test.ResetToData = v.Reset;
                            test.RasToConvergence = v.Ras;
                            test.TargetAsCouples = v.TargetAsCouples;
                            test.PairingMethod = v.Method;
                            test.MaxRounds = v.Rounds;
                            test.RasMaxPasses = v.MaxPasses;
                            test.RasMethod = v.RasMethod;
                            test.RasActiveOnly = v.ActiveOnly;
                            test.matching();
                        }
                    }
                    finally { Console.SetOut(console); }
                    v.Ms.Add(sw.Elapsed.TotalMilliseconds);
                    if (!v.Original) { v.MsRas.Add(test.LastMsRas); v.MsPairing.Add(test.LastMsPairing); }

                    // Couple table and checks
                    double[,] formed = new double[C, C];
                    int[] matchedA = new int[C], matchedB = new int[C];
                    var seen = new HashSet<Household>();
                    foreach (var (h1, h2) in sim.Statistics.MatchedPairs)
                    {
                        if (!seen.Add(h1) || !seen.Add(h2)) checksOk = false;                      // a household in two couples
                        Person p1 = h1.FirstAdult, p2 = h2.FirstAdult;
                        if (p1.Gender == p2.Gender) { checksOk = false; continue; }                // the data have no couples of the same gender
                        Person man = p1.Gender == EGender.Male ? p1 : p2, woman = p1.Gender == EGender.Male ? p2 : p1;
                        formed[man.TestType, woman.TestType]++;
                        matchedA[man.TestType]++;
                        matchedB[woman.TestType]++;
                    }
                    int pairs = sim.Statistics.MatchedPairs.Count;
                    // The original loop can form two couples in the last cell and thereby one couple more than the target
                    if (pairs > targetPairs + (v.Original || v.Method == EPairingMethod.Original ? 1 : 0)) checksOk = false;

                    if (v.Original && !v.SecondCall) couplesA1 = sim.Statistics.MatchedPairs.ToList();
                    else if (v.Original) couplesA2 = sim.Statistics.MatchedPairs.ToList();
                    else if (v.Regression == 1) sameCouples &= couplesA1.SequenceEqual(sim.Statistics.MatchedPairs);
                    else if (v.Regression == 2) sameCouples &= couplesA2.SequenceEqual(sim.Statistics.MatchedPairs);
                    if (v.Regression != 0) continue;

                    // TEST4: variants that must form exactly the same couples as an earlier variant
                    string letter = v.Name.Substring(0, 1);
                    if (variants.Any(x => x.SameAs == letter)) couplesOf[letter] = sim.Statistics.MatchedPairs.ToList();
                    if (v.SameAs != null && couplesOf[v.SameAs].SequenceEqual(sim.Statistics.MatchedPairs)) v.SameCouples++;

                    Accumulate(v, market, formed, refTable, targetPairs);
                    for (int x = 0; x < C; x++)
                        for (int y = 0; y < C; y++)
                            v.SumTable[x, y] += formed[x, y];

                    // The type distribution of the matched persons against that of the pool (both genders)
                    double dPool = 0, sumMatched = 2.0 * pairs, sumPool = nA.Sum() + nB.Sum();
                    for (int x = 0; x < C; x++)
                        dPool += Math.Abs(matchedA[x] / sumMatched - nA[x] / sumPool) + Math.Abs(matchedB[x] / sumMatched - nB[x] / sumPool);
                    v.DPool += 50 * dPool;

                    if (!v.Original)
                    {
                        v.Passes += test.LastRasPasses;
                        v.RoundsUsed += test.LastRounds;
                        v.Omitted += test.LastOmitted;
                        if (v.Ras && !test.LastRasConverged) v.NotConverged++;
                    }
                }
                original.CloseTestFile();
            }
            test.FlushTestFile();

            double n = runs;
            Print($"=== MatchingScaleUp = {factor} ===");
            Print($"Pulje: {totalPool / n:0} personer ({totalMen / n:0} mænd, {totalWomen / n:0} kvinder), mål: {reference.Target / n:0} par. Gennemsnit over {runs} kørsler.");
            Print();
            Print("Antal par og parrenes sammensætning");
            Print("Tid er medianen over kørslerne af tiden i matching() i millisekunder; \"RAS\" og \"par\" er den del, der går til RAS-tilpasning og pardannelse.");
            Print("                                        Par  Pct. af  Alders-    Std.  Ekstr.  Samme  Samme   Tabte      RAS-  Runder    Tid  heraf  heraf");
            Print("                                             målet    forskel          pct.    udd.   reg.    par   gennemløb           ms    RAS    par");
            Print($"{"Sand model (mu)",-34}{truth.Couples,9:0}{"",9}{truth.GapMean,9:0.00}{truth.GapSd,8:0.00}{truth.Extreme,8:0.00}{truth.SameEdu,7:0.0}{truth.SameReg,7:0.0}");
            foreach (Variant v in new[] { reference, noise }.Concat(shown))
            {
                string line = $"{v.Name,-34}{v.Couples / n,9:0.0}{100 * v.Couples / v.Target,9:0.00}{v.GapMean / n,9:0.00}{v.GapSd / n,8:0.00}{v.Extreme / n,8:0.00}{v.SameEdu / n,7:0.0}{v.SameReg / n,7:0.0}";
                if (!v.Original && v != reference && v != noise)
                    line += $"{v.Omitted / n,8:0.0}{v.Passes / n,10:0.0}{(v.NotConverged > 0 ? "*" : " ")}{v.RoundsUsed / n,7:0.0}{Median(v.Ms),7:0}{Median(v.MsRas),7:0.0}{Median(v.MsPairing),7:0.0}";
                else if (v.Original)
                    line += $"{"",8}{"2",10} {"",7}{Median(v.Ms),7:0}";
                Print(line);
            }
            Print();
            Print("Dissimilaritetsindeks D i pct. af parrene (gennemsnit af D i de enkelte kørsler). \"Sum\" er D for kørslerne lagt sammen.");
            Print("                                   ---- mod den sande model ----   ---- mod reference-SBAM -----   -- Sum, samlet --   Typer");
            Print("                                    Alder    Udd    Reg  Samlet     Alder    Udd    Reg  Samlet    Sand    Reference   mod pulje");
            foreach (Variant v in new[] { reference, noise }.Concat(shown))
            {
                string line = $"{v.Name,-34}{v.DTrue[0] / n,7:0.00}{v.DTrue[1] / n,7:0.00}{v.DTrue[2] / n,7:0.00}{v.DTrue[3] / n,8:0.00}   {v.DRef[0] / n,7:0.00}{v.DRef[1] / n,7:0.00}{v.DRef[2] / n,7:0.00}{v.DRef[3] / n,8:0.00}";
                if (v == reference)
                    line += $"{market.Dissimilarity(sumRef, market.Mu)[3],8:0.00}{0,13:0.00}";
                else if (v != noise)
                    line += $"{market.Dissimilarity(v.SumTable, market.Mu)[3],8:0.00}{market.Dissimilarity(v.SumTable, sumRef)[3],13:0.00}{v.DPool / n,10:0.00}";
                Print(line);
            }
            Print();
            ok &= Check(Print, "Matching_test med alt slået fra danner præcis de samme par som Matching.cs, både i første og andet kald", sameCouples);
            ok &= Check(Print, "Ingen husholdning i to par, ingen par med samme køn, ikke flere par end målet (den oprindelige løkke: højst ét mere)", checksOk);
            foreach (Variant v in variants.Where(v => v.SameAs != null))
                ok &= Check(Print, $"Variant {v.Name.Substring(0, 1)} danner præcis de samme par som variant {v.SameAs} i {v.SameCouples} af {runs} kørsler", v.SameCouples == runs);
            if (variants.Any(v => v.NotConverged > 0)) Print("* RAS-tilpasningen nåede ikke tolerancen i mindst én kørsel.");
            Print();
        }

        Print($"Udskriften er gemt i {Path.Combine(outDir, name + ".txt")}");
        return ok ? 0 : 1;
    }

    /// <summary>Adds the measures of one couple table to the sums of the variant.</summary>
    static void Accumulate(Variant v, Market market, double[,] table, double[,] referenceTable, int targetPairs)
    {
        var s = market.Describe(table);
        v.Couples += s.Couples; v.Target += targetPairs;
        v.GapMean += s.GapMean; v.GapSd += s.GapSd; v.Extreme += s.Extreme; v.SameEdu += s.SameEdu; v.SameReg += s.SameReg;
        double[] dTrue = market.Dissimilarity(table, market.Mu), dRef = market.Dissimilarity(table, referenceTable);
        for (int i = 0; i < 4; i++) { v.DTrue[i] += dTrue[i]; v.DRef[i] += dRef[i]; }
    }

    static double Median(List<double> x)
    {
        if (x.Count == 0) return double.NaN;
        var s = x.OrderBy(v => v).ToList();
        return s.Count % 2 == 1 ? s[s.Count / 2] : (s[s.Count / 2 - 1] + s[s.Count / 2]) / 2;
    }

    static bool Check(Action<string> print, string text, bool ok)
    {
        print($"[{(ok ? "OK  " : "FEJL")}] {text}");
        return ok;
    }

    /// <summary>
    /// Check of the fast RAS methods (TEST4) on small random matrices. The matrices are symmetric and also have
    /// cells on the diagonal (two of the same type), which the test market does not have. All methods are run to
    /// the end, and the adjusted, folded tables are compared cell by cell with the original method.
    /// </summary>
    static bool RasSelfTest(Action<string> print)
    {
        var rng = new Random(12345);
        const int count = 20;
        double devFactors = 0, devSymmetric = 0, devTarget = 0;

        for (int t = 0; t < count; t++)
        {
            int n = 30 + rng.Next(30);
            double[,] a = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j <= i; j++)
                    if (rng.NextDouble() < 0.25)
                    {
                        double v = 1 + rng.Next(20);
                        if (i == j) v *= 2;     // a cell on the diagonal counts two persons of the same type per couple
                        a[i, j] = a[j, i] = v;
                    }
            // The last type gets at least two cells (see the last type with only one cell below)
            a[n - 1, 0] = a[0, n - 1] = 3;
            a[n - 1, 1] = a[1, n - 1] = 5;

            // Targets that can be met: a random symmetric scaling of the matrix. Every fifth type gets the target 0
            double[] d = new double[n], target = new double[n];
            for (int i = 0; i < n; i++)
                d[i] = i % 5 == 4 ? 0 : Math.Exp(2 * rng.NextDouble() - 1);
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    target[i] += d[i] * a[i, j] * d[j];

            var cells = new List<RASMatrixCell>();
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    if (a[i, j] > 0) cells.Add(new RASMatrixCell(i, j, a[i, j]));
            var ras = new RASMatrix_test(cells, n, false) { RunToConvergence = true, MaxPasses = 100000, Tolerance = 1e-13 };

            Dictionary<(int, int), double> Run(ERasMethod method, bool activeOnly)
            {
                ras.Method = method;
                ras.ActiveOnly = activeOnly;
                double[] m = (double[])target.Clone();
                var table = new Dictionary<(int, int), double>();
                foreach (RASMatrixCell c in ras.RASAdjust(m, m)) table[(c.Row, c.Col)] = c.Value;
                ras.Reset();
                return table;
            }
            double Deviation(Dictionary<(int, int), double> x, Dictionary<(int, int), double> y)
            {
                double max = 0;
                foreach (var k in x.Keys.Union(y.Keys))
                {
                    x.TryGetValue(k, out double vx);
                    y.TryGetValue(k, out double vy);
                    max = Math.Max(max, Math.Abs(vx - vy) / Math.Max(1.0, Math.Abs(vy)));
                }
                return max;
            }

            var original = Run(ERasMethod.Cells, false);
            foreach (bool activeOnly in new[] { false, true })
            {
                devFactors = Math.Max(devFactors, Deviation(Run(ERasMethod.Factors, activeOnly), original));
                var sym = Run(ERasMethod.Symmetric, activeOnly);
                devSymmetric = Math.Max(devSymmetric, Deviation(sym, original));

                // The sums of the types in the symmetric solution against the target. A folded cell off the diagonal counts half for each type
                double[] sum = new double[n];
                foreach (var (k, v) in sym)
                {
                    if (k.Item1 == k.Item2) sum[k.Item1] += v;
                    else { sum[k.Item1] += v / 2; sum[k.Item2] += v / 2; }
                }
                for (int i = 0; i < n; i++)
                    devTarget = Math.Max(devTarget, Math.Abs(sum[i] - target[i]) / Math.Max(1.0, target[i]));
            }
        }

        print($"Kontrol af de hurtige RAS-metoder på {count} tilfældige symmetriske matricer med 30-59 typer og celler i diagonalen.");
        print("Alle metoder er kørt til ende. Største relative afvigelse i en foldet celle fra den oprindelige metode:");
        print($"  4a (faktorer): {devFactors:0.0e+0}   4a+4b (symmetrisk): {devSymmetric:0.0e+0}   Symmetrisk, største afvigelse fra en types mål: {devTarget:0.0e+0}");
        bool ok = Check(print, "De hurtige RAS-metoder giver samme tilpassede tabel som den oprindelige (afvigelse under 1e-8)", devFactors < 1e-8 && devSymmetric < 1e-8 && devTarget < 1e-8);

        // The last type with only one cell: the constructor in RASMatrix.cs builds rows and columns with the loop
        // "for (i = 0; i < n - 1; i++)", so the very last cell is only included if it shares a row (column) with the cell before it.
        var few = new List<RASMatrixCell>
        {
            new RASMatrixCell(0, 1, 4), new RASMatrixCell(1, 0, 4), new RASMatrixCell(1, 2, 6), new RASMatrixCell(2, 1, 6)
        };
        var small = new RASMatrix_test(few, 3, false) { RunToConvergence = true };
        double[] m3 = { 4, 10, 6 };
        small.RASAdjust(m3, m3);
        int withoutDataOriginal = small.TargetsWithoutData;
        small.Reset();
        small.Method = ERasMethod.Symmetric;
        m3 = new double[] { 4, 10, 6 };
        small.RASAdjust(m3, m3);
        print($"Sidste type med kun én celle (3 typer, alle med data): typer, hvis mål sættes til 0, fordi de mangler data: oprindelig metode {withoutDataOriginal}, hurtig metode {small.TargetsWithoutData}");
        return ok;
    }
}
