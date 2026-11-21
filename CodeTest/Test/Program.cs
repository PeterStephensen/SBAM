// Quick test of the SBAM implementation in Matching.cs on the artificial market from
// LatentGroups_paper02.pdf, Section 4.1 (historical data and pool as in Sections 4.2 and 4.3).
//
// Usage:  dotnet run -- [--scale 1.0] [--seed 1] [--kommune]
//   --scale    MatchingScaleUp for all municipalities. 1.0 corresponds to the SBAM of the paper (the whole
//              pool is matched); the model uses 1.05-1.3, where the pool is scaled up and only 1/scale of
//              it is matched.
//   --seed     Seed for the pool and for Simulation.Random (the pairing). The test data have a fixed seed.
//   --kommune  Sets UseMunicipalityDependentSBAM, so that the types are divided by municipality instead of
//              province.
//
// With --sammenlign the comparison of Matching.cs and Matching_test.cs is run instead (see Sammenlign.cs).
// The command-line options and the program's printouts are in Danish.

using System.Diagnostics;
using System.Globalization;
using DreamSMILE.MicroSim;

namespace SbamTest;

/// <summary>Gives the test access to the protected fields of Matching without changing Matching.cs.</summary>
class TestMatching : Matching
{
    public TestMatching(DwellingCharacteristics dwelChar) : base(dwelChar) { }
    public void ShowRasIterations(bool on) { _ras.ConsoleMessage = on; }
    public void FlushTestFile() { _testFile.Flush(); }
    public void CloseTestFile() { _testFile.Close(); }   // so that a new Matching can be created with the same ProjectionID
}

static partial class Program   // the comparison of Matching.cs and Matching_test.cs is in Sammenlign.cs
{
    const int FirstYear = 2008, Years = 5;      // the base class Matching uses data for 2008-2012
    const int DataSeed = 20261120;

    // The market's 3 education levels and 4 regions expressed in the model's coding
    static readonly EEducationDS8[] EduCode = { EEducationDS8.Grundskole, EEducationDS8.ErhvervsfagligeHovedforlob, EEducationDS8.UdelteKandidat };
    static readonly EDwellingRegion[] RegionCode = { EDwellingRegion.KoebenhavnBy, EDwellingRegion.KoebenhavnOmegn, EDwellingRegion.Fyn, EDwellingRegion.JyllandOest };
    static readonly EDwellingMunicipality[] MunicipalityCode = { EDwellingMunicipality.København, EDwellingMunicipality.Gentofte, EDwellingMunicipality.Odense, EDwellingMunicipality.Aarhus };
    static readonly int[] MunicipalityDst = { 101, 157, 461, 751 };

    static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;   // decimal point in all printouts

        if (args.Contains("--sammenlign")) return Compare(args);  // Matching.cs against the variants in Matching_test.cs

        double scaleUp = 1.0;
        int seed = 1;
        bool byMunicipality = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--scale") scaleUp = double.Parse(args[++i], CultureInfo.InvariantCulture);
            else if (args[i] == "--seed") seed = int.Parse(args[++i]);
            else if (args[i] == "--kommune") byMunicipality = true;
            else throw new ArgumentException("Ukendt argument: " + args[i]);
        }

        string root = ProjectDir();
        string dataDir = Path.Combine(root, "data");
        string outDir = Path.Combine(root, "output");
        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(outDir);

        // ---------------------------------------------------------------------------------------
        // 1. The artificial market (Section 4.1)
        // ---------------------------------------------------------------------------------------
        var sw = Stopwatch.StartNew();
        var market = new Market();
        const int C = Market.C;
        var truth = market.Describe(market.Mu);

        Console.WriteLine("=== 1. Kunstigt marked (LatentGroups_paper02, afsnit 4.1) ===");
        Console.WriteLine($"Typer pr. køn: {C}, celler i partabellen: {C * C}");
        Console.WriteLine($"kappa = {market.Kappa:0.000000}, ligevægtsfejl = {market.EquilibriumError:0.0e+0}, tid = {sw.Elapsed.TotalSeconds:0.0} s");
        Console.WriteLine();
        Console.WriteLine("                                        Beregnet    Papiret");
        Row("Enlige mænd", market.MA.Sum(), "299,997", "0");
        Row("Enlige i alt (output2/numbers.tex)", market.MA.Sum() + market.MB.Sum(), "600,000", "0");
        Row("Par pr. år", truth.Couples, "36,000", "0");
        Row("Aldersforskel, gennemsnit (år)", truth.GapMean, "1.9");
        Row("Aldersforskel, standardafvigelse", truth.GapSd, "3.3");
        Row("Samme uddannelse (pct.)", truth.SameEdu, "55");
        Row("Samme region (pct.)", truth.SameReg, "77");
        Row("Ekstreme par (pct.)", truth.Extreme, "0.5");
        Row("Matchrate, mænd 25 år (pct.)", MaleMatchRate(market, 25), "18");
        Row("Matchrate, mænd 50 år (pct.)", MaleMatchRate(market, 50), "6");
        Console.WriteLine();

        // ---------------------------------------------------------------------------------------
        // 2. Test data: singles per type and 5 years of historical couples in the format Matching.cs reads
        // ---------------------------------------------------------------------------------------
        var dataRng = new Random(DataSeed);
        string singlesFile = Path.Combine(dataDir, "SBAM_testdata_enlige.txt");
        string histFile = Path.Combine(dataDir, $"SBAM_testdata_nyePar_{FirstYear}_{FirstYear + Years - 1}.txt");
        WriteSingles(market, singlesFile);
        int[,] hist = WriteHistory(market, dataRng, histFile);

        long histCouples = 0, histCells = 0;
        foreach (int v in hist) { histCouples += v; if (v > 0) histCells++; }
        Console.WriteLine("=== 2. Testdata ===");
        Console.WriteLine($"Enlige pr. type : {singlesFile}");
        Console.WriteLine($"Historiske par  : {histFile}");
        Console.WriteLine($"                  {histCouples} par i {histCells} celler over {Years} år (papiret: 179,922 par i 36,295 celler med R's tilfældige tal)");
        Console.WriteLine();

        // ---------------------------------------------------------------------------------------
        // 3. Set-up as in the model and reading of the data into Matching (initializeMatching)
        // ---------------------------------------------------------------------------------------
        MSSettings.OutputDir = outDir;
        MSSettings.ProjectionID = "test";
        MSSettings.InputDataMatching = histFile;
        MSSettings.MatchingMinAge = 15;
        MSSettings.MatchingMaxAge = 65;                               // as in Program.cs in the model
        MSSettings.UseMunicipalityDependentSBAM = byMunicipality;
        MSSettings.SBAMMunicipalityGrassSow = false;
        MSSettings.MatchingScaleUp = Enumerable.Repeat(scaleUp, MSSettings.MUNICIPALITY).ToArray();

        var sim = Simulation.Create(FirstYear + Years, seed);

        Console.WriteLine("=== 3. Matching.cs ===");
        Console.WriteLine($"MatchingScaleUp = {scaleUp}, seed = {seed}, typeopdeling = {(byMunicipality ? "kommune" : "landsdel")}");
        sw.Restart();
        var matching = new TestMatching(new DwellingCharacteristics());
        double tInit = sw.Elapsed.TotalSeconds;
        Console.WriteLine();

        // ---------------------------------------------------------------------------------------
        // 4. Matching pool (Section 4.3): each single enters the pool with the historical match rate of
        //    his or her type (times MatchingScaleUp); the larger side is cut back at random.
        // ---------------------------------------------------------------------------------------
        var poolRng = new Random(seed + 1000);
        int[] nA = new int[C], nB = new int[C];
        for (int x = 0; x < C; x++)
        {
            long row = 0, col = 0;
            for (int y = 0; y < C; y++) { row += hist[x, y]; col += hist[y, x]; }
            nA[x] = Market.Binomial(poolRng, market.MA[x], Math.Min(1, scaleUp * row / (double)(Years * market.MA[x])));
            nB[x] = Market.Binomial(poolRng, market.MB[x], Math.Min(1, scaleUp * col / (double)(Years * market.MB[x])));
        }
        int surplus = nA.Sum() - nB.Sum();
        if (surplus > 0) CutBack(poolRng, nA, surplus);
        if (surplus < 0) CutBack(poolRng, nB, -surplus);

        for (int x = 0; x < C; x++)
        {
            for (int i = 0; i < nA[x]; i++) sim.Households.Add(NewSingle(market, x, EGender.Male));
            for (int i = 0; i < nB[x]; i++) sim.Households.Add(NewSingle(market, x, EGender.Female));
        }

        // The target for the number of couples, computed as in Matching.matching() (integer division per municipality)
        int[] poolByMunicipality = new int[MSSettings.MUNICIPALITY];
        foreach (Household h in sim.Households) poolByMunicipality[(int)h.Dwelling.DwellingMunicipality]++;
        double targetTmp = 0;
        for (int k = 0; k < MSSettings.MUNICIPALITY; k++) targetTmp += (poolByMunicipality[k] / 2) / MSSettings.MatchingScaleUp[k];
        int targetPairs = (int)targetTmp;

        // ---------------------------------------------------------------------------------------
        // 5. Run Matching.matching()
        // ---------------------------------------------------------------------------------------
        matching.ShowRasIterations(true);                             // prints "RASAdjust : itt = ..., err = ..."
        sw.Restart();
        matching.matching();
        double tMatch = sw.Elapsed.TotalSeconds;
        matching.FlushTestFile();
        Console.WriteLine();

        // ---------------------------------------------------------------------------------------
        // 6. Result
        // ---------------------------------------------------------------------------------------
        double[,] formed = new double[C, C];
        int[] matchedA = new int[C], matchedB = new int[C];
        int sameSex = 0, notInPool = 0;
        var seen = new HashSet<Household>();
        bool duplicates = false;
        foreach (var (h1, h2) in sim.Statistics.MatchedPairs)
        {
            if (!seen.Add(h1) || !seen.Add(h2)) duplicates = true;
            if (!h1.State.Is(EHouseholdState.InMatchingPool) || !h2.State.Is(EHouseholdState.InMatchingPool)) notInPool++;
            Person p1 = h1.FirstAdult, p2 = h2.FirstAdult;
            if (p1.Gender == p2.Gender) { sameSex++; continue; }
            Person man = p1.Gender == EGender.Male ? p1 : p2;
            Person woman = p1.Gender == EGender.Male ? p2 : p1;
            formed[man.TestType, woman.TestType]++;
            matchedA[man.TestType]++;
            matchedB[woman.TestType]++;
        }
        int pairs = sim.Statistics.MatchedPairs.Count;
        int poolSize = sim.Households.Count;

        Console.WriteLine("=== 4. Resultat ===");
        Console.WriteLine($"Pulje: {nA.Sum()} mænd + {nB.Sum()} kvinder = {poolSize} personer");
        Console.WriteLine($"Mål (targetAntalPar): {targetPairs} par. Dannet: {pairs} par ({100.0 * pairs / targetPairs:0.00} pct. af målet), heraf {sameSex} med samme køn.");
        Console.WriteLine($"Umatchede i puljen: {poolSize - 2 * pairs} personer ({100.0 * (poolSize - 2 * pairs) / poolSize:0.00} pct.)");
        Console.WriteLine($"Tid: indlæsning af data {tInit:0.00} s, matching() {tMatch:0.00} s");
        Console.WriteLine();

        // Reference: the SBAM of the paper with fully converged RAS on the same historical table and the same pool
        double[,] seed0 = new double[C, C];
        for (int x = 0; x < C; x++)
            for (int y = 0; y < C; y++)
                seed0[x, y] = hist[x, y];
        double[] rowTot = nA.Select(v => (double)v).ToArray(), colTot = nB.Select(v => (double)v).ToArray();
        var reference = Ipf(seed0, rowTot, colTot);
        // The table that the pairing in Matching.cs works from: RASAdjust() stops after two passes
        double[,] twoPass = RasAsInMatching(seed0, rowTot, colTot, 2);
        var draw = ToDouble(market.Draw(dataRng));
        var refDraw = PoissonDraw(reference.Table, dataRng);

        Console.WriteLine("Sortering målt mod den sande model (mu). D er dissimilaritetsindeks i pct. af parrene.");
        Console.WriteLine("                                      Par  Forskel    Std.  Ekstr.  S.udd  S.reg   D_alder  D_udd  D_reg  D_samlet");
        Measures("Sand model (mu)", market, market.Mu, market.Mu);
        Measures("Sand model, ét træk", market, draw, market.Mu);
        Measures("Reference-SBAM, forventet", market, reference.Table, market.Mu);
        Measures("RAS med 2 gennemløb, forventet", market, twoPass, market.Mu);
        Measures("Matching.cs, dannede par", market, formed, market.Mu);
        Console.WriteLine();
        Console.WriteLine($"Reference-SBAM er papirets algoritme: RAS kørt til konvergens ({reference.Iterations} iterationer, fejl {reference.Error:0.0e+0}).");
        Console.WriteLine("RAS med 2 gennemløb er den tabel, RASAdjust() i RASMatrix.cs afleverer til pardannelsen (genberegnet i testen).");
        Console.WriteLine();
        Console.WriteLine("Sortering målt mod reference-SBAM (isolerer implementeringen fra støjen i de historiske data):");
        Console.WriteLine("                                                                                D_alder  D_udd  D_reg  D_samlet");
        Against("Reference-SBAM, ét træk (støjniveau)", market, refDraw, reference.Table);
        Against("RAS med 2 gennemløb, forventet", market, twoPass, reference.Table);
        Against("Matching.cs, dannede par", market, formed, reference.Table);
        Console.WriteLine();

        // The distribution of the matched persons over types against that of the pool (both genders, 840 types)
        double dPool = 0;
        double sumMatched = matchedA.Sum() + matchedB.Sum(), sumPool = nA.Sum() + nB.Sum();
        for (int x = 0; x < C; x++)
            dPool += Math.Abs(matchedA[x] / sumMatched - nA[x] / sumPool) + Math.Abs(matchedB[x] / sumMatched - nB[x] / sumPool);
        Console.WriteLine($"Typefordeling af de matchede mod puljens typefordeling: D = {50 * dPool:0.00} pct. (0 hvis alle typer matches i samme omfang)");
        Console.WriteLine();

        // ---------------------------------------------------------------------------------------
        // 7. Checks
        // ---------------------------------------------------------------------------------------
        Console.WriteLine("=== 5. Kontroller ===");
        bool ok = true;
        ok &= Check("Ingen husholdning indgår i mere end ét par", !duplicates);
        ok &= Check("Alle matchede husholdninger var i puljen", notInPool == 0);
        ok &= Check("Ingen par med samme køn (de historiske data indeholder ingen)", sameSex == 0);
        ok &= Check("Antal dannede par overstiger ikke målet", pairs <= targetPairs);
        Console.WriteLine();
        Console.WriteLine($"Matching.cs' egen testfil: {Path.Combine(outDir, "testMatching2_test.txt")}");
        return ok ? 0 : 1;
    }

    // -------------------------------------------------------------------------------------------

    static string ProjectDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SbamTest.csproj"))) dir = dir.Parent;
        return dir != null ? dir.FullName : Directory.GetCurrentDirectory();
    }

    static void Row(string label, double value, string paper, string format = "0.00")
    {
        Console.WriteLine($"{label,-36}{value.ToString(format),12}{paper,11}");
    }

    static double MaleMatchRate(Market m, int age)
    {
        double matched = 0, singles = 0;
        for (int x = 0; x < Market.C; x++)
        {
            if (m.Age[x] != age) continue;
            singles += m.MA[x];
            for (int y = 0; y < Market.C; y++) matched += m.Mu[x, y];
        }
        return 100 * matched / singles;
    }

    static bool Check(string text, bool ok)
    {
        Console.WriteLine($"[{(ok ? "OK  " : "FEJL")}] {text}");
        return ok;
    }

    static void Measures(string label, Market m, double[,] tab, double[,] reference)
    {
        var s = m.Describe(tab);
        double[] d = m.Dissimilarity(tab, reference);
        Console.WriteLine($"{label,-30}{s.Couples,11:0}{s.GapMean,9:0.00}{s.GapSd,8:0.00}{s.Extreme,8:0.00}{s.SameEdu,7:0.0}{s.SameReg,7:0.0}{d[0],10:0.00}{d[1],7:0.00}{d[2],7:0.00}{d[3],10:0.00}");
    }

    static void Against(string label, Market m, double[,] tab, double[,] reference)
    {
        double[] d = m.Dissimilarity(tab, reference);
        Console.WriteLine($"{label,-80}{d[0],7:0.00}{d[1],7:0.00}{d[2],7:0.00}{d[3],10:0.00}");
    }

    static double[,] ToDouble(int[,] x)
    {
        double[,] d = new double[x.GetLength(0), x.GetLength(1)];
        for (int i = 0; i < x.GetLength(0); i++)
            for (int j = 0; j < x.GetLength(1); j++)
                d[i, j] = x[i, j];
        return d;
    }

    static double[,] PoissonDraw(double[,] mean, Random rng)
    {
        double[,] d = new double[mean.GetLength(0), mean.GetLength(1)];
        for (int i = 0; i < mean.GetLength(0); i++)
            for (int j = 0; j < mean.GetLength(1); j++)
                d[i, j] = Market.Poisson(rng, mean[i, j]);
        return d;
    }

    /// <summary>Removes k random individuals from one side of the pool (cut() in cmp_pool()).</summary>
    static void CutBack(Random rng, int[] n, int k)
    {
        var individuals = new List<int>();
        for (int x = 0; x < n.Length; x++)
            for (int i = 0; i < n[x]; i++) individuals.Add(x);
        for (int i = 0; i < k; i++)
        {
            int j = rng.Next(individuals.Count);
            n[individuals[j]]--;
            individuals[j] = individuals[individuals.Count - 1];
            individuals.RemoveAt(individuals.Count - 1);
        }
    }

    static Household NewSingle(Market m, int type, EGender gender)
    {
        var person = new Person
        {
            Age = m.Age[type],
            Gender = gender,
            HighestEducation5 = Mappings.Education5(EduCode[m.Edu[type]]),
            Origin5 = EOrigin5.DanskOprindelse,
            TestType = type
        };
        int r = m.Reg[type];
        var dwelling = new Dwelling(MunicipalityCode[r], RegionCode[r], EDwellingUse.Etagebolig, EDwellingType.UdlejningsboligPrivat);
        var household = new Household(person, dwelling);
        household.State.Add(EHouseholdState.InMatchingPool);
        return household;
    }

    static void WriteSingles(Market m, string fileName)
    {
        using var file = new StreamWriter(fileName);
        file.WriteLine("alder\tuddannelse\tregion\tenlige_maend\tenlige_kvinder\tforventet_matchede_maend\tforventet_matchede_kvinder");
        for (int x = 0; x < Market.C; x++)
        {
            double row = 0, col = 0;
            for (int y = 0; y < Market.C; y++) { row += m.Mu[x, y]; col += m.Mu[y, x]; }
            file.WriteLine($"{m.Age[x]}\t{m.Edu[x] + 1}\t{m.Reg[x] + 1}\t{m.MA[x]}\t{m.MB[x]}\t{row:0.0000}\t{col:0.0000}");
        }
    }

    /// <summary>
    /// Draws a couple table for each year (independent Poisson draws with mean mu) and writes the couples as
    /// two lines per couple in the format Matching.initializeMatching() reads. Returns the total table.
    /// "alder" (age) is written as the age at the start of the year (the age of the type minus 1), because
    /// Matching.cs adds 1 to get the age at the end of the year, which is the age the persons in the pool have.
    /// </summary>
    static int[,] WriteHistory(Market m, Random rng, string fileName)
    {
        const int C = Market.C;
        int[,] total = new int[C, C];
        long id = 0;
        using var file = new StreamWriter(fileName);
        file.WriteLine("familie_id_ultimo\tt\talder\tkoen\thf\tlandsdel_primo\tkommune_primo\toprindelse");
        for (int t = FirstYear; t < FirstYear + Years; t++)
        {
            int[,] x = m.Draw(rng);
            for (int i = 0; i < C; i++)
                for (int j = 0; j < C; j++)
                {
                    total[i, j] += x[i, j];
                    for (int n = 0; n < x[i, j]; n++)
                    {
                        id++;
                        file.WriteLine(Line(m, id, t, i, 1));
                        file.WriteLine(Line(m, id, t, j, 2));
                    }
                }
        }
        for (int i = 0; i < C; i++)
        {
            long row = 0, col = 0;
            for (int j = 0; j < C; j++) { row += total[i, j]; col += total[j, i]; }
            if (row > (long)Years * m.MA[i] || col > (long)Years * m.MB[i])
                throw new Exception("Flere par end enlige for en type; cmp_draw() i R ville have skåret ned her.");
        }
        return total;
    }

    static string Line(Market m, long id, int t, int type, int gender)
    {
        int r = m.Reg[type];
        return $"{id}\t{t}\t{m.Age[type] - 1}\t{gender}\t{(int)EduCode[m.Edu[type]]}\t{(int)RegionCode[r]}\t{MunicipalityDst[r]}\t1";
    }

    /// <summary>
    /// Recomputes the expected couple table that RASAdjust() in RASMatrix.cs gives after a number of passes
    /// (rows, then columns). Matching.cs puts men and women in the same symmetric matrix: in the block
    /// with men in the rows the men are scaled first, in the mirrored block the women are scaled first. When
    /// the matrix is folded into a triangle, the two blocks are added, and the number of couples is half the sum.
    /// </summary>
    static double[,] RasAsInMatching(double[,] seed, double[] rowTot, double[] colTot, int passes)
    {
        int n = rowTot.Length;
        double[,] upper = (double[,])seed.Clone(), lower = (double[,])seed.Clone();
        for (int p = 0; p < passes; p++)
        {
            ScaleRows(upper, rowTot); ScaleCols(upper, colTot);
            ScaleCols(lower, colTot); ScaleRows(lower, rowTot);
        }
        double[,] table = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                table[i, j] = (upper[i, j] + lower[i, j]) / 2;
        return table;
    }

    static void ScaleRows(double[,] x, double[] target)
    {
        for (int i = 0; i < x.GetLength(0); i++)
        {
            double sum = 0;
            for (int j = 0; j < x.GetLength(1); j++) sum += x[i, j];
            if (sum <= 0) continue;
            for (int j = 0; j < x.GetLength(1); j++) x[i, j] *= target[i] / sum;
        }
    }

    static void ScaleCols(double[,] x, double[] target)
    {
        for (int j = 0; j < x.GetLength(1); j++)
        {
            double sum = 0;
            for (int i = 0; i < x.GetLength(0); i++) sum += x[i, j];
            if (sum <= 0) continue;
            for (int i = 0; i < x.GetLength(0); i++) x[i, j] *= target[j] / sum;
        }
    }

    sealed class IpfResult
    {
        public double[,] Table;
        public int Iterations;
        public double Error;
    }

    /// <summary>Biproportional fitting until convergence (ipf() in matching_compare.R).</summary>
    static IpfResult Ipf(double[,] seed, double[] rowTot, double[] colTot, double tol = 1e-8, int maxit = 5000)
    {
        int n = rowTot.Length, k = colTot.Length;
        double[] r = new double[n], s = Enumerable.Repeat(1.0, k).ToArray();
        double total = Math.Max(1, rowTot.Sum()), err = double.NaN;
        int it;
        for (it = 1; it <= maxit; it++)
        {
            for (int i = 0; i < n; i++)
            {
                double rs = 0;
                for (int j = 0; j < k; j++) rs += seed[i, j] * s[j];
                r[i] = rs > 0 ? rowTot[i] / rs : 0;
            }
            for (int j = 0; j < k; j++)
            {
                double cs = 0;
                for (int i = 0; i < n; i++) cs += seed[i, j] * r[i];
                s[j] = cs > 0 ? colTot[j] / cs : 0;
            }
            err = 0;
            for (int i = 0; i < n; i++)
            {
                double rs = 0;
                for (int j = 0; j < k; j++) rs += seed[i, j] * s[j];
                err = Math.Max(err, Math.Abs(r[i] * rs - rowTot[i]));
            }
            if (err < tol * total) break;
        }
        double[,] table = new double[n, k];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < k; j++)
                table[i, j] = seed[i, j] * r[i] * s[j];
        return new IpfResult { Table = table, Iterations = Math.Min(it, maxit), Error = err };
    }
}
