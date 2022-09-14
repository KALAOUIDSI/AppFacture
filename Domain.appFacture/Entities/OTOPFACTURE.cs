using System;
using System.Globalization;
using System.Collections.Generic;
using System.Data;
using System.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.Text;
using System.Linq;
using System.Data.Entity;
using Domain.appFacture.Rapports;
using Domain.appFacture.Datasets;
using log4net;

namespace Domain.appFacture
{
    public partial class OTOPFACTURE
    {
        public static string agrDbLink = ConfigurationManager.AppSettings["AGRDBLINK"];
        public static string agressostring = ConfigurationManager.ConnectionStrings["Agrtest"].ConnectionString;
        public static string logoPath = ConfigurationManager.AppSettings["LOGOPATH"];
        public static ILog logger = log4net.LogManager.GetLogger("KassagrWEBLogger");
        public static string TARIFOTOPGLO = ConfigurationManager.AppSettings["TARIFOTOP"];
        public static string TARIFOTOPTOTAL = ConfigurationManager.AppSettings["TARIFOTOPTOTAL"];
        public static string SITESSPECTARIF = ConfigurationManager.AppSettings["SITESSPECTARIF"];

        public List<TOTAUXTVA> TVATOTAUX = new List<TOTAUXTVA>();
        void OnCreated()
        {
        }

        public List<OTOPFACTURE> getAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.OTOPFACTURE
                             select c;
                return result.Include("OTOPFACTUREDETAIL").Include("FACTFACTURESTATUS").Include("OTOPVENDEUR").Include("OTOPACHETEUR").Include("FACTTYPE").ToList();
            }
        }

        public List<OTOPFACTURE> getAll(int idSite)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.OTOPFACTURE
                             where c.IDACHETEUR == idSite
                             select c;
                return result.Include("OTOPFACTUREDETAIL").Include("FACTFACTURESTATUS").Include("OTOPVENDEUR").Include("OTOPACHETEUR").Include("FACTTYPE").ToList();
            }
        }
        public int countAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.OTOPFACTURE
                             select c;
                return result.Count();
            }
        }

        public int countAll(int idsite)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.OTOPFACTURE
                             where (c.IDACHETEUR==idsite || idsite==-1)
                             select c;
                return result.Count();
            }
        }

        public int countAll(List<long> SiteRestriction)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTUSER
                             where !c.FACTGROUPE.Any(u => u.LIBELLEGROUPE == "SuperAdmin" | u.LIBELLEGROUPE == "Commercial" | u.LIBELLEGROUPE == "Administrateur") 
                             select c;
                return result.Count();
            }
        }

        public List<OTOPFACTUREDETAIL> getAllDetails(int iddemande)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.OTOPFACTUREDETAIL
                             where c.IDDEMANDE == iddemande
                             select c;
                return result.ToList();
            }
        }

        public bool isIdValid(long id, List<long> SiteRestriction)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.FACTUSER
                              select c).Include("FACTGROUPE");
                if (SiteRestriction != null)
                    result = from c in result where !c.FACTGROUPE.Any(u => u.LIBELLEGROUPE == "SuperAdmin" | u.LIBELLEGROUPE == "Commercial" | u.LIBELLEGROUPE == "Administrateur") select c;
                else
                    result = from c in result where 1 != 1 select c;
                result = from c in result where c.IDUTILISATEUR == id select c;
                if (result.Count() > 0)
                    return true;
                else
                    return false;

            }
        }

        public OTOPFACTUREPaginationRes getAll(int page, int pageSize, string search, int sortby, Boolean isasc, OTOPFACTUREAdvFiltre AdvFiltre)
        {
            using (EramEntities DB = new EramEntities())
            {
                OTOPFACTUREPaginationRes res = new OTOPFACTUREPaginationRes();

                var result = (from c in DB.OTOPFACTURE
                              //where (c.IDACHETEUR == AdvFiltre.site || AdvFiltre.site==-1)
                              select c).Include("OTOPFACTUREDETAIL").Include("FACTFACTURESTATUS").Include("OTOPVENDEUR").Include("OTOPACHETEUR").Include("FACTTYPE");

                //recherche normale
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    int searchid;
                    if (int.TryParse(search, out searchid))
                        result = from c in result where c.REFERENCEFACT.ToLower().Contains(search.Trim()) || c.LIBELLEDEMANDE.ToLower().Contains(search.Trim()) || c.CHAPITRE.ToLower().Contains(search.Trim()) || c.FACTTYPE.LIBELLE.ToLower().Contains(search.Trim()) || c.OTOPACHETEUR.LIBELLESITE.ToLower().Contains(search.Trim()) select c;
                    else
                        result = from c in result where c.REFERENCEFACT.ToLower().Contains(search.Trim()) || c.LIBELLEDEMANDE.ToLower().Contains(search.Trim()) || c.CHAPITRE.ToLower().Contains(search.Trim()) || c.FACTTYPE.LIBELLE.ToLower().Contains(search.Trim()) || c.OTOPACHETEUR.LIBELLESITE.ToLower().Contains(search.Trim()) select c;
                }

                //recherche avancée
                if (!string.IsNullOrWhiteSpace(AdvFiltre.reference))
                    result = result.Where(c => c.REFERENCEFACT.ToLower().Contains(AdvFiltre.reference.ToLower()));

                if (!string.IsNullOrWhiteSpace(AdvFiltre.designation))
                    result = result.Where(c => c.LIBELLEDEMANDE.ToLower().Contains(AdvFiltre.designation.ToLower()));

                if (!string.IsNullOrWhiteSpace(AdvFiltre.chapitre))
                    result = result.Where(c => c.CHAPITRE.ToLower().Contains(AdvFiltre.chapitre.ToLower()));

                //if (!string.IsNullOrWhiteSpace(AdvFiltre.typefacture))
                //    result = result.Where(c => c.FACTTYPE.LIBELLE.ToLower().Contains(AdvFiltre.typefacture.ToLower()));

                if (!string.IsNullOrWhiteSpace(AdvFiltre.client))
                    result = result.Where(c => c.OTOPACHETEUR.LIBELLESITE.ToLower().Contains(AdvFiltre.client.ToLower()));


                if (!string.IsNullOrWhiteSpace(AdvFiltre.typefacture))
                {
                    int idType = 0;
                    int.TryParse(AdvFiltre.typefacture, out idType);
                    if (idType > 0)
                    if (idType > 0)
                            result = result.Where(c => c.IDFACTTYPE == idType);
                }

                if (!string.IsNullOrWhiteSpace(AdvFiltre.status))
                {
                    int idStatus = 0;
                    int.TryParse(AdvFiltre.status, out idStatus);
                    if (idStatus > 0)
                        result = result.Where(c => c.STATUS == idStatus);
                }

                // tri
                switch (sortby)
                {
                    case 0:
                        result = isasc ? result.OrderBy(c => c.REFERENCEFACT) : result.OrderByDescending(c => c.REFERENCEFACT);
                        break;
                    case 1:
                        result = isasc ? result.OrderBy(c => c.LIBELLEDEMANDE) : result.OrderByDescending(c => c.LIBELLEDEMANDE);
                        break;
                    case 2:
                        result = isasc ? result.OrderBy(c => c.CHAPITRE) : result.OrderByDescending(c => c.CHAPITRE);
                        break;
                    case 3:
                        result = isasc ? result.OrderBy(c => c.IDFACTTYPE) : result.OrderByDescending(c => c.IDFACTTYPE);
                        break;
                    case 4:
                        result = isasc ? result.OrderBy(c => c.STATUS) : result.OrderByDescending(c => c.STATUS);
                        break;
                    case 5:
                        result = isasc ? result.OrderBy(c => c.OTOPACHETEUR.LIBELLESITE) : result.OrderByDescending(c => c.OTOPACHETEUR.LIBELLESITE);
                        break;
                    case 6:
                        result = isasc ? result.OrderBy(c => c.MNTHT) : result.OrderByDescending(c => c.MNTHT);
                        break;
                    case 7:
                        result = isasc ? result.OrderBy(c => c.MNTTTC) : result.OrderByDescending(c => c.MNTTTC);
                        break;
                }

                res.count = result.Count();
                res.listDEMANDE = result
                .Skip((page - 1) * pageSize)
                .Take(pageSize).ToList();

                return res;
            }
        }

        public OTOPFACTURE getOne(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.OTOPFACTURE
                              where c.IDDEMANDE == id
                              select c).Include("OTOPFACTUREDETAIL").Include("FACTFACTURESTATUS").Include("OTOPVENDEUR").Include("OTOPACHETEUR").Include("FACTTYPE").First();
                return result;

            }
        }
        public OTOPFACTURE getOne(string libelle)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.OTOPFACTURE
                              where c.LIBELLEDEMANDE.ToLower().Contains(libelle.ToLower())
                              select c).Include("OTOPFACTUREDETAIL").Include("FACTFACTURESTATUS").Include("FACTCLIENT").Include("FACTSITE1").Include("FACTTYPE").Include("FACTSITE").Include("FACTPIECEJOINTE").FirstOrDefault();
                return result;

            }
        }
        public bool exists(string login)
        {
            using (EramEntities DB = new EramEntities())
            {
                var count = (from c in DB.FACTUSER
                             where c.LOGIN.ToLower() == login.ToLower()
                             select c).Count();
                if (count > 0)
                    return true;
                else
                    return false;

            }
        }

        public FACTPIECEJOINTE getOnePJ(int idpj)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.FACTPIECEJOINTE
                              where c.IDPIECEJOINTE == idpj
                              select c).FirstOrDefault();
                return result;
            }
        }

        public OTOPFACTURE update(int IDDEMANDE, string LIBELLEDEMANDE, int IdVendeur, int UTILISATEURCONNECTE, string datedeb, string datefin, string[][] details)
        {
            OTOPFACTURE OTOPFACTURE = new OTOPFACTURE();
            if (IDDEMANDE != 0)
            {
                //modification
                using (EramEntities DB = new EramEntities())
                {
                    OTOPFACTURE = (from c in DB.OTOPFACTURE
                                   where c.IDDEMANDE == IDDEMANDE
                                   select c).First();
                    
                    // Si avoir update Avoir
                    if (OTOPFACTURE.FLAGAVOIR.HasValue && OTOPFACTURE.FLAGAVOIR.Value == 1) {
                        OTOPFACTURE = updateAvoir(IDDEMANDE, LIBELLEDEMANDE, IdVendeur, UTILISATEURCONNECTE, datedeb, datefin, details);
                        return OTOPFACTURE;
                    }

                    List<OTOPFACTUREDETAIL> l1 = OTOPFACTURE.OTOPFACTUREDETAIL.ToList();
                    foreach (OTOPFACTUREDETAIL det1 in l1)
                    {
                        DB.Entry(det1).State = System.Data.EntityState.Deleted;
                    }

                    OTOPFACTURE.OTOPFACTUREDETAIL.Clear();
                    decimal demmntttc = 0m;
                    decimal demmntht = 0m;
                    decimal demmntttva = 0m;
                    if (details != null) { 
                        foreach(string[] det in details){
                            bool retour = false;
                            for (int i = 0; i < 4; i++)
                            {
                                if (det[i] == null || det[i].Length <= 0)
                                    retour = true;
                            }
                            if (retour)
                                continue;
                            decimal qte = decimal.Parse(det[3].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal prixunitaire = decimal.Parse(det[4].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal tauxtva = decimal.Parse(det[5].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal prixttc = prixunitaire + (tauxtva * 0.01m);
                            decimal mnttva = (tauxtva * 0.01m) * prixunitaire * qte;
                            decimal mntht = prixunitaire * qte;
                            decimal mntttc = prixunitaire * qte + mnttva;

                            OTOPFACTUREDETAIL fctdet = new OTOPFACTUREDETAIL();
                            fctdet.CODERAYON = det[0];
                            fctdet.REFERENCEPRODUIT = det[1];
                            fctdet.LIBELLEPRODUIT = det[2];
                            fctdet.QUANTITE = qte;
                            fctdet.PRIXUNITAIRE = prixunitaire;
                            fctdet.TAUXTVA = tauxtva;
                            fctdet.MNTTVA = mnttva;
                            fctdet.MNTHT = mntht;
                            fctdet.MNTTTC = mntttc;
                            demmntttc = demmntttc + mntttc;
                            demmntht = demmntht + mntht;
                            demmntttva = demmntttva + mnttva;
                            OTOPFACTURE.OTOPFACTUREDETAIL.Add(fctdet);
                        }
                    }

                    OTOPFACTURE.DATEMODIFICATION = DateTime.Now;
                    OTOPFACTURE.LIBELLEDEMANDE = (LIBELLEDEMANDE.Length > 50 ? LIBELLEDEMANDE.Substring(0, 50) : LIBELLEDEMANDE);

                    //OTOPFACTURE.CHAPITRE = CHAPITRE;
                    //OTOPFACTURE.RAYON = (short?)RAYON;
                    //OTOPFACTURE.RAYONACHETEUR = (short?)RAYONACHETEUR;
                    //OTOPFACTURE.CNUFVENDEUR = CNUFVENDEUR;
                    //OTOPFACTURE.CNUFACHETEUR = CNUFACHETEUR;
                    //OTOPFACTURE.ANNEEPRESTATION = ANNEEPRESTATION;
                    //if (IDFACTTYPE!=-1)
                    //    OTOPFACTURE.IDFACTTYPE = IDFACTTYPE;
                    //OTOPFACTURE.TYPECLIENT = (short)TYPECLIENT;

                    //OTOPFACTURE.IDACHETEUR = IdSiteConnecte;
                   // OTOPFACTURE.IDVENDEUR = IdVendeur;
                    //OTOPFACTURE.IDACHETEUR = IdVendeur;

                    OTOPFACTURE.DERNIERUTILISATEUR = UTILISATEURCONNECTE;

                    OTOPFACTURE.MNTHT = demmntht;
                    OTOPFACTURE.MNTTTC = demmntttc;
                    OTOPFACTURE.MNTTVA = demmntttva;
                    DB.Entry(OTOPFACTURE).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();

                }

            }
            else
            {

                using (EramEntities DB = new EramEntities())
                {
                    OTOPFACTURE.DATECREATION = DateTime.Now;
                    OTOPFACTURE.DATEMODIFICATION = DateTime.Now;
                    OTOPFACTURE.LIBELLEDEMANDE = (LIBELLEDEMANDE.Length > 50 ? LIBELLEDEMANDE.Substring(0, 50) : LIBELLEDEMANDE);

                    OTOPFACTURE.IDFACTTYPE = (new FACTTYPE()).getOneByCat(7).IDFACTTYPE ;

                    OTOPFACTURE.IDACHETEUR = IdVendeur;

                    OTOPFACTURE.DERNIERUTILISATEUR = UTILISATEURCONNECTE;                    
                    OTOPFACTURE.STATUS = 0;
                    OTOPFACTURE.PRIODEDEBUT = Convert.ToDateTime(DateTime.ParseExact(datedeb, "dd/MM/yyyy", CultureInfo.InvariantCulture));
                    OTOPFACTURE.PRIODEFIN = Convert.ToDateTime(DateTime.ParseExact(datefin, "dd/MM/yyyy", CultureInfo.InvariantCulture));

                    decimal demmntttc = 0m;
                    decimal demmntht = 0m;
                    decimal demmntttva = 0m;
                    if (details != null)
                    {
                        foreach (string[] det in details)
                        {
                            bool retour = false;
                            for (int i=0;i<4;i++)
                            {
                                if (det[i] == null || det[i].Length <= 0)
                                    retour = true;
                            }
                            if (retour)
                                continue;
                            decimal qte = decimal.Parse(det[1].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal prixunitaire = decimal.Parse(det[2].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal tauxtva = decimal.Parse(det[3].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal prixttc = prixunitaire + (tauxtva * 0.01m);
                            decimal mnttva = (tauxtva * 0.01m) * prixunitaire * qte;
                            decimal mntht = prixunitaire * qte;
                            decimal mntttc = prixunitaire * qte + mnttva;

                            OTOPFACTUREDETAIL fctdet = new OTOPFACTUREDETAIL();
                            fctdet.REFERENCEPRODUIT = det[5];// det[1];
                            fctdet.CODERAYON = det[4];// det[0];
                            fctdet.LIBRAYON = det[6];
                            fctdet.LIBELLEPRODUIT = det[0];
                            fctdet.QUANTITE = qte;
                            fctdet.PRIXUNITAIRE = prixunitaire;
                            fctdet.TAUXTVA = tauxtva;
                            fctdet.MNTTVA = mnttva;
                            fctdet.MNTHT = mntht;
                            fctdet.MNTTTC = mntttc;
                            demmntttc = demmntttc + mntttc;
                            demmntht = demmntht + mntht;
                            demmntttva = demmntttva + mnttva;
                            OTOPFACTURE.OTOPFACTUREDETAIL.Add(fctdet);
                        }
                    }
                    OTOPFACTURE.MNTHT = demmntht;
                    OTOPFACTURE.MNTTTC=demmntttc;
                    OTOPFACTURE.MNTTVA=demmntttva;

                    DB.OTOPFACTURE.Add(OTOPFACTURE);
                    DB.SaveChanges();
                }
            }
            return OTOPFACTURE;
        }

        public OTOPFACTURE createAvoir(int IDDEMANDE, string LIBELLEDEMANDE, int IdVendeur, int UTILISATEURCONNECTE, string datedeb, string datefin, string[][] details)
        {
                OTOPFACTURE facture = new OTOPFACTURE();
                facture = facture.getOne(IDDEMANDE);
                OTOPFACTURE FACTDEMANDE = new OTOPFACTURE();

                using (EramEntities DB = new EramEntities())
                {
                    FACTDEMANDE.FLAGAVOIR = 1;
                    FACTDEMANDE.DATECREATION = DateTime.Now;
                    FACTDEMANDE.DATEMODIFICATION = DateTime.Now;
                    FACTDEMANDE.LIBELLEDEMANDE = (LIBELLEDEMANDE.Length > 50 ? LIBELLEDEMANDE.Substring(0, 50) : LIBELLEDEMANDE);

                    FACTDEMANDE.IDFACTTYPE = facture.IDFACTTYPE;
                    FACTDEMANDE.REFERENCEFACT = facture.REFERENCEFACT;

                    FACTDEMANDE.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                    FACTDEMANDE.IDACHETEUR = facture.IDACHETEUR;
                    FACTDEMANDE.STATUS = 0;
                    FACTDEMANDE.ANNEEPRESTATION = facture.ANNEEPRESTATION;
                    FACTDEMANDE.CNUFVENDEUR = facture.CNUFVENDEUR;
                    FACTDEMANDE.CNUFACHETEUR = facture.CNUFACHETEUR;
                    FACTDEMANDE.RAYON = facture.RAYON;
                    FACTDEMANDE.RAYONACHETEUR = facture.RAYONACHETEUR;


                    FACTDEMANDE.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                    FACTDEMANDE.STATUS = 0;
                    FACTDEMANDE.PRIODEDEBUT = Convert.ToDateTime(DateTime.ParseExact(datedeb, "dd/MM/yyyy", CultureInfo.InvariantCulture));
                    FACTDEMANDE.PRIODEFIN = Convert.ToDateTime(DateTime.ParseExact(datefin, "dd/MM/yyyy", CultureInfo.InvariantCulture));

                    decimal demmntttc = 0m;
                    decimal demmntht = 0m;
                    decimal demmntttva = 0m;
                    if (details != null)
                    {
                        foreach (string[] det in details)
                        {
                            bool retour = false;
                            for (int i = 0; i < 4; i++)
                            {
                                if (det[i] == null || det[i].Length <= 0)
                                    retour = true;
                            }
                            if (retour)
                                continue;
                            decimal qte = decimal.Parse(det[1].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal prixunitaire = decimal.Parse(det[2].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal tauxtva = decimal.Parse(det[3].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal prixttc = prixunitaire + (tauxtva * 0.01m);
                            decimal mnttva = (tauxtva * 0.01m) * prixunitaire * qte;
                            decimal mntht = prixunitaire * qte;
                            decimal mntttc = prixunitaire * qte + mnttva;

                            OTOPFACTUREDETAIL fctdet = new OTOPFACTUREDETAIL();
                            fctdet.REFERENCEPRODUIT = det[5];// det[1];
                            fctdet.CODERAYON = det[4];// det[0];
                            fctdet.LIBRAYON = det[6];
                            fctdet.LIBELLEPRODUIT = det[0];
                            fctdet.QUANTITE = qte;
                            fctdet.PRIXUNITAIRE = prixunitaire;
                            fctdet.TAUXTVA = tauxtva;

                            fctdet.MNTTVA = -1 * mnttva;
                            fctdet.MNTHT = -1 * mntht;
                            fctdet.MNTTTC = -1 * mntttc;

                            demmntttc = demmntttc + mntttc;
                            demmntht = demmntht + mntht;
                            demmntttva = demmntttva + mnttva;
                            FACTDEMANDE.OTOPFACTUREDETAIL.Add(fctdet);
                        }
                    }

                    FACTDEMANDE.MNTHT = -1 * demmntht;
                    FACTDEMANDE.MNTTTC = -1 * demmntttc;
                    FACTDEMANDE.MNTTVA = -1 * demmntttva;

                    DB.OTOPFACTURE.Add(FACTDEMANDE);
                    DB.SaveChanges();
                }

                return FACTDEMANDE;
        }


        public OTOPFACTURE updateAvoir(int IDDEMANDE, string LIBELLEDEMANDE, int IdVendeur, int UTILISATEURCONNECTE, string datedeb, string datefin, string[][] details)
        {
            OTOPFACTURE FACTDEMANDE = new OTOPFACTURE();
            using (EramEntities DB = new EramEntities())
            {
                FACTDEMANDE = (from c in DB.OTOPFACTURE
                               where c.IDDEMANDE == IDDEMANDE
                               select c).First();



                FACTDEMANDE.DATEMODIFICATION = DateTime.Now;
                FACTDEMANDE.LIBELLEDEMANDE = (LIBELLEDEMANDE.Length > 50 ? LIBELLEDEMANDE.Substring(0, 50) : LIBELLEDEMANDE);
                FACTDEMANDE.DERNIERUTILISATEUR = UTILISATEURCONNECTE;

                List<OTOPFACTUREDETAIL> l1 = FACTDEMANDE.OTOPFACTUREDETAIL.ToList();
                foreach (OTOPFACTUREDETAIL det1 in l1)
                {
                    DB.Entry(det1).State = System.Data.EntityState.Deleted;
                }
                FACTDEMANDE.OTOPFACTUREDETAIL.Clear();

                decimal demmntttc = 0m;
                decimal demmntht = 0m;
                decimal demmntttva = 0m;
                if (details != null)
                {
                    foreach (string[] det in details)
                    {
                        bool retour = false;
                        for (int i = 0; i < 4; i++)
                        {
                            if (det[i] == null || det[i].Length <= 0)
                                retour = true;
                        }
                        if (retour)
                            continue;
                        decimal qte = decimal.Parse(det[3].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                        decimal prixunitaire = decimal.Parse(det[4].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                        decimal tauxtva = decimal.Parse(det[5].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                        decimal prixttc = prixunitaire + (tauxtva * 0.01m);
                        decimal mnttva = (tauxtva * 0.01m) * prixunitaire * qte;
                        decimal mntht = prixunitaire * qte;
                        decimal mntttc = prixunitaire * qte + mnttva;

                        OTOPFACTUREDETAIL fctdet = new OTOPFACTUREDETAIL();
                        fctdet.CODERAYON = det[0];
                        fctdet.REFERENCEPRODUIT = det[1];
                        fctdet.LIBELLEPRODUIT = det[2];
                        fctdet.QUANTITE = qte;
                        fctdet.PRIXUNITAIRE = prixunitaire;
                        fctdet.TAUXTVA = tauxtva;

                        fctdet.MNTTVA = -1 * mnttva;
                        fctdet.MNTHT = -1 * mntht;
                        fctdet.MNTTTC = -1 * mntttc;

                        demmntttc = demmntttc + mntttc;
                        demmntht = demmntht + mntht;
                        demmntttva = demmntttva + mnttva;
                        FACTDEMANDE.OTOPFACTUREDETAIL.Add(fctdet);
                    }
                }

                FACTDEMANDE.MNTHT = -1 * demmntht;
                FACTDEMANDE.MNTTTC = -1 * demmntttc;
                FACTDEMANDE.MNTTVA = -1 * demmntttva;

                DB.Entry(FACTDEMANDE).State = System.Data.EntityState.Modified;
                DB.SaveChanges();
            }

            return FACTDEMANDE;
        }

        public void validerOuRefuser(int id,short status,string commentaire,int iduser)
        {
            using (EramEntities DB = new EramEntities())
            {
                OTOPFACTURE OTOPFACTURE = new OTOPFACTURE();
                OTOPFACTURE = (from c in DB.OTOPFACTURE
                               where c.IDDEMANDE == id
                               select c).FirstOrDefault();
                OTOPFACTURE.COMMENTAIRE = commentaire;
                OTOPFACTURE.STATUS = status;
                //valider
                if (status == 1) {
                    OTOPFACTURE.DATEVALIDATION = DateTime.Now;
                } 
                else { 
                    //refuser
                    OTOPFACTURE.DATEREFUS = DateTime.Now;
                }
                DB.Entry(OTOPFACTURE).State = System.Data.EntityState.Modified;
        //        //valider
        //        if (status == 1) {
        //            FACTFACTURE facture = new FACTFACTURE();
        //            facture.CHAPITRE = OTOPFACTURE.CHAPITRE;
        //            facture.RAYON = OTOPFACTURE.RAYON;
        //            facture.RAYONACHETEUR = OTOPFACTURE.RAYONACHETEUR;
        //            facture.COMMENTAIRE = OTOPFACTURE.COMMENTAIRE;
        //            facture.ANNEEPRESTATION = OTOPFACTURE.ANNEEPRESTATION;
        //            facture.CNUFVENDEUR = OTOPFACTURE.CNUFVENDEUR;
        //            facture.CNUFACHETEUR = OTOPFACTURE.CNUFACHETEUR;
        //            facture.DATECREATION = DateTime.Now;
        //            facture.DATEMODIFICATION = DateTime.Now;
        //            facture.DERNIERUTILISATEUR = iduser;
        //            facture.IDCLIENT = OTOPFACTURE.IDCLIENT;
        //            facture.IDDEMANDE = OTOPFACTURE.IDDEMANDE;
        //            facture.IDFACTTYPE = OTOPFACTURE.IDFACTTYPE;
        //            facture.IDSITE = OTOPFACTURE.IDSITE;
        //            facture.LIBELLEDEMANDE = OTOPFACTURE.LIBELLEDEMANDE;
        //            facture.MNTHT = OTOPFACTURE.MNTHT;
        //            facture.MNTTTC = OTOPFACTURE.MNTTTC;
        //            facture.MNTTVA = OTOPFACTURE.MNTTVA;
        //            facture.REFERENCEFACT = OTOPFACTURE.REFERENCEFACT;
        //            facture.IDCLIENTINTERNE = OTOPFACTURE.IDCLIENTINTERNE;
        //            facture.TYPECLIENT = OTOPFACTURE.TYPECLIENT;
        //            facture.STATUS=0;
        //            foreach (OTOPFACTUREDETAIL det in OTOPFACTURE.OTOPFACTUREDETAIL)
        //            {
        //                FACTFACTUREDETAIL fdet = new FACTFACTUREDETAIL();
        //                fdet.DATECREATION=DateTime.Now;
        //                fdet.DATEMODIFICATION=DateTime.Now;
        //                fdet.DERNIERUTILISATEUR=iduser;
        //                fdet.LIBELLEPRODUIT=det.LIBELLEPRODUIT;
        //                fdet.MNTHT=det.MNTHT;
        //                fdet.MNTTTC=det.MNTTTC;
        //                fdet.MNTTVA = det.MNTTVA;
        //                fdet.PRIXUNITAIRE=det.PRIXUNITAIRE;
        //                fdet.QUANTITE=det.QUANTITE;
        //                fdet.REFERENCEPRODUIT=det.REFERENCEPRODUIT;
        //                fdet.TAUXTVA=det.TAUXTVA;
        //                facture.FACTFACTUREDETAIL.Add(fdet);
        //            }
        //            foreach (FACTPIECEJOINTE dpj in OTOPFACTURE.FACTPIECEJOINTE)
        //            {
        //                FACTFACTPIECEJOINTE pj=new FACTFACTPIECEJOINTE();
        //                pj.CONTENTTYPE=dpj.CONTENTTYPE;
        //                pj.NOM=dpj.NOM;
        //                pj.NOMINTERNE=dpj.NOMINTERNE;
        //                pj.DATECREATION=DateTime.Now;
        //                pj.DATEMODIFICATION=DateTime.Now;
        //                pj.DERNIERUTILISATEUR = dpj.DERNIERUTILISATEUR;
        //                facture.FACTFACTPIECEJOINTE.Add(pj);
        //            }
        //            DB.FACTFACTURE.Add(facture);
        //        }
                DB.SaveChanges();
            }
        }

        public void delete(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                OTOPFACTURE OTOPFACTURE = new OTOPFACTURE();
                OTOPFACTURE = (from c in DB.OTOPFACTURE
                               where c.IDDEMANDE == id
                               select c).FirstOrDefault();

                //OTOPFACTURE.FACTPIECEJOINTE.Clear();
                //on supprime la relation avec le detail demande
                List<OTOPFACTUREDETAIL> l1 = OTOPFACTURE.OTOPFACTUREDETAIL.ToList();
                foreach (OTOPFACTUREDETAIL det1 in l1)
                {
                    DB.Entry(det1).State = System.Data.EntityState.Deleted;
                }
                //OTOPFACTURE.OTOPFACTUREDETAIL.Clear();
                DB.Entry(OTOPFACTURE).State = System.Data.EntityState.Deleted;
                DB.SaveChanges();
            }
        }

        public List<TOTAUXTVA> calcultotauxtv(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder();
                requette.Append("select round(tauxtva,2) TAUXTVA,round(sum(mntht),2) MNTHT,round(sum(mnttva),2) MNTVA,round(sum(mntttc),2) MNTTC from OTOPFACTUREDETAIL ");
                requette.Append("where iddemande="+id);
                requette.Append("group by tauxtva ");
                requette.Append("order by tauxtva asc ");

                IEnumerable<TOTAUXTVA> result = DB.Database.SqlQuery<TOTAUXTVA>(requette.ToString());
                return result.AsQueryable().ToList();
            }
        }

        public List<TOTAUXTVA> calculhtbyrayon(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder();
                requette.Append("select to_number(coderayon) TAUXTVA,round(sum(mntht),2) MNTHT,round(sum(mnttva),2) MNTVA,round(sum(mntttc),2) MNTTC from OTOPFACTUREDETAIL ");
                requette.Append("where iddemande=" + id);
                requette.Append("group by coderayon ");
                requette.Append("order by coderayon asc ");

                IEnumerable<TOTAUXTVA> result = DB.Database.SqlQuery<TOTAUXTVA>(requette.ToString());
                return result.AsQueryable().ToList();
            }
        }


        public bool verifCnufFrs(String cnuf)
        {
            using (EramEntities DB = new EramEntities())
            {
                try { 
                    StringBuilder requette = new StringBuilder();
                    requette.Append("select to_char(count(*)) from asuheader" + agrDbLink);
                    requette.Append(" where apar_id='" + cnuf + "'");

                    IEnumerable<String> result = DB.Database.SqlQuery<String>(requette.ToString());

                    String retour = result.First();

                    if (result.First().Equals("0"))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }

        //public bool verifCnufFrs(String cnuf)
        //{
        //    StringBuilder requette = new StringBuilder();
        //    requette.Append("select count(*) from asuheader ");
        //    requette.Append(" where apar_id='" + cnuf + "'");
        //    int nbrcnuf = 0;

        //    using (OracleConnection conn = new OracleConnection(agressostring))
        //    {
        //        conn.Open();
        //        using (OracleCommand cmd = new OracleCommand(requette.ToString(), conn))
        //        {
        //            using (OracleDataReader rd = cmd.ExecuteReader())
        //            {
        //                if (rd.Read())
        //                    nbrcnuf = Convert.ToInt32(rd.GetValue(0));
        //                else
        //                {
        //                    return false;
        //                }
        //                if (nbrcnuf > 0)
        //                {
        //                    return true;
        //                }
        //            }
        //        }
        //    }
        //    return false;
        //}

        public bool verifCnufClient(String cnuf)
        {
            using (EramEntities DB = new EramEntities())
            {
              try{
                    StringBuilder requette = new StringBuilder();
                    requette.Append("select to_char(count(*)) from acuheader" + agrDbLink);
                    requette.Append(" where apar_id='" + cnuf + "'");

                    IEnumerable<String> result = DB.Database.SqlQuery<String>(requette.ToString());

                    String retour = result.First();

                    if (result.First().Equals("0"))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }
        
        //public bool verifCnufClient(String cnuf)
        //{
        //    StringBuilder requette = new StringBuilder();
        //    requette.Append("select count(*) from acuheader ");
        //    requette.Append(" where apar_id='" + cnuf + "'");
        //    int nbrcnuf = 0;

        //    using (OracleConnection conn = new OracleConnection(agressostring))
        //    {
        //        conn.Open();
        //        using (OracleCommand cmd = new OracleCommand(requette.ToString(), conn))
        //        {
        //            using (OracleDataReader rd = cmd.ExecuteReader())
        //            {
        //                if (rd.Read())
        //                    nbrcnuf = Convert.ToInt32(rd.GetValue(0));
        //                else
        //                {
        //                    return false;
        //                }
        //                if (nbrcnuf > 0)
        //                {
        //                    return true;
        //                }
        //            }
        //        }
        //    }
        //    return false;
        //}

        public long seqFile()
        {
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder();
                requette.Append("select FILE_SEQ.NEXTVAL from dual ");
                IEnumerable<long> result = DB.Database.SqlQuery<long>(requette.ToString());
                return result.AsQueryable().ToList().First();
            }
        }

        public DataSet GetDataSet(int iddemande)
        {
           using (EramEntities DB = new EramEntities())
            {
               StringBuilder requette = new StringBuilder();

                requette.Append("select e.referencefact nofacture,d.referenceproduit code,d.libelleproduit designation, ");
                requette.Append("d.quantite qte,d.prixunitaire prix,d.tauxtva txtva,d.mnttva mnttva,d.mntttc mntttc ");
                requette.Append("from OTOPFACTURE e,OTOPFACTUREDETAIL d ");
                requette.Append("where e.iddemande=");
                requette.Append(iddemande);
                requette.Append("and e.iddemande = d.iddemande ");
                string SQL = requette.ToString();
                //string ConnectionString = DB.Database.Connection.ConnectionString;
                OracleConnection conn = (OracleConnection)DB.Database.Connection;
                OracleDataAdapter da = new OracleDataAdapter();
                //SqlCommand cmd = conn.CreateCommand();
                OracleCommand cmd = new OracleCommand(SQL, conn);
                //cmd.CommandText = SQL;
                da.SelectCommand = cmd;
                DataSet ds = new DataSet();
                conn.Open();
                da.Fill(ds);
                ds.AcceptChanges();
                conn.Close();
                return ds;
           }
        }

        public CrystalReportfact2 ExportPdf(int iddemande, int idenseigne,string logo)
        {
            OTOPFACTURE dem = new OTOPFACTURE();
            dem = dem.getOne(iddemande);
            double buffer = Math.Round((dem.MNTTTC.HasValue ? (double)dem.MNTTTC.Value : 0.0), 2);
            //String mntEnLettres = NombreEnLettres.ToLettres((dem.MNTTTC.HasValue ? (double)dem.MNTTTC.Value : 0.0), Pays.France, Devise.Dirham);
            String mntEnLettres = FirstCharToUpper(NombreEnLettres.ToLettres(buffer, Pays.France, Devise.Dirham));
            
            CrystalReportfact2 crp = new CrystalReportfact2();
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder();
                string libenseigne = "";
                string libvendeur = "";
                string adrline1 = "";
                string adrline2 = "";
                string icevendeur = "";

                OTOPACHETEUR sitee = new OTOPACHETEUR();
                sitee = sitee.getOne(dem.IDACHETEUR.Value);
                libenseigne = sitee.FACTENSEIGNE.LIBELLEENSEIGNE;
                libvendeur = sitee.LIBELLESITE;
                adrline1 = sitee.ADRLINE1 + " " + sitee.ADRLINE2 + " " + sitee.ADRLINE3;
                adrline2 = sitee.ADRLINE3;
                icevendeur = sitee.IP;

                FACTENSEIGNE ens = new FACTENSEIGNE();
                ens=ens.getOne(idenseigne);
                
                requette.Append("select e.referencefact nofacture,e.libelledemande desigfact,d.referenceproduit code,d.libelleproduit designation, ");
                requette.Append(" d.quantite qte,d.prixunitaire prix,d.tauxtva txtva,d.mnttva mnttva,d.mntttc mntttc,d.mntht mntht,e.mntht tmntht,e.mnttva tmnttva,e.mntttc tmntttc, ");
                requette.Append(" :pline3 libclient,:pline1 adrline1,:pline2 adrline2,:pline4 ice, ");
                /*requette.Append(" '"+libvendeur+ "' libvendeur, ");
                requette.Append(" '"+ adrline1 + "' adrvendeur1, ");
                requette.Append(" '"+ adrline2 + "' adrvendeur2, ");
                requette.Append(" '"+ icevendeur + "' icevendeur, ");
                requette.Append(" '" + libenseigne + "' ensclient, ");
                requette.Append("'"+logoPath+"'||ens.logo logo,");
                requette.Append("'" + mntEnLettres + "' mntlettres, ");*/

                requette.Append(" :libvendeur libvendeur, ");
                requette.Append(" :adrline1 adrvendeur1, ");
                requette.Append(" :adrline2 adrvendeur2, ");
                requette.Append(" :icevendeur icevendeur, ");
                requette.Append(" :libenseigne ensclient, ");
                requette.Append(" :logoPath || :logo logo,");
                requette.Append(" :mntEnLettres mntlettres, ");


                requette.Append("  to_char(nvl(e.datevalidation,e.datecreation),'DD/MM/RRRR') dfacture, ");
                requette.Append("  d.coderayon ray ");

                requette.Append(" from OTOPFACTURE e,OTOPFACTUREDETAIL d ");
                requette.Append("where e.iddemande=");requette.Append(iddemande);
                requette.Append(" and e.iddemande = d.iddemande ");
                requette.Append(" order by d.coderayon,d.librayon ");

                string SQL = requette.ToString();
                //logger.Info("demande="+SQL);
                OracleConnection conn = (OracleConnection)DB.Database.Connection;
                OracleDataAdapter da = new OracleDataAdapter();
                OracleCommand cmd = new OracleCommand(SQL, conn);
                //requette.Append(" ens.pline3 libclient,ens.pline1 adrline1,ens.pline2 adrline2,ens.pline4 ice, ");
                cmd.Parameters.Add(new OracleParameter("pline3",ens.PLINE3));
                cmd.Parameters.Add(new OracleParameter("pline1", ens.PLINE1));
                cmd.Parameters.Add(new OracleParameter("pline2", ens.PLINE2));
                cmd.Parameters.Add(new OracleParameter("pline4", ens.PLINE4));

                cmd.Parameters.Add(new OracleParameter("libvendeur", libvendeur));
                cmd.Parameters.Add(new OracleParameter("adrline1", adrline1));
                cmd.Parameters.Add(new OracleParameter("adrline2", adrline2));
                cmd.Parameters.Add(new OracleParameter("icevendeur", icevendeur));
                cmd.Parameters.Add(new OracleParameter("libenseigne", libenseigne));
                cmd.Parameters.Add(new OracleParameter("logoPath", logoPath));
                cmd.Parameters.Add(new OracleParameter("logo", logo)); //en.LOGO
                cmd.Parameters.Add(new OracleParameter("mntEnLettres", mntEnLettres));

                da.SelectCommand = cmd;
                DataSetfact dsfact = new DataSetfact();
                conn.Open();
                da.Fill(dsfact.facture);
                dsfact.AcceptChanges();
                crp.SetDataSource(dsfact);

                StringBuilder requette1 = new StringBuilder();
                requette1.Append("select 'TVA à '||tauxtva||'%' designation,round(tauxtva,2) txtva,round(sum(mntht),2) ht,round(sum(mnttva),2) taxe,round(sum(mntttc),2) ttc from OTOPFACTUREDETAIL ");
                requette1.Append("where iddemande=" + iddemande);
                requette1.Append("group by tauxtva ");
                requette1.Append("order by tauxtva asc ");
                //da = new OracleDataAdapter();
                cmd = new OracleCommand(requette1.ToString(), conn);
                da.SelectCommand = cmd;
                DataSetTva dstva = new DataSetTva();
                da.Fill(dstva.totauxtva);
                dstva.AcceptChanges();

                StringBuilder requette2 = new StringBuilder();
                requette2.Append("select ");
                requette2.Append(" e.mntht tmntht,e.mnttva tmnttva,e.mntttc tmntttc, ");
                requette2.Append(" :mntEnLettres mntlettres ");
                requette2.Append(" from OTOPFACTURE e ");
                requette2.Append("where e.iddemande=");
                requette2.Append(iddemande);
                cmd = new OracleCommand(requette2.ToString(), conn);
                cmd.Parameters.Add(new OracleParameter("mntEnLettres", mntEnLettres));
                da.SelectCommand = cmd;
                da.Fill(dstva.unefacture);
                dstva.AcceptChanges();
                crp.Subreports[0].SetDataSource(dstva);
               
                conn.Close();
            }
            return crp;
        }

        public List<FACTCLIENT> getAllClients()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTCLIENT
                             select c;
                return result.ToList();
            }
        }
        public void imprimer(int from, int id, int iduser)
        {
            using (EramEntities DB = new EramEntities())
            {
                OTOPFACTURE demande = new OTOPFACTURE();
                demande = demande.getOne(id);
                if (!demande.DATEIMPRESSION.HasValue)
                {
                    demande.DATEIMPRESSION = DateTime.Now;
                    demande.DERNIERUTILISATEUR = iduser;
                    demande.USERIMPRIM = iduser;
                    DB.Entry(demande).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();
                }
            }
        }


        public static List<OTOPACHETEUR> getAllAcheteur()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.OTOPACHETEUR
                             select c;
                return result.Include("FACTENSEIGNE").ToList();
            }
        }

        public static List<OTOPVENDEUR> getAllVendeur()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.OTOPVENDEUR
                             select c;
                return result.Include("FACTENSEIGNE").ToList();
            }
        }

        public static OTOPACHETEUR getAcheteurBySite(int sitegold)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.OTOPACHETEUR
                             where c.CODEGOLD == sitegold
                             select c;
                return result.Include("FACTENSEIGNE").FirstOrDefault();
            }
        }

        public static string getLinesfacturesFromGOLD(int site, string datedeb, string datefin)
        {
            StringBuilder requette = new StringBuilder();
            requette.Append(" select libart,ean,scode,sum(qte),prixvente,ctva,cext_ray,lib_ray ");
            requette.Append(" from (");
            
            requette.Append(  getLinesreceptionsFromGOLD(site,datedeb,datefin) );
            requette.Append(" UNION ALL ");
            requette.Append(  getLinesTransfertsFromGOLD(site, datedeb, datefin) );

            requette.Append(" ) ");
            requette.Append(" group by libart,ean,scode,prixvente,ctva,cext_ray,lib_ray ");
            requette.Append(" order by cext_ray,scode ");

            logger.Info("principale requette=" + requette.ToString());

            return requette.ToString();
        }

        public static string getLinesreceptionsFromGOLD(int site,string datedeb,string datefin) {
            StringBuilder requette = new StringBuilder();
            requette.Append(" select ");
            requette.Append(" replace( ");
            requette.Append(" replace( ");
            requette.Append(" replace( ");
            requette.Append(" replace( ");
            requette.Append(" replace( ");
            requette.Append("pkstrucobj.get_desc(d.SDRCINR,'FR')");
            requette.Append(", '''',' ' ) ");
            requette.Append(", '&',' ' ) ");
            requette.Append(", '/',' ' ) ");
            requette.Append(", '\',' ' ) ");
            requette.Append(", ',',' ' ) ");
            requette.Append("  libart,  ");
            requette.Append(" pkartcoca.get_closestEAN(d.SDRCINLS) ean,d.SDRCODE scode,d.SDRQTEA qte, ");
            //requette.Append(" nvl(round(mj_pkmarjane.GET_PRIX_VENTE_HT(d.SDRCINLS,r.sersite,pkresrel.get_resid_prx_vte_a_date(r.sersite,sysdate),sysdate),2),0) ");
            
            /******* prix de vente ****/
            requette.Append(" nvl(round( ");
            requette.Append(" (select pv.aviprix / ");
            requette.Append(" (1+ pktvas.getTauxTVA(pv.avictva, 3, TO_CHAR(r.serdrec, 'DD/MM/RR'))*0.01)  ");
            requette.Append(" from AVEPRIX pv where  pv.avintar=" + getTarif(site) + " and pv.avicinv=d.SDRCINLS and trunc(r.serdrec) between trunc(pv.aviddeb) and trunc(pv.avidfin) and rownum=1) ");
            requette.Append(" ,2),               ");
            requette.Append("      0              ");
            //requette.Append(" nvl(round(mj_pkmarjane.GET_PRIX_VENTE_HT(d.SDRCINLS,r.sersite,pkresrel.get_resid_prx_vte_a_date(r.sersite,sysdate),sysdate),2),0) ");
            requette.Append("       ) prixvente, ");
            /******* Prix de vente ****/

            requette.Append(" d.sdrttva ctva, ");
            requette.Append(" a.cext_ray, ");
            requette.Append(" pkstrucobj.get_desc(a.cint_ray,'FR') lib_ray ");

            requette.Append(conditionsreceptionsFromGOLD(site, datedeb, datefin));

            return requette.ToString();
        }

        public static string getLinesTransfertsFromGOLD(int site, string datedeb, string datefin)
        {
            StringBuilder requette = new StringBuilder();
            requette.Append(" select  ");
            requette.Append("              replace(   ");
            requette.Append("              replace(   ");
            requette.Append("              replace(   ");
            requette.Append("              replace(   ");
            requette.Append("              replace(   ");
            requette.Append("             pkstrucobj.get_desc(a.mascinr,'FR')  ");
            requette.Append("             , '''',' ' )   ");
            requette.Append("             , '&',' ' )   ");
            requette.Append("             , '/',' ' )   ");
            requette.Append("             , '\',' ' )   ");
            requette.Append("             , ',',' ' )   ");
            requette.Append("              libart,  ");
            requette.Append("             pkartcoca.get_closestEAN(s.stmcinl) ean,pkartcoca.get_closestEAN(s.stmcinl) scode,s.stmval qte,   ");
            /******* Debut prix de vente ****/
            requette.Append("              nvl(round(   ");
            requette.Append("              (select pv.aviprix /   ");
            requette.Append("              (1+ pktvas.getTauxTVA(pv.avictva, 3, TO_CHAR(STMDMVT, 'DD/MM/RR'))*0.01)    ");
            requette.Append("              from AVEPRIX pv where  pv.avintar=" + getTarif(site) + " and pv.avicinv=s.stmcinl and trunc(STMDMVT) between trunc(pv.aviddeb) and trunc(pv.avidfin) and rownum=1)   ");
            requette.Append("              ,2),                 ");
            requette.Append("                   0                ");
            requette.Append("                    ) prixvente,   ");
            /******* Fin prix de vente ****/
            requette.Append("              pktvas.getTauxTVA(pkprixvente.get_code_tva(s.stmcinl, s.stmsite, 1, STMDMVT),3,trunc(STMDMVT)) ctva,  ");

            //requette.Append("              (  ");
            //requette.Append("              select pv.avictva   ");
            //requette.Append("              from AVEPRIX pv where  pv.avintar=s.stmsite||1 and pv.avicinv=s.stmcinl and rownum=1  ");
            //requette.Append("              ) ctva2,  ");
            requette.Append("              a.cext_ray,   ");
            requette.Append("              pkstrucobj.get_desc(a.cint_ray,'FR') lib_ray   ");
            requette.Append(conditionstransfertsFromGOLD(site, datedeb, datefin));

            return requette.ToString();
        }

        public static string getCountLinesreceptionsFromGOLD(int site, string datedeb, string datefin)
        {
            StringBuilder requette = new StringBuilder();
            requette.Append("select  count(*) ");
            requette.Append(conditionsreceptionsFromGOLD(site, datedeb, datefin));

            return requette.ToString();
        }

        public static string getCountLinesfacturesFromGOLD(int site, string datedeb, string datefin)
        {
            StringBuilder requette = new StringBuilder();
            requette.Append(" select count(*) ");
            requette.Append(" from (");
            requette.Append(" select pkartcoca.get_closestEAN(d.SDRCINLS) ean ");
            requette.Append(conditionsreceptionsFromGOLD(site, datedeb, datefin));
            requette.Append(" UNION ");
            requette.Append(" select pkartcoca.get_closestEAN(s.stmcinl) ean  ");
            requette.Append(conditionstransfertsFromGOLD(site, datedeb, datefin));
            requette.Append(" ) ");

            logger.Info("count requette="+requette.ToString());

            return requette.ToString();
        }

        public static string getLinesfacturesAvFromGOLD(int site, string datedeb, string datefin)
        {
            StringBuilder requette = new StringBuilder();
            requette.Append(getLinesRuturnsFromGOLD(site, datedeb, datefin));
            requette.Append(" order by cext_ray,scode ");

            logger.Info("Requette avoir=" + requette.ToString());

            return requette.ToString();
        }

        public static string getLinesRuturnsFromGOLD(int site, string datedeb, string datefin)
        {
            StringBuilder requette = new StringBuilder();
            requette.Append(" select  ");
            requette.Append("              replace(   ");
            requette.Append("              replace(   ");
            requette.Append("              replace(   ");
            requette.Append("              replace(   ");
            requette.Append("              replace(   ");
            requette.Append("             pkstrucobj.get_desc(a.mascinr,'FR')  ");
            requette.Append("             , '''',' ' )   ");
            requette.Append("             , '&',' ' )   ");
            requette.Append("             , '/',' ' )   ");
            requette.Append("             , '\',' ' )   ");
            requette.Append("             , ',',' ' )   ");
            requette.Append("              libart,  ");
            requette.Append("             pkartcoca.get_closestEAN(s.stmcinl) ean,pkartcoca.get_closestEAN(s.stmcinl) scode,s.stmval qte,   ");
            /******* Debut prix de vente ****/
            requette.Append("              nvl(round(   ");
            requette.Append("              (select pv.aviprix /   ");
            requette.Append("              (1+ pktvas.getTauxTVA(pv.avictva, 3, TO_CHAR(STMDMVT, 'DD/MM/RR'))*0.01)    ");
            requette.Append("              from AVEPRIX pv where  pv.avintar="+getTarif(site)+" and pv.avicinv=s.stmcinl and trunc(STMDMVT) between trunc(pv.aviddeb) and trunc(pv.avidfin) and rownum=1)   ");
            requette.Append("              ,2),                 ");
            requette.Append("                   0                ");
            requette.Append("                    ) prixvente,   ");
            /******* Fin prix de vente ****/
            requette.Append("              pktvas.getTauxTVA(pkprixvente.get_code_tva(s.stmcinl, s.stmsite, 1, STMDMVT),3,trunc(STMDMVT)) ctva,  ");
            //requette.Append("              (  ");
            //requette.Append("              select pv.avictva   ");
            //requette.Append("              from AVEPRIX pv where  pv.avintar=s.stmsite||1 and pv.avicinv=s.stmcinl and rownum=1  ");
            //requette.Append("              ) ctva2,  ");
            requette.Append("              a.cext_ray,   ");
            requette.Append("              pkstrucobj.get_desc(a.cint_ray,'FR') lib_ray   ");
            requette.Append(conditionsreturnsFromGOLD(site, datedeb, datefin));

            return requette.ToString();
        }

        private static string getTarif(int site){
            string[] authorsList = SITESSPECTARIF.Split(',');

            foreach (string sitespec in authorsList) { 
                int sitessp=0;
                bool isnumber = Int32.TryParse(sitespec.Trim(), out sitessp);
                if (isnumber && sitessp == site)
                    return  TARIFOTOPTOTAL;
            }

            return TARIFOTOPGLO;
        }


        public static string getCountLinesfacturesAvFromGOLD(int site, string datedeb, string datefin)
        {
            StringBuilder requette = new StringBuilder();
            requette.Append(" select count(pkartcoca.get_closestEAN(s.stmcinl))  ");
            requette.Append(conditionsreturnsFromGOLD(site, datedeb, datefin));
            logger.Info("count requette=" + requette.ToString());

            return requette.ToString();
        }

        public static string conditionsreturnsFromGOLD(int site, string datedeb, string datefin)
        {
            StringBuilder requette = new StringBuilder();
            requette.Append("  from stomvt s, mj_art_struct a  ");
            requette.Append(" where s.stmtmvt=100  "); //26 //26 //26
            requette.Append(" and s.stmmotf=21  "); 
            requette.Append(" and s.stmsite=" + site);
            requette.Append(" and a.mascinr=pkartul.getCinr(s.stmcinl)  ");
            requette.Append(" and trunc(STMDMVT) between trunc(to_date(:datedeb,'DD/MM/RRRR')) and trunc(to_date(:datefin,'DD/MM/RRRR')) ");

            return requette.ToString();
        }


        public static string conditionstransfertsFromGOLD(int site, string datedeb, string datefin)
        {
            StringBuilder requette = new StringBuilder();
            requette.Append("  from stomvt s, mj_art_struct a  ");
            requette.Append(" where s.stmtmvt=50  ");
            requette.Append(" and s.stmsite=" + site);
            requette.Append(" and a.mascinr=pkartul.getCinr(s.stmcinl)  ");
            requette.Append(" and trunc(STMDMVT) between trunc(to_date(:datedeb,'DD/MM/RRRR')) and trunc(to_date(:datefin,'DD/MM/RRRR')) ");

            return requette.ToString();
        }

        public static string conditionsreceptionsFromGOLD(int site, string datedeb, string datefin)
        {
            StringBuilder requette = new StringBuilder();
            requette.Append(" from stoentre r,stodetre d,mj_art_struct a ");
            requette.Append(" where r.sercinrec = d.sdrcinrec ");
            requette.Append(" and r.sersite=" + site);
            requette.Append(" and a.mascinr=d.sdrcinr ");
            requette.Append(" and trunc(r.serdrec) between trunc(to_date(:datedeb,'DD/MM/RRRR')) and trunc(to_date(:datefin,'DD/MM/RRRR')) ");
            requette.Append(" and r.sertmvt=1 ");
           // requette.Append(" order by a.cext_ray, d.SDRCODE ");
            return requette.ToString();
        }

        public static string FirstCharToUpper(string input)
        {
            if (String.IsNullOrEmpty(input))
                return input;
            return input.First().ToString().ToUpper() + input.Substring(1);
        }

        public void majRef(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                OTOPFACTURE demande = new OTOPFACTURE();
                demande = demande.getOne(id);
                if (demande.FLAGAVOIR.HasValue && demande.FLAGAVOIR.Value == 1)
                    demande.REFERENCEFACT = returnNextValSeqAvoirReference();
                else
                    demande.REFERENCEFACT = returnNextValSeqFactReference();

                DB.Entry(demande).State = System.Data.EntityState.Modified;
                DB.SaveChanges();
            }
        }



        public static long returnNextValSeq(string sequence)
        {
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette=new StringBuilder("select ");
                requette.Append(sequence);
                requette.Append(".NEXTVAL from dual ");

                List<long> result2 = DB.Database.SqlQuery<long>(requette.ToString()).ToList();

                return result2[0];
            }
        }

        public static string returnNextValSeqFactReference()
        {
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder("select ");
                requette.Append("OTOPFACTURE_VRAI_SEQ");
                requette.Append(".NEXTVAL||'/'||to_char(sysdate,'RRRR') from dual ");

                List<string> result2 = DB.Database.SqlQuery<string>(requette.ToString()).ToList();

                return result2[0];
            }
        }

        public static string returnNextValSeqAvoirReference()
        {
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder("select ");
                requette.Append("otopfacture_AV_VRAI_SEQ");
                requette.Append(".NEXTVAL||'/'||to_char(sysdate,'RRRR') from dual ");

                List<string> result2 = DB.Database.SqlQuery<string>(requette.ToString()).ToList();

                return result2[0];
            }
        }

        public long getSequence(String seqname)
        {
            long seq = 0;
            using (EramEntities DB = new EramEntities())
            {
                using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
                {
                    conn.Open();
                    using (OracleCommand cmd = new OracleCommand("SELECT " + seqname + ".Nextval FROM dual ", conn))
                    {
                        using (OracleDataReader rd = cmd.ExecuteReader())
                        {
                            if (rd.Read())
                                seq = Convert.ToInt64(rd.GetValue(0));
                            else
                            {
                                conn.Close();
                                conn.Dispose();
                                return 0;
                            }
                        }
                    }
                    conn.Close();
                    conn.Dispose();
                }
            }
            return seq;
        }

        public List<OTOPFACTURE> facturesAGenerer()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.OTOPFACTURE
                             where c.DATEIMPRESSION != null && c.STATUS == 0
                             select c;
                return result.ToList();
            }
        }

        public void deleteAcrtrans(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.OTOPACRTRANS
                             where c.IDFACTURE == id
                             select c;
                List<OTOPACRTRANS> listeacrtrans = result.ToList();
                foreach (OTOPACRTRANS acr in listeacrtrans)
                {
                    DB.Entry(acr).State = System.Data.EntityState.Deleted;
                }
                DB.SaveChanges();
            }
        }

        public void acomptabiliser(int id, int user)
        {
            using (EramEntities DB = new EramEntities())
            {
                OTOPFACTURE facture = new OTOPFACTURE();
                facture = (from c in DB.OTOPFACTURE
                           where c.IDDEMANDE == id
                           select c).First();
                facture.STATUS = 2;
                facture.DATEMODIFICATION = DateTime.Now;
                facture.DERNIERUTILISATEUR = user;
                facture.USERGENERE = user;
                facture.DATEGENERATION = DateTime.Now;

                DB.Entry(facture).State = System.Data.EntityState.Modified;
                DB.SaveChanges();
            }
        }

        public void reporter(int id, int user)
        {
            using (EramEntities DB = new EramEntities())
            {
                OTOPFACTURE FACTFACTURE = new OTOPFACTURE();
                FACTFACTURE = (from c in DB.OTOPFACTURE
                               where c.IDDEMANDE == id
                               select c).FirstOrDefault();
                if ((FACTFACTURE.STATUS == 0 && FACTFACTURE.DATEIMPRESSION.HasValue) || FACTFACTURE.STATUS == 2)
                {
                    if (FACTFACTURE.STATUS == 2)
                    {
                        deleteAcrtrans(id);
                    }
                    //on supprime la relation les pieces jointes
                    FACTFACTURE.STATUS = 6;
                    FACTFACTURE.USERREPORT = user;
                    DB.Entry(FACTFACTURE).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();
                }
                else
                {
                    if (FACTFACTURE.STATUS == 6)
                    {
                        FACTFACTURE.STATUS = 0;
                        FACTFACTURE.DERNIERUTILISATEUR = user;
                        FACTFACTURE.DATEMODIFICATION = DateTime.Now;

                        DB.Entry(FACTFACTURE).State = System.Data.EntityState.Modified;
                        DB.SaveChanges();
                    }
                }
            }
        }

        public bool insert_temp(AGR bc, String temp)
        {
            String req = Utils.getRequeteInsertAcrtrans(bc, temp);
            logger.Error("insert_temp req =" + req);
            using (EramEntities DB = new EramEntities())
            {
                using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
                {
                    conn.Open();
                    using (OracleCommand cmd = new OracleCommand(req, conn))
                    {
                        try
                        {
                            cmd.Parameters.Add(new OracleParameter("description", bc.DESCRIPTION));
                            cmd.Parameters.Add(new OracleParameter("reference", bc.EXT_INV_REF));
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            logger.Error("Error =" + ex.ToString());
                            return false;
                        }
                    }
                    conn.Close();
                    conn.Dispose();
                }
            }
            return true;
        }

        public ICollection<INTEGBROUILLARD> consulterBrouillard(long NOLOT, int iduser)
        {
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder();
                requette.Append("select distinct NOLOT NO_LOT, ");
                requette.Append("m.voucher_no CROID, ");
                requette.Append("to_char(m.voucher_no) SCROID, ");
                requette.Append("m.Idfacture IDFACTURE, ");
                requette.Append("(select count(*) from OTOPACRTRANS b where b.voucher_no=m.voucher_no and b.client=m.client and b.IDFACTURE=m.IDFACTURE) NBRE_SEQUENCE, ");
                requette.Append("m.EXT_INV_REF REF_FACT, ");
                requette.Append("m.client CLIENT, ");
                requette.Append("m.Fiscal_Year ANNEE_FISCALE, ");
                requette.Append("m.PERIOD PERIOD, ");
                requette.Append("m.apar_id CNUF, ");
                requette.Append("trunc(m.VOUCHER_DATE) DATE_COMPTABILITE, ");
                requette.Append("trunc(m.TRANS_DATE) DATE_FACTURE, ");
                requette.Append("(select sum(decode(m1.dc_flag,-1,m1.amount,0)) from OTOPACRTRANS m1 where m1.voucher_no=m.voucher_no and m1.client=m.client and m1.IDFACTURE=m.IDFACTURE) MNT_NEG, ");
                requette.Append("(select sum(decode(m1.dc_flag,1,m1.amount,0)) from OTOPACRTRANS m1 where m1.voucher_no=m.voucher_no and m1.client=m.client and m1.IDFACTURE=m.IDFACTURE) MNT_POS, ");
                requette.Append("(select sum(m1.amount) from OTOPACRTRANS m1 where m1.voucher_no=m.voucher_no and m1.client=m.client and m1.IDFACTURE=m.IDFACTURE ) EQUILIBRE ");
                requette.Append("from OTOPACRTRANS m,otopfacture f "); /* ,factassouserfacttype uf */
                requette.Append(" where 1 = 1 ");
                requette.Append(" and m.IDFACTURE = f.iddemande ");
                requette.Append(" and f.status in (2,4) ");
                /*requette.Append(" and uf.idfacttype=f.idfacttype ");
                requette.Append(" and uf.idutilisateur=" + iduser);*/
                requette.Append(" order by m.IDFACTURE,m.voucher_no  ");

                IEnumerable<INTEGBROUILLARD> result2 = DB.Database.SqlQuery<INTEGBROUILLARD>(requette.ToString());

                return result2.ToList();
            }

        }

        public List<INTEGBROUILLARDDET> chercherDetailsBrouillard(decimal croId, string client, decimal idfacture)
        {
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder();
                requette.Append(" SELECT m.sequence_no SEQUENCE, ");
                requette.Append(" m.account COMPTE, ");
                requette.Append(" m.amount MONTANT, ");
                requette.Append(" m.DIM_1 DIM1, ");
                requette.Append(" m.DIM_2 DIM2, ");
                requette.Append(" m.DIM_4 DIM4, ");
                requette.Append(" m.description DESCRIPTION ");
                requette.Append(" FROM OTOPACRTRANS m ");
                requette.Append(" WHERE m.voucher_no = " + croId);
                requette.Append(" and m.IDFACTURE = " + idfacture);
                requette.Append(" and m.client = '" + client + "' ");
                requette.Append("order by m.voucher_no,m.sequence_no asc ");

                IEnumerable<INTEGBROUILLARDDET> result2 = DB.Database.SqlQuery<INTEGBROUILLARDDET>(requette.ToString());
                IQueryable<INTEGBROUILLARDDET> result = result2.AsQueryable();


                return result.ToList();
            }
        }

        public List<OTOPFACTURE> facturesAIntegrer(int iduser)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.OTOPFACTURE
                             where c.STATUS == 2
                             select c;
                return result.ToList();
            }
        }

        public void updateAcrtransLastUpdate(int idfacture)
        {
            using (EramEntities DB = new EramEntities())
            {
                List<OTOPACRTRANS> listAcrtrans = new List<OTOPACRTRANS>();
                listAcrtrans = (from c in DB.OTOPACRTRANS
                                where c.IDFACTURE == idfacture
                                select c).ToList();
                foreach (OTOPACRTRANS acrtrans in listAcrtrans)
                {
                    acrtrans.LAST_UPDATE = DateTime.Now;
                    acrtrans.DUE_DATE = DateTime.Now;
                    DB.Entry(acrtrans).State = System.Data.EntityState.Modified;
                }
                DB.SaveChanges();
            }
        }

        public void comptabiliser(int id, int user)
        {
            using (EramEntities DB = new EramEntities())
            {
                OTOPFACTURE facture = new OTOPFACTURE();
                facture = (from c in DB.OTOPFACTURE
                           where c.IDDEMANDE == id
                           select c).First();
                facture.STATUS = 3;
                facture.DATECOMPTABILISATION = DateTime.Now;
                facture.DATEMODIFICATION = DateTime.Now;
                facture.DERNIERUTILISATEUR = user;
                facture.USERINTEGAGR = user;

                DB.Entry(facture).State = System.Data.EntityState.Modified;
                DB.SaveChanges();
            }
        }

    }
}
