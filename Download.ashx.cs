using System;
using System.IO;
using System.Web;
using SupplierPortal.lib;

namespace SupplierPortal
{
    /// <summary>Serves a request's files only to people who can view that request.</summary>
    public class Download : IHttpHandler
    {
        public bool IsReusable { get { return false; } }

        public void ProcessRequest(HttpContext context)
        {
            int id;
            int.TryParse(context.Request.QueryString["id"], out id);
            string name = context.Request.QueryString["f"] ?? "";

            var me = CurrentUser.Get();
            var req = Requests.Load(id);
            string path = Requests.CanView(me, req) ? UploadStore.Resolve(id, name) : null;
            if (path == null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            string ext = Path.GetExtension(path).ToLowerInvariant();
            context.Response.ContentType =
                ext == ".pdf" ? "application/pdf" :
                ext == ".png" ? "image/png" :
                ext == ".jpg" || ext == ".jpeg" ? "image/jpeg" :
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            context.Response.AddHeader("X-Content-Type-Options", "nosniff");
            string downloadName = name == UploadStore.FormFileName ? "Request_" + id + "_Vendor_Form.xlsx" : name;
            context.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + downloadName + "\"");
            context.Response.TransmitFile(path);
        }
    }
}
