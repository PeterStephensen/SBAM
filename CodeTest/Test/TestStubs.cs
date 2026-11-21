// Test replacements for the parts of SMILE that Matching.cs uses but that cannot be included without
// pulling in the whole model (MSSettings.cs, Simulation.cs, Statistics.cs, Person.cs, Household.cs
// and DwellingCharacteristics*.cs). The classes have the same names and member names as in the model,
// but contain only what Matching.cs calls. They are NOT the model's code.

using DreamSMILE.DemoSim;
using DreamSMILE.Numerical;

// Empty namespace: "using DreamSMILE.SimulationNS;" appears in Matching.cs and SmileFiles\LogFile.cs.
namespace DreamSMILE.SimulationNS { }

namespace DreamSMILE.MicroSim
{
    /// <summary>The settings from MSSettings.cs that Matching.cs reads. The values are the model's default values.</summary>
    public static class MSSettings
    {
        public const int MAX_AGE = 121;
        public const int ORIGIN5 = 5;
        public const int GENDER = 2;
        public const int REGION = 11;
        public const int MUNICIPALITY = 98;

        public static int Seed = 12345;
        public static string ProjectionID = "1";
        public static string OutputDir = "";
        public static string InputDataMatching = "";

        public static double[] MatchingScaleUp;
        public static int MatchingMaxAge = 55;   // Program.cs in the model sets it to 65
        public static int MatchingMinAge = 15;

        public static bool UseMunicipalityDependentSBAM = false;
        public static bool SBAMMunicipalityGrassSow = false;
    }

    /// <summary>Copy of the enum in Household.cs.</summary>
    public enum EHouseholdState
    {
        New,
        Immigrant,
        InMatchingPool
    }

    /// <summary>Used only in parring(), which is switched off with SBAM_TEST.</summary>
    public class DwellingCharacteristics { }

    public class Person
    {
        public int Age;                 // age at the end of the year, as at the time of matching in the model
        public EGender Gender;
        public int HighestEducation5;
        public EOrigin5 Origin5;
        public Household Household;

        public EDwellingRegion Region { get { return Household.Dwelling.DwellingRegion; } }
        public EDwellingMunicipality Municipality { get { return Household.Dwelling.DwellingMunicipality; } }

        /// <summary>For the test only: type index in the artificial market (Market.TypeIndex).</summary>
        public int TestType = -1;
    }

    public class Household
    {
        public State<EHouseholdState> State = new State<EHouseholdState>();
        public Person FirstAdult;
        public Dwelling Dwelling;

        public Household(Person firstAdult, Dwelling dwelling)
        {
            FirstAdult = firstAdult;
            Dwelling = dwelling;
            firstAdult.Household = this;
        }
    }

    /// <summary>Collects the couples that Matching.parring() reports, instead of the model's statistics.</summary>
    public class Statistics
    {
        public readonly List<(Household First, Household Second)> MatchedPairs = new List<(Household, Household)>();

        public void Report(int idEvent, Household h1, Household h2)
        {
            if (idEvent == Event.Demographics.Matching)
                MatchedPairs.Add((h1, h2));
        }
    }

    public class Simulation
    {
        public static Simulation Instance { get; private set; }
        public static RandomEvent Random = new RandomEvent(MSSettings.Seed);

        public List<Household> Households = new List<Household>();
        public Statistics Statistics = new Statistics();
        public Time Time;
        public LogFile LogFile;

        /// <summary>Creates the simulation that Matching fetches via Simulation.Instance. Requires MSSettings.OutputDir to be set.</summary>
        public static Simulation Create(int year, int seed)
        {
            Random = new RandomEvent(seed);
            Instance = new Simulation
            {
                Time = new Time(year, year),
                LogFile = new LogFile(Path.Combine(MSSettings.OutputDir, "SbamTest_log.txt"), "SBAM-test af Matching.cs", "CodeTest")
            };
            return Instance;
        }
    }
}
