<%@ Page Title="New supplier" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="NewRequest.aspx.cs" Inherits="SupplierPortal.NewRequestPage" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
    <h1>New supplier</h1>
    <p>Fill in one company sheet (ATS, MLS or TAR) of the Vendor Form, then upload it here with the supporting documents.</p>
    <p><a href="<%: ResolveUrl("~/Templates/Vendor_Form.xlsx") %>">Download the Vendor Form</a></p>

    <div class="field"><label for="<%= txtTitle.ClientID %>">Request title</label>
        <asp:TextBox ID="txtTitle" runat="server" MaxLength="255" /></div>
    <div class="field"><label>Vendor Form (.xlsx)</label>
        <asp:FileUpload ID="fileForm" runat="server" accept=".xlsx" /></div>
    <div class="field"><label>Supporting documents (PDF or image: CR, owner's ID, bank letter, VAT certificate…)</label>
        <asp:FileUpload ID="fileDocs" runat="server" AllowMultiple="true" accept=".pdf,.png,.jpg,.jpeg" /></div>

    <asp:Button ID="btnCreate" runat="server" Text="Upload and validate" CssClass="btn primary" OnClick="btnCreate_Click" />
    <asp:BulletedList ID="listErrors" runat="server" CssClass="errors" />
</asp:Content>
