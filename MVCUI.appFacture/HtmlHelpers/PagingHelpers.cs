using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using appFacture.Models;

namespace appFacture.HtmlHelpers
{
    /// <summary>
    /// This class contains the helper function to display the paging functionality.
    /// </summary>
    public static class PagingHelpers
    {
        public static MvcHtmlString PageLinks(this HtmlHelper html, InfosPagination pagingInfo, Func<int, string> pageUrl)
        {
            var builder = new StringBuilder();

            TagBuilder firstLi = new TagBuilder("li");
            firstLi.AddCssClass("prev");

            TagBuilder firstA = new TagBuilder("a");
            firstA.MergeAttribute("title", "Précédent");

            TagBuilder firstI = new TagBuilder("i");
            firstI.AddCssClass("fa fa-angle-left");
            if (pagingInfo.CurrentPage == 1)
            {
                firstLi.AddCssClass("disabled");
                firstA.MergeAttribute("href", "#");
            }
            else
            {
                firstA.MergeAttribute("href", pageUrl(pagingInfo.CurrentPage - 1));
            }
            firstA.InnerHtml = firstI.ToString();
            firstLi.InnerHtml = firstA.ToString();
            builder.Append(firstLi.ToString());
            int nbrButtons = 8;
            int firstindex;
            if (pagingInfo.CurrentPage - nbrButtons / 2 < 1)
                firstindex = 1;
            else
                firstindex = pagingInfo.CurrentPage - nbrButtons / 2;
            
            int lastindex;
            if (firstindex + nbrButtons > pagingInfo.TotalPages)
                lastindex = pagingInfo.TotalPages;
            else
                lastindex = firstindex + nbrButtons;

            if (lastindex - nbrButtons > 0)
                firstindex = lastindex - nbrButtons;


            for (int i = firstindex; i <= lastindex; i++)
            {
                TagBuilder li = new TagBuilder("li");

                TagBuilder a = new TagBuilder("a");
                a.MergeAttribute("href", pageUrl(i));
                a.InnerHtml = i.ToString();

                if (i == pagingInfo.CurrentPage)
                {
                    li.AddCssClass("active");
                }
                li.InnerHtml = a.ToString();
                builder.Append(li.ToString());
            }

            TagBuilder lastLi = new TagBuilder("li");
            lastLi.AddCssClass("next");

            TagBuilder lastA = new TagBuilder("a");
            lastA.MergeAttribute("title", "Suivant");

            TagBuilder lastI = new TagBuilder("i");
            lastI.AddCssClass("fa fa-angle-right");
            if (pagingInfo.CurrentPage == pagingInfo.TotalPages)
            {
                lastLi.AddCssClass("disabled");
                lastA.MergeAttribute("href", "#");
            }
            else
            {
                lastA.MergeAttribute("href", pageUrl(pagingInfo.CurrentPage + 1));
            }
            lastA.InnerHtml = lastI.ToString();
            lastLi.InnerHtml = lastA.ToString();
            builder.Append(lastLi.ToString());

            return MvcHtmlString.Create(builder.ToString());
        }
    }
}