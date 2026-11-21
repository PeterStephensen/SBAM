using System;
using System.Collections.Generic;

namespace DreamSMILE.MicroSim
{
  /// <summary>
  /// Klasse der benyttes til at registrere hvilken tilstand (state) et givent objekt er i.
  /// </summary>
  /// <typeparam name="T">En enumeration der angiver de mulige tilstande</typeparam>
  [Serializable]
  public class State<T>
  {

    #region Private fields
    List<T> _list = new List<T>();
    #endregion

    #region Constructors
    /// <summary>
    /// Konstruktor for State-klassen
    /// </summary>
    public State()
    { 
    
    }

    /// <summary>
    /// Konstruktor for State-klassen
    /// </summary>
    /// <param name="initState">Initial tilstand</param>
    public State(T initState)
    {
      Add(initState);
    }    
    #endregion

    #region Public methods
    /// <summary>
    /// Tilføjer en ny tilstand
    /// </summary>
    /// <param name="s">Et element fra en enumeration der angiver tilstanden</param>
    public void Add(T s)
    {
      // Sikrer sig at den enkelte state kun er med en gang
      foreach (T i in _list)
        if (i.Equals(s))
          return;
      
      _list.Add(s);    
    }

    /// <summary>
    /// Fjerner en tilstand
    /// </summary>
    /// <param name="s">Et element fra en enumeration der angiver tilstanden</param>
    public void Remove(T s)
    {
      _list.Remove(s);
    }

    /// <summary>
    /// Tester om en tilstand er tilføjet
    /// </summary>
    /// <param name="s">Et element fra en enumeration der angiver tilstanden</param>
    public bool Is(T s)
    {
      foreach (T i in _list)
        if (i.Equals(s))
          return true;

      return false;    
    }
    #endregion

    #region Properties
    public List<T> List
    {
      get { return _list; }
    }
    
    #endregion

    #region ToString() - override
    public override string ToString()
    {

      if (_list.Count == 0)
        return "Empty";

      int n = _list.Count;
      
      T[] t = (T[])_list.ToArray();
      string s = t[0].ToString();

      for (int i = 1; i < n; i++)
        s = s + "," + t[i].ToString();


      return s;

    }    
    #endregion

  }
}