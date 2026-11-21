
// SBAM_TEST: Når konstanten er defineret, slås de linjer fra, som kræver resten af SMILE-modellen
// (R-namespace, rapportfiler, boligvalg, flytning og husholdningsdannelse i parring()), så
// SBAM-algoritmen kan testes isoleret i CodeTest-projektet. Sæt "//" foran linjen for at få den
// oprindelige kode tilbage. Alle frakoblede linjer står mellem "#if !SBAM_TEST" og "#endif".
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
    class Matching
    {
        protected StreamWriter _testFile = new StreamWriter(MSSettings.OutputDir + "\\testMatching2_" + MSSettings.ProjectionID + ".txt");
        protected Simulation _sim;
        protected RASMatrix _ras;
        protected DwellingCharacteristics dwelChar;

        public Matching()
        {

        }

        public Matching(DwellingCharacteristics _dwelChar)
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

            _ras = new RASMatrix(lRas, I_Age * I_Gender * I_Edu * I_Landsdel * I_Oprindelse, false);

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

            // Matricen _ras er fyldt med rådata i initializeMatching() 
            // Her tilpasses den til target
            List<RASMatrixCell> rasAdj = _ras.RASAdjust(target, target);


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

            _ras.Reset();
        }

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
