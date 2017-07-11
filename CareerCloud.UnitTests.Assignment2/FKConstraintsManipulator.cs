using System;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerCloud.UnitTests.Assignment2
{
    /// <summary>
    /// Uses POCOS assembly to read all database table names associated with POCOS, and 
    /// exposes methods to enable/disable the FK constraints defined on those tables
    /// 
    /// -----INSTRUCTIONS TO CONSUME APIs----
    /// 
    /// - Invoke method 'DropFKConstraintsOnAllTables' at the very beginning of test initialization. This will disable all FK constraints on all tables
    /// - Invoke method 'RestoreFKConstraintsOnAllTables' when 
    ///     a) Inside a catch block when an exception is caught in a test
    ///     b) In [TestCleanup], after test finishes 
    /// 
    /// </summary>
    internal static class FKConstraintsManipulator
    {
        private const string _pocosAssembly = "CareerCloud.Pocos.dll";
        private static Type[] _types;

        private static List<string> _cachedFKConstraintsEnableQueries;

        static FKConstraintsManipulator()
        {
            _types = Assembly.LoadFrom(_pocosAssembly).GetTypes();
            _cachedFKConstraintsEnableQueries = new List<string>();
        }
        
        /// <summary>
        /// Disables all foreign key constraints on all database tables corresponding to the POCOS
        /// </summary>
        public static void DropFKConstraintsOnAllTables()
        {
            var pocos = _types.Where(t => t.Name.Contains("Poco") && t.Name != "IPoco");

            string connString = ConfigurationManager.ConnectionStrings["dbconnection"].ToString();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                
                foreach (var poco in pocos)
                {
                    var pocoTableAttr = poco.GetCustomAttribute(typeof(TableAttribute)) as TableAttribute;

                    if (pocoTableAttr != null)
                        DropFKConstraintsOnTable(pocoTableAttr.Name, conn);
                }

                conn.Close();
            }
        }

        /// <summary>
        /// //Disables all foreign key constraints on the database table named 'tableName'
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="conn">An opened connection to the database</param>
        private static void DropFKConstraintsOnTable(string tableName, SqlConnection conn)
        {
            //Retrieve foreign key constraints (specified on the database table 'tableName') which we wish to disable temporarily
            String[] tableRestrictions = new String[4];
            tableRestrictions[2] = tableName;
            DataTable dtForeignKeysForJustTheOrderTable = conn.GetSchema("ForeignKeys", tableRestrictions);
            var foreignKeys = dtForeignKeysForJustTheOrderTable.Rows;

            if (foreignKeys.Count == 0) //SKIP DROPPING THE CONSTRAINT ON THIS TABLE
                return;

            //Now create the comma separated list of foreign key constraints to be TEMPORARILY DISABLED
            StringBuilder fkConstraintsStr = new StringBuilder();
            for (int i = 0; i < foreignKeys.Count; i++)
            {
                var fkConstraintName = foreignKeys[i][2] as string;
                string fkConstraintsStrDelimiter = ((i == foreignKeys.Count - 1) ? "" : ", ");

                fkConstraintsStr.Append(fkConstraintName);
                fkConstraintsStr.Append(fkConstraintsStrDelimiter);
            }

            //Construct the SQL command to temporarily DISABLE all foreign key constraints on table 'tableName'
            string fkConstraintsDisableQueryStr = $"ALTER TABLE {tableName} NOCHECK CONSTRAINT {fkConstraintsStr.ToString()}";

            //Also cache (into 'cachedFKConstraintsToEnable') the SQL commands to temporarily ENABLE all foreign key constraints 
            //on table 'tableName'. These will be executed after tests finish/fail
            string fkConstraintsEnableQueryStr = $"ALTER TABLE {tableName} WITH CHECK CHECK CONSTRAINT {fkConstraintsStr.ToString()}";
            _cachedFKConstraintsEnableQueries.Add(fkConstraintsEnableQueryStr);

            //Finally, execute query to 
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.CommandText = fkConstraintsDisableQueryStr;

            cmd.ExecuteNonQuery();
        }


        /// <summary>
        /// Enables all foreign key constraints (that were disabled before starting the test) 
        /// on all database tables corresponding to the POCOS
        /// </summary>
        public static void RestoreFKConstraintsOnAllTables()
        {
            if (_cachedFKConstraintsEnableQueries.Count == 0)
                return;

            string connString = ConfigurationManager.ConnectionStrings["jobportaldb"].ToString();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                foreach (var fkEnableQuery in _cachedFKConstraintsEnableQueries)
                {
                    cmd.CommandText = fkEnableQuery;
                    cmd.ExecuteNonQuery();
                }

                conn.Close();
            }
        }

    }
}