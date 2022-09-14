using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Configuration;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.IO;
using Domain.appFacture;
using appFacture.Models;
using System.Web.Script.Serialization;
using System.Security.Claims;
using System.Threading;
using log4net;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using APIAGRESSO.AgressoControllers;
using APIAGRESSO.Models;


namespace appFacture.Controllers
{
    public class CommonFacturation
    {
        static ILog logger = log4net.LogManager.GetLogger("KassagrWEBLogger");
        //public static string OracleConnectionAgresso = ConfigurationManager.ConnectionStrings["Agrprod"].ConnectionString;
        public static string OracleConnectionAgresso = ConfigurationManager.ConnectionStrings["Agrtest"].ConnectionString;
        public static string agrDbLink = ConfigurationManager.AppSettings["AGRDBLINK"];
        public static string equilibeMnt=  ConfigurationManager.AppSettings["EQUILIBREMNT"];
        public static string OracleConAppFact = ConfigurationManager.ConnectionStrings["Factdb"].ConnectionString;

        public static int periodeOuverte(string client)
        {
            //--API

            AgressoControllers AgrAPI = new AgressoControllers();

            //--DB
            StringBuilder requette = new StringBuilder();
            requette.Append("SELECT PERIOD FROM ACRPERIOD where  STATUS='N' and client=:client and PERIOD=to_number(to_char(ADD_MONTHS(sysdate,-1),'RRRRMM'))");
            int periode = -1;

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleConnectionAgresso))
                {
                    conn.Open();
                    //OracleGlobalization info = conn.GetSessionInfo();
                    //info.TimeZone = "France/Paris";
                    //conn.SetSessionInfo(info);
                    using (OracleCommand cmd = new OracleCommand(requette.ToString(), conn))
                    {
                        cmd.Parameters.Add(new OracleParameter("client", client.Trim()));
                        using (OracleDataReader rd = cmd.ExecuteReader())
                        {
                            if (rd.Read())
                                periode = Int32.Parse(rd.GetValue(0).ToString());
                        }
                    }
                    conn.Close();
                    conn.Dispose();
                }
            }
            catch (Exception ex)
            {
                logger.Error("Probleme récupération periode comptabilisation; requette=" + requette.ToString());
                logger.Error("erreur=" + ex.Message);
                return -1;
            }

            if (periode != -1)
                return periode;
            else
            {
                requette = new StringBuilder();
                requette.Append("SELECT PERIOD FROM ACRPERIOD where  STATUS='N' and  client=:client and PERIOD=to_number(to_char(sysdate,'RRRRMM'))");
                try
                {
                    using (OracleConnection conn = new OracleConnection(OracleConnectionAgresso))
                    {
                        conn.Open();
                        //OracleGlobalization info = conn.GetSessionInfo();
                        //info.TimeZone = "France/Paris";
                        //conn.SetSessionInfo(info);

                        using (OracleCommand cmd = new OracleCommand(requette.ToString(), conn))
                        {
                            cmd.Parameters.Add(new OracleParameter("client", client.Trim()));
                            using (OracleDataReader rd = cmd.ExecuteReader())
                            {
                                if (rd.Read())
                                    periode = Int32.Parse(rd.GetValue(0).ToString());
                            }
                        }
                        conn.Close();
                        conn.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Probleme récupération periode comptabilisation; requette=" + requette.ToString());
                    logger.Error("erreur=" + ex.Message);
                    return -1;
                }
            }
            return periode;
        }

        public static String getTrans_type(String client, String account)
        {
            String trans_type = "";
            using (EramEntities DB = new EramEntities())
            {
                try
                {
                    using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
                    {
                        conn.Open();
                        using (OracleCommand cmd = new OracleCommand(" select account_type from aglaccounts" + agrDbLink + " where client='" + client + "' and account='" + account + "' ", conn))
                        {
                            using (OracleDataReader rd = cmd.ExecuteReader())
                            {
                                if (rd.Read())
                                    trans_type = rd.GetValue(0).ToString();
                                else
                                {
                                    logger.Error("Merci de parametrer le trans type pour le client=" + client + " et le compte=" + account);
                                    conn.Close();
                                    conn.Dispose();
                                }
                            }
                        }
                        conn.Close();
                        conn.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Probleme recuperation trans_type pour le client=" + client + " et le compte=" + account);
                    logger.Error("erreur=" + ex.Message);
                }
            }
            return trans_type;
        }

        public static String[] getListATT_ID(String client, String account)
        {
            String[] listAtt_id = new String[7] { "", "", "", "", "", "", "" };
            using (EramEntities DB = new EramEntities())
            {
                try
                {
                    StringBuilder requette = new StringBuilder();
                    requette.Append("select ATT_1_ID,ATT_2_ID,ATT_3_ID,ATT_4_ID,ATT_5_ID,ATT_6_ID,ATT_7_ID ");
                    requette.Append(" from aglaccounts" + agrDbLink + " aac, aglrules" + agrDbLink + " aru  ");
                    requette.Append(" where aac.client=aru.client and AAC.ACCOUNT_RULE=ARU.ACCOUNT_RULE ");
                    requette.Append(" and aac.account='" + account + "' and aac.client='" + client + "' and rownum=1 ");

                    logger.Error("La requette est =" + requette);
                    using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
                    {
                        conn.Open();
                        using (OracleCommand cmd = new OracleCommand(requette.ToString(), conn))
                        {
                            using (OracleDataReader rd = cmd.ExecuteReader())
                            {
                                if (rd.Read())
                                {
                                    listAtt_id[0] = rd.GetValue(0).ToString();
                                    listAtt_id[1] = rd.GetValue(1).ToString();
                                    listAtt_id[2] = rd.GetValue(2).ToString();
                                    listAtt_id[3] = rd.GetValue(3).ToString();
                                    listAtt_id[4] = rd.GetValue(4).ToString();
                                    listAtt_id[5] = rd.GetValue(5).ToString();
                                    listAtt_id[6] = rd.GetValue(6).ToString();
                                }
                                else
                                {
                                    logger.Error("Merci de parametrer les att_id pour le client=" + client + " et le compte=" + account);
                                    conn.Close();
                                    conn.Dispose();
                                }
                            }
                        }
                        conn.Close();
                        conn.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Probleme recuperation des att_id; err:" + ex.Message);
                }
            }
            return listAtt_id;
        }

        public static String getDim2(String client, String att1_id, String att2_id, String magasin)
        {
            String dim2 = "";
            using (EramEntities DB = new EramEntities())
            {
                try
                {
                    using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
                    {
                        conn.Open();
                        using (OracleCommand cmd = new OracleCommand(" select att_value from aglrelvalue" + agrDbLink + " where client='" + client + "' and attribute_id='" + att2_id + "' and rel_attr_id='" + att1_id + "' and rel_value='" + magasin + "' and rownum=1 ", conn))
                        {
                            using (OracleDataReader rd = cmd.ExecuteReader())
                            {
                                if (rd.Read())
                                    dim2 = rd.GetValue(0).ToString();
                                else
                                {
                                    dim2 = " ";
                                    //logger.Error("Merci de parametrer DIM2 pour le client=" + client + " et le magasin=" + magasin);
                                    conn.Close();
                                    conn.Dispose();
                                }
                            }
                        }
                        conn.Close();
                        conn.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Probleme recuperation DIM2 pour le client=" + client + " et le magasin=" + magasin);
                    logger.Error("erreur=" + ex.Message);
                }
            }
            return dim2;
        }

        public static String getDim2Coquille(String client, String rayon, String magasin)
        {
            if (client.Equals("S5") || client.Equals("O2") || client.Equals("S9"))
            {
                return magasin;
            }
            else
            {
                return rayon;
            }
        }


        public static String getDim4Coquille(String client, String rayon, String magasin)
        {
            return "03";
        }

        public static float Prorata_Tva(String Site, String Compte, String Date)
        {
            float Prorata = 0;
            float taux = 0, Reduction = 0;
            bool existant = true;

            //--API
            AgressoControllers AgrAPI = new AgressoControllers();
            
            //--DB
            //________________________ Connection to AGRESSO
            try
            {
                //using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
                using (OracleConnection conn = new OracleConnection(OracleConnectionAgresso))
                {
                    conn.Open();
                    using (OracleCommand cmd = new OracleCommand(Prorata_TVA(Site, Compte, Date), conn))
                    {
                        using (OracleDataReader rd = cmd.ExecuteReader())
                        {
                            if (rd.Read())
                            {
                                taux = Convert.ToSingle(rd.GetValue(0).ToString());
                                Reduction = Convert.ToSingle(rd.GetValue(1).ToString());
                            }
                            else
                            {
                                existant = false;
                                conn.Close();
                                conn.Dispose();
                            }
                        }
                    }
                    conn.Close();
                    conn.Dispose();
                }
            }
            catch (Exception ex)
            {
                logger.Error("Probleme recuperation Prorata_Tva; requette=" + Prorata_TVA(Site, Compte, Date).ToString());
                logger.Error("erreur=" + ex.Message);
            }
            //________________________ Fin connexion __________
            if (!existant)
                Prorata = -1;
            else
                Prorata = (Reduction / 100);
            return Prorata;
        }

        public static String Prorata_TVA(String site, String account, String Date)
        {
            String SQL = "";
            //DateTime Dat = DateTime.ParseExact(Date, "dd/MM/yyyy", null);
            SQL = "select VAT_PCT,REDUCTION from AGLTAXCODE where client ='" + site + "' and status ='N' and acc_vat ='" + account + "' and to_date('" + Date + "','RRRRMMDD') between date_from and date_to ";
            return SQL;
        }

        public static float Prorata(String Site, String Compte, String Date)
        {
            float Prorata = 0;
            float taux = 0, Reduction = 0;
            bool existant = true;
            //________________________ Connection to AGRESSO
            try
            {
                //using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
                using (OracleConnection conn = new OracleConnection(OracleConnectionAgresso))
                {
                    conn.Open();
                    using (OracleCommand cmd = new OracleCommand(Prorata_TVA(Site, Compte, Date), conn))
                    {
                        using (OracleDataReader rd = cmd.ExecuteReader())
                        {
                            if (rd.Read())
                            {
                                taux = Convert.ToSingle(rd.GetValue(0).ToString());
                                Reduction = Convert.ToSingle(rd.GetValue(1).ToString());
                            }
                            else
                            {
                                existant = false;
                                conn.Close();
                                conn.Dispose();
                            }
                        }
                    }
                    conn.Close();
                    conn.Dispose();
                }
            }
            catch (Exception ex)
            {
                logger.Error("Probleme recuperation Prorata; requette=" + Prorata_TVA(Site, Compte, Date).ToString());
                logger.Error("erreur=" + ex.Message);
            }
            //________________________ Fin connexion __________
            if (!existant)
                Prorata = -1;
            else
                Prorata = 1 + (taux / 100) * (1 - (Reduction / 100));
            return Prorata;
        }

        public static bool verifCnufFrs(String cnuf)
        {
            int nbrCnuf = 0;
            StringBuilder requette = new StringBuilder();
            requette.Append("select count(*) from asuheader" /*+ agrDbLink*/);
            requette.Append(" where apar_id='" + cnuf + "'");

            try
            {
                //using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
                using (OracleConnection conn = new OracleConnection(OracleConnectionAgresso))
                {
                    conn.Open();


                    using (OracleCommand cmd = new OracleCommand(requette.ToString(), conn))
                    {
                        using (OracleDataReader rd = cmd.ExecuteReader())
                        {
                            if (rd.Read())
                                nbrCnuf = Int32.Parse(rd.GetValue(0).ToString());
                            else
                            {
                                conn.Close();
                                conn.Dispose();
                            }
                        }
                    }
                    conn.Close();
                    conn.Dispose();
                }
            }
            catch (Exception ex)
            {
                logger.Error("Probleme verification CNUF fournisseur; requette=" + requette.ToString());
                logger.Error("erreur=" + ex.Message);
            }
            //}
            return (nbrCnuf != 0);
        }

        public static bool verifCnufClient(String cnuf)
        {
            int nbrCnuf = 0;
            //--API
            AgressoControllers AgrAPI = new AgressoControllers();
            
            
            APIAGRESSO.Models.fournisseur f = AgrAPI.GetFrn("", cnuf);
            //--DB
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder();
                requette.Append("select to_char(count(*)) from acuheader" /*+ agrDbLink*/);
                requette.Append(" where apar_id='" + cnuf + "'");
                try
                {
                    //using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
                    using (OracleConnection conn = new OracleConnection(OracleConnectionAgresso))
                    {
                        conn.Open();


                        using (OracleCommand cmd = new OracleCommand(requette.ToString(), conn))
                        {
                            using (OracleDataReader rd = cmd.ExecuteReader())
                            {
                                if (rd.Read())
                                    nbrCnuf = Int32.Parse(rd.GetValue(0).ToString());
                                else
                                {
                                    conn.Close();
                                    conn.Dispose();
                                }
                            }
                        }
                        conn.Close();
                        conn.Dispose();
                    }
                }
                catch (Exception ex)
                {

                    logger.Error("Probleme verification CNUF client; requette=" + requette.ToString());
                    logger.Error("erreur=" + ex.Message);
                }
            }
            return (nbrCnuf != 0);
        }
        public static bool verifEquilibre(int idfacture)
        {
            double dequilibeMnt=0.0;
            if (!Double.TryParse(equilibeMnt, out dequilibeMnt)) {
                dequilibeMnt = 1;
            }
            String reqVerif = "select sum(a.amount) from acrtrans a where a.idfacture=" + idfacture;
            double somComptes = 0.0;
            try
            {
                //using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
                using (OracleConnection conn = new OracleConnection(OracleConAppFact))
                {
                    conn.Open();
                    using (OracleCommand cmd = new OracleCommand(reqVerif, conn))
                    {
                        using (OracleDataReader rd = cmd.ExecuteReader())
                        {
                            if (rd.Read())
                            {
                                somComptes = Convert.ToSingle(rd.GetValue(0).ToString());
                            }
                            else
                            {
                                conn.Close();
                                conn.Dispose();
                            }
                        }
                    }
                    conn.Close();
                    conn.Dispose();
                }
            }
            catch (Exception ex)
            {
                logger.Error("Probleme verification Equilibre; requette=" + reqVerif);
                logger.Error("erreur=" + ex.Message);
            }

            return (somComptes < dequilibeMnt && somComptes > -1 * dequilibeMnt);
        }

        public static bool verifEquilibreOtop(int idfacture)
        {
            double dequilibeMnt = 0.0;
            if (!Double.TryParse(equilibeMnt, out dequilibeMnt))
            {
                dequilibeMnt = 1;
            }
            String reqVerif = "select sum(a.amount) from otopacrtrans a where a.idfacture=" + idfacture;
            double somComptes = 0.0;
            try
            {
                //using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
                using (OracleConnection conn = new OracleConnection(OracleConAppFact))
                {
                    conn.Open();
                    using (OracleCommand cmd = new OracleCommand(reqVerif, conn))
                    {
                        using (OracleDataReader rd = cmd.ExecuteReader())
                        {
                            if (rd.Read())
                            {
                                somComptes = Convert.ToSingle(rd.GetValue(0).ToString());
                            }
                            else
                            {
                                conn.Close();
                                conn.Dispose();
                            }
                        }
                    }
                    conn.Close();
                    conn.Dispose();
                }
            }
            catch (Exception ex)
            {
                logger.Error("Probleme verification Equilibre; requette=" + reqVerif);
                logger.Error("erreur=" + ex.Message);
            }

            return (somComptes < dequilibeMnt && somComptes > -1 * dequilibeMnt);
        }

        public static bool insererAgresso(String query)
        {
            using (OracleConnection conn = new OracleConnection(OracleConAppFact))
            {
                conn.Open();
                using (OracleTransaction trans = conn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Transaction = trans;
                        try
                        {
                            int nbIns = cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            logger.Error("Problème d'insertion dans agresso, la requette=" + query);
                            logger.Error("Error =" + ex.ToString());
                            trans.Rollback();
                            return false;
                        }
                    }
                    trans.Commit();
                }
            }
            return true;
        }


    }
}