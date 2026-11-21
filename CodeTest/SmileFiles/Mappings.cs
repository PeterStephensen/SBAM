// SBAM_TEST: slår LMSToSocioeconomic() fra, der kræver Person og Economics fra resten af modellen.
#define SBAM_TEST

using System;

namespace DreamSMILE.MicroSim
{
  /// <summary>
  /// Indeholder hjælpemetoder til oversættelse/mapping af forskellige koder og enums
  /// </summary>
  public class Mappings
  {
    static public EEducation6 Education6(EEducationDS8 edu)
    {
      switch (edu)
      {
        case EEducationDS8.Ukendt:
          return EEducation6.Ukendt;

        case EEducationDS8.Grundskole:
        case EEducationDS8.TiendeKlasse:
          return EEducation6.Grundskole;

        case EEducationDS8.Almengymnasial:
        case EEducationDS8.Erhvervsgymnasial:
          return EEducation6.Gymnasial;

        case EEducationDS8.ErhvervsfagligeGrundforlob:
        case EEducationDS8.ErhvervsfagligeHovedforlob:
          return EEducation6.Erhvervsfaglig;

        case EEducationDS8.KortVideregående:
          return EEducation6.KVU;

        case EEducationDS8.ProffessionsBachelor:
        case EEducationDS8.MellemlangVideregående:
        case EEducationDS8.UniversitetsBachelor:
          return EEducation6.MVU;

        case EEducationDS8.UdelteKandidat:
        case EEducationDS8.DelteKandidat:
        case EEducationDS8.Master:
        case EEducationDS8.Phd:
          return EEducation6.LVU;
      }
      throw new Exception("Du skulle ikke komme her!");
    }
    /// <summary>
    /// Omregner højst fuldførte uddannelse fra fra standard 14-gruppering til 12-gruppering (hvor erhvervsfaglige grund- og hovedforløb er slået sammen og delte og udelte kandidatuddannelser er slået sammen).
    /// </summary>
    /// <param name="edu">Højst fuldførte uddannelse ifølge 14-gruppering.</param>
    /// <returns>Returnerer højst fuldførte uddannelse ifølge 12-gruppering.</returns>
    static public EEducation12 Education12(EEducationDS8 edu)
    {
      switch (edu)
      {
        case EEducationDS8.Ukendt:
          return EEducation12.Ukendt;

        case EEducationDS8.Grundskole:
          return EEducation12.Grundskole;

        case EEducationDS8.TiendeKlasse:
          return EEducation12.TiendeKlasse;

        case EEducationDS8.Almengymnasial:
          return EEducation12.Almengymnasial;

        case EEducationDS8.Erhvervsgymnasial:
          return EEducation12.Erhvervsgymnasial;

        case EEducationDS8.ErhvervsfagligeGrundforlob:
        case EEducationDS8.ErhvervsfagligeHovedforlob:
          return EEducation12.Erhvervsfaglige;

        case EEducationDS8.KortVideregående:
          return EEducation12.KortVideregående;

        case EEducationDS8.ProffessionsBachelor:
          return EEducation12.ProffessionsBachelor;

        case EEducationDS8.MellemlangVideregående:
          return EEducation12.MellemlangVideregående;

        case EEducationDS8.UniversitetsBachelor:
          return EEducation12.UniversitetsBachelor;

        case EEducationDS8.UdelteKandidat:
        case EEducationDS8.DelteKandidat:
          return EEducation12.Kandidat;

        case EEducationDS8.Master:
          return EEducation12.Master;

        case EEducationDS8.Phd:
          return EEducation12.Phd;
      }
      throw new Exception("Du skulle ikke komme her!");
    }
    /// <summary>
    /// Omregner arbejdsmarkedstildeltagelsestilstande fra standard 9-gruppering til 8-gruppering (hvor ledige på dagpenge og kontanthjælp slået sammen til én gruppe, alle øvrige grupper er ikke ændret).
    /// </summary>
    /// <param name="labourstate">Arbejdsmarkedstilknytning på 9-gruppering.</param>
    /// <returns>Returnerer arbejdsmarkedstilknytning på 8-gruppering.</returns>
    static public ELabourMarketState8 LabourMarketState8(ELabourMarketState labourstate)
    {
      switch (labourstate)
      {
        case ELabourMarketState.Unknown:
          return ELabourMarketState8.Unknown;
        case ELabourMarketState.Employed:
          return ELabourMarketState8.Employed;
        case ELabourMarketState.UnemployedBenefit:
        case ELabourMarketState.UnemployedCash:
          return ELabourMarketState8.Unemployed;
        case ELabourMarketState.NoIncome:
          return ELabourMarketState8.NoIncome;
        case ELabourMarketState.Cashbenefit:
          return ELabourMarketState8.Cashbenefit;
        case ELabourMarketState.Student:
          return ELabourMarketState8.Student;
        case ELabourMarketState.Disability:
          return ELabourMarketState8.Disability;
        case ELabourMarketState.EarlyRetirement:
          return ELabourMarketState8.EarlyRetirement;
        case ELabourMarketState.Agepension:
          return ELabourMarketState8.Agepension;
      }
      throw new Exception("Du skulle ikke komme her!");
    }

    /// <summary>
    /// Danner EOrigin12-enumeration ud fra husholdningskarateristika
    /// </summary>
    /// <returns>EOrigin12-enumeration</returns>
    static public EOrigin12 Origin12(EPopulationStatus populationStatus, ECitizenship citizenship, ECountry country)
    {
      switch (populationStatus)
      {
        case EPopulationStatus.Dane:

          if (citizenship == ECitizenship.Danish)
            return EOrigin12.ous;
          else
            return EOrigin12.ouis;

        case EPopulationStatus.Immigrant:

          switch (country)
          {
            case ECountry.NonWestern:

              if (citizenship == ECitizenship.Danish)
                return EOrigin12.imus;
              else
                return EOrigin12.imuis;

            case ECountry.Western:

              if (citizenship == ECitizenship.Danish)
                return EOrigin12.ius;
              else
                return EOrigin12.iuis;

            default:
              throw new Exception("Ukendt oprindelsesland");
          }

        case EPopulationStatus.Descendent:

          switch (country)
          {
            case ECountry.NonWestern:

              if (citizenship == ECitizenship.Danish)
                return EOrigin12.emus;
              else
                return EOrigin12.emuis;

            case ECountry.Western:

              if (citizenship == ECitizenship.Danish)
                return EOrigin12.eus;
              else
                return EOrigin12.euis;

            default:
              throw new Exception("Ukendt oprindelsesland");
          }

        default:
          throw new Exception("Ukendt oprindelsestype");

      }
    }
    /// <summary>
    /// Danner EOrigin9-enumeration ud fra husholdningskarateristika
    /// </summary>
    /// <returns>EOrigin9-enumeration</returns>
    static public EOrigin9 Origin9(EPopulationStatus populationStatus, ECitizenship citizenship, ECountry country)
    {
      switch (populationStatus)
      {
        case EPopulationStatus.Dane:
          return EOrigin9.das;

        case EPopulationStatus.Immigrant:

          switch (country)
          {
            case ECountry.NonWestern:

              if (citizenship == ECitizenship.Danish)
                return EOrigin9.ixs;
              else
                return EOrigin9.ixis;

            case ECountry.Western:

              if (citizenship == ECitizenship.Danish)
                return EOrigin9.iws;
              else
                return EOrigin9.iwis;

            default:
              throw new Exception("Ukendt oprindelsesland");
          }

        case EPopulationStatus.Descendent:

          switch (country)
          {
            case ECountry.NonWestern:

              if (citizenship == ECitizenship.Danish)
                return EOrigin9.dxs;
              else
                return EOrigin9.dxis;

            case ECountry.Western:

              if (citizenship == ECitizenship.Danish)
                return EOrigin9.dws;
              else
                return EOrigin9.dwis;

            default:
              throw new Exception("Ukendt oprindelsesland");
          }

        default:
          throw new Exception("Ukendt oprindelsestype");

      }
    }

    /// <summary>
    /// Danner EOrigin9-enumeration ud fra tekst-streng 
    /// </summary>
    /// <param name="txt">Tekst-streng (eks: "das")</param>
    /// <returns>EOrigin9-enumeration</returns>
    static public EOrigin9 Origin9(string txt)
    {
      string s = txt.ToLower();

      if (s == "das")  return EOrigin9.das;
      if (s == "dwis") return EOrigin9.dwis;
      if (s == "dws")  return EOrigin9.dws;
      if (s == "dxis") return EOrigin9.dxis;
      if (s == "dxs")  return EOrigin9.dxs;
      if (s == "iwis") return EOrigin9.iwis;
      if (s == "iws")  return EOrigin9.iws;
      if (s == "ixis") return EOrigin9.ixis;
      if (s == "ixs")  return EOrigin9.ixs;

      throw new Exception("Ugenkendelig input-tekst");
    }

    /// <summary>
    /// Omregner fra DREAMs 9 oprindelsesgrupper til DREAMs 5 oprindelsesgrupper.
    /// </summary>
    /// <param name="origin">Oprindelse efter DREAMs 9 oprindelsesgrupper.</param>
    /// <returns>Returnerer oprindelse på DREAMs 5-gruppering.</returns>
    static public EOrigin5 Origin5(EOrigin9 origin)
    {
      switch (origin)
      {
        case EOrigin9.das:
          return EOrigin5.DanskOprindelse;

        case EOrigin9.ixis:
        case EOrigin9.ixs:
          return EOrigin5.IndvandrereIkkeVestligeLande;

        case EOrigin9.iwis:
        case EOrigin9.iws:
          return EOrigin5.IndvandrereVestligeLande;

        case EOrigin9.dxis:
        case EOrigin9.dxs:
          return EOrigin5.EfterkommereIkkeVestligeLande;

        case EOrigin9.dwis:
        case EOrigin9.dws:
          return EOrigin5.EfterkommereVestligeLande;

        default:
          throw new Exception("Ukendt oprindelse");
      }
    }
    
    /// <summary>
    /// Omregner fra DREAMs 12 oprindelsesgrupper til DREAMs 5 oprindelsesgrupper.
    /// </summary>
    /// <param name="origin">Oprindelse efter DREAMs 12 oprindelsesgrupper.</param>
    /// <returns>Returnerer oprindelse på DREAMs 5-gruppering.</returns>
    static public EOrigin5 Origin5(EOrigin12 origin)
    {
      switch (origin)
      {
        case EOrigin12.omuis:
        case EOrigin12.omus:
        case EOrigin12.ouis:
        case EOrigin12.ous:

          return EOrigin5.DanskOprindelse;

        case EOrigin12.imuis:
        case EOrigin12.imus:

          return EOrigin5.IndvandrereIkkeVestligeLande;

        case EOrigin12.iuis:
        case EOrigin12.ius:

          return EOrigin5.IndvandrereVestligeLande;

        case EOrigin12.emuis:
        case EOrigin12.emus:

          return EOrigin5.EfterkommereIkkeVestligeLande;

        case EOrigin12.euis:
        case EOrigin12.eus:

          return EOrigin5.EfterkommereVestligeLande;

        default:
          throw new Exception("Ukendt oprindelse");
      }
    }

    /// <summary>
    /// Omregner fra DREAMs 5 oprindelsesgrupper (heltal) til anvendte textforkortelser (da, ix, iw, dx, dw).
    /// </summary>
    /// <param name="origin">Oprindelsesvariabel (heltal), som skal konverteres.</param>
    /// <returns>Returnerer tekstforkortelse for oprindelsesgruppen (da, ix, iw, dx eller dw).</returns>
    static public string Origin5ToString(EOrigin5 origin)
    {
      switch (origin)
      {
        case EOrigin5.DanskOprindelse:
          return "da";
        case EOrigin5.IndvandrereIkkeVestligeLande:
          return "ix";
        case EOrigin5.IndvandrereVestligeLande:
          return "iw";
        case EOrigin5.EfterkommereIkkeVestligeLande:
          return "dx";
        case EOrigin5.EfterkommereVestligeLande:
          return "dw";
        default:
          throw new Exception("Ukendt oprindelse");
      }
    }

    /// <summary>
    /// Omregner fra DREAMs 12 oprindelsesgrupper (heltal) til anvendte textforkortelser (dais, das, ixis, ixs, iwis, iws, dxis, dxs, dwis, dws).
    /// </summary>
    /// <param name="origin">Oprindelsesvariabel (heltal), som skal konverteres.</param>
    /// <returns>Returnerer tekstforkortelse for oprindelsesgruppen (dais, das, ixis, ixs, iwis, iws, dxis, dxs, dwis eller dws).</returns>
    public string MapOrigin_text(int origin)
    {
      switch (origin)
      {
        case 2:
          return "dais";
        case 3:
          return "das";
        case 4:
          return "ixis";
        case 5:
          return "ixs";
        case 6:
          return "iwis";
        case 7:
          return "iws";
        case 8:
          return "dxis";
        case 9:
          return "dxs";
        case 10:
          return "dwis";
        case 11:
          return "dws";
        default:
          throw new Exception("Ukendt oprindelse");
      }
    }

    /// <summary>
    /// Beregner en persons landsdel ud fra Danmarks Statistiks kode for landsdele.
    /// </summary>
    /// <param name="inputDSTkode">Kode for landsdel efter Danmarks Statistiks værdisæt.</param>
    /// <returns>Returnerer landsdel efter DREAMs anvendte koder.</returns>
    static public EDwellingRegion Region11(int inputDSTkode)
    {
      if (inputDSTkode == 810) //NER: Her kunne man med fordel anvende en switch condition
        return EDwellingRegion.JyllandNord;
      else if (inputDSTkode == 820)
        return EDwellingRegion.JyllandOest;
      else if (inputDSTkode == 821)
        return EDwellingRegion.JyllandVest;
      else if (inputDSTkode == 830)
        return EDwellingRegion.Fyn;
      else if (inputDSTkode == 831)
        return EDwellingRegion.JyllandSyd;
      else if (inputDSTkode == 840)
        return EDwellingRegion.KoebenhavnBy;
      else if (inputDSTkode == 841)
        return EDwellingRegion.KoebenhavnOmegn;
      else if (inputDSTkode == 842)
        return EDwellingRegion.SjaellandNord;
      else if (inputDSTkode == 843)
        return EDwellingRegion.Bornholm;
      else if (inputDSTkode == 850)
        return EDwellingRegion.SjaellandOest;
      else if (inputDSTkode == 851)
        return EDwellingRegion.SjaellandVestogSyd;
      else if (inputDSTkode == 99)
        return EDwellingRegion.Ukendt;
      else
        throw new Exception(String.Format("Ukendt kode for landsdel: {0}", inputDSTkode));
    }

    /// <summary>
    /// Omregner EEducationDS8 til 5 uddannelser (Ufaglært, Gymnasial, Erhvervsfaglig, Kort+mellem, Lang)
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    static public int Education5(EEducationDS8 e)
    {
      if (e < EEducationDS8.Almengymnasial)
        return 0; // Ufaglært
      else if (e < EEducationDS8.ErhvervsfagligeGrundforlob)
        return 1; // Gymnasial
      else if (e <= EEducationDS8.ErhvervsfagligeHovedforlob)
        return 2; // Erhvervsfaglig
      else if (e < EEducationDS8.UdelteKandidat)
        return 3; // Kort + mellem
      else
        return 4; //Lang
    }

    /// <summary>
    /// Mapping fra Danmarks Statistiks kommunekoder til enumeration anvendt i mikrosimulationsmodellen.
    /// </summary>
    /// <param name="municipality">Kommunekode ifølge Danmarks Statistiks gruppering.</param>
    /// <returns>Returnerer enumeration for kommunen.</returns>
    static public EDwellingMunicipality MapMunicipality(int municipality)
    {
      switch (municipality)
      {
        // København by
        case 101: return EDwellingMunicipality.København;
        case 147: return EDwellingMunicipality.Frederiksberg;
        case 155: return EDwellingMunicipality.Dragør;
        case 185: return EDwellingMunicipality.Tårnby;
        // Københavns omegn
        case 151: return EDwellingMunicipality.Ballerup;
        case 153: return EDwellingMunicipality.Brøndby;
        case 157: return EDwellingMunicipality.Gentofte;
        case 159: return EDwellingMunicipality.Gladsaxe;
        case 161: return EDwellingMunicipality.Glostrup;
        case 163: return EDwellingMunicipality.Herlev;
        case 165: return EDwellingMunicipality.Albertslund;
        case 167: return EDwellingMunicipality.Hvidovre;
        case 169: return EDwellingMunicipality.HøjeTaastrup;
        case 173: return EDwellingMunicipality.LyngbyTaarbæk;
        case 175: return EDwellingMunicipality.Rødovre;
        case 183: return EDwellingMunicipality.Ishøj;
        case 187: return EDwellingMunicipality.Vallensbæk;
        // Nordsjælland
        case 190: return EDwellingMunicipality.Furesø;
        case 201: return EDwellingMunicipality.Allerød;
        case 210: return EDwellingMunicipality.Fredensborg;
        case 217: return EDwellingMunicipality.Helsingør;
        case 219: return EDwellingMunicipality.Hillerød;
        case 223: return EDwellingMunicipality.Hørsholm;
        case 230: return EDwellingMunicipality.Rudersdal;
        case 240: return EDwellingMunicipality.Egedal;
        case 250: return EDwellingMunicipality.Frederikssund;
        case 260: return EDwellingMunicipality.FrederiksværkHundested;
        case 270: return EDwellingMunicipality.Gribskov;
        // Bornholm
        case 400: return EDwellingMunicipality.Bornholm;
        //PSP
        //case 411: return EDwellingMunicipality.Christiansø;
        case 411: return EDwellingMunicipality.Bornholm;
        // Østsjælland
        case 253: return EDwellingMunicipality.Greve;
        case 259: return EDwellingMunicipality.Køge;
        case 265: return EDwellingMunicipality.Roskilde;
        case 269: return EDwellingMunicipality.Solrød;
        case 350: return EDwellingMunicipality.Lejre;
        // Vest- og Sydsjælland
        case 306: return EDwellingMunicipality.Odsherred;
        case 316: return EDwellingMunicipality.Holbæk;
        case 320: return EDwellingMunicipality.Faxe;
        case 326: return EDwellingMunicipality.Kalundborg;
        case 329: return EDwellingMunicipality.Ringsted;
        case 330: return EDwellingMunicipality.Slagelse;
        case 336: return EDwellingMunicipality.Stevns;
        case 340: return EDwellingMunicipality.Sorø;
        case 360: return EDwellingMunicipality.Lolland;
        case 370: return EDwellingMunicipality.Næstved;
        case 376: return EDwellingMunicipality.Guldborgsund;
        case 390: return EDwellingMunicipality.Vordingborg;
        // Fyn
        case 410: return EDwellingMunicipality.Middelfart;
        case 420: return EDwellingMunicipality.Assens;
        case 430: return EDwellingMunicipality.FaaborgMidtfyn;
        case 440: return EDwellingMunicipality.Kerteminde;
        case 450: return EDwellingMunicipality.Nyborg;
        case 461: return EDwellingMunicipality.Odense;
        case 479: return EDwellingMunicipality.Svendborg;
        case 480: return EDwellingMunicipality.Nordfyns;
        case 482: return EDwellingMunicipality.Langeland;
        case 492: return EDwellingMunicipality.Ærø;
        // Sydjylland
        case 510: return EDwellingMunicipality.Haderslev;
        case 530: return EDwellingMunicipality.Billund;
        case 540: return EDwellingMunicipality.Sønderborg;
        case 550: return EDwellingMunicipality.Tønder;
        case 561: return EDwellingMunicipality.Esbjerg;
        case 563: return EDwellingMunicipality.Fanø;
        case 573: return EDwellingMunicipality.Varde;
        case 575: return EDwellingMunicipality.Vejen;
        case 580: return EDwellingMunicipality.Aabenraa;
        case 607: return EDwellingMunicipality.Fredericia;
        case 621: return EDwellingMunicipality.Kolding;
        case 630: return EDwellingMunicipality.Vejle;
        // Østjylland
        case 706: return EDwellingMunicipality.Syddjurs;
        case 707: return EDwellingMunicipality.Norddjurs;
        case 710: return EDwellingMunicipality.Favrskov;
        case 727: return EDwellingMunicipality.Odder;
        case 730: return EDwellingMunicipality.Randers;
        case 740: return EDwellingMunicipality.Silkeborg;
        case 741: return EDwellingMunicipality.Samsø;
        case 746: return EDwellingMunicipality.Skanderborg;
        case 751: return EDwellingMunicipality.Aarhus;
        case 766: return EDwellingMunicipality.Hedensted;
        case 615: return EDwellingMunicipality.Horsens;
        // Vestjylland
        case 657: return EDwellingMunicipality.Herning;
        case 661: return EDwellingMunicipality.Holstebro;
        case 665: return EDwellingMunicipality.Lemvig;
        case 671: return EDwellingMunicipality.Struer;
        case 756: return EDwellingMunicipality.IkastBrande;
        case 760: return EDwellingMunicipality.RingkøbingSkjern;
        case 779: return EDwellingMunicipality.Skive;
        case 791: return EDwellingMunicipality.Viborg;
        // Nordjylland
        case 773: return EDwellingMunicipality.Morsø;
        case 787: return EDwellingMunicipality.Thisted;
        case 810: return EDwellingMunicipality.Brønderslev;
        case 813: return EDwellingMunicipality.Frederikshavn;
        case 820: return EDwellingMunicipality.Vesthimmerlands;
        case 825: return EDwellingMunicipality.Læsø;
        case 840: return EDwellingMunicipality.Rebild;
        case 846: return EDwellingMunicipality.Mariagerfjord;
        case 849: return EDwellingMunicipality.Jammerbugt;
        case 851: return EDwellingMunicipality.Aalborg;
        case 860: return EDwellingMunicipality.Hjørring;
        
        default:
          throw new Exception(String.Format("Ukendt kommunekode: {0}", municipality));
      }
    }

    /// <summary>
    /// Mapping fra enumeration anvendt i mikrosimulationsmodellen til kommuneindex anvendt i CTREEs (i dette index slåes Bornholm og Christiansø sammen til én, og kommunerne nummereres herefter fra 0 til 97 i henhold til deres placering på DSTs liste over kommunenumre).
    /// </summary>
    /// <param name="municipality">Enumeration for kommunen.</param>
    /// <returns>Returnerer index for kummuner, som anvedes i CTREEs.</returns>
    static public int MapMunicipality2index(EDwellingMunicipality municipality)
    {
      switch (municipality)
      {
        case EDwellingMunicipality.København: return 0;
        case EDwellingMunicipality.Frederiksberg: return 1;
        case EDwellingMunicipality.Ballerup: return 2;
        case EDwellingMunicipality.Brøndby: return 3;
        case EDwellingMunicipality.Dragør: return 4;
        case EDwellingMunicipality.Gentofte: return 5;
        case EDwellingMunicipality.Gladsaxe: return 6;
        case EDwellingMunicipality.Glostrup: return 7;
        case EDwellingMunicipality.Herlev: return 8;
        case EDwellingMunicipality.Albertslund: return 9;
        case EDwellingMunicipality.Hvidovre: return 10;
        case EDwellingMunicipality.HøjeTaastrup: return 11;
        case EDwellingMunicipality.LyngbyTaarbæk: return 12;
        case EDwellingMunicipality.Rødovre: return 13;
        case EDwellingMunicipality.Ishøj: return 14;
        case EDwellingMunicipality.Tårnby: return 15;
        case EDwellingMunicipality.Vallensbæk: return 16;
        case EDwellingMunicipality.Furesø: return 17;
        case EDwellingMunicipality.Allerød: return 18;
        case EDwellingMunicipality.Fredensborg: return 19;
        case EDwellingMunicipality.Helsingør: return 20;
        case EDwellingMunicipality.Hillerød: return 21;
        case EDwellingMunicipality.Hørsholm: return 22;
        case EDwellingMunicipality.Rudersdal: return 23;
        case EDwellingMunicipality.Egedal: return 24;
        case EDwellingMunicipality.Frederikssund: return 25;
        case EDwellingMunicipality.Greve: return 26;
        case EDwellingMunicipality.Køge: return 27;
        case EDwellingMunicipality.FrederiksværkHundested: return 28;
        case EDwellingMunicipality.Roskilde: return 29;
        case EDwellingMunicipality.Solrød: return 30;
        case EDwellingMunicipality.Gribskov: return 31;
        case EDwellingMunicipality.Odsherred: return 32;
        case EDwellingMunicipality.Holbæk: return 33;
        case EDwellingMunicipality.Faxe: return 34;
        case EDwellingMunicipality.Kalundborg: return 35;
        case EDwellingMunicipality.Ringsted: return 36;
        case EDwellingMunicipality.Slagelse: return 37;
        case EDwellingMunicipality.Stevns: return 38;
        case EDwellingMunicipality.Sorø: return 39;
        case EDwellingMunicipality.Lejre: return 40;
        case EDwellingMunicipality.Lolland: return 41;
        case EDwellingMunicipality.Næstved: return 42;
        case EDwellingMunicipality.Guldborgsund: return 43;
        case EDwellingMunicipality.Vordingborg: return 44;
        case EDwellingMunicipality.Bornholm: return 45;
        case EDwellingMunicipality.Middelfart: return 46;
        //PSP
        //case EDwellingMunicipality.Christiansø: return 45;
        case EDwellingMunicipality.Assens: return 47;
        case EDwellingMunicipality.FaaborgMidtfyn: return 48;
        case EDwellingMunicipality.Kerteminde: return 49;
        case EDwellingMunicipality.Nyborg: return 50;
        case EDwellingMunicipality.Odense: return 51;
        case EDwellingMunicipality.Svendborg: return 52;
        case EDwellingMunicipality.Nordfyns: return 53;
        case EDwellingMunicipality.Langeland: return 54;
        case EDwellingMunicipality.Ærø: return 55;
        case EDwellingMunicipality.Haderslev: return 56;
        case EDwellingMunicipality.Billund: return 57;
        case EDwellingMunicipality.Sønderborg: return 58;
        case EDwellingMunicipality.Tønder: return 59;
        case EDwellingMunicipality.Esbjerg: return 60;
        case EDwellingMunicipality.Fanø: return 61;
        case EDwellingMunicipality.Varde: return 62;
        case EDwellingMunicipality.Vejen: return 63;
        case EDwellingMunicipality.Aabenraa: return 64;
        case EDwellingMunicipality.Fredericia: return 65;
        case EDwellingMunicipality.Horsens: return 66;
        case EDwellingMunicipality.Kolding: return 67;
        case EDwellingMunicipality.Vejle: return 68;
        case EDwellingMunicipality.Herning: return 69;
        case EDwellingMunicipality.Holstebro: return 70;
        case EDwellingMunicipality.Lemvig: return 71;
        case EDwellingMunicipality.Struer: return 72;
        case EDwellingMunicipality.Syddjurs: return 73;
        case EDwellingMunicipality.Norddjurs: return 74;
        case EDwellingMunicipality.Favrskov: return 75;
        case EDwellingMunicipality.Odder: return 76;
        case EDwellingMunicipality.Randers: return 77;
        case EDwellingMunicipality.Silkeborg: return 78;
        case EDwellingMunicipality.Samsø: return 79;
        case EDwellingMunicipality.Skanderborg: return 80;
        case EDwellingMunicipality.Aarhus: return 81;
        case EDwellingMunicipality.IkastBrande: return 82;
        case EDwellingMunicipality.RingkøbingSkjern: return 83;
        case EDwellingMunicipality.Hedensted: return 84;
        case EDwellingMunicipality.Morsø: return 85;
        case EDwellingMunicipality.Skive: return 86;
        case EDwellingMunicipality.Thisted: return 87;
        case EDwellingMunicipality.Viborg: return 88;
        case EDwellingMunicipality.Brønderslev: return 89;
        case EDwellingMunicipality.Frederikshavn: return 90;
        case EDwellingMunicipality.Vesthimmerlands: return 91;
        case EDwellingMunicipality.Læsø: return 92;
        case EDwellingMunicipality.Rebild: return 93;
        case EDwellingMunicipality.Mariagerfjord: return 94;
        case EDwellingMunicipality.Jammerbugt: return 95;
        case EDwellingMunicipality.Aalborg: return 96;
        case EDwellingMunicipality.Hjørring: return 97;
        
    default:
          throw new Exception(String.Format("Ukendt enumeration for kommunekode: {0}", municipality));
      }
    }

      /// <summary>
    /// Mapping fra kommuneindex anvendt i CTREEs til enumeration anvendt i mikrosimulationsmodellen (Der afbilledes ikke noget indeks til ChristiansØ).
      /// </summary>
      /// <param name="municipalityindex"></param>
      /// <returns></returns>
      static public EDwellingMunicipality MapIndex2Municipality(int municipalityindex)
    {
        switch (municipalityindex)
        {
            case 0: return EDwellingMunicipality.København;
            case 1: return EDwellingMunicipality.Frederiksberg;
            case 2: return EDwellingMunicipality.Ballerup;
            case 3: return EDwellingMunicipality.Brøndby;
            case 4: return EDwellingMunicipality.Dragør;
            case 5: return EDwellingMunicipality.Gentofte;
            case 6: return EDwellingMunicipality.Gladsaxe;
            case 7: return EDwellingMunicipality.Glostrup;
            case 8: return EDwellingMunicipality.Herlev;
            case 9: return EDwellingMunicipality.Albertslund;
            case 10: return EDwellingMunicipality.Hvidovre;
            case 11: return EDwellingMunicipality.HøjeTaastrup;
            case 12: return EDwellingMunicipality.LyngbyTaarbæk;
            case 13: return EDwellingMunicipality.Rødovre;
            case 14: return EDwellingMunicipality.Ishøj;
            case 15: return EDwellingMunicipality.Tårnby;
            case 16: return EDwellingMunicipality.Vallensbæk;
            case 17: return EDwellingMunicipality.Furesø;
            case 18: return EDwellingMunicipality.Allerød;
            case 19: return EDwellingMunicipality.Fredensborg;
            case 20: return EDwellingMunicipality.Helsingør;
            case 21: return EDwellingMunicipality.Hillerød;
            case 22: return EDwellingMunicipality.Hørsholm;
            case 23: return EDwellingMunicipality.Rudersdal;
            case 24: return EDwellingMunicipality.Egedal;
            case 25: return EDwellingMunicipality.Frederikssund;
            case 26: return EDwellingMunicipality.Greve;
            case 27: return EDwellingMunicipality.Køge;
            case 28: return EDwellingMunicipality.FrederiksværkHundested;
            case 29: return EDwellingMunicipality.Roskilde;
            case 30: return EDwellingMunicipality.Solrød;
            case 31: return EDwellingMunicipality.Gribskov;
            case 32: return EDwellingMunicipality.Odsherred;
            case 33: return EDwellingMunicipality.Holbæk;
            case 34: return EDwellingMunicipality.Faxe;
            case 35: return EDwellingMunicipality.Kalundborg;
            case 36: return EDwellingMunicipality.Ringsted;
            case 37: return EDwellingMunicipality.Slagelse;
            case 38: return EDwellingMunicipality.Stevns;
            case 39: return EDwellingMunicipality.Sorø;
            case 40: return EDwellingMunicipality.Lejre;
            case 41: return EDwellingMunicipality.Lolland;
            case 42: return EDwellingMunicipality.Næstved;
            case 43: return EDwellingMunicipality.Guldborgsund;
            case 44: return EDwellingMunicipality.Vordingborg;
            case 45: return EDwellingMunicipality.Bornholm;
            case 46: return EDwellingMunicipality.Middelfart;
            case 47: return EDwellingMunicipality.Assens;
            case 48: return EDwellingMunicipality.FaaborgMidtfyn;
            case 49: return EDwellingMunicipality.Kerteminde;
            case 50: return EDwellingMunicipality.Nyborg;
            case 51: return EDwellingMunicipality.Odense;
            case 52: return EDwellingMunicipality.Svendborg;
            case 53: return EDwellingMunicipality.Nordfyns;
            case 54: return EDwellingMunicipality.Langeland;
            case 55: return EDwellingMunicipality.Ærø;
            case 56: return EDwellingMunicipality.Haderslev;
            case 57: return EDwellingMunicipality.Billund;
            case 58: return EDwellingMunicipality.Sønderborg;
            case 59: return EDwellingMunicipality.Tønder;
            case 60: return EDwellingMunicipality.Esbjerg;
            case 61: return EDwellingMunicipality.Fanø;
            case 62: return EDwellingMunicipality.Varde;
            case 63: return EDwellingMunicipality.Vejen;
            case 64: return EDwellingMunicipality.Aabenraa;
            case 65: return EDwellingMunicipality.Fredericia;
            case 66: return EDwellingMunicipality.Horsens;
            case 67: return EDwellingMunicipality.Kolding;
            case 68: return EDwellingMunicipality.Vejle;
            case 69: return EDwellingMunicipality.Herning;
            case 70: return EDwellingMunicipality.Holstebro;
            case 71: return EDwellingMunicipality.Lemvig;
            case 72: return EDwellingMunicipality.Struer;
            case 73: return EDwellingMunicipality.Syddjurs;
            case 74: return EDwellingMunicipality.Norddjurs;
            case 75: return EDwellingMunicipality.Favrskov;
            case 76: return EDwellingMunicipality.Odder;
            case 77: return EDwellingMunicipality.Randers;
            case 78: return EDwellingMunicipality.Silkeborg;
            case 79: return EDwellingMunicipality.Samsø;
            case 80: return EDwellingMunicipality.Skanderborg;
            case 81: return EDwellingMunicipality.Aarhus;
            case 82: return EDwellingMunicipality.IkastBrande;
            case 83: return EDwellingMunicipality.RingkøbingSkjern;
            case 84: return EDwellingMunicipality.Hedensted;
            case 85: return EDwellingMunicipality.Morsø;
            case 86: return EDwellingMunicipality.Skive;
            case 87: return EDwellingMunicipality.Thisted;
            case 88: return EDwellingMunicipality.Viborg;
            case 89: return EDwellingMunicipality.Brønderslev;
            case 90: return EDwellingMunicipality.Frederikshavn;
            case 91: return EDwellingMunicipality.Vesthimmerlands;
            case 92: return EDwellingMunicipality.Læsø;
            case 93: return EDwellingMunicipality.Rebild;
            case 94: return EDwellingMunicipality.Mariagerfjord;
            case 95: return EDwellingMunicipality.Jammerbugt;
            case 96: return EDwellingMunicipality.Aalborg;
            case 97: return EDwellingMunicipality.Hjørring;

            default:
                throw new Exception(String.Format("Ukendt kommuneindeks: {0}", municipalityindex));
        }
    }
    


    /// <summary>
    /// Mapping fra kommunekoder til landsdele.
    /// </summary>
    /// <param name="municipality">Kommunekode som enumeration.</param>
    /// <returns>Returnerer enumeration for landsdelen.</returns>
    static public EDwellingRegion MapMunicipality2Region(EDwellingMunicipality municipality)
    {
      switch (municipality)
      {
        // København by
        case EDwellingMunicipality.København: return EDwellingRegion.KoebenhavnBy;
        case EDwellingMunicipality.Frederiksberg: return EDwellingRegion.KoebenhavnBy;
        case EDwellingMunicipality.Dragør: return EDwellingRegion.KoebenhavnBy;
        case EDwellingMunicipality.Tårnby: return EDwellingRegion.KoebenhavnBy;
        // Københavns omegn
        case EDwellingMunicipality.Albertslund: return EDwellingRegion.KoebenhavnOmegn;
        case EDwellingMunicipality.Ballerup: return EDwellingRegion.KoebenhavnOmegn;
        case EDwellingMunicipality.Brøndby: return EDwellingRegion.KoebenhavnOmegn;
        case EDwellingMunicipality.Gentofte: return EDwellingRegion.KoebenhavnOmegn;
        case EDwellingMunicipality.Gladsaxe: return EDwellingRegion.KoebenhavnOmegn;
        case EDwellingMunicipality.Glostrup: return EDwellingRegion.KoebenhavnOmegn;
        case EDwellingMunicipality.Herlev: return EDwellingRegion.KoebenhavnOmegn;
        case EDwellingMunicipality.Hvidovre: return EDwellingRegion.KoebenhavnOmegn;
        case EDwellingMunicipality.HøjeTaastrup: return EDwellingRegion.KoebenhavnOmegn;
        case EDwellingMunicipality.Ishøj: return EDwellingRegion.KoebenhavnOmegn;
        case EDwellingMunicipality.LyngbyTaarbæk: return EDwellingRegion.KoebenhavnOmegn;
        case EDwellingMunicipality.Rødovre: return EDwellingRegion.KoebenhavnOmegn;
        case EDwellingMunicipality.Vallensbæk: return EDwellingRegion.KoebenhavnOmegn;
        // Nordsjælland
        case EDwellingMunicipality.Allerød: return EDwellingRegion.SjaellandNord;
        case EDwellingMunicipality.Egedal: return EDwellingRegion.SjaellandNord;
        case EDwellingMunicipality.Fredensborg: return EDwellingRegion.SjaellandNord;
        case EDwellingMunicipality.Frederikssund: return EDwellingRegion.SjaellandNord;
        case EDwellingMunicipality.FrederiksværkHundested: return EDwellingRegion.SjaellandNord;
        case EDwellingMunicipality.Furesø: return EDwellingRegion.SjaellandNord;
        case EDwellingMunicipality.Gribskov: return EDwellingRegion.SjaellandNord;
        case EDwellingMunicipality.Helsingør: return EDwellingRegion.SjaellandNord;
        case EDwellingMunicipality.Hillerød: return EDwellingRegion.SjaellandNord;
        case EDwellingMunicipality.Hørsholm: return EDwellingRegion.SjaellandNord;
        case EDwellingMunicipality.Rudersdal: return EDwellingRegion.SjaellandNord;
        // Bornholm
        case EDwellingMunicipality.Bornholm: return EDwellingRegion.Bornholm;
        //PSP
        //case EDwellingMunicipality.Christiansø: return EDwellingRegion.Bornholm;
        // Østsjælland
        case EDwellingMunicipality.Greve: return EDwellingRegion.SjaellandOest;
        case EDwellingMunicipality.Køge: return EDwellingRegion.SjaellandOest;
        case EDwellingMunicipality.Lejre: return EDwellingRegion.SjaellandOest;
        case EDwellingMunicipality.Roskilde: return EDwellingRegion.SjaellandOest;
        case EDwellingMunicipality.Solrød: return EDwellingRegion.SjaellandOest;
        // Vest- og Sydsjælland
        case EDwellingMunicipality.Faxe: return EDwellingRegion.SjaellandVestogSyd;
        case EDwellingMunicipality.Guldborgsund: return EDwellingRegion.SjaellandVestogSyd;
        case EDwellingMunicipality.Holbæk: return EDwellingRegion.SjaellandVestogSyd;
        case EDwellingMunicipality.Kalundborg: return EDwellingRegion.SjaellandVestogSyd;
        case EDwellingMunicipality.Lolland: return EDwellingRegion.SjaellandVestogSyd;
        case EDwellingMunicipality.Næstved: return EDwellingRegion.SjaellandVestogSyd;
        case EDwellingMunicipality.Odsherred: return EDwellingRegion.SjaellandVestogSyd;
        case EDwellingMunicipality.Ringsted: return EDwellingRegion.SjaellandVestogSyd;
        case EDwellingMunicipality.Slagelse: return EDwellingRegion.SjaellandVestogSyd;
        case EDwellingMunicipality.Sorø: return EDwellingRegion.SjaellandVestogSyd;
        case EDwellingMunicipality.Stevns: return EDwellingRegion.SjaellandVestogSyd;
        case EDwellingMunicipality.Vordingborg: return EDwellingRegion.SjaellandVestogSyd;
        // Fyn
        case EDwellingMunicipality.Assens: return EDwellingRegion.Fyn;
        case EDwellingMunicipality.FaaborgMidtfyn: return EDwellingRegion.Fyn;
        case EDwellingMunicipality.Kerteminde: return EDwellingRegion.Fyn;
        case EDwellingMunicipality.Langeland: return EDwellingRegion.Fyn;
        case EDwellingMunicipality.Middelfart: return EDwellingRegion.Fyn;
        case EDwellingMunicipality.Nordfyns: return EDwellingRegion.Fyn;
        case EDwellingMunicipality.Nyborg: return EDwellingRegion.Fyn;
        case EDwellingMunicipality.Odense: return EDwellingRegion.Fyn;
        case EDwellingMunicipality.Svendborg: return EDwellingRegion.Fyn;
        case EDwellingMunicipality.Ærø: return EDwellingRegion.Fyn;
        // Sydjylland
        case EDwellingMunicipality.Billund: return EDwellingRegion.JyllandSyd;
        case EDwellingMunicipality.Esbjerg: return EDwellingRegion.JyllandSyd;
        case EDwellingMunicipality.Fanø: return EDwellingRegion.JyllandSyd;
        case EDwellingMunicipality.Fredericia: return EDwellingRegion.JyllandSyd;
        case EDwellingMunicipality.Haderslev: return EDwellingRegion.JyllandSyd;
        case EDwellingMunicipality.Kolding: return EDwellingRegion.JyllandSyd;
        case EDwellingMunicipality.Sønderborg: return EDwellingRegion.JyllandSyd;
        case EDwellingMunicipality.Tønder: return EDwellingRegion.JyllandSyd;
        case EDwellingMunicipality.Varde: return EDwellingRegion.JyllandSyd;
        case EDwellingMunicipality.Vejen: return EDwellingRegion.JyllandSyd;
        case EDwellingMunicipality.Vejle: return EDwellingRegion.JyllandSyd;
        case EDwellingMunicipality.Aabenraa: return EDwellingRegion.JyllandSyd;
        // Østjylland
        case EDwellingMunicipality.Favrskov: return EDwellingRegion.JyllandOest;
        case EDwellingMunicipality.Hedensted: return EDwellingRegion.JyllandOest;
        case EDwellingMunicipality.Horsens: return EDwellingRegion.JyllandOest;
        case EDwellingMunicipality.Norddjurs: return EDwellingRegion.JyllandOest;
        case EDwellingMunicipality.Odder: return EDwellingRegion.JyllandOest;
        case EDwellingMunicipality.Randers: return EDwellingRegion.JyllandOest;
        case EDwellingMunicipality.Samsø: return EDwellingRegion.JyllandOest;
        case EDwellingMunicipality.Silkeborg: return EDwellingRegion.JyllandOest;
        case EDwellingMunicipality.Skanderborg: return EDwellingRegion.JyllandOest;
        case EDwellingMunicipality.Syddjurs: return EDwellingRegion.JyllandOest;
        case EDwellingMunicipality.Aarhus: return EDwellingRegion.JyllandOest;
        // Vestjylland
        case EDwellingMunicipality.Herning: return EDwellingRegion.JyllandVest;
        case EDwellingMunicipality.Holstebro: return EDwellingRegion.JyllandVest;
        case EDwellingMunicipality.IkastBrande: return EDwellingRegion.JyllandVest;
        case EDwellingMunicipality.Lemvig: return EDwellingRegion.JyllandVest;
        case EDwellingMunicipality.RingkøbingSkjern: return EDwellingRegion.JyllandVest;
        case EDwellingMunicipality.Skive: return EDwellingRegion.JyllandVest;
        case EDwellingMunicipality.Struer: return EDwellingRegion.JyllandVest;
        case EDwellingMunicipality.Viborg: return EDwellingRegion.JyllandVest;
        // Nordjylland
        case EDwellingMunicipality.Brønderslev: return EDwellingRegion.JyllandNord;
        case EDwellingMunicipality.Frederikshavn: return EDwellingRegion.JyllandNord;
        case EDwellingMunicipality.Hjørring: return EDwellingRegion.JyllandNord;
        case EDwellingMunicipality.Jammerbugt: return EDwellingRegion.JyllandNord;
        case EDwellingMunicipality.Læsø: return EDwellingRegion.JyllandNord;
        case EDwellingMunicipality.Mariagerfjord: return EDwellingRegion.JyllandNord;
        case EDwellingMunicipality.Morsø: return EDwellingRegion.JyllandNord;
        case EDwellingMunicipality.Rebild: return EDwellingRegion.JyllandNord;
        case EDwellingMunicipality.Thisted: return EDwellingRegion.JyllandNord;
        case EDwellingMunicipality.Vesthimmerlands: return EDwellingRegion.JyllandNord;
        case EDwellingMunicipality.Aalborg: return EDwellingRegion.JyllandNord;

        default:
          throw new Exception("Ukendt kommunekode");
      }
    }




    static public int MapMunicipality2index2019PCA1(EDwellingMunicipality municipality)
    {
        switch (municipality)
        {
            case EDwellingMunicipality.Aabenraa: return 29;
            case EDwellingMunicipality.Aalborg: return 52;
            case EDwellingMunicipality.Aarhus: return 78;
            case EDwellingMunicipality.Ærø: return 5;
            case EDwellingMunicipality.Albertslund: return 72;
            case EDwellingMunicipality.Allerød: return 91;
            case EDwellingMunicipality.Assens: return 78;
            case EDwellingMunicipality.Ballerup: return 81;
            case EDwellingMunicipality.Billund: return 56;
            case EDwellingMunicipality.Bornholm: return 6;
            case EDwellingMunicipality.Brøndby: return 80;
            case EDwellingMunicipality.Brønderslev: return 12;
            case EDwellingMunicipality.Dragør: return 87;
            case EDwellingMunicipality.Egedal: return 79;
            case EDwellingMunicipality.Esbjerg: return 41;
            case EDwellingMunicipality.FaaborgMidtfyn: return 22;
            case EDwellingMunicipality.Fanø: return 61;
            case EDwellingMunicipality.Favrskov: return 62;
            case EDwellingMunicipality.Faxe: return 40;
            case EDwellingMunicipality.Fredensborg: return 74;
            case EDwellingMunicipality.Fredericia: return 54;
            case EDwellingMunicipality.Frederiksberg: return 95;
            case EDwellingMunicipality.Frederikshavn: return 17;
            case EDwellingMunicipality.Frederikssund: return 60;
            case EDwellingMunicipality.FrederiksværkHundested: return 45;
            case EDwellingMunicipality.Furesø: return 89;
            case EDwellingMunicipality.Gentofte: return 98;
            case EDwellingMunicipality.Gladsaxe: return 92;
            case EDwellingMunicipality.Glostrup: return 90;
            case EDwellingMunicipality.Greve: return 83;
            case EDwellingMunicipality.Gribskov: return 65;
            case EDwellingMunicipality.Guldborgsund: return 11;
            case EDwellingMunicipality.Haderslev: return 26;
            case EDwellingMunicipality.Hedensted: return 48;
            case EDwellingMunicipality.Helsingør: return 67;
            case EDwellingMunicipality.Herlev: return 86;
            case EDwellingMunicipality.Herning: return 49;
            case EDwellingMunicipality.Hillerød: return 76;
            case EDwellingMunicipality.Hjørring: return 20;
            case EDwellingMunicipality.HøjeTaastrup: return 75; //40
            case EDwellingMunicipality.Holbæk: return 51;
            case EDwellingMunicipality.Holstebro: return 50;
            case EDwellingMunicipality.Horsens: return 53;
            case EDwellingMunicipality.Hørsholm: return 94;
            case EDwellingMunicipality.Hvidovre: return 73;
            case EDwellingMunicipality.IkastBrande: return 47;
            case EDwellingMunicipality.Ishøj: return 69;
            case EDwellingMunicipality.Jammerbugt: return 14;
            case EDwellingMunicipality.Kalundborg: return 25;
            case EDwellingMunicipality.Kerteminde: return 31;
            case EDwellingMunicipality.København: return 93;
            case EDwellingMunicipality.Køge: return 70;
            case EDwellingMunicipality.Kolding: return 58;
            case EDwellingMunicipality.Læsø: return 3;
            case EDwellingMunicipality.Langeland: return 2;
            case EDwellingMunicipality.Lejre: return 68;
            case EDwellingMunicipality.Lemvig: return 24;
            case EDwellingMunicipality.Lolland: return 1;
            case EDwellingMunicipality.LyngbyTaarbæk: return 96;
            case EDwellingMunicipality.Mariagerfjord: return 16;
            case EDwellingMunicipality.Middelfart: return 44;
            case EDwellingMunicipality.Morsø: return 9;
            case EDwellingMunicipality.Næstved: return 43;
            case EDwellingMunicipality.Norddjurs: return 18;
            case EDwellingMunicipality.Nordfyns: return 15;
            case EDwellingMunicipality.Nyborg: return 28;
            case EDwellingMunicipality.Odder: return 47;
            case EDwellingMunicipality.Odense: return 55;
            case EDwellingMunicipality.Odsherred: return 8;
            case EDwellingMunicipality.Randers: return 38; // 70
            case EDwellingMunicipality.Rebild: return 46;
            case EDwellingMunicipality.RingkøbingSkjern: return 32;
            case EDwellingMunicipality.Ringsted: return 59;
            case EDwellingMunicipality.Rødovre: return 77;
            case EDwellingMunicipality.Roskilde: return 82;
            case EDwellingMunicipality.Rudersdal: return 97;
            case EDwellingMunicipality.Samsø: return 4;
            case EDwellingMunicipality.Skanderborg: return 71;
            case EDwellingMunicipality.Silkeborg: return 63;
            case EDwellingMunicipality.Skive: return 21;
            case EDwellingMunicipality.Slagelse: return 42;
            case EDwellingMunicipality.Solrød: return 85;
            case EDwellingMunicipality.Sønderborg: return 33;
            case EDwellingMunicipality.Sorø: return 35;
            case EDwellingMunicipality.Stevns: return 57;
            case EDwellingMunicipality.Struer: return 30;
            case EDwellingMunicipality.Svendborg: return 27;
            case EDwellingMunicipality.Syddjurs: return 36;
            case EDwellingMunicipality.Tårnby: return 84;
            case EDwellingMunicipality.Thisted: return 10;
            case EDwellingMunicipality.Tønder: return 19;
            case EDwellingMunicipality.Vallensbæk: return 88;
            case EDwellingMunicipality.Varde: return 37;
            case EDwellingMunicipality.Vejen: return 34;
            case EDwellingMunicipality.Vejle: return 66;
            case EDwellingMunicipality.Vesthimmerlands: return 7;
            case EDwellingMunicipality.Viborg: return 39;
            case EDwellingMunicipality.Vordingborg: return 13;
            default:
                throw new Exception(String.Format("Ukendt enumeration for kommunekode: {0}", municipality));
        }
    }

    /// <summary>
    /// Mapping fra enumeration anvendt i mikrosimulationsmodellen til kommunens positionsnummer bestemt af PC 1 anvendt i CTREEs.
    /// <param name="municipality">Enumeration for kommunen.</param>
    /// <returns>>Returnerer kommunens position iflg. PC 1, som anvedes i CTREEs. Første position har indeks 0.</returns>
    static public int MapMunicipality2indexPCA1(EDwellingMunicipality municipality)
    {
      switch (municipality)
      {
        case EDwellingMunicipality.København: return 0;
        case EDwellingMunicipality.Frederiksberg: return 2;
        case EDwellingMunicipality.Ballerup: return 9;
        case EDwellingMunicipality.Brøndby: return 11;
        case EDwellingMunicipality.Dragør: return 26;
        case EDwellingMunicipality.Gentofte: return 1;
        case EDwellingMunicipality.Gladsaxe: return 6;
        case EDwellingMunicipality.Glostrup: return 8;
        case EDwellingMunicipality.Herlev: return 10;
        case EDwellingMunicipality.Albertslund: return 5;
        case EDwellingMunicipality.Hvidovre: return 19;
        case EDwellingMunicipality.HøjeTaastrup: return 16;
        case EDwellingMunicipality.LyngbyTaarbæk: return 4;
        case EDwellingMunicipality.Rødovre: return 14;
        case EDwellingMunicipality.Ishøj: return 12;
        case EDwellingMunicipality.Tårnby: return 17;
        case EDwellingMunicipality.Vallensbæk: return 23;
        case EDwellingMunicipality.Furesø: return 13;
        case EDwellingMunicipality.Allerød: return 18;
        case EDwellingMunicipality.Fredensborg: return 22;
        case EDwellingMunicipality.Helsingør: return 21;
        case EDwellingMunicipality.Hillerød: return 24;
        case EDwellingMunicipality.Hørsholm: return 7;
        case EDwellingMunicipality.Rudersdal: return 3;
        case EDwellingMunicipality.Egedal: return 30;
        case EDwellingMunicipality.Frederikssund: return 32;
        case EDwellingMunicipality.Greve: return 25;
        case EDwellingMunicipality.Køge: return 28;
        case EDwellingMunicipality.FrederiksværkHundested: return 33;
        case EDwellingMunicipality.Roskilde: return 20;
        case EDwellingMunicipality.Solrød: return 27;
        case EDwellingMunicipality.Gribskov: return 31;
        case EDwellingMunicipality.Odsherred: return 79;
        case EDwellingMunicipality.Holbæk: return 45;
        case EDwellingMunicipality.Faxe: return 59;
        case EDwellingMunicipality.Kalundborg: return 55;
        case EDwellingMunicipality.Ringsted: return 40;
        case EDwellingMunicipality.Slagelse: return 43;
        case EDwellingMunicipality.Stevns: return 49;
        case EDwellingMunicipality.Sorø: return 61;
        case EDwellingMunicipality.Lejre: return 39;
        case EDwellingMunicipality.Lolland: return 87;
        case EDwellingMunicipality.Næstved: return 50;
        case EDwellingMunicipality.Guldborgsund: return 74;
        case EDwellingMunicipality.Vordingborg: return 73;
        case EDwellingMunicipality.Bornholm: return 91;
        case EDwellingMunicipality.Middelfart: return 57;
        //PSP
        //case EDwellingMunicipality.Christiansø: return 91;
        case EDwellingMunicipality.Assens: return 78;
        case EDwellingMunicipality.FaaborgMidtfyn: return 83;
        case EDwellingMunicipality.Kerteminde: return 60;
        case EDwellingMunicipality.Nyborg: return 65;
        case EDwellingMunicipality.Odense: return 29;
        case EDwellingMunicipality.Svendborg: return 63;
        case EDwellingMunicipality.Nordfyns: return 86;
        case EDwellingMunicipality.Langeland: return 94;
        case EDwellingMunicipality.Ærø: return 96;
        case EDwellingMunicipality.Haderslev: return 68;
        case EDwellingMunicipality.Billund: return 56;
        case EDwellingMunicipality.Sønderborg: return 53;
        case EDwellingMunicipality.Tønder: return 72;
        case EDwellingMunicipality.Esbjerg: return 46;
        case EDwellingMunicipality.Fanø: return 38;
        case EDwellingMunicipality.Varde: return 67;
        case EDwellingMunicipality.Vejen: return 84;
        case EDwellingMunicipality.Aabenraa: return 58;
        case EDwellingMunicipality.Fredericia: return 34;
        case EDwellingMunicipality.Horsens: return 41;
        case EDwellingMunicipality.Kolding: return 36;
        case EDwellingMunicipality.Vejle: return 37;
        case EDwellingMunicipality.Herning: return 48;
        case EDwellingMunicipality.Holstebro: return 51;
        case EDwellingMunicipality.Lemvig: return 80;
        case EDwellingMunicipality.Struer: return 71;
        case EDwellingMunicipality.Syddjurs: return 62;
        case EDwellingMunicipality.Norddjurs: return 66;
        case EDwellingMunicipality.Favrskov: return 54;
        case EDwellingMunicipality.Odder: return 47;
        case EDwellingMunicipality.Randers: return 64;
        case EDwellingMunicipality.Silkeborg: return 44;
        case EDwellingMunicipality.Samsø: return 85;
        case EDwellingMunicipality.Skanderborg: return 42;
        case EDwellingMunicipality.Aarhus: return 15;
        case EDwellingMunicipality.IkastBrande: return 52;
        case EDwellingMunicipality.RingkøbingSkjern: return 77;
        case EDwellingMunicipality.Hedensted: return 70;
        case EDwellingMunicipality.Morsø: return 93;
        case EDwellingMunicipality.Skive: return 89;
        case EDwellingMunicipality.Thisted: return 92;
        case EDwellingMunicipality.Viborg: return 69;
        case EDwellingMunicipality.Brønderslev: return 90;
        case EDwellingMunicipality.Frederikshavn: return 75;
        case EDwellingMunicipality.Vesthimmerlands: return 97;
        case EDwellingMunicipality.Læsø: return 76;
        case EDwellingMunicipality.Rebild: return 81;
        case EDwellingMunicipality.Mariagerfjord: return 88;
        case EDwellingMunicipality.Jammerbugt: return 95;
        case EDwellingMunicipality.Aalborg: return 35;
        case EDwellingMunicipality.Hjørring: return 82;

        default:
          throw new Exception(String.Format("Ukendt enumeration for kommunekode: {0}", municipality));
      }
    }

    

    /// <summary>
    /// Mapping fra enumeration anvendt i mikrosimulationsmodellen til kommunens positionsnummer bestemt af PC 2 anvendt i CTREEs.
    /// </summary>
    /// <param name="municipality">Enumeration for kommunen.</param>
    /// <returns>>Returnerer kommunens position iflg. PC 2, som anvedes i CTREEs. Første position har indeks 0.</returns>
    static public int MapMunicipality2indexPCA2(EDwellingMunicipality municipality)
    {
      switch (municipality)
      {
        case EDwellingMunicipality.København: return 5;
        case EDwellingMunicipality.Frederiksberg: return 61;
        case EDwellingMunicipality.Ballerup: return 9;
        case EDwellingMunicipality.Brøndby: return 4;
        case EDwellingMunicipality.Dragør: return 97;
        case EDwellingMunicipality.Gentofte: return 96;
        case EDwellingMunicipality.Gladsaxe: return 32;
        case EDwellingMunicipality.Glostrup: return 19;
        case EDwellingMunicipality.Herlev: return 8;
        case EDwellingMunicipality.Albertslund: return 1;
        case EDwellingMunicipality.Hvidovre: return 20;
        case EDwellingMunicipality.HøjeTaastrup: return 13;
        case EDwellingMunicipality.LyngbyTaarbæk: return 84;
        case EDwellingMunicipality.Rødovre: return 11;
        case EDwellingMunicipality.Ishøj: return 0;
        case EDwellingMunicipality.Tårnby: return 57;
        case EDwellingMunicipality.Vallensbæk: return 87;
        case EDwellingMunicipality.Furesø: return 83;
        case EDwellingMunicipality.Allerød: return 93;
        case EDwellingMunicipality.Fredensborg: return 62;
        case EDwellingMunicipality.Helsingør: return 24;
        case EDwellingMunicipality.Hillerød: return 64;
        case EDwellingMunicipality.Hørsholm: return 91;
        case EDwellingMunicipality.Rudersdal: return 95;
        case EDwellingMunicipality.Egedal: return 94;
        case EDwellingMunicipality.Frederikssund: return 56;
        case EDwellingMunicipality.Greve: return 82;
        case EDwellingMunicipality.Køge: return 53;
        case EDwellingMunicipality.FrederiksværkHundested: return 26;
        case EDwellingMunicipality.Roskilde: return 67;
        case EDwellingMunicipality.Solrød: return 92;
        case EDwellingMunicipality.Gribskov: return 69;
        case EDwellingMunicipality.Odsherred: return 15;
        case EDwellingMunicipality.Holbæk: return 50;
        case EDwellingMunicipality.Faxe: return 66;
        case EDwellingMunicipality.Kalundborg: return 28;
        case EDwellingMunicipality.Ringsted: return 39;
        case EDwellingMunicipality.Slagelse: return 22;
        case EDwellingMunicipality.Stevns: return 80;
        case EDwellingMunicipality.Sorø: return 55;
        case EDwellingMunicipality.Lejre: return 90;
        case EDwellingMunicipality.Lolland: return 2;
        case EDwellingMunicipality.Næstved: return 47;
        case EDwellingMunicipality.Guldborgsund: return 23;
        case EDwellingMunicipality.Vordingborg: return 38;
        case EDwellingMunicipality.Bornholm: return 12;
        case EDwellingMunicipality.Middelfart: return 71;
        //PSP
        //case EDwellingMunicipality.Christiansø: return 12;
        case EDwellingMunicipality.Assens: return 51;
        case EDwellingMunicipality.FaaborgMidtfyn: return 45;
        case EDwellingMunicipality.Kerteminde: return 46;
        case EDwellingMunicipality.Nyborg: return 31;
        case EDwellingMunicipality.Odense: return 10;
        case EDwellingMunicipality.Svendborg: return 25;
        case EDwellingMunicipality.Nordfyns: return 49;
        case EDwellingMunicipality.Langeland: return 3;
        case EDwellingMunicipality.Ærø: return 21;
        case EDwellingMunicipality.Haderslev: return 37;
        case EDwellingMunicipality.Billund: return 70;
        case EDwellingMunicipality.Sønderborg: return 40;
        case EDwellingMunicipality.Tønder: return 29;
        case EDwellingMunicipality.Esbjerg: return 16;
        case EDwellingMunicipality.Fanø: return 75;
        case EDwellingMunicipality.Varde: return 74;
        case EDwellingMunicipality.Vejen: return 79;
        case EDwellingMunicipality.Aabenraa: return 41;
        case EDwellingMunicipality.Fredericia: return 17;
        case EDwellingMunicipality.Horsens: return 36;
        case EDwellingMunicipality.Kolding: return 43;
        case EDwellingMunicipality.Vejle: return 60;
        case EDwellingMunicipality.Herning: return 52;
        case EDwellingMunicipality.Holstebro: return 59;
        case EDwellingMunicipality.Lemvig: return 63;
        case EDwellingMunicipality.Struer: return 65;
        case EDwellingMunicipality.Syddjurs: return 78;
        case EDwellingMunicipality.Norddjurs: return 34;
        case EDwellingMunicipality.Favrskov: return 88;
        case EDwellingMunicipality.Odder: return 81;
        case EDwellingMunicipality.Randers: return 33;
        case EDwellingMunicipality.Silkeborg: return 77;
        case EDwellingMunicipality.Samsø: return 7;
        case EDwellingMunicipality.Skanderborg: return 89;
        case EDwellingMunicipality.Aarhus: return 14;
        case EDwellingMunicipality.IkastBrande: return 72;
        case EDwellingMunicipality.RingkøbingSkjern: return 76;
        case EDwellingMunicipality.Hedensted: return 85;
        case EDwellingMunicipality.Morsø: return 27;
        case EDwellingMunicipality.Skive: return 58;
        case EDwellingMunicipality.Thisted: return 42;
        case EDwellingMunicipality.Viborg: return 73;
        case EDwellingMunicipality.Brønderslev: return 54;
        case EDwellingMunicipality.Frederikshavn: return 30;
        case EDwellingMunicipality.Vesthimmerlands: return 35;
        case EDwellingMunicipality.Læsø: return 6;
        case EDwellingMunicipality.Rebild: return 86;
        case EDwellingMunicipality.Mariagerfjord: return 48;
        case EDwellingMunicipality.Jammerbugt: return 68;
        case EDwellingMunicipality.Aalborg: return 18;
        case EDwellingMunicipality.Hjørring: return 44;

        default:
          throw new Exception(String.Format("Ukendt enumeration for kommunekode: {0}", municipality));
      }
    }

    /// <summary>
    /// Mapping fra enumeration anvendt i mikrosimulationsmodellen til kommunens positionsnummer bestemt af PC 3 anvendt i CTREEs.
    /// </summary>
    /// <param name="municipality">Enumeration for kommunen.</param>
    /// <returns>>Returnerer kommunens position iflg. PC 3, som anvedes i CTREEs. Første position har indeks 0.</returns>
    static public int MapMunicipality2indexPCA3(EDwellingMunicipality municipality)
    {
      switch (municipality)
      {
        case EDwellingMunicipality.København: return 97;
        case EDwellingMunicipality.Frederiksberg: return 93;
        case EDwellingMunicipality.Ballerup: return 10;
        case EDwellingMunicipality.Brøndby: return 29;
        case EDwellingMunicipality.Dragør: return 12;
        case EDwellingMunicipality.Gentofte: return 16;
        case EDwellingMunicipality.Gladsaxe: return 24;
        case EDwellingMunicipality.Glostrup: return 32;
        case EDwellingMunicipality.Herlev: return 27;
        case EDwellingMunicipality.Albertslund: return 31;
        case EDwellingMunicipality.Hvidovre: return 44;
        case EDwellingMunicipality.HøjeTaastrup: return 26;
        case EDwellingMunicipality.LyngbyTaarbæk: return 20;
        case EDwellingMunicipality.Rødovre: return 33;
        case EDwellingMunicipality.Ishøj: return 58;
        case EDwellingMunicipality.Tårnby: return 79;
        case EDwellingMunicipality.Vallensbæk: return 19;
        case EDwellingMunicipality.Furesø: return 7;
        case EDwellingMunicipality.Allerød: return 11;
        case EDwellingMunicipality.Fredensborg: return 18;
        case EDwellingMunicipality.Helsingør: return 34;
        case EDwellingMunicipality.Hillerød: return 22;
        case EDwellingMunicipality.Hørsholm: return 5;
        case EDwellingMunicipality.Rudersdal: return 9;
        case EDwellingMunicipality.Egedal: return 21;
        case EDwellingMunicipality.Frederikssund: return 15;
        case EDwellingMunicipality.Greve: return 65;
        case EDwellingMunicipality.Køge: return 61;
        case EDwellingMunicipality.FrederiksværkHundested: return 17;
        case EDwellingMunicipality.Roskilde: return 39;
        case EDwellingMunicipality.Solrød: return 40;
        case EDwellingMunicipality.Gribskov: return 8;
        case EDwellingMunicipality.Odsherred: return 6;
        case EDwellingMunicipality.Holbæk: return 82;
        case EDwellingMunicipality.Faxe: return 60;
        case EDwellingMunicipality.Kalundborg: return 43;
        case EDwellingMunicipality.Ringsted: return 70;
        case EDwellingMunicipality.Slagelse: return 84;
        case EDwellingMunicipality.Stevns: return 38;
        case EDwellingMunicipality.Sorø: return 56;
        case EDwellingMunicipality.Lejre: return 13;
        case EDwellingMunicipality.Lolland: return 14;
        case EDwellingMunicipality.Næstved: return 81;
        case EDwellingMunicipality.Guldborgsund: return 50;
        case EDwellingMunicipality.Vordingborg: return 57;
        case EDwellingMunicipality.Bornholm: return 28;
        case EDwellingMunicipality.Middelfart: return 49;
        //PSP
        //case EDwellingMunicipality.Christiansø: return 28;
        case EDwellingMunicipality.Assens: return 41;
        case EDwellingMunicipality.FaaborgMidtfyn: return 30;
        case EDwellingMunicipality.Kerteminde: return 35;
        case EDwellingMunicipality.Nyborg: return 52;
        case EDwellingMunicipality.Odense: return 96;
        case EDwellingMunicipality.Svendborg: return 51;
        case EDwellingMunicipality.Nordfyns: return 45;
        case EDwellingMunicipality.Langeland: return 2;
        case EDwellingMunicipality.Ærø: return 4;
        case EDwellingMunicipality.Haderslev: return 67;
        case EDwellingMunicipality.Billund: return 73;
        case EDwellingMunicipality.Sønderborg: return 74;
        case EDwellingMunicipality.Tønder: return 55;
        case EDwellingMunicipality.Esbjerg: return 91;
        case EDwellingMunicipality.Fanø: return 3;
        case EDwellingMunicipality.Varde: return 63;
        case EDwellingMunicipality.Vejen: return 77;
        case EDwellingMunicipality.Aabenraa: return 80;
        case EDwellingMunicipality.Fredericia: return 76;
        case EDwellingMunicipality.Horsens: return 89;
        case EDwellingMunicipality.Kolding: return 85;
        case EDwellingMunicipality.Vejle: return 86;
        case EDwellingMunicipality.Herning: return 92;
        case EDwellingMunicipality.Holstebro: return 87;
        case EDwellingMunicipality.Lemvig: return 25;
        case EDwellingMunicipality.Struer: return 68;
        case EDwellingMunicipality.Syddjurs: return 48;
        case EDwellingMunicipality.Norddjurs: return 46;
        case EDwellingMunicipality.Favrskov: return 72;
        case EDwellingMunicipality.Odder: return 36;
        case EDwellingMunicipality.Randers: return 83;
        case EDwellingMunicipality.Silkeborg: return 88;
        case EDwellingMunicipality.Samsø: return 1;
        case EDwellingMunicipality.Skanderborg: return 64;
        case EDwellingMunicipality.Aarhus: return 95;
        case EDwellingMunicipality.IkastBrande: return 78;
        case EDwellingMunicipality.RingkøbingSkjern: return 69;
        case EDwellingMunicipality.Hedensted: return 62;
        case EDwellingMunicipality.Morsø: return 23;
        case EDwellingMunicipality.Skive: return 71;
        case EDwellingMunicipality.Thisted: return 54;
        case EDwellingMunicipality.Viborg: return 90;
        case EDwellingMunicipality.Brønderslev: return 47;
        case EDwellingMunicipality.Frederikshavn: return 53;
        case EDwellingMunicipality.Vesthimmerlands: return 37;
        case EDwellingMunicipality.Læsø: return 0;
        case EDwellingMunicipality.Rebild: return 75;
        case EDwellingMunicipality.Mariagerfjord: return 59;
        case EDwellingMunicipality.Jammerbugt: return 42;
        case EDwellingMunicipality.Aalborg: return 94;
        case EDwellingMunicipality.Hjørring: return 66;

        default:
          throw new Exception(String.Format("Ukendt enumeration for kommunekode: {0}", municipality));
      }
    }


    /// <summary>
    /// Mapping fra enumeration anvendt i mikrosimulationsmodellen til kommunens positionsnummer bestemt af PC 4 anvendt i CTREEs.
    /// </summary>
    /// <param name="municipality">Enumeration for kommunen.</param>
    /// <returns>>Returnerer kommunens position iflg. PC 4, som anvedes i CTREEs. Første position har indeks 0.</returns>
    static public int MapMunicipality2indexPCA4(EDwellingMunicipality municipality)
    {
      switch (municipality)
      {
        case EDwellingMunicipality.København: return 0;
        case EDwellingMunicipality.Frederiksberg: return 2;
        case EDwellingMunicipality.Ballerup: return 94;
        case EDwellingMunicipality.Brøndby: return 80;
        case EDwellingMunicipality.Dragør: return 54;
        case EDwellingMunicipality.Gentofte: return 1;
        case EDwellingMunicipality.Gladsaxe: return 17;
        case EDwellingMunicipality.Glostrup: return 91;
        case EDwellingMunicipality.Herlev: return 75;
        case EDwellingMunicipality.Albertslund: return 95;
        case EDwellingMunicipality.Hvidovre: return 85;
        case EDwellingMunicipality.HøjeTaastrup: return 86;
        case EDwellingMunicipality.LyngbyTaarbæk: return 15;
        case EDwellingMunicipality.Rødovre: return 92;
        case EDwellingMunicipality.Ishøj: return 97;
        case EDwellingMunicipality.Tårnby: return 29;
        case EDwellingMunicipality.Vallensbæk: return 87;
        case EDwellingMunicipality.Furesø: return 96;
        case EDwellingMunicipality.Allerød: return 89;
        case EDwellingMunicipality.Fredensborg: return 79;
        case EDwellingMunicipality.Helsingør: return 84;
        case EDwellingMunicipality.Hillerød: return 81;
        case EDwellingMunicipality.Hørsholm: return 13;
        case EDwellingMunicipality.Rudersdal: return 12;
        case EDwellingMunicipality.Egedal: return 90;
        case EDwellingMunicipality.Frederikssund: return 83;
        case EDwellingMunicipality.Greve: return 68;
        case EDwellingMunicipality.Køge: return 88;
        case EDwellingMunicipality.FrederiksværkHundested: return 73;
        case EDwellingMunicipality.Roskilde: return 70;
        case EDwellingMunicipality.Solrød: return 69;
        case EDwellingMunicipality.Gribskov: return 39;
        case EDwellingMunicipality.Odsherred: return 44;
        case EDwellingMunicipality.Holbæk: return 67;
        case EDwellingMunicipality.Faxe: return 71;
        case EDwellingMunicipality.Kalundborg: return 51;
        case EDwellingMunicipality.Ringsted: return 93;
        case EDwellingMunicipality.Slagelse: return 45;
        case EDwellingMunicipality.Stevns: return 18;
        case EDwellingMunicipality.Sorø: return 77;
        case EDwellingMunicipality.Lejre: return 63;
        case EDwellingMunicipality.Lolland: return 48;
        case EDwellingMunicipality.Næstved: return 62;
        case EDwellingMunicipality.Guldborgsund: return 53;
        case EDwellingMunicipality.Vordingborg: return 58;
        case EDwellingMunicipality.Bornholm: return 16;
        case EDwellingMunicipality.Middelfart: return 43;
        //PSP
        //case EDwellingMunicipality.Christiansø: return 16;
        case EDwellingMunicipality.Assens: return 31;
        case EDwellingMunicipality.FaaborgMidtfyn: return 25;
        case EDwellingMunicipality.Kerteminde: return 74;
        case EDwellingMunicipality.Nyborg: return 52;
        case EDwellingMunicipality.Odense: return 23;
        case EDwellingMunicipality.Svendborg: return 41;
        case EDwellingMunicipality.Nordfyns: return 33;
        case EDwellingMunicipality.Langeland: return 19;
        case EDwellingMunicipality.Ærø: return 4;
        case EDwellingMunicipality.Haderslev: return 55;
        case EDwellingMunicipality.Billund: return 46;
        case EDwellingMunicipality.Sønderborg: return 24;
        case EDwellingMunicipality.Tønder: return 11;
        case EDwellingMunicipality.Esbjerg: return 40;
        case EDwellingMunicipality.Fanø: return 10;
        case EDwellingMunicipality.Varde: return 26;
        case EDwellingMunicipality.Vejen: return 42;
        case EDwellingMunicipality.Aabenraa: return 34;
        case EDwellingMunicipality.Fredericia: return 78;
        case EDwellingMunicipality.Horsens: return 30;
        case EDwellingMunicipality.Kolding: return 37;
        case EDwellingMunicipality.Vejle: return 21;
        case EDwellingMunicipality.Herning: return 20;
        case EDwellingMunicipality.Holstebro: return 61;
        case EDwellingMunicipality.Lemvig: return 7;
        case EDwellingMunicipality.Struer: return 38;
        case EDwellingMunicipality.Syddjurs: return 65;
        case EDwellingMunicipality.Norddjurs: return 32;
        case EDwellingMunicipality.Favrskov: return 72;
        case EDwellingMunicipality.Odder: return 22;
        case EDwellingMunicipality.Randers: return 56;
        case EDwellingMunicipality.Silkeborg: return 64;
        case EDwellingMunicipality.Samsø: return 3;
        case EDwellingMunicipality.Skanderborg: return 76;
        case EDwellingMunicipality.Aarhus: return 6;
        case EDwellingMunicipality.IkastBrande: return 66;
        case EDwellingMunicipality.RingkøbingSkjern: return 9;
        case EDwellingMunicipality.Hedensted: return 35;
        case EDwellingMunicipality.Morsø: return 8;
        case EDwellingMunicipality.Skive: return 36;
        case EDwellingMunicipality.Thisted: return 14;
        case EDwellingMunicipality.Viborg: return 57;
        case EDwellingMunicipality.Brønderslev: return 82;
        case EDwellingMunicipality.Frederikshavn: return 28;
        case EDwellingMunicipality.Vesthimmerlands: return 59;
        case EDwellingMunicipality.Læsø: return 5;
        case EDwellingMunicipality.Rebild: return 60;
        case EDwellingMunicipality.Mariagerfjord: return 50;
        case EDwellingMunicipality.Jammerbugt: return 47;
        case EDwellingMunicipality.Aalborg: return 27;
        case EDwellingMunicipality.Hjørring: return 49;

        default:
          throw new Exception(String.Format("Ukendt enumeration for kommunekode: {0}", municipality));
      }
    }

    /// <summary>
    /// Mapping fra enumeration anvendt i mikrosimulationsmodellen til kommunens positionsnummer bestemt af PC 5 anvendt i CTREEs.
    /// </summary>
    /// <param name="municipality">Enumeration for kommunen.</param>
    /// <returns>Returnerer kommunens position iflg. PC 5, som anvedes i CTREEs. Første position har indeks 0.</returns>
    static public int MapMunicipality2indexPCA5(EDwellingMunicipality municipality)
    {
      switch (municipality)
      {
        case EDwellingMunicipality.København: return 85;
        case EDwellingMunicipality.Frederiksberg: return 69;
        case EDwellingMunicipality.Ballerup: return 33;
        case EDwellingMunicipality.Brøndby: return 89;
        case EDwellingMunicipality.Dragør: return 47;
        case EDwellingMunicipality.Gentofte: return 29;
        case EDwellingMunicipality.Gladsaxe: return 92;
        case EDwellingMunicipality.Glostrup: return 51;
        case EDwellingMunicipality.Herlev: return 90;
        case EDwellingMunicipality.Albertslund: return 74;
        case EDwellingMunicipality.Hvidovre: return 44;
        case EDwellingMunicipality.HøjeTaastrup: return 93;
        case EDwellingMunicipality.LyngbyTaarbæk: return 15;
        case EDwellingMunicipality.Rødovre: return 22;
        case EDwellingMunicipality.Ishøj: return 97;
        case EDwellingMunicipality.Tårnby: return 73;
        case EDwellingMunicipality.Vallensbæk: return 96;
        case EDwellingMunicipality.Furesø: return 20;
        case EDwellingMunicipality.Allerød: return 25;
        case EDwellingMunicipality.Fredensborg: return 13;
        case EDwellingMunicipality.Helsingør: return 1;
        case EDwellingMunicipality.Hillerød: return 19;
        case EDwellingMunicipality.Hørsholm: return 5;
        case EDwellingMunicipality.Rudersdal: return 7;
        case EDwellingMunicipality.Egedal: return 70;
        case EDwellingMunicipality.Frederikssund: return 40;
        case EDwellingMunicipality.Greve: return 63;
        case EDwellingMunicipality.Køge: return 60;
        case EDwellingMunicipality.FrederiksværkHundested: return 55;
        case EDwellingMunicipality.Roskilde: return 18;
        case EDwellingMunicipality.Solrød: return 72;
        case EDwellingMunicipality.Gribskov: return 48;
        case EDwellingMunicipality.Odsherred: return 27;
        case EDwellingMunicipality.Holbæk: return 52;
        case EDwellingMunicipality.Faxe: return 75;
        case EDwellingMunicipality.Kalundborg: return 61;
        case EDwellingMunicipality.Ringsted: return 54;
        case EDwellingMunicipality.Slagelse: return 42;
        case EDwellingMunicipality.Stevns: return 91;
        case EDwellingMunicipality.Sorø: return 45;
        case EDwellingMunicipality.Lejre: return 66;
        case EDwellingMunicipality.Lolland: return 23;
        case EDwellingMunicipality.Næstved: return 41;
        case EDwellingMunicipality.Guldborgsund: return 8;
        case EDwellingMunicipality.Vordingborg: return 17;
        case EDwellingMunicipality.Bornholm: return 11;
        case EDwellingMunicipality.Middelfart: return 56;
        //PSP
        //case EDwellingMunicipality.Christiansø: return 11;
        case EDwellingMunicipality.Assens: return 87;
        case EDwellingMunicipality.FaaborgMidtfyn: return 64;
        case EDwellingMunicipality.Kerteminde: return 57;
        case EDwellingMunicipality.Nyborg: return 35;
        case EDwellingMunicipality.Odense: return 6;
        case EDwellingMunicipality.Svendborg: return 16;
        case EDwellingMunicipality.Nordfyns: return 83;
        case EDwellingMunicipality.Langeland: return 12;
        case EDwellingMunicipality.Ærø: return 28;
        case EDwellingMunicipality.Haderslev: return 46;
        case EDwellingMunicipality.Billund: return 81;
        case EDwellingMunicipality.Sønderborg: return 36;
        case EDwellingMunicipality.Tønder: return 78;
        case EDwellingMunicipality.Esbjerg: return 4;
        case EDwellingMunicipality.Fanø: return 0;
        case EDwellingMunicipality.Varde: return 58;
        case EDwellingMunicipality.Vejen: return 71;
        case EDwellingMunicipality.Aabenraa: return 31;
        case EDwellingMunicipality.Fredericia: return 14;
        case EDwellingMunicipality.Horsens: return 32;
        case EDwellingMunicipality.Kolding: return 34;
        case EDwellingMunicipality.Vejle: return 68;
        case EDwellingMunicipality.Herning: return 67;
        case EDwellingMunicipality.Holstebro: return 21;
        case EDwellingMunicipality.Lemvig: return 95;
        case EDwellingMunicipality.Struer: return 65;
        case EDwellingMunicipality.Syddjurs: return 37;
        case EDwellingMunicipality.Norddjurs: return 30;
        case EDwellingMunicipality.Favrskov: return 79;
        case EDwellingMunicipality.Odder: return 62;
        case EDwellingMunicipality.Randers: return 9;
        case EDwellingMunicipality.Silkeborg: return 39;
        case EDwellingMunicipality.Samsø: return 88;
        case EDwellingMunicipality.Skanderborg: return 80;
        case EDwellingMunicipality.Aarhus: return 3;
        case EDwellingMunicipality.IkastBrande: return 86;
        case EDwellingMunicipality.RingkøbingSkjern: return 77;
        case EDwellingMunicipality.Hedensted: return 94;
        case EDwellingMunicipality.Morsø: return 82;
        case EDwellingMunicipality.Skive: return 50;
        case EDwellingMunicipality.Thisted: return 53;
        case EDwellingMunicipality.Viborg: return 24;
        case EDwellingMunicipality.Brønderslev: return 38;
        case EDwellingMunicipality.Frederikshavn: return 10;
        case EDwellingMunicipality.Vesthimmerlands: return 43;
        case EDwellingMunicipality.Læsø: return 76;
        case EDwellingMunicipality.Rebild: return 84;
        case EDwellingMunicipality.Mariagerfjord: return 49;
        case EDwellingMunicipality.Jammerbugt: return 59;
        case EDwellingMunicipality.Aalborg: return 2;
        case EDwellingMunicipality.Hjørring: return 26;

        default:
          throw new Exception(String.Format("Ukendt enumeration for kommunekode: {0}", municipality));
      }
    }

#if !SBAM_TEST
    static public ESocioEconomicStatus LMSToSocioeconomic(ELabourMarketState lstate, Person p, Random random)
    {
        bool sd = Simulation.Instance.Economics._sygedagpengeProb[(int)p.Gender, (int)lstate] > random.NextDouble();
         
      if (sd)
        return ESocioEconomicStatus.TemporaryOut;
      else
      {
        switch (lstate)
        {
          case ELabourMarketState.Employed:
          case ELabourMarketState.UnemployedBenefit:
          case ELabourMarketState.UnemployedCash:
            return ESocioEconomicStatus.LaborForce;

          case ELabourMarketState.Cashbenefit:
            return ESocioEconomicStatus.CashBenefits;

          case ELabourMarketState.Disability:
            return ESocioEconomicStatus.EarlyRetirement;

          case ELabourMarketState.EarlyRetirement:
          case ELabourMarketState.Agepension:
            return ESocioEconomicStatus.Retired;

          case ELabourMarketState.NoIncome:
            return ESocioEconomicStatus.Other;

          case ELabourMarketState.Student:
            return ESocioEconomicStatus.Education;

          default:
            return ESocioEconomicStatus.Other;
        }
      }
    }
#endif
  }
}