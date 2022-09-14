using System;
using System.Globalization;
using System.Collections.Generic;
using System.Data;
using System.Configuration;
using System.Text;
using log4net;

namespace Domain.appFacture
{
    public class Utils
    {
        private static string getApar_Type(string compte)
        {
            if (compte.StartsWith("441"))
                return "P";
            else
                if (compte.StartsWith("342"))
                    return "R";
                else
                    return " ";
        }

        public static string getRequeteInsertAcrtrans(AGR bc, String temp)
        {
            bc.APAR_TYPE = getApar_Type(bc.ACCOUNT);
            string status = "'N'";

            String fields = "ACCEPT_STATUS,ACCOUNT,ACCOUNT2,ADDRESS,ALLOCATION_KEY,AMOUNT,APAR_ID,APAR_NAME,APAR_TYPE,ARRIVAL_DATE,ARRIVE_ID,ATT_1_ID,ATT_2_ID,ATT_3_ID,ATT_4_ID,ATT_5_ID,ATT_6_ID,ATT_7_ID,AUTH_CODE,BANK_ACC_TYPE,BANK_ACCOUNT,BASE_AMOUNT,BASE_CURR,BASE_VALUE_2,BASE_VALUE_3,CLEARING_CODE,CLIENT,CLIENT_REF,";
            fields += "Collection,COMMITMENT,COMPLAINT,COMPRESS_FLAG,CONTRACT_ID,CONTRACT_ORDER,CUR_AMOUNT,CURR_DOC,CURR_LICENCE,CURRENCY,DC_FLAG,Description,DIM_1,DIM_2,DIM_3,DIM_4,DIM_5,DIM_6,DIM_7,DISC_DATE,DISC_PERCENT,DISCOUNT,DUE_DATE,EXCH_RATE,EXCH_RATE2,";
            fields += "EXCH_RATE3,EXT_INV_REF,EXT_REF,FACTOR_SHORT,FISCAL_YEAR,";
            fields += "HEADER_FLAG ,INT_STATUS,INTRULE_ID,INV_ARR_SEQ,IS_OPEN_POST,KID,LAST_UPDATE,LINE_NO,NUMBER_1,ORDER_ID,ORIG_REFERENCE,PART_PAY_FLAG,PAY_CURRENCY,";
            fields += "PAY_FLAG,PAY_METHOD,PAY_PLAN_ID,PAY_PLAN_ID_REF,PAY_TRANSFER,PERIOD,PERIOD_NO,PLACE,PO_FLAG,PROVINCE,PSEUDO_ID,REG_AMOUNT,REM_LEVEL,";
            fields += "RESPONSIBLE,REV_PERIOD,REVERSE_FLAG,REVERSE_TYPE,SEQUENCE_NO,SEQUENCE_REF ,SEQUENCE_REF2,STATUS,STOP_TRIG,SWIFT,TAX_AMEND_FLAG,TAX_CODE,TAX_ID,TAX_SEQ_REF,TAX_SYSTEM,";
            fields += "TEMPLATE_ID,TEMPLATE_TYPE,TERMS_ID,TRANS_DATE,TRANS_ID,TRANS_TYPE,TREAT_CODE,USER_ID,VALUE_1,VALUE_2,VALUE_3,VAT_AMOUNT,";
            fields += "VAT_REG_NO,VOUCH_STAT,VOUCHER_DATE,VOUCHER_NO,VOUCHER_REF,VOUCHER_REF2,VOUCHER_TYPE,WF_STATE,ZIP_CODE,IDFACTURE,NOLOT";

            String req;
            req = "insert into " + temp + " (" + fields + ") values(";
            req += " ' ' ,'" + bc.ACCOUNT + "',' ',' ', '0',";
            req += "" + bc.AMOUNT.ToString().Replace(',', '.') + ",'" + (bc.APAR_ID != null && bc.APAR_ID.Length > 0 ? bc.APAR_ID : "0") + "',' ',' ', to_date('01/01/00','dd/mm/yy'),'0',";
            req += "'" + bc.ATT_1_ID + "','" + bc.ATT_2_ID + "','" + bc.ATT_3_ID + "','" + bc.ATT_4_ID + "','" + bc.ATT_5_ID + "',";
            req += "'" + bc.ATT_6_ID + "','" + bc.ATT_7_ID + "',' ',' ',' ','0','0','0','0',' ',";
            req += "'" + bc.CLIENT + "',' ','0',' ',' ',";
            req += "'0',' ',' '," + bc.AMOUNT.ToString().Replace(',', '.') + ",' ',' ',";
            req += " 'MAD', '" + bc.DC_FLAG + "',:description,'" + bc.DIM_1 + "','" + bc.DIM_2 + "',";
            req += "' ','" + bc.DIM_4 + "',' ',' ',' ',";
            req += " to_date('01/01/00','dd/mm/yy'),'0','0',to_date('01/01/00','dd/mm/yy'),'0',";
            req += "'0','0', :reference,' ',' ','" + bc.FISCAL_YEAR + "',";
            req += "'0',' ',' ','0' ,'0',' ', to_date('" + bc.LAST_UPDATE.ToString() + "','DD/MM/YYYY HH24:MI:SS'),";
            req += "'0','0','0','0','0',' ','0',";
            req += " ' ','0','0', ' ', '" + bc.PERIOD + "','0',' ','0',";
            req += "' ',' ','0',' ',' ','0',' ',' ',";
            req += " '" + bc.SEQUENCE_NO + "','0','0'," + status + ",'0',' ','0','0',";
            req += "'0','0',' ','0',' ',' ',to_date('" + bc.TRANS_DATE.ToString().Substring(0, 10) + "','DD/MM/YYYY'),";
            req += " '" + bc.TRANS_ID + "','" + bc.TRANS_TYPE + "','BI', 'sys','0',";
            req += "'0','0','0',' ',' ',";
            req += "to_date('" + bc.VOUCHER_DATE.ToString().Substring(0, 10) + "','DD/MM/YYYY'), '" + bc.VOUCHER_NO + "','0','0', '" + bc.VOUCHER_TYPE + "',";
            req += "' ',' '," + bc.IDFACTURE + "," + bc.NOLOT + ")";
            return req;
        }

    }
}