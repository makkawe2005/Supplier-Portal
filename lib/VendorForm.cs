using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SupplierPortal.lib
{
    /// <summary>One supplier, as read from the Vendor Form. Field names follow DW_MDM_APP_SUP_NEW.</summary>
    internal sealed class VendorForm
    {
        internal string Company;              // sheet the form was filled on: ATS, MLS or TAR
        internal string SupplierName;
        internal string AlternativeName;
        internal string SupplierChannel;
        internal string SupplierType;         // "Types/Nature of Supplies"
        internal string Country;
        internal string Region;
        internal string City;
        internal string AddressLine1;
        internal string PostalCode;           // not in the staging table yet
        internal string ZipCode;              // not in the staging table yet
        internal string AddressName;
        internal string CrNumber, CrExpiry;
        internal string CocNumber, CocExpiry;
        internal string ZakatNumber, ZakatExpiry;
        internal string BankName;
        internal string BranchName;
        internal string BankAccountNum;
        internal string SwiftCode;            // not in the staging table yet
        internal string Iban;
        internal string PaymentMethod;        // not in the staging table yet
        internal string TermsName;
        internal string LiabilityGl;
        internal string PrepaymentGl;
        internal string TaxRegistrationNum;
        internal string TaxRegistrationStatus;
        internal string TaxEffectiveFrom;
        internal string FiscalClassificationCode;

        internal readonly List<string> Errors = new List<string>();
    }

    /// <summary>
    /// Reads the Vendor Form (one supplier per file). The sheets ATS, MLS and "TAR " share one
    /// layout; exactly one of them must be filled in. Cell addresses are listed in
    /// docs/vendor-form-cell-map.md.
    /// </summary>
    internal static class VendorFormParser
    {
        internal static readonly string[] Companies = { "ATS", "MLS", "TAR" };
        private const int Rows = 60, Cols = 19; // A1:S60

        internal static VendorForm Parse(string xlsxPath)
        {
            string cs = string.Format(
                "Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=\"Excel 12.0 Xml;HDR=NO;IMEX=1;\";",
                xlsxPath);

            var filled = new List<Tuple<string, DataTable>>();
            using (var con = new OleDbConnection(cs))
            {
                con.Open();
                var sheetNames = con.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null)
                    .Rows.Cast<DataRow>()
                    .Select(r => r["TABLE_NAME"].ToString().Trim('\''))
                    .Where(n => n.EndsWith("$"))
                    .Select(n => n.TrimEnd('$'))
                    .ToList();

                foreach (string company in Companies)
                {
                    // Sheet names can carry a trailing space ("TAR "); match on the trimmed name.
                    string sheet = sheetNames.FirstOrDefault(n => n.Trim().Equals(company, StringComparison.OrdinalIgnoreCase));
                    if (sheet == null) continue;

                    var dt = new DataTable();
                    // The sheet name comes from the workbook's own schema and is matched against a
                    // fixed list above; brackets are escaped so it cannot break out of the identifier.
                    string sql = "SELECT * FROM [" + sheet.Replace("]", "]]") + "$A1:S" + Rows + "]";
                    using (var da = new OleDbDataAdapter(sql, con)) da.Fill(dt);
                    if (!LooksLikeVendorForm(dt)) continue;
                    if (Cell(dt, "E7") != "") filled.Add(Tuple.Create(company, dt));
                }
            }

            var form = new VendorForm();
            if (filled.Count == 0)
            {
                form.Errors.Add("This isn't a filled-in Vendor Form: no ATS, MLS or TAR sheet has a beneficiary name.");
                return form;
            }
            if (filled.Count > 1)
            {
                form.Errors.Add("More than one company sheet is filled in (" + string.Join(", ", filled.Select(f => f.Item1)) + "). Fill in one sheet per request.");
                return form;
            }

            var t = filled[0].Item2;
            form.Company = filled[0].Item1;
            form.SupplierName = Cell(t, "E7");
            form.AlternativeName = Cell(t, "E8");
            form.SupplierChannel = Cell(t, "E9");
            form.SupplierType = Cell(t, "N9");
            form.Country = Cell(t, "D12");
            form.Region = Cell(t, "K12");
            form.City = Cell(t, "P12");
            form.AddressLine1 = Cell(t, "D13");
            form.PostalCode = Cell(t, "D14");
            form.ZipCode = Cell(t, "J14");
            form.AddressName = Cell(t, "Q14");
            form.CrNumber = Cell(t, "I20");
            form.CrExpiry = Date(form, t, "O20", "CR expiry");
            form.CocNumber = Cell(t, "I21");
            form.CocExpiry = Date(form, t, "O21", "CoC expiry");
            form.ZakatNumber = Cell(t, "I22");
            form.ZakatExpiry = Date(form, t, "O22", "Zakat expiry");
            form.BankName = Cell(t, "E26");
            form.BranchName = Cell(t, "E27");
            form.BankAccountNum = Cell(t, "E28");
            form.SwiftCode = Cell(t, "N28");
            form.Iban = Regex.Replace(string.Concat(new[] { "E30", "F30", "H30", "J30", "L30", "N30", "P30" }.Select(a => Cell(t, a))), @"\s", "").ToUpperInvariant();
            form.PaymentMethod = Cell(t, "E33");
            form.TermsName = Cell(t, "E34");
            form.LiabilityGl = Cell(t, "E36");
            form.PrepaymentGl = Cell(t, "E37");
            form.TaxRegistrationNum = Regex.Replace(Cell(t, "F42"), @"\s", "");
            form.TaxRegistrationStatus = Cell(t, "O42");
            form.TaxEffectiveFrom = Date(form, t, "E43", "Tax effective from");
            form.FiscalClassificationCode = Cell(t, "F45");

            BasicChecks(form);
            return form;
        }

        /// <summary>Quick checks before anything reaches the database. The Oracle validator does the full validation.</summary>
        private static void BasicChecks(VendorForm f)
        {
            if (f.SupplierName == "") f.Errors.Add("Beneficiary name is empty.");
            if (f.Iban != "" && !Regex.IsMatch(f.Iban, "^[A-Z0-9]{15,34}$"))
                f.Errors.Add("IBAN must be 15 to 34 letters and digits.");

            foreach (var field in typeof(VendorForm).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
            {
                var v = field.GetValue(f) as string;
                if (v != null && v.Length > 255) f.Errors.Add(field.Name + " is longer than 255 characters.");
            }
        }

        private static bool LooksLikeVendorForm(DataTable t)
        {
            return Cell(t, "A4").IndexOf("Supplier", StringComparison.OrdinalIgnoreCase) >= 0
                && Cell(t, "B7").IndexOf("Beneficiary", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Reads a date cell and returns it as YYYYMMDD, the format the old 467 template used.</summary>
        private static string Date(VendorForm f, DataTable t, string address, string label)
        {
            object obj = CellValue(t, address);
            if (obj is DateTime) return ((DateTime)obj).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string raw = Cell(t, address);
            if (raw == "") return "";
            double serial;
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out serial) && serial > 20000 && serial < 80000)
                return DateTime.FromOADate(serial).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            DateTime d;
            string[] formats = { "yyyyMMdd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "dd-MMM-yyyy", "dd.MM.yyyy" };
            if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return d.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            f.Errors.Add(label + " (" + address + ") isn't a date: \"" + raw + "\".");
            return raw;
        }

        private static string Cell(DataTable t, string address)
        {
            object v = CellValue(t, address);
            return v == null || v == DBNull.Value ? "" : v.ToString().Trim();
        }

        private static object CellValue(DataTable t, string address)
        {
            var m = Regex.Match(address, "^([A-Z]+)([0-9]+)$");
            int col = 0;
            foreach (char c in m.Groups[1].Value) col = col * 26 + (c - 'A' + 1);
            int row = int.Parse(m.Groups[2].Value);
            if (row > t.Rows.Count || col > t.Columns.Count || col > Cols) return null;
            return t.Rows[row - 1][col - 1];
        }
    }
}
