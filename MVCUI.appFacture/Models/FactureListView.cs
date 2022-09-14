using Domain.appFacture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace appFacture.Models
{
    public class FactureListView
    {
        public IEnumerable<FACTFACTURE> Factures { get; set; }
        public InfosPagination PagingInfo { get; set; }
        public string Search { get; set; }
        public int SortBy { get; set; } //sort criteria
        public Boolean IsAsc { get; set; } //sort direction
        public string AdvSearch { get; set; }
        public FACTUREAdvFiltre AdvSearchFilters { get; set; }
        }
}