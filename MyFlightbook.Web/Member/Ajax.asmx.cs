﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.ServiceModel;
using System.Web;
using System.Web.Services;
using Resources;

/******************************************************
 * 
 * Copyright (c) 2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Member
{
    /// <summary>
    /// Provides AUTHENTICATED AJAX support for the Website.  NOT FOR EXTERNAL CONSUMPTION!!!  These APIs may change at any point.
    /// </summary>
    [WebService(Namespace = "http://myflightbook.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ServiceContract]
    [System.Web.Script.Services.ScriptService]
    [System.ComponentModel.ToolboxItem(false)]
    public class Ajax : System.Web.Services.WebService
    {
        private static void CheckAuth()
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
                throw new UnauthorizedAccessException();
        }

        /// <summary>
        /// Gets a flight by id for the current user.
        /// </summary>
        /// <param name="idFlight"></param>
        /// <param name="fIncludeImages"></param>
        /// <param name="fIncludeTelemetry"></param>
        /// <returns></returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        [WebMethod(EnableSession = true)]
        public LogbookEntryDisplay GetFlight(int idFlight, bool fIncludeImages = false, bool fIncludeTelemetry = false)
        {
            CheckAuth();

            LogbookEntryDisplay led = new LogbookEntryDisplay(idFlight, HttpContext.Current.User.Identity.Name, fIncludeTelemetry ? LogbookEntryCore.LoadTelemetryOption.LoadAll : LogbookEntryCore.LoadTelemetryOption.None);
            if (led.FlightID != idFlight)
                throw new UnauthorizedAccessException();

            if (fIncludeImages)
                led.PopulateImages();

            return led;
        }

        [WebMethod(EnableSession = true)]
        public void sendFlight(int idFlight, string szTargetEmail, string szMessage, string szSendPageTarget)
        {
            CheckAuth();

            if (String.IsNullOrWhiteSpace(szTargetEmail))
                throw new ArgumentException(LocalizedText.ValidationEmailRequired);

            if (!Regex.IsMatch(szTargetEmail, "\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*"))
                throw new ArgumentException(LocalizedText.ValidationEmailFormat);

            string szUser = HttpContext.Current.User.Identity.Name;
            LogbookEntry le = new LogbookEntry(Convert.ToInt32(idFlight, CultureInfo.InvariantCulture), szUser);
            Profile pfSender = Profile.GetUser(szUser);

            using (MailMessage msg = new MailMessage())
            {
                msg.Body = Branding.ReBrand(Resources.LogbookEntry.SendFlightBody.Replace("<% Sender %>", HttpUtility.HtmlEncode(pfSender.UserFullName))
                    .Replace("<% Message %>", HttpUtility.HtmlEncode(szMessage))
                    .Replace("<% Date %>", le.Date.ToShortDateString())
                    .Replace("<% Aircraft %>", HttpUtility.HtmlEncode(le.TailNumDisplay))
                    .Replace("<% Route %>", HttpUtility.HtmlEncode(le.Route))
                    .Replace("<% Comments %>", HttpUtility.HtmlEncode(le.Comment))
                    .Replace("<% Time %>", le.TotalFlightTime.FormatDecimal(pfSender.UsesHHMM))
                    .Replace("<% FlightLink %>", le.SendFlightUri(Branding.CurrentBrand.HostName, szSendPageTarget).ToString()));

                msg.Subject = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.SendFlightSubject, pfSender.UserFullName);
                msg.From = new MailAddress(Branding.CurrentBrand.EmailAddress, String.Format(CultureInfo.CurrentCulture, Resources.SignOff.EmailSenderAddress, Branding.CurrentBrand.AppName, pfSender.UserFullName));
                msg.ReplyToList.Add(new MailAddress(pfSender.Email));
                msg.To.Add(new MailAddress(szTargetEmail));
                msg.IsBodyHtml = true;
                util.SendMessage(msg);
            }
        }
    }
}
