using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using System.Data.Entity;

namespace Domain.appFacture
{
    public partial class FACTTYPE
    {
        void OnCreated()
        {
        }

        public string IDCAT
        {
            get
            {
                int cat = (this.IDCATEGORIE.HasValue ? this.IDCATEGORIE.Value : -1);
                return this.IDFACTTYPE + "," + cat;
            }
        }

        public FACTTYPE(int id, string value)
        {
            this.IDFACTTYPE = id;
            this.LIBELLE = value;
        }

        public List<FACTTYPE> getAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                IQueryable<FACTTYPE> result = from c in DB.FACTTYPE
                                            orderby c.LIBELLE 
                                            select c;
                return result.Include("FACTDEMANDE").Include("FACTASSOGFACTTYPEENSEIGNE.FACTENSEIGNE").ToList();
            }
        }

        public List<FACTTYPE> getAllOfUser(int idUser)
        {
            using (EramEntities DB = new EramEntities())
            {
                IQueryable<FACTTYPE> result = from c in DB.FACTTYPE
                                              where c.FACTUSER.Any(u => u.IDUTILISATEUR == idUser)
                                              orderby c.LIBELLE
                                              select c;
                return result.Include("FACTDEMANDE").Include("FACTASSOGFACTTYPEENSEIGNE.FACTENSEIGNE").ToList();
            }
        }

        public List<FACTTYPE> getTypeExport(int idUser)
        {
            using (EramEntities DB = new EramEntities())
            {
                IQueryable<FACTTYPE> result = from c in DB.FACTTYPE
                                              where c.FACTUSER.Any(u => u.IDUTILISATEUR == idUser) && c.IDCATEGORIE==6
                                              orderby c.LIBELLE
                                              select c;
                return result.Include("FACTASSOGFACTTYPEENSEIGNE.FACTENSEIGNE").ToList();
            }
        }

        public List<FACTTYPE> getAll(long IdDemande)
        {
            using (EramEntities DB = new EramEntities())
            {
                IQueryable<FACTTYPE> result = from c in DB.FACTTYPE
                                              where c.FACTDEMANDE.Any(u => u.IDDEMANDE == IdDemande)
                                            select c;
                return result.ToList();
            }
        }

        public int countAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTTYPE
                             select c;
                return result.Count();
            }
        }
        public FACTTYPEPaginationRes getAll(int page, int pageSize, string search, int sortby, Boolean isasc, FACTTYPEAdvFiltre AdvFiltre)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTTYPEPaginationRes res = new FACTTYPEPaginationRes();
                IQueryable<FACTTYPE> result = (from c in DB.FACTTYPE
                                               select c).Include("FACTDEMANDE").Include("FACTASSOGFACTTYPEENSEIGNE.FACTENSEIGNE");
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    int searchid;
                    if (int.TryParse(search, out searchid))
                        result = from c in result where c.LIBELLE.ToLower().Contains(search.Trim()) || c.IDFACTTYPE == searchid select c;
                    else
                        result = from c in result where c.LIBELLE.ToLower().Contains(search.Trim()) select c;
                }

                //recherche avancée
                if (!string.IsNullOrWhiteSpace(AdvFiltre.libelle))
                    result = result.Where(c => c.LIBELLE.ToLower().Contains(AdvFiltre.libelle.ToLower()));

                if (!string.IsNullOrWhiteSpace(AdvFiltre.compte))
                    result = result.Where(c => c.COMPTE.ToLower().Contains(AdvFiltre.compte.ToLower()));

                switch (sortby)
                {
                    case 1:
                        result = isasc ? result.OrderBy(c => c.LIBELLE) : result.OrderByDescending(c => c.LIBELLE);
                        break;
                    case 0:
                        result = result.OrderByDescending(c => c.IDFACTTYPE);
                        break;
                }
                res.count = result.Count();
                res.listTYPE = result
                .Skip((page - 1) * pageSize)
                .Take(pageSize).Include("FACTDEMANDE").ToList();

                return res;
            }
        }

        

        public FACTTYPE getOne(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTTYPE FACTTYPE = (from c in DB.FACTTYPE
                                 where c.IDFACTTYPE == id
                                     select c).Include("FACTDEMANDE").Include("FACTASSOGFACTTYPEENSEIGNE").Include("FACTTYPECATEGORIE").Include("FACTASSOGFACTTYPEENSEIGNE.FACTENSEIGNE").First();
                return FACTTYPE;
            }
        }

        public FACTTYPE getOneByCat(int idcat)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTTYPE FACTTYPE = (from c in DB.FACTTYPE
                                     where c.IDCATEGORIE == idcat
                                     select c).First();
                return FACTTYPE;
            }
        }


        public List<FACTTYPECATEGORIE> getAllCategories()
        {
            using (EramEntities DB = new EramEntities())
            {
                List<FACTTYPECATEGORIE> listCategories = (from c in DB.FACTTYPECATEGORIE
                                     select c).OrderBy(c=>c.IDCATEGORIE).ToList();
                return listCategories;
            }
        }

        public FACTTYPE update(int IDFACTTYPE, string LIBELLEFACTTYPE,int IDCATEGORIE, string COMPTE, int UTILISATEURCONNECTE)
        {
            FACTTYPE FACTTYPE = new FACTTYPE();
            if (IDFACTTYPE != 0)
            {
                //modification
                using (EramEntities DB = new EramEntities())
                {
                    FACTTYPE = (from c in DB.FACTTYPE
                                             where c.IDFACTTYPE == IDFACTTYPE
                                     select c).First();
                    FACTTYPE.LIBELLE = LIBELLEFACTTYPE;
                    FACTTYPE.COMPTE = "";
                    FACTTYPE.IDCATEGORIE = (short)IDCATEGORIE;
                    FACTTYPE.DATEMODIFICATION = DateTime.Now;
                    FACTTYPE.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                    
                    DB.Entry(FACTTYPE).State = System.Data.EntityState.Modified;
                    
                    try
                    {
                        DB.SaveChanges();
                    }
                    catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                    {
                        Exception raise = dbEx;
                        foreach (var validationErrors in dbEx.EntityValidationErrors)
                        {
                            foreach (var validationError in validationErrors.ValidationErrors)
                            {
                                string message = string.Format("{0}:{1}",
                                    validationErrors.Entry.Entity.ToString(),
                                    validationError.ErrorMessage);
                                // raise a new exception nesting
                                // the current instance as InnerException
                                raise = new InvalidOperationException(message, raise);
                            }
                        }
                        throw raise;
                    }
                }

            }
            else
            {
                using (EramEntities DB = new EramEntities())
                {
                    //DB.Configuration.ValidateOnSaveEnabled = false;
                    FACTTYPE = new FACTTYPE();

                    FACTTYPE.LIBELLE = LIBELLEFACTTYPE;
                    FACTTYPE.IDCATEGORIE = (short)IDCATEGORIE;
                    FACTTYPE.DATECREATION = DateTime.Now;
                    FACTTYPE.DATEMODIFICATION = DateTime.Now;
                    FACTTYPE.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                    DB.FACTTYPE.Add(FACTTYPE);
                    DB.SaveChanges();
                }
            }
            return FACTTYPE;
        }

        public void delete(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTTYPE FACTTYPE = new FACTTYPE();
                FACTTYPE = (from c in DB.FACTTYPE
                          where c.IDFACTTYPE == id
                          select c).First();
                if (FACTTYPE.FACTDEMANDE.Count == 0)
                {
                    FACTTYPE.FACTASSOGFACTTYPEENSEIGNE.Clear();
                    DB.Entry(FACTTYPE).State = System.Data.EntityState.Deleted;
                    DB.SaveChanges();
                }
                else
                {
                    throw new Exception("Suppression impossible, des demandes utilisent ce TYPE FACTURE !");
                }

            }
        }
        public void updateLienEnsFactTypeV(int IDFACTTYPE, int IDENSEIGNE,
            String vcompte, String vtva20, String vtva14, String vtva10, String vtva7, String vipprf, String vcomptecli,string vtsc,string vtsctaxe,
            String acompte, String atva20, String atva14, String atva10, String atva7, String aipprf, String acomptefrs)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTASSOGFACTTYPEENSEIGNE LIENENSFACTTYPE = (from c in DB.FACTASSOGFACTTYPEENSEIGNE
                                     where c.IDFACTTYPE == IDFACTTYPE && c.IDENSEIGNE == IDENSEIGNE
                                     select c).FirstOrDefault();
                if (LIENENSFACTTYPE != null)
                {
                    LIENENSFACTTYPE.COMPTEVENDEUR = vcompte;
                    LIENENSFACTTYPE.COMPTEVENDTVA20 = vtva20;
                    LIENENSFACTTYPE.COMPTEVENDTVA14 = vtva14;
                    LIENENSFACTTYPE.COMPTEVENDTVA10 = vtva10;
                    LIENENSFACTTYPE.COMPTEVENDTVA7 = vtva7;
                    LIENENSFACTTYPE.COMPTEVENDTVAIPPRF = vipprf;
                    LIENENSFACTTYPE.COMPTEVENDCLIENT = vcomptecli;
                    LIENENSFACTTYPE.COMPTEVENDTSC = vtsc;
                    LIENENSFACTTYPE.COMPTEVENDTSCTAX = vtsctaxe;


                    LIENENSFACTTYPE.COMPTEACHETEUR = acompte;
                    LIENENSFACTTYPE.COMPTEACHETTVA20 = atva20;
                    LIENENSFACTTYPE.COMPTEACHETTVA14 = atva14;
                    LIENENSFACTTYPE.COMPTEACHETTVA10 = atva10;
                    LIENENSFACTTYPE.COMPTEACHETTVA7 = atva7;
                    LIENENSFACTTYPE.COMPTEACHETTVAIPPRF = aipprf;
                    LIENENSFACTTYPE.COMPTEACHETFRS = acomptefrs;

                    DB.Entry(LIENENSFACTTYPE).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();
                }
                else {
                    LIENENSFACTTYPE = new FACTASSOGFACTTYPEENSEIGNE();
                    LIENENSFACTTYPE.IDENSEIGNE = IDENSEIGNE;
                    LIENENSFACTTYPE.IDFACTTYPE = IDFACTTYPE;

                    LIENENSFACTTYPE.COMPTEVENDEUR = vcompte;
                    LIENENSFACTTYPE.COMPTEVENDTVA20 = vtva20;
                    LIENENSFACTTYPE.COMPTEVENDTVA14 = vtva14;
                    LIENENSFACTTYPE.COMPTEVENDTVA10 = vtva10;
                    LIENENSFACTTYPE.COMPTEVENDTVA7 = vtva7;
                    LIENENSFACTTYPE.COMPTEVENDTVAIPPRF = vipprf;
                    LIENENSFACTTYPE.COMPTEVENDCLIENT = vcomptecli;
                    LIENENSFACTTYPE.COMPTEVENDTSC = vtsc;
                    LIENENSFACTTYPE.COMPTEVENDTSCTAX = vtsctaxe;

                    LIENENSFACTTYPE.COMPTEACHETEUR = acompte;
                    LIENENSFACTTYPE.COMPTEACHETTVA20 = atva20;
                    LIENENSFACTTYPE.COMPTEACHETTVA14 = atva14;
                    LIENENSFACTTYPE.COMPTEACHETTVA10 = atva10;
                    LIENENSFACTTYPE.COMPTEACHETTVA7 = atva7;
                    LIENENSFACTTYPE.COMPTEACHETTVAIPPRF = aipprf;
                    LIENENSFACTTYPE.COMPTEACHETFRS = acomptefrs;

                    DB.FACTASSOGFACTTYPEENSEIGNE.Add(LIENENSFACTTYPE);
                    DB.SaveChanges();
                }
            }
        }


    }
}
