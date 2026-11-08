using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SupplierPortal.lib
{
    internal static class Status
    {
        internal const string Entered = "Entered";
        internal const string NeedsValidation = "Needs Validation";
        internal const string ReadyForSubmit = "Ready for Submit";
        internal const string InProgress = "In Progress";
        internal const string Executing = "Executing";
        internal const string Executed = "Executed";
        internal const string Rejected = "Rejected";
    }

    /// <summary>
    /// The request lifecycle, reusing the MDM Portal tables and procedures:
    /// DW_MDM_APP_REQUESTS, DW_MDM_APP_REQUEST_STEPS, DW_PROC_MDM_APPROVALS, DW_PROC_MDM_EXEC_STEPS.
    /// Each public method checks on the server that the current user may do it.
    /// </summary>
    internal static class Requests
    {
        internal sealed class Info
        {
            internal int RequestId, TemplateId;
            internal string RequestedBy, Status, Title, Division, Region;
            internal DateTime RequestedOn;
        }

        internal static Info Load(int requestId)
        {
            var dt = Db.Query(
                "SELECT TEMPLATE_ID, REQUEST_ID, LOWER(REQUESTED_BY) REQUESTED_BY, REQUESTED_ON, REQUEST_STATUS, COMMENTS, DIVISION, REGION " +
                "FROM DW_MDM_APP_REQUESTS WHERE REQUEST_ID = ? AND TEMPLATE_ID = ?", requestId, Config.TemplateId);
            if (dt.Rows.Count == 0) return null;
            var r = dt.Rows[0];
            return new Info
            {
                TemplateId = Convert.ToInt32(r["TEMPLATE_ID"]),
                RequestId = Convert.ToInt32(r["REQUEST_ID"]),
                RequestedBy = r["REQUESTED_BY"].ToString(),
                RequestedOn = Convert.ToDateTime(r["REQUESTED_ON"]),
                Status = r["REQUEST_STATUS"].ToString(),
                Title = r["COMMENTS"].ToString(),
                Division = r["DIVISION"].ToString(),
                Region = r["REGION"].ToString(),
            };
        }

        // ---------- access ----------

        internal static bool IsAdmin(CurrentUser u)
        {
            return u.IsKnown && Config.AdminRoles.Contains(u.Role, StringComparer.OrdinalIgnoreCase);
        }

        internal static bool IsOwner(CurrentUser u, Info r)
        {
            return r != null && r.RequestedBy == u.UserName;
        }

        /// <summary>Requester, anyone with a step on the request, or an admin.</summary>
        internal static bool CanView(CurrentUser u, Info r)
        {
            if (r == null || !u.IsKnown) return false;
            if (IsOwner(u, r) || IsAdmin(u)) return true;
            object n = Db.Scalar(
                "SELECT COUNT(*) FROM DW_MDM_APP_REQUEST_STEPS WHERE REQUEST_ID = ? AND (LOWER(USER_NAME) = ? OR USER_ROLE = ?)",
                r.RequestId, u.UserName, u.Role);
            return Convert.ToInt32(n) > 0;
        }

        /// <summary>The current ('C') step if this user is assigned to it, by name or by role.</summary>
        internal static DataRow MyCurrentStep(CurrentUser u, Info r)
        {
            if (r == null || !u.IsKnown || r.Status != Status.InProgress) return null;
            var dt = Db.Query(
                "SELECT STEP_ID, STEP_SEQUENCE, STEP_TYPE, USER_ROLE, USER_NAME FROM DW_MDM_APP_REQUEST_STEPS " +
                "WHERE REQUEST_ID = ? AND STEP_STATUS = 'C' AND (LOWER(USER_NAME) = ? OR (USER_NAME IS NULL AND USER_ROLE = ?))",
                r.RequestId, u.UserName, u.Role);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        internal static bool IsExecuteStep(DataRow step)
        {
            return step != null && string.Equals(step["STEP_TYPE"].ToString(), "EXECUTE", StringComparison.OrdinalIgnoreCase);
        }

        // ---------- lifecycle ----------

        internal static int Create(CurrentUser u, string title)
        {
            if (!u.CanSubmit()) throw new UnauthorizedAccessException("You don't have access to create supplier requests.");
            int id = Convert.ToInt32(Db.Scalar("SELECT SEQ_MDM_APP_REQUESTS.NEXTVAL FROM DUAL"));
            Db.Execute(
                "INSERT INTO DW_MDM_APP_REQUESTS (TEMPLATE_ID, REQUEST_ID, REQUESTED_BY, REQUESTED_ON, REQUEST_STATUS, COMMENTS, DIVISION, REGION) " +
                "VALUES (?, ?, ?, SYSDATE, ?, ?, ?, ?)",
                Config.TemplateId, id, u.UserName, Status.Entered, Trim(title, 255), u.Division, u.Region);
            return id;
        }

        /// <summary>Replaces the request's staging row with the parsed form, then runs the Oracle validator.</summary>
        internal static void LoadAndValidate(CurrentUser u, Info r, VendorForm f)
        {
            RequireOwnerEditable(u, r);
            string ou = Settings.OperatingUnitFor(f.Company);

            Db.Execute("DELETE FROM DW_MDM_APP_SUP_NEW WHERE REQUEST_ID = ?", r.RequestId);
            Db.Execute(
                "INSERT INTO DW_MDM_APP_SUP_NEW (TEMPLATE_ID, REQUEST_ID, REQUESTED_BY, REQUESTED_ON, " +
                "SUPPLIER_NAME, ALTERNATIVE_NAME, SUPPLIER_CHANNEL, SUPPLIER_TYPE, TERMS_NAME, " +
                "SITE_OPERATING_UNIT, SITE_ADDRESS_NAME, SITE_ADDRESS_LINE1, SITE_REGION, SITE_CITY, SITE_COUNTRY, " +
                "SITE_LIABILITY_GL, SITE_PREPAYMENT_GL, SITE_TERMS_NAME, " +
                "TAX_REGISTRATION_NUM, TAX_REGISTRATION_STATUS, TAX_EFFECTIVE_FROM, FIS_CLASSIFICATION_CODE, " +
                "CR_CERTIFICATE_NUMBER, CR_EXPIRY_DATE, COC_CERTIFICATE_NUMBER, COC_EXPIRY_DATE, ZAKAT_CERTIFICATE_NUMBER, ZAKAT_EXPIRY_DATE, " +
                "BANK_NAME, BRANCH_NAME, BANK_ACCOUNT_NUM, IBAN) " +
                "VALUES (?, ?, ?, SYSDATE, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                Config.TemplateId, r.RequestId, u.UserName,
                N(f.SupplierName), N(f.AlternativeName), N(f.SupplierChannel), N(f.SupplierType), N(f.TermsName),
                N(ou), N(f.AddressName), N(f.AddressLine1), N(f.Region), N(f.City), N(f.Country),
                N(f.LiabilityGl), N(f.PrepaymentGl), N(f.TermsName),
                N(f.TaxRegistrationNum), N(f.TaxRegistrationStatus), N(f.TaxEffectiveFrom), N(f.FiscalClassificationCode),
                N(f.CrNumber), N(f.CrExpiry), N(f.CocNumber), N(f.CocExpiry), N(f.ZakatNumber), N(f.ZakatExpiry),
                N(f.BankName), N(f.BranchName), N(f.BankAccountNum), N(f.Iban));

            Db.Procedure(Config.DwhConnection, Config.ValidatorProc,
                Db.P("P_TEMPLATE_ID", Config.TemplateId), Db.P("P_REQUEST_ID", r.RequestId));

            object bad = Db.Scalar(
                "SELECT COUNT(*) FROM DW_MDM_APP_SUP_NEW WHERE REQUEST_ID = ? AND NVL(VALIDATED_FLAG, 0) <> 1", r.RequestId);
            object rows = Db.Scalar("SELECT COUNT(*) FROM DW_MDM_APP_SUP_NEW WHERE REQUEST_ID = ?", r.RequestId);
            bool ok = Convert.ToInt32(rows) > 0 && Convert.ToInt32(bad) == 0;
            SetStatus(r.RequestId, ok ? Status.ReadyForSubmit : Status.NeedsValidation);
        }

        internal static DataTable StagingRows(int requestId)
        {
            return Db.Query("SELECT * FROM DW_MDM_APP_SUP_NEW WHERE REQUEST_ID = ?", requestId);
        }

        internal static DataTable Steps(int requestId)
        {
            return Db.Query(
                "SELECT STEP_SEQUENCE, STEP_TYPE, STEP_STATUS, USER_ROLE, USER_NAME, STEP_COMMENTS, " +
                "TO_CHAR(LAST_UPDATE_DATE, 'DD-Mon-YYYY HH24:MI') LAST_UPDATE_TIME " +
                "FROM DW_MDM_APP_REQUEST_STEPS WHERE REQUEST_ID = ? ORDER BY STEP_SEQUENCE, STEP_STATUS DESC", requestId);
        }

        internal static void Submit(CurrentUser u, Info r)
        {
            if (!IsOwner(u, r)) throw new UnauthorizedAccessException("Only the requester can submit this request.");
            if (r.Status != Status.ReadyForSubmit) throw new InvalidOperationException("The request must pass validation before it can be submitted.");

            object steps = Db.Scalar("SELECT COUNT(*) FROM DW_MDM_APP_REQUEST_STEPS WHERE REQUEST_ID = ?", r.RequestId);
            if (Convert.ToInt32(steps) == 0)
                Db.Procedure(Config.DwhConnection, Config.ApprovalsProc,
                    Db.P("P_TEMPLATE_ID", Config.TemplateId), Db.P("P_REQUEST_ID", r.RequestId));

            Step(r, "SUBMIT", u.UserName, "");
            SetStatus(r.RequestId, Status.InProgress);
        }

        internal static void Approve(CurrentUser u, Info r, string comments)
        {
            var step = MyCurrentStep(u, r);
            if (step == null || IsExecuteStep(step)) throw new UnauthorizedAccessException("This request isn't waiting on your approval.");
            Step(r, "APPROVE", u.UserName, comments);
        }

        internal static void Reject(CurrentUser u, Info r, string comments)
        {
            var step = MyCurrentStep(u, r);
            if (step == null) throw new UnauthorizedAccessException("This request isn't waiting on you.");
            if (string.IsNullOrWhiteSpace(comments)) throw new InvalidOperationException("Add a comment saying why it's rejected.");
            Step(r, "REJECT", u.UserName, comments);
            SetStatus(r.RequestId, Status.Rejected);
        }

        /// <summary>
        /// Runs the creation procedure chosen in the Admin tab. The status moves In Progress -> Executing
        /// in one conditional UPDATE, so a double click or two executors can't run it twice.
        /// </summary>
        internal static void Execute(CurrentUser u, Info r)
        {
            var step = MyCurrentStep(u, r);
            if (!IsExecuteStep(step)) throw new UnauthorizedAccessException("This request isn't waiting on you to execute it.");

            string proc = Settings.Get(Settings.CreateProc);
            string conn = Settings.Get(Settings.CreateProcConn);
            if (proc == "") throw new InvalidOperationException("No creation procedure is set yet. An admin can set it in the Admin tab.");
            string problem = Settings.CheckProcedure(proc, conn);
            if (problem != null) throw new InvalidOperationException("The creation procedure in the Admin tab isn't usable: " + problem);

            int claimed = Db.Execute(
                "UPDATE DW_MDM_APP_REQUESTS SET REQUEST_STATUS = ? WHERE REQUEST_ID = ? AND REQUEST_STATUS = ?",
                Status.Executing, r.RequestId, Status.InProgress);
            if (claimed != 1) throw new InvalidOperationException("This request is already being executed.");

            try
            {
                Db.Procedure(Settings.ConnectionString(conn), proc,
                    Db.P("P_TEMPLATE_ID", Config.TemplateId), Db.P("P_REQUEST_ID", r.RequestId));
            }
            catch
            {
                SetStatus(r.RequestId, Status.InProgress);
                throw;
            }
            SetStatus(r.RequestId, Status.Executed);
            Step(r, "EXECUTE", u.UserName, "");
        }

        internal static DataTable MyRequests(CurrentUser u)
        {
            return Db.Query(
                "SELECT REQUEST_ID, COMMENTS, REQUEST_STATUS, REQUESTED_ON FROM DW_MDM_APP_REQUESTS " +
                "WHERE TEMPLATE_ID = ? AND LOWER(REQUESTED_BY) = ? ORDER BY REQUEST_ID DESC",
                Config.TemplateId, u.UserName);
        }

        internal static DataTable MyTasks(CurrentUser u)
        {
            return Db.Query(
                "SELECT DISTINCT R.REQUEST_ID, R.COMMENTS, R.REQUEST_STATUS, R.REQUESTED_BY, R.REQUESTED_ON, S.STEP_TYPE " +
                "FROM DW_MDM_APP_REQUESTS R, DW_MDM_APP_REQUEST_STEPS S " +
                "WHERE R.REQUEST_ID = S.REQUEST_ID AND R.TEMPLATE_ID = ? AND R.REQUEST_STATUS = ? AND S.STEP_STATUS = 'C' " +
                "AND (LOWER(S.USER_NAME) = ? OR (S.USER_NAME IS NULL AND S.USER_ROLE = ?)) ORDER BY R.REQUEST_ID DESC",
                Config.TemplateId, Status.InProgress, u.UserName, u.Role);
        }

        // ---------- helpers ----------

        private static void RequireOwnerEditable(CurrentUser u, Info r)
        {
            if (!IsOwner(u, r)) throw new UnauthorizedAccessException("Only the requester can change this request.");
            var editable = new[] { Status.Entered, Status.NeedsValidation, Status.ReadyForSubmit, Status.Rejected };
            if (!editable.Contains(r.Status)) throw new InvalidOperationException("The request can't be changed while it's " + r.Status + ".");
        }

        private static void Step(Info r, string action, string user, string comments)
        {
            Db.Procedure(Config.DwhConnection, Config.StepsProc,
                Db.P("P_TEMPLATE_ID", Config.TemplateId), Db.P("P_REQUEST_ID", r.RequestId),
                Db.P("P_ACTION", action), Db.P("P_USER_NAME", user), Db.P("P_COMMENTS", Trim(comments, 2000)));
        }

        private static void SetStatus(int requestId, string status)
        {
            Db.Execute("UPDATE DW_MDM_APP_REQUESTS SET REQUEST_STATUS = ? WHERE REQUEST_ID = ?", status, requestId);
        }

        private static object N(string s) { return string.IsNullOrEmpty(s) ? (object)DBNull.Value : s; }

        private static string Trim(string s, int max)
        {
            s = (s ?? "").Trim();
            return s.Length > max ? s.Substring(0, max) : s;
        }
    }
}
