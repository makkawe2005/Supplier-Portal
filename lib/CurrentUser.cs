using System;
using System.Data;
using System.Web;

namespace SupplierPortal.lib
{
    /// <summary>
    /// The signed-in Windows user and their MDM role. Identity always comes from IIS
    /// (Windows authentication). There is no query-string override.
    /// </summary>
    internal sealed class CurrentUser
    {
        internal string UserName { get; private set; }
        internal string Role { get; private set; }
        internal string Division { get; private set; }
        internal string Region { get; private set; }
        internal string Email { get; private set; }
        internal bool IsKnown { get { return Role != null; } }

        private const string ItemKey = "SupplierPortal.CurrentUser";

        internal static CurrentUser Get()
        {
            var ctx = HttpContext.Current;
            var cached = ctx.Items[ItemKey] as CurrentUser;
            if (cached != null) return cached;

            var u = new CurrentUser { UserName = StripDomain(ctx.User.Identity.Name) };
            var dt = Db.Query(
                "SELECT USER_ROLE, DIVISION, REGION, LOWER(EMPLOYEE_EMAIL) EMPLOYEE_EMAIL FROM DW_MDM_APP_USERS WHERE LOWER(USER_NAME) = ?",
                u.UserName);
            if (dt.Rows.Count > 0)
            {
                DataRow r = dt.Rows[0];
                u.Role = r["USER_ROLE"].ToString();
                u.Division = r["DIVISION"].ToString();
                u.Region = r["REGION"].ToString();
                u.Email = r["EMPLOYEE_EMAIL"].ToString();
            }
            ctx.Items[ItemKey] = u;
            return u;
        }

        /// <summary>True when the user's role or user name is linked to the supplier template.</summary>
        internal bool CanSubmit()
        {
            if (!IsKnown) return false;
            object n = Db.Scalar(
                "SELECT COUNT(*) FROM DW_MDM_APP_TEMPLATES_ROLES WHERE TEMPLATE_ID = ? AND (USER_ROLE = ? OR LOWER(USER_NAME) = ?)",
                Config.TemplateId, Role, UserName);
            return Convert.ToInt32(n) > 0;
        }

        private static string StripDomain(string name)
        {
            name = (name ?? "").Trim().ToLowerInvariant();
            int i = name.IndexOf('\\');
            return i >= 0 ? name.Substring(i + 1) : name;
        }
    }
}
