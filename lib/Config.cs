using System;
using System.Collections.Generic;
using System.Configuration;

namespace SupplierPortal.lib
{
    /// <summary>Settings read from Web.config. Nothing here is user input.</summary>
    internal static class Config
    {
        internal static string DwhConnection { get { return ConfigurationManager.ConnectionStrings["FMCGBIDB"].ConnectionString; } }
        internal static string AppsConnection { get { return ConfigurationManager.ConnectionStrings["APPSRO"].ConnectionString; } }

        internal static int TemplateId { get { return int.Parse(Setting("Supplier.TemplateId")); } }
        internal static string ValidatorProc { get { return Setting("Proc.Validate"); } }
        internal static string ApprovalsProc { get { return Setting("Proc.CreateApprovals"); } }
        internal static string StepsProc { get { return Setting("Proc.ExecSteps"); } }

        /// <summary>Uploads are kept under App_Data, which IIS never serves directly.</summary>
        internal static string UploadRoot { get { return Setting("Upload.Root"); } }
        internal static int MaxFormBytes { get { return int.Parse(Setting("Upload.MaxFormBytes")); } }
        internal static int MaxAttachmentBytes { get { return int.Parse(Setting("Upload.MaxAttachmentBytes")); } }

        /// <summary>USER_ROLE values (DW_MDM_APP_USERS) allowed to open the Admin tab.</summary>
        internal static string[] AdminRoles
        {
            get { return Setting("Admin.Roles").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries); }
        }

        /// <summary>Schemas the Admin tab may pick a creation procedure from.</summary>
        internal static string[] ProcedureOwners
        {
            get { return Setting("Admin.ProcedureOwners").ToUpperInvariant().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries); }
        }

        private static string Setting(string key)
        {
            string v = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrEmpty(v)) throw new ConfigurationErrorsException("Missing appSetting: " + key);
            return v;
        }
    }
}
