using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DreamSMILE.MicroSim
{

    public enum ModelType
    { 
        Baseline,
        TestNyInitial2013,
        TestNyInitial2018,
        TestNyDemographics,
        TestMoveAndDwelling,
        TestNyeArbMarkEst,
        TestArbMarkAlignment
    }

    public enum EEducationModelType
    {
        Base2019,
        Base_includePE2019,
        Base2026,
        Base_includePE2026
    }

    public enum NaturalizationEstimates
    { 
        Projection2014,
        Projection2014Recalculated,
        Projection2019,
        Projection2019Recalculated,
        Projection2020,
        Projection2024
    }

    public enum AlignmentOption
    { 
        Proj2014NoAlignment,
        Proj2014Alignment,
        Proj2019NoAlignment,
        Proj2019Alignment,
        Proj2020NoBirthAlign,
        Proj2020NoAlignment,
        Proj2020Alignment,
        Proj2024Alignment
    }

    public enum DSTFlowFile
    { 
      DSTFile2014,
      DSTFile2019,
      DSTFile2020,
      DSTFile2024
    }

    /// <summary>
    /// Fastsætter datagrundlag for anvendte overgangssandsynligheder.
    /// </summary>
    public enum TransProbs
    {
        /// <summary>
        /// Alle overgangssandsynligheder (demografi, flyttebevægelser/boligvalg og arbejdsmarked) beregnes med udgangspunkt i forløb anvendt i basisfremskrivning 2013.
        /// </summary>
        Base2013,
        /// <summary>
        /// Overgangssandsynligheder for demografi som basisfremskrivning 2019. Overgangssandsynligheder for flyttebevægelser/boligvalg og arbejdsmarked som basisfremskrivning 2013.
        /// </summary>
        Demo2019,
        /// <summary>
        /// Overgangssandsynligheder for demografi og flyttebevægelser/boligvalg som basisfremskrivning 2019. Overgangssandsynligheder for arbejdsmarked som basisfremskrivning 2013.
        /// </summary>
        MoveDwelling2019,
        /// <summary>
        /// Alle overgangssandsynligheder (demografi, flyttebevægelser/boligvalg og arbejdsmarked) beregnes med udgangspunkt i forløb anvendt i basisfremskrivning 2019.
        /// </summary>
        Base2019,
        /// <summary>
        /// Overgangssandsynligheder for demografi som basisfremskrivning 2019. Overgangssandsynligheder for flyttebevægelser/boligvalg og arbejdsmarked som basisfremskrivning 2013.
        /// </summary>
        Demo2026,
        /// <summary>
        /// Overgangssandsynligheder for demografi og flyttebevægelser/boligvalg som basisfremskrivning 2019. Overgangssandsynligheder for arbejdsmarked som basisfremskrivning 2013.
        /// </summary>
        MoveDwelling2026,
        /// <summary>
        /// Alle overgangssandsynligheder (demografi, flyttebevægelser/boligvalg og arbejdsmarked) beregnes med udgangspunkt i forløb anvendt i basisfremskrivning 2019.
        /// </summary>
        Base2026
    }

    public enum LMSModelVersion
    {
        CR2013,
        CR2026
    }

    /// <summary>
    /// Definerer start population
    /// </summary>
    public enum InitPop
    {
        Baseline2013,
        Update2013,
        Baseline2018,
        Baseline2024
    }

    public enum Experiment
    {
        NoExperiment,
        LMTransitionExperiment001,
        LMTransitionExperiment002
    }


    public enum ESocialYdelseRegulering : byte
    {
        /// <summary>
        /// Folkepensionens Grund- og Tillægsydelse
        /// </summary>
        FolkepensionsYdelse,            //0 

        FolkePensionSupplerendeYdelse,  //1

        ArbejdsløshedsDagpenge,         //2

        SygeDagpenge,                   //3

        KontanthjælpsYdelse,            //4

        FørtidspensionsYdelser,         //5

        RevalideringsYdelse,            //6

        StatensUddannelsesStøtte,       //7

        BørneOgUngeYdelse,              //8

        BoligYdelse,                    //9

        Øvrige                          //10
    }


    /// <summary>
    /// Enlig, hvis personen bor uden andre voksne. 
    /// </summary>
    public enum CivilstandPensionist : byte
    {
        /// <summary>
        /// Pensionist, der lever i ægteskabslignende forhold, hvor både personens egen og samlevers pension er tilkendt efter 1999.
        /// </summary>
        Samlevende,
        /// <summary>
        /// Pensionist i ægteskabslignende forhold etableret før 1999 med pensions-start før 1999
        /// </summary>
        Enlig,
        /// <summary>
        /// Pensionist, der lever med slægtning el. logerende (ikke-ægteskabslignende forhold)
        /// </summary>
        ReeltEnlig,
        /// <summary>
        /// Pensionist, der er gift med ikke pensionist
        /// </summary>
        Gift,
        /// <summary>
        /// Pesionist gift med anden pensionist
        /// </summary>
        Samgift
    };

    public enum Model : byte
    {
        Municipality,
        RegionalModel
    }

    public enum ModelPopulation : byte
    {
        GenerateNew,
        CalculateOnExisting
    }

    /// <summary>
    /// Køn
    /// </summary>
    public enum EGender : byte  { Male, Female }
    /// <summary>
    /// Vestlig, ikke-vestlig eller ukendt
    /// </summary>
    public enum ECountry : byte { Western, NonWestern, Unknown }
    /// <summary>
    /// Oprindelse
    /// </summary>
    public enum EPopulationStatus : byte { Dane, Immigrant, Descendent, Unknown }
    /// <summary>
    /// Statsborgerskab
    /// </summary>
    public enum ECitizenship : byte { Danish, NotDanish, Unknown }
    /// <summary>
    /// Persontype (barn/voksen)
    /// </summary>
    public enum EPersonStatus : byte { Adult, Child }
    /// <summary>
    /// Parsonstadie (nyfødt, død el. immigrant)
    /// </summary>
    public enum EPersonState  { New, Dead, Immigrant }
    /// <summary>
    /// Oprindelse. Den oprindelige opdeling på mere eller mindre udviklede lande
    /// </summary>
    public enum EOrigin12  //m=mindre
    {
        omuis,
        omus,
        ouis,
        ous,
        imuis,
        imus,
        iuis,
        ius,
        emuis,
        emus,
        euis,
        eus,
        Unknown
    }
    /// <summary>
    /// Oprindelse
    /// </summary>
    public enum EOrigin5
    {
        DanskOprindelse,
        IndvandrereIkkeVestligeLande,
        IndvandrereVestligeLande,
        EfterkommereIkkeVestligeLande,
        EfterkommereVestligeLande
    }
    /// <summary>
    /// Oprindelse. Nye opdeling på vestlige og ikke-vestlige lande
    /// </summary>
    public enum EOrigin9 
    {
        das,
        ixis,
        ixs,
        iwis,
        iws,
        dxis,
        dxs,
        dwis,
        dws
    }
    /// <summary>
    /// Familie oprindelse
    /// </summary>
    public enum EFamilyOrigin 
    {
        Ukendt = -1,
        DanskOprindelse,
        DanskOprindelseBlandet,           // DK-DW, DK-DX, DK-IW, DK-IX
        IndvandrereIkkeVestlige,
        IndvandrereVestlige,
        EfterkommereIkkeVestlige,
        EfterkommereVestlige,
        IndvandrereVestligeBlandet,       // IW-IX
        EfterkommereIkkeVestligeBlandet,  // DX-IW, DX-IX
        EfterkommereVestligeBlandet       // DW-DX, DW-IW, DW-IX  
    }
    /// <summary>
    /// Oprindelsesgruppe
    /// </summary>
    public enum EGroup5TT 
    {
        RestOfPopulation,                     //0
        ImmigrantsFromLessDevelopedCountry,   //1
        ImmigrantsFromMoreDevelopedCountry,   //2
        DecendantsFromLessDevelopedCountry,   //3
        DecendantsFromMoreDevelopedCountry    //4
    }
    /// <summary>
    /// Region (landsdel)
    /// </summary>
    public enum ERegion5 
    {
        RegionNordjylland,
        RegionMidtjylland,
        RegionSyddanmark,
        RegionHovedstaden,
        RegionSjaelland,
        Unknown
    }
    /// <summary>
    /// Uddannelsesgrupperinger
    /// </summary>
    public enum EEducationDS8 
    {
        Ukendt,                           //0
        Grundskole,                       //1
        TiendeKlasse,                     //2
        Almengymnasial,                   //3
        Erhvervsgymnasial,                //4
        ErhvervsfagligeGrundforlob,       //5
        ErhvervsfagligeHovedforlob,       //6
        KortVideregående,                 //7
        ProffessionsBachelor,             //8
        MellemlangVideregående,           //9
        UniversitetsBachelor,             //10
        UdelteKandidat,                   //11
        DelteKandidat,                    //12
        Phd,                              //13
        Master,                           //14
        IkkeUnderUddannelse               //15
    }
    /// <summary>
    /// Uddannelsesgrupperinger - 6 grupper!
    /// </summary>
    public enum EEducation6 
    {
        Ukendt = -1,
        Grundskole,
        Gymnasial,
        Erhvervsfaglig,
        KVU,
        MVU,
        LVU
    }
    /// <summary>
    /// Uddannelsesgrupperinger - to grupper færre end i standard-grupperingen, erhvervsfaglige grund- og hovedforløb slået sammen og delte og udelte kandidatuddannelser slået sammen
    /// </summary>
    public enum EEducation12 
    {
        Ukendt,                           //0
        Grundskole,                       //1
        TiendeKlasse,                     //2
        Almengymnasial,                   //3
        Erhvervsgymnasial,                //4
        Erhvervsfaglige,                  //5
        KortVideregående,                 //6
        ProffessionsBachelor,             //7
        MellemlangVideregående,           //8
        UniversitetsBachelor,             //9
        Kandidat,                         //10
        Phd,                              //11
        Master,                           //12
        IkkeUnderUddannelse               //13
    }
    /// <summary>
    /// Familiens uddannelse
    /// </summary>
    public enum EFamilyEducation6 
    {
        Ukendt = -1,
        Grundskole,
        Gymnasial,
        Erhvervsfaglig,
        ErhvervsfagligBlandet,    //GS                          1
        KVU,
        KVUBlandet,               //GS, GY                      1    2
        MVU,
        MVUBlandet,               //GS, GY, EF                  1    2   3
        LVU,
        LVUBlandet                //GS, GY, EF, KV              1    2   3   4
    }
    /// <summary>
    /// Familiens uddannelsesniveau
    /// </summary>
    public enum EFamilyEducation6_NEW 
    {
        Ukendt = -1,
        Grundskole,
        Gymnasial,
        Erhvervsfaglig,
        KVU,
        MVU,
        LVU,
        ErhvervsfagligBlandet,    //GS                          1
        KVUBlandet,               //GS, GY                      1    2
        MVUBlandet,               //GS, GY, EF                  1    2   3
        LVUBlandet                //GS, GY, EF, KV              1    2   3   4
    }
    /// <summary>
    /// Uddannelsesgrupper - 7 stk!
    /// </summary>
    public enum EEducation5 
    {
        Ukendt,
        Ufaglært,
        Faglært,
        KVU,
        MVU,
        LVU,
        IkkeUnderUddannelse
    }
    /// <summary>
    /// Arbejdsmarkedstilknytning
    /// </summary>
    public enum ESocioEconomicStatus
    {
        LaborForce,         // Beskæftigede + ledige 
        Education,          // Uddannelsessøgende
        TemporaryOut,       // Orlov + sygedagpenge
        CashBenefits,       // Aktivering + omskoling + kontanthjælp
        EarlyRetirement,
        Retired,            // Efterløn + pensionister
        Other               // Øvrige udenfor arbejdsstyrken   
    }

    /// <summary>
    /// Tilbagetrækningsordninger
    /// <\summary>
    /// 
    public enum ERetirementScheme 
    {
        EarlyRetirement,    //person på efterlønsordning
        Disability,         //person på førtidspension
        Agepension,          //person på folke- eller tjenestemandspension
        NotRetired         //person stadigvæk arbejdsmarkedsaktiv
    }

    ///<summary>
    /// Arbejdsmarkedstildeltagelsestilstande: en person der regnes for knyttet til arbejdsmarkedet, men som eksempelvis kan være midlertidigt ude
    /// </summary>
    /// 
    public enum ELabourMarketState
    {
        Unknown = -1,
        /// <summary>
        /// beskæftigede
        /// </summary>
        Employed,
        /// <summary>
        /// ledige dagpengemodtagere
        /// </summary>
        UnemployedBenefit,
        /// <summary>
        /// ledige kontanthjælpsmodtagere
        /// </summary>
        UnemployedCash,
        /// <summary>
        /// uden ydelser
        /// </summary>
        NoIncome,
        /// <summary>
        /// kontanthjælpsmodtagere og aktiverede
        /// </summary>
        Cashbenefit,
        /// <summary>
        /// førtidspension
        /// </summary>
        Disability,
        /// <summary>
        /// Efterløn
        /// </summary>
        EarlyRetirement,
        /// <summary>
        /// Folkepensionister
        /// </summary>
        Agepension,
        /// <summary>
        /// Studerende
        /// </summary>
        Student
    }
    /// <summary>
    /// Arbejdsmarkedstildeltagelsestilstande på 8-gruppering (her er ledige på dagpenge og kontanthjælp slået sammen til én gruppe).
    /// </summary>
    public enum ELabourMarketState8 
    {
        Unknown = -1,
        Employed,       // beskæftigede
        Unemployed,     // ledige dagpenge- og kontanthjælpsmodtagere
        NoIncome,       // uden ydelser
        Cashbenefit,    // kontanthjælpsmodtagere og aktiverede
        Disability,       // ikke arbejdsmarkedsaktiv/permanent uden for arbejdsmarkedet
        EarlyRetirement,
        Agepension,
        Student
    }

    public enum ELabourMarketState5 : byte
    {
        Employed,
        Unemployed,
        NoIncome,
        CashBenefit,
        NotActive
    }

    ///<summary>
    /// arbejdsmarkedstype angiver hvilken type af model der anvendes for varighedsberegning
    /// </summary>
    /// typer:
    /// nyuddannede, er personer der netop har afsluttet en uddannelse og bestået; personen ophører med denne status når personen skifter status(????!!!!!) og overgår til ordinær
    /// afbrydere, er personer der netop har afbrudt en uddannelse uden beståelse; personen ophører med denne status når personen skifter status(????!!!!!) og overgår til ordinær
    /// (ny)indvandrer, er personer der netop er indvandret til landet; personen ophører med denne status når personen skifter status(????!!!!!) og overgår til ordinær
    /// ordinærer, personer der ikke er nogle af de andre
    /// 
    public enum ELabourMarketType : byte
    {
        Ordinary,
        NewEducated,
        EduDropOut,
        NewImmigrant
    }
    /// <summary>
    /// Type of employment: Selvstændige, medhjælpende ægtefælle, lønmodtager og ikke-beskæftigede (incl. non-employeed as a separate category)
    /// </summary>
    public enum EEmploymentType 
    {
        Ukendt,
        Entrepreneur,
        WorkingSpouse,
        Employeed,
        NotEmployeed
    }


    public enum ERASStatus
    {
        Beskæftigelse, //0
        BeskæftigelseUnderUddannelseMedSU, //1
        BeskæftigelseUnderUddannelseUdenSU, //2
        BeskæftigetSygedagpenge, //3
        BeskæftigetFørtidspension, //4
        BeskæftigetEfterløn, //5
        BeskæftigetFolkepension, //6
        BeskæftigetLøntilskudForsikrede, //7
        BeskæftigetLøntilskudIkkeForsikrede, //8
        Kat9, //9
        BeskæftigetFleksjob, //10
        BeskæftigetSkånejob, //11
        BeskæftigetRevalidenter,//12
        BeskæftigetVoksenlærlinge,//13
        DelvistLedige,//14
        Kat15,//15
        LedigeForsikrede,//16
        LedigeIkkeForsikrede,//17
        StuderendeMedSU,//18
        StuderendeUdenSU,//19
        Kat20,//20
        Barsel,//21
        Sygedagpenge,//22
        AktiveredeForsikrede,//23
        AktiveredeIkkeForsikret,//24
        Revalidering,//25
        Ledighedsydelse,//26
        Kontanthjælp,//27
        Integrationsydelse,//28
        Førtidspension,//29
        Kat30,//30
        Fleksydelse,//31
        Efterløn,//32
        Folkepension,//33
        Tjenestemandspension,//34
        PermanentUdenYdelse//35

    }
    /// <summary>
    /// Det er vigtigt at variabel-navne er identiske med inputvariable
    /// </summary>
    public enum EInputPopData
    {
        familie_plads,
        gender,
        age,
        durationPartnership,
        populationStatus,
        country,
        citizenship,
        timeOfResidence,
        regionAtAge15,
        municipalityAtAge15,
        municipalityAtAge15NA,
        couple,
        numberOfChildren,
        ageYoungestChild,
        ongoingEducation,
        highestEducation,
        durationEducation,
        labourStatus_lag,
        labourStatus,
        nFamilies,
        BBR,
        pnr,
        NewLabourStatus,
        currDuration,
        indv,
        nyudd,
        uddaf,
        foerstejobdummy,
        erfaring,
        karakter,
        misskar,
        skala13,
        udhfmorNA,
        udhfmor2,
        udhfmor3,
        udhfmor4,
        udhfmor5,
        udhfmor6,
        udhffarNA,
        udhffar2,
        udhffar3,
        udhffar4,
        udhffar5,
        udhffar6,
        dtype,
        duse,
        dsize,
        dyear,
        dregion,
        dmunicipality,
        ddensity,
        immigrationID,
        ddwellingUse,
        ddwellingType,
        year,
        personligIndkomst,
        origin5,
        isMarried,
        isImmigrant,
        firstAdult,
        monthEarlyRetirementThisYear,
        monthsDisabilityThisYear,
        monthsAgePensionThisYear,
        monthsUnemploymentBenefitsThisYear,
        monthsStudentThisYear,
        monthsCashBenefitsThisYear,
        currentEducationEnrollmentState,
        municipality,
        isDropOut,
        isGraduated,
        experience,
        isFirstJob,
        isStudentEmployed,
        isNewborn,
        dwellingRegion,
        lmStatusPrimo
    }

}
