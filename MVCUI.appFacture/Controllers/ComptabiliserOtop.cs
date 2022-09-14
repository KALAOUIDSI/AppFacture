using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Domain.appFacture;
using appFacture.Models;
using System.Web.Script.Serialization;
using System.Security.Claims;
using System.Threading;
using Newtonsoft.Json;
using log4net;
using Domain.appFacture.Rapports;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Text;
using System.Globalization;
using AGRESSOAPI;

namespace appFacture.Controllers
{
    public class ComptabiliserOtop
    {
        static ILog logger = log4net.LogManager.GetLogger("KassagrWEBLogger");
        public static string agrDbLink = ConfigurationManager.AppSettings["AGRDBLINK"];
        public static string OracleConnectionString = ConfigurationManager.ConnectionStrings["Factdb"].ConnectionString;
        //public static string OracleConnectionAgresso = ConfigurationManager.ConnectionStrings["Agrprod"].ConnectionString;
        public static string OracleConnectionAgresso = ConfigurationManager.ConnectionStrings["Agrtest"].ConnectionString;

        public static string textiBayId = ConfigurationManager.AppSettings["IDENTIFENSEIGNE"];
        

        private const string compteBidon = "99999999";
        static List<String> rapportGereration = new List<String>();

        public static void genererBrouillard()
        {
            logger.Info("Début génération brouillard");
            rapportGereration.Clear();
            rapportGereration.Add("Début génération brouillard");
            OTOPFACTURE ff = new OTOPFACTURE();

            long nolot = ff.getSequence("NLOT_SEQ");
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);

            List<OTOPFACTURE> factures = ff.facturesAGenerer();

            foreach (OTOPFACTURE facture in factures)
            {

                logger.Info("Début traitement facture N°=" + facture.REFERENCEFACT + " - id=" + facture.IDDEMANDE);
                rapportGereration.Add("***** Début traitement facture N°=" + facture.REFERENCEFACT + " - id=" + facture.IDDEMANDE + " ***** ");
                int seq = -1;

                if (!inserVendeur(facture.IDDEMANDE, out seq, nolot))
                {
                    facture.deleteAcrtrans(facture.IDDEMANDE);
                    continue;
                }

                /*
                 //Reglement
                 seq = -1;
                if (!inserAcheteur(facture.IDDEMANDE, seq, nolot))
                {
                    facture.deleteAcrtrans(facture.IDDEMANDE);
                    continue;
                }*/

                facture.acomptabiliser(facture.IDDEMANDE, UTILISATEURCONNECTE);

                logger.Info("Fin traitement facture N°=" + facture.REFERENCEFACT + " - id=" + facture.IDDEMANDE);
                rapportGereration.Add("***** Fin traitement facture N°=" + facture.REFERENCEFACT + " - id=" + facture.IDDEMANDE + " ***** ");
            }

            logger.Info("Fin génération brouillard");
            rapportGereration.Add("Fin génération brouillard");

            string body = string.Join("-NEWLINE-", rapportGereration.ToArray());
            FACRAPPORTGEN rapport = new FACRAPPORTGEN();
            rapport.insererFichier(UTILISATEURCONNECTE, body);
        }


        public static bool inserVendeur(int id, out int seq, long nolot)
        {
            seq = -1;
            OTOPFACTURE facture = new OTOPFACTURE();
            facture = facture.getOne(id);
            AGR bc = new AGR();
            FACTENSEIGNE ens = new FACTENSEIGNE();
            ens = ens.getOne(Int32.Parse(textiBayId));

            bc.IDFACTURE = facture.IDDEMANDE;
            bc.NOLOT = (int)nolot;


            bc.DIM_1 = "14";//N° site
            bc.DIM_4 = " ";

            //construire libelle facture
            bc.DESCRIPTION = facture.OTOPACHETEUR.LIBELLESITE + "-" + facture.REFERENCEFACT + " (" + facture.LIBELLEDEMANDE + ")";

            //bc.DESCRIPTION = ens.LIBELLEENSEIGNE + "-" + " (" + facture.LIBELLEDEMANDE + ")"; //a construire
            bc.CLIENT = ens.CODE;

            bc.FISCAL_YEAR = Int32.Parse(facture.DATECREATION.Value.ToString("yyyy"));

            bc.LAST_UPDATE = DateTime.Now;
            bc.PERIOD = CommonFacturation.periodeOuverte(bc.CLIENT); //gePeriodeOuverte(bc.CLIENT, anneeFiscal); // Int32.Parse(facture.DATECREATION.Value.ToString("yyyyMM"));
            if (bc.PERIOD == -1)
            {
                logger.Info("Problème communication avec Agresso ou aucune période ouverte pour la société :" + ens.CODE);
                rapportGereration.Add("Ko : Problème communication avec Agresso ou aucune période ouverte pour la société :" + ens.CODE);
                return false;
            }

            bc.TRANS_DATE = facture.DATECREATION.Value;

            bc.VOUCHER_DATE = DateTime.Now;
            bc.VOUCHER_TYPE = "GF";

            Int64 counter = 0;
            int trans_id = 0;

            if (!getListInfosClient(ens.CODE, out counter, out trans_id))
            {
                logger.Info("Ko: Probleme recuperation infos client :" + ens.CODE + " depuis Agresso, merci de vérifier la communication avec Agresso");
                rapportGereration.Add("Ko: Probleme recuperation infos client :" + ens.CODE + " depuis Agresso, merci de vérifier la communication avec Agresso");
                return false;
            }

            bc.VOUCHER_NO = counter;
            bc.TRANS_ID = trans_id;
            bc.EXT_INV_REF = facture.REFERENCEFACT;

            if (counter == 0)
            {
                logger.Info("Merci de parametrer le voucher_no sur Agresso pour la société :" + ens.CODE);
                rapportGereration.Add("Ko : Le voucher_no n'est pas paramétré sur Agresso pour la société :" + ens.CODE);
                return false;
            }

            FACTASSOGFACTTYPEENSEIGNE assoFE = ens.getOneAssoFactEns(facture.IDFACTTYPE.Value, ens.IDENSEIGNE);
            if (assoFE == null)
            {
                logger.Info("Merci de parametrer les comptes de comptabilisation !!!");
                rapportGereration.Add("Ko : Merci de parametrer les comptes de comptabilisation pour le chapitre de cette facture !!!");
                return false;
            }
            bool isok = true;
            /*****************************************Début Vendeur*******************************************************/
            bc.APAR_ID = facture.OTOPACHETEUR.CNUF;

            string compteipprrf = assoFE.COMPTEVENDTVAIPPRF;

            //La ligne TVA
            if (facture.MNTTVA != 0)
            {
                List<TOTAUXTVA> totauxtva = facture.calcultotauxtv(facture.IDDEMANDE);
                foreach (TOTAUXTVA ligneTva in totauxtva)
                {
                    if (ligneTva.MNTVA == 0)
                        continue;
                    if (ligneTva.TAUXTVA == 20)
                    {
                        if (assoFE.COMPTEVENDTVA20 == null || assoFE.COMPTEVENDTVA20.Length <= 7)
                        {
                            logger.Info("Compte TVA20 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                            rapportGereration.Add("Ko : Compte TVA20 est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                            isok = false;
                            return isok;
                        }
                        bc.ACCOUNT = assoFE.COMPTEVENDTVA20;
                    }
                    if (ligneTva.TAUXTVA == 10)
                    {
                        if (assoFE.COMPTEVENDTVA10 == null || assoFE.COMPTEVENDTVA10.Length <= 7)
                        {
                            logger.Info("Compte TVA10 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                            rapportGereration.Add("Ko : Compte TVA10 est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                            isok = false;
                            return isok;
                        }
                        bc.ACCOUNT = assoFE.COMPTEVENDTVA10;
                    }
                    if (ligneTva.TAUXTVA == 14)
                    {
                        if (assoFE.COMPTEVENDTVA14 == null || assoFE.COMPTEVENDTVA14.Length <= 7)
                        {
                            logger.Info("Compte TVA14 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                            rapportGereration.Add("Ko : Compte TVA14 est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                            isok = false;
                            return isok;
                        }
                        bc.ACCOUNT = assoFE.COMPTEVENDTVA14;
                    }

                    if (ligneTva.TAUXTVA == 7)
                    {
                        if (assoFE.COMPTEVENDTVA7 == null || assoFE.COMPTEVENDTVA7.Length <= 7)
                        {
                            logger.Info("Compte TVA7 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                            rapportGereration.Add("Ko : Compte TVA7 est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                            isok = false;
                            return isok;
                        }
                        bc.ACCOUNT = assoFE.COMPTEVENDTVA7;
                    }

                    if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                    {
                        bc.AMOUNT = -1 * Convert.ToDouble(ligneTva.MNTVA);
                        bc.DC_FLAG = 1;
                    }
                    else
                    {
                        bc.AMOUNT = -1 * Convert.ToDouble(ligneTva.MNTVA);
                        bc.DC_FLAG = -1;
                    }

                    seq = seq + 1;
                    bc.SEQUENCE_NO = seq;

                    bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                    String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);

                    bc.ATT_1_ID = att_ids[0];
                    bc.ATT_2_ID = att_ids[1];
                    bc.ATT_3_ID = att_ids[2];
                    bc.ATT_4_ID = att_ids[3];
                    bc.ATT_5_ID = att_ids[4];
                    bc.ATT_6_ID = att_ids[5];
                    bc.ATT_7_ID = att_ids[6];

                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, bc.DIM_1);

                    if (isok)
                    {
                        try
                        {
                            if (!facture.insert_temp(bc, "OTOPACRTRANS"))
                                return false;
                        }
                        catch (Exception e)
                        {
                            logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                            rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                            return false;
                        }
                    }
              }
            }


            //La ligne HT
            if (facture.MNTHT != 0)
            {
                if (assoFE.COMPTEVENDEUR == null || assoFE.COMPTEVENDEUR.Length <= 7)
                {
                    logger.Info("Compte client est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte client est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEVENDEUR;

                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];
                bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, bc.DIM_1);

                List<TOTAUXTVA> mntrayon = facture.calculhtbyrayon(facture.IDDEMANDE);

                foreach (TOTAUXTVA htrayon in mntrayon) {
                    if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                    {
                        bc.AMOUNT = -1 * Convert.ToDouble(htrayon.MNTHT);
                        bc.DC_FLAG = 1;
                    }
                    else
                    {
                        bc.DC_FLAG = -1;
                        bc.AMOUNT = -1 * Convert.ToDouble(htrayon.MNTHT);
                    }

                    seq = seq + 1;
                    bc.SEQUENCE_NO = seq;
                    bc.DIM_4 = htrayon.TAUXTVA.ToString();

                    if (isok)
                    {
                        try
                        {
                            if (!facture.insert_temp(bc, "OTOPACRTRANS"))
                                return false;
                        }
                        catch (Exception e)
                        {
                            logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                            rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                            return false;
                        }
                    }
              }
            }
            double mntIpprrf = 0.0;
            //La ligne IPPRRF
            if (compteipprrf != null && compteipprrf.Length > 0 && compteipprrf != compteBidon)
            {
                if (facture.MNTHT != 0)
                {
                    bc.ACCOUNT = assoFE.COMPTEVENDTVAIPPRF;

                    if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                    {
                        bc.AMOUNT = Convert.ToDouble(facture.MNTHT) * 0.2;
                        bc.DC_FLAG = -1;
                    }
                    else
                    {
                        bc.DC_FLAG = 1;
                        bc.AMOUNT = Convert.ToDouble(facture.MNTHT) * 0.2;
                    }

                    mntIpprrf = bc.AMOUNT;

                    seq = seq + 1;
                    bc.SEQUENCE_NO = seq;
                    bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                    String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                    bc.ATT_1_ID = att_ids[0];
                    bc.ATT_2_ID = att_ids[1];
                    bc.ATT_3_ID = att_ids[2];
                    bc.ATT_4_ID = att_ids[3];
                    bc.ATT_5_ID = att_ids[4];
                    bc.ATT_6_ID = att_ids[5];
                    bc.ATT_7_ID = att_ids[6];
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, bc.DIM_1);
                    bc.DIM_4 = " ";
                    if (isok)
                    {
                        try
                        {
                            if (!facture.insert_temp(bc, "OTOPACRTRANS"))
                                return false;
                        }
                        catch (Exception e)
                        {
                            logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                            rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                            return false;
                        }
                    }
                }
            }
            //La ligne TTC
            if (facture.MNTTTC != 0)
            {
                if (assoFE.COMPTEVENDCLIENT == null || assoFE.COMPTEVENDCLIENT.Length <= 7)
                {
                    logger.Info("Compte vendeur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte vendeur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEVENDCLIENT;

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.AMOUNT = Convert.ToDouble(facture.MNTTTC);
                    bc.DC_FLAG = -1;
                }
                else
                {
                    bc.DC_FLAG = 1;
                    bc.AMOUNT = Convert.ToDouble(facture.MNTTTC);
                }

                bc.AMOUNT = bc.AMOUNT - mntIpprrf;

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];

                bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, bc.DIM_1);
                bc.DIM_4 = " ";

                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "OTOPACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }

            return isok;
        }

        public static bool inserAcheteur(int id, int seq, long nolot)
        {
            OTOPFACTURE facture = new OTOPFACTURE();
            facture = facture.getOne(id);

            AGR bc = new AGR();
            FACTENSEIGNE ens = new FACTENSEIGNE();
            ens = ens.getOne(int.Parse(textiBayId)); //l acheteur

            bc.IDFACTURE = facture.IDDEMANDE;
            bc.NOLOT = (int)nolot;

            bc.ATT_1_ID = "0P";
            bc.ATT_2_ID = "0O";
            bc.ATT_4_ID = "0G";

            bc.DIM_1 = "14";//N° site


            bc.DIM_4 =" ";


            //construire libelle facture
            bc.DESCRIPTION = facture.REFERENCEFACT + " (" + facture.LIBELLEDEMANDE + ")";


            //bc.DESCRIPTION = facture.LIBELLEDEMANDE; //a construire
            bc.CLIENT = ens.CODE;

            bc.FISCAL_YEAR = Int32.Parse(facture.DATECREATION.Value.ToString("yyyy"));

            bc.LAST_UPDATE = DateTime.Now;

            bc.PERIOD = CommonFacturation.periodeOuverte(bc.CLIENT); //gePeriodeOuverte(bc.CLIENT, anneeFiscal); // Int32.Parse(facture.DATECREATION.Value.ToString("yyyyMM"));
            if (bc.PERIOD == -1)
            {
                logger.Info("Problème communication avec Agresso ou aucune période ouverte pour la société :" + ens.CODE);
                rapportGereration.Add("Ko : Problème communication avec Agresso ou aucune période ouverte pour la société :" + ens.CODE);
                return false;
            }


            bc.TRANS_DATE = facture.DATECREATION.Value;
            bc.VOUCHER_DATE = DateTime.Now;

            bc.VOUCHER_TYPE = "GF";

            Int64 counter = 0;
            int trans_id = 0;
            if (!getListInfosClient(ens.CODE, out counter, out trans_id))
            {
                logger.Info("Ko: Probleme recuperation infos client :" + ens.CODE + " depuis Agresso, merci de vérifier la communication avec Agresso");
                rapportGereration.Add("Ko: Probleme recuperation infos client :" + ens.CODE + " depuis Agresso, merci de vérifier la communication avec Agresso");
                return false;
            }
            bc.VOUCHER_NO = counter;
            bc.TRANS_ID = trans_id;
            bc.EXT_INV_REF = facture.REFERENCEFACT;

            if (counter == 0)
            {
                logger.Info("Merci de parametrer le voucher_no sur Agresso pour la société :" + ens.CODE);
                rapportGereration.Add("Ko : Merci de parametrer le voucher_no sur Agresso pour la société :" + ens.CODE);
                return false;
            }

            FACTASSOGFACTTYPEENSEIGNE assoFE = ens.getOneAssoFactEns(facture.IDFACTTYPE.Value, ens.IDENSEIGNE);
            if (assoFE == null)
            {
                logger.Info("Merci de parametrer les comptes de comptabilisation !!!");
                rapportGereration.Add("Ko : Merci de parametrer les comptes de comptabilisation !!!");
                return false;
            }
            bool isok = true;
            /*****************************************Début Vendeur*******************************************************/
            bc.APAR_ID = facture.OTOPACHETEUR.CNUF;

            string compteIpprrf = assoFE.COMPTEACHETTVAIPPRF;
            double mntipprrf = 0.0;
            //La ligne IPPRRF
            if (compteIpprrf != null && compteIpprrf.Length > 0 && compteIpprrf != compteBidon)
            {
                if (facture.MNTHT != 0)
                {
                    bc.ACCOUNT = assoFE.COMPTEACHETTVAIPPRF;

                    if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                    {
                        bc.DC_FLAG = 1;
                        bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTHT) * 0.2;
                    }
                    else
                    {
                        bc.DC_FLAG = -1;
                        bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTHT) * 0.2;
                    }
                    mntipprrf = bc.AMOUNT;

                    seq = seq + 1;
                    bc.SEQUENCE_NO = seq;
                    bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                    String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                    bc.ATT_1_ID = att_ids[0];
                    bc.ATT_2_ID = att_ids[1];
                    bc.ATT_3_ID = att_ids[2];
                    bc.ATT_4_ID = att_ids[3];
                    bc.ATT_5_ID = att_ids[4];
                    bc.ATT_6_ID = att_ids[5];
                    bc.ATT_7_ID = att_ids[6];
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, bc.DIM_1);

                    if (isok)
                    {
                        try
                        {
                            if (!facture.insert_temp(bc, "OTOPACRTRANS"))
                                return false;
                        }
                        catch (Exception e)
                        {
                            logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                            rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                            return false;
                        }
                    }
                }
            }


            //Reglement 1           
            if (facture.MNTTTC != 0)
            {
                if (assoFE.COMPTEACHETEUR == null || assoFE.COMPTEACHETEUR.Length <= 7)
                {
                    logger.Info("Compte acheteur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte acheteur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEACHETEUR;

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.DC_FLAG = 1;
                }
                else
                {
                    bc.DC_FLAG = - 1;
                }

                bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTTTC) - mntipprrf; 

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];

                bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, bc.DIM_1);

                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "OTOPACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }

            //Reglement 2
            bc.APAR_ID = " ";
            if (facture.MNTTTC != 0)
            {
                if (assoFE.COMPTEACHETFRS == null || assoFE.COMPTEACHETFRS.Length <= 7)
                {
                    logger.Info("Compte fournisseur de l'acheteur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte fournisseur de l'acheteur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEACHETFRS;

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.DC_FLAG = -1;
                }
                else
                {
                    bc.DC_FLAG = 1;
                }

                bc.AMOUNT =  Convert.ToDouble(facture.MNTTTC);

                bc.AMOUNT = bc.AMOUNT - mntipprrf;

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];
      
                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "OTOPACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }

            return isok;
        }

        public static bool executerRequette(String req)
        {
            using (EramEntities DB = new EramEntities())
            {
                using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
                {
                    conn.Open();
                    using (OracleCommand cmd = new OracleCommand(req, conn))
                    {
                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            logger.Error("La requette est :" + req + "; Error =" + ex.ToString());
                            return false;
                        }
                    }
                    conn.Close();
                    conn.Dispose();
                }
            }
            return true;
        }

        public static bool getListInfosClient(String client, out Int64 counter, out int trans_id)
        {
            counter = 0;
            trans_id = 0;
            StringBuilder requette = new StringBuilder();
                try
                {   //" + agrDbLink + "
                    //" + agrDbLink + "
                    requette.Append("select counter,trans_id from acrtransgr, acrvouchtype ");
                    requette.Append(" where ACRTRANSGR.VOUCH_SERIES=ACRVOUCHTYPE.VOUCH_SERIES and ACRTRANSGR.client=ACRVOUCHTYPE.client ");
                    requette.Append(" and ACRTRANSGR.client='" + client + "' and ACRVOUCHTYPE.VOUCHER_TYPE='GF' and rownum=1");

                    //logger.Error("La requette est =" + requette);

                
                using (OracleConnection conn = new OracleConnection(OracleConnectionAgresso))
                {
                        conn.Open();
                        using (OracleCommand cmd = new OracleCommand(requette.ToString(), conn))
                        {
                            using (OracleDataReader rd = cmd.ExecuteReader())
                            {
                                if (rd.Read())
                                {
                                    counter = Convert.ToInt64(rd.GetValue(0));
                                    trans_id = Convert.ToInt32(rd.GetValue(1));
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
                    logger.Error("Probleme recuperation infos client getListInfosClient =" + ex.Message + "La requette est =" + requette.ToString());
                    return false;
                }
            
            if (counter != 0)
            {
                if (!executerRequette((new FACTFACTURE()).updateCounter(client, counter + 1)))
                {
                    logger.Info("Impossible de mettre à jour le counter(voucher_no) sur Agresso, merci de vérifier la plage !!!");
                    rapportGereration.Add("Ko : Impossible de mettre à jour le counter(voucher_no) sur Agresso, merci de vérifier la plage !!!");
                    return false;
                }
            }
            return true;
        }

        public static void integrer(){
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);

            OTOPFACTURE fact = new OTOPFACTURE();
            List<OTOPFACTURE> listAcomptabilise = fact.facturesAIntegrer(UTILISATEURCONNECTE);
            rapportGereration.Clear();
            rapportGereration.Add("Début intégration brouillard");

            foreach (OTOPFACTURE facture in listAcomptabilise)
            {
                string reqInsert = requetteInsertAgr(facture.IDDEMANDE);
                if (CommonFacturation.verifEquilibreOtop(facture.IDDEMANDE))
                {
                    facture.updateAcrtransLastUpdate(facture.IDDEMANDE);
                    if (CommonFacturation.insererAgresso(reqInsert))
                    {
                        facture.comptabiliser(facture.IDDEMANDE, UTILISATEURCONNECTE);
                    }
                    else
                    {
                        rapportGereration.Add("Problème communication avec Agresso ou Problème d'insertion dans agresso, la requette=" + reqInsert);
                    }
                }
                else
                    rapportGereration.Add("Une pièce comptable de la facture " + facture.REFERENCEFACT + " est non équilibrée !");
            }

            rapportGereration.Add("Fin intégration brouillard");

            string body = string.Join("-NEWLINE-", rapportGereration.ToArray());
            FACRAPPORTGEN rapport = new FACRAPPORTGEN();
            rapport.insererFichier(UTILISATEURCONNECTE, body);
        }

        private static string requetteInsertAgr(int idfacture)
        {
            StringBuilder requette = new StringBuilder();

            requette.Append("insert into acrtrans" + agrDbLink + " ( "); //acrtrans@AGRPROD
            requette.Append("  accept_status  , ");
            requette.Append("  account        , ");
            requette.Append("  account2       , ");
            requette.Append("  address        , ");
            requette.Append("  allocation_key , ");
            requette.Append("  amount         , ");
            requette.Append("  apar_id        , ");
            requette.Append("  apar_name      , ");
            requette.Append("  apar_type      , ");
            requette.Append("  arrival_date   , ");
            requette.Append("  arrive_id      , ");
            requette.Append("  att_1_id       , ");
            requette.Append("  att_2_id       , ");
            requette.Append("  att_3_id       , ");
            requette.Append("  att_4_id       , ");
            requette.Append("  att_5_id       , ");
            requette.Append("  att_6_id       , ");
            requette.Append("  att_7_id       , ");
            requette.Append("  bank_account   , ");
            requette.Append("  base_amount    , ");
            requette.Append("  base_curr      , ");
            requette.Append("  clearing_code  , ");
            requette.Append("  client         , ");
            requette.Append("  client_ref     , ");
            requette.Append("  collection     , ");
            requette.Append("  commitment     , ");
            requette.Append("  complaint      , ");
            requette.Append("  compress_flag  , ");
            requette.Append("  contract_order , ");
            requette.Append("  cur_amount     , ");
            requette.Append("  curr_doc       , ");
            requette.Append("  curr_licence   , ");
            requette.Append("  currency       , ");
            requette.Append("  dc_flag        , ");
            requette.Append("  description    , ");
            requette.Append("  dim_1          , ");
            requette.Append("  dim_2          , ");
            requette.Append("  dim_3          , ");
            requette.Append("  dim_4          , ");
            requette.Append("  dim_5          , ");
            requette.Append("  dim_6          , ");
            requette.Append("  dim_7          , ");
            requette.Append("  disc_date      , ");
            requette.Append("  disc_percent   , ");
            requette.Append("  discount       , ");
            requette.Append("  due_date       , ");
            requette.Append("  exch_rate      , ");
            requette.Append("  exch_rate2     , ");
            requette.Append("  exch_rate3     , ");
            requette.Append("  ext_inv_ref    , ");
            requette.Append("  factor_short   , ");
            requette.Append("  fiscal_year    , ");
            requette.Append("  header_flag    , ");
            requette.Append("  int_status     , ");
            requette.Append("  intrule_id     , ");
            requette.Append("  kid            , ");
            requette.Append("  last_update    , ");
            requette.Append("  line_no        , ");
            requette.Append("  number_1       , ");
            requette.Append("  order_id       , ");
            requette.Append("  pay_currency   , ");
            requette.Append("  pay_flag       , ");
            requette.Append("  pay_method     , ");
            requette.Append("  pay_transfer   , ");
            requette.Append("  period         , ");
            requette.Append("  period_no      , ");
            requette.Append("  place          , ");
            requette.Append("  province       , ");
            requette.Append("  pseudo_id      , ");
            requette.Append("  reg_amount     , ");
            requette.Append("  rem_level      , ");
            requette.Append("  responsible    , ");
            requette.Append("  rev_period     , ");
            requette.Append("  sequence_no    , ");
            requette.Append("  sequence_ref   , ");
            requette.Append("  sequence_ref2  , ");
            requette.Append("  status         , ");
            requette.Append("  swift          , ");
            requette.Append("  tax_code       , ");
            requette.Append("  tax_id         , ");
            requette.Append("  tax_system     , ");
            requette.Append("  template_type  , ");
            requette.Append("  terms_id       , ");
            requette.Append("  trans_date     , ");
            requette.Append("  trans_id       , ");
            requette.Append("  trans_type     , ");
            requette.Append("  treat_code     , ");
            requette.Append("  user_id        , ");
            requette.Append("  value_1        , ");
            requette.Append("  value_2        , ");
            requette.Append("  value_3        , ");
            requette.Append("  vat_amount     , ");
            requette.Append("  vat_reg_no     , ");
            requette.Append("  vouch_stat     , ");
            requette.Append("  voucher_date   , ");
            requette.Append("  voucher_no     , ");
            requette.Append("  voucher_ref    , ");
            requette.Append("  voucher_ref2   , ");
            requette.Append("  voucher_type   , ");
            requette.Append("  zip_code       , ");
            requette.Append("  auth_code      , ");
            requette.Append("  base_value_2   , ");
            requette.Append("  base_value_3   , ");
            requette.Append("  contract_id    , ");
            requette.Append("  orig_reference ) ");
            requette.Append("  select  ");
            requette.Append("    m.accept_status  , ");
            requette.Append("  m.account        , ");
            requette.Append("  m.account2       , ");
            requette.Append("  m.address        , ");
            requette.Append("  m.allocation_key , ");
            requette.Append("  m.amount         , ");
            requette.Append("  m.apar_id        , ");
            requette.Append("  m.apar_name      , ");
            requette.Append("  m.apar_type      , ");
            requette.Append("  m.arrival_date   , ");
            requette.Append("  m.arrive_id      , ");
            requette.Append("  m.att_1_id       , ");
            requette.Append("  m.att_2_id       , ");
            requette.Append("  m.att_3_id       , ");
            requette.Append("  m.att_4_id       , ");
            requette.Append("  m.att_5_id       , ");
            requette.Append("  m.att_6_id       , ");
            requette.Append("  m.att_7_id       , ");
            requette.Append("  m.bank_account   , ");
            requette.Append("  m.base_amount    , ");
            requette.Append("  m.base_curr      , ");
            requette.Append("  m.clearing_code  , ");
            requette.Append("  m.client         , ");
            requette.Append("  m.client_ref     , ");
            requette.Append("  m.collection     , ");
            requette.Append("  m.commitment     , ");
            requette.Append("  m.complaint      , ");
            requette.Append("  m.compress_flag  , ");
            requette.Append("  m.contract_order , ");
            requette.Append("  m.cur_amount     , ");
            requette.Append("  m.curr_doc       , ");
            requette.Append("  m.curr_licence   , ");
            requette.Append("  m.currency       , ");
            requette.Append("  m.dc_flag        , ");
            requette.Append("  m.description    , ");
            requette.Append("  m.dim_1          , ");
            requette.Append("  m.dim_2          , ");
            requette.Append("  m.dim_3          , ");
            requette.Append("  m.dim_4          , ");
            requette.Append("  m.dim_5          , ");
            requette.Append("  m.dim_6          , ");
            requette.Append("  m.dim_7          , ");
            requette.Append("  m.disc_date      , ");
            requette.Append("  m.disc_percent   , ");
            requette.Append("  m.discount       , ");
            requette.Append("  m.due_date       , ");
            requette.Append("  m.exch_rate      , ");
            requette.Append("  m.exch_rate2     , ");
            requette.Append("  m.exch_rate3     , ");
            requette.Append("  m.ext_inv_ref    , ");
            requette.Append("  m.factor_short   , ");
            requette.Append("  m.fiscal_year    , ");
            requette.Append("  m.header_flag    , ");
            requette.Append("  m.int_status     , ");
            requette.Append("  m.intrule_id     , ");
            requette.Append("  m.kid            , ");
            requette.Append("  m.last_update    , ");
            requette.Append("  m.line_no        , ");
            requette.Append("  m.number_1       , ");
            requette.Append("  m.order_id       , ");
            requette.Append("  m.pay_currency   , ");
            requette.Append("  m.pay_flag       , ");
            requette.Append("  m.pay_method     , ");
            requette.Append("  m.pay_transfer   , ");
            requette.Append("  m.period         , ");
            requette.Append("  m.period_no      , ");
            requette.Append("  m.place          , ");
            requette.Append("  m.province       , ");
            requette.Append("  m.pseudo_id      , ");
            requette.Append("  m.reg_amount     , ");
            requette.Append("  m.rem_level      , ");
            requette.Append("  m.responsible    , ");
            requette.Append("  m.rev_period     , ");
            requette.Append("  m.sequence_no    , ");
            requette.Append("  m.sequence_ref   , ");
            requette.Append("  m.sequence_ref2  , ");
            requette.Append("  m.status         , ");
            requette.Append("  m.swift          , ");
            requette.Append("  m.tax_code       , ");
            requette.Append("  m.tax_id         , ");
            requette.Append("  m.tax_system     , ");
            requette.Append("  m.template_type  , ");
            requette.Append("  m.terms_id       , ");
            requette.Append("  m.trans_date     , ");
            requette.Append("  m.trans_id       , ");
            requette.Append("  m.trans_type     , ");
            requette.Append("  m.treat_code     , ");
            requette.Append("  m.user_id        , ");
            requette.Append("  m.value_1        , ");
            requette.Append("  m.value_2        , ");
            requette.Append("  m.value_3        , ");
            requette.Append("  m.vat_amount     , ");
            requette.Append("  m.vat_reg_no     , ");
            requette.Append("  m.vouch_stat     , ");
            requette.Append("  m.voucher_date   , ");
            requette.Append("  m.voucher_no     , ");
            requette.Append("  m.voucher_ref    , ");
            requette.Append("  m.voucher_ref2   , ");
            requette.Append("  m.voucher_type   , ");
            requette.Append("  m.zip_code       , ");
            requette.Append("  m.auth_code      , ");
            requette.Append("  base_value_2   , ");
            requette.Append("  m.base_value_3   , ");
            requette.Append("  m.contract_id    , ");
            requette.Append("  m.orig_reference  ");
            requette.Append("  from otopacrtrans m,otopfacture f ");
            requette.Append("  where f.iddemande ='" + idfacture + "'");
            requette.Append(" AND m.idfacture=f.iddemande ");
            requette.Append(" AND f.status = 2 ");

            logger.Error("requetteInsertAgr =" + requette.ToString());
            return requette.ToString();
        }
	 

    }
}