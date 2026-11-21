
namespace DreamSMILE.DemoSim
{
  [Serializable]
  public class Time
  {

    #region Private fields
    int _startYear, _endYear;
    int _t, _month;
    #endregion

    #region Constructors
    public Time(int startYear, int endYear)
    {
      _startYear = startYear;
      _endYear = endYear;
      _t = startYear;
      _month = 1;
    }
    #endregion

    #region NextYear()
    public bool NextYear()
    {
      _t++;
      if (_t > _endYear)
        return false;
      else
        return true;
    }
    #endregion

    #region NextMonth()
    public void NextMonth()
    {
      _month++;
      if (_month > 12)
        _month = 1;
    }
    #endregion

    #region Public properties
    /// <summary>
    /// First year in simulation
    /// </summary>
    public int StartYear
    {
      get { return _startYear; }
    }

    /// <summary>
    /// Last year in simulation
    /// </summary>
    public int EndYear
    {
      get { return _endYear; }
    }

    /// <summary>
    /// Current year in simulation
    /// </summary>
    public int Now
    {
      get { return _t; }
    }

    public int SinceStart
    {
      get { return _t - _startYear; }
    }
    #endregion

    /// <summary>
    /// Current month in simulation
    /// </summary>
    public int NowMonth
    {
      get { return _month; }
    }
  }
}