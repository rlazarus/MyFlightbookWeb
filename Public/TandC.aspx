﻿<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="TandC.aspx.cs" Inherits="Public_TandC" Title="Terms and Conditions" %>
<%@ Register src="../Controls/mfbTandC.ascx" tagname="mfbTandC" tagprefix="uc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="contentHeader" ContentPlaceHolderID="cpPageTitle" runat="server">
    <% =Resources.LocalizedText.TermsAndConditionsHeader %>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <uc1:mfbTandC ID="mfbTandC1" runat="server" />
</asp:Content>

