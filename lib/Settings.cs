using System;
using System.Data;
using System.Text.RegularExpressions;

namespace SupplierPortal.lib
{
    /// <summary>
    /// Values the Admin tab edits, kept in DW_MDM_APP_SUP_SETTINGS (see sql/01_settings.sql):
    ///   CREATE_PROC        owner.procedure that creates the supplier in Oracle
    ///   CREATE_PROC_CONN   DWH or APPS: which connection runs it
    ///   OU.ATS / OU.MLS / OU.TAR   operating unit for each company sheet
    /// </summary>
    internal static class Settings
    {
        internal const string CreateProc = "CREATE_PROC";
        internal const string CreateProcConn = "CREATE_PROC_CONN";

        internal static string Get(string key)
        {
            object v = Db.Scalar("SELECT SETTING_VALUE FROM DW_MDM_APP_SUP_SETTINGS WHERE SETTING_KEY = ?", key);
            return v == null || v == DBNull.Value ? "" : v.ToString();
        }

        internal static void Set(string key, string value, string user)
        {
            int n = Db.Execute(
                "UPDATE DW_MDM_APP_SUP_SETTINGS SET SETTING_VALUE = ?, UPDATED_BY = ?, UPDATED_ON = SYSDATE WHERE SETTING_KEY = ?",
                value, user, key);
            if (n == 0)
                Db.Execute(
                    "INSERT INTO DW_MDM_APP_SUP_SETTINGS (SETTING_KEY, SETTING_VALUE, UPDATED_BY, UPDATED_ON) VALUES (?, ?, ?, SYSDATE)",
                    key, value, user);
            Db.Execute(
                "INSERT INTO DW_MDM_APP_SUP_SETTINGS_LOG (SETTING_KEY, SETTING_VALUE, UPDATED_BY, UPDATED_ON) VALUES (?, ?, ?, SYSDATE)",
                key, value, user);
        }

        internal static string OperatingUnitFor(string company)
        {
            return Get("OU." + company);
        }

        /// <summary>
        /// A creation procedure is accepted only if it's a plain OWNER.NAME identifier, the owner is in
        /// Admin.ProcedureOwners (Web.config), and the procedure actually exists in ALL_PROCEDURES.
        /// Returns null when valid, otherwise the reason.
        /// </summary>
        internal static string CheckProcedure(string qualifiedName, string connection)
        {
            qualifiedName = (qualifiedName ?? "").Trim().ToUpperInvariant();
            var m = Regex.Match(qualifiedName, @"^([A-Z][A-Z0-9_$#]{0,29})\.([A-Z][A-Z0-9_$#]{0,29})$");
            if (!m.Success) return "Use the form OWNER.PROCEDURE_NAME (letters, digits and _ only).";
            string owner = m.Groups[1].Value, name = m.Groups[2].Value;
            if (Array.IndexOf(Config.ProcedureOwners, owner) < 0)
                return "Procedures must belong to one of: " + string.Join(", ", Config.ProcedureOwners) + ".";
            object n = Db.ScalarOn(ConnectionString(connection),
                "SELECT COUNT(*) FROM ALL_PROCEDURES WHERE OWNER = ? AND OBJECT_NAME = ? AND OBJECT_TYPE = 'PROCEDURE'",
                owner, name);
            if (Convert.ToInt32(n) == 0) return "No procedure " + qualifiedName + " is visible on the " + connection + " connection.";
            return null;
        }

        /// <summary>Maps the stored connection choice to a connection string. Only DWH and APPS exist.</summary>
        internal static string ConnectionString(string connection)
        {
            if (connection == "APPS") return Config.AppsConnection;
            if (connection == "DWH") return Config.DwhConnection;
            throw new InvalidOperationException("Unknown connection: " + connection);
        }
    }
}
