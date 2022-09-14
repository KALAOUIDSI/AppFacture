using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.appFacture
{
    public class INTEGBROUILLARD
    {
        public INTEGBROUILLARD()
        {
            this.DETAILS = new HashSet<INTEGBROUILLARDDET>();
        }
        public decimal NO_LOT { get; set; }
        public decimal CROID { get; set; }
        public decimal IDFACTURE { get; set; }
        public string SCROID { get; set; }
        public string REF_FACT { get; set; }
        public Nullable<System.DateTime> DATE_FACTURE { get; set; }
        public Nullable<System.DateTime> DATE_COMPTABILITE { get; set; }
        public decimal ANNEE_FISCALE { get; set; }
        public decimal PERIOD { get; set; }
        public string CLIENT { get; set; }
        public decimal MNT_NEG { get; set; }
        public decimal MNT_POS { get; set; }
        public decimal EQUILIBRE { get; set; }
        public decimal NBRE_SEQUENCE { get; set; }
        public ICollection<INTEGBROUILLARDDET> DETAILS { get; set; }
        public decimal SEQUENCE { get; set; }
        public decimal MONTANT { get; set; }
        public string COMPTE { get; set; }
        public string DESCRIPTION { get; set; }
        public string DIM1 { get; set; }
        public string DIM2 { get; set; }
        public string DIM4 { get; set; }
        public string CNUF { get; set; }

    }
}
