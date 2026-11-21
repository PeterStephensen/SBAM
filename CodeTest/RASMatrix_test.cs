// RASMatrix_test.cs: trial version of Numerical\RASMatrix.cs, used by Matching_test.cs.
//
// The class is a copy of RASMatrix with THREE changes, marked //TEST0, //TEST1 and //TEST4. The constructor
// and the folding into a triangle are unchanged. The parts of RASMatrix.cs that Matching does not use
// (the constructor taking double[,], NSum() and RASAdjust_OLD()) are not included. The cells are the same
// RASMatrixCell as in RASMatrix.cs.
//
// The comments and names that belong to the changes are in English. The code taken over from RASMatrix.cs,
// including its Danish comments, is unchanged.
//
// TEST0: Reset() restores the historical data.
//   RASMatrixCell.Reset() sets Value back to the value the cell was created with. Matching.cs creates
//   one cell per couple with the value 1 and only afterwards adds the cells together by counting Value
//   up. Reset() therefore sets ALL cells to 1, and from the second call of matching() the historical
//   couple table has been replaced by a table with 1 in every cell that occurs in the data. Here the
//   values are stored as they are when the matrix is created, and Reset() restores them.
//
// TEST1: The stopping criterion in RASAdjust().
//   In RASMatrix.cs "err0 = err" is set just before the comparison "err - err0 < 0.00001", so the loop
//   always stops in the second pass. Here the error from the previous pass is remembered correctly, and
//   the loop runs until the sums match (Tolerance), until the error no longer falls, or until MaxPasses
//   has been reached. The error is measured relative to the total target, so that the criterion does not
//   depend on the number of types.
//
// TEST4: Faster computation of the same adjustment (Method and ActiveOnly).
//   RASMatrix.cs scales the cells themselves. The cells are objects in linked lists, each pass visits all
//   cells four times (sum and scaling for rows and for columns), and after each call all cells are sorted
//   to fold the table into a triangle. The three steps below change only the way it is computed:
//   4a  Method = Factors. The adjusted cell is always historical value * row factor * column factor. The
//       computation is therefore done only on the two vectors of factors. The matrix is stored in arrays
//       (CSR), each pass reads the cells twice, and the folding is fixed once and for all. The historical
//       values are never overwritten, so Reset() is superfluous. Pass by pass the figures are the same as
//       in RASMatrix.cs apart from rounding in the last decimal.
//   4b  Method = Symmetric. The matrix is symmetric, and the target is the same for rows and columns. Then
//       the adjusted table is also symmetric, and one vector d is enough: cell (i,j) = d[i] * value * d[j].
//       The types are taken one at a time, and d[i] is set so that the sum of type i matches, given the
//       current factors of the other types. Each pass reads the cells once. If the table only has cells
//       between a man and a woman, this is ordinary RAS on men x women. Cells with two of the same type
//       (on the diagonal) give a quadratic equation in d[i]. The result is the same as 4a when the
//       adjustment has been run to the end, but not pass by pass.
//   4c  ActiveOnly = true. The computation is done only on types with a positive target and on cells
//       between two such types, and only those cells are returned. This matters most in the mop-up
//       rounds, where few types are left, and when there are many types without persons in the pool.
//   4a-4c require that the matrix is symmetric and is folded (Triagonalize), and they always use the
//   historical values (like RestoreData = true).
//
// With RestoreData = false, RunToConvergence = false and Method = Cells the class behaves exactly like
// RASMatrix. This is used to check that Matching_test.cs with all changes switched off gives the same
// couples as Matching.cs.

using System;
using System.Collections.Generic;

namespace DreamSMILE.Numerical
{
  //TEST4: the way the adjustment is computed
  public enum ERasMethod
  {
    /// <summary>As in RASMatrix.cs: the cells are scaled directly (linked lists).</summary>
    Cells,
    /// <summary>4a: the computation is done on row and column factors in arrays. Same figures as Cells apart from rounding.</summary>
    Factors,
    /// <summary>4a+4b: one factor per type. Requires the same target for rows and columns.</summary>
    Symmetric
  }

  public class RASMatrix_test
  {

    #region Public fields
    public bool ConsoleMessage = false;
    public bool Triagonalize = true;

    //TEST4
    /// <summary>The way the adjustment is computed. Cells is the original one.</summary>
    public ERasMethod Method = ERasMethod.Cells;
    /// <summary>4c: compute only on types with a positive target, and return only cells between two such types (not with Method = Cells).</summary>
    public bool ActiveOnly = false;

    //TEST0
    /// <summary>true: Reset() restores the values the matrix was created with. false: as in RASMatrix.cs (all cells are set to 1).</summary>
    public bool RestoreData = true;

    //TEST1: settings for the stopping criterion
    /// <summary>true: run until the sums match. false: stop as in RASMatrix.cs (in the second pass).</summary>
    public bool RunToConvergence = true;
    /// <summary>Cap on the number of passes (one pass is a scaling of all rows and all columns).</summary>
    public int MaxPasses = 1000;
    /// <summary>Stop when the total absolute deviation in row and column sums is below this share of the total target.</summary>
    public double Tolerance = 1e-6;

    //TEST1: result of the latest call of RASAdjust()
    /// <summary>Number of passes in the latest call.</summary>
    public int Passes;
    /// <summary>Total deviation relative to the total target, measured in the last pass.</summary>
    public double Error;
    /// <summary>true if the latest call stopped because Error came below Tolerance.</summary>
    public bool Converged;
    /// <summary>Number of types with a positive target but without data in the latest call (the target is set to 0).</summary>
    public int TargetsWithoutData;
    #endregion

    #region Private fields
    RASMatrixCell[] _rows, _cols;
    List<string> _warnings;
    //TEST0: the cells and their values as they were when the matrix was created
    RASMatrixCell[] _cells;
    double[] _value0;
    //TEST4: the row and column of the cells when the matrix was created (RASAdjust() swaps them during the folding)
    int[] _row0, _col0;
    #endregion

    #region Constructors
    public RASMatrix_test(List<RASMatrixCell> lRas, int m, bool consMgs)
    {

      _warnings = new List<string>();
      ConsoleMessage = consMgs;

      //TEST0
      _cells = lRas.ToArray();
      _value0 = new double[_cells.Length];
      for (int i = 0; i < _cells.Length; i++)
        _value0[i] = _cells[i].Value;

      //TEST4
      _row0 = new int[_cells.Length];
      _col0 = new int[_cells.Length];
      for (int i = 0; i < _cells.Length; i++)
      {
        _row0[i] = _cells[i].Row;
        _col0[i] = _cells[i].Col;
      }

      int n = lRas.Count;
      _rows = new RASMatrixCell[m];
      _cols = new RASMatrixCell[m];
      if (ConsoleMessage) Console.WriteLine("Match> Antal non-zeroes: {0} = {1:#.#####} pct.", n, 100.0 * n / (m * m));

      // Sorter efter række
      if (ConsoleMessage) Console.WriteLine("Match> --> Sorterer rækker...");
      lRas.Sort(delegate(RASMatrixCell a, RASMatrixCell b)
      {
        int diff = a.Row - b.Row;
        if (diff != 0) return diff;
        return a.Col - b.Col;

      });

      // Laver række-struktur
      if (ConsoleMessage) Console.WriteLine("Match> --> Række-struktur...");

      int row = -1;
      for (int i = 0; i < n - 1; i++)
      {
        if (lRas[i].Row != row)
        {
          row = lRas[i].Row;
          _rows[row] = lRas[i];
        }
        if (lRas[i + 1].Row == row)
          lRas[i].NextCol = lRas[i + 1];

      }

      // Sorter efter søjler
      if (ConsoleMessage) Console.WriteLine("Match> --> Sorterer søjler...");
      lRas.Sort(delegate(RASMatrixCell a, RASMatrixCell b)
      {
        int diff = a.Col - b.Col;
        if (diff != 0) return diff;
        return a.Row - b.Row;

      });

      // Laver række-struktur
      if (ConsoleMessage) Console.WriteLine("Match> --> Søjle-struktur...");

      int col = -1;
      for (int i = 0; i < n - 1; i++)
      {
        if (lRas[i].Col != col)
        {
          col = lRas[i].Col;
          _cols[col] = lRas[i];
        }
        if (lRas[i + 1].Col == col)
          lRas[i].NextRow = lRas[i + 1];

      }
    }
    #endregion

    #region RASAdjust()

    public List<RASMatrixCell> RASAdjust(double[] rowTarget, double[] colTarget)
    {
      //TEST4
      if (Method != ERasMethod.Cells)
        return RASAdjustFast(rowTarget, colTarget);

      int n = _rows.Length;
            
      if (n != rowTarget.Length)
        throw new Exception("RASAdjust: Wrong dimension in rowTarget");

      if (n != colTarget.Length)
        throw new Exception("RASAdjust: Wrong dimension in colTarget");

      double colSum = 0, rowSum = 0;

      for (int i = 0; i < n; i++)
      {
        colSum += colTarget[i];
        rowSum += rowTarget[i];
      }

      if (Math.Abs(colSum - rowSum) > 0.0000001)
        throw new Exception("RASAdjust: Math.Abs(colSum - rowSum) > 0.0000001");

      double[] colSums = new double[n];

      double sum1 = 0, sum2 = 0;

      for (int i = 0; i < n; i++)
      {
        double sum = 0;
        for (RASMatrixCell c = _cols[i]; c != null; c = c.NextRow)
          sum += c.Value;


        colSums[i] = sum;
        sum1 += sum;
      }


      double[] rowSums = new double[n];

      for (int i = 0; i < n; i++)
      {
        double sum = 0;
        for (RASMatrixCell c = _rows[i]; c != null; c = c.NextCol)
          sum += c.Value;

        rowSums[i] = sum;
        sum2 += sum;
      }

      int nRowWarn = 0;
      // Sørg for at der er de samme 0'er i data og target 
      for (int i = 0; i < n; i++)
        if (rowSums[i] == 0 && rowTarget[i] > 0)
        {
          rowTarget[i] = 0;
          nRowWarn++;       
        }

      if(nRowWarn>0)
        _warnings.Add(String.Format("WARNING RASAdjust : {0} positive Targets hvor rowSum = 0. Sat til 0.", nRowWarn));

      int nColWarn = 0;
      for (int i = 0; i < n; i++)
        if (colSums[i] == 0 && colTarget[i] > 0)
        {
          colTarget[i] = 0;
          nColWarn++;        
        }

      if (nColWarn > 0)
        _warnings.Add(String.Format("WARNING RASAdjust : {0} positive Targets hvor colSum = 0. Sat til 0.", nColWarn));
        //Console.WriteLine("WARNING RASAdjust : {0} positive Targets hvor colSum = 0. Sat til 0.", nColWarn);

      //TEST1: makes the number visible to the caller (in RASMatrix.cs it ends up in _warnings, which is never printed)
      TargetsWithoutData = Math.Max(nRowWarn, nColWarn);

      sum1 = 0;
      sum2 = 0;
      for (int i = 0; i < n; i++)
      {
        sum1 += colTarget[i];
        sum2 += rowTarget[i];
      }

      // Tilpas summer
      if (Math.Abs(sum1 - sum2) > 0.000000001)
      {
        double sum = (sum1 + sum2) / 2;

        for (int i = 0; i < n; i++)
        {
          colTarget[i] = sum * colTarget[i] / sum1;
          rowTarget[i] = sum * rowTarget[i] / sum2;
        }
      }
      
      //RAS adjustment
      int ittMax = 1000;  //1000 oprindeligt
      int itt = 0;
      double errOK = 0.001;
      double err = 0;

      //TEST1
      double totalTarget = sum1 + sum2;        // total target for rows and columns
      double error = 0;                        // total deviation relative to the total target
      double previousError = double.MaxValue;  // the error in the previous pass
      Converged = false;

      while (true)
      {
        //Rækkeoperationer
        double r_ssq = 0;
        for (int i = 0; i < n; i++)
        {
          double sum = 0;
          for (RASMatrixCell c = _rows[i]; c != null; c = c.NextCol)
            sum += c.Value;

          if (sum > 0)
          {
            r_ssq += Math.Abs(rowTarget[i] - sum);
            double corr = rowTarget[i] / sum;

            for (RASMatrixCell c = _rows[i]; c != null; c = c.NextCol)
              c.Value *= corr;
          }
        }

        //Søjleoperationer
        double c_ssq = 0;
        for (int i = 0; i < n; i++)
        {
          double sum = 0;
          for (RASMatrixCell c = _cols[i]; c != null; c = c.NextRow)
            sum += c.Value;

          if (sum > 0)
          {
            c_ssq += Math.Abs(colTarget[i] - sum);
            double corr = colTarget[i] / sum;

            for (RASMatrixCell c = _cols[i]; c != null; c = c.NextRow)
              c.Value *= corr;
          }
        }

        //Test fejl
        
        err = (r_ssq + c_ssq) / n;
        //if (ConsoleMessage) Console.WriteLine("--> {0}", err);

        //TEST1: new stopping criterion
        error = totalTarget > 0 ? (r_ssq + c_ssq) / totalTarget : 0;
        if (RunToConvergence)
        {
          if (error < Tolerance) { Converged = true; break; }         // the sums match
          if (itt + 1 >= MaxPasses) break;                            // the cap has been reached
          if (previousError - error < 0.001 * Tolerance) break;       // the error no longer falls
          previousError = error;
          itt++;
        }
        else
        {
          // As in RASMatrix.cs, where "err - err0" is always 0, so the loop stops as soon as itt > 0
          if (err < errOK) break;
          if (itt > ittMax) break;
          if (itt > 0) break;
          itt++;
        }
      }

      //TEST1
      Passes = itt + 1;
      Error = error;

      if (ConsoleMessage) Console.WriteLine("RASAdjust : itt = {0}, err = {1}", itt, err);


      //double[,] x = new double[n, n];
      List<RASMatrixCell> lst = new List<RASMatrixCell>();
      
      // Fylder x ud
      for (int i = 0; i < n; i++)
        for (RASMatrixCell c = _cols[i]; c != null; c = c.NextRow)
          lst.Add(c);


      if (Triagonalize)
      {
        foreach (RASMatrixCell c in lst)
        {
          if (c.Row < c.Col)
          {
            int r = c.Row;
            c.Row = c.Col;
            c.Col = r;
          }        
        }

        //Sorter
        lst.Sort(delegate(RASMatrixCell a, RASMatrixCell b)
        {
          int diff = a.Col - b.Col;
          if (diff != 0) return diff;
          return a.Row - b.Row;
        });


        //Aggregerer celler
        for (int i = 1; i < lst.Count; i++)
        {
          if (lst[i].Col == lst[i - 1].Col && lst[i].Row == lst[i - 1].Row)
          {
            lst[i].Value += lst[i - 1].Value;
            lst[i - 1].Row = -1; // Markerer at den skal fjernes      
          }
        }

        lst.RemoveAll(x => x.Row == -1);  // Fjerner de markerede      
      }
      
      return lst;
    }
    #endregion

    #region TEST4: RASAdjustFast()

    // The matrix in CSR form (built at the first fast call): the cells in row i are no. _hStart[i] to _hStart[i+1]-1
    int[] _hStart;
    int[] _hCol;               // the column of the cell
    double[] _hValue;          // the historical value of the cell
    int[] _hFold;              // for cells with column <= row: the number of the folded cell in _folded. Otherwise -1
    RASMatrixCell[] _folded;   // the folded cells (Row >= Col), sorted by (Col, Row) as in the original method
    bool[] _hHasData;          // the type has at least one cell with a positive value
    bool _hSymmetric;
    int[] _compact;            // working number of a type, -1 when the type is not included

    /// <summary>The types and cells that are computed on in a call. The types are numbered consecutively.</summary>
    sealed class WorkMatrix
    {
      public int M;              // number of types
      public int[] Type;         // the number of the type in the full matrix
      public int[] Start;        // the cells of type a are no. Start[a] to Start[a+1]-1
      public int[] Col;          // the working number of the other type
      public int[] Fold;         // the number of the folded cell (only for Col <= a)
      public double[] Value;     // historical value
    }
    WorkMatrix _all;             // all types with data (built once)
    WorkMatrix _active;          // buffer for the active types in the current call (ActiveOnly)

    /// <summary>Puts the matrix into arrays and fixes the folding. Called once.</summary>
    void BuildFast()
    {
      int n = _rows.Length;
      int count = _cells.Length;

      // The cells are sorted by (row, column). Cells with the same row and column are added together
      long[] key = new long[count];
      int[] index = new int[count];
      for (int k = 0; k < count; k++)
      {
        key[k] = (long)_row0[k] * n + _col0[k];
        index[k] = k;
      }
      Array.Sort(key, index);

      List<int> col = new List<int>(count);
      List<double> val = new List<double>(count);
      _hStart = new int[n + 1];
      long previous = -1;
      int row = 0;
      for (int k = 0; k < count; k++)
      {
        if (key[k] == previous)
        {
          val[val.Count - 1] += _value0[index[k]];
          continue;
        }
        previous = key[k];
        int i = _row0[index[k]];
        while (row < i)
          _hStart[++row] = col.Count;
        col.Add(_col0[index[k]]);
        val.Add(_value0[index[k]]);
      }
      while (row < n)
        _hStart[++row] = col.Count;
      _hCol = col.ToArray();
      _hValue = val.ToArray();
      int nnz = _hCol.Length;

      // Is the matrix symmetric? And which types have data?
      _hSymmetric = true;
      _hHasData = new bool[n];
      for (int i = 0; i < n; i++)
        for (int k = _hStart[i]; k < _hStart[i + 1]; k++)
        {
          int j = _hCol[k];
          if (_hValue[k] > 0)
            _hHasData[i] = true;
          int mirror = Array.BinarySearch(_hCol, _hStart[j], _hStart[j + 1] - _hStart[j], i);
          if (mirror < 0 || Math.Abs(_hValue[mirror] - _hValue[k]) > 1e-9 * Math.Max(1.0, Math.Abs(_hValue[k])))
            _hSymmetric = false;
        }

      // The folding: cells (i,j) and (j,i) become one cell with Row = the larger and Col = the smaller type.
      // The folded cells are sorted by (Col, Row), as RASAdjust() does in the original method
      List<long> foldKey = new List<long>();
      List<int> foldCell = new List<int>();
      for (int i = 0; i < n; i++)
        for (int k = _hStart[i]; k < _hStart[i + 1]; k++)
          if (_hCol[k] <= i)
          {
            foldKey.Add((long)_hCol[k] * n + i);
            foldCell.Add(k);
          }
      long[] fn = foldKey.ToArray();
      int[] fc = foldCell.ToArray();
      Array.Sort(fn, fc);
      _hFold = new int[nnz];
      for (int k = 0; k < nnz; k++)
        _hFold[k] = -1;
      _folded = new RASMatrixCell[fc.Length];
      for (int f = 0; f < fc.Length; f++)
      {
        _hFold[fc[f]] = f;
        _folded[f] = new RASMatrixCell((int)(fn[f] % n), (int)(fn[f] / n), 0);
      }

      _compact = new int[n];
      for (int i = 0; i < n; i++)
        _compact[i] = -1;

      // The work matrix with all types that have data
      List<int> withData = new List<int>();
      for (int i = 0; i < n; i++)
        if (_hHasData[i])
          withData.Add(i);
      _all = new WorkMatrix();
      BuildWorkMatrix(_all, withData);
      _active = new WorkMatrix();
    }

    /// <summary>Fills w with the given types (ascending) and the cells between them.</summary>
    void BuildWorkMatrix(WorkMatrix w, List<int> types)
    {
      int m = types.Count;
      if (w.Type == null || w.Type.Length < m)
      {
        w.Type = new int[m];
        w.Start = new int[m + 1];
      }
      if (w.Col == null)
      {
        w.Col = new int[_hCol.Length];
        w.Fold = new int[_hCol.Length];
        w.Value = new double[_hCol.Length];
      }
      w.M = m;

      for (int a = 0; a < m; a++)
      {
        w.Type[a] = types[a];
        _compact[types[a]] = a;
      }

      int p = 0;
      for (int a = 0; a < m; a++)
      {
        int i = types[a];
        w.Start[a] = p;
        for (int k = _hStart[i]; k < _hStart[i + 1]; k++)
        {
          int b = _compact[_hCol[k]];
          if (b >= 0)
          {
            w.Col[p] = b;
            w.Fold[p] = _hFold[k];
            w.Value[p] = _hValue[k];
            p++;
          }
        }
      }
      w.Start[m] = p;

      for (int a = 0; a < m; a++)
        _compact[types[a]] = -1;
    }

    /// <summary>
    /// Same adjustment as RASAdjust(), computed on factors in arrays (see TEST4 at the top of the file). Returns
    /// the folded cells. The cells in the original structure are not touched.
    /// </summary>
    List<RASMatrixCell> RASAdjustFast(double[] rowTarget, double[] colTarget)
    {
      int n = _rows.Length;

      if (n != rowTarget.Length)
        throw new Exception("RASAdjust: Wrong dimension in rowTarget");

      if (n != colTarget.Length)
        throw new Exception("RASAdjust: Wrong dimension in colTarget");

      if (!Triagonalize)
        throw new Exception("RASAdjust: Method Factors and Symmetric require Triagonalize");

      if (_hStart == null)
        BuildFast();

      if (!_hSymmetric)
        throw new Exception("RASAdjust: Method Factors and Symmetric require a symmetric matrix");

      // Same treatment of the targets as in RASAdjust()
      double colSum = 0, rowSum = 0;
      for (int i = 0; i < n; i++)
      {
        colSum += colTarget[i];
        rowSum += rowTarget[i];
      }

      if (Math.Abs(colSum - rowSum) > 0.0000001)
        throw new Exception("RASAdjust: Math.Abs(colSum - rowSum) > 0.0000001");

      // Make sure that the data and the target have the same zeros
      int nRowWarn = 0, nColWarn = 0;
      for (int i = 0; i < n; i++)
        if (!_hHasData[i])
        {
          if (rowTarget[i] > 0) { rowTarget[i] = 0; nRowWarn++; }
          if (colTarget[i] > 0) { colTarget[i] = 0; nColWarn++; }
        }
      TargetsWithoutData = Math.Max(nRowWarn, nColWarn);

      double sum1 = 0, sum2 = 0;
      for (int i = 0; i < n; i++)
      {
        sum1 += colTarget[i];
        sum2 += rowTarget[i];
      }

      // Adjust the sums
      if (Math.Abs(sum1 - sum2) > 0.000000001)
      {
        double sum = (sum1 + sum2) / 2;

        for (int i = 0; i < n; i++)
        {
          colTarget[i] = sum * colTarget[i] / sum1;
          rowTarget[i] = sum * rowTarget[i] / sum2;
        }
      }
      double totalTarget = sum1 + sum2;

      // The types that are computed on
      WorkMatrix w = _all;
      if (ActiveOnly)
      {
        List<int> types = new List<int>();
        for (int i = 0; i < n; i++)
          if (_hHasData[i] && (rowTarget[i] > 0 || colTarget[i] > 0))
            types.Add(i);
        BuildWorkMatrix(_active, types);
        w = _active;
      }

      int m = w.M;
      int[] start = w.Start, col = w.Col;
      double[] val = w.Value;
      double[] tr = new double[m], tc = new double[m];
      for (int a = 0; a < m; a++)
      {
        tr[a] = rowTarget[w.Type[a]];
        tc[a] = colTarget[w.Type[a]];
      }

      // The stopping criterion is the same as in RASAdjust(). deviation is the sum of the absolute deviations in row and column sums
      int itt = 0;
      double error = 0;
      double previousError = double.MaxValue;
      Converged = false;
      bool Done(double deviation)
      {
        error = totalTarget > 0 ? deviation / totalTarget : 0;
        if (RunToConvergence)
        {
          if (error < Tolerance) { Converged = true; return true; }     // the sums match
          if (itt + 1 >= MaxPasses) return true;                        // the cap has been reached
          if (previousError - error < 0.001 * Tolerance) return true;   // the error no longer falls
          previousError = error;
          itt++;
          return false;
        }
        // As in RASMatrix.cs: stop in the second pass
        if (deviation / n < 0.001) return true;
        if (itt > 0) return true;
        itt++;
        return false;
      }

      double[] r = new double[m], c;
      for (int a = 0; a < m; a++)
        r[a] = 1;

      if (Method == ERasMethod.Symmetric)
      {
        // 4b: one factor per type
        for (int a = 0; a < m; a++)
          if (Math.Abs(tr[a] - tc[a]) > 1e-9 * Math.Max(1.0, tr[a]))
            throw new Exception("RASAdjust: Method Symmetric requires the same target for rows and columns");

        c = r;
        double[] d = r;
        while (true)
        {
          double dev = 0;
          for (int a = 0; a < m; a++)
          {
            double s = 0, diag = 0;
            for (int k = start[a]; k < start[a + 1]; k++)
            {
              int b = col[k];
              if (b == a)
                diag = val[k];
              else
                s += val[k] * d[b];
            }

            double sum = d[a] * (s + diag * d[a]);   // the sum of the type with the current factors
            if (sum > 0)
            {
              dev += Math.Abs(tr[a] - sum);
              // d[a] is set so that d[a] * (s + diag * d[a]) = the target
              if (tr[a] == 0)
                d[a] = 0;
              else if (diag == 0)
                d[a] = tr[a] / s;
              else
                d[a] = 2 * tr[a] / (s + Math.Sqrt(s * s + 4 * diag * tr[a]));
            }
          }

          // The deviation is measured once for each type. In RASAdjust() it is measured for both rows and columns
          if (Done(2 * dev)) break;
        }
      }
      else
      {
        // 4a: row and column factors. The matrix is symmetric, so column j has the same cells as row j
        c = new double[m];
        for (int a = 0; a < m; a++)
          c[a] = 1;

        while (true)
        {
          // Row operations
          double r_ssq = 0;
          for (int a = 0; a < m; a++)
          {
            double s = 0;
            for (int k = start[a]; k < start[a + 1]; k++)
              s += val[k] * c[col[k]];

            double sum = r[a] * s;
            if (sum > 0)
            {
              r_ssq += Math.Abs(tr[a] - sum);
              r[a] = tr[a] / s;
            }
          }

          // Column operations
          double c_ssq = 0;
          for (int a = 0; a < m; a++)
          {
            double s = 0;
            for (int k = start[a]; k < start[a + 1]; k++)
              s += val[k] * r[col[k]];

            double sum = c[a] * s;
            if (sum > 0)
            {
              c_ssq += Math.Abs(tc[a] - sum);
              c[a] = tc[a] / s;
            }
          }

          if (Done(r_ssq + c_ssq)) break;
        }
      }

      Passes = itt + 1;
      Error = error;

      // The folded cells: cells (a,b) and (b,a) added together
      List<RASMatrixCell> lst;
      if (ActiveOnly)
        lst = new List<RASMatrixCell>(start[m] / 2 + m);
      else
      {
        for (int f = 0; f < _folded.Length; f++)
          _folded[f].Value = 0;
        lst = new List<RASMatrixCell>(_folded);
      }

      for (int a = 0; a < m; a++)
        for (int k = start[a]; k < start[a + 1]; k++)
        {
          int b = col[k];
          if (b > a)
            continue;

          RASMatrixCell cell = _folded[w.Fold[k]];
          cell.Value = b == a ? val[k] * r[a] * c[a] : val[k] * (r[a] * c[b] + r[b] * c[a]);
          if (ActiveOnly)
            lst.Add(cell);
        }

      return lst;
    }
    #endregion

    #region Reset()
    public void Reset()
    {
      //TEST4: the fast methods do not change the cells
      if (Method != ERasMethod.Cells)
      {
        _warnings = new List<string>();
        return;
      }

      int n = _rows.Length;

      for (int i = 0; i < n; i++)
        for (RASMatrixCell c = _cols[i]; c != null; c = c.NextRow)
          c.Reset();

      //TEST0: c.Reset() has set Value to 1 (the value the cell was created with). Restore the counted values
      if (RestoreData)
        for (int i = 0; i < _cells.Length; i++)
          _cells[i].Value = _value0[i];

      _warnings = new List<string>();
    }
    #endregion

  }
}
