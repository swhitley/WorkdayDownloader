using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Reflection;

namespace WorkdayDownloader
{
    public static class Database
    {
        public static void Write(DataSet dta, string tableName, bool truncate, AppConfig appConfig)
        {
            // get your connection string
            string connString = "";

            connString = appConfig.Value(tableName, "CONN");
            if (connString == null)
            {
                connString = ConfigurationManager.AppSettings["Default_CONN"];
            }

            // connect to SQL
            using (SqlConnection connection =
                    new SqlConnection(connString))
            {
                connection.Open();

                ///
                //Special Column Reformatting for some tables.
                ///
                //Dynamic datahandler based on table name.
                object[] arguments = new object[2] {connection, dta};
                DataHandler dh = new DataHandler();
                Type t = dh.GetType();
                //All methods have the name "tablename" + "_Handler"
                MethodInfo mi = t.GetMethod(tableName + "_Handler", BindingFlags.Static | BindingFlags.IgnoreCase | BindingFlags.Public);
                if (mi != null)
                {
                    mi.Invoke(dh, arguments);
                    //The dataset may have changed, so return the argument after the method completes.
                    dta = (DataSet)arguments[1];
                }
                else
                {
                    dta.Tables[0].Rows.FillKeyCols(1);
                }


                //Transaction
                SqlTransaction transaction;

                // Start a local transaction.
                transaction = connection.BeginTransaction();

                try
                {
                    if (truncate)
                    {
                        //truncate the table
                        SqlCommand cmd = new SqlCommand("truncate table @table", connection);
                        cmd.Parameters.AddWithValue("@table", tableName);
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();
                    }

                    //Bulk Copy Update
                    SqlBulkCopy bulkCopy =
                        new SqlBulkCopy
                        (
                        connection,
                        SqlBulkCopyOptions.TableLock,
                        transaction
                        );

                    // set the destination table name
                    bulkCopy.DestinationTableName = tableName;

                    // write the data in the "dataTable"
                    bulkCopy.WriteToServer(dta.Tables[0]);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    if (connection != null && connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                    throw;
                }
                connection.Close();
            }
        }

    }
}
