<%@ Page Title="My requests" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SupplierPortal.DefaultPage" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
    <h1>Waiting on me</h1>
    <asp:GridView ID="gridTasks" runat="server" AutoGenerateColumns="false" CssClass="grid" EmptyDataText="Nothing is waiting on you.">
        <Columns>
            <asp:HyperLinkField HeaderText="#" DataTextField="REQUEST_ID" DataNavigateUrlFields="REQUEST_ID" DataNavigateUrlFormatString="~/Request.aspx?id={0}" />
            <asp:BoundField HeaderText="Title" DataField="COMMENTS" />
            <asp:BoundField HeaderText="Step" DataField="STEP_TYPE" />
            <asp:BoundField HeaderText="Requested by" DataField="REQUESTED_BY" />
            <asp:BoundField HeaderText="Requested on" DataField="REQUESTED_ON" DataFormatString="{0:dd-MMM-yyyy}" />
        </Columns>
    </asp:GridView>

    <h1>My requests</h1>
    <asp:GridView ID="gridMine" runat="server" AutoGenerateColumns="false" CssClass="grid" EmptyDataText="You haven't raised any supplier requests yet.">
        <Columns>
            <asp:HyperLinkField HeaderText="#" DataTextField="REQUEST_ID" DataNavigateUrlFields="REQUEST_ID" DataNavigateUrlFormatString="~/Request.aspx?id={0}" />
            <asp:BoundField HeaderText="Title" DataField="COMMENTS" />
            <asp:BoundField HeaderText="Status" DataField="REQUEST_STATUS" />
            <asp:BoundField HeaderText="Requested on" DataField="REQUESTED_ON" DataFormatString="{0:dd-MMM-yyyy}" />
        </Columns>
    </asp:GridView>
</asp:Content>
