﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbGoogleAdSense.ascx.cs" Inherits="Controls_mfbGoogleAdSense" %>
<asp:MultiView ID="mvGoogleAd" runat="server">
    <asp:View ID="vwHorizontalAd" runat="server">
        <script type="text/javascript"><!--
            google_ad_client = "ca-pub-9647598172039156";
            /* MyFlightbookHorizontal */
            google_ad_slot = "9443557277";
            google_ad_width = 728;
            google_ad_height = 90;
        //-->
        </script>
        <script type="text/javascript"
        src='<%= (Page.Request.IsSecureConnection ? "https:" : "http:") + "//pagead2.googlesyndication.com/pagead/show_ads.js" %>'>
        </script>
    </asp:View>
    <asp:View ID="vwVerticalAd" runat="server">
        <script type="text/javascript"><!--
            google_ad_client = "ca-pub-9647598172039156";
            /* MyFlightbookVertical */
            google_ad_slot = "1825828745";
            google_ad_width = 120;
            google_ad_height = 600;
        //-->
        </script>
        <script type="text/javascript"
        src='<%= (Page.Request.IsSecureConnection ? "https:" : "http:") + "//pagead2.googlesyndication.com/pagead/show_ads.js" %>'>
        </script>
    </asp:View>
</asp:MultiView>