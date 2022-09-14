
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Configuration;
using System.Data;
using Domain.appFacture;
using appFacture.Models;
using Oracle.ManagedDataAccess.Client;
using log4net;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;
using NPOI.SS.Util;
using Newtonsoft.Json;
namespace MVCUI.Kagr.Controllers
{
    [Authorize(Roles = "REPORTING")]
    public class REPORTINGController : Controller
    {
        public int pageSize = 100;
        public static ILog logger = log4net.LogManager.GetLogger("KassagrWEBLogger");
        public static string OracleConnectionString = ConfigurationManager.ConnectionStrings["Factdb"].ConnectionString;

        public void keepFilters()
        {
            TempData.Keep("page");
            TempData.Keep("search");
            TempData.Keep("sortby");
            TempData.Keep("isasc");
            TempData.Keep("advSearch");
        }

        public ActionResult Reporting()
        {
            Reporting1View report = new Reporting1View();
            ViewBag.listSocietes = getSocietes();
            ViewBag.listChapitres = getChapitres();
            return View(report);
        }

        //Liste déroulante des societes
        public List<FACTENSEIGNE> getSocietes()
        {
            List<FACTENSEIGNE> listSocietes = new List<FACTENSEIGNE>();
            FACTENSEIGNE ens = new FACTENSEIGNE();
            listSocietes=ens.getAll();

            listSocietes.Insert(0, new FACTENSEIGNE(-1, "Sélectionner une société"));
            return listSocietes;
        }
        //Liste déroulante des chapitres
        public List<FACTTYPE> getChapitres()
        {
            List<FACTTYPE> listChapitres = new List<FACTTYPE>();
            FACTTYPE ens = new FACTTYPE();
            listChapitres=ens.getAll();

            listChapitres.Insert(0, new FACTTYPE(-1, "Sélectionner un chapitre"));
            return listChapitres;
        }

         [HttpPost]
        public void ExportReport(String debutcmd, String fincmd, String selectedSocietes, String selectedChapitres)
        {

            String origineSocietes = selectedSocietes;
            //selectedSocietes=selectedSocietes.Replace(",", "','");

            DataTable aDt = getLignesReports(debutcmd, fincmd, selectedSocietes, selectedChapitres);
            byte[] bytes = CreateExcellSheet("titre", "description", debutcmd, fincmd, origineSocietes, aDt);
            if (bytes==null || bytes.Length<=0)
                bytes = excelVide("titre", "description", debutcmd, fincmd, origineSocietes, aDt);

            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("Content-Length", (bytes!=null ?bytes.Length.ToString():"0"));
            Response.AddHeader("Content-disposition", "attachment; filename=Extraction-factures.xlsx");
            Response.BinaryWrite((bytes != null ? bytes : new byte[1] ));
            Response.Flush();
            Response.End();
        }

         public DataTable getLignesReports(String debutcmd, String fincmd, String societes,String chapitres)
         {
             //*****************generate Data*************************
            DataTable aDt = new DataTable();
            aDt.Columns.Add(new DataColumn("Refrence", typeof(string)));
            aDt.Columns.Add(new DataColumn("Designation", typeof(string)));
            aDt.Columns.Add(new DataColumn("Chapitre", typeof(string)));
            aDt.Columns.Add(new DataColumn("Societe", typeof(string)));
            aDt.Columns.Add(new DataColumn("Client", typeof(string)));
            aDt.Columns.Add(new DataColumn("Datecreation", typeof(string)));
            aDt.Columns.Add(new DataColumn("MontantTTC", typeof(Double)));
            aDt.Columns.Add(new DataColumn("MontantHT", typeof(Double)));
            aDt.Columns.Add(new DataColumn("MontantTVA", typeof(Double)));
            aDt.Columns.Add(new DataColumn("MontantTVAPPRF", typeof(Double)));


            int nbrErrors = 0;
             string ErrorTrace = "";
             using (OracleConnection conn = new OracleConnection(OracleConnectionString))
             {
                 conn.Open();
                 StringBuilder requette = new StringBuilder();
                 requette.Append("select f.referencefact refrence,f.libelledemande designation,t.libelle chapitre, ");
                 requette.Append("e.libelleenseigne societe, ");
                 requette.Append("case ");
                 requette.Append("  when f.typeclient =1 then ( ");
                 requette.Append("select ce.libelleenseigne clilib from factsite cs,factenseigne ce ");
                 requette.Append("where cs.idsite = f.idclientinterne ");
                 requette.Append("and cs.idenseigne=ce.idenseigne) ");
                 requette.Append("else ");
                 requette.Append("(select cli.designationclient  clilib  from factclient cli  ");
                 requette.Append("where cli.idclient=f.idclient) ");
                 requette.Append("end Client,to_char(f.datecreation,'DD/MM/RRRR') datecreation, ");
                 requette.Append("f.mntttc montantTTC,f.mntht montantHT,f.mnttva montantTVA,f.iddemande,f.MNTTVAIPPRF MontantTVAPPRF  ");
                 requette.Append(" from factdemande f,facttype t,factsite s,factenseigne e ");
                 requette.Append("where t.idfacttype = f.idfacttype ");
                 requette.Append("and t.idcategorie=2 ");
                 requette.Append("and f.dateimpression is not null ");
                 requette.Append("and f.idsite=s.idsite ");
                 requette.Append("and s.idenseigne=e.idenseigne ");

                 if (societes != null && societes.Length > 0 && societes!="-1") {
                     requette.Append(" and s.idenseigne in (" + societes + ")");
                 }

                 if (chapitres != null && chapitres.Length > 0 && chapitres != "-1")
                 {
                     requette.Append(" and f.idfacttype in (" + chapitres + ")");
                 }

                 requette.Append(" and trunc(f.datecreation) >= trunc(to_date('" + debutcmd + "','DD/MM/RRRR')) ");
                 if (fincmd != null && fincmd.Length > 0)
                 {
                     requette.Append(" and trunc(f.datecreation) <= trunc(to_date('" + fincmd + "','DD/MM/RRRR')) ");
                 }

                 requette.Append(" UNION ");

                 requette.Append("select f.referencefact refrence,f.libelledemande designation,t.libelle chapitre, ");
                 requette.Append("e.libelleenseigne societe, ");
                 requette.Append("case ");
                 requette.Append("  when f.typeclient =1 then ( ");
                 requette.Append("select ce.libelleenseigne clilib from factsite cs,factenseigne ce ");
                 requette.Append("where cs.idsite = f.idclientinterne ");
                 requette.Append("and cs.idenseigne=ce.idenseigne) ");
                 requette.Append("else ");
                 requette.Append("(select cli.designationclient  clilib  from factclient cli  ");
                 requette.Append("where cli.idclient=f.idclient) ");
                 requette.Append("end Client,to_char(f.datecreation,'DD/MM/RRRR') datecreation, ");
                 requette.Append("f.mntttc montantTTC,f.mntht montantHT,f.mnttva montantTVA,f.iddemande,f.MNTTVAPPRF MontantTVAPPRF ");
                 requette.Append(" from factfacture f,facttype t,factsite s,factenseigne e ");
                 requette.Append("where t.idfacttype = f.idfacttype ");
                 requette.Append("and t.idcategorie!=2 ");
                 requette.Append("and f.dateimpression is not null ");
                 requette.Append("and f.idsite=s.idsite ");
                 requette.Append("and s.idenseigne=e.idenseigne ");
                 requette.Append("and f.status!=5 ");

                 if (societes != null && societes.Length > 0 && societes != "-1")
                 {
                     requette.Append(" and s.idenseigne in (" + societes + ")");
                 }

                 if (chapitres != null && chapitres.Length > 0 && chapitres != "-1")
                 {
                     requette.Append(" and f.idfacttype in (" + chapitres + ")");
                 }

                 requette.Append(" and trunc(f.datecreation) >= trunc(to_date('" + debutcmd + "','DD/MM/RRRR')) ");
                 if (fincmd != null && fincmd.Length > 0)
                 {
                     requette.Append(" and trunc(f.datecreation) <= trunc(to_date('" + fincmd + "','DD/MM/RRRR')) ");
                 }


                requette.Append("order by iddemande desc  ");
                string sql = requette.ToString();

                logger.Info("requette=" + requette);

                string refrence = "";
                string designation = "";
                string chapitre = "";
                string societe = "";
                string Client = "";
                string datecreation = "";
                Double montantTTC = 0.0;
                Double montantHT = 0.0;
                Double montantTVA = 0.0;
                Double montantTVAPPRF = 0.0;


                using (OracleCommand cmd = new OracleCommand(sql, conn))
                 {
                     using (OracleDataReader rd = cmd.ExecuteReader())
                     {
                         while (rd.Read())
                         {
                             try
                             {
                                 refrence = Convert.ToString(rd.GetValue(0));
                                 designation = Convert.ToString(rd.GetValue(1));
                                 chapitre = Convert.ToString(rd.GetValue(2));
                                 societe = Convert.ToString(rd.GetValue(3));
                                 Client = Convert.ToString(rd.GetValue(4));
                                 datecreation = Convert.ToString(rd.GetValue(5));
                                 montantTTC = Convert.ToDouble(rd.GetValue(6));
                                 montantHT = Convert.ToDouble(rd.GetValue(7));
                                 montantTVA = Convert.ToDouble(rd.GetValue(8));
                                montantTVAPPRF = Convert.ToDouble(rd.GetValue(10));

                                aDt.Rows.Add(refrence, designation, chapitre,
                                                 societe, Client, datecreation,
                                                 montantTTC, montantHT, montantTVA, montantTVAPPRF);
                             }
                             catch (Exception ex)
                             {
                                 nbrErrors++;
                                 ErrorTrace += "Erreur : " + ex.ToString() + ":" + sql + "<br>";
                             }
                         }
                         rd.Close();
                     }
                 }
             }

             ErrorTrace = ErrorTrace;
             return aDt;
         }

         public byte[] CreateExcellSheet(string titre, string description, string debutcmd, string fincmd, string societe, DataTable aDt)
         {
             byte[] bytes = null;
             if (aDt.Rows.Count > 0)
             {
                 //***********************create the Excel Sheet************************
                 IWorkbook workbook = new XSSFWorkbook();
                 ISheet sheet1 = workbook.CreateSheet(titre);
                 //setting styles
                 var titleStyle = workbook.CreateCellStyle();
                 var titleFont = workbook.CreateFont();
                 titleFont.FontHeightInPoints = 14;
                 titleFont.Boldweight = (short)FontBoldWeight.Bold;
                 titleFont.IsItalic = true;
                 titleStyle.SetFont(titleFont);
                 var headerStyle = workbook.CreateCellStyle();
                 var headerFont = workbook.CreateFont();
                 headerStyle.FillForegroundColor = IndexedColors.DarkBlue.Index;
                 headerStyle.FillPattern = FillPattern.SolidForeground;
                 headerFont.FontHeightInPoints = 11;
                 headerFont.Boldweight = (short)FontBoldWeight.Bold;
                 headerFont.Color = IndexedColors.White.Index;
                 headerStyle.SetFont(headerFont);
                 IDataFormat dataFormatCustom = workbook.CreateDataFormat();
                 var rowStyle = workbook.CreateCellStyle();

                 //IRow TitleRow = sheet1.CreateRow(0);
                 //ICell titleRow = TitleRow.CreateCell(0);
                 //titleRow.SetCellValue(description);
                 //titleRow.CellStyle = titleStyle;

                 IRow DateRow = sheet1.CreateRow(0);
                 ICell DateRowcel = DateRow.CreateCell(0);
                 DateRowcel.SetCellValue("Date comptabilité");
                 DateRowcel.CellStyle = titleStyle;
                 ICell datedu = DateRow.CreateCell(1);
                 datedu.SetCellValue(debutcmd);
                 ICell dateau = DateRow.CreateCell(2);
                 dateau.SetCellValue(fincmd);

                 IRow SiteRow = sheet1.CreateRow(1);
                 ICell SiteRowcel = SiteRow.CreateCell(0);
                 SiteRowcel.SetCellValue("Société");
                 SiteRowcel.CellStyle = titleStyle;
                 ICell SiteRowcelValue = SiteRow.CreateCell(1);
                 SiteRowcelValue.SetCellValue(societe);


                 IRow headerRow = sheet1.CreateRow(2);
                 for (int j = 0; j < aDt.Columns.Count; j++)
                 {
                     headerRow.CreateCell(j).SetCellValue(aDt.Columns[j].ColumnName);
                     headerRow.Cells[j].CellStyle = headerStyle;
                 }

                 int debutrow = 3;

                 for (int i = debutrow; i <= aDt.Rows.Count + debutrow - 1; i++)
                 {
                     IRow row = sheet1.CreateRow(i);
                     for (int j = 0; j < aDt.Columns.Count; j++)
                     {
                         switch (Type.GetTypeCode(aDt.Rows[i - debutrow][j].GetType()))
                         {
                             case TypeCode.DateTime:
                                 ICell cell = row.CreateCell(j);
                                 cell.SetCellValue((DateTime)aDt.Rows[i - debutrow][j]);
                                 cell.CellStyle = rowStyle;
                                 cell.CellStyle.DataFormat = dataFormatCustom.GetFormat("dd/MM/yyyy");
                                 break;
                             case TypeCode.Double:
                                 row.CreateCell(j, CellType.Numeric).SetCellValue((double)aDt.Rows[i - debutrow][j]);
                                 break;
                             default:
                                 row.CreateCell(j, CellType.String).SetCellValue(aDt.Rows[i - debutrow][j].ToString());
                                 break;
                         }
                     }
                 }

                 for (int i = 0; i <= aDt.Columns.Count; i++)
                 {
                     sheet1.AutoSizeColumn(i);
                     GC.Collect(); // Add this line
                 }

                 using (var exportData = new MemoryStream())
                 {
                     workbook.Write(exportData);
                     bytes = exportData.ToArray();
                 }
             }
             return bytes;
         }


         public byte[] excelVide(string titre, string description, string debutcmd, string fincmd, string societe, DataTable aDt)
         {

                 byte[] bytes = null;

                 //***********************create the Excel Sheet************************
                 IWorkbook workbook = new XSSFWorkbook();
                 ISheet sheet1 = workbook.CreateSheet(titre);
                 //setting styles
                 var titleStyle = workbook.CreateCellStyle();
                 var titleFont = workbook.CreateFont();
                 titleFont.FontHeightInPoints = 14;
                 titleFont.Boldweight = (short)FontBoldWeight.Bold;
                 titleFont.IsItalic = true;
                 titleStyle.SetFont(titleFont);
                 var headerStyle = workbook.CreateCellStyle();
                 var headerFont = workbook.CreateFont();
                 headerStyle.FillForegroundColor = IndexedColors.DarkBlue.Index;
                 headerStyle.FillPattern = FillPattern.SolidForeground;
                 headerFont.FontHeightInPoints = 11;
                 headerFont.Boldweight = (short)FontBoldWeight.Bold;
                 headerFont.Color = IndexedColors.White.Index;
                 headerStyle.SetFont(headerFont);
                 IDataFormat dataFormatCustom = workbook.CreateDataFormat();
                 var rowStyle = workbook.CreateCellStyle();

                  sheet1.SetColumnWidth(0, 4000);
                 //IRow TitleRow = sheet1.CreateRow(0);
                 //ICell titleRow = TitleRow.CreateCell(0);
                 //titleRow.SetCellValue(description);
                 //titleRow.CellStyle = titleStyle;

                 IRow DateRow = sheet1.CreateRow(0);
                 ICell DateRowcel = DateRow.CreateCell(0);
                 DateRowcel.SetCellValue("Date comptabilité");
                 DateRowcel.CellStyle = titleStyle;
                 ICell datedu = DateRow.CreateCell(1);
                 datedu.SetCellValue(debutcmd);
                 ICell dateau = DateRow.CreateCell(2);
                 dateau.SetCellValue(fincmd);

                 IRow SiteRow = sheet1.CreateRow(1);
                 ICell SiteRowcel = SiteRow.CreateCell(0);
                 SiteRowcel.SetCellValue("Société");
                 SiteRowcel.CellStyle = titleStyle;
                 ICell SiteRowcelValue = SiteRow.CreateCell(1);
                 SiteRowcelValue.SetCellValue(societe);

                 IRow headerRow = sheet1.CreateRow(2);

                 for (int j = 0; j < aDt.Columns.Count; j++)
                 {
                     headerRow.CreateCell(j).SetCellValue(aDt.Columns[j].ColumnName);
                     headerRow.Cells[j].CellStyle = headerStyle;
                 }

                 for (int i = 0; i <= aDt.Columns.Count; i++)
                 {
                     sheet1.AutoSizeColumn(i);
                     GC.Collect(); // Add this line
                 }

                 using (var exportData = new MemoryStream())
                 {
                     workbook.Write(exportData);
                     bytes = exportData.ToArray();

                 }
             
             return bytes;
         }

    }
}
