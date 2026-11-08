using System;
using System.Data;
using System.Data.OleDb;

namespace SupplierPortal.lib
{
    /// <summary>
    /// Every query goes through here with bind parameters ("?" placeholders, in order).
    /// No method accepts a pre-built SQL string with values concatenated into it.
    /// </summary>
    internal static class Db
    {
        internal static DataTable Query(string sql, params object[] args)
        {
            var dt = new DataTable();
            using (var con = new OleDbConnection(Config.DwhConnection))
            using (var cmd = Command(con, sql, args))
            using (var da = new OleDbDataAdapter(cmd))
            {
                con.Open();
                da.Fill(dt);
            }
            return dt;
        }

        internal static object Scalar(string sql, params object[] args)
        {
            return ScalarOn(Config.DwhConnection, sql, args);
        }

        internal static object ScalarOn(string connectionString, string sql, params object[] args)
        {
            using (var con = new OleDbConnection(connectionString))
            using (var cmd = Command(con, sql, args))
            {
                con.Open();
                return cmd.ExecuteScalar();
            }
        }

        internal static int Execute(string sql, params object[] args)
        {
            using (var con = new OleDbConnection(Config.DwhConnection))
            using (var cmd = Command(con, sql, args))
            {
                con.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Runs a stored procedure. The name comes from Web.config, never from the request.</summary>
        internal static void Procedure(string connectionString, string procName, params Tuple<string, object>[] args)
        {
            using (var con = new OleDbConnection(connectionString))
            using (var cmd = new OleDbCommand(procName, con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                foreach (var a in args)
                    cmd.Parameters.Add(new OleDbParameter(a.Item1, a.Item2 ?? DBNull.Value));
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        internal static Tuple<string, object> P(string name, object value)
        {
            return Tuple.Create(name, value);
        }

        private static OleDbCommand Command(OleDbConnection con, string sql, object[] args)
        {
            var cmd = new OleDbCommand(sql, con);
            for (int i = 0; i < args.Length; i++)
                cmd.Parameters.Add(new OleDbParameter("p" + i, args[i] ?? DBNull.Value));
            return cmd;
        }
    }
}
