using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using System.Data.Entity;

namespace Domain.appFacture
{
    public partial class FACTIMPORTXLSX
    {
        void OnCreated()
        {
        }

        public FACTIMPORTXLSX()
        {
        }

        public FACTIMPORTXLSX(int id, string value)
        {
            this.IDFICHIER = id;
            this.NOMFICHIER = value;
        }

        public List<FACTIMPORTXLSX> getAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTIMPORTXLSX
                             select c;
                return result.ToList();
            }
        }

        public List<FACTIMPORTXLSX> getAll(int status)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTIMPORTXLSX
                             where c.STATUS == 0 || c.STATUS ==null
                             select c;
                return result.ToList();
            }
        }

        public int countAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTIMPORTXLSX
                             select c;
                return result.Count();
            }
        }

        public FACTIMPORTXLSXPaginationRes getAll(int page, int pageSize, string search, int sortby, Boolean isasc, FACTIMPORTXLSXAdvFiltre AdvFiltre)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTIMPORTXLSXPaginationRes res = new FACTIMPORTXLSXPaginationRes();
                var result = (from c in DB.FACTIMPORTXLSX
                              select c);

                //result.OrderBy(c => c.IDFICHIER);
                //result.OrderByDescending(c => c.IDFICHIER);

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    result = from c in result where c.NOMFICHIER.ToLower().Contains(search)  select c;
                }

                //recherche avancée
                if (!string.IsNullOrWhiteSpace(AdvFiltre.nomfichier))
                    result = result.Where(c => c.NOMFICHIER.ToLower().Contains(AdvFiltre.nomfichier.ToLower()));

                if (!string.IsNullOrWhiteSpace(AdvFiltre.dateCrationDeb))
                {
                    DateTime DATECRATION_OUT_DEB;
                    if (DateTime.TryParseExact(AdvFiltre.dateCrationDeb, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DATECRATION_OUT_DEB))
                        result = result.Where(c => c.DATECREATION >= DATECRATION_OUT_DEB);
                }

                if (!string.IsNullOrWhiteSpace(AdvFiltre.dateCrationFin))
                {
                    DateTime DATECRATION_OUT_FIN;
                    if (DateTime.TryParseExact(AdvFiltre.dateCrationFin, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DATECRATION_OUT_FIN))
                        result = result.Where(c => c.DATECREATION <= DATECRATION_OUT_FIN);
                }
                if (!string.IsNullOrWhiteSpace(AdvFiltre.status))
                {
                    if (AdvFiltre.status == "0")
                        result = result.Where(c => c.STATUS == 0);
                    if (AdvFiltre.status == "1")
                        result = result.Where(c => c.STATUS == 1);
                }

                switch (sortby)
                {
                    case 0:
                        result = isasc ? result.OrderBy(c => c.IDFICHIER) : result.OrderByDescending(c => c.IDFICHIER);
                        break;
                    case 1:
                        result = isasc ? result.OrderBy(c => c.NOMFICHIER) : result.OrderByDescending(c => c.NOMFICHIER);
                        break;
                    case 2:
                        result = isasc ? result.OrderBy(c => c.DATECREATION) : result.OrderByDescending(c => c.DATECREATION);
                        break;
                    case 3:
                        result = isasc ? result.OrderBy(c => c.STATUS) : result.OrderByDescending(c => c.STATUS);
                        break;
                }
                res.count = result.Count();
                res.listIMPORTXLSX = result
                .Skip((page - 1) * pageSize)
                .Take(pageSize).ToList();

                return res;
            }
        }

        public FACTIMPORTXLSX getOne(long id)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTIMPORTXLSX
                             where c.IDFICHIER == id
                             select c;

                return result.First();

            }
        }

        public FACTIMPORTXLSX update(long IDFICHIER, string NOMFICHIER, string nomfichierinter, bool STATUS, int nbrlignesintegrees, int nbrlignesrejetees, int? IDUTILISATEUR = null)
        {
            FACTIMPORTXLSX Fichier = new FACTIMPORTXLSX();
            if (IDFICHIER != 0)
            {

                //modification
                using (EramEntities DB = new EramEntities())
                {

                    Fichier = Fichier.getOne(IDFICHIER);
                    Fichier.DATEMODIFICATION = DateTime.Now;
                    Fichier.NOMFICHIER = NOMFICHIER;
                    if (STATUS)
                        Fichier.STATUS = 1;
                    else
                        Fichier.STATUS = 0;
                    Fichier.NBRLIGNESINTEGREE = nbrlignesintegrees;
                    Fichier.NBRLIGNESREJETEE = nbrlignesrejetees;
                    if (nomfichierinter != null && nomfichierinter.Length > 0)
                    {
                        Fichier.NOMFICHINTERNE = nomfichierinter;
                    }
                    //Fichier.NBRTOTALE = (nbrcmdcolis + nbrcmdvrac);
                    Fichier.DERNIERUTILISATEUR = IDUTILISATEUR;

                    DB.Entry(Fichier).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();

                }

            }
            else
            {

                using (EramEntities DB = new EramEntities())
                {
                    DB.Configuration.ValidateOnSaveEnabled = false;
                    Fichier.DATECREATION = DateTime.Now;
                    Fichier.NOMFICHIER = NOMFICHIER;
                    if (STATUS)
                        Fichier.STATUS = 1;
                    else
                        Fichier.STATUS = 0;

                    Fichier.NBRLIGNESINTEGREE = 0;
                    Fichier.NBRLIGNESREJETEE = 0;
                   // Fichier.NBRTOTALE = 0;

                    Fichier.DERNIERUTILISATEUR = IDUTILISATEUR;
                    DB.FACTIMPORTXLSX.Add(Fichier);
                    DB.SaveChanges();
                }
            }
            return Fichier;
        }

        public void updateStatus(long IDFICHIER, short status)
        {
            FACTIMPORTXLSX Fichier = new FACTIMPORTXLSX();
            //modification
            using (EramEntities DB = new EramEntities())
            {

                Fichier = Fichier.getOne(IDFICHIER);
                Fichier.DATEMODIFICATION = DateTime.Now;
                Fichier.STATUS = status;


                DB.Entry(Fichier).State = System.Data.EntityState.Modified;
                DB.SaveChanges();
            }
        }

        public void updateDerniereExec(int IDSITE,DateTime Dernieredate)
        {
                using (EramEntities DB = new EramEntities())
                {
                    FACTSITE Site = new FACTSITE();
                    Site = Site.getOne(IDSITE);
                    Site.DATEDERNIEREEXECUTION = Dernieredate;
                    DB.Entry(Site).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();
                }
        }


        public void delete(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTIMPORTXLSX Fichier = new FACTIMPORTXLSX();
                Fichier = (from c in DB.FACTIMPORTXLSX
                           where c.IDFICHIER == id
                           select c).FirstOrDefault();
                DB.Entry(Fichier).State = System.Data.EntityState.Deleted;
                DB.SaveChanges();
            }
        }
        //public static INTDETCMDPaginationRes getAllArticleRejete(int idFichier, int page, int pageSize, string search, int sortby, Boolean isasc, INTDETCMDAdvFiltre AdvFiltre)
        //{

        //    using (EramEntities DB = new EramEntities())
        //    {
        //        INTDETCMDPaginationRes res = new INTDETCMDPaginationRes();
        //        var result = (from c in DB.INTDETCMD
        //                      where c.IDFICHIER == idFichier
        //                      && c.SATUT == 2
        //                      select c).Include("FACTIMPORTXLSX").Include("ARTICLE").Include("ARTICLETAILLE.TAILLE");

        //        result.OrderBy(c => c.IDINTDETCMD);

        //        if (!string.IsNullOrEmpty(search))
        //        {
        //            search = search.ToLower();

        //            long searchOut;
        //            if (long.TryParse(search, out searchOut))
        //                result = from c in result where c.CONDITIONNEMENT.Equals(search) || c.ARTICLE.LIBELLEARTICLE.ToLower().Contains(search) || c.ARTICLE.REFNSI == searchOut || c.ARTICLETAILLE.TAILLE.LIBELLELONGTAILLE.ToLower().Contains(search) || c.MOTIF_REJET.ToLower().Contains(search) || c.IDINTDETCMD == searchOut select c;
        //            else
        //                result = from c in result where c.CONDITIONNEMENT.Equals(search) || c.ARTICLE.LIBELLEARTICLE.ToLower().Contains(search) || c.ARTICLETAILLE.TAILLE.LIBELLELONGTAILLE.ToLower().Contains(search) || c.MOTIF_REJET.ToLower().Contains(search) select c;
        //        }

        //        //recherche avancée
        //        if (!string.IsNullOrWhiteSpace(AdvFiltre.idintdetcmd))
        //        {
        //            long idintdetcmdOut;
        //            if (long.TryParse(AdvFiltre.idintdetcmd, out idintdetcmdOut))
        //                result = result.Where(c => c.IDINTDETCMD == idintdetcmdOut);
        //        }

        //        if (!string.IsNullOrWhiteSpace(AdvFiltre.conditionnement))
        //        {
        //            if (AdvFiltre.conditionnement == "c")
        //                result = result.Where(c => c.CONDITIONNEMENT.Equals("c"));
        //            if (AdvFiltre.conditionnement == "v")
        //                result = result.Where(c => c.CONDITIONNEMENT.Equals("v"));
        //        }

        //        if (!string.IsNullOrWhiteSpace(AdvFiltre.article))
        //        {
        //            result = result.Where(c => c.ARTICLE.LIBELLEARTICLE.ToLower().Contains(AdvFiltre.article.ToLower()));
        //        }

        //        if (!string.IsNullOrWhiteSpace(AdvFiltre.refnsi))
        //        {
        //            long refnsiOut;
        //            if (long.TryParse(AdvFiltre.refnsi, out refnsiOut))
        //                result = result.Where(c => c.ARTICLE.REFNSI == refnsiOut);
        //        }

        //        if (!string.IsNullOrWhiteSpace(AdvFiltre.taille))
        //        {
        //            result = result.Where(c => c.ARTICLETAILLE.TAILLE.LIBELLELONGTAILLE.ToLower().Contains(AdvFiltre.taille.ToLower()));
        //        }

        //        if (!string.IsNullOrWhiteSpace(AdvFiltre.numligne))
        //        {
        //            int numligneOut;
        //            if (int.TryParse(AdvFiltre.numligne, out numligneOut))
        //                result = result.Where(c => c.NUMLIGNE == numligneOut);
        //        }

        //        if (!string.IsNullOrWhiteSpace(AdvFiltre.numcolonne))
        //        {
        //            int numcolonneOut;
        //            if (int.TryParse(AdvFiltre.numcolonne, out numcolonneOut))
        //                result = result.Where(c => c.NUMCOLONNE == numcolonneOut);
        //        }

        //        if (!string.IsNullOrWhiteSpace(AdvFiltre.motifRejet))
        //        {
        //            result = result.Where(c => c.MOTIF_REJET.ToLower().Contains(AdvFiltre.motifRejet.ToLower()));
        //        }

        //        switch (sortby)
        //        {
        //            case 0:
        //                result = isasc ? result.OrderBy(c => c.IDINTDETCMD) : result.OrderByDescending(c => c.IDINTDETCMD);
        //                break;
        //            case 1:
        //                result = isasc ? result.OrderBy(c => c.CONDITIONNEMENT) : result.OrderByDescending(c => c.CONDITIONNEMENT);
        //                break;
        //            case 2:
        //                result = isasc ? result.OrderBy(c => c.ARTICLE.LIBELLEARTICLE) : result.OrderByDescending(c => c.ARTICLE.LIBELLEARTICLE);
        //                break;
        //            case 3:
        //                result = isasc ? result.OrderBy(c => c.ARTICLE.REFNSI) : result.OrderByDescending(c => c.ARTICLE.REFNSI);
        //                break;
        //            case 4:
        //                result = isasc ? result.OrderBy(c => c.ARTICLETAILLE.TAILLE.LIBELLELONGTAILLE) : result.OrderByDescending(c => c.ARTICLETAILLE.TAILLE.LIBELLELONGTAILLE);
        //                break;
        //            case 5:
        //                result = isasc ? result.OrderBy(c => c.NUMLIGNE) : result.OrderByDescending(c => c.NUMLIGNE);
        //                break;
        //            case 6:
        //                result = isasc ? result.OrderBy(c => c.NUMCOLONNE) : result.OrderByDescending(c => c.NUMCOLONNE);
        //                break;
        //            case 7:
        //                result = isasc ? result.OrderBy(c => c.MOTIF_REJET) : result.OrderByDescending(c => c.MOTIF_REJET);
        //                break;
        //        }
        //        res.count = result.Count();
        //        res.listINTDETCMD = result
        //        .Skip((page - 1) * pageSize)
        //        .Take(pageSize).ToList();

        //        return res;
        //    }
        //}
    
    
    }
}
