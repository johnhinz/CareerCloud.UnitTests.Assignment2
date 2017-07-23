using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CareerCloud.UnitTests.Assignment2
{
    class DatabaseConstraints
    {
        protected readonly string _connStr;

        public DatabaseConstraints()
        {
            _connStr = ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString;
        }

        public void DisableAll()
        {
            ExecuteCommandForEachTable("ALTER TABLE ? NOCHECK CONSTRAINT all");
           
        }

        public void EnableAll()
        {
            ExecuteCommandForEachTable("ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all"); 
        }

        public void DisableConstraintsForPoco(Type pocoType)
        {
            AlterConstraintsForPoco(pocoType, " NOCHECK CONSTRAINT ALL");
        }

        public void EnableConstraintsForPoco(Type pocoType)
        {
            AlterConstraintsForPoco(pocoType, " WITH CHECK CHECK CONSTRAINT ALL");           
        }

        private void ExecuteCommandForEachTable(string command)
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                using (SqlCommand cmd = new SqlCommand("sp_msforeachtable", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@command1", command));
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private void AlterConstraintsForPoco(Type pocoType, string alterOption)
        {
            string tableName;
            bool success = TryGetTableName(pocoType, out tableName);
            if (success)
            {
                ExecuteCommand(String.Concat("ALTER TABLE ", tableName, alterOption));
            }
        }     

        private void ExecuteCommand(string commandText)
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                using (SqlCommand cmd = new SqlCommand(commandText, conn))
                {                            
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }    
        }

        private bool TryGetTableName(Type pocoType, out string tableName)
        {
            bool result = false;
            tableName = "";
            if (pocoType != null)
            {
                TableAttribute tableAttribute = (TableAttribute)pocoType.GetCustomAttribute(typeof(TableAttribute));
                if (tableAttribute != null)
                {
                    tableName = tableAttribute.Name;
                    result = true;
                }
            }
          
            return result;
        }
    }
}
