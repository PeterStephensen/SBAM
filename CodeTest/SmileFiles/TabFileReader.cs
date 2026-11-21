using System;
using System.Collections;
using System.Data;
using System.Data.Odbc;
using System.Globalization;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;

namespace DreamSMILE.IO
{

  #region enum TabFileOption
  public enum TabFileOption
  {
    /// <summary>Calculate the number of obsevations in the file. </summary>		
    CalcNumberOfObs = 1,
    /// <summary>Transpose file before reading</summary>
    Transpose = 2,
    IgnoreQuotes = 4,
    //Option4=8,
    //Option5=16,	
    //Option6=32,
  }
  #endregion
  /// <summary>
  /// Implements a DREAM.IO.TabFileReader
  /// </summary>
  public class TabFileReader
  {

    #region Constants
    const string TMP_FILE = "~TabFileReader.tmp";
    #endregion

    #region Public fields
    public string Missing = "";
    public CultureInfo CultureInfo;
    public int MaxScanRows = 1000;
    #endregion

    #region Private fields
    StreamReader _file;
    string[] _keyword;
    string[] _element;
    string _line, _firstline;
    int _nLines = 0;
    string _fileName;
    int _maxGrpSize = 100;
    int _groupSize = 0, _nGroup = 0;
    Int64 _next_group = 0;
    string _group;
    string[] _last_element;
    string _last_line;
    string[][] _group_element;
    string[] _group_line;
    Hashtable _hashTable;
    bool ignoreQuotes = false;

    #region Missing Values
    Int64 _missingInt64 = Int64.MinValue;
    Int32 _missingInt32 = Int32.MinValue;
    Int16 _missingInt16 = Int16.MinValue;
    Byte _missingByte = Byte.MinValue;
    SByte _missingSByte = SByte.MinValue;
    double _missingDouble = Double.MinValue;
    char _missingChar = (char)0;
    string _missingString = "";
    #endregion

    #endregion

    #region Constructors
    public TabFileReader(string FileName) : this(FileName, 0) { }

    public TabFileReader(string FileName, TabFileOption flags) : this(FileName, flags, "") { }

    public TabFileReader(string FileName, string headerLine) : this(FileName, 0, headerLine) { }

    public TabFileReader(string fileNameWithPath, TabFileOption flags, string headerLine)
    {
      bool isZipped = false;
      if (fileNameWithPath.EndsWith(".zip") || fileNameWithPath.EndsWith(".tabx"))
      {
                
                isZipped = true;
                #region set paths
                //TTH: dette er en zippet tab-fil
                //Vi unzipper den, lægger den et midlertidigt sted og læser den så ind.
                //Det midlertidige sted er System.Windows.Forms.Application.LocalUserAppDataPath + "\\TempTabFiles, dvs. f.eks.
                //C:\Users\tth-dream\AppData\Local\DREAM\Dream.Demographics.Simulation.Education.UI\1.0.0.0\TempTabFiles.
                //For at det midlertidige sted/mappe ikke langsomt bliver fyldt op af store tab-filer, slettes indholdet af mappen først (hvis nu der ligger noget
                //fra sidste gang).
                string pathToTemporaryLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "\\TempTabFiles");// System.Windows.Forms.Application.LocalUserAppDataPath + "\\TempTabFiles";

        // Determine whether the directory exists.
        if (!Directory.Exists(pathToTemporaryLocation))
        {
          Directory.CreateDirectory(pathToTemporaryLocation);
        }
        //Directory.SetCurrentDirectory(path);
                
        string[] fileList = Directory.GetFiles(pathToTemporaryLocation, "*.*");
        foreach (string img in fileList)
        {
          FileInfo imgInfo = new FileInfo(img);
          imgInfo.Delete();
        }

        //TTH: En tabfil på 266 MB tager ca. 14 s at pakke ned til 67 MB med 7zip. Tager ca. 4 s at pakke ud igen. At den pakker så hurtigt er fordi den er sat til
        //at pakke hurtigt (og ikke nødvendigvis småt): CompressionLevel.Fast. Med CompressionLevel.Normal pakkes en del mere ned (til 52 MB), men det tager 1.5 
        //minutter, og det synes jeg er for meget for at gå fra 67 til 52 MB. Med 67 MB er vi allerede nede på 25% af den oprindelige fil.
        //TTH: Der skal peges på en .dll fra 7zip. Vi hopper tre mappeniveauer op, for at komme fra ...\bin\Debug til roden (hvor .sln-filen ligger). Derefter gås to mapper ned i \Diverse\ExternalUtilities
        //string pathTo7Zip = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(Application.StartupPath))) + "\\Diverse\\ExternalUtilities\\7z.dll";
                //string pathTo7Zip = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "\\Diverse\\ExternalUtilities\\7z.dll");
                

        //SevenZipExtractor.SetLibraryPath(pathTo7Zip);

        //if (false)  //dette er nedzipning
        //{
        //  var tmp = new SevenZipCompressor();
        //  tmp.ArchiveFormat = OutArchiveFormat.Zip;
        //  tmp.CompressionLevel = CompressionLevel.Fast;  //svarer til '1', hvor '0' er uden komprimering (ren kopiering). '0' er selvfølgelig hurtigst, men vi vil da gerne have filerne mindre nu vi er i gang med en zip-maskine.        
        //  tmp.CompressFiles(fileNameWithPath, pathToTemporaryLocation);
        //}

        string fileNameWithoutPathStripped = "";        
        string fileNameWithoutPath = Path.GetFileName(fileNameWithPath);
                

                int truncate = -12345;
        if (fileNameWithoutPath.EndsWith(".zip", true, null))
        {
          truncate = 4;
        }
        else if (fileNameWithoutPath.EndsWith(".tabx", true, null))
        {
          truncate = 5;
        }
        else throw new Exception("Hov!");
        fileNameWithoutPathStripped = fileNameWithoutPath.Substring(0, fileNameWithoutPath.Length - truncate);
                #endregion set paths

                //Extract compressed file and place in temp path
                ExtractWithSharpCompress(fileNameWithPath, fileNameWithoutPathStripped, pathToTemporaryLocation);

      //  using (SevenZipExtractor tmp2 = new SevenZipExtractor(fileNameWithPath))
      //  {
      //    string tabFile = "";
      //    for (int i = 0; i < tmp2.ArchiveFileData.Count; i++)
      //    {
      //      string fileName2 = tmp2.ArchiveFileNames[i];
      //      Console.WriteLine(fileName2);
      //      if (string.Compare(fileNameWithoutPathStripped, fileName2, true) == 0)
      //      {
      //        tabFile = fileName2;
      //      }
      //    }
      //    if (tabFile == "") throw new Exception("*** ERROR: TabFileReader cannot find tab-file inside zip-file. Expected to find '" + fileNameWithoutPathStripped + "' inside '" + fileNameWithPath + "'");
          
      //    for (int i = 0; i < tmp2.ArchiveFileData.Count; i++)
      //    {            
      //      tmp2.ExtractFiles(pathToTemporaryLocation, tmp2.ArchiveFileData[i].Index);
      //    }          
      //  }
      //  _fileName = pathToTemporaryLocation + "\\" + fileNameWithoutPathStripped;
      }
      
      //this.CultureInfo = CultureInfo.CurrentCulture;  //TT: dette er FARLIGT mht. decimalkomma. Sættes til invariant, hvor "." altid er decimal-adskiller uanset lokale settings i Vista.
      this.CultureInfo = CultureInfo.InvariantCulture;

      //_hashTable = new Hashtable( new CaseInsensitiveHashCodeProvider(), new CaseInsensitiveComparer());//StringComparer.CurrentCultureIgnoreCase
      _hashTable = new Hashtable(StringComparer.CurrentCultureIgnoreCase);

      if (!isZipped) _fileName = fileNameWithPath;  //TT: Hvis den er zippet, er filnavnet for den udpakkede fil blevet sat ovenfor

      if (isFlag(flags, TabFileOption.Transpose))
      {
        _fileName = System.Environment.GetEnvironmentVariable("TEMP") + TMP_FILE;
        Transpose(fileNameWithPath, _fileName);
      }
      if (isFlag(flags, TabFileOption.IgnoreQuotes))
      {
        ignoreQuotes = true;  //in order to remember this setting when reading afterwards
      }

            #region after unzipped
            try
            {
        _file = new StreamReader(_fileName);
      }
      catch (FileNotFoundException e)
      {
        throw e;
      }
      // Calc number of lines
      _nLines = 0;
      if (isFlag(flags, TabFileOption.CalcNumberOfObs))
      {
        while (_file.ReadLine() != null) _nLines++;
        _file.BaseStream.Seek(0, SeekOrigin.Begin);
      }

      // Read 1. line and parse it for keywords

      if (headerLine != "")
        _firstline = headerLine;
      else
      {
        _firstline = _file.ReadLine();        
      }
      if (ignoreQuotes) _firstline = _firstline.Replace("\"", "");
      while (_firstline[0] == '*') _firstline = _file.ReadLine();   // Allow for comments (starting with *)
      _keyword = _firstline.Split('\t');      
      for (int i = 0; i < _keyword.Length; i++) _hashTable[_keyword[i]] = i;

            #endregion after unzippet
        }


        #region Old-style

        //public TabFileReader(string FileName) : this(FileName, true, false){}

        // Old-style
        public TabFileReader(string FileName, bool bCalcNumOfObs) : this(FileName, bCalcNumOfObs, false) { }

    // Old-style
    public TabFileReader(string FileName, bool bCalcNumOfObs, bool bTransposed)
    {
      this.CultureInfo = CultureInfo.CurrentCulture;

            //_hashTable = new Hashtable(new CaseInsensitiveHashCodeProvider(), new CaseInsensitiveComparer());
            _hashTable = new Hashtable(StringComparer.CurrentCultureIgnoreCase);
            _fileName = FileName;

      if (bTransposed)
      {
        _fileName = System.Environment.GetEnvironmentVariable("TEMP") + TMP_FILE;
        Transpose(FileName, _fileName);
      }

      try
      {
        _file = new StreamReader(_fileName);
      }
      catch { throw; }
      // Calc number of lines
      _nLines = 0;
      if (bCalcNumOfObs)
      {
        while (_file.ReadLine() != null) _nLines++;
        _file.BaseStream.Seek(0, SeekOrigin.Begin);
      }

      // Read 1. line and parse it for keywords
      _firstline = _file.ReadLine();
      while (_firstline[0] == '*') _firstline = _file.ReadLine();   // Allow for comments (starting with *)
      _keyword = _firstline.Split('\t');
      for (int i = 0; i < _keyword.Length; i++) _hashTable[_keyword[i]] = i;
    }
    #endregion
    #endregion

    #region Public Methods
    #region Group-related methods

    /// <summary>
    /// Set the name of the keyword that ReadGroup() use
    /// </summary>
    /// <param name="keyword">A valid keyword</param>
    public void SetGroup(string keyword)
    {
      _group_element = new String[_maxGrpSize][];
      _group_line = new String[_maxGrpSize];
      _group = keyword;
      //_useGroup = true;
    }

    /// <summary>
    ///	Read lines as long as keyword defined by SetGroup is unchanged	 
    /// </summary>
    /// <returns>False if End Of File</returns>
    public bool ReadGroup()
    {
      if (_next_group == -1)
      {
        _next_group = 0;
        return false;
      }

      //int g = 0, g0 = _next_group;
      Int64 g = 0, g0 = _next_group;

      if (_nGroup == 0)  // First time
      {
        this.ReadLine();
        this.Get(_group, out g0);
        _group_element[0] = _element;
        _group_line[0] = _line;
      }
      else
      {
        _group_element[0] = _last_element;
        _group_line[0] = _last_line;
      }


      _groupSize = 1;
      _nGroup++;
      while (this.ReadLine())
      {
        this.Get(_group, out g);

        if (g == g0)
        {
          _group_element[_groupSize] = _element;
          _group_line[_groupSize] = _line;
        }
        else
        {
          _last_element = _element;
          _last_line = _line;
          _element = _group_element[0];  // Dette sikrer at scalar-Get kan bruges
          _line = _group_line[0];  // Dette sikrer at scalar-Get kan bruges
          _next_group = g;
          return true;
        }

        _groupSize++;
        if (_groupSize == _maxGrpSize) throw new ArgumentOutOfRangeException("Group too big. _maxGroupSize=" + _maxGrpSize.ToString());
        g0 = g;
      }

      // EOF
      _element = _group_element[0];  // Dette sikrer at scalar-Get kan bruges
      _line = _group_line[0];  // Dette sikrer at scalar-Get kan bruges
      _next_group = -1;
      return true;
      //return false;
    }
    #endregion

    #region ReadLine(), Rewind(), Close()
    /// <summary>
    /// Reads and parses the next line
    /// </summary>
    public bool ReadLine()
    {
      _line = _file.ReadLine();
      if (_line == null) return false;
      if (ignoreQuotes) _line = _line.Replace("\"", "");
      _element = _line.Split('\t');
      return true;
    }

    /// <summary>
    /// Rewind the file to the beginning
    /// </summary>
    public void Rewind()
    {
      _file.BaseStream.Seek(0, SeekOrigin.Begin);
      _file.ReadLine();
    }

    /// <summary>
    /// Close the file
    /// </summary>
    public void Close()
    {
      FileInfo fi = new FileInfo(_fileName);
      FileInfo fi1 = new FileInfo(fi.DirectoryName + "\\schema.ini");
      if (fi1.Exists) fi1.Delete();

      if (_file != null)
      {
          _file.Close();
          _file.Dispose();
      }
    }



    #endregion

    #region Get...
    #region GetHandle
    public int GetHandle(string keyword)
    {
      object o = _hashTable[keyword];
      if (o == null)  throw new ArgumentException(keyword + " is unknown keyword.");

      return (int)o;

    }
    #endregion

    #region TryGetHandle
    public int TryGetHandle(string keyword)
    {
      object o = _hashTable[keyword];

      if (o == null) return -1;
      return (int)o;

    }
    #endregion

    #region GetString
    public string GetString(string keyword)
    {
      return _element[GetHandle(keyword)];
    }

    public string GetString(int handle)
    {
      return _element[handle];
    }
    #endregion

    #region Get()
    #region Scalar
    public void Get(string keyword, out string ret)
    {
      ret = _element[GetHandle(keyword)];
    }

    public void Get(string keyword, out char ret)
    {
      int j = GetHandle(keyword);
      ret = (_element[j] != "") ? _element[j].ToCharArray()[0] : MissingChar;
    }

    public void Get(string keyword, out byte ret)
    {
      int j = GetHandle(keyword);
      ret = (_element[j] != "") ? Convert.ToByte(_element[j], this.CultureInfo) : MissingByte;
    }

    public void Get(string keyword, out sbyte ret)
    {
      int j = GetHandle(keyword);
      ret = (_element[j] != "") ? Convert.ToSByte(_element[j], this.CultureInfo) : MissingSByte;
    }

    public void Get(string keyword, out int ret)
    {
      int j = GetHandle(keyword);
      ret = (_element[j] != "") ? Convert.ToInt32(_element[j], this.CultureInfo) : MissingInt32;
    }
    public void Get(string keyword, out Int64 ret)
    {
      int j = GetHandle(keyword);
      ret = (_element[j] != "") ? Convert.ToInt64(_element[j], this.CultureInfo) : MissingInt64;
    }

    public void Get(string keyword, out Int16 ret)
    {
      int j = GetHandle(keyword);
      ret = (_element[j] != "") ? Convert.ToInt16(_element[j], this.CultureInfo) : MissingInt16;
    }

    public void Get(string keyword, out double ret)
    {
      int j = GetHandle(keyword);
      ret = (_element[j] != "") ? Convert.ToDouble(_element[j], this.CultureInfo) : MissingDouble;
    }
    #endregion

    #region Array
    #region Get - original
    public void Get(string keyword, string[] ret)
    {
      int j = GetHandle(keyword);
      for (int i = 0; i < _groupSize; i++)
        ret[i] = _group_element[i][j];
    }

    public void Get(string keyword, char[] ret)
    {
      int j = GetHandle(keyword);
      for (int i = 0; i < _groupSize; i++)
        ret[i] = _group_element[i][j].ToCharArray()[0];
    }

    public void Get(string keyword, byte[] ret)
    {
      int j = GetHandle(keyword);
      for (int i = 0; i < _groupSize; i++)
        ret[i] = (_group_element[i][j] != "") ? Convert.ToByte(_group_element[i][j], this.CultureInfo) : MissingByte;
    }

    public void Get(string keyword, sbyte[] ret)
    {
      int j = GetHandle(keyword);
      for (int i = 0; i < _groupSize; i++)
        ret[i] = (_group_element[i][j] != "") ? Convert.ToSByte(_group_element[i][j], this.CultureInfo) : MissingSByte;
    }

    public void Get(string keyword, int[] ret)
    {
      int j = GetHandle(keyword);
      for (int i = 0; i < _groupSize; i++)
        ret[i] = (_group_element[i][j] != "") ? Convert.ToInt32(_group_element[i][j], this.CultureInfo) : MissingInt32;

    }

    public void Get(int handle, int[] ret)
    {
      int j = handle;
      for (int i = 0; i < _groupSize; i++)
        ret[i] = (_group_element[i][j] != "" && _group_element[i][j] != "NA") ? Convert.ToInt32(_group_element[i][j], this.CultureInfo) : MissingInt32;

    }

    public void Get(string keyword, Int64[] ret)
    {
      int j = GetHandle(keyword);
      for (int i = 0; i < _groupSize; i++)
      {
        ret[i] = (_group_element[i][j] != "") ? Convert.ToInt64(_group_element[i][j], this.CultureInfo) : MissingInt64;
      }

    }

    public void Get(string keyword, Int16[] ret)
    {
      int j = GetHandle(keyword);
      for (int i = 0; i < _groupSize; i++)
        ret[i] = (_group_element[i][j] != "") ? Convert.ToInt16(_group_element[i][j], this.CultureInfo) : MissingInt16;
    }

    public void Get(string keyword, double[] ret)
    {

      int j = GetHandle(keyword);
      for (int i = 0; i < _groupSize; i++)
        ret[i] = (_group_element[i][j] != "") ? Convert.ToDouble(_group_element[i][j], this.CultureInfo) : MissingDouble;
    }

    #region TryGet
    public void TryGet(string keyword, int[] ret)
    {
      bool kk;

      TryGet(keyword, ret, out kk);
    }

    public void TryGet(string keyword, int[] ret, out bool ok)
    {
      int j = TryGetHandle(keyword);
      if (j > -1)
      {
        ok = true;
        for (int i = 0; i < _groupSize; i++)
          ret[i] = (_group_element[i][j] != "") ? Convert.ToInt32(_group_element[i][j], this.CultureInfo) : MissingInt32;
      }
      else
      {
        ok = false;
        for (int i = 0; i < _groupSize; i++)
          ret[i] = MissingInt32;

      }
    }
    #endregion


    #endregion

    #region Get - out-version

    public void Get(string keyword, out string[] ret)
    {
      ret = new string[_groupSize];
      int j = GetHandle(keyword);
      for (int i = 0; i < _groupSize; i++)
        ret[i] = _group_element[i][j];
    }

    public void Get(string keyword, out char[] ret)
    {
      ret = new char[_groupSize];
      int j = GetHandle(keyword);
      for (int i = 0; i < _groupSize; i++)
        ret[i] = _group_element[i][j].ToCharArray()[0];
    }

    public void Get(string keyword, out byte[] ret)
    {
      ret = new byte[_groupSize];
      int j = GetHandle(keyword);
      for (int i = 0; i < _groupSize; i++)
        ret[i] = (_group_element[i][j] != "") ? Convert.ToByte(_group_element[i][j], this.CultureInfo) : MissingByte;
    }

    public void Get(string keyword, out sbyte[] ret)
    {
      ret = new sbyte[_groupSize];
      int j = GetHandle(keyword);
      for (int i = 0; i < _groupSize; i++)
        ret[i] = (_group_element[i][j] != "") ? Convert.ToSByte(_group_element[i][j], this.CultureInfo) : MissingSByte;
    }

    public void Get(string keyword, out int[] ret)
    {
      ret = new int[_groupSize];
      int j = GetHandle(keyword);
      for (int i = 0; i < _groupSize; i++)
        ret[i] = (_group_element[i][j] != "") ? Convert.ToInt32(_group_element[i][j], this.CultureInfo) : MissingInt32;

    }

    public void Get(string keyword, out Int64[] ret)
    {
      ret = new Int64[_groupSize];
      int j = GetHandle(keyword);
      for (int i = 0; i < _groupSize; i++)
      {
        ret[i] = (_group_element[i][j] != "") ? Convert.ToInt64(_group_element[i][j], this.CultureInfo) : MissingInt64;
      }
    }

    public void Get(string keyword, out Int16[] ret)
    {
      ret = new Int16[_groupSize];
      int j = GetHandle(keyword);
      for (int i = 0; i < _groupSize; i++)
        ret[i] = (_group_element[i][j] != "") ? Convert.ToInt16(_group_element[i][j], this.CultureInfo) : MissingInt16;
    }

    public void Get(string keyword, out double[] ret)
    {
      ret = new double[_groupSize];
      int j = GetHandle(keyword);
      for (int i = 0; i < _groupSize; i++)
        ret[i] = (_group_element[i][j] != "") ? Convert.ToDouble(_group_element[i][j], this.CultureInfo) : MissingDouble;
    }
    
    #endregion

    #region GetArray

    public string[] GetStringArray(string keyword)
    {
      string[] ret;
      Get(keyword, out ret);
      return ret;        
    }

    public char[] GetCharArray(string keyword)
    {
      char[] ret;
      Get(keyword, out ret);
      return ret;
    }

    public byte[] GetByteArray(string keyword)
    {
      byte[] ret;
      Get(keyword, out ret);
      return ret;
    }

    public sbyte[] GetSByteArray(string keyword)
    {
      sbyte[] ret;
      Get(keyword, out ret);
      return ret;
    }

    public int[] GetInt32Array(string keyword)
    {
      int[] ret;
      Get(keyword, out ret);
      return ret;
    }

    public Int64[] GetInt64Array(string keyword)
    {
      Int64[] ret;
      Get(keyword, out ret);
      return ret;
    }

    public Int16[] GetInt16Array(string keyword)
    {
      Int16[] ret;
      Get(keyword, out ret);
      return ret;
    }

    public double[] GetDoubleArray(string keyword)
    {
      double[] ret;
      Get(keyword, out ret);
      return ret;
    }
    
    #endregion

    #endregion

    #region GetChar
    public char GetChar(string keyword)
    {
      string s = GetString(keyword);
      //if (ignoreQuotes) s = s.Replace("\"", "");
      if (s.Trim().Length == 0) return ' ';
      return s.Trim().ToCharArray()[0];
    }

    public char GetChar(int handle)
    {
      string s = GetString(handle);
      //if (ignoreQuotes) s = s.Replace("\"", "");
      if (s.Trim().Length == 0) return ' ';
      return s.Trim().ToCharArray()[0];
    }
    #endregion

    #region GetByte
    public byte GetByte(string keyword)
    {
      string s = GetString(keyword);
      //if (ignoreQuotes) s = s.Replace("\"", "");      
      if (s.Trim() == "") return _missingByte;

      return Convert.ToByte(s, this.CultureInfo);
    }

    public byte GetByte(int handle)
    {
      string s = GetString(handle);
      //if (ignoreQuotes) s = s.Replace("\"", "");
      if (s.Trim() == "") return _missingByte;
      return Convert.ToByte(s, this.CultureInfo);
    }
    #endregion

    #region GetSByte
    public sbyte GetSByte(string keyword)
    {
      string s = GetString(keyword);
      //if (ignoreQuotes) s = s.Replace("\"", "");
      if (s.Trim() == "") return _missingSByte;
      return Convert.ToSByte(s, this.CultureInfo);
    }

    public sbyte GetSByte(int handle)
    {
      string s = GetString(handle);
      //if (ignoreQuotes) s = s.Replace("\"", "");
      if (s.Trim() == "") return _missingSByte;
      return Convert.ToSByte(s, this.CultureInfo);
    }
    #endregion

    #region GetInd64
    public Int64 GetInt64(string keyword)
    {
      string s = GetString(keyword);
      //if (ignoreQuotes) s = s.Replace("\"", "");
      if (s.Trim() == "") return _missingInt64;
      return Convert.ToInt64(s, this.CultureInfo);
    }

    public Int64 GetInt64(int handle)
    {
      string s = GetString(handle);
      //if (ignoreQuotes) s = s.Replace("\"", "");
      if (s.Trim() == "") return _missingInt64;
      return Convert.ToInt64(s, this.CultureInfo);
    }
    #endregion

    #region GetInd32
    public int GetInt32(string keyword)
    {
      string s = GetString(keyword);
      //if (ignoreQuotes) s = s.Replace("\"", "");
      if (s.Trim() == "") return _missingInt32;
      return Convert.ToInt32(s, this.CultureInfo);
    }

    public int GetInt32(int handle)
    {
      string s = GetString(handle);
      //if (ignoreQuotes) s = s.Replace("\"", "");
      if (s.Trim() == "") return _missingInt32;
      return Convert.ToInt32(s, this.CultureInfo);
    }
    #endregion

    #region GetInt16
    public Int16 GetInt16(string keyword)
    {
      string s = GetString(keyword);
      //if (ignoreQuotes) s = s.Replace("\"", "");
      if (s.Trim() == "") return _missingInt16;
      return Convert.ToInt16(s, this.CultureInfo);
    }

    public Int16 GetInt16(int handle)
    {
      string s = GetString(handle);
      //if (ignoreQuotes) s = s.Replace("\"", "");
      if (s.Trim() == "") return _missingInt16;
      return Convert.ToInt16(s, this.CultureInfo);
    }
    #endregion

    #region GetDouble
    public double GetDouble(string keyword)
    {
      string s = GetString(keyword);
      //if (ignoreQuotes) s = s.Replace("\"", "");
      if (s.Trim() == "") return _missingDouble;
      return Convert.ToDouble(s, this.CultureInfo);
    }

    public double GetDouble(int handle)
    {
      string s = GetString(handle);
      //if (ignoreQuotes) s = s.Replace("\"", "");
      if (s.Trim() == "") return _missingDouble;
      return Convert.ToDouble(s, this.CultureInfo);
    }
    #endregion

    #endregion

    #region GetLine()
    public string GetLine(int index)
    {
      if (index < this.GroupSize)
      {
        return _group_line[index];
      }
      else
      {
        return null;
      }
    }
    #endregion

    #region Sort()
    public static void Sort(string Tabfile, string SortedTabFile, string SortVar)
    {
      Sort(Tabfile, SortedTabFile, SortVar, typeof(Int64));
    }

    public static void Sort(string Tabfile, string SortedTabFile, string SortVar, Type type)
    {
      TabFileReader tab;
      StreamWriter file;
      StreamReader test;
      string sortVar = SortVar.ToLower();

      test = new StreamReader(Tabfile);

      int n = 0;
      while (test.ReadLine() != null) n++; n--;
      test.Close();

      tab = new TabFileReader(Tabfile);
      file = new StreamWriter(SortedTabFile);

      string[] line = new string[n];
      Array index = Array.CreateInstance(type, n);

      int j = 0;
      while (tab.ReadLine())
      {
        line[j] = tab.Line;
        if (type == typeof(Int64))
          index.SetValue(tab.GetInt64(sortVar), j);
        else if (type == typeof(Int32))
          index.SetValue(tab.GetInt32(sortVar), j);
        else if (type == typeof(Int16))
          index.SetValue(tab.GetInt16(sortVar), j);
        else if (type == typeof(double))
          index.SetValue(tab.GetDouble(sortVar), j);
        else if (type == typeof(byte))
          index.SetValue(tab.GetByte(sortVar), j);
        else if (type == typeof(sbyte))
          index.SetValue(tab.GetSByte(sortVar), j);
        else if (type == typeof(string))
          index.SetValue(tab.GetString(sortVar), j);
        else
          throw new Exception("The type " + type.ToString() + " is unknown for TabFileReader.Sort");

        j++;
      }

      Array.Sort(index, line);

      file.WriteLine(tab.FirstLine);

      for (int i = 0; i < n; i++) file.WriteLine(line[i]);

      tab.Close();
      file.Close();

    }
    #endregion

    #region Transpose()
    public static void Transpose(string Tabfile1)
    {
      Transpose(Tabfile1, Tabfile1);
    }
    public static void Transpose(string Tabfile1, string TransposedTabfile)
    {
      TabFileReader tab1;
      try
      {
        tab1 = new TabFileReader(Tabfile1);
      }
      catch { throw; }
      string[] Columns = new string[tab1.Keywords.Length];
      tab1.Keywords.CopyTo(Columns, 0);
      bool loop = tab1.ReadLine();
      while (loop)
      {
        for (int i = 0; i < Columns.Length; i++)
        {
          Columns[i] = Columns[i] + "\t" + tab1.Elements[i].ToString();
        }
        loop = tab1.ReadLine();
      }
      tab1.Close();

      StreamWriter file;
      try
      {
        file = new StreamWriter(TransposedTabfile);
      }
      catch { throw; }
      for (int i = 0; i < Columns.Length; i++)
      {
        file.WriteLine(Columns[i]);
      }
      file.Close();
    }
    #endregion

    //TTH: denne bliver nok langsomt overflødig, efterhånden som vi har kohorte/fødselsår med inde som felt i mange tab-filer
    public static void AddBirthYear(string Tabfile1, string outfile, string yearName, string ageName)
    {
      TabFileReader tab1;
      StreamWriter file;

      try
      {
        tab1 = new TabFileReader(Tabfile1);
        file = new StreamWriter(outfile);
      }
      catch { throw; }

      int yearHandle = -12345;
      int ageHandle = -12345;
      for (int i = 0; i < tab1._keyword.Length; i++)
      {
        if (tab1._keyword[i] == yearName)
        {
          yearHandle = i;
        }
        else if (tab1._keyword[i] == ageName)
        {
          ageHandle = i;
        }
      }
      if (yearHandle == -12345) throw new Exception("Could not find " + yearName + " in the header");
      if (ageHandle == -12345) throw new Exception("Could not find " + ageName + " in the header");

      string firstLine = tab1._firstline + "\t" + "birthYear";
      file.WriteLine(firstLine);

      while (tab1.ReadLine())
      {
        int year = tab1.GetInt32(yearHandle);
        int age = tab1.GetInt32(ageHandle);
        int birthYear = year - age;
        string xx = tab1._line + "\t" + birthYear;
        file.WriteLine(xx);
      }
      file.Flush();
      file.Close();
    }

    #region Merge()
    public static void Merge(string Tabfile1, string Tabfile2, string MergedTabFile, string MergeVar)
    {
      TabFileReader tab1, tab2;
      StreamWriter file;
      string mergeVar = MergeVar.ToUpper();

      tab1 = new TabFileReader(Tabfile1);
      tab2 = new TabFileReader(Tabfile2);
      file = new StreamWriter(MergedTabFile);

      string varline1 = tab1.FirstLine;
      string varline2 = tab2.FirstLine;
      int i1 = -1, i2 = -1;

      for (int i = 0; i < tab1._keyword.Length; i++)
      {
        if (mergeVar.ToLower() == tab1._keyword[i].ToLower())
        {
          i1 = i;
          break;
        }
      }

      for (int i = 0; i < tab2._keyword.Length; i++)
      {
        if (mergeVar.ToLower() == tab2._keyword[i].ToLower())
        {
          i2 = i;
          break;
        }
      }

      if ((i1 == -1) || (i2 == -1))
      {
        throw new Exception("Both tab-files should include the variable " + MergeVar);
      }

      // Skriv 1. linie
      for (int i = 0; i < tab1._keyword.Length; i++) file.Write("{0}\t", tab1._keyword[i]);
      for (int i = 0; i < i2; i++) file.Write("{0}\t", tab2._keyword[i]);
      for (int i = i2 + 1; i < tab2._keyword.Length - 1; i++) file.Write("{0}\t", tab2._keyword[i]);
      if (i2 != tab2._keyword.Length - 1) file.Write(tab2._keyword[tab2._keyword.Length - 1]);
      file.Write("\n");

      Int64 index1, index2, z;
      bool loop = true;

      tab1.ReadLine();
      index1 = tab1.GetInt64(mergeVar);

      tab2.ReadLine();
      index2 = tab2.GetInt64(mergeVar);

      while (loop)
      {
        if (index1 <= index2)
        {
          if (index1 == index2)
          {
            for (int i = 0; i < tab1._element.Length; i++) file.Write("{0}\t", tab1._element[i]);
            for (int i = 0; i < i2; i++) file.Write("{0}\t", tab2._element[i]);
            for (int i = i2 + 1; i < tab2._element.Length - 1; i++) file.Write("{0}\t", tab2._element[i]);
            if (i2 != tab2._element.Length - 1) file.Write(tab2._element[tab2._element.Length - 1]);
            file.Write("\n");

          }

          loop = tab1.ReadLine();
          if (loop)
          {
            z = tab1.GetInt64(mergeVar);
            if (z <= index1) throw new Exception("The tab-file '" + Tabfile1 + "' is not sorted with respect to merge-variable " + MergeVar);
            index1 = z;
          }
        }
        else
        {
          loop = tab2.ReadLine();
          if (loop)
          {
            z = tab2.GetInt64(mergeVar);
            Console.WriteLine("{0},{1}", index2, z);
            if (z <= index2) throw new Exception("The tab-file '" + Tabfile2 + "' is not sorted with respect to merge-variable " + MergeVar);
            index2 = z;
          }
        }
      }

      tab1.Close();
      tab2.Close();
      file.Close();

    }
    #endregion

    public static void Compare(string Tabfile1, string Tabfile2, string fileWithDifferences)
    {
      TabFileReader tab1, tab2;
      StreamWriter file;
      Hashtable ht1 = new Hashtable();
      Hashtable ht2 = new Hashtable();

      try
      {
        tab1 = new TabFileReader(Tabfile1);
        tab2 = new TabFileReader(Tabfile2);
        file = new StreamWriter(fileWithDifferences);
      }
      catch { throw; }

      if (tab1._keyword.Length != tab2._keyword.Length)
      {
        throw new Exception("Tab-file header problem: file #1 has " + tab1._keyword.Length + " keywords, and file #2 has " + tab2._keyword.Length + " keywords.");
      }

      for (int i = 0; i < tab1._keyword.Length; i++)
      {
        if (tab1._keyword[i].ToLower() != tab2._keyword[i].ToLower())
        {
          throw new Exception("Tab-file header problem: different header keywords ('" + tab1._keyword[i] + "' <> '" + tab2._keyword[i] + "').");
        }
      }
      int counter1 = 0;
      while (tab1.ReadLine())
      {
        counter1++;
        string s = "";
        for (int i = 0; i < tab1._element.Length; i++) s += (string)tab1._element[i];
        ht1.Add(s, "" + counter1);
      }

      int counter2 = 0;
      while (tab2.ReadLine())
      {
        counter2++;
        string s = "";
        for (int i = 0; i < tab1._element.Length; i++) s += (string)tab2._element[i];
        ht2.Add(s, "" + counter2);
      }

      int counter1a = 0;
      foreach (string s in ht1)
      {
        counter1a++;
        string s2 = (string)ht2[s];
        if (s2 == null)
        {
          Console.WriteLine("Problem in file1: " + s + " from file 2 does not exist");
        }
      }

      int counter2a = 0;
      foreach (string s in ht2)
      {
        counter2a++;
        string s1 = (string)ht1[s];
        if (s1 == null)
        {
          Console.WriteLine("Problem in file2: " + s + " from file 1 does not exist");
        }
      }

      Console.WriteLine();
      Console.WriteLine("Problems in file1: " + counter1a);
      Console.WriteLine("Problems in file2: " + counter2a);
      tab1.Close();
      tab2.Close();
      file.Close();

    }

    #region Add()

    public static void Add(string Tabfile1, string Tabfile2, string AddedTabFile)
    {
      Add(Tabfile1, Tabfile2, AddedTabFile, double.NaN);
    }

    /// <summary>
    /// Adds two tab-files with n1 and n2 data lines together into one larger file with n1+n2 data lines.
    /// Requires that the headers in the two files are identical.
    /// </summary>
    /// <param name="Tabfile1">Input tab-file #1</param>
    /// <param name="Tabfile2">Input tab-file #2</param>
    /// <param name="AddedTabFile">Output tab-file</param>
    public static void Add(string Tabfile1, string Tabfile2, string AddedTabFile, double newProjectionID)
    {
      TabFileReader tab1, tab2;
      StreamWriter file;

      try
      {
        tab1 = new TabFileReader(Tabfile1);
        tab2 = new TabFileReader(Tabfile2);
        file = new StreamWriter(AddedTabFile);
      }
      catch { throw; }

      if (tab1._keyword.Length != tab2._keyword.Length)
      {
        throw new Exception("Tab-file header problem: file #1 has " + tab1._keyword.Length + " keywords, and file #2 has " + tab2._keyword.Length + " keywords.");
      }

      for (int i = 0; i < tab1._keyword.Length; i++)
      {
        if (tab1._keyword[i].ToLower() != tab2._keyword[i].ToLower())
        {
          throw new Exception("Tab-file header problem: different header keywords ('" + tab1._keyword[i] + "' <> '" + tab2._keyword[i] + "').");
        }
      }

      // Skriv 1. linie (kunne lige så godt have været 2. linje, da de er ens)
      for (int i = 0; i < tab1._keyword.Length; i++) file.Write("{0}\t", tab1._keyword[i]);

      // (OBS: Dataskrivningen kunne køre hurtigere vha. StreamReader.ReadLine() og StreamWriter.WriteLine(), da der jo ikke er grund til
      // at data først splittes op og senere tab-separeres. De kunne læses og skrives råt linje for linje uden at se på linjens indhold.
      // Det kan jo overvejes, hvis hastigheden bliver et issue).

      if (newProjectionID == double.NaN)
      {
        // Skriv data fra fil #1
        while (tab1.ReadLine())
        {
          file.WriteLine();
          for (int i = 0; i < tab1._element.Length; i++) file.Write("{0}\t", tab1._element[i]);
        }
        // Skriv data fra fil #2
        while (tab2.ReadLine())
        {
          file.WriteLine(); //skrives her for at undgå en tom blank linje allersidst i filen
          for (int i = 0; i < tab2._element.Length; i++) file.Write("{0}\t", tab2._element[i]);
        }
      }
      else
      {
        // Skriv data fra fil #1
        while (tab1.ReadLine())
        {
          file.WriteLine();
          for (int i = 0; i < tab1._element.Length; i++)
          {
            if (string.Compare(tab1._element[i], "ProjectionID", true) == 0)
            {
              file.Write("{0}\t", newProjectionID);
            }
            else
            {
              file.Write("{0}\t", tab1._element[i]);
            }
          }
        }
        // Skriv data fra fil #2
        while (tab2.ReadLine())
        {
          file.WriteLine(); //skrives her for at undgå en tom blank linje allersidst i filen
          for (int i = 0; i < tab2._element.Length; i++) file.Write("{0}\t", tab2._element[i]);
        }
      }

      tab1.Close();
      tab2.Close();
      file.Close();

    }    
    #endregion

    #region Tab() og String()
    public string Tab(params object[] o)
    {

      string[] s = new string[o.Length];

      for (int i = 0; i < o.Length; i++)
      {
        if (o[i].GetType() == typeof(int)) s[i] = ((int)o[i] == _missingInt32) ? "" : o[i].ToString();
        else if (o[i].GetType() == typeof(double)) s[i] = ((double)o[i] == _missingDouble) ? "" : o[i].ToString();
        else if (o[i].GetType() == typeof(string)) s[i] = ((string)o[i] == _missingString) ? "" : o[i].ToString();
        else if (o[i].GetType() == typeof(char)) s[i] = ((char)o[i] == _missingChar) ? "" : o[i].ToString();
        else if (o[i].GetType() == typeof(byte)) s[i] = ((byte)o[i] == _missingByte) ? "" : o[i].ToString();
        else if (o[i].GetType() == typeof(sbyte)) s[i] = ((sbyte)o[i] == _missingSByte) ? "" : o[i].ToString();
        else if (o[i].GetType() == typeof(Int16)) s[i] = ((Int16)o[i] == _missingInt16) ? "" : o[i].ToString();
        else if (o[i].GetType() == typeof(Int64)) s[i] = ((Int64)o[i] == _missingInt64) ? "" : o[i].ToString();
      }

      string ret = s[0];
      for (int i = 1; i < s.Length; i++) ret = ret + "\t" + s[i];

      return ret;

    }

    public string String(params int[] x)
    {
      string s = (x[0] == _missingInt32) ? Missing : x[0].ToString();
      for (int i = 1; i < x.Length; i++) s = s + "\t" + ((x[i] == _missingInt32) ? Missing : x[i].ToString());
      return s;
    }

    public string String(params double[] x)
    {
      string s = (x[0] == _missingDouble) ? this.Missing : x[0].ToString();
      for (int i = 1; i < x.Length; i++) s = s + "\t" + ((x[i] == _missingDouble) ? Missing : x[i].ToString());
      return s;
    }



    #endregion

    #region GetDataTable

    public DataTable GetDataTable()
    {
      Type[] type = new Type[_keyword.Length];
      for (int i = 0; i < type.Length; i++) type[i] = typeof(System.String);

      return GetDataTable(type, "");

    }

    public DataTable GetDataTable(string sqlSelect)
    {
      Type[] type = new Type[_keyword.Length];
      for (int i = 0; i < type.Length; i++) type[i] = typeof(System.String);

      return GetDataTable(type, sqlSelect);

    }

    public DataTable GetDataTable(Type t, string sqlSelect)
    {
      Type[] type = new Type[_keyword.Length];
      for (int i = 0; i < type.Length; i++) type[i] = t;

      return GetDataTable(type, sqlSelect);

    }

    public DataTable GetDataTable(Type[] type, string sqlSelect)
    {

      _file.Close();

      FileInfo fi = new FileInfo(_fileName);
      StreamWriter schema = new StreamWriter(fi.DirectoryName + "\\schema.ini");

      schema.WriteLine("[" + fi.Name + "]");
      schema.WriteLine("Format=TabDelimited");
      schema.WriteLine("ColNameHeader=True");
      schema.WriteLine("MaxScanRows=" + MaxScanRows.ToString());
      schema.WriteLine();
      schema.Close();

      string connString = @"Driver={Microsoft Text Driver (*.txt; *.csv)};DBQ=" + @fi.DirectoryName;
      OdbcConnection conn = new OdbcConnection(connString);

      conn.Open();

      OdbcDataAdapter da;

      if (sqlSelect == "")
        da = new OdbcDataAdapter("SELECT * FROM " + fi.Name, conn);
      else
        da = new OdbcDataAdapter(sqlSelect, conn);

      DataSet ds;
      if (da != null)
      {
        ds = new DataSet();
        da.Fill(ds, "TabFileReaderTable");

      }
      else
        throw new Exception("Error creating OdbcDataAdapter in TabFileReader");

      conn.Close();

      return ds.Tables[0];

    }

        #endregion
        #endregion

        #region Extract - method

        public static void Extract7z(string archivePath, string outputDir)
        {
            Directory.CreateDirectory(outputDir);

            using var archive = SevenZipArchive.Open(archivePath);

            foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
            {
                entry.WriteToDirectory(
                    outputDir,
                    new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    }
                );
            }
        }

        public static void ExtractWithSharpCompress(
    string fileNameWithPath,
    string fileNameWithoutPathStripped,
    string pathToTemporaryLocation)
        {
            using var archive = SevenZipArchive.Open(fileNameWithPath);

            string tabFile = null;

            // 1. Scan for file match
            foreach (var entry in archive.Entries)
            {
                Console.WriteLine(entry.Key);

                if (string.Equals(entry.Key, fileNameWithoutPathStripped, StringComparison.OrdinalIgnoreCase))
                {
                    tabFile = entry.Key;
                }
            }

            if (tabFile == null)
                throw new Exception(
                    $"*** ERROR: TabFileReader cannot find tab-file inside zip-file. " +
                    $"Expected to find '{fileNameWithoutPathStripped}' inside '{fileNameWithPath}'"
                );

            // 2. Extract all files
            foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
            {
                entry.WriteToDirectory(
                    pathToTemporaryLocation,
                    new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    }
                );
            }
        }
        #endregion Extract - method

        #endregion

        #region Private Methods


        // Helper-functions
        #region getIndex
        int getindex(string keyword)
    {
      string kw = keyword.ToUpper();
      for (int i = 0; i < _keyword.Length; i++)
      {
        if (kw == _keyword[i]) return i;
      }

      throw new InvalidOperationException(keyword + " is unknown keyword.");
    }
    #endregion

    #region isFlag
    bool isFlag(TabFileOption all, TabFileOption one)
    {
      return ((int)all & (int)one) == (int)one;
    }
    #endregion

    #endregion

    #region Properties
    #region Missing...
    /// <summary>
    /// Returned if missing value. Default=Int64.MinValue
    /// </summary>
    public Int64 MissingInt64
    {
      get { return _missingInt64; }
      set { _missingInt64 = value; }
    }

    /// <summary>
    /// Returned if missing value. Default=Int32.MinValue
    /// </summary>
    public Int32 MissingInt32
    {
      get { return _missingInt32; }
      set { _missingInt32 = value; }
    }

    /// <summary>
    /// Returned if missing value. Default=-1
    /// </summary>
    public Int16 MissingInt16
    {
      get { return _missingInt16; }
      set { _missingInt16 = value; }
    }

    /// <summary>
    /// Returned if missing value. Default=-1.0
    /// </summary>
    public Double MissingDouble
    {
      get { return _missingDouble; }
      set { _missingDouble = value; }
    }

    /// <summary>
    /// Returned if missing value. Default=0
    /// </summary>
    public Byte MissingByte
    {
      get { return _missingByte; }
      set { _missingByte = value; }
    }

    /// <summary>
    /// Returned if missing value. Default=-1
    /// </summary>
    public SByte MissingSByte
    {
      get { return _missingSByte; }
      set { _missingSByte = value; }
    }
    public char MissingChar
    {
      get { return _missingChar; }
      set { _missingChar = value; }
    }
    public string MissingString
    {
      get { return _missingString; }
      set { _missingString = value; }
    }


    #endregion

    /// <summary>
    /// First line in the file, containing keywords
    /// </summary>
    public string FirstLine
    {
      get { return _firstline; }
    }

    /// <summary>
    /// Current line
    /// </summary>
    public string Line
    {
      get { return _line; }
    }

    /// <summary>
    /// Returns the underlying StreamReader-object
    /// </summary>
    public StreamReader StreamReader
    {
      get { return _file; }
    }

    /// <summary>
    /// Antal søjler i data
    /// </summary>
    public int NumberOfColumns
    {
      get { return _keyword.Length; }
    }

    /// <summary>
    /// Number of lines in the file
    /// </summary>
    public int NumberOfLines
    {
      get { return _nLines; }
    }

    /// <summary>
    /// Number of observations in the file.
    /// </summary>
    public int NumberOfObservations
    {
      get { return _nLines - 1; }
    }


    /// <summary>
    /// The size of the current group
    /// </summary>
    public int GroupSize
    {
      get { return _groupSize; }
    }

    /// <summary>
    /// Returns an array of all keywords defined in the file
    /// </summary>
    public string[] Keywords
    {
      get { return _keyword; }
    }

    /// <summary>
    /// Returns an array of all elements parsed from the current line
    /// </summary>
    public string[] Elements
    {
      get { return _element; }
    }

    #endregion

  }
}
