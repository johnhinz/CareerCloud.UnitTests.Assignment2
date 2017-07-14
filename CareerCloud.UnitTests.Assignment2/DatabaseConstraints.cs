using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
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
            //EXEC sp_MSforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT all"
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                using (SqlCommand cmd = new SqlCommand("sp_msforeachtable", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@command1", "ALTER TABLE ? NOCHECK CONSTRAINT all"));
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void EnableAll()
        {
            //   EXEC sp_msforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all"
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                using (SqlCommand cmd = new SqlCommand("sp_msforeachtable", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@command1", "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all"));
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
