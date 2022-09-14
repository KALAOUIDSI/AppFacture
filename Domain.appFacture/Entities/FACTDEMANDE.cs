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
using CrystalDecisions.CrystalReports.Engine;
using Domain.appFacture.Datasets;
using log4net;


namespace Domain.appFacture
{
    public partial class FACTDEMANDE
    {
        public static string agrDbLink = ConfigurationManager.AppSettings["AGRDBLINK"];
        public static string agressostring = ConfigurationManager.ConnectionStrings["Agrtest"].ConnectionString;
        public static string logoPath = ConfigurationManager.AppSettings["LOGOPATH"];
        public static string tauxtsc = ConfigurationManager.AppSettings["TAUXTSC"];
        public static ILog logger = log4net.LogManager.GetLogger("KassagrWEBLogger");


        public List<TOTAUXTVA> TVATOTAUX = new List<TOTAUXTVA>();
        public List<TOTAUXTVA> TVATOTAUXIPPRF = new List<TOTAUXTVA>();
        void OnCreated()
        {
        }

        public string IDCAT
        {
            get
            {
                return (this.FACTTYPE != null ? this.FACTTYPE.IDCAT : "-1,-1");
            }
        }

        public int IDSITE2
        {
            get
            {
                return -1;
            }
        }

        public decimal? MNTTVAREV
        {
            get
            {
                if (this.FACTTYPE != null && this.FACTTYPE.IDCATEGORIE == 5)
                    return this.TAUXTVA;
                else
                    return this.MNTTVA;
            }
        }

        public List<FACTDEMANDE> getAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTDEMANDE
                             select c;
                return result.Include("FACTDEMANDEDETAIL").Include("FACTDEMANDESTATUS").Include("FACTCLIENT").Include("FACTSITE1").Include("FACTTYPE.FACTTYPECATEGORIE").Include("FACTSITE").Include("FACTPIECEJOINTE").ToList();
            }
        }

        public List<FACTDEMANDE> getAll(int idSite)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTDEMANDE
                             where c.IDSITE == idSite
                             select c;
                return result.Include("FACTDEMANDEDETAIL").Include("FACTDEMANDESTATUS").Include("FACTCLIENT").Include("FACTSITE1").Include("FACTTYPE.FACTTYPECATEGORIE").Include("FACTSITE").Include("FACTPIECEJOINTE").ToList();
            }
        }
        public int countAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTDEMANDE
                             select c;
                return result.Count();
            }
        }

        public int countAll(int idsite)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTDEMANDE
                             where (c.IDSITE==idsite || idsite==-1)
                             select c;
                return result.Count();
            }
        }

        public int countAll(int idsite, int idUser)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTDEMANDE
                             where (c.IDSITE == idsite || idsite == -1) && c.FACTTYPE.FACTUSER.Any(u => u.IDUTILISATEUR == idUser)
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

        public List<FACTDEMANDEDETAIL> getAllDetails(int iddemande)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTDEMANDEDETAIL
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

        public DEMANDEPaginationRes getAll(int page, int pageSize, string search, int sortby, Boolean isasc, DEMANDEAdvFiltre AdvFiltre )
        {
            using (EramEntities DB = new EramEntities())
            {
                DEMANDEPaginationRes res = new DEMANDEPaginationRes();

                var result = (from c in DB.FACTDEMANDE
                              where (c.IDSITE == AdvFiltre.site || AdvFiltre.site == -1) && c.FACTTYPE.FACTUSER.Any(u => u.IDUTILISATEUR == AdvFiltre.user)
                              select c).Include("FACTDEMANDEDETAIL").Include("FACTDEMANDESTATUS").Include("FACTCLIENT").Include("FACTSITE1").Include("FACTTYPE.FACTTYPECATEGORIE").Include("FACTSITE").Include("FACTPIECEJOINTE");

                //recherche normale
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    int searchid;
                    if (int.TryParse(search, out searchid))
                        result = from c in result where c.REFERENCEFACT.ToLower().Contains(search.Trim()) || c.LIBELLEDEMANDE.ToLower().Contains(search.Trim()) || c.CHAPITRE.ToLower().Contains(search.Trim()) || c.FACTTYPE.LIBELLE.ToLower().Contains(search.Trim()) || c.FACTCLIENT.DESIGNATIONCLIENT.ToLower().Contains(search.Trim()) select c;
                    else
                        result = from c in result where c.REFERENCEFACT.ToLower().Contains(search.Trim()) || c.LIBELLEDEMANDE.ToLower().Contains(search.Trim()) || c.CHAPITRE.ToLower().Contains(search.Trim()) || c.FACTTYPE.LIBELLE.ToLower().Contains(search.Trim()) || c.FACTCLIENT.DESIGNATIONCLIENT.ToLower().Contains(search.Trim()) select c;
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
                    result = result.Where(c => c.FACTCLIENT.DESIGNATIONCLIENT.ToLower().Contains(AdvFiltre.client.ToLower()));


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
                        result = isasc ? result.OrderBy(c => c.FACTCLIENT.DESIGNATIONCLIENT) : result.OrderByDescending(c => c.FACTCLIENT.DESIGNATIONCLIENT);
                        break;
                    case 6:
                        result = isasc ? result.OrderBy(c => c.MNTHT) : result.OrderByDescending(c => c.MNTHT);
                        break;
                    case 7:
                        result = isasc ? result.OrderBy(c => c.MNTTTC) : result.OrderByDescending(c => c.MNTTTC);
                        break;
                    default: // Not: case "Default"
                        result = result.OrderByDescending(c => c.IDDEMANDE);
                        break;
                }

                res.count = result.Count();
                res.listDEMANDE = result
                .Skip((page - 1) * pageSize)
                .Take(pageSize).ToList();

                return res;
            }
        }

        public FACTDEMANDE getOne(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.FACTDEMANDE
                              where c.IDDEMANDE == id
                              select c).Include("FACTDEMANDEDETAIL").Include("FACTDEMANDESTATUS").Include("FACTCLIENT").Include("FACTSITE1").Include("FACTTYPE.FACTTYPECATEGORIE").Include("FACTSITE").Include("FACTPIECEJOINTE").First();
                return result;

            }
        }
        public FACTDEMANDE getOne(string libelle)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.FACTDEMANDE
                              where c.LIBELLEDEMANDE.ToLower().Contains(libelle.ToLower())
                              select c).Include("FACTDEMANDEDETAIL").Include("FACTDEMANDESTATUS").Include("FACTCLIENT").Include("FACTSITE1").Include("FACTTYPE.FACTTYPECATEGORIE").Include("FACTSITE").Include("FACTPIECEJOINTE").FirstOrDefault();
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

        public FACTDEMANDE updateMutiSites(int IDDEMANDE, string LIBELLEDEMANDE, int IDFACTTYPE, string CHAPITRE, int? RAYON, string CNUFVENDEUR, int TYPECLIENT, int IDCLIENT, int IDCLIENTINTERNE, String CNUFACHETEUR, int? RAYONACHETEUR, string ANNEEPRESTATION, string MNTTVAREV, string MNTTVAIPPRF, int UTILISATEURCONNECTE, int IdSiteConnecte, string[][] details, string filename, string filenameinterne, string contenttype)
        {

            FACTDEMANDE FACTDEMANDE = new FACTDEMANDE();

            if (IDDEMANDE != 0)
            {
                //modification
                using (EramEntities DB = new EramEntities())
                {
                    FACTDEMANDE = (from c in DB.FACTDEMANDE
                                   where c.IDDEMANDE == IDDEMANDE
                                   select c).First();
                    List<FACTDEMANDEDETAIL> l1 = FACTDEMANDE.FACTDEMANDEDETAIL.ToList();
                    foreach (FACTDEMANDEDETAIL det1 in l1)
                    {
                        DB.Entry(det1).State = System.Data.EntityState.Deleted;
                    }

                    FACTDEMANDE.FACTDEMANDEDETAIL.Clear();
                    decimal demmntttc = 0m;
                    decimal demmntht = 0m;
                    decimal demmntttva = 0m;
                    decimal demmntttvaipprf = 0m;

                    if (details != null)
                    {
                        foreach (string[] det in details)
                        {
                            bool retour = false;
                            for (int i = 0; i < 3; i++)
                            {
                                if (det[i] == null || det[i].Length <= 0)
                                    retour = true;
                            }
                            if (retour)
                                continue;
                            decimal qte = 1;
                            decimal prixunitaire = decimal.Parse(det[2].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal tauxtva = decimal.Parse(MNTTVAREV.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal tauxtvaipprf = decimal.Parse(MNTTVAIPPRF.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            
                            decimal prixttc = prixunitaire + (tauxtva * 0.01m) - (tauxtvaipprf * 0.01m);
                            decimal mnttva = (tauxtva * 0.01m) * prixunitaire * qte;

                            decimal Z_Chiffre = prixunitaire;
                            decimal Z_Resultat = Z_Chiffre / 10;
                            Z_Resultat = Math.Truncate(Z_Resultat) + 1;
                            Z_Resultat = (Z_Resultat * 10) - Z_Chiffre;
                            Z_Resultat = Z_Resultat + prixunitaire;

                            decimal mnttvaipprf = (tauxtvaipprf * 0.01m) * Math.Ceiling(Z_Resultat) * qte;
                            decimal mntht = prixunitaire * qte;
                            decimal mntttc = prixunitaire * qte + mnttva - Math.Ceiling(mnttvaipprf);

                            det[0] = det[0].Replace("-APO-", "'");
                            FACTDEMANDEDETAIL fctdet = new FACTDEMANDEDETAIL();
                            fctdet.REFERENCEPRODUIT = det[1]; //Rayon 
                            fctdet.LIBELLEPRODUIT = det[0];//(det[0].Length > 0 ? det[0].Substring(det[0].IndexOf("-") + 1, det[0].Length) : "-1"); // (det[0].Length > 100 ? det[0].Substring(0, 100) : det[0]); 
                            fctdet.DERNIERUTILISATEUR = (det[0].Length > 0 ? int.Parse(det[0].Substring(0, det[0].IndexOf("-"))) : -1); // (det[0].Length > 100 ? det[0].Substring(0, 100) : det[0]); 
                            fctdet.QUANTITE = qte;
                            fctdet.PRIXUNITAIRE = prixunitaire;
                            fctdet.TAUXTVA = tauxtva;
                            fctdet.TTVAIPPRF = tauxtvaipprf;
                            fctdet.MNTTVA = mnttva;
                            fctdet.MNTTVAIPPRF = Math.Ceiling(mnttvaipprf);
                            fctdet.MNTHT = mntht;
                            fctdet.MNTTTC = mntttc;
                            demmntttc = demmntttc + mntttc;
                            demmntht = demmntht + mntht;
                            demmntttva = demmntttva + mnttva;
                            demmntttvaipprf = Math.Ceiling(demmntttvaipprf) + Math.Ceiling(mnttvaipprf);
                            FACTDEMANDE.FACTDEMANDEDETAIL.Add(fctdet);
                        }
                    }
                    if (filename != null && filename.Length > 0)
                    {
                        FACTPIECEJOINTE pj;
                        if (!FACTDEMANDE.FACTPIECEJOINTE.Any())
                        {
                            pj = new FACTPIECEJOINTE();
                            pj.NOMINTERNE = filenameinterne;
                            pj.NOM = filename;
                            pj.CONTENTTYPE = contenttype;
                            pj.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                            pj.DATEMODIFICATION = DateTime.Now;
                            pj.DATECREATION = DateTime.Now;
                            FACTDEMANDE.FACTPIECEJOINTE.Add(pj);
                        }
                        else
                        {
                            pj = FACTDEMANDE.FACTPIECEJOINTE.First();
                            pj.NOMINTERNE = filenameinterne;
                            pj.NOM = filename;
                            pj.CONTENTTYPE = contenttype;
                            pj.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                            pj.DATEMODIFICATION = DateTime.Now;
                            DB.Entry(pj).State = System.Data.EntityState.Modified;
                        }
                    }
                    FACTDEMANDE.TAUXTVA = decimal.Parse(MNTTVAREV.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                    FACTDEMANDE.TAUXTVAIPPRF = decimal.Parse(MNTTVAIPPRF.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                    FACTDEMANDE.DATEMODIFICATION = DateTime.Now;
                    FACTDEMANDE.LIBELLEDEMANDE = (LIBELLEDEMANDE.Length > 50 ? LIBELLEDEMANDE.Substring(0, 50) : LIBELLEDEMANDE);
                    FACTDEMANDE.CHAPITRE = CHAPITRE;
                    FACTDEMANDE.RAYON = (short?)RAYON;
                    FACTDEMANDE.RAYONACHETEUR = (short?)RAYONACHETEUR;
                    FACTDEMANDE.CNUFVENDEUR = CNUFVENDEUR;
                    FACTDEMANDE.CNUFACHETEUR = CNUFACHETEUR;
                    FACTDEMANDE.ANNEEPRESTATION = ANNEEPRESTATION;
                    if (IDFACTTYPE != -1)
                        FACTDEMANDE.IDFACTTYPE = IDFACTTYPE;
                    FACTDEMANDE.TYPECLIENT = (short)TYPECLIENT;
                    if (TYPECLIENT == 1)
                        FACTDEMANDE.IDCLIENTINTERNE = IDCLIENTINTERNE;
                    else
                        FACTDEMANDE.IDCLIENT = IDCLIENT;
                    FACTDEMANDE.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                    FACTDEMANDE.IDSITE = IdSiteConnecte;
                    FACTDEMANDE.MNTHT = demmntht;
                    FACTDEMANDE.MNTTVA = demmntttva;
                    FACTDEMANDE.MNTTVAIPPRF = demmntttvaipprf;

                    FACTDEMANDE.MNTTTC = demmntttc;//FACTDEMANDE.MNTHT + FACTDEMANDE.MNTTVA;

                    DB.Entry(FACTDEMANDE).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();

                }

            }
            else
            {

                using (EramEntities DB = new EramEntities())
                {
                    FACTDEMANDE.DATECREATION = DateTime.Now;
                    FACTDEMANDE.DATEMODIFICATION = DateTime.Now;
                    FACTDEMANDE.LIBELLEDEMANDE = (LIBELLEDEMANDE.Length > 50 ? LIBELLEDEMANDE.Substring(0, 50) : LIBELLEDEMANDE);
                    FACTDEMANDE.CHAPITRE = CHAPITRE;
                    FACTDEMANDE.RAYON = (short?)RAYON;
                    FACTDEMANDE.RAYONACHETEUR = (short?)RAYONACHETEUR;
                    if (IDFACTTYPE != -1)
                        FACTDEMANDE.IDFACTTYPE = IDFACTTYPE;
                    FACTDEMANDE.TYPECLIENT = (short)TYPECLIENT;
                    if (TYPECLIENT == 1)
                        FACTDEMANDE.IDCLIENTINTERNE = IDCLIENTINTERNE;
                    else
                        FACTDEMANDE.IDCLIENT = IDCLIENT;
                    FACTDEMANDE.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                    FACTDEMANDE.IDSITE = IdSiteConnecte;
                    FACTDEMANDE.STATUS = 0;
                    FACTDEMANDE.CNUFVENDEUR = CNUFVENDEUR;
                    FACTDEMANDE.CNUFACHETEUR = CNUFACHETEUR;
                    FACTDEMANDE.ANNEEPRESTATION = ANNEEPRESTATION;
                    FACTDEMANDE.TAUXTVA = decimal.Parse(MNTTVAREV.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                    FACTDEMANDE.TAUXTVAIPPRF = decimal.Parse(MNTTVAIPPRF.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                    decimal demmntttc = 0m;
                    decimal demmntht = 0m;
                    decimal demmntttva = 0m;
                    decimal demmntttvaipprf = 0m;
                    if (details != null)
                    {
                        foreach (string[] det in details)
                        {
                            bool retour = false;
                            for (int i = 0; i < 3; i++)
                            {
                                if (det[i] == null || det[i].Length <= 0)
                                    retour = true;
                            }
                            if (retour)
                                continue;
                            decimal qte = 1;
                            decimal prixunitaire = decimal.Parse(det[2].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal tauxtva = decimal.Parse(MNTTVAREV.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal tauxtvaipprf = decimal.Parse(MNTTVAIPPRF.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal prixttc = prixunitaire + (tauxtva * 0.01m) - (tauxtvaipprf * 0.01m);
                            decimal mnttva = (tauxtva * 0.01m) * prixunitaire * qte;
                            decimal mnttvaipprf = (tauxtvaipprf * 0.01m) * Math.Round(prixunitaire,2) * qte;
                            decimal mntht = prixunitaire * qte;
                            decimal mntttc = prixunitaire * qte + mnttva - Math.Ceiling(mnttvaipprf);

                            det[0] = det[0].Replace("-APO-", "'");
                            FACTDEMANDEDETAIL fctdet = new FACTDEMANDEDETAIL();
                            fctdet.REFERENCEPRODUIT = det[1]; //Rayon 
                            fctdet.LIBELLEPRODUIT = det[0];//(det[0].Length > 0 ? det[0].Substring(det[0].IndexOf("-") + 1, det[0].Length) : "-1"); // (det[0].Length > 100 ? det[0].Substring(0, 100) : det[0]); 
                            fctdet.DERNIERUTILISATEUR = (det[0].Length > 0 ? int.Parse(det[0].Substring(0, det[0].IndexOf("-"))) : -1); // (det[0].Length > 100 ? det[0].Substring(0, 100) : det[0]); 
                            fctdet.QUANTITE = qte;
                            fctdet.PRIXUNITAIRE = prixunitaire;
                            fctdet.TAUXTVA = tauxtva;
                            fctdet.TTVAIPPRF = tauxtvaipprf;
                            fctdet.MNTTVA = mnttva;
                            fctdet.MNTTVAIPPRF = Math.Ceiling(mnttvaipprf);
                            fctdet.MNTHT = mntht;
                            fctdet.MNTTTC = mntttc;
                            demmntttc = demmntttc + mntttc;
                            demmntht = demmntht + mntht;
                            demmntttva = demmntttva + mnttva;
                            demmntttvaipprf = Math.Ceiling(demmntttvaipprf)  + Math.Ceiling(mnttvaipprf);
                            FACTDEMANDE.FACTDEMANDEDETAIL.Add(fctdet);
                        }
                    }
                    if (filename != null && filename.Length > 0)
                    {
                        FACTPIECEJOINTE pj = new FACTPIECEJOINTE();
                        pj.NOMINTERNE = filenameinterne;
                        pj.NOM = filename;
                        pj.CONTENTTYPE = contenttype;
                        pj.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                        pj.DATEMODIFICATION = DateTime.Now;
                        pj.DATECREATION = DateTime.Now;
                        FACTDEMANDE.FACTPIECEJOINTE.Add(pj);
                    }

                    FACTDEMANDE.MNTHT = demmntht;
                    FACTDEMANDE.MNTTVA = demmntttva;
                    FACTDEMANDE.MNTTVAIPPRF = demmntttvaipprf;
                    FACTDEMANDE.MNTTTC = demmntttc;// FACTDEMANDE.MNTHT + FACTDEMANDE.MNTTVA;

                    DB.FACTDEMANDE.Add(FACTDEMANDE);
                    DB.SaveChanges();
                }
            }
            return FACTDEMANDE;
        }


        public FACTDEMANDE update(int IDDEMANDE, string LIBELLEDEMANDE, int IDFACTTYPE, string CHAPITRE, int? RAYON, string CNUFVENDEUR, int TYPECLIENT, int IDCLIENT, int IDCLIENTINTERNE, String CNUFACHETEUR, int? RAYONACHETEUR, string ANNEEPRESTATION, string MNTTVAREV, int UTILISATEURCONNECTE, int IdSiteConnecte, string[][] details, string filename, string filenameinterne, string contenttype)
        {

            FACTDEMANDE FACTDEMANDE = new FACTDEMANDE();

            bool locationgerance = false;
            bool immobilisation = false;
            bool clientstation = false;
            FACTTYPE facttype=new FACTTYPE();
            facttype=facttype.getOne(IDFACTTYPE);

            if (facttype != null && facttype.IDCATEGORIE.HasValue && facttype.IDCATEGORIE==3)
            {
                locationgerance = true;
            }
            if (facttype != null && facttype.IDCATEGORIE.HasValue && facttype.IDCATEGORIE == 4)
            {
                immobilisation = true;
            }
            if (facttype != null && facttype.IDCATEGORIE.HasValue && facttype.IDCATEGORIE == 2)
            {
                clientstation = true;
            }


            if (IDDEMANDE != 0)
            {
                //modification
                using (EramEntities DB = new EramEntities())
                {
                    FACTDEMANDE = (from c in DB.FACTDEMANDE
                                   where c.IDDEMANDE == IDDEMANDE
                                   select c).First();
                    List<FACTDEMANDEDETAIL> l1 = FACTDEMANDE.FACTDEMANDEDETAIL.ToList();
                    foreach (FACTDEMANDEDETAIL det1 in l1)
                    {
                        DB.Entry(det1).State = System.Data.EntityState.Deleted;
                    }

                    FACTDEMANDE.FACTDEMANDEDETAIL.Clear();
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
                            decimal qte = decimal.Parse(det[1].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal prixunitaire = decimal.Parse(det[2].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal tauxtva = decimal.Parse(det[3].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal prixttc = prixunitaire + (tauxtva * 0.01m);
                            decimal mnttva = (tauxtva * 0.01m) * prixunitaire * qte;
                            decimal mntht = prixunitaire * qte;
                            decimal mntttc = prixunitaire * qte + mnttva;

                            //if (clientstation) {
                            //    prixttc = prixunitaire;
                            //    mntttc = prixttc * qte;
                            //    mntht = mntttc / (1 + (tauxtva * 0.01m));
                            //    mnttva = (tauxtva * 0.01m) * mntht;
                            //}

                            if (locationgerance){
                                decimal dtauxtsc = decimal.Parse(tauxtsc.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                                 decimal mnttsc = mntht * (dtauxtsc * 0.01m);
                                 mnttva = (mnttsc + mntht) * (tauxtva * 0.01m);
                                 mntttc = mnttsc + mntht + mnttva;
                            }

                            if (immobilisation && MNTTVAREV != null && MNTTVAREV.Length > 0 && decimal.Parse(MNTTVAREV.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." })>0)
                            {
                                decimal mnttvarev = decimal.Parse(MNTTVAREV.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                                mnttva = mnttvarev;
                                mntttc = mntht + mnttva;
                            }
                            det[0] = det[0].Replace("-APO-", "'");
                            FACTDEMANDEDETAIL fctdet = new FACTDEMANDEDETAIL();
                            fctdet.REFERENCEPRODUIT = ""; // det[0];
                            fctdet.LIBELLEPRODUIT = (det[0].Length > 100 ? det[0].Substring(0, 100) : det[0]); //det[0];
                            fctdet.QUANTITE = qte;
                            fctdet.PRIXUNITAIRE = prixunitaire;
                            fctdet.TAUXTVA = tauxtva;
                            fctdet.MNTTVA = mnttva;
                            fctdet.MNTHT = mntht;
                            fctdet.MNTTTC = mntttc;
                            demmntttc = demmntttc + mntttc;
                            demmntht = demmntht + mntht;
                            demmntttva = demmntttva + mnttva;
                            FACTDEMANDE.FACTDEMANDEDETAIL.Add(fctdet);
                        }
                    }
                    if (filename != null && filename.Length>0)
                    {
                        FACTPIECEJOINTE pj;
                        if (!FACTDEMANDE.FACTPIECEJOINTE.Any())
                        {
                            pj = new FACTPIECEJOINTE();
                            pj.NOMINTERNE = filenameinterne;
                            pj.NOM = filename;
                            pj.CONTENTTYPE = contenttype;
                            pj.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                            pj.DATEMODIFICATION = DateTime.Now;
                            pj.DATECREATION = DateTime.Now;
                            FACTDEMANDE.FACTPIECEJOINTE.Add(pj);
                        }else {
                            pj = FACTDEMANDE.FACTPIECEJOINTE.First();
                            pj.NOMINTERNE = filenameinterne;
                            pj.NOM = filename;
                            pj.CONTENTTYPE = contenttype;
                            pj.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                            pj.DATEMODIFICATION = DateTime.Now;
                            DB.Entry(pj).State = System.Data.EntityState.Modified;
                        }
                    }
                    FACTDEMANDE.DATEMODIFICATION = DateTime.Now;
                    FACTDEMANDE.LIBELLEDEMANDE = (LIBELLEDEMANDE.Length > 50 ? LIBELLEDEMANDE.Substring(0, 50) : LIBELLEDEMANDE);
                    FACTDEMANDE.CHAPITRE = CHAPITRE;
                    FACTDEMANDE.RAYON = (short?)RAYON;
                    FACTDEMANDE.RAYONACHETEUR = (short?)RAYONACHETEUR;
                    FACTDEMANDE.CNUFVENDEUR = CNUFVENDEUR;
                    FACTDEMANDE.CNUFACHETEUR = CNUFACHETEUR;
                    FACTDEMANDE.ANNEEPRESTATION = ANNEEPRESTATION;
                    if (IDFACTTYPE!=-1)
                        FACTDEMANDE.IDFACTTYPE = IDFACTTYPE;
                    FACTDEMANDE.TYPECLIENT = (short)TYPECLIENT;
                    if (TYPECLIENT==1)
                        FACTDEMANDE.IDCLIENTINTERNE =IDCLIENTINTERNE;
                    else
                        FACTDEMANDE.IDCLIENT = IDCLIENT;
                    FACTDEMANDE.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                    FACTDEMANDE.IDSITE = IdSiteConnecte;
                    FACTDEMANDE.MNTHT = demmntht;
                    FACTDEMANDE.MNTTTC = demmntttc;
                    FACTDEMANDE.MNTTVA = demmntttva;
                    DB.Entry(FACTDEMANDE).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();

                }

            }
            else
            {

                using (EramEntities DB = new EramEntities())
                {
                    FACTDEMANDE.DATECREATION = DateTime.Now;
                    FACTDEMANDE.DATEMODIFICATION = DateTime.Now;
                    FACTDEMANDE.LIBELLEDEMANDE = (LIBELLEDEMANDE.Length > 50 ? LIBELLEDEMANDE.Substring(0, 50) : LIBELLEDEMANDE);
                    FACTDEMANDE.CHAPITRE = CHAPITRE;
                    FACTDEMANDE.RAYON = (short?)RAYON;
                    FACTDEMANDE.RAYONACHETEUR = (short?)RAYONACHETEUR;
                    if (IDFACTTYPE != -1)
                        FACTDEMANDE.IDFACTTYPE = IDFACTTYPE;
                    FACTDEMANDE.TYPECLIENT = (short)TYPECLIENT;
                    if (TYPECLIENT == 1)
                        FACTDEMANDE.IDCLIENTINTERNE = IDCLIENTINTERNE;
                    else
                        FACTDEMANDE.IDCLIENT = IDCLIENT;
                    FACTDEMANDE.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                    FACTDEMANDE.IDSITE = IdSiteConnecte;
                    FACTDEMANDE.STATUS = 0;
                    FACTDEMANDE.CNUFVENDEUR = CNUFVENDEUR;
                    FACTDEMANDE.CNUFACHETEUR = CNUFACHETEUR;
                    FACTDEMANDE.ANNEEPRESTATION = ANNEEPRESTATION;
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

                            //if (clientstation)
                            //{
                            //    prixttc = prixunitaire;
                            //    mntttc = prixttc * qte;
                            //    mntht = mntttc / (1 + (tauxtva * 0.01m));
                            //    mnttva = (tauxtva * 0.01m) * mntht;
                            //}

                            if (locationgerance)
                            {
                                decimal dtauxtsc = decimal.Parse(tauxtsc, new NumberFormatInfo() { NumberDecimalSeparator = "." });
                                decimal mnttsc = mntht * (dtauxtsc * 0.01m);
                                mnttva = (mnttsc + mntht) * (tauxtva * 0.01m);
                                mntttc = mnttsc + mntht + mnttva;
                           }

                            if (immobilisation)
                            {
                                decimal mnttvarev = decimal.Parse(MNTTVAREV.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                                mnttva = mnttvarev;
                                mntttc = mntht + mnttva;
                            }
                             det[0]=det[0].Replace("-APO-", "'");
                            FACTDEMANDEDETAIL fctdet = new FACTDEMANDEDETAIL();
                            fctdet.REFERENCEPRODUIT = "";// det[0];
                            fctdet.LIBELLEPRODUIT = (det[0].Length > 100 ? det[0].Substring(0, 100) : det[0]);
                            fctdet.QUANTITE = qte;
                            fctdet.PRIXUNITAIRE = prixunitaire;
                            fctdet.TAUXTVA = tauxtva;
                            fctdet.MNTTVA = mnttva;
                            fctdet.MNTHT = mntht;
                            fctdet.MNTTTC = mntttc;
                            demmntttc = demmntttc + mntttc;
                            demmntht = demmntht + mntht;
                            demmntttva = demmntttva + mnttva;
                            FACTDEMANDE.FACTDEMANDEDETAIL.Add(fctdet);
                        }
                    }
                    if (filename != null && filename.Length > 0)
                    {
                        FACTPIECEJOINTE pj = new FACTPIECEJOINTE();
                        pj.NOMINTERNE = filenameinterne;
                        pj.NOM = filename;
                        pj.CONTENTTYPE = contenttype;
                        pj.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                        pj.DATEMODIFICATION = DateTime.Now;
                        pj.DATECREATION = DateTime.Now;
                        FACTDEMANDE.FACTPIECEJOINTE.Add(pj);
                    }

                    FACTDEMANDE.MNTHT = demmntht;
                    FACTDEMANDE.MNTTTC=demmntttc;
                    FACTDEMANDE.MNTTVA=demmntttva;

                    DB.FACTDEMANDE.Add(FACTDEMANDE);
                    DB.SaveChanges();
                }
            }
            return FACTDEMANDE;
        }

        public FACTDEMANDE updateExport(int IDDEMANDE, string LIBELLEDEMANDE, int IDFACTTYPE, int? RAYON, string CNUFVENDEUR, int TYPECLIENT, int IDCLIENT, int IDCLIENTINTERNE, String CNUFACHETEUR, int? RAYONACHETEUR, string ANNEEPRESTATION, int UTILISATEURCONNECTE, int IdSiteConnecte, List<string[]> details, string filename, string filenameinterne, string contenttype)
        {

            FACTDEMANDE FACTDEMANDE = new FACTDEMANDE();
            if (IDDEMANDE != 0)
            {
                //modification
                using (EramEntities DB = new EramEntities())
                {
                    FACTDEMANDE = (from c in DB.FACTDEMANDE
                                   where c.IDDEMANDE == IDDEMANDE
                                   select c).First();
                    List<FACTDEMANDEDETAIL> l1 = FACTDEMANDE.FACTDEMANDEDETAIL.ToList();
                    foreach (FACTDEMANDEDETAIL det1 in l1)
                    {
                        DB.Entry(det1).State = System.Data.EntityState.Deleted;
                    }

                    FACTDEMANDE.FACTDEMANDEDETAIL.Clear();
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
                            decimal qte = decimal.Parse(det[2].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal prixunitaire = decimal.Parse(det[3].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal tauxtva = 0; //decimal.Parse(det[3].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal prixttc = prixunitaire + (tauxtva * 0.01m);
                            decimal mnttva = (tauxtva * 0.01m) * prixunitaire * qte;
                            decimal mntht = prixunitaire * qte;
                            decimal mntttc = prixunitaire * qte + mnttva;

                            det[0] = det[0].Replace("-APO-", "'");
                            FACTDEMANDEDETAIL fctdet = new FACTDEMANDEDETAIL();
                            fctdet.REFERENCEPRODUIT = det[0];
                            fctdet.LIBELLEPRODUIT = (det[1].Length > 100 ? det[1].Substring(0, 100) : det[1]); //det[0];
                            fctdet.QUANTITE = qte;
                            fctdet.PRIXUNITAIRE = prixunitaire;
                            fctdet.TAUXTVA = tauxtva;
                            fctdet.MNTTVA = mnttva;
                            fctdet.MNTHT = mntht;
                            fctdet.MNTTTC = mntttc;
                            demmntttc = demmntttc + mntttc;
                            demmntht = demmntht + mntht;
                            demmntttva = demmntttva + mnttva;
                            FACTDEMANDE.FACTDEMANDEDETAIL.Add(fctdet);
                        }
                    }
                    if (filename != null && filename.Length > 0)
                    {
                        FACTPIECEJOINTE pj;
                        if (!FACTDEMANDE.FACTPIECEJOINTE.Any())
                        {
                            pj = new FACTPIECEJOINTE();
                            pj.NOMINTERNE = filenameinterne;
                            pj.NOM = filename;
                            pj.CONTENTTYPE = contenttype;
                            pj.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                            pj.DATEMODIFICATION = DateTime.Now;
                            pj.DATECREATION = DateTime.Now;
                            FACTDEMANDE.FACTPIECEJOINTE.Add(pj);
                        }
                        else
                        {
                            pj = FACTDEMANDE.FACTPIECEJOINTE.First();
                            pj.NOMINTERNE = filenameinterne;
                            pj.NOM = filename;
                            pj.CONTENTTYPE = contenttype;
                            pj.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                            pj.DATEMODIFICATION = DateTime.Now;
                            DB.Entry(pj).State = System.Data.EntityState.Modified;
                        }
                    }
                    FACTDEMANDE.DATEMODIFICATION = DateTime.Now;
                    FACTDEMANDE.LIBELLEDEMANDE = (LIBELLEDEMANDE.Length > 50 ? LIBELLEDEMANDE.Substring(0, 50) : LIBELLEDEMANDE);
                    FACTDEMANDE.CHAPITRE = CHAPITRE;
                    FACTDEMANDE.RAYON = (short?)RAYON;
                    FACTDEMANDE.RAYONACHETEUR = (short?)RAYONACHETEUR;
                    FACTDEMANDE.CNUFVENDEUR = CNUFVENDEUR;
                    FACTDEMANDE.CNUFACHETEUR = CNUFACHETEUR;
                    FACTDEMANDE.ANNEEPRESTATION = ANNEEPRESTATION;
                    if (IDFACTTYPE != -1)
                        FACTDEMANDE.IDFACTTYPE = IDFACTTYPE;
                    FACTDEMANDE.TYPECLIENT = (short)TYPECLIENT;
                    if (TYPECLIENT == 1)
                        FACTDEMANDE.IDCLIENTINTERNE = IDCLIENTINTERNE;
                    else
                        FACTDEMANDE.IDCLIENT = IDCLIENT;
                    FACTDEMANDE.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                    FACTDEMANDE.IDSITE = IdSiteConnecte;
                    FACTDEMANDE.MNTHT = demmntht;
                    FACTDEMANDE.MNTTTC = demmntttc;
                    FACTDEMANDE.MNTTVA = demmntttva;
                    DB.Entry(FACTDEMANDE).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();

                }

            }
            else
            {
                using (EramEntities DB = new EramEntities())
                {
                    FACTDEMANDE.DATECREATION = DateTime.Now;
                    FACTDEMANDE.DATEMODIFICATION = DateTime.Now;
                    FACTDEMANDE.LIBELLEDEMANDE = (LIBELLEDEMANDE.Length > 50 ? LIBELLEDEMANDE.Substring(0, 50) : LIBELLEDEMANDE);
                    FACTDEMANDE.CHAPITRE = CHAPITRE;
                    FACTDEMANDE.RAYON = (short?)RAYON;
                    FACTDEMANDE.RAYONACHETEUR = (short?)RAYONACHETEUR;
                    if (IDFACTTYPE != -1)
                        FACTDEMANDE.IDFACTTYPE = IDFACTTYPE;
                    FACTDEMANDE.TYPECLIENT = (short)TYPECLIENT;
                    if (TYPECLIENT == 1)
                        FACTDEMANDE.IDCLIENTINTERNE = IDCLIENTINTERNE;
                    else
                        FACTDEMANDE.IDCLIENT = IDCLIENT;
                    FACTDEMANDE.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                    FACTDEMANDE.IDSITE = IdSiteConnecte;
                    FACTDEMANDE.STATUS = 0;
                    FACTDEMANDE.CNUFVENDEUR = CNUFVENDEUR;
                    FACTDEMANDE.CNUFACHETEUR = CNUFACHETEUR;
                    FACTDEMANDE.ANNEEPRESTATION = ANNEEPRESTATION;
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
                            decimal qte = decimal.Parse(det[2].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal prixunitaire = decimal.Parse(det[3].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal tauxtva = 0; // decimal.Parse(det[3].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal prixttc = prixunitaire + (tauxtva * 0.01m);
                            decimal mnttva = (tauxtva * 0.01m) * prixunitaire * qte;
                            decimal mntht = prixunitaire * qte;
                            decimal mntttc = prixunitaire * qte + mnttva;

                            det[0] = det[0].Replace("-APO-", "'");
                            FACTDEMANDEDETAIL fctdet = new FACTDEMANDEDETAIL();
                            fctdet.REFERENCEPRODUIT = det[0] ;
                            fctdet.LIBELLEPRODUIT = (det[1].Length > 100 ? det[1].Substring(0, 100) : det[1]);
                            fctdet.QUANTITE = qte;
                            fctdet.PRIXUNITAIRE = prixunitaire;
                            fctdet.TAUXTVA = tauxtva;
                            fctdet.MNTTVA = mnttva;
                            fctdet.MNTHT = mntht;
                            fctdet.MNTTTC = mntttc;
                            demmntttc = demmntttc + mntttc;
                            demmntht = demmntht + mntht;
                            demmntttva = demmntttva + mnttva;
                            FACTDEMANDE.FACTDEMANDEDETAIL.Add(fctdet);
                        }
                    }
                    if (filename != null && filename.Length > 0)
                    {
                        FACTPIECEJOINTE pj = new FACTPIECEJOINTE();
                        pj.NOMINTERNE = filenameinterne;
                        pj.NOM = filename;
                        pj.CONTENTTYPE = contenttype;
                        pj.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                        pj.DATEMODIFICATION = DateTime.Now;
                        pj.DATECREATION = DateTime.Now;
                        FACTDEMANDE.FACTPIECEJOINTE.Add(pj);
                    }

                    FACTDEMANDE.MNTHT = demmntht;
                    FACTDEMANDE.MNTTTC = demmntttc;
                    FACTDEMANDE.MNTTVA = demmntttva;

                    DB.FACTDEMANDE.Add(FACTDEMANDE);
                    DB.SaveChanges();
                }
            }
            return FACTDEMANDE;
        }


        public void validerOuRefuser(int id,short status,string commentaire,int iduser,string period)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTDEMANDE FACTDEMANDE = new FACTDEMANDE();
                FACTDEMANDE = (from c in DB.FACTDEMANDE
                               where c.IDDEMANDE == id
                               select c).FirstOrDefault();


                string referenceFacture = "";

                FACTDEMANDE.COMMENTAIRE = commentaire;
                FACTDEMANDE.STATUS = status;

                if (status == 1) {
                    ///valider
                    FACTENSEIGNE ens = new FACTENSEIGNE();
                    ens = ens.getOne(FACTDEMANDE.FACTSITE1.IDENSEIGNE.Value);
                    string nom_seq = ens.CODE.Trim() + "_FACT_SEQ";

                    referenceFacture = returnNextValSeq(nom_seq, ens.CODE, period.Substring(0,4));
                    FACTDEMANDE.REFERENCEFACT = referenceFacture;

                    FACTDEMANDE.DATEVALIDATION = DateTime.Now;
                } 
                else { 
                    //refuser
                    FACTDEMANDE.DATEREFUS = DateTime.Now;
                }
                DB.Entry(FACTDEMANDE).State = System.Data.EntityState.Modified;
                //valider
                if (status == 1) {                 
                    FACTFACTURE facture = new FACTFACTURE();
                    facture.REFERENCEFACT = referenceFacture;//FACTDEMANDE.REFERENCEFACT; //
                    facture.CHAPITRE = FACTDEMANDE.CHAPITRE;
                    facture.RAYON = FACTDEMANDE.RAYON;
                    facture.RAYONACHETEUR = FACTDEMANDE.RAYONACHETEUR;
                    facture.COMMENTAIRE = FACTDEMANDE.COMMENTAIRE;
                    facture.ANNEEPRESTATION = FACTDEMANDE.ANNEEPRESTATION;
                    facture.CNUFVENDEUR = FACTDEMANDE.CNUFVENDEUR;
                    facture.CNUFACHETEUR = FACTDEMANDE.CNUFACHETEUR;
                    facture.DATECREATION = DateTime.Now;
                    facture.DATEMODIFICATION = DateTime.Now;
                    facture.DERNIERUTILISATEUR = iduser;
                    facture.IDCLIENT = FACTDEMANDE.IDCLIENT;
                    facture.IDDEMANDE = FACTDEMANDE.IDDEMANDE;
                    facture.IDFACTTYPE = FACTDEMANDE.IDFACTTYPE;
                    facture.IDSITE = FACTDEMANDE.IDSITE;
                    facture.LIBELLEDEMANDE = FACTDEMANDE.LIBELLEDEMANDE;
                    facture.MNTHT = FACTDEMANDE.MNTHT;
                    facture.MNTTTC = FACTDEMANDE.MNTTTC;
                    facture.MNTTVA = FACTDEMANDE.MNTTVA;
                    facture.MNTTVAPPRF = FACTDEMANDE.MNTTVAIPPRF;
                    facture.IDCLIENTINTERNE = FACTDEMANDE.IDCLIENTINTERNE;
                    facture.TYPECLIENT = FACTDEMANDE.TYPECLIENT;
                    facture.STATUS=0;
                    foreach (FACTDEMANDEDETAIL det in FACTDEMANDE.FACTDEMANDEDETAIL)
                    {
                        FACTFACTUREDETAIL fdet = new FACTFACTUREDETAIL();
                        fdet.DATECREATION=DateTime.Now;
                        fdet.DATEMODIFICATION=DateTime.Now;
                        fdet.DERNIERUTILISATEUR=iduser;
                        fdet.LIBELLEPRODUIT=det.LIBELLEPRODUIT;
                        fdet.MNTHT=det.MNTHT;
                        fdet.MNTTTC=det.MNTTTC;
                        fdet.MNTTVA = det.MNTTVA;
                        fdet.MNTTVAIPPRF = det.MNTTVAIPPRF;
                        fdet.PRIXUNITAIRE=det.PRIXUNITAIRE;
                        fdet.QUANTITE=det.QUANTITE;
                        fdet.REFERENCEPRODUIT=det.REFERENCEPRODUIT;
                        fdet.TAUXTVA=det.TAUXTVA;
                        fdet.TAUXTVAIPPRF = det.TTVAIPPRF;
                        facture.FACTFACTUREDETAIL.Add(fdet);
                    }
                    foreach (FACTPIECEJOINTE dpj in FACTDEMANDE.FACTPIECEJOINTE)
                    {
                        FACTFACTPIECEJOINTE pj=new FACTFACTPIECEJOINTE();
                        pj.CONTENTTYPE=dpj.CONTENTTYPE;
                        pj.NOM=dpj.NOM;
                        pj.NOMINTERNE=dpj.NOMINTERNE;
                        pj.DATECREATION=DateTime.Now;
                        pj.DATEMODIFICATION=DateTime.Now;
                        pj.DERNIERUTILISATEUR = dpj.DERNIERUTILISATEUR;
                        facture.FACTFACTPIECEJOINTE.Add(pj);
                    }
                    DB.FACTFACTURE.Add(facture);
                }
                DB.SaveChanges();
            }
        }

        public static bool seqExists(string sequence)
        {
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder("SELECT COUNT(*) ");
                requette.Append("FROM user_sequences  ");
                requette.Append("WHERE sequence_name = '");
                requette.Append(sequence);
                requette.Append("' ");
                List<long> result2 = DB.Database.SqlQuery<long>(requette.ToString()).ToList();
                return result2[0]>0 ;
            }
        }

        public static int createSequence(string sequence)
        {
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder("create sequence ");
                requette.Append(sequence);
                requette.Append(" minvalue 0 ");
                requette.Append(" maxvalue 999999999999999999999999999 ");
                requette.Append(" start with 1 ");
                requette.Append(" increment by 1 ");
                requette.Append(" nocache ");
                return DB.Database.ExecuteSqlCommand(requette.ToString());
            }
        }

        public static string returnNextValSeq(string sequence, string societe,string annee)
        {
            if (!seqExists(sequence))
                createSequence(sequence);

            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder("select ");
                requette.Append(sequence);
                requette.Append(".NEXTVAL||'/'||'" + societe.Trim() + "'||'/'||'" + annee + "' from dual "); //to_char(sysdate,'RRRR')

                List<string> result2 = DB.Database.SqlQuery<string>(requette.ToString(),
                                                                new OracleParameter("societe", societe)
                                                                ).ToList();
                return result2[0];
            }
        }

        public void delete(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTDEMANDE FACTDEMANDE = new FACTDEMANDE();
                FACTDEMANDE = (from c in DB.FACTDEMANDE
                               where c.IDDEMANDE == id
                               select c).FirstOrDefault();
                //on supprime la relation les pieces jointes

                List<FACTPIECEJOINTE> lp1 = FACTDEMANDE.FACTPIECEJOINTE.ToList();
                foreach (FACTPIECEJOINTE piecej in lp1)
                {
                    DB.Entry(piecej).State = System.Data.EntityState.Deleted;
                }

                //FACTDEMANDE.FACTPIECEJOINTE.Clear();
                //on supprime la relation avec le detail demande
                List<FACTDEMANDEDETAIL> l1 = FACTDEMANDE.FACTDEMANDEDETAIL.ToList();
                foreach (FACTDEMANDEDETAIL det1 in l1)
                {
                    DB.Entry(det1).State = System.Data.EntityState.Deleted;
                }
                //FACTDEMANDE.FACTDEMANDEDETAIL.Clear();
                DB.Entry(FACTDEMANDE).State = System.Data.EntityState.Deleted;
                DB.SaveChanges();
            }
        }

        public List<TOTAUXTVA> calcultotauxtv(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder();
                requette.Append("select round(tauxtva,2) TAUXTVA,round(sum(mntht),2) MNTHT,round(sum(mnttva),2) MNTVA,round(sum(MNTTVAIPPRF),2) MNTVAIPPRF,round(sum(mntttc),2) MNTTC,round(TTVAIPPRF,2) TAUXTVAIPPRF from factdemandedetail ");
                requette.Append("where iddemande="+id);
                requette.Append("group by tauxtva,TTVAIPPRF ");
                requette.Append("order by tauxtva asc ");

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
                requette.Append("from factdemande e,factdemandedetail d ");
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

        public ReportDocument ExportPdf(int iddemande)
        {
            FACTDEMANDE dem = new FACTDEMANDE();
            dem = dem.getOne(iddemande);
            double buffer = Math.Round((dem.MNTTTC.HasValue ? (double)dem.MNTTTC.Value : 0.0), 2);

           // String mntEnLettres = NombreEnLettres.ToLettres((dem.MNTTTC.HasValue ? (double)dem.MNTTTC.Value : 0.0), Pays.France, Devise.Dirham);
            //String mntEnLettres = NombreEnLettres.ToLettres(buffer, Pays.France, Devise.Dirham);
            
            String mntEnLettres = FirstCharToUpper(NombreEnLettres.ToLettres(buffer, Pays.France, Devise.Dirham));
            ReportDocument crp = new CrystalReportfact();

            if (dem.FACTTYPE.IDCATEGORIE==3)
                crp = new CrystalReportfactlg();

            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder();
                string libenseigne = "";
                string libvendeur = "";
                string adrline1 = "";
                string adrline2 = "";
                string icevendeur = "";

                if (dem.TYPECLIENT == 2)
                {
                    FACTCLIENT cli = new FACTCLIENT();
                    cli = cli.getOne(dem.IDCLIENT.Value);
                    libvendeur = cli.DESIGNATIONCLIENT;
                    adrline1 = cli.ADRFACTCLIENT;
                    adrline2 = "";
                    icevendeur = cli.ICECLIENT;
                }
                else {
                    FACTSITE sitee = new FACTSITE();
                    sitee = sitee.getOne(dem.IDCLIENTINTERNE.Value);
                    libenseigne = sitee.FACTENSEIGNE.LIBELLEENSEIGNE;
                    libvendeur = sitee.LIBELLESITE;
                    adrline1 = sitee.ADRLINE1 + " " + sitee.ADRLINE2;
                    adrline2 = "";
                    icevendeur = sitee.FACTENSEIGNE.ICE;
                }

                requette.Append("select e.referencefact nofacture,e.libelledemande desigfact,d.referenceproduit code,decode(t.idcategorie,5,d.libelleproduit||' '||e.libelledemande,d.libelleproduit) designation, ");
                requette.Append(" d.quantite qte,d.prixunitaire prix,d.tauxtva txtva,d.mnttva mnttva,d.mntttc mntttc,d.mntht mntht,e.mntht tmntht,e.mnttva tmnttva,e.mntttc tmntttc,d.MNTTVAIPPRF,d.TTVAIPPRF, e.MNTTVAIPPRF,e.TAUXTVAIPPRF, ");
                requette.Append(" ens.pline3 libclient,ens.pline1 adrline1,ens.pline2 adrline2,ens.pline4 ice, ");
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
                requette.Append(" :logoPath || ens.logo logo,");
                requette.Append(" :mntEnLettres mntlettres, ");


                requette.Append("  to_char(nvl(e.datevalidation,e.datecreation),'DD/MM/RRRR') dfacture ");

                requette.Append(" from factdemande e,factdemandedetail d,factsite s,factenseigne ens,facttype t  ");
                requette.Append("where e.iddemande=");
                requette.Append(iddemande);
                requette.Append(" and e.iddemande = d.iddemande ");
                requette.Append(" and s.idenseigne=ens.idenseigne  ");
                requette.Append(" and e.idsite = s.idsite ");
                requette.Append(" and e.idfacttype = t.idfacttype ");
                string SQL = requette.ToString();
                logger.Info("demande="+SQL);
                OracleConnection conn = (OracleConnection)DB.Database.Connection;
                OracleDataAdapter da = new OracleDataAdapter();
                OracleCommand cmd = new OracleCommand(SQL, conn);

                cmd.Parameters.Add(new OracleParameter("libvendeur", libvendeur));
                cmd.Parameters.Add(new OracleParameter("adrline1", adrline1));
                cmd.Parameters.Add(new OracleParameter("adrline2", adrline2));
                cmd.Parameters.Add(new OracleParameter("icevendeur", icevendeur));
                cmd.Parameters.Add(new OracleParameter("libenseigne", libenseigne));
                cmd.Parameters.Add(new OracleParameter("logoPath", logoPath));
                cmd.Parameters.Add(new OracleParameter("mntEnLettres", mntEnLettres));

                da.SelectCommand = cmd;
                DataSetfact dsfact = new DataSetfact();
                conn.Open();
                da.Fill(dsfact.facture);
                dsfact.AcceptChanges();
                
                crp.SetDataSource(dsfact);

                //StringBuilder requette1 = new StringBuilder();
                //requette1.Append("select 'TVA a '||tauxtva||'%' designation,round(tauxtva,2) txtva,round(sum(mntht),2) ht,round(sum(mnttva),2) taxe,round(sum(mntttc),2) ttc from factdemandedetail ");
                //requette1.Append("where iddemande=" + iddemande);
                //requette1.Append("group by tauxtva ");
                //requette1.Append("order by tauxtva asc ");
                ////da = new OracleDataAdapter();
                //cmd = new OracleCommand(requette1.ToString(), conn);
                //da.SelectCommand = cmd;
                //DataSetTva dstva = new DataSetTva();
                //da.Fill(dstva.totauxtva);
                //dstva.AcceptChanges();

                //StringBuilder requette2 = new StringBuilder();
                //requette2.Append("select ");
                //requette2.Append(" e.mntht tmntht,e.mnttva tmnttva,e.mntttc tmntttc ");
                //requette2.Append(" from factdemande e ");
                //requette2.Append("where e.iddemande=");
                //requette2.Append(iddemande);
                //cmd = new OracleCommand(requette2.ToString(), conn);
                //da.SelectCommand = cmd;
                //da.Fill(dstva.unefacture);
                //dstva.AcceptChanges();
                //crp.Subreports[0].SetDataSource(dstva);
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
        public void imprimer(int from, int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTDEMANDE demande = new FACTDEMANDE();
                demande = demande.getOne(id);
                //if (from == 1)
                //{
                //    facture = facture.getOnebyDemande(id);
                //}
                //else
                //{
                //    facture = facture.getOne2(id);
                //}

                if (!demande.DATEIMPRESSION.HasValue)
                {
                    //imprimee
                    //facture.FACTFACTURESTATUS.clear();// = null;
                    //demande.STATUS = 1;
                    //Date impression
                    demande.DATEIMPRESSION = DateTime.Now;
                    //facture.FACTDEMANDE.DATEIMPRESSION = DateTime.Now;
                    //DB.Entry(facture.FACTDEMANDE).State = System.Data.EntityState.Modified;
                    DB.Entry(demande).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();
                }
            }
        }

        public static string FirstCharToUpper(string input)
        {
            if (String.IsNullOrEmpty(input))
                return input;
            return input.First().ToString().ToUpper() + input.Substring(1);
        }

    }
}
