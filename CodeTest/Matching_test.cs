
// Matching_test.cs: trial version of Matching.cs with five changes to the SBAM implementation.
// Matching.cs itself is unchanged. Each change is switched on and off with a field of the class, and with
// all of them switched off Matching_test forms exactly the same couples as Matching (the test program
// checks this). The changes are marked //TEST0 to //TEST4 in the code:
//
//   TEST0  ResetToData       _ras.Reset() at the end of matching() restores the historical data. In Matching.cs
//                            Reset() sets all cells to 1, so that the historical couple table is only used in
//                            the first call of matching() (see RASMatrix_test.cs).
//   TEST1  RasToConvergence  The RAS adjustment runs until the row and column sums match (RASMatrix_test.cs),
//                            instead of stopping in the second pass.
//   TEST2  TargetAsCouples   The table is adjusted so that it sums to the number of couples to be formed: each
//                            person in the pool counts 1/MatchingScaleUp, and men and women are scaled to the
//                            same sum. MatchingScaleUp is thereby only a reserve of persons.
//   TEST3  PairingMethod     The couples are formed in one pass through the cells: all whole couples in a cell
//                            at once, and the fraction is rounded up or down without bias. MaxRounds > 1 gives
//                            mop-up rounds on the persons who are left, if the target was not reached.
//   TEST4  RasMethod         The RAS adjustment is computed faster (RASMatrix_test.cs): on factors in arrays (4a),
//          RasActiveOnly     with one factor per type (4b) and only on types that have persons in the pool (4c).
//                            The adjustment is the same; only the way it is computed has changed.
//
// The comments and names that belong to the changes are in English. The code taken over from Matching.cs,
// including its Danish comments and names, is unchanged.
//
// SBAM_TEST: When the constant is defined, the lines that require the rest of the SMILE model are switched
// off (the R namespace, report files, choice of dwelling, moving and household formation in parring()), so
// that the SBAM algorithm can be tested in isolation in the CodeTest project. Put "//" in front of the line
// to get the original code back. All disconnected lines are between "#if !SBAM_TEST" and "#endif".
#define SBAM_TEST

using DreamSMILE.SimulationNS;
using DreamSMILE.IO;
#if !SBAM_TEST
using DreamSMILE.R;
#endif
using DreamSMILE.Numerical;
using DreamSMILE.DemoSim;

namespace DreamSMILE.MicroSim
{
    //TEST3: ways of forming couples from the adjusted table
    public enum EPairingMethod
    {
        /// <summary>As in Matching.cs: up to 1000 passes with at most one whole couple per cell at a time.</summary>
        Original,
        /// <summary>One pass. The fraction in a cell is rounded up with probability equal to the fraction, independently of the other cells.</summary>
        Independent,
        /// <summary>One pass. Systematic selection across the cells in type order (column type, row type).</summary>
        SystematicTypeOrder,
        /// <summary>One pass. Systematic selection across the cells sorted by the characteristics of the couple (see SortKey).</summary>
        SystematicHierarchical
    }

    class Matching_test
    {
        protected StreamWriter _testFile = new StreamWriter(MSSettings.OutputDir + "\\testMatching_test_" + MSSettings.ProjectionID + ".txt");
        protected Simulation _sim;
        protected RASMatrix_test _ras;
        protected DwellingCharacteristics dwelChar;

        #region TEST: settings and results
        //TEST0: true = Reset() restores the historical data. false = Reset() sets all cells to 1 as in RASMatrix.cs
        public bool ResetToData = true;

        //TEST1: true = the RAS adjustment runs to convergence. false = stops in the second pass as in RASMatrix.cs
        public bool RasToConvergence = true;
        public int RasMaxPasses = 200;
        public double RasTolerance = 1e-6;   // total deviation in row and column sums relative to the total target

        //TEST2: true = the table sums to the number of couples to be formed. false = to the whole pool as in Matching.cs
        public bool TargetAsCouples = true;

        //TEST3: the way the couples are formed, and the number of rounds (only for the one-pass methods).
        // Round 2 onwards repeats adjustment and pairing on the persons who are left, if the target has not been reached.
        public EPairingMethod PairingMethod = EPairingMethod.SystematicHierarchical;
        public int MaxRounds = 1;

        //TEST4: the way the RAS adjustment is computed. Cells = as in RASMatrix.cs. Factors = 4a. Symmetric = 4a+4b.
        // RasActiveOnly = 4c: only types with persons in the pool are computed on (not with Cells).
        public ERasMethod RasMethod = ERasMethod.Cells;
        public bool RasActiveOnly = false;

        // Results from the latest call of matching()
        public int LastTargetCouples;            // the number of couples that were to be formed
        public int LastCouples;                  // the number of couples that were formed
        public int LastRounds;                   // number of rounds of adjustment and pairing
        public int LastRasPasses;                // number of passes in the RAS adjustment (first round)
        public double LastRasError;              // total deviation relative to the total target (first round)
        public bool LastRasConverged;            // whether the RAS adjustment reached RasTolerance (first round)
        public double LastCouplesInTable;        // expected number of couples in the adjusted table (first round)
        public int LastPersonsWithoutData;       // persons in the pool of types that do not occur in the historical data
        public int LastOmitted;                  // couples the table asked for but that could not be formed because a type was used up
        public double LastMsRas;                 // time in the RAS adjustment in milliseconds (all rounds)
        public double LastMsPairing;             // time in the pairing in milliseconds (all rounds, without RAS adjustment)

        bool[] _hasData;                         // _hasData[j] = type j occurs in the historical data
        #endregion

        public Matching_test()
        {

        }

        public Matching_test(DwellingCharacteristics _dwelChar)
        {
            _sim = Simulation.Instance;
            dwelChar = _dwelChar;
            Console.Write("\r-> Matching...                                     ");
            initializeMatching();
        }

        protected void initializeMatching()
        {

            // Arrays til afrapportering
            //NER: Man kan undre sig over at der sker afrapportering allerede inden simuleringen er igang...det burde være overflødigt
            //PSP: Svar til NER: Det er til testformål (man kunne evt lave en test-bool)
            int[,] matchAge0 = new int[MSSettings.MAX_AGE, MSSettings.MAX_AGE];
            int[,] matchEducation0 = new int[5, 5];
            int[,] matchOrigin0 = new int[MSSettings.ORIGIN5, MSSettings.ORIGIN5];
            int[,] matchRegion0 = new int[MSSettings.REGION, MSSettings.REGION]; ;

            //PSP
            int[,] matchMunicipality0 = null;
            if (MSSettings.UseMunicipalityDependentSBAM)
                matchMunicipality0 = new int[MSSettings.MUNICIPALITY, MSSettings.MUNICIPALITY];

            bool wCons = false;  // Skriv i console

            List<RASMatrixCell> lRas = new List<RASMatrixCell>();

            #region indlæs data
            if (wCons) Console.WriteLine("\rMatch> Indlæser data...");
            _testFile.WriteLine("Match> Indlæser data...");

            TabFileReader file = new TabFileReader(MSSettings.InputDataMatching);
            //PSP
            //file.SetGroup("familie_id_lead");
            file.SetGroup("familie_id_ultimo");

            int n = 0;
            int[] j = new int[2];

            int AgeMax = MSSettings.MatchingMaxAge;
            int AgeMin = MSSettings.MatchingMinAge;

            // HACK: Hard-kodede konstanter
            int I_Age = 1 + AgeMax - AgeMin;
            int I_Gender = 2;
            int I_Edu = 5;
            int I_Landsdel = 11;
            int I_Oprindelse = 5;

            //PSP
            if (MSSettings.UseMunicipalityDependentSBAM)
                I_Landsdel = MSSettings.MUNICIPALITY;

            while (file.ReadGroup())
            {
                if (file.GroupSize == 2)
                {
                    int[] a = new int[2];  //Alder
                    int[] g = new int[2];  //Køn
                    int[] o = new int[2];  //Oprindelse
                    int[] e = new int[2];  //Udd
                    int[] ld = new int[2];  //Landsdel
                    //PSP
                    int[] ld0 = new int[2];  //Landsdel
                    int[] t = new int[2];  //År

                    file.Get("alder", a);
                    file.Get("koen", g);
                    file.Get("hf", e);

                    //PSP
                    //file.Get("landsdel", ld);
                    if (MSSettings.UseMunicipalityDependentSBAM)
                        file.Get("kommune_primo", ld);
                    else
                        file.Get("landsdel_primo", ld);

                    file.Get("landsdel_primo", ld0);

                    file.Get("oprindelse", o);
                    file.Get("t", t);
                    //if (t[0] == 2008)  // Kun data for 2008 !!!!!!!!!!!!!!!!!!!!!!!          
                    if (SelectDataPeriod(t))  // Medtag data for 5 år i RAS matrix.
                    {
                        int male = 0;
                        int female = 1;
                        if (g[0] == 2)
                        {
                            male = 1;
                            female = 0;
                        }

                        for (int i = 0; i < 2; i++)
                        {
                            if (a[i] < AgeMin) a[i] = AgeMin;
                            if (a[i] > AgeMax) a[i] = AgeMax;
                            // Aggregeres og gøres 0-baserede
                            //TMA (2/10/2019): alder ultimo-ficeres
                            a[i] = a[i] - AgeMin + 1;
                            e[i] = Mappings.Education5((EEducationDS8)e[i]);
                            g[i] = g[i] - 1;
                            o[i] = o[i] - 1;

                            //PSP
                            //ld[i] = (int)Mappings.Region11(ld[i]);
                            if (MSSettings.UseMunicipalityDependentSBAM)
                                ld[i] = (int)Mappings.MapMunicipality(ld[i]);

                            // Danner indeks
                            j[i] = a[i] + I_Age * g[i]
                                        + I_Age * I_Gender * e[i]
                                        + I_Age * I_Gender * I_Edu * ld[i]
                                        + I_Age * I_Gender * I_Edu * I_Landsdel * o[i];
                        }

                        matchAge0[a[male] + AgeMin, a[female] + AgeMin]++;
                        matchEducation0[e[male], e[female]]++;
                        //PSP
                        matchRegion0[ld0[male], ld0[female]]++;
                        if (MSSettings.UseMunicipalityDependentSBAM)
                            matchMunicipality0[ld[male], ld[female]]++;

                        matchOrigin0[o[male], o[female]]++;

                        // Symmetrisk
                        lRas.Add(new RASMatrixCell(j[0], j[1], 1));
                        lRas.Add(new RASMatrixCell(j[1], j[0], 1));

                        n++;
                    }
                }
            }
            file.Close();
            #endregion indlæs data
            #region Grass sow
            if (MSSettings.SBAMMunicipalityGrassSow)
            {
                for (int a1 = MSSettings.MatchingMinAge; a1 < MSSettings.MatchingMaxAge - 10; a1++)
                    for (int s1 = 0; s1 < MSSettings.GENDER; s1++)
                        //for (int e1 = 0; e1 < MSSettings.EDU6; e1++)
                        for (int r1 = 0; r1 < I_Landsdel; r1++)
                            //for (int o1 = 0; o1 < I_Oprindelse; o1++)
                            for (int a2 = MSSettings.MatchingMinAge; a2 < MSSettings.MatchingMaxAge - 10; a2++)
                                for (int s2 = 0; s2 < MSSettings.GENDER; s2++)
                                    //for (int e2 = 0; e2 < MSSettings.EDU6; e2++)
                                    for (int r2 = 0; r2 < I_Landsdel; r2++)
                                    //for (int o2 = 0; o2 < I_Oprindelse; o2++ )
                                    {
                                        int e1 = 0;
                                        int e2 = 0;
                                        int o1 = 0;
                                        int o2 = 0;
                                        int male = 1;
                                        int female = 0;
                                        j[male] = a1 - MSSettings.MatchingMinAge + I_Age * s1
                                          + I_Age * I_Gender * e1
                                          + I_Age * I_Gender * I_Edu * r1
                                          + I_Age * I_Gender * I_Edu * I_Landsdel * o1;
                                        j[female] = a2 - MSSettings.MatchingMinAge + I_Age * s2
                                          + I_Age * I_Gender * e2
                                          + I_Age * I_Gender * I_Edu * r2
                                          + I_Age * I_Gender * I_Edu * I_Landsdel * o2;

                                        lRas.Add(new RASMatrixCell(j[0], j[1], 0.0001));
                                        lRas.Add(new RASMatrixCell(j[1], j[0], 0.0001));
                                    }
            }
            #endregion grass sow

#if !SBAM_TEST
            #region skriv MS_match-filer
            string fileName = MSSettings.OutputDir + "\\MS_matchAge0_" + MSSettings.ProjectionID + ".txt";
            StreamWriter stream = new StreamWriter(fileName);

            stream.WriteLine("AgeMale\tAgeFemale\tYear\tNumber\tID");
            for (int am = 0; am < MSSettings.MAX_AGE; am++)
                for (int af = 0; af < MSSettings.MAX_AGE; af++)
                    if (matchAge0[am, af] > 0)
                        stream.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", am, af, "Data", matchAge0[am, af], MSSettings.ProjectionID);
            stream.Close();
            stream.Dispose();

            fileName = MSSettings.OutputDir + "\\MS_matchEducation0_" + MSSettings.ProjectionID + ".txt";
            stream = new StreamWriter(fileName);

            stream.WriteLine("EduMale\tEduFemale\tYear\tNumber\tID");
            for (int am = 0; am < 5; am++)
                for (int af = 0; af < 5; af++)
                    if (matchEducation0[am, af] > 0)
                        stream.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", am, af, "Data", matchEducation0[am, af], MSSettings.ProjectionID);
            stream.Close();
            stream.Dispose();

            fileName = MSSettings.OutputDir + "\\MS_matchRegion0_" + MSSettings.ProjectionID + ".txt";
            stream = new StreamWriter(fileName);

            stream.WriteLine("RegionMale\tRegionFemale\tYear\tNumber\tID");
            for (int am = 0; am < MSSettings.REGION; am++)
                for (int af = 0; af < MSSettings.REGION; af++)
                    if (matchRegion0[am, af] > 0)
                        stream.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", am, af, "Data", matchRegion0[am, af], MSSettings.ProjectionID);
            stream.Close();
            stream.Dispose();

            //PSP
            if (MSSettings.UseMunicipalityDependentSBAM)
            {

                fileName = MSSettings.OutputDir + "\\MS_matchMunicipality0_" + MSSettings.ProjectionID + ".txt";
                stream = new StreamWriter(fileName);

                stream.WriteLine("MunicipalityMale\tMunicipalityFemale\tYear\tNumber\tID");
                for (int am = 0; am < MSSettings.MUNICIPALITY; am++)
                    for (int af = 0; af < MSSettings.MUNICIPALITY; af++)
                        if (matchMunicipality0[am, af] > 0)
                            stream.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", am, af, "Data", matchMunicipality0[am, af], MSSettings.ProjectionID);
                stream.Close();
                stream.Dispose();
            }

            fileName = MSSettings.OutputDir + "\\MS_matchOrigin0_" + MSSettings.ProjectionID + ".txt";
            stream = new StreamWriter(fileName);

            stream.WriteLine("OriginMale\tOriginFemale\tYear\tNumber\tID");
            for (int am = 0; am < MSSettings.ORIGIN5; am++)
                for (int af = 0; af < MSSettings.ORIGIN5; af++)
                    if (matchOrigin0[am, af] > 0)
                        stream.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", am, af, "Data", matchOrigin0[am, af], MSSettings.ProjectionID);
            stream.Close();
            stream.Dispose();
            #endregion skriv MS_match-filer
#endif

            if (wCons) Console.WriteLine("\rMatch> Linier læst: {0}", n);
            _testFile.WriteLine("Match> Linier læst: {0}", n);

            if (wCons) Console.WriteLine("\rMatch> Sorterer...");
            _testFile.WriteLine("Match> Sorterer...");

            //Sorter
            lRas.Sort(delegate(RASMatrixCell a, RASMatrixCell b)
            {
                int diff = a.Col - b.Col;
                if (diff != 0) return diff;
                return a.Row - b.Row;
            });

            //Aggregerer celler
            if (wCons) Console.WriteLine("\rMatch> Aggregerer...");
            _testFile.WriteLine("Match> Aggregerer...");

            for (int i = 1; i < lRas.Count; i++)
                if (lRas[i].Col == lRas[i - 1].Col && lRas[i].Row == lRas[i - 1].Row)
                {
                    lRas[i].Value += lRas[i - 1].Value;
                    lRas[i - 1].Row = -1; // Markerer at den skal fjernes      
                }

            lRas.RemoveAll(x => x.Row == -1);  // Fjerner de markerede //NER: Hvorfor først markere og derefter fjerne, hvorfor ikke bare fjerne i første omgang?

            if (wCons) Console.WriteLine("\rMatch> Allokerer RASMatrix...");
            _testFile.WriteLine("Match> Allokerer RASMatrix...");

            //TEST2: remember which types occur in the historical data (the table is symmetric, so the rows are enough)
            _hasData = new bool[I_Age * I_Gender * I_Edu * I_Landsdel * I_Oprindelse];
            foreach (RASMatrixCell c in lRas)
                if (c.Row < _hasData.Length)
                    _hasData[c.Row] = true;

            //TEST1: RASMatrix_test instead of RASMatrix
            _ras = new RASMatrix_test(lRas, I_Age * I_Gender * I_Edu * I_Landsdel * I_Oprindelse, false);

        }

        protected virtual bool SelectDataPeriod(int[] t)
        {
            return t[0] >= 2008 && t[0] <= 2012;
        }

        public void matching()
        {
            Console.Write("\r      Matching (Initialisering)...                                ");

            bool wCons = false; // Skiv på konsollen (bør normalt være false)      

            if (wCons) Console.WriteLine("\rMatching> Start...");
            _testFile.WriteLine("****************************************************************************");
            _testFile.WriteLine("År: {0}", _sim.Time.Now);

            List<Household> matchingHouseholds = new List<Household>(); // En liste af husholdninger, der kan matches/parres.

            // Danner maching-pool 
            foreach (Household h in _sim.Households)
                if (h.State.Is(EHouseholdState.InMatchingPool))
                    matchingHouseholds.Add(h);

            int I_Age = 1 + MSSettings.MatchingMaxAge - MSSettings.MatchingMinAge;
            int I_Gender = MSSettings.GENDER;
            int I_Edu = 5; // HACK: Hard-kodede konstanter
            int I_Region = MSSettings.REGION; // Antal ladsdele. Laves om til kommuner nedenfor
            int I_Herkomst = MSSettings.ORIGIN5;

            //PSP
            if (MSSettings.UseMunicipalityDependentSBAM)
                I_Region = MSSettings.MUNICIPALITY;

            // Dett er antallet af forskellige typer
            int I = I_Age * I_Gender * I_Edu * I_Region * I_Herkomst;

            // Vektor der tæller fordelingen af typer
            double[] target = new double[I];

            if (wCons) Console.WriteLine("\rMatching> Antal personer i pool\t{0}", matchingHouseholds.Count);
            _testFile.WriteLine("Antal personer i pool\t{0}", matchingHouseholds.Count);

            // dic.TryGetValue(j, out lst) returnerer en liste lst af husholdninger af type j
            Dictionary<int, List<Household>> dic = new Dictionary<int, List<Household>>();

            if (wCons) Console.WriteLine("\rMatching> Fylder dictionary...");
            _testFile.WriteLine("Fylder dictionary...");
            int nHusk = 0; // Tæller hvor mange der faktisk bruges
            int[] nHuskMun = new int[MSSettings.MUNICIPALITY];
            foreach (Household h in matchingHouseholds)
            {
                Person p = h.FirstAdult;

                if (p.Age >= MSSettings.MatchingMinAge && p.Age <= MSSettings.MatchingMaxAge)
                {
                    nHusk++;
                    nHuskMun[(int)p.Household.Dwelling.DwellingMunicipality]++;

                    int j = 0;

                    //PSP
                    // beregner type j
                    if (MSSettings.UseMunicipalityDependentSBAM)
                    {
                        j = p.Age - MSSettings.MatchingMinAge + I_Age * (int)p.Gender
                                                              + I_Age * I_Gender * p.HighestEducation5
                                                              + I_Age * I_Gender * I_Edu * (int)p.Municipality
                                                              + I_Age * I_Gender * I_Edu * I_Region * (int)p.Origin5;
                    }
                    else
                    {
                        j = p.Age - MSSettings.MatchingMinAge + I_Age * (int)p.Gender
                                                                  + I_Age * I_Gender * p.HighestEducation5
                                                                  + I_Age * I_Gender * I_Edu * (int)p.Region
                                                                  + I_Age * I_Gender * I_Edu * I_Region * (int)p.Origin5;

                    }

                    target[j]++;  // Tæller en op for type j
                    List<Household> list;
                    if (dic.TryGetValue(j, out list))
                        list.Add(h); //hvis der allerede findes en liste med denne type husholdning i dic, tilføjes h listen
                    else
                        dic.Add(j, new List<Household>() { h }); //hvis der ikke findes husholdninger af samme type i dic, tilføjes en liste med h som eneste element
                }
            }

            int ddd = matchingHouseholds.Count - nHusk;
            Console.WriteLine("\r      Matching       : {0} ud af {1} (dvs. {2:#.#}%) udeladt af matching", ddd, matchingHouseholds.Count, 100 * (double)ddd / matchingHouseholds.Count);

            if (wCons) Console.WriteLine("\rMatching> Antal personer i pool\t{0}", nHusk);
            _testFile.WriteLine("Antal personer i pool (aldersgrænse)\t{0}", nHusk);

            int antalPar = nHusk / 2;
            //int targetAntalPar = (int)(antalPar / MSSettings.MatchingScaleUp);
            double targetAntalPar_tmp = 0;
            for (int k = 0; k < MSSettings.MUNICIPALITY; k++)
            {
                double antalParMun = nHuskMun[k] / 2;

                targetAntalPar_tmp += antalParMun / MSSettings.MatchingScaleUp[k];
            }

            int targetAntalPar = (int)targetAntalPar_tmp;

            if (wCons) Console.WriteLine("\rMatching> Maksimalt antal par\t{0}", antalPar);
            _testFile.WriteLine("Maksimalt antal par\t{0}", antalPar);

            if (wCons) Console.WriteLine("\rMatching> Randomiserer...");
            _testFile.WriteLine("Randomiserer...");

            foreach (var p in dic)
                p.Value.Shuffle(); //Randomiser lister

            Console.Write("\r      Matching (RAS-adjustment)...                                ");

            if (wCons) Console.WriteLine("\rMatching> RAS-adjustment:");
            _testFile.WriteLine("RAS-adjustment:");

            //TEST2: the target for each type is set so that the table sums to the number of couples to be formed
            LastPersonsWithoutData = 0;
            int couplesInTable = targetAntalPar;
            if (TargetAsCouples)
            {
                couplesInTable = SetTargets(target, dic, targetAntalPar, I_Age, I_Gender, true);
                _testFile.WriteLine("TEST2: Målet summerer til\t{0} par ({1} personer er af typer uden historiske data)", couplesInTable, LastPersonsWithoutData);
            }

            //TEST0: what _ras.Reset() does
            _ras.RestoreData = ResetToData;

            //TEST1: stopping criterion for the RAS adjustment
            _ras.RunToConvergence = RasToConvergence;
            _ras.MaxPasses = RasMaxPasses;
            _ras.Tolerance = RasTolerance;

            //TEST4: the way it is computed
            _ras.Method = RasMethod;
            _ras.ActiveOnly = RasActiveOnly;

            // Matricen _ras er fyldt med rådata i initializeMatching() 
            // Her tilpasses den til target
            var rasTimer = System.Diagnostics.Stopwatch.StartNew();   //TEST: timing
            List<RASMatrixCell> rasAdj = _ras.RASAdjust(target, target);
            rasTimer.Stop();

            //TEST1
            LastRasPasses = _ras.Passes;
            LastRasError = _ras.Error;
            LastRasConverged = _ras.Converged;
            _testFile.WriteLine("TEST1: RAS-gennemløb\t{0}\tfejl i forhold til mål\t{1:0.0e+0}\tkonvergeret\t{2}", _ras.Passes, _ras.Error, _ras.Converged);


            Console.Write("\r      Matching (Efterbehandling)...                                ");

            #region sorter
            if (wCons) Console.WriteLine("Matching> Sorterer rasAdj...");
            _testFile.WriteLine("Sorterer rasAdj...");
            rasAdj.Sort(delegate(RASMatrixCell a, RASMatrixCell b)
            {
                double diff = a.Value - b.Value;
                if (diff == 0) return 0;
                if (diff < 0) return -1;
                return 1;
            });
            #endregion sorter

            double dTest = 0;
            foreach (RASMatrixCell c in rasAdj)
                dTest += c.Value;

            int n_target = (int)Math.Floor(dTest);
            LastCouplesInTable = dTest / 2.0;   //TEST: a folded cell holds twice the expected number of couples

            if (wCons) Console.WriteLine("\rMatching> Test: Antal personer i rasAdj\t{0}", dTest);
            _testFile.WriteLine("Test: Antal personer i rasAdj\t{0}", dTest);

            double dTest2 = 0;
            foreach (RASMatrixCell c in rasAdj)
                dTest2 += Math.Floor(c.Value);

            if (wCons) Console.WriteLine("\rMatching> Test: Antal personer i rasAdj efter afrunding\t{0}", dTest2);
            _testFile.WriteLine("Test: Antal personer i rasAdj efter afrunding\t{0}", dTest2);
            _testFile.WriteLine("Test: Andel efter afrunding\t{0:#.##}%", 100 * dTest2 / dTest);

            int ukendtType = 0;
            int udeladt = 0;
            int succes = 0;
            if (wCons) Console.WriteLine("\rMatching> Finder par...");
            _testFile.WriteLine("Finder par...");

            Console.Write("\r      Matching (Shuffle)...                                ");
            rasAdj.Shuffle();

            Console.Write("\r      Matching (Par-dannelse)...                            ");

            int pairsCount = 0;
            var pairingTimer = System.Diagnostics.Stopwatch.StartNew();   //TEST: timing

            //TEST3: pairing in one pass instead of the loop below
            int round = 1;
            if (PairingMethod != EPairingMethod.Original)
            {
                pairsCount = FormCouplesInOnePass(rasAdj, dic, targetAntalPar, I_Age, I_Gender, I_Edu, I_Region, I_Herkomst,
                                                  ref ukendtType, ref udeladt, ref succes);
                _testFile.WriteLine("TEST3: Runde 1\t{0} par", pairsCount);

                // Mop-up rounds: adjustment and pairing are repeated on the persons who are left in the lists
                while (pairsCount < targetAntalPar && round < MaxRounds)
                {
                    round++;
                    _ras.Reset();
                    SetTargets(target, dic, targetAntalPar - pairsCount, I_Age, I_Gender, false);
                    pairingTimer.Stop(); rasTimer.Start();
                    rasAdj = _ras.RASAdjust(target, target);
                    rasTimer.Stop(); pairingTimer.Start();
                    int newCouples = FormCouplesInOnePass(rasAdj, dic, targetAntalPar - pairsCount, I_Age, I_Gender, I_Edu, I_Region, I_Herkomst,
                                                          ref ukendtType, ref udeladt, ref succes);
                    pairsCount += newCouples;
                    _testFile.WriteLine("TEST3: Runde {0}\t{1} par", round, newCouples);
                    if (newCouples == 0)
                        break;
                }

                goto endFordelPar;
            }

            //PSP1
            int itt = 0;
            int tal = 0;
            int tal2 = 0;
            //while (pairsCount < targetAntalPar)
            while (itt < 1000)
            {
                itt++;
                if (wCons) Console.WriteLine("Iteration: {0}", itt);


                if (itt / 100 == tal2 + 1)
                    tal2++;


                foreach (RASMatrixCell c in rasAdj)
                {
                    // Test om vi har fundet det ønskede antal ar
                    if (pairsCount >= targetAntalPar)
                        //PSP1
                        //break;
                        goto endFordelPar;

                    //PSP1
                    if (c.Value == 0)
                        continue;

                    //PSP1
                    //int n = (int)Math.Floor(c.Value / 2.0); // n = Antal par
                    //c.Value -= 2.0 * n;
                    int n = 1;  // Antal par der skal laves
                    if (c.Value >= 2.0)
                        c.Value -= 2.0;
                    else
                        n = 0;

                    //PSP1
                    if (c.Value < 0)
                        throw new Exception("ups!");

                    // Det gælder at c.Value < 2.0 på grund af ovenstående.
                    // Med ssh'en (c.Value/2.0) tilføjer vi 1 par
                    // Det giver den korrekte forventede værdi
                    //PSP1
                    if (c.Value < 2.0)
                    {
                        if (Simulation.Random.NextEvent(c.Value / 2.0))
                        {
                            n++;  //n = 1
                            c.Value = 0;
                        }
                    }

                    if (n == 0)
                        continue;

                    List<Household> lCol = null;
                    List<Household> lRow = null;

                    // Samme type (dvs. også samme køn) hvis c.Row == c.Col
                    // bHome = true hvis 100% ens (samme køn, uddannelse osv.)
                    bool bHomo = c.Row == c.Col;

                    // Finder husholdnings-lister af de rette typer
                    if (!dic.TryGetValue(c.Row, out lRow) || (!bHomo && !dic.TryGetValue(c.Col, out lCol)))
                    {
                        // Registrerer hvis det mislykkedes
                        //ukendtType++;
                        //udeladt++;
                        ukendtType += n;
                        udeladt += n;
                        continue;
                    }

                    if (!bHomo)
                    {

                        int m = Math.Min(Math.Min(n, lCol.Count), lRow.Count);

                        if (m < n)
                            udeladt += n - m;

                        succes += m;

                        for (int i = 0; i < m; i++)
                        {
                            parring(lRow[i], lCol[i]); //nyt par
                            pairsCount++;
                        }

                        lRow.RemoveRange(0, m);
                        lCol.RemoveRange(0, m);
                    }
                    else // hvis homo
                    {
                        int m = Math.Min(n, lRow.Count / 2);

                        if (m < n)
                            udeladt += n - m;

                        succes += m;

                        for (int i = 0; i < m; i++)
                        {
                            parring(lRow[i], lRow[i + m]); //nyt par
                            pairsCount++;
                        }

                        lRow.RemoveRange(0, 2 * m);
                    }

                }// end foreach (RASMatrixCell c in rasAdj)
                //PSP1
                rasAdj.Shuffle();
            }// end while (pairsCount < targetAntalPar)

   endFordelPar:


            pairingTimer.Stop();   //TEST: timing
            LastMsRas = rasTimer.Elapsed.TotalMilliseconds;
            LastMsPairing = pairingTimer.Elapsed.TotalMilliseconds;

            if (pairsCount < targetAntalPar) // Hvis ikke alle er matchede
            {
                _sim.LogFile.WriteLine("WARNING: Matching - pairs.Count = {0} < targetAntalPar = {1}", pairsCount, targetAntalPar);
                _sim.LogFile.NumberOfWarnings++;
                Console.WriteLine("\r      Matching !!!: pairs.Count = {0} < targetAntalPar = {1}", pairsCount, targetAntalPar);
            }

            Console.WriteLine("\r      Antal matchede : {0}                                                 ", 2 * pairsCount);

            if (wCons) Console.WriteLine("\rMatching> udeladt\t\t{0}", udeladt);
            if (wCons) Console.WriteLine("Matching> ukendtType\t\t{0}", ukendtType);
            if (wCons) Console.WriteLine("Matching> For små\t\t{0}", antalPar - succes - ukendtType - udeladt);
            if (wCons) Console.WriteLine("Matching> succes\t\t{0}", succes);
            if (wCons) Console.WriteLine("Matching> targetAntalPar\t\t{0}", targetAntalPar);
            if (wCons) Console.WriteLine("Matching> Succes-rate\t\t{0}", 1.0 * succes / antalPar);
            if (wCons) Console.WriteLine("Matching> Slut...");

            _testFile.WriteLine("udeladt\t\t{0}", udeladt);
            _testFile.WriteLine("ukendtType\t\t{0}", ukendtType);
            _testFile.WriteLine("For små\t\t{0}", antalPar - succes - ukendtType - udeladt);
            _testFile.WriteLine("succes\t\t{0}", succes);
            _testFile.WriteLine("targetAntalPar\t\t{0}", targetAntalPar);
            _testFile.WriteLine("Succes-rate\t\t{0}", 1.0 * succes / antalPar);

            //TEST: results for the test program
            LastTargetCouples = targetAntalPar;
            LastCouples = pairsCount;
            LastRounds = round;
            LastOmitted = udeladt;

            _ras.Reset();
        }

        #region TEST2: SetTargets()
        /// <summary>
        /// Sets target for each type from the persons in the lists in dic, and returns the number of couples the
        /// target sums to (at most wantedCouples).
        /// With TargetAsCouples each person counts 1/MatchingScaleUp for his or her municipality. Then the men's
        /// targets and the women's targets are scaled separately so that they sum to the number of couples. This is
        /// necessary for the RAS adjustment to have a solution when the pool does not hold equally many men and
        /// women, and it means that the surplus of one gender is left unmatched instead of being put into cells with
        /// two of the same gender. No type gets a target larger than the number of persons of the type. Types that
        /// do not occur in the historical data get the target 0 (RASAdjust() does that in any case); the persons are
        /// counted in LastPersonsWithoutData if countWithoutData is set.
        /// Without TargetAsCouples the target is the number of persons of each type, as in Matching.cs.
        /// </summary>
        int SetTargets(double[] target, Dictionary<int, List<Household>> dic, int wantedCouples, int I_Age, int I_Gender, bool countWithoutData)
        {
            Array.Clear(target, 0, target.Length);

            if (!TargetAsCouples)
            {
                foreach (var p in dic)
                    target[p.Key] = p.Value.Count;
                return wantedCouples;
            }

            double[] count = new double[target.Length];   // persons of each type who can be matched
            int[] countByGender = new int[I_Gender];
            List<int>[] types = new List<int>[I_Gender];  // the types of each gender
            for (int g = 0; g < I_Gender; g++)
                types[g] = new List<int>();

            foreach (var p in dic)
            {
                int j = p.Key;
                if (p.Value.Count == 0)
                    continue;

                if (!_hasData[j])
                {
                    if (countWithoutData) LastPersonsWithoutData += p.Value.Count;
                    continue;
                }

                double weight = 0;
                foreach (Household h in p.Value)
                    weight += 1.0 / MSSettings.MatchingScaleUp[(int)h.FirstAdult.Household.Dwelling.DwellingMunicipality];

                int gender = (j / I_Age) % I_Gender;   // the type index is age + I_Age * gender + ...
                target[j] = weight;
                count[j] = p.Value.Count;
                countByGender[gender] += p.Value.Count;
                types[gender].Add(j);
            }

            // Number of couples: the wanted number, but at most the number of men and the number of women there are
            int couples = wantedCouples;
            for (int g = 0; g < I_Gender; g++)
                couples = Math.Min(couples, countByGender[g]);

            // Scales the targets of each gender to the number of couples. Types that hit the cap (all of the type
            // are matched) are held fixed, and the remainder is distributed over the other types
            for (int g = 0; g < I_Gender; g++)
            {
                List<int> free = types[g];
                double remaining = couples;
                while (free.Count > 0)
                {
                    double sum = 0;
                    foreach (int j in free)
                        sum += target[j];
                    double factor = remaining / sum;

                    List<int> stillFree = new List<int>();
                    foreach (int j in free)
                    {
                        if (target[j] * factor > count[j] * (1 + 1e-12))
                        {
                            target[j] = count[j];
                            remaining -= count[j];
                        }
                        else
                            stillFree.Add(j);
                    }

                    if (stillFree.Count == free.Count)   // none hit the cap
                    {
                        foreach (int j in free)
                            target[j] *= factor;
                        break;
                    }
                    free = stillFree;
                }
            }

            return couples;
        }
        #endregion

        #region TEST3: FormCouplesInOnePass()
        /// <summary>
        /// Forms couples from the adjusted, folded table in one pass through the cells and returns the number (at most
        /// maxCouples). In a folded cell Value is twice the expected number of couples e. The cell gets the integer part
        /// of e plus 0 or 1 couple for the fraction, so that the expected number is e:
        ///   Independent:  1 extra couple with probability equal to the fraction, independently from cell to cell.
        ///   Systematic:   the cells are put in a fixed order, and e is accumulated across them from a random starting
        ///                 point between 0 and 1. The cell gets a couple each time the running sum passes an integer.
        ///                 The number of couples in any contiguous run of cells then deviates by less than 1 from the
        ///                 expected number.
        /// </summary>
        int FormCouplesInOnePass(List<RASMatrixCell> rasAdj, Dictionary<int, List<Household>> dic, int maxCouples,
                                 int I_Age, int I_Gender, int I_Edu, int I_Region, int I_Origin,
                                 ref int unknownType, ref int omitted, ref int success)
        {
            // 1. The order of the cells (only for systematic selection)
            if (PairingMethod == EPairingMethod.SystematicTypeOrder)
            {
                rasAdj.Sort(delegate(RASMatrixCell a, RASMatrixCell b)
                {
                    int diff = a.Col - b.Col;
                    if (diff != 0) return diff;
                    return a.Row - b.Row;
                });
            }
            else if (PairingMethod == EPairingMethod.SystematicHierarchical)
            {
                foreach (RASMatrixCell c in rasAdj)
                    c.Index = SortKey(c.Col, c.Row, I_Age, I_Gender, I_Edu, I_Region, I_Origin);

                rasAdj.Sort(delegate(RASMatrixCell a, RASMatrixCell b)
                {
                    return a.Index.CompareTo(b.Index);
                });
            }

            // 2. Number of couples in each cell. The number is put in c.Value (_ras.Reset() restores the cells afterwards)
            double cum = 0;   // running sum of expected couples
            if (PairingMethod != EPairingMethod.Independent)
                cum = Simulation.Random.NextDouble();   // random starting point

            foreach (RASMatrixCell c in rasAdj)
            {
                double e = c.Value / 2.0;   // expected number of couples in the cell
                int n;
                if (PairingMethod == EPairingMethod.Independent)
                {
                    n = (int)Math.Floor(e);
                    if (Simulation.Random.NextEvent(e - n))
                        n++;
                }
                else
                {
                    n = (int)(Math.Floor(cum + e) - Math.Floor(cum));
                    cum += e;
                }
                c.Value = n;
            }

            // 3. The couples are formed. The cells are taken in random order, so that it is not particular cells
            //    that lose couples when a type is used up
            rasAdj.Shuffle();

            int couples = 0;
            foreach (RASMatrixCell c in rasAdj)
            {
                if (couples >= maxCouples)
                    break;

                int n = Math.Min((int)c.Value, maxCouples - couples);   // number of couples to form
                if (n == 0)
                    continue;

                List<Household> lCol = null;
                List<Household> lRow = null;

                // Same type (and thus also same gender) if c.Row == c.Col
                bool bHomo = c.Row == c.Col;

                // Finds the household lists of the right types
                if (!dic.TryGetValue(c.Row, out lRow) || (!bHomo && !dic.TryGetValue(c.Col, out lCol)))
                {
                    unknownType += n;
                    omitted += n;
                    continue;
                }

                int m;
                if (!bHomo)
                {
                    m = Math.Min(Math.Min(n, lCol.Count), lRow.Count);

                    for (int i = 0; i < m; i++)
                        parring(lRow[i], lCol[i]); // new couple

                    lRow.RemoveRange(0, m);
                    lCol.RemoveRange(0, m);
                }
                else // two of the same type
                {
                    m = Math.Min(n, lRow.Count / 2);

                    for (int i = 0; i < m; i++)
                        parring(lRow[i], lRow[i + m]); // new couple

                    lRow.RemoveRange(0, 2 * m);
                }

                if (m < n)
                    omitted += n - m;

                success += m;
                couples += m;
            }

            return couples;
        }

        /// <summary>
        /// Sort key for a cell with the types t1 and t2. The cells are ordered by the characteristics of the couple in
        /// this order: gender, origin, region (province or municipality), education, five-year age group and age, at
        /// each level for both partners (the man first, if the couple consists of a man and a woman). Cells that
        /// resemble each other thereby end up next to each other, and the systematic selection hits the number of
        /// couples at all the levels (e.g. couples with a particular combination of regions and educations) with a
        /// deviation of less than 1.
        /// </summary>
        static long SortKey(int t1, int t2, int I_Age, int I_Gender, int I_Edu, int I_Region, int I_Origin)
        {
            int gender1 = (t1 / I_Age) % I_Gender, gender2 = (t2 / I_Age) % I_Gender;
            if (gender1 > gender2)   // EGender.Male = 0, so the man comes first
            {
                int t = t1; t1 = t2; t2 = t;
                t = gender1; gender1 = gender2; gender2 = t;
            }

            int age1 = t1 % I_Age, age2 = t2 % I_Age;
            int edu1 = (t1 / (I_Age * I_Gender)) % I_Edu, edu2 = (t2 / (I_Age * I_Gender)) % I_Edu;
            int reg1 = (t1 / (I_Age * I_Gender * I_Edu)) % I_Region, reg2 = (t2 / (I_Age * I_Gender * I_Edu)) % I_Region;
            int origin1 = t1 / (I_Age * I_Gender * I_Edu * I_Region), origin2 = t2 / (I_Age * I_Gender * I_Edu * I_Region);
            int I_Age5 = (I_Age + 4) / 5;   // number of five-year age groups

            long k = gender1 + gender2;     // 0 = two men, 1 = man and woman, 2 = two women
            k = k * I_Origin + origin1;
            k = k * I_Origin + origin2;
            k = k * I_Region + reg1;
            k = k * I_Region + reg2;
            k = k * I_Edu + edu1;
            k = k * I_Edu + edu2;
            k = k * I_Age5 + age1 / 5;
            k = k * I_Age5 + age2 / 5;
            k = k * I_Age + age1;
            k = k * I_Age + age2;
            return k;
        }
        #endregion

        #region parring()
        /// <summary>
        /// Hjælpemetode til Matching
        /// </summary>
        /// <param name="h1">Første single husholdning, der skal matches</param>
        /// <param name="h2">Anden single husholdning, der skal matches</param>
        void parring(Household h1, Household h2)
        {
            _sim.Statistics.Report(Event.Demographics.Matching, h1, h2);
#if !SBAM_TEST
            _sim.Statistics.Report(Event.Demographics.ShackUp, h1.FirstAdult, 3);
            _sim.Statistics.Report(Event.Demographics.ShackUp, h2.FirstAdult, 3);

            Dwelling dw_matching = dwelChar.getDwellingMatching(h1, h2, Simulation.Random);

            // Reporter flytning, hvis ny bolig er forskellig fra en af de to matchedes familiers tidligere bolig
            if (h1.Dwelling != dw_matching)
            {
                h1.FirstAdult.Move = true;

                if (h1.FirstAdult.ReasonToMove == -1) // Hvis vedkommende er blevet skilt eller flyttet hjemmefra tælles flytningen under disse årsager (Reason = 0,1)
                {
                    h1.FirstAdult.ReasonToMove = 2;

                    // Ved forudgående skilsmisse er det antaget, at børnene følger moderen og dermed er de blevet registreret med årsag = 1, hvis moderen flytter
                    // Ved matching uden forudgående skilsmisse, skal børnene tælles som flyttede med årsag = 2, hvis single forælderen flytter:
                    foreach (Person c in h1.Children)
                    {
                        c.Move = true;
                        c.ReasonToMove = h1.FirstAdult.ReasonToMove;
                    }

                }


            }
            //gentag for anden husholdning
            if (h2.Dwelling != dw_matching)
            {
                h2.FirstAdult.Move = true;

                if (h2.FirstAdult.ReasonToMove == -1) // Hvis vedkommende er blevet skilt eller flyttet hjemmefra tælles flytningen under disse årsager (Reason = 0,1)
                {
                    h2.FirstAdult.ReasonToMove = 2;
                    // Ved forudgående skilsmisse er det antaget, at børnene følger moderen og dermed er de blevet registreret med årsag = 1, hvis moderen flytter
                    // Ved matching uden forudgående skilsmisse, skal børnene tælles som flyttede med årsag = 2, hvis single forælderen flytter:
                    foreach (Person c in h2.Children)
                    {
                        c.Move = true;
                        c.ReasonToMove = h2.FirstAdult.ReasonToMove;
                    }
                }
            }

            h1.Dwelling = dw_matching; //h1 flytter ind i ny fælles bolig
            h1.AddHousehold(h2); //h2 flytter nu også ind hos h1 i den nye bolig
            foreach (Person p in h1.Adults)
                p.EventProc(Event.Demographics.Matching, Simulation.Random);

            h1.MakeNewID(); //pga pardannelse tildeles husholdning ny ID
            if (MSSettings.DoWageModel) //fast ejendom fastlægges for ny fælles husholdning
            {
                h1.UpdateEjdParticipation((RandomEvent)Simulation.Random);
            }
#endif
        }
        #endregion
    }
}
