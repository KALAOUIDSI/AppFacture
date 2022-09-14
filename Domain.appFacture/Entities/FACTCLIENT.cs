using System;
using System.Text;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using System.Data.Entity;

namespace Domain.appFacture
{
    public partial class FACTCLIENT
    {
        void OnCreated()
        {
        }

        public FACTCLIENT(int id, string value)
        {
            this.IDCLIENT = id;
            this.DESIGNATIONCLIENT = value;
        }

        public List<FACTCLIENT> getAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTCLIENT
                             select c;
                return result.ToList();
            }
        }

        public List<FACTCLIENT> getAllNonRepeat()
        {
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder();

                requette.Append(" select min(c.idclient) idclient,trim(upper(c.referenceclient)) referenceclient, ");
                requette.Append(" trim(upper(c.designationclient)) designationclient,c.iceclient  ");
                
                requette.Append(" ,'' adrfactclient,'' status, ");
                requette.Append(" -1 dernierutilisateur, ");
                requette.Append(" trunc(sysdate) datecreation, ");
                requette.Append(" trunc(sysdate) datemodification ");

                requette.Append(" from factclient c  ");
                requette.Append(" group by trim(upper(c.referenceclient)),trim(upper(c.designationclient)),c.iceclient ");
                requette.Append(" order by  designationclient asc");

                IEnumerable<FACTCLIENT> result = DB.Database.SqlQuery<FACTCLIENT>(requette.ToString());

                return result.ToList();
            }
        }

        public int countAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTCLIENT
                             select c;
                return result.Count();
            }
        }

        public FACTCLIENT getOne(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTCLIENT
                             where c.IDCLIENT == id
                             select c;

                return result.First();

            }
        }

        public FACTCLIENT getOneByRef(string refer)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTCLIENT
                             where c.REFERENCECLIENT == refer
                             select c;
                return result.First();
            }
        }

        public void update(int IDCLIENT,string REFCLIENT, string LIBELLECLIENT,string ICECLIENT,string adrfacturation, int IDUTILISATEUR)
        {
            if (IDCLIENT != 0)
            {
                //modification
                using (EramEntities DB = new EramEntities())
                {
                    FACTCLIENT Client = new FACTCLIENT();
                    Client = Client.getOne(IDCLIENT);
                    Client.DATEMODIFICATION = DateTime.Now;
                    Client.REFERENCECLIENT = (!String.IsNullOrEmpty(REFCLIENT) ? REFCLIENT.Trim() : REFCLIENT);
                    Client.DESIGNATIONCLIENT = (!String.IsNullOrEmpty(LIBELLECLIENT) ? LIBELLECLIENT.Trim() : LIBELLECLIENT);
                    Client.ICECLIENT = ICECLIENT;
                    Client.ADRFACTCLIENT = (!String.IsNullOrEmpty(ADRFACTCLIENT) ? ADRFACTCLIENT.Trim() : ADRFACTCLIENT);
                    Client.DERNIERUTILISATEUR = IDUTILISATEUR;

                    DB.Entry(Client).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();
                }

            }
            else
            {

                using (EramEntities DB = new EramEntities())
                {
                    DB.Configuration.ValidateOnSaveEnabled = false;
                    FACTCLIENT Client = new FACTCLIENT();
                    Client.DATEMODIFICATION = DateTime.Now;
                    Client.DATECREATION = DateTime.Now;
                    Client.REFERENCECLIENT = (!String.IsNullOrEmpty(REFCLIENT) ? REFCLIENT.Trim() : REFCLIENT);
                    Client.DESIGNATIONCLIENT = (!String.IsNullOrEmpty(LIBELLECLIENT) ? LIBELLECLIENT.Trim() : LIBELLECLIENT);
                    Client.ICECLIENT = ICECLIENT;
                    Client.ADRFACTCLIENT = (!String.IsNullOrEmpty(ADRFACTCLIENT) ? ADRFACTCLIENT.Trim() : ADRFACTCLIENT);
                    Client.DERNIERUTILISATEUR = IDUTILISATEUR;
                    DB.FACTCLIENT.Add(Client);
                    DB.SaveChanges();
                }
            }
        }

        public void delete(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTCLIENT Client = new FACTCLIENT();
                Client = (from c in DB.FACTCLIENT
                        where c.IDCLIENT == id
                        select c).FirstOrDefault();
                //on vérifie que le client n'est pas utilisé ailleurs
                if (!(Client.FACTDEMANDE.Count > 0))
                {
                    DB.Entry(Client).State = System.Data.EntityState.Deleted;
                    DB.SaveChanges();
                }
                else
                {
                    throw new Exception("Impossible de supprimer ce client, il est utilisé par des demandes !");
                }
            }
        }
    }
}
