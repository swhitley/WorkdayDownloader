#region About
/*
 * This application downloads data from Workday and inserts the data into a SQL Server table. 
 * 
 */
#endregion
#region License
/*
The MIT License (MIT)

Copyright (c) 2014 Shannon Whitley

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
 */
#endregion


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using WorkdayDownloader.WD_TalentService;
using System.ServiceModel;
using Microsoft.VisualBasic.FileIO;
using System.Web;
using System.Collections.Specialized;


namespace WorkdayDownloader
{
    class Program
    {
        public static string GlobalMessages { get; set; }
        public static AppConfig appConfig;

        static void Main(string[] args)
        {
            DataSet dta = new DataSet();
            string[] columns = null;
            string[] allow_null = null;
            string[] keys = null;
            string[] types = null;
            string table = "";
            string[] cred = null;
            DateTime asOfDate = DateTime.Today;
            bool truncate = true;
            NameValueCollection rptArgs = null;

            //Argument 1 = Table Name
            if(args.Count() > 0)
            {
                table = args[0].ToString();
            }

            //Argument 2 = Report Arguments - Querystring format for replacements or a single date
            if (args.Count() > 1)
            {
                //Use comma as separator instead of ampersand since ampersand is special character in batch files.
                //arg1=val1,arg2=val2...
                //Convert comma to ampersand to use ParseQueryString utility.
                rptArgs = HttpUtility.ParseQueryString(args[1].Replace(",","&"));

                //A single date is allowed as a default argument.
                if (rptArgs.Count == 1 && args[1].ToString().IndexOf("=") < 0)
                {
                    asOfDate = DateTime.Parse(args[1].ToString());
                }

                //Allow AsOfDate override from the argument string.
                if (rptArgs.AllKeys.Contains("AsOfDate"))
                {
                    asOfDate = DateTime.Parse(rptArgs["AsOfDate"].ToString());
                }

            }

            //Argument 3 = Truncate Table - Boolean
            if (args.Count() > 2)
            {
                truncate = bool.Parse(args[2].ToString());
            }


            Console.WriteLine("** Begin Processing for " + table + " -- As Of " + asOfDate.ToString("s"));


            //Load The Config Values
            //Configuration can come from config file or database.  Config file takes precedence.
            appConfig = new AppConfig(ConfigurationManager.AppSettings["ConfigGroup"], ConfigurationManager.AppSettings["Config_CONN"]);


            //Url must exist
            if(appConfig.Value(table, "URL") == null )
            {
                    throw new Exception("The table name " + table + " was not found in the configuration.  Please check the _URL setting.");
            }

            //Column List
            columns = appConfig.ValueArray(table, "COLS");

            //Default of empty string can be overridden by a DBNull
            allow_null = appConfig.ValueArray(table, "NULLS");

            //If these columns are empty, copy down values from the previous row.
            keys = appConfig.ValueArray(table, "KEYS");

            //Explicitly Set Types
            types = appConfig.ValueArray(table, "TYPES");


            //Build the column list.
            if (columns != null)
            {
                dta = ColumnsAdd(table, dta, columns, types, allow_null);
            }


            //Credentials
            if (ConfigurationManager.AppSettings.AllKeys.Contains(table + "_CRED"))
            {
                cred = ConfigurationManager.AppSettings[table + "_CRED"].Split(',');
            }
            else
            {
                cred = ConfigurationManager.AppSettings["Default_CRED"].Split(',');
            }


            if (table == "PS_School_Tbl")
            {
                //Web Api Download
                Schools.Download(cred,table, dta, columns, appConfig);
            }
            else
            {
                //CSV Download
                CSVDownload(cred, table, dta, columns, types, allow_null, keys, asOfDate, truncate, rptArgs);
            }
            
            Console.WriteLine("** End Processing for " + table);

            if (!string.IsNullOrEmpty(GlobalMessages))
            {
                Common.EmailSend("WorkdayDownloader Messages", GlobalMessages);
            }

        }

        

        private static void CSVDownload(string[] cred, string table, DataSet dta, string[] columns, string[] types, string[] allow_null, string[] keys, DateTime asOfDate, bool truncate, NameValueCollection rptArgs)
        {
            DataRow row = null;

            //Connect to the Web Service
            WebClient client = new WebClient();
            client.Encoding = Encoding.UTF8;
            client.Credentials = new NetworkCredential(cred[0], cred[1]);
            string csv = "";

            //Report Url (format=csv)
            string url = "";
            url = appConfig.Value(table, "URL");

            if(url.Length > 0)
            {
                //Check for relative URL
                if (url.Trim().Substring(0, 4).ToLower() != "http")
                {
                    url = ConfigurationManager.AppSettings["URL"] + url;
                }


                string effdt = asOfDate.ToString("s");
                //Future-dated table contains those hired after today, up to 10 days in the future.
                //--Earlier version did not include flexible program arguments.
                //--This is specific to our implementation and will be removed at a later date.
                //--START DEPRECATED--
                if (table == "WD_Personnel_Future_Temp")
                {
                    effdt = asOfDate.AddDays(1).ToString("s");
                    url = url.Replace("%%Hire_Date_From%%", effdt);
                    effdt = asOfDate.AddDays(10).ToString("s");
                }
                url = url.Replace("%%Hire_Date_Thru%%", effdt);
                url = url.Replace("%%Effective_as_of_Date%%", effdt);
                //--END DEPRECATED--

                //Additional Argument Replacements
                if (rptArgs != null)
                {
                    foreach (string key in rptArgs.Keys)
                    {
                        url = url.Replace("%%" + key + "%%", rptArgs[key]);
                    }
                }

                //Download to csv string.
                try
                {
                    csv = client.DownloadString(url);
                }
                catch
                {
                    //Second try if the first download fails.
                    csv = client.DownloadString(url);
                }
            }
            else
            {
                throw new Exception("Table name not found in configuration.");
            }

            if (csv.Length > 0)
            {
                //Read the csv string.
                TextFieldParser reader = new TextFieldParser(csv.ToStream());
                reader.TextFieldType = FieldType.Delimited;
                reader.SetDelimiters(",");

                //Read the csv rows.
                List<string> header = new List<string>();
                int rowIndex = 0;
                string[] cols = reader.ReadFields();
                while (cols != null)
                {
                    //Process the header row.
                    if (rowIndex == 0)
                    {
                        //Default columns from header if _COLS configuration row
                        //was not provided.
                        if (columns == null)
                        {
                            columns = cols;
                            ColumnsAdd(table, dta, columns, types, allow_null);
                        }
                        foreach (string col in cols)
                        {
                            header.Add(col);
                        }
                        cols = reader.ReadFields();
                        if (cols == null)
                        {
                            continue;
                        }
                        if (cols.Length == 0)
                        {
                            throw new Exception("First column header must match configuration.");
                        }
                        rowIndex++;
                        continue;
                    }
                    //Create a new data row.
                    row = dta.Tables[0].Rows.Add();
                    int colIndex = 0;
                    if (rowIndex > 0)
                    {
                        foreach (string col in header)
                        {
                            if (columns.Contains(col, StringComparer.OrdinalIgnoreCase))
                            {
                                if (dta.Tables[0].Columns[col].AllowDBNull && cols[colIndex].ToString().Length == 0)
                                {
                                    //ignore unless null
                                    if (row[col].GetType().ToString() == "System.DBNull")
                                    {
                                        row[col] = DBNull.Value;
                                    }
                                }
                                else
                                {
                                    //Set the column data.
                                    row[col] = cols[colIndex];
                                }
                                //Empty Column
                                //Get data from the previous row for key fields.
                                if (keys != null && cols[colIndex].ToString().Length == 0)
                                {
                                    if (keys.Contains(col, StringComparer.OrdinalIgnoreCase))
                                    {
                                        if (rowIndex > 1)
                                        {
                                            if (colIndex == 0 || dta.Tables[0].Rows[rowIndex - 2][0] == row[0])
                                            {
                                                if (dta.Tables[0].Rows[rowIndex - 2][col] != null)
                                                {
                                                    row[col] = dta.Tables[0].Rows[rowIndex - 2][col];
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            colIndex++;
                        }
                    }
                    cols = reader.ReadFields();
                    rowIndex++;
                }
                //Bulk upload to the database.
                if (dta.Tables[0].Rows.Count > 0)
                {
                    Database.Write(dta, table, truncate, appConfig);
                }
                else
                {
                    //TODO: Make empty report option configurable.
                    //OK for future temp to be empty
                    if(table != "WD_Personnel_Future_Temp")
                    {
                        throw new Exception("No data was downloaded from the report url.");
                    }
                }
            }
            else
            {
                throw new Exception("No data was downloaded from the report url.");
            }

        }

/// <summary>
/// Builds the dataset table by adding columns and setting defaults.
/// </summary>
/// <param name="tableName"></param>
/// <param name="dta"></param>
/// <param name="columns"></param>
/// <param name="types"></param>
/// <param name="allow_null"></param>
/// <returns></returns>
        public static DataSet ColumnsAdd(string tableName, DataSet dta, string[] columns, string[] types, string[] allow_null)
        {
            //Read a row from the database to get the data types.
            Dictionary<string, Type> dataTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);


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
                SqlCommand cmd = new SqlCommand("select top 1 * from " + tableName, connection);
                SqlDataReader rows = cmd.ExecuteReader();
                rows.Read();
                for(int ndx = 0; ndx < rows.FieldCount; ndx++)
                {
                    dataTypes.Add(rows.GetName(ndx), rows.GetFieldType(ndx));
                }
            }

            //Build the column list.
            dta.Tables.Add();
            foreach (string col in columns)
            {
                 //Set the column type -- default is string.
                Type type = null;

                //Set the type from the database
                if (dataTypes.ContainsKey(col))
                {
                    type = dataTypes[col];
                }

                //Override the type to INT
                if (types != null && types.Contains(col + ":INT"))
                {
                    type = new int().GetType();
                }
                else
                {
                    //Override the type to DEC
                    if (types != null && types.Contains(col + ":DEC"))
                    {
                        type = new decimal().GetType();
                    }
                    else
                    {
                        //if type hasn't been set, default to string
                        if (type == null)
                        {
                            //Default to string
                            type = new string(new char[] { ' ' }).GetType();
                        }
                    }
                }

                //Default all values to an empty string unless nulls are allowed in _NULLS config.
                DataColumn c = dta.Tables[0].Columns.Add(col, type);
                
                if (allow_null == null)
                {
                    if (c.DataType.Name == "String")
                    {
                        c.DefaultValue = "";
                    }
                }
                else
                {
                    if (!allow_null.Contains(col, StringComparer.OrdinalIgnoreCase))
                    {
                        c.DefaultValue = "";
                    }
                }
            }
            return dta;
        }
    }
}
