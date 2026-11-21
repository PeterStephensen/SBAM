// Comparison of the new SBAM implementation (Matching_test.cs in the CodeTest project) with stochastic
// matching as used in DYNASIM, CORSIM/DYNACAN and CBOLT (StochasticMatching.cs) on an artificial marriage
// market where the true couple table is known (Market.cs).
//
// Usage:  dotnet run -c Release -- [--experiment all] [--runs 10] [--seed 1]
//   --experiment  baseline  The pool is drawn from the population that the historical data come from.
//                 change    The pool is drawn from a changed population (older, better educated, more in region 1).
//                 size      Pools of 0.1, 0.3 and 3 times the size of the baseline pool.
//                 history   Fewer historical data: one year, and a 10 per cent sample of one year.
//                 all       All four (default).
//   --runs        Number of pools per setting (seed, seed+1, ...). The tables show averages over the runs.
//   --seed        First seed for the pools and the matching. The historical data have a fixed seed.
//   --methods     Comma-separated keys of the methods to run, e.g. SBAM,CBO-L,COR-O (default: all).
//
// All methods get the same historical data and the same pools. The results are written to the folder output:
// results_<experiment>.txt (tables), results_<experiment>.tsv (the same numbers, tab separated) and
// agegap_<experiment>.tsv (distribution of the age gap).

using System.Diagnostics;
using System.Globalization;
using DreamSMILE.DemoSim;
using DreamSMILE.MicroSim;
using DreamSMILE.Numerical;

namespace CompareStocastic;

/// <summary>
/// The new SBAM implementation: Matching_test with all its changes switched on. The RAS adjustment runs until
/// the row and column sums match, the table sums to the number of couples, the couples are formed in one pass
/// with systematic selection across the sorted cells, up to four mop-up rounds are run on the persons who are
/// left, and the RAS adjustment is computed with one factor per type on the types present in the pool.
/// </summary>
sealed class Sbam : Matching_test
{
    public Sbam(string historyFile, string id) : base(Setup(historyFile, id))
    {
        ResetToData = true;
        RasToConvergence = true;
        TargetAsCouples = true;
        PairingMethod = EPairingMethod.SystematicHierarchical;
        MaxRounds = 5;
        RasMethod = ERasMethod.Symmetric;
        RasActiveOnly = true;
    }

    // Runs before the constructor of Matching_test, which reads the historical data named in MSSettings
    static DwellingCharacteristics Setup(string historyFile, string id)
    {
        MSSettings.InputDataMatching = historyFile;
        MSSettings.ProjectionID = id;
        return new DwellingCharacteristics();
    }

    public void Flush() { _testFile.Flush(); }
}

/// <summary>Historical data: the couples formed over a number of years, as a couple table and as a file in the format the SBAM code reads.</summary>
sealed class History
{
    public string Label, File;
    public int[,] Table;
    public long Couples, Cells;
}

/// <summary>One matching method and the sums of its results over the runs.</summary>
sealed class Method
{
    public string Key, Name;
    public Action<int> Run;            // forms couples from the pool in the simulation; the argument is the seed
    public Func<int> Forced;           // couples formed with index 0 in the latest call (null: not relevant)
    public Func<long> Lookups;         // number of index look-ups in the latest call (null: not relevant)
    public string SameAs;              // key of a method that must form exactly the same couples
    public bool Slow;                  // by-person implementation: run in fewer pools and not warmed up
    public bool Reference;             // a table that is computed, not a matching method

    public int Runs, SameCouples;
    public List<double> Ms = new List<double>();
    public double Couples, Target, GapMean, GapSd, GapSdYoung, GapSdOld, Extreme, SameEdu, SameReg, WifeHigher, ForcedSum, LookupSum;
    public double[] D = new double[4];
    public double[,] SumTable = new double[Market.C, Market.C];

    // Further figures from the latest call that are averaged over the pools (e.g. the time used on the RAS adjustment)
    public string[] DetailNames = new string[0];
    public Func<double[]> Details;
    public double[] DetailSum = new double[0];

    public void Clear()
    {
        Runs = 0; SameCouples = 0; Ms.Clear();
        Couples = Target = GapMean = GapSd = GapSdYoung = GapSdOld = Extreme = SameEdu = SameReg = WifeHigher = ForcedSum = LookupSum = 0;
        Array.Clear(D); Array.Clear(SumTable);
        DetailSum = new double[DetailNames.Length];
    }

    /// <summary>The averages of the further figures as "name=value;name=value".</summary>
    public string Extra
    {
        get { return string.Join(";", DetailNames.Select((name, i) => name + "=" + (DetailSum[i] / Math.Max(1, Runs)).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))); }
    }
}

static class Program
{
    const int FirstYear = 2008, Years = 5;      // Matching_test selects historical data for 2008-2012
    const int DataSeed = 20261121;
    const int C = Market.C;

    // The market's 3 education levels and 4 regions expressed in the model's coding
    static readonly EEducationDS8[] EduCode = { EEducationDS8.Grundskole, EEducationDS8.ErhvervsfagligeHovedforlob, EEducationDS8.UdelteKandidat };
    static readonly EDwellingRegion[] RegionCode = { EDwellingRegion.KoebenhavnBy, EDwellingRegion.KoebenhavnOmegn, EDwellingRegion.Fyn, EDwellingRegion.JyllandOest };
    static readonly EDwellingMunicipality[] MunicipalityCode = { EDwellingMunicipality.København, EDwellingMunicipality.Gentofte, EDwellingMunicipality.Odense, EDwellingMunicipality.Aarhus };
    static readonly int[] MunicipalityDst = { 101, 157, 461, 751 };

    static Simulation _sim;
    static string _outDir, _dataDir;
    static StreamWriter _txt, _tsv, _gap;
    static string[] _only;                      // --methods: keys of the methods to run (null: all)
    static string _pinned;                      // description of the processor core the process is pinned to

    /// <summary>
    /// Times a fixed computation on each logical processor (the best of three attempts) and pins the process to
    /// the fastest one. Returns a description for the printout.
    /// </summary>
    static string PinToFastestCore()
    {
        try
        {
            if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux()) return "The process is not pinned to a processor core.";
            Process proc = Process.GetCurrentProcess();
            int n = Math.Min(Environment.ProcessorCount, 62);
            double[] ms = Enumerable.Repeat(double.MaxValue, n).ToArray();
            for (int attempt = 0; attempt < 3; attempt++)
                for (int cpu = 0; cpu < n; cpu++)
                {
                    proc.ProcessorAffinity = (IntPtr)(1L << cpu);
                    Thread.Sleep(1);                       // lets the thread move to the processor
                    ms[cpu] = Math.Min(ms[cpu], FixedComputationMs());
                }
            int best = Array.IndexOf(ms, ms.Min());
            proc.ProcessorAffinity = (IntPtr)(1L << best);
            return $"The process is pinned to logical processor {best}, the fastest of {n} in a calibration. A fixed computation took, in ms per logical processor: "
                   + string.Join(" ", ms.Select(v => v.ToString("0"))) + ".";
        }
        catch (Exception e)
        {
            return "The process could not be pinned to a processor core: " + e.Message;
        }
    }

    static double _sink;
    static readonly int[] _memory = new int[1 << 23];      // 32 MB, more than the processor's cache

    /// <summary>
    /// The time in milliseconds of a fixed computation: arithmetic and reads at scattered places in a large
    /// array, since the matching methods depend on both the speed of the processor and of the memory. It is
    /// used to find the fastest processor core and to show whether the speed of the machine changed while the
    /// methods were timed.
    /// </summary>
    static double FixedComputationMs()
    {
        var timer = Stopwatch.StartNew();
        double sink = 0;
        uint j = 12345;
        for (int i = 1; i <= 3000000; i++)
        {
            j = j * 1664525 + 1013904223;                  // pseudo-random position
            sink += Math.Sqrt(i) / (i + sink % 2) + _memory[j >> 9];
        }
        _sink = sink;                                      // keeps the compiler from removing the loop
        return timer.Elapsed.TotalMilliseconds;
    }

    static void Print(string s = "") { Console.WriteLine(s); _txt?.WriteLine(s); }

    static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;   // decimal point in all printouts

        // The test machine has processor cores of different speed. The process gets high priority and is pinned
        // to the fastest core, so that the operating system does not move it to a slower one while the methods are timed.
        try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High; } catch { }
        _pinned = PinToFastestCore();

        string experiment = "all";
        int runs = 10, seed = 1;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--experiment") experiment = args[++i];
            else if (args[i] == "--runs") runs = int.Parse(args[++i]);
            else if (args[i] == "--seed") seed = int.Parse(args[++i]);
            else if (args[i] == "--methods") _only = args[++i].Split(',');
            else throw new ArgumentException("Unknown argument: " + args[i]);
        }
        string[] experiments = experiment == "all" ? new[] { "baseline", "change", "size", "history" } : new[] { experiment };

        string root = ProjectDir();
        _dataDir = Path.Combine(root, "data");
        _outDir = Path.Combine(root, "output");
        Directory.CreateDirectory(_dataDir);
        Directory.CreateDirectory(_outDir);

        // Set-up as in the model
        MSSettings.OutputDir = _outDir;
        MSSettings.ProjectionID = "compare";
        MSSettings.MatchingMinAge = 15;
        MSSettings.MatchingMaxAge = 65;
        MSSettings.UseMunicipalityDependentSBAM = false;
        MSSettings.SBAMMunicipalityGrassSow = false;
        MSSettings.MatchingScaleUp = Enumerable.Repeat(1.0, MSSettings.MUNICIPALITY).ToArray();   // the whole pool is matched
        _sim = Simulation.Create(FirstYear + Years, seed);

        // The market and the historical data
        var sw = Stopwatch.StartNew();
        var market = new Market();
        var history = MakeHistory(market, "5 years", "history_5y.txt", Years, 1.0);
        var logit = Compatibility.Logit(market, history.Table);
        var logitX = Compatibility.Logit(market, history.Table, extended: true);
        Sbam sbam = NewSbam(history, "5y");

        // Translation from the model's codes to the education and region of the market, for the stochastic matching
        int[] eduIndex = Enumerable.Repeat(-1, 16).ToArray(), regIndex = Enumerable.Repeat(-1, MSSettings.REGION).ToArray();
        for (int e = 0; e < Market.NEdu; e++) eduIndex[Mappings.Education5(EduCode[e])] = e;
        for (int r = 0; r < Market.NReg; r++) regIndex[(int)RegionCode[r]] = r;
        if (eduIndex.Count(v => v >= 0) != Market.NEdu || regIndex.Count(v => v >= 0) != Market.NReg)
            throw new Exception("The education or region codes of the market are not distinct in the model's coding");
        var sm = new StochasticMatching(eduIndex, regIndex);

        float[] idxD = Compatibility.Distance(market), idxO = Compatibility.Oracle(market);
        float[] idxL = logit.Index, idxX = logitX.Index, idxT = Compatibility.Table(history.Table);
        double setupSeconds = sw.Elapsed.TotalSeconds;

        bool ok = true;
        foreach (string exp in experiments)
        {
            _txt = new StreamWriter(Path.Combine(_outDir, $"results_{exp}.txt"));
            _tsv = new StreamWriter(Path.Combine(_outDir, $"results_{exp}.tsv"));
            _gap = new StreamWriter(Path.Combine(_outDir, $"agegap_{exp}.tsv"));
            _tsv.WriteLine("experiment\tsetting\tkey\tname\truns\tcouples\ttarget\tgap_mean\tgap_sd\tgap_sd_young\tgap_sd_old\textreme\tsame_edu\tsame_reg\twoman_higher"
                           + "\td_age\td_edu\td_reg\td_joint\td_age_pooled\td_edu_pooled\td_reg_pooled\td_joint_pooled\tforced\tms_median\tms_min\tms_max\tlookups_per_couple\textra");

            Print("Comparison of the new SBAM implementation with stochastic matching");
            Print($"Experiment: {exp}. Pools per setting: {runs} (seed {seed}-{seed + runs - 1}).");
            Print($"Machine: {Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")}, {Environment.ProcessorCount} logical processors, "
                  + $"{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}. All methods run on one thread. Process priority: {Process.GetCurrentProcess().PriorityClass}.");
            Print(_pinned);
            Print();
            PrintMarket(market, history, logit, logitX, setupSeconds);

            if (exp == "baseline")
            {
                var methods = Methods(sbam, sm, idxD, idxL, idxX, idxT, idxO, personAll: true, personLogit: false, diagnostic: true);
                ok &= RunSetting(exp, "base population, pool x 1, 5 years of history", market, 1.0, runs, runs, seed, methods);
            }
            else if (exp == "change")
            {
                foreach (double change in new[] { 0.5, 1.0 })
                {
                    var changed = new Market(change, market.Kappa);
                    PrintPopulation(market, changed);
                    var methods = Methods(sbam, sm, idxD, idxL, idxX, idxT, idxO, personAll: false, personLogit: false);
                    ok &= RunSetting(exp, $"changed population (change = {change}), pool x 1, 5 years of history", changed, 1.0, runs, 0, seed, methods);
                }
            }
            else if (exp == "size")
            {
                foreach (double scale in new[] { 0.1, 0.3, 3.0 })
                {
                    var methods = Methods(sbam, sm, idxD, idxL, idxX, idxT, idxO, personAll: false, personLogit: true);
                    ok &= RunSetting(exp, $"base population, pool x {scale}, 5 years of history", market, scale, runs, Math.Min(runs, 3), seed, methods);
                }
            }
            else if (exp == "history")
            {
                var histories = new[]
                {
                    MakeHistory(market, "1 year", "history_1y.txt", 1, 1.0),
                    MakeHistory(market, "10 per cent sample of 1 year", "history_1y_10pct.txt", 1, 0.1)
                };
                int id = 0;
                foreach (History h in histories)
                {
                    var lg = Compatibility.Logit(market, h.Table);
                    var lgX = Compatibility.Logit(market, h.Table, extended: true);
                    Print($"Historical data: {h.Label}: {h.Couples} couples in {h.Cells} cells.");
                    PrintLogit(lg, "basic specification");
                    PrintLogit(lgX, "extended specification");
                    Sbam s = NewSbam(h, "h" + (++id));
                    var methods = Methods(s, sm, idxD, lg.Index, lgX.Index, Compatibility.Table(h.Table), idxO, personAll: false, personLogit: false);
                    ok &= RunSetting(exp, $"base population, pool x 1, history: {h.Label}", market, 1.0, runs, 0, seed, methods);
                    s.Flush();
                }
            }
            else throw new ArgumentException("Unknown experiment: " + exp);

            _txt.Close(); _tsv.Close(); _gap.Close();
            _txt = null;
        }
        sbam.Flush();
        return ok ? 0 : 1;
    }

    // -------------------------------------------------------------------------------------------
    // The methods
    // -------------------------------------------------------------------------------------------

    static List<Method> Methods(Sbam sbam, StochasticMatching sm, float[] idxD, float[] idxL, float[] idxX, float[] idxT, float[] idxO,
                                bool personAll, bool personLogit, bool diagnostic = false)
    {
        var list = new List<Method>();

        var m0 = new Method { Key = "SBAM", Name = "SBAM, new implementation" };
        m0.Run = seed =>
        {
            Simulation.Random = new RandomEvent(seed);
            TextWriter console = Console.Out;
            Console.SetOut(TextWriter.Null);           // the SBAM code writes status lines to the console
            try { sbam.matching(); }
            finally { Console.SetOut(console); }
        };
        m0.DetailNames = new[] { "ras_ms", "pairing_ms", "ras_passes", "rounds", "ras_converged", "persons_without_data", "couples_missing" };
        m0.Details = () => new double[] { sbam.LastMsRas, sbam.LastMsPairing, sbam.LastRasPasses, sbam.LastRounds, sbam.LastRasConverged ? 1 : 0,
                                          sbam.LastPersonsWithoutData, sbam.LastTargetCouples - sbam.LastCouples };
        list.Add(m0);

        Func<string, string, float[], Action<StochasticMatching>, Method> stochastic = (key, name, index, call) =>
        {
            double scale = 1.0 / Compatibility.Max(index);   // the DYNASIM rule: the most compatible types are accepted with certainty
            return new Method
            {
                Key = key, Name = name,
                Run = seed =>
                {
                    sm.Index = index;
                    sm.Scale = scale;
                    sm.Rng = new Random(seed);
                    call(sm);
                },
                Forced = () => sm.LastForced,
                Lookups = () => sm.LastEvaluations,
                DetailNames = new[] { "prepare_ms" },
                Details = () => new double[] { sm.LastMsPrepare }
            };
        };

        var indices = new (string Key, string Name, float[] Index)[]
        {
            ("L", "logit index", idxL), ("X", "extended logit index", idxX), ("T", "table index", idxT), ("O", "oracle index", idxO)
        };

        list.Add(stochastic("DYN-D", "DYNASIM rule, distance function", idxD, s => s.Dynasim(_sim.Households)));
        foreach (var ix in indices)
            list.Add(stochastic("DYN-" + ix.Key, "DYNASIM rule, " + ix.Name, ix.Index, s => s.Dynasim(_sim.Households)));
        foreach (var ix in indices)
            list.Add(stochastic("CBO-" + ix.Key, "CBOLT rule, " + ix.Name, ix.Index, s => s.Cbolt(_sim.Households, true)));
        foreach (var ix in indices)
            list.Add(stochastic("COR-" + ix.Key, "CORSIM rule, " + ix.Name, ix.Index, s => s.Corsim(_sim.Households, true)));

        // The same rules computed person by person (not with the extended logit index: the time does not depend on the index)
        foreach (var ix in indices)
        {
            if (ix.Key == "X" || (!personAll && !(personLogit && ix.Key == "L"))) continue;
            var p1 = stochastic("CBO-" + ix.Key + "-P", "CBOLT rule, " + ix.Name + ", by person", ix.Index, s => s.Cbolt(_sim.Households, false));
            p1.Slow = true; p1.SameAs = "CBO-" + ix.Key;
            list.Add(p1);
        }
        foreach (var ix in indices)
        {
            if (ix.Key == "X" || (!personAll && !(personLogit && ix.Key == "L"))) continue;
            var p2 = stochastic("COR-" + ix.Key + "-P", "CORSIM rule, " + ix.Name + ", by person", ix.Index, s => s.Corsim(_sim.Households, false));
            p2.Slow = true;
            list.Add(p2);
        }

        // Diagnostic variant of the CBOLT rule (not part of the documented rule): the second pass starts at a random woman
        if (diagnostic)
            list.Add(stochastic("CBO-O-R", "CBOLT rule, oracle index, random start", idxO, s => s.Cbolt(_sim.Households, true, true)));

        // --methods: only the named methods (and those they are compared with) are run. SBAM is always run
        if (_only != null)
            list = list.Where(m => m.Key == "SBAM" || _only.Contains(m.Key) || list.Any(o => _only.Contains(o.Key) && o.SameAs == m.Key)).ToList();
        return list;
    }

    static Sbam NewSbam(History h, string id)
    {
        TextWriter console = Console.Out;
        Console.SetOut(TextWriter.Null);
        try { return new Sbam(h.File, id); }
        finally { Console.SetOut(console); }
    }

    // -------------------------------------------------------------------------------------------
    // One setting: a population for the pool, a pool size and a set of methods
    // -------------------------------------------------------------------------------------------

    /// <param name="poolMarket">The market the pool is drawn from. Its couple table is the correct answer.</param>
    /// <param name="slowRuns">Number of pools in which the by-person implementations are run.</param>
    static bool RunSetting(string experiment, string setting, Market poolMarket, double poolScale, int runs, int slowRuns, int seed, List<Method> methods)
    {
        foreach (Method m in methods) m.Clear();
        var truth = new Method { Key = "TRUE", Name = "True model", Reference = true };
        var draw = new Method { Key = "DRAW", Name = "True model, one draw", Reference = true };
        var random = new Method { Key = "RANDOM", Name = "Random matching, expected", Reference = true };
        bool checksOk = true;
        double totalPool = 0;

        // Warm-up on a pool that is not used in the results, so that no method is timed while it is being compiled
        MakePool(poolMarket, poolScale, seed - 1, out _, out _);
        foreach (Method m in methods.Where(m => !m.Slow))
        {
            _sim.Statistics.MatchedPairs.Clear();
            m.Run(seed - 1);
        }

        var speedCheck = new List<double> { FixedComputationMs() };   // the speed of the machine before, during and after the timing
        for (int k = 0; k < runs; k++)
        {
            int s = seed + k;
            MakePool(poolMarket, poolScale, s, out int[] nA, out int[] nB);
            int target = nA.Sum();                      // equally many men and women: everybody can be matched
            totalPool += 2.0 * target;

            // Reference tables: the true table scaled to the number of couples, one random draw from it, and random matching
            double f = target / poolMarket.Couples;
            double[,] expected = new double[C, C], oneDraw = new double[C, C], rnd = new double[C, C];
            var drawRng = new Random(s + 2000);
            for (int x = 0; x < C; x++)
                for (int y = 0; y < C; y++)
                {
                    expected[x, y] = f * poolMarket.Mu[x, y];
                    oneDraw[x, y] = Market.Poisson(drawRng, expected[x, y]);
                    rnd[x, y] = nA[x] * (double)nB[y] / target;
                }
            Accumulate(truth, poolMarket, expected, target);
            Accumulate(draw, poolMarket, oneDraw, target);
            Accumulate(random, poolMarket, rnd, target);

            foreach (Method m in methods.Where(m => !m.Slow))
                checksOk &= RunMethod(m, s, target, poolMarket);
            speedCheck.Add(FixedComputationMs());
        }

        // The by-person implementations are run afterwards, on the same pools (a pool is fully determined by its
        // seed). They take seconds per call, and running them last keeps their load on the processor from
        // disturbing the timing of the fast methods.
        for (int k = 0; k < slowRuns; k++)
        {
            int s = seed + k;
            MakePool(poolMarket, poolScale, s, out int[] nA, out _);
            int target = nA.Sum();
            foreach (Method m in methods.Where(m => m.Slow))
            {
                // The couples of the by-type implementation of the same rule, for comparison
                List<(Household, Household)> reference = null;
                if (m.SameAs != null)
                {
                    _sim.Statistics.MatchedPairs.Clear();
                    methods.First(o => o.Key == m.SameAs).Run(s);
                    reference = _sim.Statistics.MatchedPairs.ToList();
                }
                checksOk &= RunMethod(m, s, target, poolMarket);
                if (reference != null && reference.SequenceEqual(_sim.Statistics.MatchedPairs)) m.SameCouples++;
            }
        }

        // ---------------------------------------------------------------------------------------
        // Printout
        // ---------------------------------------------------------------------------------------
        var rows = new[] { truth, draw, random }.Concat(methods).Where(m => m.Runs > 0).ToList();
        Print($"=== {setting} ===");
        Print($"Pool: {totalPool / runs:0} persons on average ({totalPool / runs / 2:0} men and as many women). Averages over {runs} pools"
              + (methods.Any(m => m.Slow) ? $"; the by-person implementations are run in {slowRuns} pools." : "."));
        Print();
        Print("Number of couples and their composition. The age gap is the man's age minus the woman's.");
        Print("                                            Couples  Pct. of     Age      SD   SD men   SD men  Extreme    Same    Same   Woman");
        Print("                                                     possible    gap            20-29    45-54     pct.    edu.    reg.  higher");
        foreach (Method m in rows)
        {
            double n = m.Runs;
            Print($"{m.Name,-42}{m.Couples / n,9:0.0}{100 * m.Couples / m.Target,9:0.00}{m.GapMean / n,8:0.00}{m.GapSd / n,8:0.00}{m.GapSdYoung / n,9:0.00}{m.GapSdOld / n,9:0.00}"
                  + $"{m.Extreme / n,9:0.00}{m.SameEdu / n,8:0.0}{m.SameReg / n,8:0.0}{m.WifeHigher / n,8:0.0}");
        }
        Print();
        Print("Dissimilarity index D against the true model, in per cent of the couples, and running time.");
        Print("\"Single pools\" is the average of D in the individual pools. \"All pools\" is D for the couples of all pools added together.");
        Print("Forced couples have compatibility index 0. Time is the time of one call in milliseconds: the fastest pool and the median over the pools.");
        Print("                                            ------- single pools -------   ------- all pools ---------   Forced  --- Time, ms ---   Look-ups   Pools");
        Print("                                               Age     Edu     Reg   Joint     Age     Edu     Reg   Joint  couples   Fastest  Median  per couple");
        foreach (Method m in rows)
        {
            double n = m.Runs;
            double[] pooled = poolMarket.Dissimilarity(m.SumTable, poolMarket.Mu);
            string line = $"{m.Name,-42}{m.D[0] / n,8:0.00}{m.D[1] / n,8:0.00}{m.D[2] / n,8:0.00}{m.D[3] / n,8:0.00}{pooled[0],8:0.00}{pooled[1],8:0.00}{pooled[2],8:0.00}{pooled[3],8:0.00}";
            if (!m.Reference)
                line += $"{(m.Forced != null ? (m.ForcedSum / n).ToString("0.0") : ""),9}{m.Ms.Min(),10:0.0}{Median(m.Ms),8:0.0}{(m.Lookups != null ? (m.LookupSum / m.Couples).ToString("0.0") : ""),11}{m.Runs,8}";
            Print(line);

            _tsv.WriteLine(string.Join("\t", experiment, setting, m.Key, m.Name, m.Runs,
                F(m.Couples / n), F(m.Target / n), F(m.GapMean / n), F(m.GapSd / n), F(m.GapSdYoung / n), F(m.GapSdOld / n), F(m.Extreme / n), F(m.SameEdu / n), F(m.SameReg / n), F(m.WifeHigher / n),
                F(m.D[0] / n), F(m.D[1] / n), F(m.D[2] / n), F(m.D[3] / n), F(pooled[0]), F(pooled[1]), F(pooled[2]), F(pooled[3]),
                m.Forced != null ? F(m.ForcedSum / n) : "", m.Ms.Count > 0 ? F(Median(m.Ms)) : "", m.Ms.Count > 0 ? F(m.Ms.Min()) : "", m.Ms.Count > 0 ? F(m.Ms.Max()) : "",
                m.Lookups != null ? F(m.LookupSum / m.Couples) : "", m.Extra));
        }
        Print();
        Method sbam = methods.First(m => m.Key == "SBAM");
        Print("SBAM, averages over the pools: " + sbam.Extra.Replace(";", ", ").Replace("=", " = "));
        Print($"Speed of the machine while the fast methods were timed: a fixed computation took {speedCheck.Min():0.0}-{speedCheck.Max():0.0} ms (median {Median(speedCheck):0.0} ms), measured before the first pool and after each pool.");

        bool ok = Check("No household in two couples, no couples of the same gender, the stochastic rules match the whole pool", checksOk);
        foreach (Method m in methods.Where(m => m.SameAs != null && m.Runs > 0))
            ok &= Check($"{m.Name} forms exactly the same couples as by type in {m.SameCouples} of {m.Runs} pools", m.SameCouples == m.Runs);
        Print();

        // Distribution of the age gap, all pools added together
        _gap.WriteLine("setting\tgap\t" + string.Join("\t", rows.Where(m => !m.Slow).Select(m => m.Key)));
        var dists = rows.Where(m => !m.Slow).Select(m => poolMarket.GapDistribution(m.SumTable)).ToList();
        for (int g = -15; g <= 20; g++)
            _gap.WriteLine(setting + "\t" + g + "\t" + string.Join("\t", dists.Select(d => F(d[g + Market.NAge - 1]))));
        _txt.Flush(); _tsv.Flush(); _gap.Flush();
        return ok;
    }

    /// <summary>
    /// Runs one method on the pool in the simulation, times it and adds its results to the sums of the method.
    /// Returns false if the couples fail a check.
    /// </summary>
    static bool RunMethod(Method m, int seed, int target, Market poolMarket)
    {
        bool ok = true;
        _sim.Statistics.MatchedPairs.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // The call is timed. The fast methods are called three times with the same seed (which gives the
        // same couples), and the median of the three times is used, so that a disturbance from the
        // operating system or the garbage collector during one call does not count.
        var times = new List<double>();
        for (int rep = 0; rep < (m.Slow ? 1 : 3); rep++)
        {
            _sim.Statistics.MatchedPairs.Clear();
            var timer = Stopwatch.StartNew();
            m.Run(seed);
            times.Add(timer.Elapsed.TotalMilliseconds);
        }
        m.Ms.Add(Median(times));

        // Couple table and checks
        double[,] formed = new double[C, C];
        var seen = new HashSet<Household>();
        foreach (var (h1, h2) in _sim.Statistics.MatchedPairs)
        {
            if (!seen.Add(h1) || !seen.Add(h2)) ok = false;                            // a household in two couples
            Person p1 = h1.FirstAdult, p2 = h2.FirstAdult;
            if (p1.Gender == p2.Gender) { ok = false; continue; }                      // the data have no couples of the same gender
            Person man = p1.Gender == EGender.Male ? p1 : p2, woman = p1.Gender == EGender.Male ? p2 : p1;
            formed[man.TestType, woman.TestType]++;
        }
        int pairs = _sim.Statistics.MatchedPairs.Count;
        if (pairs > target) ok = false;
        if (m.Key != "SBAM" && pairs != target) ok = false;                           // the stochastic rules match everybody

        Accumulate(m, poolMarket, formed, target);
        if (m.Forced != null) m.ForcedSum += m.Forced();
        if (m.Lookups != null) m.LookupSum += m.Lookups();
        if (m.Details != null)
        {
            double[] d = m.Details();
            for (int i = 0; i < d.Length; i++) m.DetailSum[i] += d[i];
        }
        return ok;
    }

    static string F(double v) { return v.ToString("0.####", CultureInfo.InvariantCulture); }

    /// <summary>Adds the measures of one couple table to the sums of the method.</summary>
    static void Accumulate(Method m, Market market, double[,] table, int target)
    {
        var s = market.Describe(table);
        m.Runs++;
        m.Couples += s.Couples; m.Target += target;
        m.GapMean += s.GapMean; m.GapSd += s.GapSd; m.GapSdYoung += s.GapSdYoung; m.GapSdOld += s.GapSdOld;
        m.Extreme += s.Extreme; m.SameEdu += s.SameEdu; m.SameReg += s.SameReg; m.WifeHigher += s.WifeHigher;
        double[] d = market.Dissimilarity(table, market.Mu);
        for (int i = 0; i < 4; i++) m.D[i] += d[i];
        for (int x = 0; x < C; x++)
            for (int y = 0; y < C; y++)
                m.SumTable[x, y] += table[x, y];
    }

    static double Median(List<double> x)
    {
        if (x.Count == 0) return double.NaN;
        var s = x.OrderBy(v => v).ToList();
        return s.Count % 2 == 1 ? s[s.Count / 2] : (s[s.Count / 2 - 1] + s[s.Count / 2]) / 2;
    }

    static bool Check(string text, bool ok)
    {
        Print($"[{(ok ? "OK   " : "ERROR")}] {text}");
        return ok;
    }

    // -------------------------------------------------------------------------------------------
    // Pool and historical data
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// Draws a pool and puts it into the simulation as single households. Each single enters the pool with the
    /// probability that his or her type forms a couple in a year in the true model, times poolScale. The larger
    /// side is cut back at random, so that the pool holds equally many men and women.
    /// </summary>
    static void MakePool(Market m, double poolScale, int seed, out int[] nA, out int[] nB)
    {
        var rng = new Random(seed + 1000);
        nA = new int[C]; nB = new int[C];
        for (int x = 0; x < C; x++)
        {
            nA[x] = Market.Binomial(rng, m.MA[x], Math.Min(1, poolScale * m.RowSum[x] / m.MA[x]));
            nB[x] = Market.Binomial(rng, m.MB[x], Math.Min(1, poolScale * m.ColSum[x] / m.MB[x]));
        }
        int surplus = nA.Sum() - nB.Sum();
        if (surplus > 0) CutBack(rng, nA, surplus);
        if (surplus < 0) CutBack(rng, nB, -surplus);

        _sim.Households.Clear();
        for (int x = 0; x < C; x++)
        {
            for (int i = 0; i < nA[x]; i++) _sim.Households.Add(NewSingle(m, x, EGender.Male));
            for (int i = 0; i < nB[x]; i++) _sim.Households.Add(NewSingle(m, x, EGender.Female));
        }
    }

    /// <summary>Removes k random individuals from one side of the pool.</summary>
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
            TestType = type                    // used only to measure the result, not by the matching methods
        };
        int r = m.Reg[type];
        var dwelling = new Dwelling(MunicipalityCode[r], RegionCode[r], EDwellingUse.Etagebolig, EDwellingType.UdlejningsboligPrivat);
        var household = new Household(person, dwelling);
        household.State.Add(EHouseholdState.InMatchingPool);
        return household;
    }

    /// <summary>
    /// Draws historical data from the base market: for each year a couple table with independent Poisson draws
    /// around the true table, of which each couple is kept with probability "sample". The couples are written
    /// as two lines per couple in the format the SBAM code reads. "alder" (age) is written as the age at the
    /// start of the year, because the SBAM code adds 1 to get the age at the end of the year.
    /// </summary>
    static History MakeHistory(Market m, string label, string fileName, int years, double sample)
    {
        var rng = new Random(DataSeed);
        var h = new History { Label = label, File = Path.Combine(_dataDir, fileName), Table = new int[C, C] };
        long id = 0;
        using (var file = new StreamWriter(h.File))
        {
            file.WriteLine("familie_id_ultimo\tt\talder\tkoen\thf\tlandsdel_primo\tkommune_primo\toprindelse");
            for (int t = FirstYear; t < FirstYear + years; t++)
            {
                int[,] x = m.Draw(rng);
                for (int i = 0; i < C; i++)
                    for (int j = 0; j < C; j++)
                    {
                        int n = sample < 1 ? Market.Binomial(rng, x[i, j], sample) : x[i, j];
                        h.Table[i, j] += n;
                        for (int c = 0; c < n; c++)
                        {
                            id++;
                            file.WriteLine(Line(m, id, t, i, 1));
                            file.WriteLine(Line(m, id, t, j, 2));
                        }
                    }
            }
        }
        foreach (int v in h.Table) { h.Couples += v; if (v > 0) h.Cells++; }
        return h;
    }

    static string Line(Market m, long id, int t, int type, int gender)
    {
        int r = m.Reg[type];
        return $"{id}\t{t}\t{m.Age[type] - 1}\t{gender}\t{(int)EduCode[m.Edu[type]]}\t{(int)RegionCode[r]}\t{MunicipalityDst[r]}\t1";
    }

    // -------------------------------------------------------------------------------------------
    // Description of the market
    // -------------------------------------------------------------------------------------------

    static void PrintMarket(Market m, History h, Compatibility.LogitResult logit, Compatibility.LogitResult logitX, double seconds)
    {
        var t = m.Describe(m.Mu);
        Print("The artificial market (base population)");
        Print($"  Types per gender: {C}, cells in the couple table: {C * C}");
        Print($"  Single men: {m.MA.Sum()}, single women: {m.MB.Sum()}, couples per year: {m.Couples:0.0}");
        Print($"  kappa = {m.Kappa:0.000000}, equilibrium error = {m.EquilibriumError:0.0e+0}");
        Print($"  Age gap: mean {t.GapMean:0.00}, SD {t.GapSd:0.00} (men 20-29: {t.GapSdYoung:0.00}, men 45-54: {t.GapSdOld:0.00}), extreme couples {t.Extreme:0.00} pct.");
        Print($"  Same education {t.SameEdu:0.0} pct., same region {t.SameReg:0.0} pct., woman has the higher education {t.WifeHigher:0.0} pct.");
        Print($"Historical data: {h.Label}: {h.Couples} couples in {h.Cells} cells.");
        int without = 0;
        for (int x = 0; x < C; x++)
        {
            long row = 0, col = 0;
            for (int y = 0; y < C; y++) { row += h.Table[x, y]; col += h.Table[y, x]; }
            if (row == 0) without++;
            if (col == 0) without++;
        }
        Print($"  Types (of 840) without historical couples: {without}");
        PrintLogit(logit, "basic specification");
        PrintLogit(logitX, "extended specification");
        Print($"Set-up time (market, historical data, logit, reading of the data into the SBAM code): {seconds:0.0} s");
        Print();
    }

    static void PrintLogit(Compatibility.LogitResult logit, string specification)
    {
        Print($"Logit on potential pairs, {specification}: {logit.Couples:0} couples among {logit.PotentialPairs:0} potential pairs, {logit.Iterations} iterations, log-likelihood {logit.LogLikelihood:0.0}");
        for (int i = 0; i < logit.Beta.Length; i++)
            Print($"  {logit.Names[i],-44}{logit.Beta[i],10:0.0000}");
    }

    static void PrintPopulation(Market b, Market c)
    {
        Func<Market, bool, double[]> shares = (m, female) =>
        {
            int[] pop = female ? m.MB : m.MA;
            double n = pop.Sum(), age = 0, hi = 0, lo = 0, r1 = 0;
            for (int x = 0; x < C; x++)
            {
                age += pop[x] * m.Age[x];
                if (m.Edu[x] == 2) hi += pop[x];
                if (m.Edu[x] == 0) lo += pop[x];
                if (m.Reg[x] == 0) r1 += pop[x];
            }
            return new[] { age / n, 100 * lo / n, 100 * hi / n, 100 * r1 / n };
        };
        var tb = b.Describe(b.Mu);
        var tc = c.Describe(c.Mu);
        Print($"Changed population (change = {c.Change}) against the base population");
        Print("                                         Base   Changed");
        string[] names = { "mean age", "lowest education, pct.", "highest education, pct.", "region 1, pct." };
        foreach (bool female in new[] { false, true })
        {
            double[] sb = shares(b, female), sc = shares(c, female);
            for (int i = 0; i < 4; i++)
                Print($"  {(female ? "Single women" : "Single men")}, {names[i],-24}{sb[i],8:0.0}{sc[i],10:0.0}");
        }
        Print($"  Couples per year                      {b.Couples,8:0}{c.Couples,10:0}");
        Print($"  Age gap, mean                         {tb.GapMean,8:0.00}{tc.GapMean,10:0.00}");
        Print($"  Age gap, SD                           {tb.GapSd,8:0.00}{tc.GapSd,10:0.00}");
        Print($"  Same education, pct.                  {tb.SameEdu,8:0.0}{tc.SameEdu,10:0.0}");
        Print($"  Same region, pct.                     {tb.SameReg,8:0.0}{tc.SameReg,10:0.0}");
        Print($"  Woman has the higher education, pct.  {tb.WifeHigher,8:0.0}{tc.WifeHigher,10:0.0}");
        Print($"  Extreme couples, pct.                 {tb.Extreme,8:0.00}{tc.Extreme,10:0.00}");
        Print($"  Equilibrium error of the changed market: {c.EquilibriumError:0.0e+0}");
        Print();
    }

    static string ProjectDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CompareStocastic.csproj"))) dir = dir.Parent;
        return dir != null ? dir.FullName : Directory.GetCurrentDirectory();
    }
}
