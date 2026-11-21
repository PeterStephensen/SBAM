// Stochastic matching: the three algorithms that Perese (2002) documents for the microsimulation models
// DYNASIM, CORSIM/DYNACAN and CBOLT.
//
// All three take the same input as the SBAM code: the households in the model that are in the matching pool.
// They split the pool into a randomly ordered queue of men and one of women, cut the longer queue down to the
// length of the shorter, and form couples from a compatibility index (Compatibility.cs). The couples are
// reported to the model in the same way as the SBAM code does (Statistics.Report).
//
//   Dynasim   For each man in turn: the first available women in the queue are tried one at a time (10 women,
//             20 if the man is 35 or older). A woman is accepted if the index exceeds a random number. If none
//             is accepted, the man gets the woman with the highest index among those tried.
//   Cbolt     For each man in turn: a first pass over all remaining women finds the highest index. In a second
//             pass a woman is accepted if her index divided by that maximum exceeds a random number.
//   Corsim    A couple is drawn among all remaining pairs with probability proportional to the index. This is
//             repeated until everybody is matched.
//
// Cbolt and Corsim are implemented in two ways that follow the same rule:
//   by person  The index is looked up for every pair of persons the rule considers, as the algorithms are
//              described in the literature. The time grows with the square of the pool.
//   by type    Persons of the same type are interchangeable, so the computation can be done on the number of
//              persons of each type. Cbolt by type forms exactly the same couples as Cbolt by person. Corsim
//              by type draws from the same distribution as Corsim by person.

using DreamSMILE.DemoSim;
using DreamSMILE.MicroSim;

namespace CompareStocastic;

sealed class StochasticMatching
{
    const int C = Market.C;

    readonly int[] _eduIndex, _regIndex;    // from the model's codes to the education and region of the market

    /// <summary>Compatibility index by type of man and type of woman, [x * C + y].</summary>
    public float[] Index;

    /// <summary>Factor that turns the index into a probability in Dynasim (1 / the largest value of the index).</summary>
    public double Scale = 1;

    public Random Rng = new Random(1);

    // Results from the latest call
    public int LastCouples;          // couples formed
    public int LastForced;           // couples formed although the index of the couple was 0
    public long LastEvaluations;     // number of times the index was looked up
    public double LastMsPrepare;     // time used on building the queues, in milliseconds

    // The pool: households and types in queue order. Position i in the men's queue, w in the women's.
    Household[] _men = new Household[0], _women = new Household[0];
    int[] _typeM = new int[0], _typeW = new int[0];
    int _n;

    // The women's queue as a linked list, so that a woman can be removed without changing the order of the rest
    int[] _next = new int[0], _prev = new int[0];
    int _head;

    Simulation _sim;

    /// <param name="eduIndex">eduIndex[HighestEducation5] = education level in the market (0-2).</param>
    /// <param name="regIndex">regIndex[(int)Region] = region in the market (0-3).</param>
    public StochasticMatching(int[] eduIndex, int[] regIndex)
    {
        _eduIndex = eduIndex;
        _regIndex = regIndex;
    }

    // ---------------------------------------------------------------------------------------------
    // Preparation, common to the three algorithms
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Builds the two queues from the households in the matching pool: computes the type of each person from
    /// his or her age, education and region, puts each queue in random order and cuts the longer queue down.
    /// </summary>
    void Prepare(List<Household> households)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        _sim = Simulation.Instance;
        var men = new List<Household>();
        var women = new List<Household>();
        foreach (Household h in households)
        {
            if (!h.State.Is(EHouseholdState.InMatchingPool)) continue;
            Person p = h.FirstAdult;
            if (p.Age < Market.AgeMin || p.Age > Market.AgeMax) continue;
            if (p.Gender == EGender.Male) men.Add(h); else women.Add(h);
        }
        Shuffle(men);
        Shuffle(women);
        _n = Math.Min(men.Count, women.Count);

        if (_men.Length < _n)
        {
            _men = new Household[_n]; _women = new Household[_n];
            _typeM = new int[_n]; _typeW = new int[_n];
            _next = new int[_n]; _prev = new int[_n];
        }
        for (int i = 0; i < _n; i++)
        {
            _men[i] = men[i]; _typeM[i] = TypeOf(men[i].FirstAdult);
            _women[i] = women[i]; _typeW[i] = TypeOf(women[i].FirstAdult);
            _next[i] = i + 1; _prev[i] = i - 1;
        }
        _head = 0;

        LastCouples = 0;
        LastForced = 0;
        LastEvaluations = 0;
        LastMsPrepare = timer.Elapsed.TotalMilliseconds;
    }

    int TypeOf(Person p)
    {
        return (p.Age - Market.AgeMin) + Market.NAge * (_eduIndex[p.HighestEducation5] + Market.NEdu * _regIndex[(int)p.Region]);
    }

    void Shuffle(List<Household> list)
    {
        for (int n = list.Count; n > 1; n--)
        {
            int k = Rng.Next(n);
            Household t = list[k]; list[k] = list[n - 1]; list[n - 1] = t;
        }
    }

    /// <summary>Forms the couple of man i and woman w (positions in the original queues).</summary>
    void Match(int i, int w)
    {
        _sim.Statistics.Report(Event.Demographics.Matching, _men[i], _women[w]);
        LastCouples++;
    }

    /// <summary>Removes woman w from the linked queue of available women.</summary>
    void Remove(int w)
    {
        int p = _prev[w], q = _next[w];
        if (p >= 0) _next[p] = q; else _head = q;
        if (q < _n) _prev[q] = p;
    }

    // ---------------------------------------------------------------------------------------------
    // DYNASIM
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// For each man: the first available women in the queue are tried in turn, at most 10 (20 for men aged 35
    /// or more). The match is made if Index * Scale exceeds a random number. If no match has been made, the
    /// man is matched with the woman with the highest index among those tried.
    /// </summary>
    public void Dynasim(List<Household> households)
    {
        Prepare(households);
        int n = _n;
        float[] idx = Index;
        for (int i = 0; i < n; i++)
        {
            int row = _typeM[i] * C;
            int age = Market.AgeMin + _typeM[i] % Market.NAge;
            int trials = age >= 35 ? 20 : 10;
            int best = -1, chosen = -1;
            float bestC = -1;
            int w = _head;
            for (int t = 0; t < trials && w < n; t++, w = _next[w])
            {
                float c = idx[row + _typeW[w]];
                LastEvaluations++;
                if (c * Scale > Rng.NextDouble()) { chosen = w; break; }
                if (c > bestC) { bestC = c; best = w; }
            }
            if (chosen < 0)
            {
                chosen = best;
                if (bestC <= 0) LastForced++;
            }
            Match(i, chosen);
            Remove(chosen);
        }
    }

    // ---------------------------------------------------------------------------------------------
    // CBOLT
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// For each man: the first pass finds the highest index over all remaining women (the normalisation factor).
    /// In the second pass the women are taken in queue order, and a woman is accepted if her index divided by
    /// the normalisation factor exceeds a random number. The woman with the highest index is accepted with
    /// certainty, so the man always finds a partner in the second pass.
    /// byType: the first pass runs over the types that still have women in the queue instead of over the women.
    /// The result is the same.
    /// randomStart: a diagnostic variant that is not part of the documented rule. The second pass starts at a
    /// randomly chosen remaining woman instead of at the head of the queue (and continues from the head when it
    /// reaches the end). It shows how much of the rule's error comes from every man meeting the women in the
    /// same order.
    /// </summary>
    public void Cbolt(List<Household> households, bool byType, bool randomStart = false)
    {
        Prepare(households);
        int n = _n;
        float[] idx = Index;

        int[] left = null;                 // women left of each type
        if (byType || randomStart)
        {
            left = new int[C];
            for (int w = 0; w < n; w++) left[_typeW[w]]++;
        }

        if (randomStart) { CboltRandomStart(left); return; }

        for (int i = 0; i < n; i++)
        {
            int row = _typeM[i] * C;

            // First pass: the normalisation factor
            float max = 0;
            if (byType)
            {
                for (int y = 0; y < C; y++)
                    if (left[y] > 0 && idx[row + y] > max) max = idx[row + y];
                LastEvaluations += C;
            }
            else
            {
                for (int w = _head; w < n; w = _next[w])
                {
                    float c = idx[row + _typeW[w]];
                    if (c > max) max = c;
                    LastEvaluations++;
                }
            }

            // Second pass
            int chosen = -1;
            if (max <= 0)
            {
                chosen = _head;            // no remaining woman is compatible with the man: he gets the first in the queue
                LastForced++;
            }
            else
            {
                for (int w = _head; w < n; w = _next[w])
                {
                    LastEvaluations++;
                    if (idx[row + _typeW[w]] > max * Rng.NextDouble()) { chosen = w; break; }
                }
                if (chosen < 0)            // can only happen through rounding: take the first woman with the highest index
                    for (int w = _head; w < n; w = _next[w])
                        if (idx[row + _typeW[w]] >= max) { chosen = w; break; }
            }

            Match(i, chosen);
            Remove(chosen);
            if (byType) left[_typeW[chosen]]--;
        }
    }

    /// <summary>The CBOLT rule by type, with the second pass starting at a random remaining woman (diagnostic variant).</summary>
    void CboltRandomStart(int[] left)
    {
        int n = _n;
        float[] idx = Index;
        int[] alive = new int[n], where = new int[n];   // the remaining women, so that one can be drawn at random
        for (int w = 0; w < n; w++) { alive[w] = w; where[w] = w; }
        int rem = n;

        for (int i = 0; i < n; i++)
        {
            int row = _typeM[i] * C;
            float max = 0;
            for (int y = 0; y < C; y++)
                if (left[y] > 0 && idx[row + y] > max) max = idx[row + y];
            LastEvaluations += C;

            int start = alive[Rng.Next(rem)], chosen = -1;
            if (max <= 0)
            {
                chosen = start;
                LastForced++;
            }
            else
            {
                int w = start;
                do
                {
                    LastEvaluations++;
                    if (idx[row + _typeW[w]] > max * Rng.NextDouble()) { chosen = w; break; }
                    w = _next[w];
                    if (w >= n) w = _head;
                } while (w != start);
                if (chosen < 0)
                    for (w = _head; w < n; w = _next[w])
                        if (idx[row + _typeW[w]] >= max) { chosen = w; break; }
            }

            Match(i, chosen);
            Remove(chosen);
            left[_typeW[chosen]]--;
            rem--;
            int k = where[chosen];
            alive[k] = alive[rem];
            where[alive[k]] = k;
        }
    }

    // ---------------------------------------------------------------------------------------------
    // CORSIM/DYNACAN
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// A couple is drawn among all remaining pairs of a man and a woman with probability proportional to the
    /// index of the pair. The two are removed, and the draw is repeated until everybody is matched. If all
    /// remaining pairs have index 0, the rest are matched in queue order.
    /// </summary>
    public void Corsim(List<Household> households, bool byType)
    {
        Prepare(households);
        if (byType) CorsimByType(); else CorsimByPerson();
    }

    /// <summary>
    /// The n x n matrix of cumulative probabilities in the description is not stored. Instead the sum of the
    /// index over the remaining women is kept for each man. A couple is drawn by first drawing the man with
    /// probability proportional to his sum and then the woman with probability proportional to the index of
    /// the pair. That gives each pair a probability proportional to its index. The sums are updated when a
    /// woman is removed. Each couple costs a few passes over the remaining persons.
    /// </summary>
    void CorsimByPerson()
    {
        int n = _n;
        float[] idx = Index;
        int[] tm = new int[n], tw = new int[n], im = new int[n], iw = new int[n];
        for (int i = 0; i < n; i++) { tm[i] = _typeM[i]; tw[i] = _typeW[i]; im[i] = i; iw[i] = i; }

        // rowSum[k] = sum of the index of man k with all remaining women
        double[] rowSum = new double[n];
        double total = 0;
        for (int k = 0; k < n; k++)
        {
            int row = tm[k] * C;
            double s = 0;
            for (int w = 0; w < n; w++) s += idx[row + tw[w]];
            rowSum[k] = s;
            total += s;
        }
        LastEvaluations += (long)n * n;

        int rem = n;
        while (rem > 0)
        {
            // The man
            int k0 = -1;
            if (total > 0)
            {
                double u = Rng.NextDouble() * total, cum = 0;
                for (int k = 0; k < rem; k++)
                {
                    cum += rowSum[k];
                    if (u < cum) { k0 = k; break; }
                }
            }
            if (k0 < 0)
            {
                // The sums are exhausted (or only rounding errors are left): recompute them exactly
                total = 0;
                for (int k = 0; k < rem; k++)
                {
                    int r = tm[k] * C;
                    double s = 0;
                    for (int w = 0; w < rem; w++) s += idx[r + tw[w]];
                    rowSum[k] = s;
                    total += s;
                }
                LastEvaluations += (long)rem * rem;
                if (total > 0) continue;
                for (int k = 0; k < rem; k++) { Match(im[k], iw[k]); LastForced++; }   // no compatible pairs left
                return;
            }

            // The woman: exact sum of the man's row, then a draw proportional to the index
            int row0 = tm[k0] * C;
            double sum = 0;
            for (int w = 0; w < rem; w++) sum += idx[row0 + tw[w]];
            LastEvaluations += rem;
            if (sum <= 0)
            {
                total -= rowSum[k0];       // the kept sum was a rounding error
                rowSum[k0] = 0;
                if (total < 0) total = 0;
                continue;
            }
            double v = Rng.NextDouble() * sum, c2 = 0;
            int w0 = -1;
            for (int w = 0; w < rem; w++)
            {
                float c = idx[row0 + tw[w]];
                if (c <= 0) continue;
                c2 += c;
                w0 = w;
                if (v < c2) break;
            }

            Match(im[k0], iw[w0]);

            // Remove the two (the last in each array takes the free place) and update the sums
            int ty = tw[w0];
            rem--;
            tm[k0] = tm[rem]; im[k0] = im[rem]; rowSum[k0] = rowSum[rem];
            tw[w0] = tw[rem]; iw[w0] = iw[rem];
            total = 0;
            for (int k = 0; k < rem; k++)
            {
                double s = rowSum[k] - idx[tm[k] * C + ty];
                if (s < 0) s = 0;
                rowSum[k] = s;
                total += s;
            }
            LastEvaluations += rem;
        }
    }

    /// <summary>
    /// The same rule computed on types. The sum of the index over all remaining pairs with a man of type x is
    /// (men left of type x) * s[x], where s[x] is the sum over the women's types of (women left) * index. A
    /// couple is drawn by drawing the man's type, then the woman's type, and then taking the next man and the
    /// next woman of the two types in the queues. Each couple costs a few passes over the types.
    /// </summary>
    void CorsimByType()
    {
        int n = _n;
        float[] idx = Index;

        // The transposed index, so that the update of s runs through memory in order
        float[] idxT = new float[C * C];
        for (int x = 0; x < C; x++)
            for (int y = 0; y < C; y++)
                idxT[y * C + x] = idx[x * C + y];

        // The queues by type: the positions of the men (women) of each type in queue order
        int[] leftM = new int[C], leftW = new int[C];
        for (int i = 0; i < n; i++) { leftM[_typeM[i]]++; leftW[_typeW[i]]++; }
        int[] startM = new int[C + 1], startW = new int[C + 1];
        for (int x = 0; x < C; x++) { startM[x + 1] = startM[x] + leftM[x]; startW[x + 1] = startW[x] + leftW[x]; }
        int[] posM = new int[n], posW = new int[n], fillM = new int[C], fillW = new int[C];
        for (int i = 0; i < n; i++)
        {
            posM[startM[_typeM[i]] + fillM[_typeM[i]]++] = i;
            posW[startW[_typeW[i]] + fillW[_typeW[i]]++] = i;
        }
        int[] nextM = (int[])startM.Clone(), nextW = (int[])startW.Clone();

        double[] s = new double[C];
        Action recompute = () =>
        {
            for (int x = 0; x < C; x++)
            {
                int row = x * C;
                double sum = 0;
                for (int y = 0; y < C; y++)
                    if (leftW[y] > 0) sum += leftW[y] * (double)idx[row + y];
                s[x] = sum;
            }
            LastEvaluations += C * C;
        };
        recompute();

        int formed = 0, sinceRecompute = 0;
        while (formed < n)
        {
            if (sinceRecompute >= 1000) { recompute(); sinceRecompute = 0; }   // removes accumulated rounding errors

            // The man's type
            double total = 0;
            for (int x = 0; x < C; x++) total += leftM[x] * s[x];
            int x0 = -1;
            if (total > 0)
            {
                double u = Rng.NextDouble() * total, cum = 0;
                for (int x = 0; x < C; x++)
                {
                    cum += leftM[x] * s[x];
                    if (u < cum) { x0 = x; break; }
                }
            }
            if (x0 < 0)
            {
                recompute(); sinceRecompute = 0;
                total = 0;
                for (int x = 0; x < C; x++) total += leftM[x] * s[x];
                if (total > 0) continue;
                // No compatible pairs left: the rest are matched in queue order
                var restM = new List<int>();
                var restW = new List<int>();
                for (int x = 0; x < C; x++)
                {
                    for (int p = nextM[x]; p < startM[x + 1]; p++) restM.Add(posM[p]);
                    for (int p = nextW[x]; p < startW[x + 1]; p++) restW.Add(posW[p]);
                }
                restM.Sort();
                restW.Sort();
                for (int k = 0; k < restM.Count; k++) { Match(restM[k], restW[k]); LastForced++; }
                return;
            }

            // The woman's type: exact sum of the row, then a draw proportional to (women left) * index
            int row0 = x0 * C;
            double sum0 = 0;
            for (int y = 0; y < C; y++)
                if (leftW[y] > 0) sum0 += leftW[y] * (double)idx[row0 + y];
            LastEvaluations += C;
            if (sum0 <= 0) { s[x0] = 0; continue; }      // the kept sum was a rounding error
            double v = Rng.NextDouble() * sum0, c2 = 0;
            int y0 = -1;
            for (int y = 0; y < C; y++)
            {
                if (leftW[y] == 0) continue;
                double c = leftW[y] * (double)idx[row0 + y];
                if (c <= 0) continue;
                c2 += c;
                y0 = y;
                if (v < c2) break;
            }

            Match(posM[nextM[x0]++], posW[nextW[y0]++]);
            leftM[x0]--;
            leftW[y0]--;
            formed++;
            sinceRecompute++;

            // One woman of type y0 fewer: s[x] falls by the index of (x, y0)
            int col = y0 * C;
            for (int x = 0; x < C; x++)
            {
                double sx = s[x] - idxT[col + x];
                s[x] = sx < 0 ? 0 : sx;
            }
            LastEvaluations += C;
        }
    }
}
