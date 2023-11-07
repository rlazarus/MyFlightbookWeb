﻿using MyFlightbook.Printing;
using MyFlightbook.SponsoredAds;
using System;
using System.Globalization;
using System.Web.Mvc;

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    /******************************************************
     * 
     * Copyright (c) 2007-2023 MyFlightbook LLC
     * Contact myflightbook-at-gmail.com for more information
     *
    *******************************************************/

    /// <summary>
    /// Controller for pages that are only used internally.  I.e., a user would never (deliberately) hit it, but they
    /// may bounce against it (e.g., ad tracking).
    /// </summary>
    public class InternalController : Controller
    {
        public ActionResult PrintFooter(string id, int page, int topage)
        {
            bool fHasCover = PDFOptions.CoverFromEncodedOptions(id);
            bool fHasTotal = PDFOptions.TotalPagesFromEncodedOptions(id);

            // If we have a cover page, start numbering on the page AFTER the cover.
            if (fHasCover)
            {
                page--;
                topage--;
            }

            ViewBag.page = page;
            ViewBag.topage = topage;
            ViewBag.modifiedFooter = PDFOptions.ShowChangeTrack(id) ? Resources.LogbookEntry.FlightModifiedFooter : string.Empty;
            ViewBag.pageNumber = fHasTotal ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrintedFooterPageCountWithTotals, page, topage) : String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrintedFooterPageCount, page);
            return PartialView("_printFooter");
        }

        public ActionResult AdTracker(int id = -1, int imp = 0)
        {
            if (id > 0)
            {
                SponsoredAd ad = SponsoredAd.GetAd(id);
                if (ad != null)
                {
                    // Yeah, it's a click, but if "imp=1" is present, treat it as an impression, not a click.
                    if (imp != 0)
                        ad.AddImpression();
                    else
                    {
                        ad.AddClick();
                        return Redirect(ad.TargetLink);
                    }
                }
            }
            return new EmptyResult();
        }

        // GET: mvc/Internal
        public ActionResult Index()
        {
            return View();
        }
    }
}