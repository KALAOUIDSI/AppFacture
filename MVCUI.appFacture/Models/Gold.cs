using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using log4net;
using System.Net.Mail;

namespace Domain.appFacture
{
    public static class GOLD
    {
        
        private static string OracleConnectionString = ConfigurationManager.ConnectionStrings["Gold"].ConnectionString;
        //public static KeyValuePair<int, string> Process(int idSite, int idOrder)
        //{
        //    SITE aSite = new SITE();
        //    aSite = aSite.getOne(idSite);

        //    ILog logger = log4net.LogManager.GetLogger("EramGoldLogger");
        //    List<string> mailMessagesINTCDE = new List<string>();
        //    KeyValuePair<int, string> returnValue = new KeyValuePair<int, string>(0, "Erreur interne");
        //    logger.Error("Début interfaçage commande " + idOrder.ToString() + "; IdSite:" + idSite.ToString() + "; SiteGold:" + aSite.CODEGOLD.Value.ToString());
        //    try
        //    {
        //        COMMANDEACHAT ca = new COMMANDEACHAT();
        //        ca = ca.getOne(idSite, idOrder);
        //        if (!ca.DATEINTERFACAGE.HasValue)
        //        {
        //            LIGNECOMMANDEACHAT lca = new LIGNECOMMANDEACHAT();
        //            List<LIGNECOMMANDEACHAT> ListeLca = lca.getAll(idOrder);
                    
        //            string cdeext = "";
        //            string sequence = "";
        //            using (OracleConnection conn = new OracleConnection(OracleConnectionString))
        //            {
        //                conn.Open();
        //                string TVA = "20";
        //                string login = ConfigurationManager.AppSettings["loginInterfGold"];
        //                int pSite =aSite.CODEGOLD.Value; 
        //                string CNUF = ConfigurationManager.AppSettings["CNUF"];
        //                string codeFiliereFour = ConfigurationManager.AppSettings["FILIERE"];
        //                int codeFiliereClient = 0;
        //                int pclass = 0;
        //                int ptypes = 0;
        //                decimal cdeint = 0;
        //                //********* lecture de la classe de données *********************
        //                using (OracleCommand cmd = new OracleCommand("SELECT soccmag FROM SITDGENE WHERE socsite=" + pSite.ToString(), conn))
        //                {
        //                    using (OracleDataReader rd = cmd.ExecuteReader())
        //                    {
        //                        if (rd.Read())
        //                            pclass = Convert.ToInt32(rd.GetValue(0).ToString());
        //                        else
        //                        {
        //                            logger.Error("Erreur : lecture de la classe de données soccmaf de la table SITDGENE pour le site : " + pSite.ToString());
        //                            conn.Close();
        //                            conn.Dispose();
        //                            return returnValue;
        //                        }
        //                    }
        //                }
        //                //********* fin de la lecture de la classe de données *********************

        //                //********* lecture du type de site *********************
        //                using (OracleCommand cmd = new OracleCommand("SELECT NVL(parpost, 0) FROM parpostes WHERE parcmag=" + pclass.ToString() + " AND partabl=1054 AND parvan1=" + pclass.ToString() + "", conn))
        //                {
        //                    using (OracleDataReader rd = cmd.ExecuteReader())
        //                    {
        //                        if (rd.Read())
        //                            ptypes = Convert.ToInt32(rd.GetValue(0).ToString());
        //                        else
        //                        {
        //                            logger.Error("Erreur : lecture type de site pour le site : " + pSite.ToString());
        //                            conn.Close();
        //                            conn.Dispose();
        //                            return returnValue;
        //                        }
        //                    }
        //                }
        //                //********* fin de la lecture du type de site *********************

        //                //********* lecture du code filière client *********************
        //                using (OracleCommand cmd = new OracleCommand("SELECT cfinfilc FROM clifilie WHERE cfincli = TO_CHAR(" + pSite.ToString() + ")", conn))
        //                {
        //                    using (OracleDataReader rd = cmd.ExecuteReader())
        //                    {
        //                        if (rd.Read())
        //                            codeFiliereClient = Convert.ToInt32(rd.GetValue(0).ToString());
        //                        else
        //                        {
        //                            logger.Error("Erreur : lecture du code filière client pour le site : " + pSite.ToString());
        //                            conn.Close();
        //                            conn.Dispose();
        //                            return returnValue;
        //                        }
        //                    }
        //                }
        //                //*********fin de la lecture du code filière client *********************

        //                //******** Transaction ****************************
        //                using (OracleTransaction trans = conn.BeginTransaction(IsolationLevel.ReadCommitted))
        //                {
        //                    //********* lecture d'une nouvelle séquence de intcde *********************
        //                    using (OracleCommand cmd = new OracleCommand("SELECT SEQ_INTCDENSEQ.nextval FROM DUAL", conn))
        //                    {
        //                        cmd.Transaction = trans;
        //                        using (OracleDataReader rd = cmd.ExecuteReader())
        //                        {
        //                            if (rd.Read())
        //                                sequence = rd.GetValue(0).ToString();
        //                            else
        //                            {
        //                                trans.Rollback();
        //                                logger.Error("Erreur : lecture nextval de la sequence intcde (SEQ_INTCDENSEQ)");
        //                                conn.Close();
        //                                conn.Dispose();
        //                                return returnValue;
        //                            }
        //                        }
        //                    }
        //                    //********* fin de la lecture d'une nouvelle séquence de intcde *********************

        //                    //******** génération des numéros commande externes et internes ***********
        //                    OracleCommand oc = new OracleCommand();
        //                    oc.Transaction = trans;
        //                    oc.Connection = conn;
        //                    oc.CommandType = CommandType.StoredProcedure;
        //                    oc.CommandText = "PKATTRIBNCOM.Attribncom";

        //                    OracleParameter v_pclasse = new OracleParameter();
        //                    v_pclasse.ParameterName = "pclasse";
        //                    v_pclasse.OracleDbType = OracleDbType.Decimal;
        //                    v_pclasse.Direction = ParameterDirection.Input;
        //                    v_pclasse.Value = pclass; // number 3
        //                    oc.Parameters.Add(v_pclasse);

        //                    OracleParameter v_ptypef = new OracleParameter();
        //                    v_ptypef.ParameterName = "ptypef";
        //                    v_ptypef.OracleDbType = OracleDbType.Decimal;
        //                    v_ptypef.Direction = ParameterDirection.Input;
        //                    v_ptypef.Value = 3; //static
        //                    oc.Parameters.Add(v_ptypef);

        //                    OracleParameter v_ptypesite = new OracleParameter();
        //                    v_ptypesite.ParameterName = "ptypesite";
        //                    v_ptypesite.OracleDbType = OracleDbType.Decimal;
        //                    v_ptypesite.Direction = ParameterDirection.Input;
        //                    v_ptypesite.Value = ptypes;
        //                    oc.Parameters.Add(v_ptypesite);

        //                    OracleParameter v_psite = new OracleParameter();
        //                    v_psite.ParameterName = "psite";
        //                    v_psite.OracleDbType = OracleDbType.Decimal;
        //                    v_psite.Direction = ParameterDirection.Input;
        //                    v_psite.Value = pSite; //number 5
        //                    oc.Parameters.Add(v_psite);

        //                    OracleParameter v_rncdeext = new OracleParameter();
        //                    v_rncdeext.ParameterName = "rncdeext";
        //                    v_rncdeext.OracleDbType = OracleDbType.Decimal;
        //                    v_rncdeext.Direction = ParameterDirection.Output;
        //                    oc.Parameters.Add(v_rncdeext);

        //                    OracleParameter v_rncdeint = new OracleParameter();
        //                    v_rncdeint.ParameterName = "rncdeint";
        //                    v_rncdeint.OracleDbType = OracleDbType.Decimal;
        //                    v_rncdeint.Direction = ParameterDirection.Output;
        //                    oc.Parameters.Add(v_rncdeint);

        //                    try
        //                    {
        //                        oc.ExecuteNonQuery();
        //                        cdeext = oc.Parameters[4].Value.ToString();
        //                        cdeint = Convert.ToDecimal(oc.Parameters[5].Value.ToString());
        //                    }
        //                    catch (OracleException x)
        //                    {
        //                        trans.Rollback();
        //                        logger.Error("Erreur: génération des numéros commande externes et internes (PKATTRIBNCOM.Attribncom); Ex : " + x.Message);
        //                        conn.Close();
        //                        conn.Dispose();
        //                        return returnValue;
        //                    }
        //                    //******** fin de la génération des numéros commande externes et internes ***********



        //                    //********* on lit la commande et on boucle sur les produits *********************
        //                    logger.Error("Ajout des lignes de la commande " + idOrder.ToString() + " dans INTCDE");
        //                    List<string> listProduits = new List<string>();
        //                    string CONTRAT = "";
        //                    int numLigne = 1;

        //                    foreach (LIGNECOMMANDEACHAT item in ListeLca)
        //                    {
        //                        string R = item.ARTICLE.IDSERVICE.HasValue ? item.ARTICLE.getActivite(item.ARTICLE.IDSERVICE.Value).ToString() : (item.ARTICLE.IDSERIE.HasValue ? item.ARTICLE.getActivite2(item.ARTICLE.IDSERIE.Value).ToString() : "");
        //                        CONTRAT = ConfigurationManager.AppSettings["CONTRAT" + R];
        //                        string EAN = item.getEan(item.IDLIGNECOMMANDEACHAT).ToString();
        //                        string prix = "null";//(item.ARTICLE.PRIXACHAT.HasValue ? item.ARTICLE.PRIXACHAT.Value.ToString().Replace(",", ".") : "null")
        //                        //pour chaque article on lit de la table artuc les informations qui nous intéressent
        //                        string sqlLca = "select distinct aracexr, aracexta, arlcexvl, aracinl from artcoca join artuc on arccinr = aracinr join artvl on artvl.arlcinr = artuc.aracinr where arccode = '" + EAN + "'";
        //                        using (OracleCommand cmd = new OracleCommand(sqlLca, conn))
        //                        {
        //                            cmd.Transaction = trans;
        //                            using (OracleDataReader rd = cmd.ExecuteReader())
        //                            {
        //                                if (rd.Read())
        //                                {
        //                                    string aracexr = Convert.ToString(rd.GetValue(0));
        //                                    //string ararefc = rd.GetString(1);
        //                                    string aracexta = Convert.ToString(rd.GetValue(1));
        //                                    string arlcexvl = Convert.ToString(rd.GetValue(2));
        //                                    string aracinl = Convert.ToString(rd.GetValue(3));
        //                                    //insertion dans intCDE 
        //                                    string sqlIntCde = "INSERT INTO intcde (INTID,INTLCDE,INTSITE,INTCNUF,INTCCOM,INTNFILF,INTFILC,INTCONF,INTGREL,INTCOUC,INTCOM1,INTCOM2,INTENLEV,INTDCOM,INTCODE,INTRCOM,INTCEXVA,INTCEXVL,INTQTEC,INTUAUVC,INTSTAT,INTFLUX,INTLDIST,INTETAT,INTPACH,INTURG,INTFRAN,INTNSEQ,INTNLIG,INTFICH,INTDCRE,INTDMAJ,INTUTIL,INTCTVA,INTUAPP,INTALTF,INTTYPUL,INTCEXOGL,INTDLIV, INTDLIM)";
        //                                    sqlIntCde += " VALUES ('" + cdeext + "', null," + pSite.ToString() + ",'" + CNUF + "','" + CONTRAT + "'," + codeFiliereFour + "," + codeFiliereClient.ToString() + " ,0,0,0,'','" + item.COMMANDEACHAT.REFERENCE + "',0,trunc(sysdate),'" + aracexr + "','-1'," + aracexta + "," + arlcexvl + "," + (item.QUANTITECOLIS + item.QUANTITEVRAC).ToString() + ",Pkartstock.RecupCoeffUVC(" + aracinl + "),0,1,0,5," + prix + ",0,0," + sequence.ToString() + "," + numLigne.ToString() + ",'" + login + DateTime.Now.ToString() + "',trunc(sysdate),trunc(sysdate),'MJ_ERAM',decode('" + TVA + "',7,1,14,4,20,5,10,7,0,9,5),null,0,PKARTUL.getTypeUL(" + aracinl + "),null,trunc(sysdate+180),trunc(sysdate+180))";

        //                                    using (OracleCommand cmd2 = new OracleCommand(sqlIntCde, conn))
        //                                    {
        //                                        cmd2.Transaction = trans;
        //                                        try
        //                                        {
        //                                            cmd2.ExecuteNonQuery();
        //                                        }
        //                                        catch (Exception ex)
        //                                        {
        //                                            logger.Error("Erreur d'insertion sur intcde; EAN = " + EAN + "; Exception : '" + ex.ToString());
        //                                            trans.Rollback();
        //                                            conn.Close();
        //                                            conn.Dispose();
        //                                            return returnValue;
        //                                        }
        //                                    }
        //                                    numLigne++;
        //                                }
        //                                else
        //                                {
        //                                    trans.Rollback();
        //                                    logger.Error("Erreur : produit non trouvé sur Gold; EAN = " + EAN);
        //                                    conn.Close();
        //                                    conn.Dispose();
        //                                    returnValue = new KeyValuePair<int, string>(0, "Produit " + EAN + " non trouvé sur Gold");
        //                                    return returnValue;
        //                                }
        //                            }
        //                        }
        //                    }
        //                    //********* fin de la lecture de la commande *********************
        //                    trans.Commit();
        //                    //********** fin de la transaction **********************
        //                    returnValue = new KeyValuePair<int, string>(1, cdeext);

        //                }
        //                logger.Error("Fin d'ajout des lignes de la commande " + idOrder.ToString() + " dans INTCDE");

        //            }

        //            //INTCDE
        //            logger.Error("Lancement du programme psint05p");
        //            string cheminRexec = ConfigurationManager.AppSettings["cheminRexec"].ToString();
        //            string ipbingold = ConfigurationManager.AppSettings["IPBINGOLD"].ToString();

        //            string a = DateTime.Today.ToString();
        //            string jourM = a.Substring(0, 6);
        //            string ANN = a.Substring(8, 2);
        //            string dateJ = jourM + ANN;

        //            StreamWriter swrintintcde = new StreamWriter(cheminRexec + "intcde.bat");
        //            string commande = null;
        //            commande = "cd /exec/products/gcent; cd prod/env;. ./envGOLD;";
        //            commande = commande + "psint05p psint05p cenprd/cenprd  " + dateJ + " -1 " + sequence + " FR ";
        //            swrintintcde.Write(cheminRexec + "rexec "+ipbingold+" -l goldcent -p marjcent " + commande);

        //            logger.Error("cmd psint05p=" + cheminRexec + "rexec "+ipbingold+" -l goldcent -p marjcent " + commande);

        //            //commande = "cd /exec/products/gcent; cd recette/env;. ./envGOLD;";
        //            //commande = commande + "psint05p psint05p cenrec/cenrec  " + dateJ + " -1 " + sequence + " FR ";
        //            //swrintintcde.Write(cheminRexec + "rexec -d 192.168.99.96 -l goldcent -p gold08 " + commande);
        //            swrintintcde.Close();

        //            System.Diagnostics.Process ftpprocess = System.Diagnostics.Process.Start(cheminRexec + "intcde.bat");
        //            ftpprocess.WaitForExit();
        //            ftpprocess.Close();

        //            System.Threading.Thread.Sleep(5000);

        //            //vérifier s'il y a des erreur trt = 2 dans intcde afin d'envoyer un mail et ajouter un log
        //            using (OracleConnection conn = new OracleConnection(OracleConnectionString))
        //            {
        //                conn.Open();
        //                using (OracleCommand cmd = new OracleCommand("select INTCODE,INTNERR,INTMESS from INTCDE where INTSTAT = 2 and INTID = '" + cdeext + "'", conn))
        //                {
        //                    using (OracleDataReader rd = cmd.ExecuteReader())
        //                    {
        //                        while (rd.Read())
        //                        {
                                    
        //                                string codeExterne = rd.GetValue(0).ToString();
        //                                int INTNERR = Convert.ToInt32(rd.GetValue(1).ToString());
        //                                string INTMESS = rd.GetValue(2).ToString();

        //                                string strError = "Erreur sur INTCDE : Code Externe article = '" + codeExterne + "'; Num. Erreur = " + INTNERR.ToString() + "; Message = '" + INTMESS + "'";
        //                                mailMessagesINTCDE.Add(strError);
        //                                logger.Error(strError);
        //                        }
        //                    }
        //                }
        //            }


        //            if (mailMessagesINTCDE.Count > 0)
        //                sendmailINTCDE(mailMessagesINTCDE);
        //        }
        //        else
        //        {
        //            returnValue = new KeyValuePair<int, string>(0, "Commande déjà interfacée");
        //            return returnValue;
        //        }
        //    }
        //    catch (Exception except)
        //    {
        //        logger.Error("Erreur générale; Exception : '" + except.ToString());
        //    }
        //    logger.Error("Fin interfaçage commande " + idOrder.ToString());
        //    return returnValue;
        //}

        //public static void sendmailINTCDE(List<string> msg)
        //{
        //    MailMessage mail = new MailMessage("noreply@rid.com", ConfigurationManager.AppSettings["MAIL_ERROR_INTCDE_TO1"]);
        //    SmtpClient client = new SmtpClient();
        //    client.Port = 25;
        //    client.DeliveryMethod = SmtpDeliveryMethod.Network;
        //    client.UseDefaultCredentials = true;
        //    client.Host = "192.168.99.103";
        //    mail.Bcc.Add(new MailAddress(ConfigurationManager.AppSettings["MAIL_ERROR_INTCDE_TO2"]));
        //    //envoyer le mail au groupe super_si (configuré sur app_config)
        //    UTILISATEUR ut = new UTILISATEUR();
        //    List<UTILISATEUR> liste = ut.getAll(int.Parse(ConfigurationManager.AppSettings["MAIL_ERROR_TO_GROUPE"]));
        //    foreach (UTILISATEUR item in liste)
        //    {
        //        if (!string.IsNullOrWhiteSpace(item.EMAIL))
        //            mail.To.Add(new MailAddress(item.EMAIL.Trim()));
        //    }
        //    mail.Subject = "Département Eram : Erreurs lors de l'insertion dans INTCDE";
        //    string body = string.Join(System.Environment.NewLine, msg.ToArray());
        //    mail.Body = body;
        //    client.Send(mail);


        //}
    }
}
