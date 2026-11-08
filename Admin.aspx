<%@ Page Title="Admin" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Admin.aspx.cs" Inherits="SupplierPortal.AdminPage" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
    <h1>Admin</h1>
    <asp:BulletedList ID="listErrors" runat="server" CssClass="errors" />
    <asp:Label ID="lblDone" runat="server" CssClass="done" Visible="false" />

    <section class="box">
        <h2>Creation procedure</h2>
        <p>Runs at the EXECUTE step with <code>P_TEMPLATE_ID</code> and <code>P_REQUEST_ID</code>, and reads the request's row from <code>DW_MDM_APP_SUP_NEW</code>.</p>
        <div class="field"><label>Procedure (OWNER.NAME)</label><asp:TextBox ID="txtProc" runat="server" MaxLength="61" /></div>
        <div class="field"><label>Runs on</label>
            <asp:DropDownList ID="ddlConn" runat="server">
                <asp:ListItem Value="APPS">APPS (Oracle EBS)</asp:ListItem>
                <asp:ListItem Value="DWH">DWH</asp:ListItem>
            </asp:DropDownList></div>
    </section>

    <section class="box">
        <h2>Operating unit per company sheet</h2>
        <div class="field"><label>ATS</label><asp:TextBox ID="txtOuAts" runat="server" MaxLength="240" /></div>
        <div class="field"><label>MLS</label><asp:TextBox ID="txtOuMls" runat="server" MaxLength="240" /></div>
        <div class="field"><label>TAR</label><asp:TextBox ID="txtOuTar" runat="server" MaxLength="240" /></div>
    </section>

    <asp:Button ID="btnSave" runat="server" Text="Save" CssClass="btn primary" OnClick="btnSave_Click" />
    <p class="meta">Every change is recorded in DW_MDM_APP_SUP_SETTINGS_LOG with your user name.</p>
</asp:Content>
