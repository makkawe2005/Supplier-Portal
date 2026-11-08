using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SupplierPortal.lib;

namespace SupplierPortal
{
    public partial class RequestPage : BasePage
    {
        private Requests.Info req;

        private static readonly string[][] Fields =
        {
            new[] { "SUPPLIER_NAME", "Beneficiary name" }, new[] { "ALTERNATIVE_NAME", "Alternative name" },
            new[] { "SUPPLIER_CHANNEL", "Supplier channel" }, new[] { "SUPPLIER_TYPE", "Nature of supplies" },
            new[] { "SITE_OPERATING_UNIT", "Operating unit" }, new[] { "SITE_ADDRESS_NAME", "Address name" },
            new[] { "SITE_ADDRESS_LINE1", "Address line 1" }, new[] { "SITE_CITY", "City" },
            new[] { "SITE_REGION", "Region" }, new[] { "SITE_COUNTRY", "Country" },
            new[] { "CR_CERTIFICATE_NUMBER", "CR number" }, new[] { "CR_EXPIRY_DATE", "CR expiry" },
            new[] { "COC_CERTIFICATE_NUMBER", "CoC number" }, new[] { "COC_EXPIRY_DATE", "CoC expiry" },
            new[] { "ZAKAT_CERTIFICATE_NUMBER", "Zakat number" }, new[] { "ZAKAT_EXPIRY_DATE", "Zakat expiry" },
            new[] { "TAX_REGISTRATION_NUM", "VAT registration no." }, new[] { "TERMS_NAME", "Payment terms" },
            new[] { "SITE_LIABILITY_GL", "Liability GL" }, new[] { "SITE_PREPAYMENT_GL", "Prepayment GL" },
            new[] { "BANK_NAME", "Bank" }, new[] { "BRANCH_NAME", "Branch" },
            new[] { "BANK_ACCOUNT_NUM", "Account no." }, new[] { "IBAN", "IBAN" },
        };

        protected void Page_Load(object sender, EventArgs e)
        {
            req = Requests.Load(QueryInt("id"));
            if (!Requests.CanView(Me, req)) Deny("You don't have access to this request.");

            if (!IsPostBack)
            {
                var carried = Session["SP.Problems." + req.RequestId] as List<string>;
                if (carried != null) { ShowProblems(carried); Session.Remove("SP.Problems." + req.RequestId); }
                Bind();
            }
        }

        protected void btnReupload_Click(object sender, EventArgs e)
        {
            Run(() =>
            {
                if (!fileForm.HasFile && !fileDocs.HasFiles) throw new InvalidOperationException("Choose a file to upload.");
                var problems = FormUpload.Process(Me, req, fileForm.PostedFile, fileDocs.PostedFiles.ToList());
                ShowProblems(problems);
                return problems.Count == 0 ? "Uploaded." : null;
            });
        }

        protected void btnSubmit_Click(object sender, EventArgs e) { Run(() => { Requests.Submit(Me, req); return "Submitted for approval."; }); }
        protected void btnApprove_Click(object sender, EventArgs e) { Run(() => { Requests.Approve(Me, req, txtComments.Text); return "Approved."; }); }
        protected void btnReject_Click(object sender, EventArgs e) { Run(() => { Requests.Reject(Me, req, txtComments.Text); return "Rejected."; }); }
        protected void btnExecute_Click(object sender, EventArgs e) { Run(() => { Requests.Execute(Me, req); return "The supplier was created in Oracle."; }); }

        /// <summary>Runs an action, shows its outcome, and re-reads the request so the buttons match its new state.</summary>
        private void Run(Func<string> action)
        {
            try
            {
                string done = action();
                if (done != null) { lblDone.Text = Server.HtmlEncode(done); lblDone.Visible = true; }
            }
            catch (UnauthorizedAccessException ex) { ShowProblems(new List<string> { ex.Message }); }
            catch (InvalidOperationException ex) { ShowProblems(new List<string> { ex.Message }); }
            catch (Exception ex)
            {
                // Oracle errors are logged on the server and shown to the user without stack traces.
                System.Diagnostics.Trace.TraceError("Request {0}: {1}", req.RequestId, ex);
                ShowProblems(new List<string> { "Something went wrong: " + ex.Message });
            }
            req = Requests.Load(req.RequestId);
            Bind();
        }

        private void Bind()
        {
            litId.Text = req.RequestId.ToString();
            litStatus.Text = Server.HtmlEncode(req.Status);
            litMeta.Text = Server.HtmlEncode(req.Title + " · requested by " + req.RequestedBy + " on " + req.RequestedOn.ToString("dd-MMM-yyyy"));

            DataTable rows = Requests.StagingRows(req.RequestId);
            DataRow row = rows.Rows.Count > 0 ? rows.Rows[0] : null;
            rptFields.DataSource = row == null
                ? new List<KeyValuePair<string, string>>()
                : Fields.Select(f => new KeyValuePair<string, string>(f[1], row[f[0]].ToString())).ToList();
            rptFields.DataBind();
            pnlValidation.Visible = row != null;
            if (row != null) litValidation.Text = Server.HtmlEncode(row["VALIDATION_RESULTS"].ToString());

            var files = new List<object>();
            if (UploadStore.Resolve(req.RequestId, UploadStore.FormFileName) != null)
                files.Add(new { Name = "Vendor Form", Url = DownloadUrl(UploadStore.FormFileName) });
            files.AddRange(UploadStore.ListAttachments(req.RequestId).Select(n => (object)new { Name = n, Url = DownloadUrl(n) }));
            rptFiles.DataSource = files;
            rptFiles.DataBind();

            gridSteps.DataSource = Requests.Steps(req.RequestId);
            gridSteps.DataBind();

            // Buttons only reflect what the server would allow; every action re-checks it anyway.
            bool owner = Requests.IsOwner(Me, req);
            var editable = new[] { Status.Entered, Status.NeedsValidation, Status.ReadyForSubmit, Status.Rejected };
            pnlEdit.Visible = owner && editable.Contains(req.Status);
            btnSubmit.Visible = owner && req.Status == Status.ReadyForSubmit;

            var step = Requests.MyCurrentStep(Me, req);
            pnlAction.Visible = step != null;
            if (step != null)
            {
                bool exec = Requests.IsExecuteStep(step);
                litActionTitle.Text = Server.HtmlEncode(exec ? "Ready to create" : "Your approval (" + step["STEP_TYPE"] + ")");
                btnApprove.Visible = !exec;
                btnExecute.Visible = exec;
            }
        }

        private string DownloadUrl(string name)
        {
            return ResolveUrl("~/Download.ashx") + "?id=" + req.RequestId + "&f=" + Uri.EscapeDataString(name);
        }

        private void ShowProblems(IEnumerable<string> problems)
        {
            foreach (var p in problems) listErrors.Items.Add(p);
        }
    }
}
