using DreamSMILE.SimulationNS;

namespace DreamSMILE.DemoSim
{
  /// <summary>
  /// 
  /// </summary>
  [Serializable]
  public class LogFile
  {

    #region Constants
    const string LINE = "--------------------------------------------------------------------------------";
    #endregion

    #region Public fields
    public int NumberOfWarnings;
    #endregion

    #region Private fields
    StreamWriter _file;
    //StreamWriter _test;
    //Simulation _simulation;
    string _fileName;
    #endregion

    #region Constructors
    public LogFile(string fileName, string title, string version)
    {
      _fileName = fileName;
      _file = new StreamWriter(fileName);
      _file.AutoFlush = true;

      this.WriteLine(LINE);
      //this.WriteLine(Misc.Space((80 - Title.Length) / 2) + Title);
      this.WriteLine("      " + title);
      this.WriteLine(LINE);
      this.WriteLine("{0,-15}{1}", "    Version", version);
      this.WriteLine("{0,-15}{1}", "    Date", DateTime.Now.ToShortDateString());
      this.WriteLine("{0,-15}{1}", "    Time", DateTime.Now.ToShortTimeString());
      this.WriteLine(LINE);
      this.WriteLine();
    }
    #endregion

    #region FillLine
    public void FillLine()
    {
      _file.WriteLine(LINE);
    }
    #endregion

    #region WriteLine()
    public void WriteLine(string format, params object[] arg)
    {
      _file.WriteLine(format, arg);
      _file.Flush();
    }

    public void WriteLine(string s)
    {
      _file.WriteLine(s);
      _file.Flush();
    }

    public void WriteLine(object o)
    {
      _file.WriteLine(o);
      _file.Flush();
    }

    public void WriteLine()
    {
      _file.WriteLine();
      _file.Flush();
    }
    #endregion//WriteLine

    #region Close()
    public void Close()
    {
      this.WriteLine();
      this.WriteLine("*********************************End of LogFile*********************************");
      _file.Close();
    }
    #endregion//Close

    #region Public properties
    /// <summary>
    /// Logfilens StreamWriter. Gør det muligt at skrive direkte i filen
    /// </summary>
    public StreamWriter StreamWriter
    {
      get { return _file; }
    }
    /// <summary>
    /// Logfilens navn
    /// </summary>
    public string FileName
    {
      get { return _fileName; }
    }    
    #endregion

  }
}
