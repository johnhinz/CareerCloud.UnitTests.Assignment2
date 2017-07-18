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
    /// ----- PUBLIC APIs ----
    /// 
    /// 1. 'DisableFKConstraintsOnAllTables()' - Disables all FK constraints on ALL database tables. Invoke during [TestInitialize].
    /// 2. 'EnableFKConstraintsOnAllTables()' - Enables all FK constraints on ALL tables. Invoke during [TestCleanup].
    /// 3. 'DisableFKConstraintsOnTable(string pocoName)' - Disable all FK constraints on a particular poco
    /// 4. 'EnableFKConstraintsOnTable(string pocoName)' - Enable all FK constraints on a particular poco
    /// 
    /// </summary>
    internal static class FKConstraintsManipulator
    {
        private const string _pocosAssembly = "CareerCloud.Pocos.dll";
        private static Type[] _types;
        private static string _connectionName = "dbconnection";

        //Poco names mapped to PocoInfo objects
        private static Dictionary<string, PocoInfo> _cachedPocoInfoMap;

        #region Cache Creation for Foreign Key Constraint Data
        static FKConstraintsManipulator()
        {
            _types = Assembly.LoadFrom(_pocosAssembly).GetTypes();
            _cachedPocoInfoMap = new Dictionary<string, PocoInfo>();

            ConstructAllPocosMappingData();
        }

        private static void ConstructAllPocosMappingData()
        {
            var pocos = _types.Where(t => t.Name.Contains("Poco") && t.Name != "IPoco");

            string connString = ConfigurationManager.ConnectionStrings[_connectionName].ToString();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                foreach (var poco in pocos)
                {
                    var pocoTableAttr = poco.GetCustomAttribute(typeof(TableAttribute)) as TableAttribute;

                    if (pocoTableAttr != null)
                         _cachedPocoInfoMap[poco.Name] = GetPocoMappingData(pocoTableAttr.Name, conn);
                }

                conn.Close();
            }
        }

        private static PocoInfo GetPocoMappingData(string tableName, SqlConnection conn)
        {
            //Retrieve foreign key constraints (specified on the database table 'tableName') which we wish to disable temporarily
            String[] tableRestrictions = new String[4];
            tableRestrictions[2] = tableName;
            DataTable dtForeignKeysForJustTheOrderTable = conn.GetSchema("ForeignKeys", tableRestrictions);
            var foreignKeys = dtForeignKeysForJustTheOrderTable.Rows;

            List<string> fkConstraintNames = new List<string>();
            if (foreignKeys.Count > 0)
            {
                for (int i = 0; i < foreignKeys.Count; i++)
                {
                    var fkConstraintName = foreignKeys[i][2] as string;
                    fkConstraintNames.Add(fkConstraintName);
                }
            }

            PocoInfo pInfo = new PocoInfo(tableName, fkConstraintNames.ToArray());

            return pInfo;
        }
        #endregion

        #region Public API to be consumed by clients
        /// <summary>
        /// Disables all foreign key constraints on all database tables corresponding to the POCOS
        /// </summary>
        public static void DisableFKConstraintsOnAllTables()
        {
            foreach (var poco in _cachedPocoInfoMap.Keys)
                DisableFKConstraintsOnTable(poco);
        }

        /// <summary>
        /// Enables all foreign key constraints on all database tables corresponding to the POCOS
        /// </summary>
        public static void EnableFKConstraintsOnAllTables()
        {
            foreach (var poco in _cachedPocoInfoMap.Keys)
                EnableFKConstraintsOnTable(poco);
        }

        /// <summary>
        /// Disables foreign key constraints on a particular database table
        /// </summary>
        /// <param name="pocoName">poco for which foreign key constraints are to be disabled</param>
        public static void DisableFKConstraintsOnTable(string pocoName)
        {
            string queryStrTemplate = GetQueryStringTemplate(ConstraintManipulationEnum.Disable);
            ManipulateFKConstraintsOnTable(pocoName, queryStrTemplate);
        }

        /// <summary>
        /// Enables foreign key constraints on a particular database table
        /// </summary>
        /// <param name="pocoName">poco for which foreign key constraints are to be enabled</param>
        public static void EnableFKConstraintsOnTable(string pocoName)
        {
            string queryStrTemplate = GetQueryStringTemplate(ConstraintManipulationEnum.Enable);
            ManipulateFKConstraintsOnTable(pocoName, queryStrTemplate);
        }
        #endregion

        #region Private methods utilized by this class
        private static void ManipulateFKConstraintsOnTable(string pocoName, string queryStrTemplate)
        {
            var pocoInfo = _cachedPocoInfoMap[pocoName];
            string tableName = pocoInfo.DbTableName;
            string[] fkConstraintArr = pocoInfo.FKConstraints;

            if (fkConstraintArr.Length == 0)
                return;

            string fkConstraintsCSV = GetFKConstraintsAsCSV(fkConstraintArr);

            string connString = ConfigurationManager.ConnectionStrings[_connectionName].ToString();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                cmd.CommandText = string.Format(queryStrTemplate, tableName, fkConstraintsCSV);
                cmd.ExecuteNonQuery();

                conn.Close();
            }
        }

        private static string GetQueryStringTemplate(ConstraintManipulationEnum choice)
        {
            switch(choice)
            {
                case ConstraintManipulationEnum.Disable:
                    return @"ALTER TABLE {0} NOCHECK CONSTRAINT {1}";

                case ConstraintManipulationEnum.Enable:
                    return @"ALTER TABLE {0} WITH CHECK CHECK CONSTRAINT {1}";

                default:
                    throw new Exception($"Enum value {choice.ToString()} not supported!");
            }
        }
        
        private static string GetFKConstraintsAsCSV(params string[] fkConstraints)
        {
            //Now create the comma separated list of foreign key constraints to be TEMPORARILY DISABLED
            StringBuilder fkConstraintsStrBldr = new StringBuilder();
            for (int i = 0; i < fkConstraints.Length; i++)
            {
                var fkConstraintName = fkConstraints[i];
                string fkConstraintsStrDelimiter = ((i == fkConstraints.Length - 1) ? "" : ", ");

                fkConstraintsStrBldr.Append(fkConstraintName);
                fkConstraintsStrBldr.Append(fkConstraintsStrDelimiter);
            }

            return fkConstraintsStrBldr.ToString();
        }
        #endregion
    }

    internal class PocoInfo
    {
        public string DbTableName { get; private set; }
        public string[] FKConstraints { get; private set; }

        public PocoInfo(string tableName, string[] fkConstraints)
        {
            DbTableName = tableName;
            FKConstraints = fkConstraints;
        }
    }

    enum ConstraintManipulationEnum
    {
        Enable = 0,
        Disable
    }
}