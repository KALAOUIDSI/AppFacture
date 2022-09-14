using Domain.appFacture;
//using Oracle.DataAccess.Client;
using System;
using System.Web;
using System.Transactions;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading;
using log4net;


namespace appFacture.Controllers
{

    public static class processImport
    {
        public static ILog logger = log4net.LogManager.GetLogger("EramGoldLogger");
        public static int nbrErrors;
        public static string ErrorTrace;

        public static Object[] importProcess(HttpPostedFileBase fileBase)
        {
            try
            {
                XSSFWorkbook hssfwb;
                hssfwb = new XSSFWorkbook(fileBase.InputStream);
                ISheet feuille1 = hssfwb.GetSheetAt(0);
                Object[] retour=traiterFeuille(feuille1);
                return retour;
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("Erreur system : {0}", ex.Message));
                return null;
            }            
        }

        public static Object[] traiterFeuille(ISheet sheet)
        {
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            //int IdSiteConnecte = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.SerialNumber).Value);

            List<string[]> maliste = new List<string[]>();
            string messageError = "";
            int dernierRow = -1;
            Object[] retour=new  Object[2];

            for (int row = 1; row <= sheet.LastRowNum; row++)
            {
                string[] ligne=new string[5];
                IRow crow = sheet.GetRow(row);
                if (crow != null && testRowisNotNull(crow)) //null is when the row only contains empty cells 
                {
                    String ean = "";
                    if (testCellNotNull(crow.GetCell(0)))
                    {
                        ean = GetCellValue(crow.GetCell(0));
                        ligne[0] = ean;
                    }
                    else {
                        messageError = "l'ean a la ligne " + (row + 1) + " est non mentionné !!!";
                        retour[0] = messageError;
                        retour[1] = null;
                        return retour;
                    }

                    String libelle = "";
                    if (testCellNotNull(crow.GetCell(1)))
                    {
                        libelle = GetCellValue(crow.GetCell(1));
                        ligne[1] = libelle;
                    }
                    else
                    {
                        messageError = "le libellé a la ligne " + (row + 1) + " est non mentionné !!!";
                        retour[0] = messageError;
                        retour[1] = null;
                        return retour;
                    }

                    Double qte = 0.0;
                    if (testCellNotNull(crow.GetCell(2)))
                    {
                        if (!testCellNumeric(crow.GetCell(2)))
                        {
                            messageError = "La quantité a la ligne " + (row + 1) + " non numérique !!!";
                            retour[0] = messageError;
                            retour[1] = null;
                            return retour;
                        }
                        else
                        {
                            qte = crow.GetCell(2).NumericCellValue;
                            ligne[2] = qte.ToString();
                        }
                    }else
                    {
                        messageError = "La quantité a la ligne " + (row + 1) + " est non mentionnée !!!";
                        retour[0] = messageError;
                        retour[1] = null;
                        return retour;
                    }

                    Double prix = 0.0;
                    if (testCellNotNull(crow.GetCell(4)))
                    {
                        if (!testCellNumeric(crow.GetCell(4)))
                        {
                            messageError = "Le prix a la ligne " + (row + 1) + " non numérique !!!";
                            retour[0] = messageError;
                            retour[1] = null;
                            return retour;
                        }
                        else
                        {
                            prix = crow.GetCell(4).NumericCellValue;
                            ligne[3] = prix.ToString();
                        }
                    }
                    else
                    {
                        messageError = "Le prix a la ligne " + (row + 1) + " est non mentionnée !!!";
                        retour[0] = messageError;
                        retour[1] = null;
                        return retour;
                    }

                    Double total = 0.0;

                    if (testCellNotNull(crow.GetCell(5)))
                    {
                        if (!double.TryParse(GetCellValue(crow.GetCell(5)), out total))
                        {
                            messageError = "Le total a la ligne " + (row + 1) + " est vide ou non numerique !!!";
                            retour[0] = messageError;
                            retour[1] = null;
                            return retour;
                        }
                        ligne[4] = total.ToString();
                    }
                    else
                    {
                        messageError = "Le total a la ligne  " + (row + 1) + " est non mentionnée !!!";
                        retour[0] = messageError;
                        retour[1] = null;
                        return retour;
                    }
                    //String ean = sheet.GetRow(row).GetCell(0);
                    //String ean = sheet.GetRow(row).GetCell(0);
                    //getDoubleValueCell
                    maliste.Add(ligne);
                }
                else
                {
                    dernierRow = row;
                    break;
                }
            }

            if (dernierRow != -1 && dernierRow < sheet.LastRowNum)
            {
                if ((dernierRow + 4) < sheet.LastRowNum)
                {
                    IRow crowFraisExp = sheet.GetRow(dernierRow + 4);
                    double fraisExp = 0.0;
                    if (!testCellNotNull(crowFraisExp.GetCell(5)))
                    {
                        messageError = "Le frais export a la ligne " + (dernierRow + 4 + 1) + " est non mentionné !!!";
                        retour[0] = messageError;
                        retour[1] = null;
                        return retour;
                    }
                    if (!double.TryParse(GetCellValue(crowFraisExp.GetCell(5)), out fraisExp)) {
                        messageError = "Le frais export a la ligne " + (dernierRow + 4 + 1) + " est non numérique !!!";
                        retour[0] = messageError;
                        retour[1] = null;
                        return retour;
                    }
                    logger.Info("fraisExp=" + fraisExp);
                }
                else {
                    messageError = "Le format du pied de la facture est incorrect le frais export n'est pas mentionné a la ligne " + (dernierRow + 4 + 1) + " !!!";
                    retour[0] = messageError;
                    retour[1] = null;
                    return retour;
                }

                if ((dernierRow + 5) < sheet.LastRowNum)
                {
                    IRow crowFraisPal = sheet.GetRow(dernierRow + 5);
                    double fraisPal = 0.0;
                    if (!testCellNotNull(crowFraisPal.GetCell(5)))
                    {
                        messageError = "Le frais palette a la ligne " + (dernierRow + 5 + 1) + " est non mentionné !!!";
                        retour[0] = messageError;
                        retour[1] = null;
                        return retour;
                    }

                    if (!double.TryParse(GetCellValue(crowFraisPal.GetCell(5)), out fraisPal))
                    {
                        messageError = "Le frais palette a la ligne " + (dernierRow + 4 + 1) + " est non numérique !!!";
                        retour[0] = messageError;
                        retour[1] = null;
                        return retour;
                    }
                    logger.Info("fraisPal=" + fraisPal);
                }
                else
                {
                    messageError = "Le format du pied de la facture est incorrect le frais palette n'est pas mentionné a la ligne " + (dernierRow + 5 + 1) + " !!!";
                    retour[0] = messageError;
                    retour[1] = null;
                    return retour;
                }
            }
            else
            {
                messageError = "Le pied de la facture est innexistant  a partir de la ligne " + dernierRow + " !!!";
                retour[0] = messageError;
                retour[1] = null;
                return retour;
            }

            retour[0] = "OK";
            retour[1] = maliste;
          return retour;
        }
        private static bool testCellNumeric(ICell cell)
        {
            if (cell == null)
                return false;

            if (cell.CellType == CellType.Numeric)
                return true;

            if (cell.CellType == CellType.String) {
                double val = 0.0;
                return double.TryParse(cell.StringCellValue, out val);
            }
            return false;
        }

        private static bool testCellNotNull(ICell cell)
        {
            if (cell == null)
                return false;
            if (cell.CellType == CellType.Blank)
                return false;

            if (cell.CellType == CellType.Numeric || cell.CellType == CellType.Unknown){
                if (cell.ToString() == null || cell.ToString().Length == 0)
                    return false;
                else 
                    return true;
            }

            if (cell.CellType == CellType.String ){
                if (cell.StringCellValue == null || cell.StringCellValue.Length == 0)
                    return false;
                else 
                    return true;
            }

            if (cell.CellType == CellType.Formula) {
                return true;
            }

            return false;
        }


        private static string GetCellValue(ICell cell)
        {
            if (cell == null)
                return string.Empty;
            switch (cell.CellType)
            {
                case CellType.Blank:
                    return string.Empty;
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                case CellType.Error:
                    return cell.ErrorCellValue.ToString();
                case CellType.Numeric:
                case CellType.Unknown:
                default:
                    return cell.ToString();//This is a trick to get the correct value of the cell. NumericCellValue will return a numeric value no matter the cell value is a date or a number
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Formula:
                    try
                    {
                        HSSFFormulaEvaluator e = new HSSFFormulaEvaluator(cell.Sheet.Workbook);
                        e.EvaluateInCell(cell);
                        return cell.ToString();
                    }
                    catch
                    {
                        return cell.NumericCellValue.ToString();
                    }
            }
        }


        //public static void ProcessColis(FACTIMPORTXLSX item, int nbrLigneCmdMax, ISheet sheet)
        //{
        //    string conditionnement = "c";
        //    int lheader = 0;
        //    int colStartQte = 2;
        //    int colRefNsi = 0;
        //        try
        //        {
        //            logger.Error("Debut lecture feuille colis");
        //            //logger.Error("sheet.GetRow(lheader).Cells.Count()=" + sheet.GetRow(lheader).Cells.Count());
        //            int nbrCols = sheet.GetRow(lheader).Cells.Count();
        //            int nombreMagasin = nbrCols - colStartQte;

        //            int[] magasin= new int[nombreMagasin];
        //            using (OracleConnection conn = new OracleConnection(connstring))
        //            {
        //                conn.Open();
        //                if (!deleteIntdetcmd(conn, item.IDFICHIER, conditionnement))
        //                {
        //                    conn.Close();
        //                    conn.Dispose();
        //                    return;
        //                };


        //                for (int k = colStartQte, j = 0; k < nbrCols; k++, j++)
        //                {
        //                    //if (sheet.GetRow(lheader).GetCell(k).CellType == CellType.Numeric)
        //                    if ( testIntTypeCell(sheet.GetRow(lheader).GetCell(k)) )
        //                    {
        //                        magasin[j] = getIntValueCell(sheet.GetRow(lheader).GetCell(k));//(int)sheet.GetRow(lheader).GetCell(k).NumericCellValue;
        //                        SITE site = new SITE();
        //                        site = site.getFromCodeGold(magasin[j]);
        //                        if (site == null) {
        //                            insertIntdetcmd(conn, item.IDFICHIER, "null", "2", "la cellue (ligne,colonne) (1," + (k + 1) + ") :  le site " + magasin[j] + " est innexistant", conditionnement, 1, (k + 1));
        //                            conn.Close();
        //                            conn.Dispose();
        //                            return;
        //                        }
        //                    }else{
        //                        //logger.Error("Non numeric");
        //                        insertIntdetcmd(conn, item.IDFICHIER, "null", "2", "la cellue (ligne,colonne) (1," + (k + 1) + ") :  la valeur est non numerique", conditionnement,1, (k + 1));
        //                        conn.Close();
        //                        conn.Dispose();
        //                        return;
        //                    }
        //                }
                        
        //                if (magasin.Length<=0){
        //                    insertIntdetcmd(conn, item.IDFICHIER, "null", "2", "Aucun magasin defini", conditionnement,0,0);
        //                    conn.Close();
        //                    conn.Dispose();
        //                    return;
        //                }

        //                nbrLfeuilleC=sheet.LastRowNum;
        //                for (int row = 1; row <= sheet.LastRowNum; row++)
        //                {
        //                    string sql = " insert into intdetcmd "
        //                        + "("
        //                        + "idfichier,"
        //                        + "idarticle,"
        //                        + "refnsi,"
        //                        + "satut,"
        //                        + "Motif_rejet,"
        //                        + "conditionnement,"//conditionnement
        //                        + "numligne,"
        //                        + "numcolonne,"
        //                        + "dernierutilisateur,"
        //                        + "datecreation,"
        //                        + "datemodification )";
        //                    if (sheet.GetRow(row) != null) //null is when the row only contains empty cells 
        //                    {
        //                        //logger.Error(string.Format("Row 1= {0}", sheet.GetRow(row).GetCell(0).StringCellValue));
        //                        string status = "0";
        //                        string motif = "";
        //                        int numligne = 0;
        //                        int numcol = 0;
        //                        string refnsi = "";
        //                        long lrefnsi = 0;
        //                        //if (sheet.GetRow(row).GetCell(colRefNsi).CellType != CellType.Numeric) {
        //                        if (!testLongTypeCell(sheet.GetRow(row).GetCell(colRefNsi)))
        //                        {
        //                            status = "2";
        //                            motif = "le contenu de la cellule dont les cordonnees (ligne, colonne) sont (" + (row + 1) + "," + (colRefNsi+1) + ") est non numerique";
        //                            numligne = row+1;
        //                            numcol = colRefNsi + 1;
        //                            nbrLfeuilleCRejet++;
        //                        }
        //                        else {
        //                            refnsi = "" + getLongValueCell(sheet.GetRow(row).GetCell(colRefNsi));//sheet.GetRow(row).GetCell(colRefNsi).NumericCellValue;
        //                            lrefnsi = getLongValueCell(sheet.GetRow(row).GetCell(colRefNsi));//(long)sheet.GetRow(row).GetCell(colRefNsi).NumericCellValue;
        //                        }

        //                        sql += "values (";
        //                        sql += item.IDFICHIER+",";

        //                        if (status.Equals("0"))
        //                        {
        //                            ARTICLE art = new ARTICLE();
        //                            art = art.getOneByRef(lrefnsi);
        //                            if (art != null)
        //                            {
        //                                        ARTICLETAILLE artTaille = new ARTICLETAILLE();
        //                                        List<ARTICLETAILLE> liste = artTaille.getAll(art.IDARTICLE);
        //                                        if (!verifDispo(liste))
        //                                        {
        //                                            status = "2";
        //                                            numligne = row + 1;
        //                                            numcol = 1;
        //                                            motif = "Cellule cordonnees (ligne, colonne) sont (" + numligne + "," + (numcol) + "); Article refnsi=" + refnsi + " est non disponible en colis";
        //                                            nbrLfeuilleCRejet++;
        //                                        }
        //                                sql += art.IDARTICLE + ",";
        //                            }
        //                            else
        //                            {
        //                                sql += "null,";
        //                                status = "2";
        //                                numligne = row + 1;
        //                                numcol = 1;
        //                                motif = "Cellule cordonnees (ligne, colonne) sont (" + numligne + "," + (numcol) + "); Article innexistant refnsi=" + refnsi;

        //                                nbrLfeuilleCRejet++;
        //                            }
        //                        }
        //                        else {
        //                            sql += "null,";
        //                        }
        //                        sql += refnsi + ",";

        //                        sql += status + ",";
        //                        sql += "'"+motif+"',";
        //                        sql += "'" + conditionnement + "',";
        //                        sql += numligne + ",";
        //                        sql += numcol + ",";
        //                        sql += item.DERNIERUTILISATEUR + ",";
        //                        sql += "sysdate,";
        //                        sql += "sysdate )";
                                
        //                        //logger.Error("sql=" + sql);

        //                        try
        //                        {
        //                            using (OracleCommand comm = new OracleCommand(sql, conn))
        //                                comm.ExecuteNonQuery();
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            nbrErrors++;
        //                            ErrorTrace += "Erreur : " + ex.ToString() + ":" + sql + "<br>";
        //                        }

        //                        if (status.Equals("0"))
        //                        {
        //                            bool errorQte = false;
        //                            long idintdetcmd = getIdIntdetcmd(conn, item.IDFICHIER, lrefnsi,-1,conditionnement);
        //                            //tester si toutes les quantites sont valides.
        //                            int jj = colStartQte;
        //                            foreach (int mg in magasin)
        //                            {
        //                                //if (sheet.GetRow(row).GetCell(jj).CellType != CellType.Numeric)
        //                                if (!testDoubleTypeCell(sheet.GetRow(row).GetCell(jj)))
        //                                {
        //                                    string msg = "la cellue (ligne,colonne) (" + (row + 1) + "," + (jj + 1) + ") :  le contenu de la quantité est invalide ";
        //                                    numligne = row+1;
        //                                    numcol = jj + 1;
        //                                    updateIntdetcmd(conn, idintdetcmd, "2", msg, numligne, numcol);
        //                                    errorQte = true;
        //                                    nbrLfeuilleCRejet++;
        //                                    break;
        //                                }
        //                                jj++;
        //                            }
        //                            if (!errorQte) {
        //                                jj = colStartQte;
        //                                foreach(int mg in magasin){
        //                                    double qte = getDoubleValueCell(sheet.GetRow(row).GetCell(jj));//sheet.GetRow(row).GetCell(jj).NumericCellValue;
        //                                    if(qte>0)
        //                                        insertQteintdetcmd(conn, "" + idintdetcmd, mg, "" + qte);
        //                                    jj++;   
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //                logger.Error("Fin lecture feuille colis");
        //                logger.Error("Debut creation commandes colis");
        //                try
        //                {
        //                    string sqlcmd1 = "select distinct a.saison  from intdetcmd i,article a where i.conditionnement='" + conditionnement + "' and  i.SATUT=0 and a.idarticle=i.idarticle and i.idfichier=" + item.IDFICHIER;
        //                    using (OracleCommand cmd = new OracleCommand(sqlcmd1, conn))
        //                {
        //                    //logger.Error("cmd=" + sqlcmd1);
        //                    using (OracleDataReader rd = cmd.ExecuteReader())
        //                    {
        //                        while (rd.Read()){
        //                            string saison = rd.GetValue(0).ToString();
        //                            //logger.Error("saison=" + saison);
        //                            foreach (int mag in magasin)
        //                            {
        //                                    //logger.Error("mag=" + mag);
        //                                    COMMANDEACHAT cmdachat = createCmd(item,mag);
        //                                    nbrCmdColisCrees++;
        //                                    //logger.Error("cmdachat=" + cmdachat.IDCOMMANDEACHAT);
        //                                    int nbrLigneCmd  = 0;
        //                                    decimal totalMnt = 0;

        //                                    string sqlcmd2 = "select i.idarticle,q.qte,i.idintdetcmd,i.dernierutilisateur from intdetcmd i,qteintdetcmd q,article a where i.conditionnement='" + conditionnement + "' and  i.SATUT=0  and q.idintdetcmd=i.idintdetcmd and a.idarticle=i.idarticle and q.numsite=" + mag + " and a.saison='" + saison + "' and i.idfichier=" + item.IDFICHIER;
        //                                    OracleCommand cmd2 = new OracleCommand(sqlcmd2, conn);
                                            
        //                                    //logger.Error("cmd2=" + sqlcmd2);
        //                                    OracleDataReader rd2 = cmd2.ExecuteReader();
        //                                    while (rd2.Read()) {

        //                                        long idArticle = long.Parse(rd2.GetValue(0).ToString());
        //                                        if (rd2.GetValue(1).ToString() == null || rd2.GetValue(1).ToString() == "")
        //                                            continue;
        //                                        int qteColisBuf = int.Parse(rd2.GetValue(1).ToString());
        //                                        if (qteColisBuf == 0)
        //                                            continue;
        //                                        long idIntdetcmd = long.Parse(rd2.GetValue(2).ToString());
        //                                        //logger.Error("idArticle=" + idArticle);
        //                                        //logger.Error("qteColisBuf=" + qteColisBuf);
                                                
        //                                        ARTICLE art = new ARTICLE();
        //                                        art=art.getOne(idArticle);

        //                                        ARTICLETAILLE artTaille = new ARTICLETAILLE();
        //                                        List<ARTICLETAILLE> liste = artTaille.getAll(art.IDARTICLE);
        //                                        LIGNECOMMANDEACHAT LCA = new LIGNECOMMANDEACHAT();
        //                                        //if (verifDispo(liste))
        //                                        //{
        //                                            foreach (ARTICLETAILLE itemTaille in liste)
        //                                            {
        //                                                //logger.Error("CODECOLISSTANDARD=" + (art.CODECOLISSTANDARD.HasValue ? art.CODECOLISSTANDARD.Value : 0));
        //                                                //logger.Error("itemTaille.IDARTICLE=" + itemTaille.IDARTICLE);
        //                                                //logger.Error("itemTaille.IDTAILLE=" + itemTaille.IDTAILLE);
        //                                                int qty = art.CODECOLISSTANDARD.HasValue ? itemTaille.getQty(itemTaille.IDARTICLE, itemTaille.IDTAILLE, art.CODECOLISSTANDARD.Value) : 0;
        //                                                //logger.Error("qty=" + qty);
        //                                                int qteColis = qteColisBuf;
        //                                                int clcqteVrac = qteColisBuf * qty;
        //                                                //logger.Error("qteColisBuf=" + qteColisBuf);
        //                                                //logger.Error("qteVrac=" + clcqteVrac);
        //                                                totalMnt += clcqteVrac * (art.PRIXACHAT.HasValue ? art.PRIXACHAT.Value : 0);
        //                                                int qteVrac = 0;
        //                                                qteColis = qteColis * qty;
        //                                                LCA.update(cmdachat.IDCOMMANDEACHAT, itemTaille.IDTAILLE, art.CODECOLISSTANDARD.HasValue ? art.CODECOLISSTANDARD.Value : 0, qteColis, qteVrac, 0, (item.DERNIERUTILISATEUR.HasValue ? item.DERNIERUTILISATEUR.Value : 0), idArticle, "", art.PRIXACHAT.HasValue ? art.PRIXACHAT.Value : 0);
        //                                                nbrLigneCmd++;
        //                                            }
        //                                        //}
        //                                        //else {
        //                                        //    string status= "2";
        //                                        //    string motif = "L article non disponible en colis";
        //                                        //    updateIntdetcmd(conn, idIntdetcmd, status, motif, -1, -1);
        //                                        //}

        //                                        if (nbrLigneCmd > nbrLigneCmdMax) {
        //                                            cmdachat = cmdachat.updateTotal(cmdachat.IDCOMMANDEACHAT, totalMnt, item.IDFICHIER, item.NOMFICHIER);
        //                                            insertImportcmdinteg(conn, item.IDFICHIER, cmdachat.IDCOMMANDEACHAT);
        //                                            cmdachat = createCmd(item, mag);
        //                                            nbrCmdColisCrees++;
        //                                            //logger.Error("new cmdachat=" + cmdachat.IDCOMMANDEACHAT + " - " + " nbreLigneCmd=" + nbrLigneCmd);
        //                                            totalMnt = 0;
        //                                            nbrLigneCmd = 0;
        //                                        }
        //                                    }
        //                                    LIGNECOMMANDEACHAT LCA2 = new LIGNECOMMANDEACHAT();
        //                                    if (LCA2.getAll(cmdachat.IDCOMMANDEACHAT).Count() <= 0)
        //                                    {
        //                                        cmdachat.delete(cmdachat.IDSITE, cmdachat.IDCOMMANDEACHAT);
        //                                        nbrCmdColisCrees--;
        //                                    }
        //                                    else
        //                                    {
        //                                        //logger.Error("cmdachat=" + cmdachat.IDCOMMANDEACHAT + " - " + " nbreLigneCmd=" + nbrLigneCmd);
        //                                        cmdachat = cmdachat.updateTotal(cmdachat.IDCOMMANDEACHAT, totalMnt, item.IDFICHIER, item.NOMFICHIER);
        //                                        insertImportcmdinteg(conn, item.IDFICHIER, cmdachat.IDCOMMANDEACHAT);
        //                                    }
        //                            }
        //                            updateTrtIntdetcmd(conn, item.IDFICHIER, saison, conditionnement);
        //                        }
        //                    }

        //                }
        //                }
        //                catch (Exception ex)
        //                {
        //                    nbrErrors++;
        //                    ErrorTrace += "Erreur : " + ex.ToString() +"<br>";
        //                    logger.Error(string.Format("Erreur system : {0} - {1} ", ex.Message, ex.ToString()));
        //                }
        //                logger.Error("Fin creation commandes colis");
        //            conn.Close();
        //            }
        //         }
        //        catch (Exception ex)
        //        {
        //            logger.Error(string.Format("Erreur system : {0}", ex.Message));
        //        }
        //}

        public static bool testRowisNotNull(IRow crow)
        {
            for (int i = 0; i < crow.LastCellNum; i++) { 
                if (testCellNotNull(crow.GetCell(i)))
                    return true;
            }
            return false;
        }

        public static bool testLongTypeCell(ICell macell)
        {
            if (macell.CellType == CellType.Numeric)
            {
                return true;
            }
            if (macell.CellType == CellType.String)
            {
                long val = 0;
                return long.TryParse(macell.StringCellValue, out val);
            }
            return false;
        }
        public static long getLongValueCell(ICell macell)
        {
            if (macell.CellType == CellType.Numeric)
            {
                return (long)macell.NumericCellValue;
            }
            if (macell.CellType == CellType.String)
            {
                long val = 0;
                bool versInt = long.TryParse(macell.StringCellValue, out val);
                if (versInt)
                    return val;
            }
            return 0;
        }
        public static bool testIntTypeCell(ICell macell){
            if (macell.CellType == CellType.Numeric)
            {
                return true;
            }
            if (macell.CellType == CellType.String)
            {
                int val = 0;
                return int.TryParse(macell.StringCellValue, out val);
            }
            return false;
        }
        public static bool testDoubleTypeCell(ICell macell)
        {
            if (macell.CellType == CellType.Numeric)
            {
                return true;
            }
            if (macell.CellType == CellType.String)
            {
                double val = 0;
                return double.TryParse(macell.StringCellValue, out val);
            }
            return false;
        }

        public static int getIntValueCell(ICell macell)
        {
            if (macell.CellType == CellType.Numeric)
            {
                return (int)macell.NumericCellValue;
            }
            if (macell.CellType == CellType.String)
            {
                int val = 0;
                bool versInt = int.TryParse(macell.StringCellValue, out val);
                if (versInt)
                    return val;
            }
            return 0;
        }

        public static double getDoubleValueCell(ICell macell)
        {
            if (macell.CellType == CellType.Numeric)
            {
                return (double)macell.NumericCellValue;
            }
            if (macell.CellType == CellType.String)
            {
                double val = 0;
                bool versInt = double.TryParse(macell.StringCellValue, out val);
                if (versInt)
                    return val;
            }
            return 0;
        }

   

        public static void sendmailINTCDE(List<string> msg)
        {
            MailMessage mail = new MailMessage("noreply@rid.com", ConfigurationManager.AppSettings["MAIL_ERROR_INTCDE_TO1"]);
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = true;
            client.Host = "192.168.99.103";
            mail.Bcc.Add(new MailAddress(ConfigurationManager.AppSettings["MAIL_ERROR_INTCDE_TO2"]));
            //envoyer le mail au groupe super_si (configuré sur app_config)
            FACTUSER ut = new FACTUSER();
            List<FACTUSER> liste = ut.getAll(int.Parse(ConfigurationManager.AppSettings["MAIL_ERROR_TO_GROUPE"]));
            foreach (FACTUSER item in liste)
            {
                if (!string.IsNullOrWhiteSpace(item.EMAIL))
                    mail.To.Add(new MailAddress(item.EMAIL.Trim()));
            }
            mail.Subject = "Département Eram : Erreurs lors de l'insertion dans INTCDE";
            string body = string.Join(System.Environment.NewLine, msg.ToArray());
            mail.Body = body;
            client.Send(mail);


        }
        

        public static void sendmailSM(List<string> msg)
        {
            MailMessage mail = new MailMessage("noreply@rid.com", ConfigurationManager.AppSettings["MAIL_ERROR_SM_TO"]);
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = true;
            client.Host = ConfigurationManager.AppSettings["MAIL_SMTP"];
            mail.Bcc.Add(new MailAddress(ConfigurationManager.AppSettings["MAIL_ERROR_SM_BCC"]));
            mail.Subject = "Département Eram : une structure marchandise manquante";
            string body = string.Join(System.Environment.NewLine, msg.ToArray());
            mail.Body = body;
            client.Send(mail);


        }


        public static void sendmailPA(List<string> msg)
        {
            MailMessage mail = new MailMessage("noreply@rid.com", ConfigurationManager.AppSettings["MAIL_ERROR_PA_TO1"]);
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = true;
            client.Host = ConfigurationManager.AppSettings["MAIL_SMTP"];
            //mail.To.Add(new MailAddress(ConfigurationManager.AppSettings["MAIL_ERROR_PA_TO2"]));
            //envoyer le mail au groupe super_si (configuré sur app_config)
            FACTUSER ut = new FACTUSER();
            List<FACTUSER> liste = ut.getAll(int.Parse(ConfigurationManager.AppSettings["MAIL_ERROR_TO_GROUPE"]));
            foreach (FACTUSER item in liste)
            {
                if (!string.IsNullOrWhiteSpace(item.EMAIL))
                    mail.To.Add(new MailAddress(item.EMAIL.Trim()));
            }
            mail.Subject = "Département Eram : problème d'insertion de prix d'achat sur Gold";
            string body = string.Join(System.Environment.NewLine, msg.ToArray());
            mail.Body = body;
            client.Send(mail);


        }


        public static string format1(string str, int precision)
        {
            string returnValue = "";
            for (int i = 0; i < precision; i++)
                returnValue += "0";
            return (returnValue + str).Right(precision);
        }
        public static string format2(int value, int precision)
        {
            string returnValue = "";
            for (int i = 0; i < precision; i++)
                returnValue += "0";
            if (value >= 0)
                return "+" + (returnValue + value.ToString()).Right(precision);
            else
                return "-" + (returnValue + (-1 * value).ToString()).Right(precision);
        }
        public static string format3(decimal value, int precision, int decimals)
        {
            long value2 = long.Parse(Math.Round(value * 100).ToString());
            string returnValue = "";
            for (int i = 0; i < precision; i++)
                returnValue += "0";
            if (value2 >= 0)
                return "+" + (returnValue + value2.ToString()).Right(precision);
            else
                return "-" + (returnValue + (-1 * value2).ToString()).Right(precision);
        }
        public static void sendmailLog(List<string> msg)
        {
            MailMessage mail = new MailMessage("noreply@rid.com", ConfigurationManager.AppSettings["MAIL_ERROR_PA_TO1"]);
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = true;
            client.Host = ConfigurationManager.AppSettings["MAIL_SMTP"];
            //mail.To.Add(new MailAddress(ConfigurationManager.AppSettings["MAIL_ERROR_PA_TO2"]));
            //envoyer le mail au groupe super_si (configuré sur app_config)
            FACTUSER ut = new FACTUSER();
            List<FACTUSER> liste = ut.getAll(int.Parse(ConfigurationManager.AppSettings["MAIL_ERROR_TO_GROUPE"]));
            foreach (FACTUSER item in liste)
            {
                if (!string.IsNullOrWhiteSpace(item.EMAIL))
                    mail.To.Add(new MailAddress(item.EMAIL.Trim()));
            }
            mail.Subject = "Département Eram : Envoie log binaire INTOGOLD";
            string body = string.Join(System.Environment.NewLine, msg.ToArray());
            mail.Body = body;
            client.Send(mail);
        }

    }
}
