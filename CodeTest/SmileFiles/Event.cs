using System;

namespace DreamSMILE.MicroSim
{
  [Serializable]
  public static class Event
  {
    #region structs
    /// <summary>
    /// Samling af konstanter, der styrer simulationens overordnede hændelser
    /// </summary>
    public struct System
    {
      /// <summary>
      /// Starter simulation
      /// </summary>
      public const int Start = 0;
      /// <summary>
      /// Afslutter simulationen
      /// </summary>
      public const int Stop = 1;
      /// <summary>
      /// Et år i simulationen påbegyndes
      /// </summary>
      public const int YearStart = 2;
      /// <summary>
      /// Et år i leves i simulationen
      /// </summary>
      //public const int Update = 3;
      /// <summary>
      /// Et år i simulationen afsluttes
      /// </summary>
      public const int YearStop = 4;

      //public const int Update1 = 5;      // Dette markerer den første update
      //               ......          Her i mellem kan evt. tilføjes Update2, Update3 osv.
      public const int UpdateLast = 6;  // Dette markerer den sidste update

      public const int YearStopUpdate = 7; //dette markerer den sidste opdatering før YearStop

      public const int AddNewHouseholds = 8;

    }

    public struct Alignment
    {
      public const int Birth = 10101;
      public const int Death = 10102;
      public const int Emigration = 10104;
      public const int LeaveHome2Emigration = 10111;
      public const int Divorce2Emigration = 10113;

      public const int EducationFinish = 10201;
      public const int EducationOutside = 10202;
      public const int EducationFinishLead = 10203;
      public const int EducationInsideLead = 10204;

      public const int Branche = 10301;

      public const int LMSFolkepension = 10401;
      public const int LMSFolkeEmploy = 104015;
      public const int LMSDisabilityPension = 10402;
      public const int LMSPreAgePension = 10403;
      public const int LMSBruttoArbejdsstyrke = 10404;
      public const int LMSBruttoArbejdsstyrkeTemporary = 10405;
      public const int LMSUnemploymentType = 104055;
      public const int LMSStudEmploy = 10406;

      public const int ARBParticipation = 105001;
      public const int PRIParticipation = 105002;

      public const int AldOpsparing = 106001;
      public const int RateOpsparing = 106002;
      public const int LivPension = 106003;
    }

    /// <summary>
    /// Samling af konstanter, der beskriver demografiske hændelser
    /// </summary>
    public struct Demographics
    {
      public const int Birth = 101;

      public const int Death = 102;
      public const int Immigration = 103;
      public const int Emmigration = 104;
      public const int Naturalization = 105;
      /// <summary>
      /// Flytter bolig
      /// </summary>
      public const int Move = 106;

      /// <summary>
      /// Flytter hjemmefra
      /// </summary>
      public const int LeaveHome = 107;
      /// <summar>
      /// Flytter sammen
      /// </summary>
      public const int ShackUp = 108;
      /// <summary>
      /// Skilsmisse
      /// </summary>
      public const int Divorce = 109;
      /// <summary>
      /// Pardannelse
      /// </summary>
      public const int Matching = 110;

      public const int LeaveHome2Emigration = 111;
      public const int LeaveHome2Matching = 112;
      public const int Divorce2Emigration = 113;
      public const int Divorce2Matching = 114;

      public const int SocioEconomics = 130;

      public const int Education = 131;
      public const int EducationStepFirst = 132;
      public const int EducationStepTwo = 133;

      public const int EducationOutside = 137;
      public const int EducationFinishLead = 138;

      public const int ReportTest = 200;
      public const int MoveReport = 201;
    }

    /// <summary>
    /// hændelser der beskriver den økonomiske del af modellen
    /// <summary\>
    /// 
    public struct Economics
    {

      /// <summary>
      /// pre-age pension scheme membership assignment event
      /// </summary>
      public const int PreAgePenMembershipAssignment = 301;

      /// <summary>
      /// pre-age pension scheme membership drop out event
      /// </summary>
      /// 
      public const int PreAgePenMembershipDropOut = 302;

      /// <summary>
      /// retirement event
      /// </summary>
      /// 
      public const int Retirement = 303;

      /// <summary>
      /// re-retirement event: returning as participating member of the labour force
      /// </summary>
      /// 
      public const int ReRetirement = 304;

      /// <summary>
      /// event if employed of age above the agepensionage should retire
      /// </summary>
      public const int AgePension = 305;
      
        /// <summary>
      /// event if ressource person turns into disability pension as 40-year old
      /// </summary>
      public const int RessourceForlob = 306;
      /// <summary>
      /// labour market activity, change of labour market status
      /// </summary>
      
        /// 
      public const int LabourMarketStatus = 310;

      public const int SicknessBenefit = 311;

      public const int LabourMarketDuration = 320;

      public const int LabourMarketFlow = 330;

      public const int LabourMarketFlowDirect = 331;

      public const int StudyEmployment = 332;

      public const int UpstartIncomeEntrepreneur = 333;

      public const int EarnedIncomeDist = 340;

      public const int WageIncomeDist = 341;

      public const int ExperEntrepreneur = 342;

      public const int ExperEmployment = 343;
      
      public const int IncomeDist = 344;

      public const int WealthDist = 345;

      public const int Branche = 350;

      public const int LMSFolkepension = 360;

      public const int LMSDisabilityPension = 361;

      public const int LMSPreAgePension = 362;

      public const int LMSBruttoArbejdsstyrke = 363;

      public const int LMSUnEmpType = 364;

      public const int LMSFolkepensionEmploy = 365;

      public const int LMSStudentEmployment = 366;

      public const int LMSFolkepensionNon = 367;

            public const int LMSDisabilityPensionNon = 368;
            public const int LMSPreAgePensionNon = 369;
            public const int LMSFolkepensionEmpNon = 370;

            public const int RegisterWageCharacteristics = 401;

      public const int RegisterLonSum0 = 501;

      public const int RegisterPensIndbe = 502;

      public const int UpdatePensionWealth = 503;

      public const int RegisterPensIndbeExPost = 504;

      public const int RegisterLonSum1 = 505;

      public const int RegisterLonSumExPost = 507;
      
        public const int RegisterPensIndbeObligatoriskPension = 508;

        public const int AlignAldersopsparing = 509;

        public const int AlignRatePension = 510;

        public const int AlignLivPension = 511;

        public const int AlignARBParticipation = 601;

        public const int AlignPRIParticipation = 602;

        
    }

    public struct Transfers
    {
        /// <summary>
        /// folkepensionens grundbeløb
        /// </summary>
        public const int FolkepensionGrundbelob = 410;
        public const int FolkepensionGrundbelobAntal = 412;

        /// <summary>
        /// folkepensionens indkomstregulerede tillægsbeløb (samt diverse andre ydelser såsom varmeydelser mv.)
        /// </summary>
        public const int FolkepensionTillaeg = 411;

        /// <summary>
        /// udbetalinger fra alle arbejdsmarkdspensioner
        /// </summary>
        public const int ArbejdsmarkedsPension = 420;

        public const int ArbejdmarkedsPensionLivrente = 421;

        public const int ArbejdmarkedsPensionLivrenteF = 422;

        public const int ArbejdmarkedsPensionRate = 423;

        /// <summary>
        /// udbetaling af sociale ydelser
        /// </summary>
        public const int SocialeYdelser = 430;

        public const int SocialYdelseBoerneYdelse = 431;

        public const int SocialYdelseBoligYdelse = 432;

        /// <summary>
        /// indkomsterstattende ydelser der modtages fra det offentlige
        /// </summary>
        public const int AndreIndkomstYdelser = 440;

        public const int AndreIndkomstYdelserDagpenge = 441;

        public const int AndreIndkomstYdelserSygedagepenge = 442;

        public const int AndreIndkomstYdelserSU = 443;

        public const int AndreIndkomstYdelserKontanthjaelp = 444;

        public const int AntalKontanthjaelp = 451;

        public const int AndreIndkomstYdelserFortidspension = 445;

        public const int AndreIndkomstYdelserEfterlon = 446;

        /// <summary>
        /// skattebetaling inklusiv fradrag(?)
        /// </summary>
        public const int SkatteBetaling = 450;
    }

      public struct Pension
    {
        public const int AMPPensionParticipation = 610;

        public const int PRIPensionParticipation = 611;

        public const int AMPPensionAmount = 620;

        public const int PRIPensionAmount = 621;

        public const int AMPPensionAmountDyn = 622;

        public const int EJDParticipation = 630;

        public const int EJDShare = 631;

        public const int EJDWealthAmount = 632;

        public const int PensionWealthAMP = 640;

        public const int PensionWealthPRI = 641;

        public const int PensionWealthATP = 642;

        public const int PensionWealthLDP = 643;
    }

    #endregion structs
  }
}
