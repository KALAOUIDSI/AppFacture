using Domain.appFacture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace appFacture.Models
{
    public class SiteListView
    {
        public IEnumerable<FACTSITE> Sites { get; set; }
        public InfosPagination PagingInfo { get; set; }
        public string Search { get; set; }
        public int SortBy { get; set; } //sort criteria
        public Boolean IsAsc { get; set; } //sort direction
        public string AdvSearch { get; set; }
        public SITEAdvFiltre AdvSearchFilters { get; set; }
    }
}