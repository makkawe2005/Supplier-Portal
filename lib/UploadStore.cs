using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace SupplierPortal.lib
{
    /// <summary>
    /// Files are saved under App_Data/Uploads/{requestId}/, which IIS does not serve. The server
    /// picks every file name: the Vendor Form is always "form.xlsx", and attachments get a safe name
    /// built from the original. Files are only ever read back through Download.ashx, which checks access.
    /// </summary>
    internal static class UploadStore
    {
        private static readonly Dictionary<string, byte[]> Signatures = new Dictionary<string, byte[]>
        {
            { ".xlsx", new byte[] { 0x50, 0x4B, 0x03, 0x04 } },  // zip container
            { ".pdf",  new byte[] { 0x25, 0x50, 0x44, 0x46 } },  // %PDF
            { ".png",  new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
            { ".jpg",  new byte[] { 0xFF, 0xD8, 0xFF } },
            { ".jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },
        };
        private static readonly string[] AttachmentTypes = { ".pdf", ".png", ".jpg", ".jpeg" };
        internal const string FormFileName = "form.xlsx";

        internal static string RequestFolder(int requestId)
        {
            string root = HttpContext.Current.Server.MapPath(Config.UploadRoot);
            string dir = Path.Combine(root, requestId.ToString());
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>Saves the Vendor Form. Returns null on success, otherwise the reason it was refused.</summary>
        internal static string SaveForm(HttpPostedFile file, int requestId, out string savedPath)
        {
            savedPath = null;
            string error = Check(file, new[] { ".xlsx" }, Config.MaxFormBytes);
            if (error != null) return error;
            savedPath = Path.Combine(RequestFolder(requestId), FormFileName);
            file.SaveAs(savedPath);
            return null;
        }

        internal static string SaveAttachment(HttpPostedFile file, int requestId)
        {
            string error = Check(file, AttachmentTypes, Config.MaxAttachmentBytes);
            if (error != null) return error;
            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            string stem = Regex.Replace(Path.GetFileNameWithoutExtension(Path.GetFileName(file.FileName)), @"[^A-Za-z0-9_\- ]", "_");
            if (stem.Length > 60) stem = stem.Substring(0, 60);
            string dir = Path.Combine(RequestFolder(requestId), "attachments");
            Directory.CreateDirectory(dir);
            string name = DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + stem + ext;
            file.SaveAs(Path.Combine(dir, name));
            return null;
        }

        internal static IList<string> ListAttachments(int requestId)
        {
            string dir = Path.Combine(RequestFolder(requestId), "attachments");
            if (!Directory.Exists(dir)) return new List<string>();
            return Directory.GetFiles(dir).Select(Path.GetFileName).OrderBy(n => n).ToList();
        }

        /// <summary>Resolves a stored file for download. Only names that exist in the request's folder are accepted.</summary>
        internal static string Resolve(int requestId, string name)
        {
            if (name == FormFileName)
            {
                string p = Path.Combine(RequestFolder(requestId), FormFileName);
                return File.Exists(p) ? p : null;
            }
            return ListAttachments(requestId).Contains(name)
                ? Path.Combine(RequestFolder(requestId), "attachments", name)
                : null;
        }

        private static string Check(HttpPostedFile file, string[] allowed, int maxBytes)
        {
            if (file == null || file.ContentLength == 0) return "No file was selected.";
            string ext = Path.GetExtension(file.FileName ?? "").ToLowerInvariant();
            if (Array.IndexOf(allowed, ext) < 0) return "Only " + string.Join(", ", allowed) + " files are accepted.";
            if (file.ContentLength > maxBytes) return "The file is larger than " + (maxBytes / 1024 / 1024) + " MB.";

            byte[] sig = Signatures[ext];
            var head = new byte[sig.Length];
            var s = file.InputStream;
            s.Position = 0;
            int read = s.Read(head, 0, head.Length);
            s.Position = 0;
            if (read < sig.Length || !head.SequenceEqual(sig)) return "The file's content doesn't match its " + ext + " extension.";
            return null;
        }
    }
}
