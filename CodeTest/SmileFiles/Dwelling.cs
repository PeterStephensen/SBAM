// SBAM_TEST: slår den konstruktør fra, der kræver InputToPartyTree (findes ikke i CodeTest-projektet).
#define SBAM_TEST

using System;

namespace DreamSMILE.MicroSim
{
  #region enums
    public enum EDwellingType : byte
  {
    Ejerbolig,
    Almenbolig,
    Andelsbolig,
    UdlejningsboligOffentlig,
    UdlejningsboligPrivat
    //Uoplyst
  }

    public enum EDwellingUse : byte
  {
    Stuehus,
    Parcelhus,
    Raekkehus,
    Etagebolig,
    Kollegium,
    AndenHelaarsbeboelse,
    Erhvervsbolig,
    Doegninstitution,
    Fritidshus
  }

    public enum EDwellingSize : byte
  {
    m2_0_39,
    m2_40_59,
    m2_60_79,
    m2_80_99,
    m2_100_119,
    m2_120_159,
    m2_160_199,
    m2_200
    //Uoplyst
  }

    public enum EDwellingConstructionYear : byte
  {
    aar_1899,
    aar_1900_1909,
    aar_1910_1919,
    aar_1920_1929,
    aar_1930_1939,
    aar_1940_1949,
    aar_1950_1959,
    aar_1960_1969,
    aar_1970_1979,
    aar_1980_1989,
    aar_1990_1999,
    aar_2000
    //Uoplyst
  }

    public enum EDwellingRegion
  {
    KoebenhavnBy,
    KoebenhavnOmegn,
    SjaellandNord,
    Bornholm,
    SjaellandOest,
    SjaellandVestogSyd,
    Fyn,
    JyllandSyd,
    JyllandOest,
    JyllandVest,
    JyllandNord,
    Ukendt //Uoplyst
  }

    public enum EDwellingMunicipality : byte
  {
    København,	//	0
    Frederiksberg,	//	1
    Dragør,	//	2
    Tårnby,	//	3
    Albertslund,	//	4
    Ballerup,	//	5
    Brøndby,	//	6
    Gentofte,	//	7
    Gladsaxe,	//	8
    Glostrup,	//	9
    Herlev,	//	10
    Hvidovre,	//	11
    HøjeTaastrup,	//	12
    Ishøj,	//	13
    LyngbyTaarbæk,	//	14
    Rødovre,	//	15
    Vallensbæk,	//	16
    Allerød,	//	17
    Egedal,	//	18
    Fredensborg,	//	19
    Frederikssund,	//	20
    FrederiksværkHundested,	//	21
    Furesø,	//	22
    Gribskov,	//	23
    Helsingør,	//	24
    Hillerød,	//	25
    Hørsholm,	//	26
    Rudersdal,	//	27
    Bornholm,	//	28
    //PSP	//	
    //Christiansø,	//	
    Greve,	//	29
    Køge,	//	30
    Lejre,	//	31
    Roskilde,	//	32
    Solrød,	//	33
    Faxe,	//	34
    Guldborgsund,	//	35
    Holbæk,	//	36
    Kalundborg,	//	37
    Lolland,	//	38
    Næstved,	//	39
    Odsherred,	//	40
    Ringsted,	//	41
    Slagelse,	//	42
    Sorø,	//	43
    Stevns,	//	44
    Vordingborg,	//	45
    Assens,	//	46
    FaaborgMidtfyn,	//	47
    Kerteminde,	//	48
    Langeland,	//	49
    Middelfart,	//	50
    Nordfyns,	//	51
    Nyborg,	//	52
    Odense,	//	53
    Svendborg,	//	54
    Ærø,	//	55
    Billund,	//	56
    Esbjerg,	//	57
    Fanø,	//	58
    Fredericia,	//	59
    Haderslev,	//	60
    Kolding,	//	61
    Sønderborg,	//	62
    Tønder,	//	63
    Varde,	//	64
    Vejen,	//	65
    Vejle,	//	66
    Aabenraa,	//	67
    Favrskov,	//	68
    Hedensted,	//	69
    Horsens,	//	70
    Norddjurs,	//	71
    Odder,	//	72
    Randers,	//	73
    Samsø,	//	74
    Silkeborg,	//	75
    Skanderborg,	//	76
    Syddjurs,	//	77
    Aarhus,	//	78
    Herning,	//	79
    Holstebro,	//	80
    IkastBrande,	//	81
    Lemvig,	//	82
    RingkøbingSkjern,	//	83
    Skive,	//	84
    Struer,	//	85
    Viborg,	//	86
    Brønderslev,	//	87
    Frederikshavn,	//	88
    Hjørring,	//	89
    Jammerbugt,	//	90
    Læsø,	//	91
    Mariagerfjord,	//	92
    Morsø,	//	93
    Rebild,	//	94
    Thisted,	//	95
    Vesthimmerlands,	//	96
    Aalborg	//	97

  }

    public enum EDwellingDensity : byte
  {
    Hovedstadsomraadet,
    Density_50000_plus,
    Density_10000_49999,
    Density_1000_9999,
    Density_lessthan_1000
    //Uoplyst
  }
  #endregion enums

  public class Dwelling
  {
    #region Public fields
    public EDwellingType DwellingType;
    public EDwellingUse DwellingUse;
    public EDwellingSize DwellingSize;
    public EDwellingConstructionYear DwellingConstructionYear;
    public EDwellingRegion DwellingRegion;
    public EDwellingMunicipality DwellingMunicipality;
    public EDwellingDensity DwellingDensity;
    #endregion

    #region private fields
    private double _husleje;
    private bool _initHusleje = false;
    private int _t;
    #endregion private fields

    #region constructors
    public Dwelling(Person p)
    {
        DwellingType = p.Household.Dwelling.DwellingType;
        DwellingUse = p.Household.Dwelling.DwellingUse;
        DwellingSize = p.Household.Dwelling.DwellingSize;
        DwellingConstructionYear = p.Household.Dwelling.DwellingConstructionYear;
        DwellingRegion = p.Household.Dwelling.DwellingRegion;
        DwellingMunicipality = p.Household.Dwelling.DwellingMunicipality;
        DwellingDensity = p.Household.Dwelling.DwellingDensity;
        _t = 0;
    }

    public Dwelling()
    {
 
        _t = 0;
    }

#if !SBAM_TEST
    public Dwelling(InputToPartyTree m)
    {
        DwellingType = (EDwellingType)m.DwellingType;
        DwellingUse = (EDwellingUse)m.DwellingUse;
        DwellingSize = (EDwellingSize)m.DwellingSize;
        DwellingConstructionYear = (EDwellingConstructionYear)m.DwellingConstructionYear;
        DwellingRegion = (EDwellingRegion)m.DwellingRegion;
        DwellingMunicipality = (EDwellingMunicipality)m.DwellingMunicipalityIndex;
        DwellingDensity = (EDwellingDensity)m.DwellingDensity;
        _t = 0;
    }
#endif

    public Dwelling(EDwellingMunicipality dwellingMunicipality)
    {
        DwellingMunicipality = dwellingMunicipality;
        _t = 0;
    }

    public Dwelling(
     EDwellingMunicipality dwellingMunicipality,
     EDwellingRegion dwellingRegion,
     EDwellingUse dwellingUse,
     EDwellingType dwellingType)
    {
        DwellingMunicipality = dwellingMunicipality;
        DwellingRegion = dwellingRegion;
        DwellingUse = dwellingUse;
        DwellingType = dwellingType;
        _t = 0;
    }

    public Dwelling(EDwellingType dwellingType, EDwellingUse dwellingUse, EDwellingSize dwellingSize, EDwellingConstructionYear dwellingConstructionYear, EDwellingRegion dwellingRegion, EDwellingDensity dwellingDensity)
    {
        DwellingType = dwellingType;
        DwellingUse = dwellingUse;
        DwellingSize = dwellingSize;
        DwellingConstructionYear = dwellingConstructionYear;
        DwellingRegion = dwellingRegion;
        DwellingDensity = dwellingDensity;

        _t = 0;
    }

    public Dwelling(EDwellingType dwellingType, EDwellingUse dwellingUse, EDwellingSize dwellingSize, EDwellingConstructionYear dwellingConstructionYear, EDwellingRegion dwellingRegion, EDwellingMunicipality dwellingMunicipality, EDwellingDensity dwellingDensity)
    {
        DwellingType = dwellingType;
        DwellingUse = dwellingUse;
        DwellingSize = dwellingSize;
        DwellingConstructionYear = dwellingConstructionYear;
        DwellingRegion = dwellingRegion;
        DwellingMunicipality = dwellingMunicipality;
        DwellingDensity = dwellingDensity;

        _t = 0;
    }
    #endregion constructors

    #region properties
    public double Husleje
    {
        get { return _husleje; }
    }
    public bool IsInitHusleje
    {
        get { return _initHusleje; }
    }

    public int Duration
    { get { return Math.Min(27, _t); } }

    #endregion properties

    #region methods
    public void InitHusleje(double husleje)
    {
        _husleje = husleje;
        _initHusleje = true;
    }

    public void update(int t = 0)
    {
        if (t == 0)
            _t++;
        else
            _t = t;
    }
    #endregion methods

    public static EDwellingMunicipality HACKmunicipalityHACK(EDwellingRegion region)
    {
        switch (region)
        {
            case EDwellingRegion.KoebenhavnBy: return EDwellingMunicipality.København;
            case EDwellingRegion.KoebenhavnOmegn: return EDwellingMunicipality.Brøndby;
            case EDwellingRegion.SjaellandNord: return EDwellingMunicipality.Gribskov;
            case EDwellingRegion.Bornholm: return EDwellingMunicipality.Bornholm;
            case EDwellingRegion.SjaellandOest: return EDwellingMunicipality.Køge;
            case EDwellingRegion.SjaellandVestogSyd: return EDwellingMunicipality.Slagelse;
            case EDwellingRegion.Fyn: return EDwellingMunicipality.Odense;
            case EDwellingRegion.JyllandSyd: return EDwellingMunicipality.Esbjerg;
            case EDwellingRegion.JyllandOest: return EDwellingMunicipality.Aarhus;
            case EDwellingRegion.JyllandVest: return EDwellingMunicipality.Herning;
            case EDwellingRegion.JyllandNord: return EDwellingMunicipality.Aalborg;

            default:
                throw new Exception(String.Format("Ukendt landsdel: {0}", region));
        }
    }

  }
}