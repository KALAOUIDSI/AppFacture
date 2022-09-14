using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.appFacture
{
    public class REGLEMENT
    {
        public string  CLIENT      { get; set; }
        public string  VOUCHER_TYPE { get; set; }
        public decimal VOUCHER_NO  { get; set; }
        public decimal AMOUNT      { get; set; }
        public decimal SEQUENCE_NO { get; set; }
    }
}
