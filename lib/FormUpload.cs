using System.Collections.Generic;
using System.Web;

namespace SupplierPortal.lib
{
    /// <summary>Saves an uploaded Vendor Form and attachments, parses the form and runs validation.</summary>
    internal static class FormUpload
    {
        /// <summary>Returns the problems found. An empty list means the form was loaded and validated.</summary>
        internal static List<string> Process(CurrentUser me, Requests.Info r, HttpPostedFile form, IList<HttpPostedFile> attachments)
        {
            var problems = new List<string>();

            if (form != null && form.ContentLength > 0)
            {
                string path;
                string err = UploadStore.SaveForm(form, r.RequestId, out path);
                if (err != null) { problems.Add("Vendor Form: " + err); return problems; }

                var parsed = VendorFormParser.Parse(path);
                if (parsed.Errors.Count > 0) { problems.AddRange(parsed.Errors); return problems; }
                if (Settings.OperatingUnitFor(parsed.Company) == "")
                    problems.Add("No operating unit is set for " + parsed.Company + " yet. An admin can set it in the Admin tab.");
                else
                    Requests.LoadAndValidate(me, r, parsed);
            }

            foreach (var a in attachments)
            {
                if (a == null || a.ContentLength == 0) continue;
                string err = UploadStore.SaveAttachment(a, r.RequestId);
                if (err != null) problems.Add(a.FileName + ": " + err);
            }
            return problems;
        }
    }
}
