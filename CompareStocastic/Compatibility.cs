// The compatibility indices used by the stochastic matching algorithms.
//
// A compatibility index gives, for each type of man x and each type of woman y, a number that measures how
// likely the two are to form a couple. All indices are stored as a flat table [x * C + y], so that the
// matching algorithms can look a value up instead of computing it for every pair of persons.
//
//   Distance  The exponential distance function documented for DYNASIM (Perese, 2002).
//   Logit     A logit model estimated on a "potential pairs" file, as in CORSIM/DYNACAN and CBOLT (Bouffard
//             et al., 2001; Perese, 2002): every man among the historical couples is paired with every woman,
//             and the dependent variable is 1 for the actual couples. There is a basic specification with 9
//             parameters and an extended one with 31.
//   Table     The saturated version of the same model: one parameter per cell. The maximum likelihood
//             estimate is the share of the potential pairs in the cell that are actual couples. It uses
//             exactly the same information as SBAM (the historical couple table).
//   Oracle    The population value of the saturated model, computed from the true couple table. It has
//             neither sampling error nor specification error and can only be computed on an artificial market.

namespace CompareStocastic;

static class Compatibility
{
    const int C = Market.C;

    /// <summary>DYNASIM: exp(-0.5 * [(age difference)^2 + (education difference)^2]). Education is measured in levels.</summary>
    public static float[] Distance(Market m)
    {
        float[] idx = new float[C * C];
        for (int x = 0; x < C; x++)
            for (int y = 0; y < C; y++)
            {
                double da = m.Age[x] - m.Age[y], de = m.Edu[x] - m.Edu[y];
                idx[x * C + y] = (float)Math.Exp(-0.5 * (da * da + de * de));
            }
        return idx;
    }

    /// <summary>The true couple table divided by the number of potential pairs: mu_xy / (mu_x. * mu_.y).</summary>
    public static float[] Oracle(Market m)
    {
        float[] idx = new float[C * C];
        for (int x = 0; x < C; x++)
            for (int y = 0; y < C; y++)
                idx[x * C + y] = (float)(m.Mu[x, y] / (m.RowSum[x] * m.ColSum[y]));
        return idx;
    }

    /// <summary>Historical couples divided by potential pairs: X_xy / (X_x. * X_.y). 0 for cells without historical couples.</summary>
    public static float[] Table(int[,] hist)
    {
        double[] row = new double[C], col = new double[C];
        for (int x = 0; x < C; x++)
            for (int y = 0; y < C; y++) { row[x] += hist[x, y]; col[y] += hist[x, y]; }
        float[] idx = new float[C * C];
        for (int x = 0; x < C; x++)
            for (int y = 0; y < C; y++)
                if (hist[x, y] > 0)
                    idx[x * C + y] = (float)(hist[x, y] / (row[x] * col[y]));
        return idx;
    }

    // ---------------------------------------------------------------------------------------------
    // Logit on potential pairs
    // ---------------------------------------------------------------------------------------------

    public sealed class LogitResult
    {
        public string[] Names;
        public double[] Beta;
        public int Iterations;
        public double LogLikelihood;
        public double PotentialPairs, Couples;
        public float[] Index;          // predicted probability for each cell
    }

    /// <summary>
    /// The explanatory variables. The first nine are the basic specification, which follows the specifications
    /// documented for CORSIM/DYNACAN and CBOLT: age difference, age difference squared and education
    /// difference, plus the region, which the types of the market also contain. The extended specification adds
    /// 22 variables: five that let the age pattern depend on the mean age of the two, four that together with
    /// the basic ones give one parameter for each combination of the two education levels, and thirteen that
    /// give one parameter for each combination of the two regions.
    /// </summary>
    const int BasicVariables = 9;

    // The combinations of education (man, woman) and of region (man, woman) that get their own variable in the
    // extended specification. The remaining combinations are identified by the basic variables.
    static readonly (int M, int W)[] EduPairs = { (0, 0), (2, 2), (0, 1), (1, 0) };
    static readonly (int M, int W)[] RegPairs =
    {
        (0, 0), (1, 1), (2, 2), (0, 1), (0, 2), (0, 3), (1, 2), (1, 3), (2, 0), (2, 1), (2, 3), (3, 0), (3, 1)
    };

    // Must come after EduPairs and RegPairs, which BuildNames uses (static fields are initialised in the order they are written)
    public static readonly string[] LogitNames = BuildNames();

    static string[] BuildNames()
    {
        var names = new List<string>
        {
            "Constant",
            "Age difference (man minus woman)",
            "Age difference squared",
            "Woman two education levels higher",
            "Woman one education level higher",
            "Woman one education level lower",
            "Woman two education levels lower",
            "Same region",
            "Neighbouring regions (1 and 2)",
            "Mean age",
            "Mean age squared",
            "Age difference x mean age",
            "Age difference squared x mean age",
            "Age difference squared x mean age squared"
        };
        foreach (var (em, ew) in EduPairs) names.Add($"Education: man {em + 1}, woman {ew + 1}");
        foreach (var (rm, rw) in RegPairs) names.Add($"Region: man {rm + 1}, woman {rw + 1}");
        return names.ToArray();
    }

    /// <summary>The explanatory variables for a man of type x and a woman of type y. Mean age is (mean age of the two - 37) / 10.</summary>
    static void Covariates(Market m, int x, int y, double[] z)
    {
        double d = m.Age[x] - m.Age[y];
        int de = m.Edu[x] - m.Edu[y];        // positive: the woman has the lower education
        z[0] = 1;
        z[1] = d;
        z[2] = d * d;
        z[3] = de == -2 ? 1 : 0;
        z[4] = de == -1 ? 1 : 0;
        z[5] = de == 1 ? 1 : 0;
        z[6] = de == 2 ? 1 : 0;
        z[7] = m.Reg[x] == m.Reg[y] ? 1 : 0;
        z[8] = (m.Reg[x] == 0 && m.Reg[y] == 1) || (m.Reg[x] == 1 && m.Reg[y] == 0) ? 1 : 0;
        if (z.Length == BasicVariables) return;
        double a = ((m.Age[x] + m.Age[y]) / 2.0 - 37) / 10;
        z[9] = a;
        z[10] = a * a;
        z[11] = d * a;
        z[12] = d * d * a;
        z[13] = d * d * a * a;
        int k = 14;
        foreach (var (em, ew) in EduPairs) z[k++] = m.Edu[x] == em && m.Edu[y] == ew ? 1 : 0;
        foreach (var (rm, rw) in RegPairs) z[k++] = m.Reg[x] == rm && m.Reg[y] == rw ? 1 : 0;
    }

    /// <summary>
    /// Maximum likelihood estimation of the logit model on the potential pairs. The file of potential pairs is
    /// not built: all pairs in a cell have the same explanatory variables, so the likelihood is that of a
    /// binomial logit with one observation per cell, n_xy = X_x. * X_.y trials and X_xy successes.
    /// Newton's method with step halving.
    /// </summary>
    public static LogitResult Logit(Market m, int[,] hist, bool extended = false)
    {
        int P = extended ? LogitNames.Length : BasicVariables;
        double[] row = new double[C], col = new double[C];
        double couples = 0;
        for (int x = 0; x < C; x++)
            for (int y = 0; y < C; y++) { row[x] += hist[x, y]; col[y] += hist[x, y]; couples += hist[x, y]; }
        double pairs = couples * couples;

        // Explanatory variables per cell
        double[][] z = new double[C * C][];
        for (int x = 0; x < C; x++)
            for (int y = 0; y < C; y++)
            {
                z[x * C + y] = new double[P];
                Covariates(m, x, y, z[x * C + y]);
            }

        Func<double[], double> logLik = b =>
        {
            double ll = 0;
            for (int x = 0; x < C; x++)
                for (int y = 0; y < C; y++)
                {
                    double n = row[x] * col[y];
                    if (n == 0) continue;
                    double eta = Dot(b, z[x * C + y]);
                    // log(1 + exp(eta)) computed without overflow
                    double log1p = eta > 30 ? eta : Math.Log(1 + Math.Exp(eta));
                    ll += hist[x, y] * eta - n * log1p;
                }
            return ll;
        };

        double[] beta = new double[P];
        beta[0] = Math.Log(couples / (pairs - couples));    // all pairs equally likely
        double llOld = logLik(beta);
        int it;
        for (it = 1; it <= 100; it++)
        {
            double[] grad = new double[P];
            double[,] hess = new double[P, P];
            for (int x = 0; x < C; x++)
                for (int y = 0; y < C; y++)
                {
                    double n = row[x] * col[y];
                    if (n == 0) continue;
                    double[] zc = z[x * C + y];
                    double p = 1 / (1 + Math.Exp(-Dot(beta, zc)));
                    double r = hist[x, y] - n * p, w = n * p * (1 - p);
                    for (int i = 0; i < P; i++)
                    {
                        if (zc[i] == 0) continue;
                        grad[i] += r * zc[i];
                        for (int j = 0; j <= i; j++) hess[i, j] += w * zc[i] * zc[j];
                    }
                }
            for (int i = 0; i < P; i++)
                for (int j = i + 1; j < P; j++) hess[i, j] = hess[j, i];

            double[] step = Solve(hess, grad);
            double llNew = double.NegativeInfinity;
            double[] trial = null;
            for (double len = 1; len > 1e-6; len /= 2)
            {
                trial = new double[P];
                for (int i = 0; i < P; i++) trial[i] = beta[i] + len * step[i];
                llNew = logLik(trial);
                if (llNew >= llOld) break;
            }
            if (llNew < llOld) break;
            bool done = llNew - llOld < 1e-9 * Math.Abs(llOld);
            beta = trial; llOld = llNew;
            if (done) break;
        }

        float[] idx = new float[C * C];
        for (int c = 0; c < C * C; c++)
            idx[c] = (float)(1 / (1 + Math.Exp(-Dot(beta, z[c]))));

        return new LogitResult
        {
            Names = LogitNames.Take(P).ToArray(), Beta = beta, Iterations = it, LogLikelihood = llOld,
            PotentialPairs = pairs, Couples = couples, Index = idx
        };
    }

    static double Dot(double[] a, double[] b)
    {
        double s = 0;
        for (int i = 0; i < a.Length; i++) s += a[i] * b[i];
        return s;
    }

    /// <summary>Solves A x = b by Gaussian elimination with partial pivoting.</summary>
    static double[] Solve(double[,] a, double[] b)
    {
        int n = b.Length;
        double[,] m = (double[,])a.Clone();
        double[] x = (double[])b.Clone();
        for (int k = 0; k < n; k++)
        {
            int piv = k;
            for (int i = k + 1; i < n; i++)
                if (Math.Abs(m[i, k]) > Math.Abs(m[piv, k])) piv = i;
            if (Math.Abs(m[piv, k]) < 1e-300) throw new Exception("Logit: singular Hessian (an explanatory variable does not vary in the data)");
            if (piv != k)
            {
                for (int j = 0; j < n; j++) { double t = m[k, j]; m[k, j] = m[piv, j]; m[piv, j] = t; }
                double tb = x[k]; x[k] = x[piv]; x[piv] = tb;
            }
            for (int i = k + 1; i < n; i++)
            {
                double f = m[i, k] / m[k, k];
                if (f == 0) continue;
                for (int j = k; j < n; j++) m[i, j] -= f * m[k, j];
                x[i] -= f * x[k];
            }
        }
        for (int k = n - 1; k >= 0; k--)
        {
            double s = x[k];
            for (int j = k + 1; j < n; j++) s -= m[k, j] * x[j];
            x[k] = s / m[k, k];
        }
        return x;
    }

    /// <summary>The largest value of an index (used to scale it to a probability in the DYNASIM rule).</summary>
    public static float Max(float[] idx)
    {
        float max = 0;
        foreach (float v in idx) if (v > max) max = v;
        return max;
    }
}
