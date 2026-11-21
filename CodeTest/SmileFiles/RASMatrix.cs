using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;

namespace DreamSMILE.Numerical
{

  #region class RASMatrixCell
  public class RASMatrixCell
  {

    #region Public fields
    public RASMatrixCell NextRow, NextCol;
    public double Value;
    public int Row, Col;
    public Int64 Index;
    #endregion

    #region Private fields
    double _value0;
    int _row0, _col0;
    Int64 _index0 = 0;
    #endregion

    #region Constructors
    public RASMatrixCell(int row, int col, double value)
    {
      Row = row;
      Col = col;
      Value = value;
      _value0 = value;
      _row0 = row;
      _col0 = col;
    }

    public RASMatrixCell(int row, int col, Int64 indx, double value)
    {
      Row = row;
      Col = col;
      Value = value;
      Index = indx;
      _value0 = value;
      _index0 = indx;
    }
    #endregion

    #region Reset()
    public void Reset()
    {
      Value = _value0;
      Row = _row0;
      Col = _col0;
      Index = _index0;
    }
    #endregion

  }
  #endregion

  #region class RASMatrix
  public class RASMatrix
  {

    #region Public fields
    public bool ConsoleMessage = false;
    public bool Triagonalize = true;
    #endregion

    #region Private fields
    RASMatrixCell[] _rows, _cols;
    List<string> _warnings;
    #endregion

    #region Constructors
    public RASMatrix(List<RASMatrixCell> lRas, int m, bool consMgs)
    {

      _warnings = new List<string>();
      ConsoleMessage = consMgs;

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

      //StreamWriter file = new StreamWriter("\\SRVFSEDREAM1\Fdrev\\SSDWork\\703255\\PSP\\Test\\test3.txt");

      //foreach (var c in lRas)
      //  file.WriteLine("{0}\t{1}\t{2}", c.Col, c.Row, c.Value);

      //file.Close();



      return;

#if false

      if (iRow.Length != iCol.Length)
        throw new Exception("Error in RASMatrix constructor. Length of iRow and iCol should be the same.");



      if (ConsoleMessage) Console.WriteLine("--> Laver skelet...");
      RASMatrixCell[,] cells = new RASMatrixCell[n, n];
      _rows = new RASMatrixCell[n];
      _cols = new RASMatrixCell[n];

      for (int i = 0; i < n; i++)
        for (int j = 0; j < n; j++)
        {
          if (x[i, j] > 0)
            cells[i, j] = new RASMatrixCell(i, j, x[i, j]);

        }

      if (ConsoleMessage) Console.WriteLine("--> Laver række-struktur...");
      //Row structure
      for (int i = 0; i < n; i++)
      {
        int next = 0;
        RASMatrixCell lastCell;

        while (next < n && cells[i, next] == null) next++;

        if (next == n) goto Videre1;

        _rows[i] = cells[i, next];
        lastCell = cells[i, next];
        next++;

      Igen1:
        while (next < n && cells[i, next] == null) next++;

        if (next < n)
        {
          lastCell.NextCol = cells[i, next];
          lastCell = cells[i, next];
          next++;
          goto Igen1;
        }

      Videre1:
        int dummy = 1;

      }


      // Col structure
      if (ConsoleMessage) Console.WriteLine("--> Laver søjle-struktur...");
      for (int i = 0; i < n; i++)
      {
        int next = 0;
        RASMatrixCell lastCell;

        while (next < n && cells[next, i] == null) next++;

        if (next == n) goto Videre2;


        _cols[i] = cells[next, i];
        lastCell = cells[next, i];
        next++;

      Igen2:
        while (next < n && cells[next, i] == null) next++;

        if (next < n)
        {
          lastCell.NextRow = cells[next, i];
          lastCell = cells[next, i];
          next++;
          goto Igen2;
        }

      Videre2:
        int dummy = 1;



      }
#endif
    }
    
    public RASMatrix(double[,] x, bool consMgs)
    {

      _warnings = new List<string>();
      ConsoleMessage = consMgs;

      if (x.GetLength(0) != x.GetLength(1))
        throw new Exception("Error in RASMatrix constructor. Input-matrix should be symmetric.");

      int n = x.GetLength(0);

      if (ConsoleMessage) Console.WriteLine("--> Laver skelet...");
      RASMatrixCell[,] cells = new RASMatrixCell[n, n];
      _rows = new RASMatrixCell[n];
      _cols = new RASMatrixCell[n];

      for (int i = 0; i < n; i++)
        for (int j = 0; j < n; j++)
        {
          if (x[i, j] > 0)
            cells[i, j] = new RASMatrixCell(i, j, x[i, j]);

        }

      if (ConsoleMessage) Console.WriteLine("--> Laver række-struktur...");
      //Row structure
      for (int i = 0; i < n; i++)
      {
        int next = 0;
        RASMatrixCell lastCell;

        while (next < n && cells[i, next] == null) next++;

        if (next == n) goto Videre1;

        _rows[i] = cells[i, next];
        lastCell = cells[i, next];
        next++;

      Igen1:
        while (next < n && cells[i, next] == null) next++;

        if (next < n)
        {
          lastCell.NextCol = cells[i, next];
          lastCell = cells[i, next];
          next++;
          goto Igen1;
        }

      Videre1:
        int dummy = 1;

      }


      // Col structure
      if (ConsoleMessage) Console.WriteLine("--> Laver søjle-struktur...");
      for (int i = 0; i < n; i++)
      {
        int next = 0;
        RASMatrixCell lastCell;

        while (next < n && cells[next, i] == null) next++;

        if (next == n) goto Videre2;


        _cols[i] = cells[next, i];
        lastCell = cells[next, i];
        next++;

      Igen2:
        while (next < n && cells[next, i] == null) next++;

        if (next < n)
        {
          lastCell.NextRow = cells[next, i];
          lastCell = cells[next, i];
          next++;
          goto Igen2;
        }

      Videre2:
        int dummy = 1;



      }

    }
    #endregion

    #region NSum()
    public double[] NSum()
    {
      int n = _rows.Length;
      double[] nSums = new double[n];

      NSum(nSums);

      //for (int i = 0; i < n; i++)
      //{
      //  double sum = 0;
      //  for (RASMatrixCell r = _rows[i]; r != null; r = r.NextCol)
      //    sum += r.Value;

      //  if (_cols[i] != null)
      //    for (RASMatrixCell c = _cols[i].NextRow; c != null; c = c.NextRow)
      //      sum += c.Value;

      //  nSums[i] = sum;

      //}


      return nSums;

    }

    public void NSum(double[] nSums)
    {
      int n = _rows.Length;

      for (int i = 0; i < n; i++)
      {
        double sum = 0;
        for (RASMatrixCell r = _rows[i]; r != null; r = r.NextCol)
          sum += r.Value;

        if (_cols[i] != null)
          for (RASMatrixCell c = _cols[i].NextRow; c != null; c = c.NextRow)
            sum += c.Value;

        nSums[i] = sum;

      }

    }
    #endregion

    #region RASAdjust()

    public List<RASMatrixCell> RASAdjust(double[] rowTarget, double[] colTarget)
    {
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
        double err0 = err;
        if (err < errOK) break;
        if (itt > ittMax) break;
        if (itt > 0 && err - err0 < 0.00001) break;
        itt++;
      }

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

    public double[,] RASAdjust_OLD(double[] rowTarget, double[] colTarget)
    {
      int n = _rows.Length;

      if (n != rowTarget.Length)
        throw new Exception("Wrong dimension in rowTarget");

      if (n != colTarget.Length)
        throw new Exception("Wrong dimension in colTarget");

      double colSum = 0, rowSum = 0;

      for (int i = 0; i < n; i++)
      {
        colSum += colTarget[i];
        rowSum += rowTarget[i];
      }

      if (Math.Abs(colSum - rowSum) > 0.0000001)
        throw new Exception("Math.Abs(colSum - rowSum) > 0.0000001");


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


      // Sørg for at der er de samme 0'er i data og target 
      for (int i = 0; i < n; i++)
        if (rowSums[i] == 0 && rowTarget[i] > 0)
          rowTarget[i] = 0;

      for (int i = 0; i < n; i++)
        if (colSums[i] == 0 && colTarget[i] > 0)
          colTarget[i] = 0;

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

      int ittMax = 50;
      int itt = 0;
      double errOK = 0.0000001;

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
        double err = (r_ssq + c_ssq) / n;
        if (ConsoleMessage) Console.WriteLine("--> {0}", err);

        if (err < errOK) break;
        if (itt > ittMax) break;
        itt++;
      }

      double[,] x = new double[n, n];

      // Fylder x ud
      for (int i = 0; i < n; i++)
        for (RASMatrixCell c = _cols[i]; c != null; c = c.NextRow)
          x[c.Row, c.Col] = c.Value;

      return x;
    }
    #endregion

    #region Reset()
    public void Reset()
    {
      int n = _rows.Length;

      for (int i = 0; i < n; i++)
        for (RASMatrixCell c = _cols[i]; c != null; c = c.NextRow)
          c.Reset();

      _warnings = new List<string>();
    }
    #endregion

  }
  #endregion
}