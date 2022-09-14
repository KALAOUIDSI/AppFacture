using System;
using System.Globalization;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.Text;
using System.Linq;
using System.Data.Entity;
using Domain.appFacture.Rapports;
using Domain.appFacture.Datasets;
using CrystalDecisions.CrystalReports.Engine;
using System.Configuration;
using System.Data.Entity.Validation;
using log4net;

namespace Domain.appFacture
{
    public partial class FACTFACTURE
    {
        public static string agrDbLink = ConfigurationManager.AppSettings["AGRDBLINK"];
        public static ILog logger = log4net.LogManager.GetLogger("KassagrWEBLogger");
        public static string logoPath = ConfigurationManager.AppSettings["LOGOPATH"];
        public static string clientStation = ConfigurationManager.AppSettings["CLIENTSTATION"];
        public static string tauxtsc = ConfigurationManager.AppSettings["TAUXTSC"];


        public List<TOTAUXTVA> TVATOTAUX = new List<TOTAUXTVA>();
        public List<TOTAUXTVA> TVATOTAUXIPPRF = new List<TOTAUXTVA>();
        void OnCreated()
        {
        }

        public decimal? MNTTVAREV
        {
            get
            {
                if(this.MNTTVA.HasValue)
                    return Math.Abs(this.MNTTVA.Value);
                else
                    return this.MNTTVA;
            }
        }

        public List<FACTFACTURE> getAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTFACTURE
                             select c;
                return result.Include("FACTDEMANDE.FACTDEMANDEDETAIL").Include("FACTFACTUREDETAIL").Include("FACTFACTURESTATUS").Include("FACTCLIENT").Include("FACTSITE1").Include("FACTTYPE").Include("FACTSITE").Include("FACTFACTPIECEJOINTE").ToList();
            }
        }

        public List<FACTFACTURE> getAll(int idSite)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTFACTURE
                             where c.IDSITE == idSite
                             select c;
                return result.Include("FACTDEMANDE.FACTDEMANDEDETAIL").Include("FACTFACTUREDETAIL").Include("FACTFACTURESTATUS").Include("FACTCLIENT").Include("FACTSITE1").Include("FACTTYPE").Include("FACTSITE").Include("FACTFACTPIECEJOINTE").ToList();
            }
        }
        public int countAll(int idsite)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTFACTURE
                             where (c.IDSITE == idsite || idsite == -1) && c.STATUS != 5 && c.FACTTYPE.IDCATEGORIE != 2
                             select c;
                return result.Count();
            }
        }

        public int countAll(int idsite, int iduser)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTFACTURE
                             where (c.IDSITE == idsite || idsite == -1) && c.STATUS != 5
                             && c.FACTTYPE.IDCATEGORIE != 2 && c.FACTTYPE.FACTUSER.Any(u => u.IDUTILISATEUR == iduser)
                             select c;
                return result.Count();
            }
        }

        public List<FACTFACTURE> facturesAGenerer(int iduser)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTFACTURE
                             where c.STATUS == 1 && c.FACTTYPE.IDCATEGORIE != 2
                             && c.FACTTYPE.FACTUSER.Any(u => u.IDUTILISATEUR == iduser)
                             select c;
                return result.ToList();
            }
        }


        public List<FACTFACTURE> facturesAIntegrer(int iduser)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTFACTURE
                             where c.STATUS == 2 && c.FACTTYPE.IDCATEGORIE != 2
                             && c.FACTTYPE.FACTUSER.Any(u => u.IDUTILISATEUR == iduser)
                             select c;
                return result.ToList();
            }
        }


        public FACTUREPaginationRes getAll(int page, int pageSize, string search, int sortby, Boolean isasc, FACTUREAdvFiltre AdvFiltre )
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTUREPaginationRes res = new FACTUREPaginationRes();

                var result = (from c in DB.FACTFACTURE
                              where (c.IDSITE == AdvFiltre.site || AdvFiltre.site == -1) && c.STATUS!=5
                              && c.FACTTYPE.IDCATEGORIE != 2 && c.FACTTYPE.FACTUSER.Any(u => u.IDUTILISATEUR == AdvFiltre.user)
                              select c).Include("FACTDEMANDE.FACTDEMANDEDETAIL").Include("FACTFACTUREDETAIL").Include("FACTFACTURESTATUS").Include("FACTCLIENT").Include("FACTSITE1").Include("FACTTYPE").Include("FACTSITE").Include("FACTFACTPIECEJOINTE");

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
                    case 8:
                        result = isasc ? result.OrderBy(c => c.DATECREATION) : result.OrderByDescending(c => c.DATECREATION);
                        break;
                    default: // Not: case "Default"
                        result = result.OrderByDescending(c => c.IDFACTURE);
                        break;
                }

                res.count = result.Count();
                res.listFACTURE = result
                .Skip((page - 1) * pageSize)
                .Take(pageSize).ToList();

                return res;
            }
        }

        public FACTFACTURE getOne(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.FACTFACTURE
                              where c.IDFACTURE == id
                              select c).Include("FACTFACTUREDETAIL").Include("FACTFACTURESTATUS").Include("FACTCLIENT").Include("FACTSITE1").Include("FACTTYPE.FACTTYPECATEGORIE").Include("FACTSITE").Include("FACTFACTPIECEJOINTE").First();
                return result;
            }
        }

        public FACTFACTURE getOnebyDemande(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.FACTFACTURE
                              where c.IDDEMANDE == id && (c.FLAGAVOIR ?? 0) == 0
                              select c).Include("FACTDEMANDE").FirstOrDefault();
                return result;
            }
        }
        public FACTFACTURE getOne2(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.FACTFACTURE
                              where c.IDFACTURE == id
                              select c).Include("FACTDEMANDE").First();
                return result;
            }
        }

        public FACTFACTURE getOne(string libelle)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.FACTFACTURE
                              where c.LIBELLEDEMANDE.ToLower().Contains(libelle.ToLower())
                              select c).Include("FACTFACTUREDETAIL").Include("FACTFACTURESTATUS").Include("FACTCLIENT").Include("FACTSITE1").Include("FACTTYPE").Include("FACTSITE").Include("FACTFACTPIECEJOINTE").FirstOrDefault();
                return result;

            }
        }

        public FACTFACTPIECEJOINTE getOnePJ(int idpj)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.FACTFACTPIECEJOINTE
                              where c.IDPIECEJOINTE == idpj
                              select c).FirstOrDefault();
                return result;
            }
        }

        public void update(int IDDEMANDE, string LIBELLEDEMANDE, int IDFACTTYPE, string CHAPITRE,int IDCLIENT, string IDENTIFCLIENT, string DESIGNATIONCLIENT, int UTILISATEURCONNECTE, int IdSiteConnecte, string[][] details, string filename, string filenameinterne)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTFACTURE facture = new FACTFACTURE();
                //Modify
                DB.Entry(facture).State = System.Data.EntityState.Modified;

                //Add
                DB.FACTFACTURE.Add(facture);
                DB.SaveChanges();
            }
        }
        public void createavoir(int IDFACTURE, string LIBELLEDEMANDE, string MNTTVAREV, int UTILISATEURCONNECTE, string[][] details)
        {
            //using (EramEntities DB = new EramEntities())
            //{
            //    FACTFACTURE facture = new FACTFACTURE();
            //    //Modify
            //    DB.Entry(facture).State = System.Data.EntityState.Modified;

            //    //Add
            //    DB.FACTFACTURE.Add(facture);
            //    DB.SaveChanges();
            //}

            bool locationgerance = false;
            bool immobilisation = false;
            bool clientstation = false;

            FACTFACTURE facture = new FACTFACTURE();
            facture = facture.getOne(IDFACTURE);

            if (facture.FACTTYPE.IDCATEGORIE.HasValue && facture.FACTTYPE.IDCATEGORIE == 3)
            {
                locationgerance = true;
            }
            if (facture.FACTTYPE.IDCATEGORIE.HasValue && facture.FACTTYPE.IDCATEGORIE == 4 
                && MNTTVAREV != null 
                && MNTTVAREV.Length > 0
                && decimal.Parse(MNTTVAREV.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." })!=0)
            {
                immobilisation = true;
            }
            if (facture.FACTTYPE.IDCATEGORIE.HasValue && facture.FACTTYPE.IDCATEGORIE == 2)
            {
                clientstation = true;
            }

            using (EramEntities DB = new EramEntities())
            {
                FACTFACTURE FACTDEMANDE = new FACTFACTURE();
                FACTDEMANDE.FLAGAVOIR = 1;
                FACTDEMANDE.DATECREATION = DateTime.Now;
                FACTDEMANDE.DATEMODIFICATION = DateTime.Now;
                FACTDEMANDE.LIBELLEDEMANDE = (LIBELLEDEMANDE.Length > 50 ? LIBELLEDEMANDE.Substring(0, 50) : LIBELLEDEMANDE);
                FACTDEMANDE.IDFACTTYPE = facture.IDFACTTYPE;
                FACTDEMANDE.TYPECLIENT = facture.TYPECLIENT;
                FACTDEMANDE.IDCLIENTINTERNE = facture.IDCLIENTINTERNE;
                FACTDEMANDE.IDCLIENT = facture.IDCLIENT;

                FACTDEMANDE.IDDEMANDE = facture.IDDEMANDE;
                FACTDEMANDE.REFERENCEFACT = facture.REFERENCEFACT;

                FACTDEMANDE.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                FACTDEMANDE.IDSITE = facture.IDSITE;
                FACTDEMANDE.STATUS = 0;
                FACTDEMANDE.ANNEEPRESTATION = facture.ANNEEPRESTATION;
                FACTDEMANDE.CNUFVENDEUR = facture.CNUFVENDEUR;
                FACTDEMANDE.CNUFACHETEUR = facture.CNUFACHETEUR;
                FACTDEMANDE.RAYON=facture.RAYON;
                FACTDEMANDE.RAYONACHETEUR=facture.RAYONACHETEUR;
                
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

                        if (locationgerance)
                        {
                            decimal dtauxtsc = decimal.Parse(tauxtsc.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal mnttsc = mntht * (dtauxtsc * 0.01m);
                            mnttva = (mnttsc + mntht) * (tauxtva * 0.01m);
                            mntttc = mnttsc + mntht + mnttva;
                        }

                        if (immobilisation)
                        {
                            decimal mnttvarev = decimal.Parse(MNTTVAREV.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });// decimal.Parse(MNTTVAREV.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            if (mnttvarev < 0)
                                mnttvarev = -1 * mnttvarev;
                            mnttva = mnttvarev;
                            mntttc = mntht + mnttva;
                        }
                        det[0] = det[0].Replace("-APO-", "'");
                        FACTFACTUREDETAIL fctdet = new FACTFACTUREDETAIL();
                        fctdet.REFERENCEPRODUIT = "";// det[0];
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
                        FACTDEMANDE.FACTFACTUREDETAIL.Add(fctdet);
                    }
                }
                FACTDEMANDE.MNTHT = -1 * demmntht;
                FACTDEMANDE.MNTTTC = -1 * demmntttc;
                FACTDEMANDE.MNTTVA = -1 * demmntttva;

                DB.FACTFACTURE.Add(FACTDEMANDE);
                DB.SaveChanges();

                //try
                //{
                //    // Your code...
                //    // Could also be before try if you know the exception occurs in SaveChanges

                //    DB.SaveChanges();
                //}
                //catch (DbEntityValidationException e)
                //{
                //    foreach (var eve in e.EntityValidationErrors)
                //    {
                //        string msg1 = String.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                //            eve.Entry.Entity.GetType().Name, eve.Entry.State);
                //        logger.Info(msg1);
                //        foreach (var ve in eve.ValidationErrors)
                //        {
                //            string msg2 =String.Format("- Property: \"{0}\", Error: \"{1}\"",ve.PropertyName, ve.ErrorMessage);
                //            logger.Info(msg2);
                //        }
                //    }
                //    throw;
                //}
            }

        }

        public void updateavoir(int IDFACTURE, string LIBELLEDEMANDE, string MNTTVAREV ,int UTILISATEURCONNECTE, string[][] details)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTFACTURE FACTDEMANDE = new FACTFACTURE();
                FACTDEMANDE = (from c in DB.FACTFACTURE
                               where c.IDFACTURE == IDFACTURE
                               select c).First();

                bool locationgerance = false;
                bool immobilisation = false;
                bool clientstation = false;

                if (FACTDEMANDE.FACTTYPE.IDCATEGORIE.HasValue && FACTDEMANDE.FACTTYPE.IDCATEGORIE == 3)
                {
                    locationgerance = true;
                }
                if (FACTDEMANDE.FACTTYPE.IDCATEGORIE.HasValue && FACTDEMANDE.FACTTYPE.IDCATEGORIE == 4
                    && MNTTVAREV != null
                    && MNTTVAREV.Length > 0
                    && decimal.Parse(MNTTVAREV.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." }) != 0)
                {
                    immobilisation = true;
                }
                if (FACTDEMANDE.FACTTYPE.IDCATEGORIE.HasValue && FACTDEMANDE.FACTTYPE.IDCATEGORIE == 2)
                {
                    clientstation = true;
                }

                List<FACTFACTUREDETAIL> l1 = FACTDEMANDE.FACTFACTUREDETAIL.ToList();
                foreach (FACTFACTUREDETAIL det1 in l1)
                {
                    DB.Entry(det1).State = System.Data.EntityState.Deleted;
                }

                FACTDEMANDE.FACTFACTUREDETAIL.Clear();

                FACTDEMANDE.DATEMODIFICATION = DateTime.Now;
                FACTDEMANDE.LIBELLEDEMANDE = (LIBELLEDEMANDE.Length > 50 ? LIBELLEDEMANDE.Substring(0, 50) : LIBELLEDEMANDE);
                FACTDEMANDE.DERNIERUTILISATEUR = UTILISATEURCONNECTE;

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

                        if (locationgerance)
                        {
                            decimal dtauxtsc = decimal.Parse(tauxtsc.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            decimal mnttsc = mntht * (dtauxtsc * 0.01m);
                            mnttva = (mnttsc + mntht) * (tauxtva * 0.01m);
                            mntttc = mnttsc + mntht + mnttva;
                        }

                        if (immobilisation)
                        {
                            decimal mnttvarev = decimal.Parse(MNTTVAREV.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });// decimal.Parse(MNTTVAREV.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                            if (mnttvarev < 0)
                                mnttvarev = -1 * mnttvarev;
                            mnttva = mnttvarev;
                            mntttc = mntht + mnttva;
                        }

                        det[0] = det[0].Replace("-APO-", "'");
                        FACTFACTUREDETAIL fctdet = new FACTFACTUREDETAIL();
                        fctdet.REFERENCEPRODUIT = "";//det[0];
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
                        FACTDEMANDE.FACTFACTUREDETAIL.Add(fctdet);
                    }
                }
                FACTDEMANDE.MNTHT = -1 * demmntht;
                FACTDEMANDE.MNTTTC = -1 * demmntttc;
                FACTDEMANDE.MNTTVA = -1 * demmntttva;

                DB.Entry(FACTDEMANDE).State = System.Data.EntityState.Modified;
                DB.SaveChanges();
            }

        }

        public void createavoirMS(int IDFACTURE, string LIBELLEDEMANDE, string MNTTVAREV, int UTILISATEURCONNECTE, string[][] details)
        {
            FACTFACTURE facture = new FACTFACTURE();
            facture = facture.getOne(IDFACTURE);

            using (EramEntities DB = new EramEntities())
            {
                FACTFACTURE FACTDEMANDE = new FACTFACTURE();
                FACTDEMANDE.FLAGAVOIR = 1;
                FACTDEMANDE.DATECREATION = DateTime.Now;
                FACTDEMANDE.DATEMODIFICATION = DateTime.Now;
                FACTDEMANDE.LIBELLEDEMANDE = (LIBELLEDEMANDE.Length > 50 ? LIBELLEDEMANDE.Substring(0, 50) : LIBELLEDEMANDE);
                FACTDEMANDE.IDFACTTYPE = facture.IDFACTTYPE;
                FACTDEMANDE.TYPECLIENT = facture.TYPECLIENT;
                FACTDEMANDE.IDCLIENTINTERNE = facture.IDCLIENTINTERNE;
                FACTDEMANDE.IDCLIENT = facture.IDCLIENT;

                FACTDEMANDE.IDDEMANDE = facture.IDDEMANDE;
                FACTDEMANDE.REFERENCEFACT = facture.REFERENCEFACT;

                FACTDEMANDE.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                FACTDEMANDE.IDSITE = facture.IDSITE;
                FACTDEMANDE.STATUS = 0;
                FACTDEMANDE.ANNEEPRESTATION = facture.ANNEEPRESTATION;
                FACTDEMANDE.CNUFVENDEUR = facture.CNUFVENDEUR;
                FACTDEMANDE.CNUFACHETEUR = facture.CNUFACHETEUR;
                FACTDEMANDE.RAYON = facture.RAYON;
                FACTDEMANDE.RAYONACHETEUR = facture.RAYONACHETEUR;
                FACTDEMANDE.TAUXTVA = decimal.Parse(MNTTVAREV.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });


                decimal demmntttc = 0m;
                decimal demmntht = 0m;
                decimal demmntttva = 0m;
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
                        decimal prixttc = prixunitaire + (tauxtva * 0.01m);
                        decimal mnttva = (tauxtva * 0.01m) * prixunitaire * qte;
                        decimal mntht = prixunitaire * qte;
                        decimal mntttc = prixunitaire * qte + mnttva;

                        det[0] = det[0].Replace("-APO-", "'");

                        FACTFACTUREDETAIL fctdet = new FACTFACTUREDETAIL();
                        fctdet.REFERENCEPRODUIT = det[1]; //Rayon 
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
                        FACTDEMANDE.FACTFACTUREDETAIL.Add(fctdet);
                    }
                }
                FACTDEMANDE.MNTHT = -1 * demmntht;
                FACTDEMANDE.MNTTTC = -1 * demmntttc;
                FACTDEMANDE.MNTTVA = -1 * demmntttva;

                DB.FACTFACTURE.Add(FACTDEMANDE);
                DB.SaveChanges();

            }

        }

        public void updateavoirMS(int IDFACTURE, string LIBELLEDEMANDE, string MNTTVAREV, int UTILISATEURCONNECTE, string[][] details)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTFACTURE FACTDEMANDE = new FACTFACTURE();
                FACTDEMANDE = (from c in DB.FACTFACTURE
                               where c.IDFACTURE == IDFACTURE
                               select c).First();

                List<FACTFACTUREDETAIL> l1 = FACTDEMANDE.FACTFACTUREDETAIL.ToList();
                foreach (FACTFACTUREDETAIL det1 in l1)
                {
                    DB.Entry(det1).State = System.Data.EntityState.Deleted;
                }

                FACTDEMANDE.FACTFACTUREDETAIL.Clear();

                FACTDEMANDE.DATEMODIFICATION = DateTime.Now;
                FACTDEMANDE.LIBELLEDEMANDE = (LIBELLEDEMANDE.Length > 50 ? LIBELLEDEMANDE.Substring(0, 50) : LIBELLEDEMANDE);
                FACTDEMANDE.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                FACTDEMANDE.TAUXTVA = decimal.Parse(MNTTVAREV.Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });

                decimal demmntttc = 0m;
                decimal demmntht = 0m;
                decimal demmntttva = 0m;
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
                        decimal prixttc = prixunitaire + (tauxtva * 0.01m);
                        decimal mnttva = (tauxtva * 0.01m) * prixunitaire * qte;
                        decimal mntht = prixunitaire * qte;
                        decimal mntttc = prixunitaire * qte + mnttva;

                        det[0] = det[0].Replace("-APO-", "'");

                        FACTFACTUREDETAIL fctdet = new FACTFACTUREDETAIL();
                        fctdet.REFERENCEPRODUIT = det[1]; //Rayon 
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
                        FACTDEMANDE.FACTFACTUREDETAIL.Add(fctdet);
                    }
                }
                FACTDEMANDE.MNTHT = -1 * demmntht;
                FACTDEMANDE.MNTTTC = -1 * demmntttc;
                FACTDEMANDE.MNTTVA = -1 * demmntttva;

                DB.Entry(FACTDEMANDE).State = System.Data.EntityState.Modified;
                DB.SaveChanges();
            }

        }

        

        public void validerOuRefuser(int id, short status, string commentaire, int iduser)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTFACTURE facture = new FACTFACTURE();
                //Modify
                DB.Entry(facture).State = System.Data.EntityState.Modified;

                //Add
                DB.FACTFACTURE.Add(facture);
                DB.SaveChanges();
            }
        }

        public void imprimer(int from,int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTFACTURE facture = new FACTFACTURE();
                if (from == 1)
                {
                    facture = facture.getOnebyDemande(id);
                }
                else
                {
                    facture = facture.getOne2(id);
                }

                if (facture.STATUS == 0)
                {
                    //imprimee
                    //facture.FACTFACTURESTATUS.clear();// = null;
                    facture.STATUS = 1;
                    //Date impression
                    facture.DATEIMPRESSION = DateTime.Now;
                    //facture.FACTDEMANDE.DATEIMPRESSION = DateTime.Now;
                    //DB.Entry(facture.FACTDEMANDE).State = System.Data.EntityState.Modified;
                    DB.Entry(facture).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();
                }
            }
        }


        public void acomptabiliser(int id,int user)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTFACTURE facture = new FACTFACTURE();
                facture = (from c in DB.FACTFACTURE
                           where c.IDFACTURE == id
                               select c).First();
                facture.STATUS = 2;
                facture.DATEMODIFICATION = DateTime.Now;
                facture.DERNIERUTILISATEUR = user;
                
                DB.Entry(facture).State = System.Data.EntityState.Modified;
                DB.SaveChanges();
            }
        }

        public void comptabiliser(int id, int user)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTFACTURE facture = new FACTFACTURE();
                facture = (from c in DB.FACTFACTURE
                           where c.IDFACTURE == id
                           select c).First();
                facture.STATUS = 3;
                facture.DATECOMPTABILISATION = DateTime.Now;
                facture.DATEMODIFICATION = DateTime.Now;
                facture.DERNIERUTILISATEUR = user;

                DB.Entry(facture).State = System.Data.EntityState.Modified;
                DB.SaveChanges();
            }
        }


        public void delete(int id, int user)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTFACTURE FACTFACTURE = new FACTFACTURE();
                FACTFACTURE = (from c in DB.FACTFACTURE
                               where c.IDFACTURE == id
                               select c).FirstOrDefault();

                FACTFACTURE.STATUS = 5;
                FACTFACTURE.DATEMODIFICATION = DateTime.Now;
                FACTFACTURE.DERNIERUTILISATEUR = user;

                DB.Entry(FACTFACTURE).State = System.Data.EntityState.Modified;
                DB.SaveChanges();
            }
        }

        public void reporter(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTFACTURE FACTFACTURE = new FACTFACTURE();
                FACTFACTURE = (from c in DB.FACTFACTURE
                               where c.IDFACTURE == id
                               select c).FirstOrDefault();
                if (FACTFACTURE.STATUS == 1 || FACTFACTURE.STATUS == 2)
                {
                    if (FACTFACTURE.STATUS == 2)
                    {
                        deleteAcrtrans(id);
                    }
                    //on supprime la relation les pieces jointes
                    FACTFACTURE.STATUS = 6;

                    DB.Entry(FACTFACTURE).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();
                }
                else { 
                    if (FACTFACTURE.STATUS == 6)
                    {
                        //on supprime la relation les pieces jointes
                        FACTFACTURE.STATUS = 1;

                        DB.Entry(FACTFACTURE).State = System.Data.EntityState.Modified;
                        DB.SaveChanges();
                    }
                }
            }
        }

        public List<TOTAUXTVA> calcultotauxtv(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder();
                requette.Append("select round(tauxtva,2) TAUXTVA,round(sum(mntht),2) MNTHT,round(sum(mnttva),2) MNTVA,round(sum(mntttc),2) MNTTC ");
                requette.Append(" ,round(TAUXTVAIPPRF,2) TAUXTVAIPPRF,round(sum(MNTTVAIPPRF),2) MNTVAIPPRF from FACTFACTUREDETAIL ");
                requette.Append("where IDFACTURE=" + id);
                requette.Append("group by tauxtva,TAUXTVAIPPRF ");
                requette.Append("order by tauxtva asc ");

                IEnumerable<TOTAUXTVA> result = DB.Database.SqlQuery<TOTAUXTVA>(requette.ToString());
                return result.AsQueryable().ToList();
            }
        }

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
            FACTFACTURE dem = new FACTFACTURE();
            dem = dem.getOne(iddemande);
            double buffer = Math.Round((dem.MNTTTC.HasValue ? (double)dem.MNTTTC.Value : 0.0), 2);
            
            //String mntEnLettres = NombreEnLettres.ToLettres(buffer, Pays.France, Devise.Dirham);

            String mntEnLettres = FACTDEMANDE.FirstCharToUpper(NombreEnLettres.ToLettres(buffer, Pays.France, Devise.Dirham));

            ReportDocument crp = new CrystalReportfact();

            if (dem.FACTTYPE.IDCATEGORIE == 3)
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
                else
                {
                    FACTSITE sitee = new FACTSITE();
                    sitee = sitee.getOne(dem.IDCLIENTINTERNE.Value);
                    libenseigne = sitee.FACTENSEIGNE.LIBELLEENSEIGNE;
                    libvendeur = sitee.LIBELLESITE;
                    adrline1 = sitee.ADRLINE1 + " " + sitee.ADRLINE2;
                    adrline2 = "";
                    icevendeur = sitee.FACTENSEIGNE.ICE;
                }

                requette.Append("select e.referencefact nofacture,e.libelledemande desigfact,d.referenceproduit code,decode(t.idcategorie,5,d.libelleproduit||' '||e.libelledemande,d.libelleproduit) designation, ");
                requette.Append(" d.quantite qte,d.prixunitaire prix,d.tauxtva txtva,d.mnttva mnttva,d.mntttc mntttc,d.mntht mntht,e.mntht tmntht,e.mnttva tmnttva,e.mntttc tmntttc,d.MNTTVAIPPRF,d.TAUXTVA, e.MNTTVAPPRF,e.TAUXTVAPPRF, ");
                requette.Append(" ens.pline3 libclient,ens.pline1 adrline1,ens.pline2 adrline2,ens.pline4 ice, ");
                
                /*requette.Append(" '" + libvendeur + "' libvendeur, ");
                requette.Append(" '" + adrline1 + "' adrvendeur1, ");
                requette.Append(" '" + adrline2 + "' adrvendeur2, ");
                requette.Append(" '" + icevendeur + "' icevendeur, ");
                requette.Append(" '" + libenseigne + "' ensclient, ");
                requette.Append("'" + logoPath + "'||ens.logo logo,");
                requette.Append("'" + mntEnLettres + "' mntlettres, ");*/

                requette.Append(" :libvendeur libvendeur, ");
                requette.Append(" :adrline1 adrvendeur1, ");
                requette.Append(" :adrline2 adrvendeur2, ");
                requette.Append(" :icevendeur icevendeur, ");
                requette.Append(" :libenseigne ensclient, ");
                requette.Append(" :logoPath || ens.logo logo,");
                requette.Append(" :mntEnLettres mntlettres, ");


                requette.Append("  to_char(e.datecreation,'DD/MM/RRRR') dfacture ");
                requette.Append(" from factfacture e,factfacturedetail d,factsite s,factenseigne ens,facttype t ");
                requette.Append("where e.idfacture=");
                requette.Append(iddemande);
                requette.Append(" and e.idfacture = d.idfacture ");
                requette.Append(" and s.idenseigne=ens.idenseigne  ");
                requette.Append(" and e.idsite = s.idsite ");
                requette.Append(" and e.idfacttype = t.idfacttype ");
                string SQL = requette.ToString();
                logger.Info("facture"+SQL);
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
                //requette1.Append("select 'TVA a '||tauxtva||'%' designation,round(tauxtva,2) txtva,round(sum(mntht),2) ht,round(sum(mnttva),2) taxe,round(sum(mntttc),2) ttc from factfacturedetail ");
                //requette1.Append("where idfacture=" + iddemande);
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
                //requette2.Append(" from factfacture e ");
                //requette2.Append("where e.idfacture=");
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

        public long getSequence(String seqname) {
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



        public bool insert_temp(AGR bc, String temp)
        {
            String req= Utils.getRequeteInsertAcrtrans(bc,temp);
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
                            throw ex;
                            //return false;
                        }
                    }
                    conn.Close();
                    conn.Dispose();
                }
            }
            return true;
        }



        public void deleteAcrtrans(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.ACRTRANS
                             where c.IDFACTURE == id
                             select c;
                List<ACRTRANS> listeacrtrans = result.ToList();
                foreach (ACRTRANS acr in listeacrtrans) { 
                    DB.Entry(acr).State = System.Data.EntityState.Deleted;
                }
                DB.SaveChanges();
            }
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
                requette.Append("(select count(*) from ACRTRANS b where b.voucher_no=m.voucher_no and b.client=m.client and b.IDFACTURE=m.IDFACTURE) NBRE_SEQUENCE, ");
                requette.Append("m.EXT_INV_REF REF_FACT, ");
                requette.Append("m.client CLIENT, ");
                requette.Append("m.Fiscal_Year ANNEE_FISCALE, ");
                requette.Append("m.PERIOD PERIOD, ");
                //requette.Append("m.DIM_2 DIM2, ");
                //requette.Append("m.DIM_4 DIM4, ");
                requette.Append("trunc(m.VOUCHER_DATE) DATE_COMPTABILITE, ");
                requette.Append("trunc(m.TRANS_DATE) DATE_FACTURE, ");
                requette.Append("(select sum(decode(m1.dc_flag,-1,m1.amount,0)) from ACRTRANS m1 where m1.voucher_no=m.voucher_no and m1.client=m.client and m1.IDFACTURE=m.IDFACTURE) MNT_NEG, ");
                requette.Append("(select sum(decode(m1.dc_flag,1,m1.amount,0)) from ACRTRANS m1 where m1.voucher_no=m.voucher_no and m1.client=m.client and m1.IDFACTURE=m.IDFACTURE) MNT_POS, ");
                requette.Append("(select sum(m1.amount) from ACRTRANS m1 where m1.voucher_no=m.voucher_no and m1.client=m.client and m1.IDFACTURE=m.IDFACTURE ) EQUILIBRE ");
                requette.Append("from ACRTRANS m,factfacture f,factassouserfacttype uf ");
                requette.Append(" where 1 = 1 ");
                requette.Append(" and m.IDFACTURE = f.IDFACTURE ");
                requette.Append(" and f.status in (2,4) ");
                requette.Append(" and uf.idfacttype=f.idfacttype ");
                requette.Append(" and uf.idutilisateur=" + iduser);
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
                requette.Append(" FROM acrtrans m ");
                requette.Append(" WHERE m.voucher_no = " + croId);
                requette.Append(" and m.IDFACTURE = " + idfacture);
                requette.Append(" and m.client = '" + client + "' ");
                requette.Append("order by m.voucher_no,m.sequence_no asc ");

                IEnumerable<INTEGBROUILLARDDET> result2 = DB.Database.SqlQuery<INTEGBROUILLARDDET>(requette.ToString());
                IQueryable<INTEGBROUILLARDDET> result = result2.AsQueryable();


                return result.ToList();
            }
        }

        public List<PIECE> pieceAlettrer()
        {
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder();
                requette.Append(" SELECT distinct CLIENT     ,");
                requette.Append("                 VOUCHER_NO  ");
                requette.Append(" FROM acrtrans ");
                requette.Append(" WHERE nvl(flaglettrage,0) = 0 ");

                IEnumerable<PIECE> result2 = DB.Database.SqlQuery<PIECE>(requette.ToString());
                IQueryable<PIECE> result = result2.AsQueryable();


                return result.ToList();
            }
        }


        public long getMaxNolot()
        {
            long seq = 0;
            using (EramEntities DB = new EramEntities())
            {
                using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
                {
                    conn.Open();
                    using (OracleCommand cmd = new OracleCommand(" select max(NOLOT) from acrtrans where NOLOT is not null ", conn))
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

        public String updateCounter(String client, Int64 counter)
        {
            StringBuilder requette = new StringBuilder();
            requette.Append("update acrtransgr" + agrDbLink + " A");
            requette.Append(" set A.counter=" + counter);
            requette.Append(" where A.client='" + client + "' ");
            requette.Append(" and A.VOUCH_SERIES in (select vouch_series from ACRVOUCHTYPE" + agrDbLink + " B where B.client='" + client + "' and B.VOUCHER_TYPE='GF' ) ");

            return requette.ToString();
        }
        public void updateAcrtransLastUpdate(int idfacture)
        {
            using (EramEntities DB = new EramEntities())
            {
                List<ACRTRANS> listAcrtrans = new List<ACRTRANS>();
                listAcrtrans = (from c in DB.ACRTRANS
                                where c.IDFACTURE == idfacture
                                select c).ToList();
                foreach (ACRTRANS acrtrans in listAcrtrans)
                {
                    acrtrans.LAST_UPDATE = DateTime.Now;
                    acrtrans.DUE_DATE    = DateTime.Now;
                    DB.Entry(acrtrans).State = System.Data.EntityState.Modified;
                }
                DB.SaveChanges();
            }
        }

        public ACRTRANS ligneALettrer(string client,decimal voucher_no)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.ACRTRANS
                             where c.VOUCHER_NO == voucher_no && c.CLIENT == client
                             select c;
                return result.OrderByDescending(c => Math.Abs(c.AMOUNT)).First();
            }
        }



        public REGLEMENT reglementAlettrer(string account,string apar_id,string reffact,string client)
        {
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder();
                requette.Append(" select VOUCHER_NO, SEQUENCE_NO, AMOUNT, CLIENT,VOUCHER_TYPE from acrtrans_hm" + agrDbLink + " ");
                requette.Append(" where account='"+account+"' ");
                requette.Append(" and apar_id='"+apar_id+"'  ");
                requette.Append(" and ext_inv_ref='"+reffact+"' ");
                requette.Append(" and client ='" + client + "' ");

                IEnumerable<REGLEMENT> result2 = DB.Database.SqlQuery<REGLEMENT>(requette.ToString());
                IQueryable<REGLEMENT> result = result2.AsQueryable();

                return result.ToList().FirstOrDefault();
            }
        }


    }
}
