using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace DreamSMILE.Numerical
{
  /// <summary>
  /// 
  /// </summary>
  public class RandomEvent : Random
  {
    public RandomEvent() : base() { }
    public RandomEvent(int seed) : base(seed) { }

    /// <summary>
    /// Returns true with probability prob 
    /// </summary>
    /// <param name="prob">Probability</param>
    /// <returns>True with probability prob </returns>
    public bool NextEvent(double prob)
    {
      return NextDouble() < prob ? true : false;
    }

    /// <summary>
    /// Returns i with probability prob[i]. If sum(prob[i])=0 then ifZeroReturn is returned. 
    /// </summary>
    /// <param name="prob">Array of probabilities</param>
    /// <param name="ifZeroReturn">Event to return if sum(prob[i])=0</param>
    /// <returns>Number of event (zero-based)</returns>
    public int NextEvent(double[] prob, int ifZeroReturn)
    {
      double sum = 0;
      for (int i = 0; i < prob.Length; i++) sum += prob[i];
      if (sum == 0)
        return ifZeroReturn;
      else
        return NextEvent(prob);

    }

    /// <summary>
    /// Returns i with probability prob[i]. It is assumed (but not tested) that sum(prob[i]) is equal to 1. 
    /// </summary>
    /// <param name="prob">Array of probabilities</param>
    /// <returns>Number of event (zero-based)</returns>
    public int NextEvent(double[] prob)
    {
      double sum = 0;
      double u = this.NextDouble();
          //  int[] pos = new int[prob.Length];

      for (int i = 0; i < prob.Length; i++)
      {
        sum += !Double.IsNaN(prob[i]) ? prob[i] : 0;
        //pos[i] = !Double.IsNaN(prob[i]) && prob[i] > 0 ? 1 : 0;
        if (u < sum) return i;
      }

      if(Math.Abs(sum - 1d) < 0.0000001)
                return (int)(prob.Length - 1);

      if(Double.IsNaN(sum))
                throw new Exception("Probabilities contain NaN-values. [sum=" + sum.ToString() + "]");
      else
            {
                //string s0 = "Array - values : ";
                //for(int i = 0; i < prob.Length; i++)
                //    s0 += prob[i] + "\t";

                //throw new Exception("Probabilities do not sum to 1 in NextEvent. [sum=" + sum.ToString() + "], while the sum of prob " + prob.Sum().ToString() + ", nb of positive entries: " + pos.Sum().ToString() + ", " + s0);
                throw new Exception("Probabilities do not sum to 1 in NextEvent. [sum=" + sum.ToString() + "]");
            }
            

      //return -1;

    }

    double _y1 = 0, _y2 = 0;
    bool _flag = false;
    /// <summary>
    /// Draws from a gaussian distribution with mean 0 and variance 1
    /// </summary>
    /// <returns>Double</returns>
    public double NextGaussian()
    {
      double x1, x2, w;

      if (_flag)
      {
        _flag = false;
        return _y2;
      }
      else
      {
        _flag = true;
        do
        {
          x1 = 2.0 * NextDouble() - 1.0;
          x2 = 2.0 * NextDouble() - 1.0;
          w = x1 * x1 + x2 * x2;
        } while (w >= 1.0);

        w = Math.Sqrt(-2 * Math.Log(w) / w);
        _y1 = x1 * w;
        _y2 = x2 * w;

        return _y1;

      }

    }

    /// <summary>
    /// Draws from a gaussian distribution with mean m and variance var
    /// </summary>
    /// <returns>Double</returns>
    public double NextGaussian(double m, double var)
    {
      return m + var * NextGaussian();
    }


    /// <summary>
    /// Draws from an expreame value distribution
    /// </summary>
    /// <returns>Double</returns>
    public double NextExtremeValue()
    {
      return -Math.Log(-Math.Log(NextDouble()));
    }

    /// <summary>
    /// Draws from a logistic distribution
    /// </summary>
    /// <returns>Double</returns>
    public double NextLogistic()
    {
      double my = NextDouble();
      return -Math.Log(my / (1 - my));
    }


    /// <summary>
    /// Returns true if bx + e > 0, where e is drawn from a gaussian distribution
    /// </summary>
    /// <param name="bx"></param>
    /// <returns>bool</returns>
    public bool NextProbitEvent(double bx)
    {
      return bx + NextGaussian() > 0 ? true : false;
    }
    /// <summary>
    /// Returns true if bx + e > 0, where e is drawn from a logistic distribution
    /// </summary>
    /// <param name="bx"></param>
    /// <returns>bool</returns>
    public bool NextLogitEvent(double bx)
    {
      return bx + NextLogistic() > 0 ? true : false;
    }

  }
}
