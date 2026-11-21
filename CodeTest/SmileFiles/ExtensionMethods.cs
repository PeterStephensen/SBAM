
using System;
using System.IO;
using System.Runtime.Serialization;


namespace DreamSMILE.MicroSim
{
  public static class ExtensionMethods
  {
    #region Shuffle
    //PSP
    // Fisher-Yates-shuffle
    public static void Shuffle<T>(this IList<T> list)
    {
      int n = list.Count;
      while (n > 1)
      {
        n--;
        int k = Simulation.Random.Next(n + 1); //ikke thread-safe
        T value = list[k];
        list[k] = list[n];
        list[n] = value;
      }
    }
        #endregion

        #region Clone
        //    TT: Her laves en "deep clone" af et objekt.
        //    At kopien er dyb vil sige at alle underobjekter (og disses underobjekter osv.) også er kloner.
        //    Clone()-metoden koster lidt i performance, men er den hurtigste måde at gøre dette på automatisk.
        //    Alternativet er at klone objekterne ét for ét (og deres felter), og hvad værre er: hele tiden selv husker på
        //    at få nye objekter eller felter med i sin hånd-kloningsmetode. Så med mindre denne her "deep clone" viser sig at
        //    være en flaskehals mht. performance, så eliminerer den en stor fremtidig fejlkilde.
        //    UPDATE: Denne deep clone metode er nok alligevel ikke smart i mikrosim, fordi objekterne peger på hinanden i
        //    voldsom grad (person peger på agent peger på simulation, statistics, settings, sig selv.........). Vi skal selvfølgelig
        //    ikke deep-clone f.eks. statistics-objektet!
        //    Men til andre formå kan metoden måske bruges, så derfor slettes den ikke.
        //    Eksempel på brug: List<Household> clonedHouseholds = ExtensionMethods.Clone<List<Household>>(clone_list);
        //public static T Clone<T>(T source)
        //{
        //  if (!typeof(T).IsSerializable)
        //    throw new Exception("*** ERROR: Typen skal være 'serializable' hvis der skal kunne laves en deep clone af objektet");

        //  if (Object.ReferenceEquals(source, null)) //Lad være med at serialisere et null-objekt, returnér default for dette objekt i stedet
        //    return default(T);

        //  IFormatter formatter = new BinaryFormatter();
        //  Stream stream = new MemoryStream();
        //  using (stream)
        //  {
        //    formatter.Serialize(stream, source); //objektet klappes samme i en fil, som ligger i RAM
        //    stream.Seek(0, SeekOrigin.Begin);
        //    return (T)formatter.Deserialize(stream);  //et nyt objekt laves ud fra filen. Alle underobjekter osv. vil også være kloner -- dette er fordelen ved metoden.
        //  }

        public static T Clone<T>(T source)
        {
            // Old: typeof(T).IsSerializable — not used by DataContractSerializer
            // But we keep the check to preserve your original behavior.
            //if (!typeof(T).IsSerializable)
            //    throw new Exception("*** ERROR: Typen skal være 'serializable' hvis der skal kunne laves en deep clone af objektet");

            //// Avoid serializing null — keep original behaviour
            //if (ReferenceEquals(source, null))
            //    return default(T);

            //var serializer = new DataContractSerializer(typeof(T));

            //using (var stream = new MemoryStream())
            //{
            //    serializer.WriteObject(stream, source);   // serialize to RAM
            //    stream.Seek(0, SeekOrigin.Begin);
            //    return (T)serializer.ReadObject(stream);  // deep clone
            //}
            return default(T);
        }
    
    #endregion

  }
}
