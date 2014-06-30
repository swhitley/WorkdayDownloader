using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace WorkdayDownloader
{
    /// <summary>
    /// Allows config values to be stored in the config file or in a configuration table in SQL Server.
    /// Both locations will be checked, with priority given to the config file.
    /// Configuration table has the following fields:  Group, Key, Value
    /// </summary>
    public class AppConfig
    {
        public Dictionary<string, string> AppSettings = new Dictionary<string, string>();

        public AppConfig(string group, string connString)
        {
            // Connect to SQL
            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();
                //Get the config values.
                SqlCommand cmd = new SqlCommand("SELECT [Key], Value FROM " + ConfigurationManager.AppSettings["AppConfig"] + " WITH(NOLOCK) WHERE [Group]=@group", connection);
                cmd.Parameters.AddWithValue("@group", group);
                SqlDataReader reader = cmd.ExecuteReader();
                while(reader.Read())
                {
                    //Load the dictionary object.
                    AppSettings.Add(reader["Key"].ToString(), reader["Value"].ToString());
                }

            }
        }

        //Return an array for some values when they contain comma-delimited lists.
        public string[] ValueArray(string table, string keyType)
        {
            string ret = Value(table, keyType);

            if (ret != null)
            {
                return ret.Split(',');
            }
            else
            {
                return null;
            }
        }

        public string Value(string table, string keyType)
        {
            string value = null;
            string key = table + "_" + keyType;

            //Check the config file first.
            if (ConfigurationManager.AppSettings.AllKeys.Contains(key))
            {
                value = ConfigurationManager.AppSettings[key];
            }
            else
            {
                //Check for database config values if none exist in the config file.
                if (this.AppSettings.ContainsKey(key))
                {
                    value = this.AppSettings[key];
                }
            }

            return value;
        }

    }       
}
