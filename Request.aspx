<%@ Page Title="Supplier request" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Request.aspx.cs" Inherits="SupplierPortal.RequestPage" %>
<asp:Content ContentPlaceHolderID="Main" runat="server">
    <h1>Request #<asp:Literal ID="litId" runat="server" /> <span class="status"><asp:Literal ID="litStatus" runat="server" /></span></h1>
    <p class="meta"><asp:Literal ID="litMeta" runat="server" /></p>

    <asp:BulletedList ID="listErrors" runat="server" CssClass="errors" />
    <asp:Label ID="lblDone" runat="server" CssClass="done" Visible="false" />

    <section>
        <h2>Supplier</h2>
        <asp:Repeater ID="rptFields" runat="server">
            <HeaderTemplate><table class="kv"></HeaderTemplate>
            <ItemTemplate><tr><th><%#: Eval("Key") %></th><td><%#: Eval("Value") %></td></tr></ItemTemplate>
            <FooterTemplate></table></FooterTemplate>
        </asp:Repeater>
        <asp:Panel ID="pnlValidation" runat="server" CssClass="validation">
            <b>Validation:</b> <asp:Literal ID="litValidation" runat="server" />
        </asp:Panel>
    </section>

    <section>
        <h2>Files</h2>
        <asp:Repeater ID="rptFiles" runat="server">
            <HeaderTemplate><ul class="files"></HeaderTemplate>
            <ItemTemplate><li><a href="<%#: Eval("Url") %>"><%#: Eval("Name") %></a></li></ItemTemplate>
            <FooterTemplate></ul></FooterTemplate>
        </asp:Repeater>
    </section>

    <asp:Panel ID="pnlEdit" runat="server" CssClass="box">
        <h2>Fix and re-upload</h2>
        <div class="field"><label>Vendor Form (.xlsx)</label><asp:FileUpload ID="fileForm" runat="server" accept=".xlsx" /></div>
        <div class="field"><label>More documents</label><asp:FileUpload ID="fileDocs" runat="server" AllowMultiple="true" accept=".pdf,.png,.jpg,.jpeg" /></div>
        <asp:Button ID="btnReupload" runat="server" Text="Upload and validate" CssClass="btn" OnClick="btnReupload_Click" />
        <asp:Button ID="btnSubmit" runat="server" Text="Submit for approval" CssClass="btn primary" OnClick="btnSubmit_Click" />
    </asp:Panel>

    <asp:Panel ID="pnlAction" runat="server" CssClass="box">
        <h2><asp:Literal ID="litActionTitle" runat="server" /></h2>
        <div class="field"><label>Comments</label><asp:TextBox ID="txtComments" runat="server" TextMode="MultiLine" Rows="3" MaxLength="2000" /></div>
        <asp:Button ID="btnApprove" runat="server" Text="Approve" CssClass="btn primary" OnClick="btnApprove_Click" />
        <asp:Button ID="btnExecute" runat="server" Text="Create supplier in Oracle" CssClass="btn primary" OnClick="btnExecute_Click"
            OnClientClick="return confirm('Create this supplier in Oracle now?');" />
        <asp:Button ID="btnReject" runat="server" Text="Reject" CssClass="btn danger" OnClick="btnReject_Click" />
    </asp:Panel>

    <section>
        <h2>Approval steps</h2>
        <asp:GridView ID="gridSteps" runat="server" AutoGenerateColumns="false" CssClass="grid" EmptyDataText="Steps are created when the request is submitted.">
            <Columns>
                <asp:BoundField HeaderText="#" DataField="STEP_SEQUENCE" />
                <asp:BoundField HeaderText="Step" DataField="STEP_TYPE" />
                <asp:BoundField HeaderText="Status" DataField="STEP_STATUS" />
                <asp:BoundField HeaderText="Role" DataField="USER_ROLE" />
                <asp:BoundField HeaderText="User" DataField="USER_NAME" />
                <asp:BoundField HeaderText="Comments" DataField="STEP_COMMENTS" />
                <asp:BoundField HeaderText="Updated" DataField="LAST_UPDATE_TIME" />
            </Columns>
        </asp:GridView>
    </section>
</asp:Content>
