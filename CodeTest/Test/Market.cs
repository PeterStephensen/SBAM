// The artificial marriage market from LatentGroups_paper02.pdf, Section 4.1.
// Port of cmp_types(), cmp_log_kernel(), cmp_population() and cmp_equilibrium() in
// LatentGroups2\R\matching_compare.R and of the determination of kappa in R\run_comparison.R.
// The parameter values are taken from there, as the paper does not state all of them.

namespace SbamTest;

public sealed class Market
{
    public const int AgeMin = 20, AgeMax = 54;
    public const int NAge = AgeMax - AgeMin + 1;        // 35 one-year age steps
    public const int NEdu = 3, NReg = 4;
    public const int C = NAge * NEdu * NReg;            // 420 types per gender
    public const int N0 = 300000;                       // singles per gender before rounding
    public const double TargetRate = 0.12;              // share of the single men who find a partner

    /// <summary>Age (20-54), education (0-2) and region (0-3) of each type.</summary>
    public readonly int[] Age = new int[C], Edu = new int[C], Reg = new int[C];

    /// <summary>Number of single men (MA) and women (MB) per type.</summary>
    public readonly int[] MA, MB;

    /// <summary>The true expected couple table: Mu[x, y] is couples with a man of type x and a woman of type y.</summary>
    public readonly double[,] Mu = new double[C, C];

    public double Kappa { get; private set; }
    public int Iterations { get; private set; }
    public double EquilibriumError { get; private set; }

    /// <summary>Same order as expand.grid(age, edu, reg) in R: age runs fastest.</summary>
    public static int TypeIndex(int age, int edu, int reg) { return (age - AgeMin) + NAge * (edu + NEdu * reg); }

    public Market()
    {
        for (int r = 0; r < NReg; r++)
            for (int e = 0; e < NEdu; e++)
                for (int a = AgeMin; a <= AgeMax; a++)
                {
                    int x = TypeIndex(a, e, r);
                    Age[x] = a; Edu[x] = e; Reg[x] = r;
                }

        MA = Population(female: false);
        MB = Population(female: true);

        double[] k0 = Kernel();                         // exp(Phi/2) without the constant kappa
        double[] a0 = new double[C], b0 = new double[C];
        for (int x = 0; x < C; x++) { a0[x] = Math.Sqrt(MA[x]); b0[x] = Math.Sqrt(MB[x]); }

        // kappa is set so that 12 per cent of the single men find a partner (uniroot on [-15, 0] in R)
        double sumMA = MA.Sum();
        Func<double, double> f = kappa =>
        {
            Equilibrium(k0, Math.Exp(kappa), a0, b0, 1e-11, 100000);
            double couples = 0;
            for (int x = 0; x < C; x++) couples += MA[x] - a0[x] * a0[x];
            return couples / sumMA - TargetRate;
        };
        Kappa = Illinois(f, -15, 0);

        Iterations = Equilibrium(k0, Math.Exp(Kappa), a0, b0, 1e-13, 100000);
        double scale = Math.Exp(Kappa);
        for (int x = 0; x < C; x++)
            for (int y = 0; y < C; y++)
                Mu[x, y] = a0[x] * b0[y] * scale * k0[x * C + y];

        double err = 0;
        for (int x = 0; x < C; x++)
        {
            double row = 0, col = 0;
            for (int y = 0; y < C; y++) { row += Mu[x, y]; col += Mu[y, x]; }
            err = Math.Max(err, Math.Abs(a0[x] * a0[x] + row - MA[x]));
            err = Math.Max(err, Math.Abs(b0[x] * b0[x] + col - MB[x]));
        }
        EquilibriumError = err;
    }

    static double Plogis(double z) { return 1.0 / (1.0 + Math.Exp(-z)); }

    /// <summary>cmp_population(): decreasing in age, women better educated, the first region largest and best educated.</summary>
    int[] Population(bool female)
    {
        double[] regW = { 0.35, 0.25, 0.25, 0.15 };
        double f = female ? 1 : 0;
        double[] w = new double[C];
        double sum = 0;
        for (int x = 0; x < C; x++)
        {
            double da = Age[x] - 20;
            double first = Reg[x] == 0 ? 1 : 0;
            double wAge = Math.Exp(-0.06 * da) + 0.25;
            double hi = Plogis(-1.2 + 0.6 * f - 0.02 * da + 0.5 * first);
            double lo = (1 - hi) * Plogis(-1.0 - 0.3 * f + 0.02 * da - 0.3 * first);
            double wEdu = Edu[x] == 0 ? lo : Edu[x] == 1 ? 1 - hi - lo : hi;
            w[x] = wAge * regW[Reg[x]] * wEdu;
            sum += w[x];
        }
        int[] m = new int[C];
        for (int x = 0; x < C; x++) m[x] = (int)Math.Round(N0 * w[x] / sum);   // rounding half to even, as round() in R
        return m;
    }

    /// <summary>cmp_log_kernel(): K = exp(Phi/2) without kappa. Rows are the man's type, columns the woman's. Flat matrix [x * C + y].</summary>
    double[] Kernel()
    {
        double[,] rhoE = { { 0.8, 0, -0.9 }, { 0, 0.5, 0 }, { -0.9, 0, 0.9 } };   // educational homogamy
        double[,] rhoR = new double[NReg, NReg];                                   // couples are mostly formed within regions
        for (int i = 0; i < NReg; i++)
            for (int j = 0; j < NReg; j++)
                rhoR[i, j] = i == j ? 0 : -2.6;
        rhoR[0, 1] = rhoR[1, 0] = -1.5;                                            // regions 1 and 2 are neighbours

        double[] k = new double[C * C];
        for (int x = 0; x < C; x++)
            for (int y = 0; y < C; y++)
            {
                double gap = Age[x] - Age[y];                      // the man's age minus the woman's
                double mid = (Age[x] + Age[y]) / 2.0;
                double tau = 2 + 0.12 * (mid - 20);                // the tolerance for an age gap grows with age
                double hM = -0.035 * (Age[x] - 20);                // the gain from forming a couple falls with age
                double hW = -0.045 * (Age[y] - 20);
                double logK = hM + hW - (gap - 2) * (gap - 2) / (2 * tau * tau)
                              + rhoE[Edu[x], Edu[y]] + rhoR[Reg[x], Reg[y]];
                k[x * C + y] = Math.Exp(logK);
            }
        return k;
    }

    /// <summary>
    /// cmp_equilibrium(): the Choo-Siow equilibrium mu_xy = sqrt(mu_x0 mu_0y) K_xy solved with IPFP (Galichon and Salanié, 2022).
    /// a and b are the square roots of the numbers who remain single; they are used as starting values and overwritten with the solution.
    /// </summary>
    int Equilibrium(double[] k0, double scale, double[] a, double[] b, double tol, int maxit)
    {
        double[] big = new double[C];
        int it;
        for (it = 1; it <= maxit; it++)
        {
            double change = 0;
            for (int x = 0; x < C; x++)
            {
                double s = 0;
                int o = x * C;
                for (int y = 0; y < C; y++) s += k0[o + y] * b[y];
                big[x] = s * scale;
            }
            for (int x = 0; x < C; x++)
            {
                double aNew = Math.Sqrt(MA[x] + big[x] * big[x] / 4) - big[x] / 2;
                change = Math.Max(change, Math.Abs(aNew - a[x]) / (a[x] + 1e-300));
                a[x] = aNew;
            }
            Array.Clear(big);
            for (int x = 0; x < C; x++)
            {
                double ax = a[x];
                int o = x * C;
                for (int y = 0; y < C; y++) big[y] += k0[o + y] * ax;
            }
            for (int y = 0; y < C; y++)
            {
                double s = big[y] * scale;
                double bNew = Math.Sqrt(MB[y] + s * s / 4) - s / 2;
                change = Math.Max(change, Math.Abs(bNew - b[y]) / (b[y] + 1e-300));
                b[y] = bNew;
            }
            if (change < tol) break;
        }
        return it;
    }

    /// <summary>Root finding with the Illinois variant of regula falsi on an interval with a sign change.</summary>
    static double Illinois(Func<double, double> f, double lo, double hi)
    {
        double flo = f(lo), fhi = f(hi);
        if (flo * fhi > 0) throw new Exception("Illinois: intet fortegnsskift på intervallet");
        int side = 0;
        double c = lo;
        for (int it = 0; it < 200; it++)
        {
            c = (flo * hi - fhi * lo) / (flo - fhi);
            double fc = f(c);
            if (Math.Abs(fc) < 1e-13 || Math.Abs(hi - lo) < 1e-13) break;
            if (fc * fhi > 0)
            {
                hi = c; fhi = fc;
                if (side == -1) flo /= 2;
                side = -1;
            }
            else
            {
                lo = c; flo = fc;
                if (side == 1) fhi /= 2;
                side = 1;
            }
        }
        return c;
    }

    // ---------------------------------------------------------------------------------------------
    // Measures of a couple table (port of cmp_measures() and dissim() in matching_compare.R)
    // ---------------------------------------------------------------------------------------------

    public sealed class Summary
    {
        public double Couples, GapMean, GapSd, Extreme, SameEdu, SameReg;
    }

    public Summary Describe(double[,] tab)
    {
        double n = 0, sGap = 0, sGap2 = 0, ext = 0, sameE = 0, sameR = 0;
        for (int x = 0; x < C; x++)
            for (int y = 0; y < C; y++)
            {
                double t = tab[x, y];
                if (t == 0) continue;
                double gap = Age[x] - Age[y];
                n += t; sGap += t * gap; sGap2 += t * gap * gap;
                if (Math.Abs(gap - 2) > 10) ext += t;      // extreme couples: more than 10 years from the typical gap of 2 years
                if (Edu[x] == Edu[y]) sameE += t;
                if (Reg[x] == Reg[y]) sameR += t;
            }
        double mean = sGap / n;
        return new Summary
        {
            Couples = n,
            GapMean = mean,
            GapSd = Math.Sqrt(Math.Max(0, sGap2 / n - mean * mean)),
            Extreme = 100 * ext / n,
            SameEdu = 100 * sameE / n,
            SameReg = 100 * sameR / n
        };
    }

    /// <summary>Dissimilarity index between two couple tables after aggregation: age, education, region and 5-year age x education x region.</summary>
    public double[] Dissimilarity(double[,] tab, double[,] reference)
    {
        int[] gAge = new int[C], gEdu = new int[C], gReg = new int[C], gJoint = new int[C];
        for (int x = 0; x < C; x++)
        {
            gAge[x] = Age[x] - AgeMin;
            gEdu[x] = Edu[x];
            gReg[x] = Reg[x];
            gJoint[x] = (Age[x] - AgeMin) / 5 + 7 * (Edu[x] + NEdu * Reg[x]);
        }
        return new[]
        {
            Dissim(Aggregate(tab, gAge, NAge), Aggregate(reference, gAge, NAge)),
            Dissim(Aggregate(tab, gEdu, NEdu), Aggregate(reference, gEdu, NEdu)),
            Dissim(Aggregate(tab, gReg, NReg), Aggregate(reference, gReg, NReg)),
            Dissim(Aggregate(tab, gJoint, 7 * NEdu * NReg), Aggregate(reference, gJoint, 7 * NEdu * NReg))
        };
    }

    static double[,] Aggregate(double[,] tab, int[] g, int groups)
    {
        double[,] agg = new double[groups, groups];
        for (int x = 0; x < C; x++)
            for (int y = 0; y < C; y++)
                agg[g[x], g[y]] += tab[x, y];
        return agg;
    }

    /// <summary>Share (per cent) of the couples that would have to be moved to another cell to reproduce the reference distribution.</summary>
    public static double Dissim(double[,] a, double[,] b)
    {
        double sa = 0, sb = 0;
        foreach (double v in a) sa += v;
        foreach (double v in b) sb += v;
        double d = 0;
        for (int i = 0; i < a.GetLength(0); i++)
            for (int j = 0; j < a.GetLength(1); j++)
                d += Math.Abs(a[i, j] / sa - b[i, j] / sb);
        return 50 * d;
    }

    // ---------------------------------------------------------------------------------------------
    // Random draws
    // ---------------------------------------------------------------------------------------------

    /// <summary>Poisson draw (Knuth's method; large means are split up so that exp(-m) does not underflow).</summary>
    public static int Poisson(Random rng, double mean)
    {
        int k = 0;
        while (mean > 30) { k += PoissonSmall(rng, 30); mean -= 30; }
        return mean > 0 ? k + PoissonSmall(rng, mean) : k;
    }

    static int PoissonSmall(Random rng, double mean)
    {
        double limit = Math.Exp(-mean), p = 1;
        int k = 0;
        do { k++; p *= rng.NextDouble(); } while (p > limit);
        return k - 1;
    }

    public static int Binomial(Random rng, int n, double p)
    {
        int k = 0;
        for (int i = 0; i < n; i++)
            if (rng.NextDouble() < p) k++;
        return k;
    }

    /// <summary>One realisation of the couple table: independent Poisson draws with mean factor * Mu.</summary>
    public int[,] Draw(Random rng, double factor = 1)
    {
        int[,] x = new int[C, C];
        for (int i = 0; i < C; i++)
            for (int j = 0; j < C; j++)
                x[i, j] = Poisson(rng, factor * Mu[i, j]);
        return x;
    }
}
