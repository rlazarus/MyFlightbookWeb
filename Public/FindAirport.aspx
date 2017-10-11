﻿<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="FindAirport.aspx.cs" Inherits="Public_FindAirport" culture="auto" meta:resourcekey="PageResource1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="../Controls/mfbGoogleMapManager.ascx" TagName="mfbGoogleMapManager" TagPrefix="uc1" %>
<%@ Register src="../Controls/mfbGoogleAdSense.ascx" tagname="mfbGoogleAdSense" tagprefix="uc2" %>
<%@ Register src="../Controls/mfbAirportServices.ascx" tagname="mfbAirportServices" tagprefix="uc4" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblPageHeader" runat="server" Text="Label" 
            meta:resourcekey="lblPageHeaderResource1"></asp:Label>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:Panel ID="Panel1" runat="server" DefaultButton="btnFind" 
        meta:resourcekey="Panel1Resource1">
        <p><asp:Label ID="lblPrompt" runat="server" 
                Text="Can't remember an airport's code?  Search for it below" 
                meta:resourcekey="lblPromptResource1"></asp:Label></p>
        <asp:TextBox ID="txtSearch" runat="server" 
            meta:resourcekey="txtSearchResource1"></asp:TextBox>
        <asp:Button ID="btnFind" runat="server" Text="Search" onclick="btnFind_Click" 
            meta:resourcekey="btnFindResource1" />
        <asp:HyperLink ID="lnkZoomOut" runat="server"
            Visible="False" Text="Zoom to fit all" 
            meta:resourcekey="lnkZoomOutResource1"></asp:HyperLink>
        <asp:GridView ID="gvResults" runat="server" GridLines="None" Visible="false"  
                AutoGenerateColumns="False" EnableModelValidation="True" OnPageIndexChanging="gridView_PageIndexChanging" 
                meta:resourcekey="gvResultsResource1" AllowPaging="True">
            <AlternatingRowStyle BackColor="#E0E0E0" />
            <Columns>
                <asp:BoundField HeaderText="Facility Type" DataField="FacilityType" 
                    meta:resourcekey="BoundFieldResource1" />
                <asp:TemplateField HeaderText="Code" meta:resourcekey="TemplateFieldResource1">
                    <ItemTemplate>
                        <a href='javascript:clickAndZoom(<%# Eval("LatLong.LatitudeString") %>, <%# Eval("LatLong.LongitudeString") %>);'><%# Eval("Code") %></a>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField HeaderText="Name" DataField="Name" 
                    meta:resourcekey="BoundFieldResource2" />
            </Columns>
            <EmptyDataTemplate>
                <asp:Label ID="lblNoResults" runat="server" Text="(No Airports Found)" 
                    meta:resourcekey="lblNoResultsResource1"></asp:Label>
            </EmptyDataTemplate>
            <PagerSettings Mode="NumericFirstLast" />
        </asp:GridView>
        <br />
        <asp:Label ID="lblError" runat="server" CssClass="error" 
            meta:resourcekey="lblErrorResource1"></asp:Label>
    </asp:Panel>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">
    <br />
    <div style="margin-left:20px; width:80%; float:left; clear:left;">
        <uc1:mfbGoogleMapManager ID="MfbGoogleMapManager1" runat="server" Height="600px" Width="100%" />
    </div>
    <div id="ads" style="float:right; width: 130px; padding:4px; clear:right;">
        <uc2:mfbGoogleAdSense ID="mfbGoogleAdSense2" runat="server" LayoutStyle="adStyleVertical" />
    </div>
    <div style="width:100%; clear:both; float:none;">&nbsp;</div>
        <script type="text/javascript"> 
//<![CDATA[
            function clickAndZoom(lat, lon) {
                var point = new google.maps.LatLng(lat, lon);
                getGMap().setCenter(point);
                getGMap().setZoom(14);
            }

//]]>
    </script>
    </asp:Content>

