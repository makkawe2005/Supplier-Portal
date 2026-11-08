using System;
using System.Linq;
using SupplierPortal.lib;

namespace SupplierPortal
{
    public partial class NewRequestPage : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Me.CanSubmit()) Deny("You don't have access to create supplier requests.");
        }

        protected void btnCreate_Click(object sender, EventArgs e)
        {
            listErrors.Items.Clear();
            if (!fileForm.HasFile) { listErrors.Items.Add("Choose the filled-in Vendor Form."); return; }

            int id = Requests.Create(Me, txtTitle.Text);
            var r = Requests.Load(id);
            var problems = FormUpload.Process(Me, r, fileForm.PostedFile, fileDocs.PostedFiles.ToList());

            // Go to the request either way: it shows the validation results and lets the user re-upload.
            Session["SP.Problems." + id] = problems;
            Response.Redirect("~/Request.aspx?id=" + id, false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
