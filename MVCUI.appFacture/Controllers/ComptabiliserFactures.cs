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

namespace appFacture.Controllers
{
    public class ComptabiliserFactures
    {
        static ILog logger = log4net.LogManager.GetLogger("KassagrWEBLogger");
        public static string agrDbLink = ConfigurationManager.AppSettings["AGRDBLINK"];
        public static string OracleConnectionString = ConfigurationManager.ConnectionStrings["Factdb"].ConnectionString;
        //public static string OracleConnectionAgresso = ConfigurationManager.ConnectionStrings["Agrprod"].ConnectionString;
        public static string OracleConnectionAgresso = ConfigurationManager.ConnectionStrings["Agrtest"].ConnectionString;

        private const string compteBidon = "99999999";
        static List<String> rapportGereration = new List<String>();

        public static void genererBrouillard()
        {
            logger.Info("Début génération brouillard");
            rapportGereration.Clear();
            rapportGereration.Add("Début génération brouillard");
            FACTFACTURE ff = new FACTFACTURE();

            long nolot = ff.getSequence("NLOT_SEQ");
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);

            List<FACTFACTURE> factures = ff.facturesAGenerer(UTILISATEURCONNECTE);
            foreach (FACTFACTURE facture in factures)
            {

                logger.Info("Début traitement facture N°=" + facture.REFERENCEFACT + " - id=" + facture.IDFACTURE);
                rapportGereration.Add("***** Début traitement facture N°=" + facture.REFERENCEFACT + " - id=" + facture.IDFACTURE + " ***** ");
                int seq = -1;

                if (!inserVendeur(facture.IDFACTURE, out seq, nolot))
                {
                    facture.deleteAcrtrans(facture.IDFACTURE);
                    continue;
                }
                if (facture.TYPECLIENT == 1)
                {
                    seq = -1;
                    if (!inserAcheteur(facture.IDFACTURE, seq, nolot))
                    {
                        facture.deleteAcrtrans(facture.IDFACTURE);
                        continue;
                    }
                }

                facture.acomptabiliser(facture.IDFACTURE, UTILISATEURCONNECTE);

                logger.Info("Fin traitement facture N°=" + facture.REFERENCEFACT + " - id=" + facture.IDFACTURE);
                rapportGereration.Add("***** Fin traitement facture N°=" + facture.REFERENCEFACT + " - id=" + facture.IDFACTURE + " ***** ");
            }

            logger.Info("Fin génération brouillard");
            rapportGereration.Add("Fin génération brouillard");

            string body = string.Join("-NEWLINE-", rapportGereration.ToArray());
            FACRAPPORTGEN rapport = new FACRAPPORTGEN();
            rapport.insererFichier(-1, body);
        }


        public static bool inserVendeur(int id, out int seq, long nolot)
        {
            seq = -1;
            FACTFACTURE facture = new FACTFACTURE();
            facture = facture.getOne(id);
            AGR bc = new AGR();
            FACTENSEIGNE ens = new FACTENSEIGNE();
            ens = ens.getOne(facture.FACTSITE1.IDENSEIGNE.Value);

            bc.IDFACTURE = facture.IDFACTURE;
            bc.NOLOT = (int)nolot;


            bc.DIM_1 = facture.FACTSITE1.CODEAGRESSO;//N° site
            //bc.DIM_2 = "DIM_2"; //TO DO
            string rayonvendeur = (facture.RAYON.HasValue ? facture.RAYON.Value.ToString() : " ");
            bc.DIM_4 = (facture.RAYON.HasValue ? facture.RAYON.Value.ToString() : " ");

            //construire libelle facture
            if (facture.TYPECLIENT == 1)
            {
                FACTENSEIGNE ens2 = new FACTENSEIGNE();
                ens2 = ens2.getOne(facture.FACTSITE.IDENSEIGNE.Value);

                bc.DESCRIPTION = ens.LIBELLEENSEIGNE + "-" + ens2.LIBELLEENSEIGNE + "-" + facture.REFERENCEFACT + " (" + facture.LIBELLEDEMANDE + ")";
            }
            else
            {
                bc.DESCRIPTION = ens.LIBELLEENSEIGNE + "-" + facture.REFERENCEFACT + " (" + facture.LIBELLEDEMANDE + ")";
            }

            //bc.DESCRIPTION = ens.LIBELLEENSEIGNE + "-" + " (" + facture.LIBELLEDEMANDE + ")"; //a construire
            bc.CLIENT = ens.CODE;

            int anneeFiscal = -1;
            if (facture.ANNEEPRESTATION != null && facture.ANNEEPRESTATION.Length > 0)
            {
                bc.FISCAL_YEAR = Int32.Parse(facture.ANNEEPRESTATION);
                anneeFiscal = bc.FISCAL_YEAR;
            }
            else
            {
                bc.FISCAL_YEAR = Int32.Parse(facture.DATECREATION.Value.ToString("yyyy"));
            }

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
            bc.APAR_ID = facture.CNUFACHETEUR;

            if (!(ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB")))
            {
                bc.DIM_2 = CommonFacturation.getDim2Coquille(ens.CODE, rayonvendeur, facture.FACTSITE1.CODEAGRESSO);
                bc.DIM_1 = "3100";
                bc.DIM_4 = "03";
            }

            string compteipprrf = assoFE.COMPTEVENDTVAIPPRF;

            //La ligne TVA
            if (facture.MNTTVA != 0)
            {
                List<TOTAUXTVA> totauxtva = facture.calcultotauxtv(facture.IDFACTURE);
                TOTAUXTVA ligneTva = totauxtva[0];
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

                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE1.CODEAGRESSO);
                }
                if (isok)
                {
                    if (!facture.insert_temp(bc, "ACRTRANS"))
                        return false;
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

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTHT);
                    bc.DC_FLAG = 1;
                }
                else
                {
                    bc.DC_FLAG = -1;
                    bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTHT);
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
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE1.CODEAGRESSO);
                }
                if (isok)
                {
                    if (!facture.insert_temp(bc, "ACRTRANS"))
                        return false;
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
                    if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB"))
                    {
                        bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE1.CODEAGRESSO);
                    }
                    if (isok)
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
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
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, bc.DIM_1);
                }


                if (isok)
                {
                    if (!facture.insert_temp(bc, "ACRTRANS"))
                        return false;
                }
            }

            return isok;
        }

        public static bool inserAcheteur(int id, int seq, long nolot)
        {
            FACTFACTURE facture = new FACTFACTURE();
            facture = facture.getOne(id);

            AGR bc = new AGR();
            FACTENSEIGNE ens = new FACTENSEIGNE();
            ens = ens.getOne(facture.FACTSITE.IDENSEIGNE.Value); //l acheteur

            bc.IDFACTURE = facture.IDFACTURE;
            bc.NOLOT = (int)nolot;
            string comptetva = "";

            if (ens.CODE.Equals("AC"))
            {
                bc.ATT_1_ID = "0I";
                bc.ATT_2_ID = "0O";
                bc.ATT_4_ID = "0J";
            }
            else
            {
                bc.ATT_1_ID = "0P";
                bc.ATT_2_ID = "0O";
                bc.ATT_4_ID = "0G";
            }

            bc.DIM_1 = facture.FACTSITE.CODEAGRESSO;//N° site


            //bc.DIM_4 = (facture.RAYON.HasValue ? facture.RAYON.Value.ToString() : "");
            string rayonacheteur = (facture.RAYONACHETEUR.HasValue ? facture.RAYONACHETEUR.Value.ToString() : " ");
            bc.DIM_4 = (facture.RAYONACHETEUR.HasValue ? facture.RAYONACHETEUR.Value.ToString() : " ");


            //construire libelle facture
            FACTENSEIGNE ens2 = new FACTENSEIGNE();
            ens2 = ens2.getOne(facture.FACTSITE1.IDENSEIGNE.Value); //le vendeur
            bc.DESCRIPTION = ens2.LIBELLEENSEIGNE + "-" + ens.LIBELLEENSEIGNE + "-" + facture.REFERENCEFACT + " (" + facture.LIBELLEDEMANDE + ")";


            //bc.DESCRIPTION = facture.LIBELLEDEMANDE; //a construire
            bc.CLIENT = ens.CODE;
            int anneeFiscal = -1;
            if (facture.ANNEEPRESTATION != null && facture.ANNEEPRESTATION.Length > 0)
            {
                bc.FISCAL_YEAR = Int32.Parse(facture.ANNEEPRESTATION);
                anneeFiscal = bc.FISCAL_YEAR;
            }
            else
            {
                bc.FISCAL_YEAR = Int32.Parse(facture.DATECREATION.Value.ToString("yyyy"));
            }

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
            bc.APAR_ID = facture.CNUFVENDEUR;


            if (!(ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB")))
            {
                bc.DIM_2 = CommonFacturation.getDim2Coquille(ens.CODE, rayonacheteur, facture.FACTSITE.CODEAGRESSO);
                bc.DIM_1 = "3100";
                bc.DIM_4 = "03";
            }

            string compteIpprrf = assoFE.COMPTEACHETTVAIPPRF;
            double mntipprrf = 0.0;
            //La ligne TVA
            if (facture.MNTTVA != 0)
            {
                List<TOTAUXTVA> totauxtva = facture.calcultotauxtv(facture.IDFACTURE);
                TOTAUXTVA ligneTva = totauxtva[0];
                if (ligneTva.TAUXTVA == 20)
                {
                    //if (assoFE.COMPTEACHETTVA20 == null || assoFE.COMPTEACHETTVA20.Length <= 7)
                    //{
                    //    logger.Info("Compte acheteur TVA20 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    rapportGereration.Add("Ko : Compte acheteur TVA20 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    isok = false;
                    //    return isok;
                    //}
                    bc.ACCOUNT = assoFE.COMPTEACHETTVA20;
                }
                if (ligneTva.TAUXTVA == 10)
                {
                    //if (assoFE.COMPTEACHETTVA10 == null || assoFE.COMPTEACHETTVA10.Length <= 7)
                    //{
                    //    logger.Info("Compte acheteur TVA10 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    rapportGereration.Add("Ko : Compte acheteur TVA10 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    isok = false;
                    //    return isok;
                    //}
                    bc.ACCOUNT = assoFE.COMPTEACHETTVA10;
                }
                if (ligneTva.TAUXTVA == 14)
                {
                    //if (assoFE.COMPTEACHETTVA14 == null || assoFE.COMPTEACHETTVA14.Length <= 7)
                    //{
                    //    logger.Info("Compte acheteur TVA14 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    rapportGereration.Add("Ko : Compte acheteur TVA14 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    isok = false;
                    //    return isok;
                    //}
                    bc.ACCOUNT = assoFE.COMPTEACHETTVA14;
                }
                if (ligneTva.TAUXTVA == 7)
                {
                    //if (assoFE.COMPTEACHETTVA7 == null || assoFE.COMPTEACHETTVA7.Length <= 7)
                    //{
                    //    logger.Info("Compte acheteur TVA7 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    rapportGereration.Add("Ko : Compte acheteur TVA7 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    isok = false;
                    //    return isok;
                    //}
                    bc.ACCOUNT = assoFE.COMPTEACHETTVA7;
                }


                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.DC_FLAG = -1;
                    //montant = ((Convert.ToSingle(row["gfamtht"]) * Convert.ToInt32(intM.TVA1)) / 100) * p.Prorata_Tva(site, intM.Compte, intM.Date_piec.ToString());
                    float prtrta = CommonFacturation.Prorata_Tva(bc.CLIENT, bc.ACCOUNT, bc.PERIOD+"01");//facture.DATECREATION.Value.ToString("dd/MM/yyyy")
                    if (prtrta == -1)
                    {
                        logger.Info("Merci de parametrer le prorate tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        rapportGereration.Add("Ko : Merci de parametrer le prorate tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        return false;
                    }
                    bc.AMOUNT = ((Convert.ToDouble(facture.MNTHT) * (double)ligneTva.TAUXTVA) / 100) * prtrta;
                    //bc.AMOUNT = Convert.ToDouble(ligneTva.MNTVA);
                }
                else
                {
                    bc.DC_FLAG = 1;
                    //bc.AMOUNT = Convert.ToDouble(ligneTva.MNTVA);
                    float prtrta = CommonFacturation.Prorata_Tva(bc.CLIENT, bc.ACCOUNT, bc.PERIOD+"01"); // facture.DATECREATION.Value.ToString("dd/MM/yyyy"));
                    if (prtrta == -1) {
                        logger.Info("Merci de parametrer le prorate tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        rapportGereration.Add("Ko : Merci de parametrer le prorate tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        return false;
                    }
                    bc.AMOUNT = ((Convert.ToDouble(facture.MNTHT) * (double)ligneTva.TAUXTVA) / 100) * prtrta;
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
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE.CODEAGRESSO);
                    bc.DIM_1 = facture.FACTSITE.CODEAGRESSO;//N° site
                }
                else
                {
                    bc.DIM_1 = "3100";
                }
                if (ens.CODE.Equals("CF"))
                {
                    bc.DIM_1 = "01";
                }

                if (isok)
                {
                    if (bc.AMOUNT != 0)
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                }
                comptetva = bc.ACCOUNT;
            }


            //La ligne HT            
            if (facture.MNTHT != 0)
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
                    bc.DC_FLAG = -1;
                    //bc.AMOUNT = Convert.ToDouble(facture.MNTHT);
                }
                else
                {
                    bc.DC_FLAG = 1;
                    //bc.AMOUNT = Convert.ToDouble(facture.MNTHT);
                }

                //montant = Convert.ToSingle(row["gfamtht"]) * p.Prorata(site, p.Compte_FG_TVA(site, intM.TVA1), intM.Date_piec.ToString());
                if (comptetva != null && !comptetva.Equals(" ") && comptetva.Length > 0)
                {
                    float prtrtah = CommonFacturation.Prorata(bc.CLIENT, comptetva, bc.PERIOD + "01");// facture.DATECREATION.Value.ToString("dd/MM/yyyy"));
                    if (prtrtah == -1)
                    {
                        logger.Info("Merci de parametrer le prorate tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        rapportGereration.Add("Ko : Merci de parametrer le prorate tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        return false;
                    }
                    bc.AMOUNT = Convert.ToDouble(facture.MNTHT) * prtrtah;
                }
                else
                {
                    bc.AMOUNT = Convert.ToDouble(facture.MNTHT);
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
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE.CODEAGRESSO);
                    bc.DIM_1 = facture.FACTSITE.CODEAGRESSO;//N° site
                }
                else
                {
                    bc.DIM_1 = "3100";
                }

                if (isok)
                {
                    if (!facture.insert_temp(bc, "ACRTRANS"))
                        return false;
                }
            }

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
                    if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB"))
                    {
                        bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE.CODEAGRESSO);
                        bc.DIM_1 = facture.FACTSITE.CODEAGRESSO;//N° site
                    }
                    else
                    {
                        bc.DIM_1 = "3100";
                    }

                    if (ens.CODE.Equals("CF"))
                    {
                        bc.DIM_1 = "01";
                    }

                    if (isok)
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                }
            }
            //La ligne TTC
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
                    bc.DC_FLAG = 1;
                    bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTTTC);
                }
                else
                {
                    bc.DC_FLAG = -1;
                    bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTTTC);
                }

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
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE.CODEAGRESSO);
                    bc.DIM_1 = facture.FACTSITE.CODEAGRESSO;//N° site
                }
                else
                {
                    bc.DIM_1 = "3100";
                }

                if (ens.CODE.Equals("CF"))
                {
                    bc.DIM_1 = "01";
                }

                if (isok)
                {
                    if (!facture.insert_temp(bc, "ACRTRANS"))
                        return false;
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
            using (EramEntities DB = new EramEntities())
            {
                try
                {
                    requette.Append("select counter,trans_id from acrtransgr" + agrDbLink + ", acrvouchtype" + agrDbLink + " ");
                    requette.Append(" where ACRTRANSGR.VOUCH_SERIES=ACRVOUCHTYPE.VOUCH_SERIES and ACRTRANSGR.client=ACRVOUCHTYPE.client ");
                    requette.Append(" and ACRTRANSGR.client='" + client + "' and ACRVOUCHTYPE.VOUCHER_TYPE='GF' and rownum=1");

                    //logger.Error("La requette est =" + requette);

                    using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
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
            }
            //FACTFACTURE facture = new FACTFACTURE();
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

    }
}