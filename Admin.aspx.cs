using System;
using SupplierPortal.lib;

namespace SupplierPortal
{
    public partial class AdminPage : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Requests.IsAdmin(Me)) Deny("The Admin tab is for MDM admins only.");
            if (!IsPostBack)
            {
                txtProc.Text = Settings.Get(Settings.CreateProc);
                string conn = Settings.Get(Settings.CreateProcConn);
                if (conn != "") ddlConn.SelectedValue = conn;
                txtOuAts.Text = Settings.OperatingUnitFor("ATS");
                txtOuMls.Text = Settings.OperatingUnitFor("MLS");
                txtOuTar.Text = Settings.OperatingUnitFor("TAR");
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            listErrors.Items.Clear();
            string proc = txtProc.Text.Trim().ToUpperInvariant();
            string conn = ddlConn.SelectedValue == "DWH" ? "DWH" : "APPS";

            if (proc != "")
            {
                string problem = Settings.CheckProcedure(proc, conn);
                if (problem != null) { listErrors.Items.Add(problem); return; }
            }

            Settings.Set(Settings.CreateProc, proc, Me.UserName);
            Settings.Set(Settings.CreateProcConn, conn, Me.UserName);
            Settings.Set("OU.ATS", txtOuAts.Text.Trim(), Me.UserName);
            Settings.Set("OU.MLS", txtOuMls.Text.Trim(), Me.UserName);
            Settings.Set("OU.TAR", txtOuTar.Text.Trim(), Me.UserName);
            txtProc.Text = proc;
            lblDone.Text = "Saved.";
            lblDone.Visible = true;
        }
    }
}
