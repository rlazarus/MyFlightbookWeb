using MyFlightbook;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2007-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

// This uses a CSS-styled menu bar based on http://www.cssportal.com/css3-menu-generator/.

public partial class XMLNav : System.Web.UI.UserControl
{
    /// <summary>
    /// Are the menus top-level only, or full popping hierarchy?
    /// </summary>
    public enum HoverStyle { Flat, HoverPop }

    #region Properties
    /// <summary>
    /// Which tab is selected?
    /// </summary>
    public tabID SelectedItem { get; set;}

    public string XmlSrc {get; set;}

    public HoverStyle MenuStyle { get; set; }

    private string CacheKey { get { return "cachedTabList" + XmlSrc; } }

    public TabList TabList
    {
        get
        {
            TabList t = (TabList) HttpRuntime.Cache[CacheKey];
            if (t == null)
            {
                t = new TabList(XmlSrc);
                HttpRuntime.Cache.Add(CacheKey, t, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(2, 0, 0), System.Web.Caching.CacheItemPriority.Normal, null);
            }
            return t;
        }
    }

    protected bool NeedsAndroidHack { get; set; }

    public event EventHandler<TabBoundEventArgs> TabItemBound;
    #endregion

    private ProfileRoles.UserRoles m_userRole;

    private void WriteTabs(IEnumerable<TabItem> lst, HtmlTextWriter m_tw, int level = 0)
    {
        // Issue #392:
        // Hack for Android touch devices: since there's no hover on a touch device, you have to tap it.
        // On iOS, the first tap is the "hover" and a 2nd tap is the actual click.
        // But on Android, the first tap does both, which makes selecting from a menu hard.
        // So an android, we'll set the top-level menu URL to "#" and have it return false in on-click to prevent a navigation.
        bool fAndroidHack = (level == 0 && NeedsAndroidHack);

        foreach (TabItem ti in lst)
        {
            if (String.IsNullOrEmpty(ti.Text))
                continue;

            if (ti.Roles.Count> 0 && !ti.Roles.Contains(m_userRole))
                continue;

            bool fHideChildren = false;

            if (TabItemBound != null)
            {
                TabBoundEventArgs tbe = new TabBoundEventArgs(ti);
                TabItemBound(this, tbe);
                if (tbe.Cancel)
                    continue;
                fHideChildren = tbe.SuppressChildren;
            }

            if (ti.ID == SelectedItem)
                m_tw.AddAttribute(HtmlTextWriterAttribute.Class, "current");
            m_tw.RenderBeginTag(HtmlTextWriterTag.Li);

            if (fAndroidHack)
                m_tw.AddAttribute(HtmlTextWriterAttribute.Onclick, "return false;");
            m_tw.AddAttribute(HtmlTextWriterAttribute.Href, fAndroidHack ? "#" : ResolveUrl(ti.Link));
            m_tw.AddAttribute(HtmlTextWriterAttribute.Id, "tabID" + ti.ID.ToString());
            m_tw.AddAttribute(HtmlTextWriterAttribute.Class, "topTab");
            m_tw.RenderBeginTag(HtmlTextWriterTag.A);
            m_tw.InnerWriter.Write(ti.Text);
            m_tw.RenderEndTag(); // Anchor tag

            if (this.MenuStyle == HoverStyle.HoverPop && ti.Children != null && ti.Children.Any() && !fHideChildren)
            {
                m_tw.RenderBeginTag(HtmlTextWriterTag.Ul);
                WriteTabs(ti.Children, m_tw, level + 1);
                m_tw.RenderEndTag();    // ul tag.
            }

            m_tw.RenderEndTag();    // li tag
        }
    }

    void Page_Load(Object sender, EventArgs e)
    {
        m_userRole = Profile.GetUser(Page.User.Identity.Name).Role;

        NeedsAndroidHack = (Request != null && Request.UserAgent != null && Request.UserAgent.ToUpper(CultureInfo.CurrentCulture).Contains("ANDROID"));

        using (StringWriter sw = new StringWriter(CultureInfo.CurrentCulture))
        {
            using (HtmlTextWriter tw = new HtmlTextWriter(sw))
            {
                WriteTabs(TabList.Tabs, tw);
            }
            LiteralControl lt = new LiteralControl(sw.ToString());
            plcMenuBar.Controls.Add(lt);
        }
    }
}
