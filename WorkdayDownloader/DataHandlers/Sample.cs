using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace WorkdayDownloader
{
    partial class DataHandler
    {
        /// <summary>
        /// Sample_Tbl
        /// The name of this method must match the program argument for the database table name + the suffix _Handler.
        /// Example:  Sample_Table_Handler -- Where Sample_Table is the table name that was supplied as an input parameter to this application.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="dta"></param>
        public static void Sample_Tbl_Handler(SqlConnection connection, ref DataSet dta)
        {
            //Depending on the report, blank fields may exist at the start of a row.  This process copies down the fields from the previous non-blank row.
            //Fill the primary keys if there are empty fields at the start of a row.
            //Increase the parameter to the number of fields that should be copied at the start of a row if those fields are blank.
            dta.Tables[0].Rows.FillKeyCols(1);

            //Sample process - Cycle through the rows of data and make changes if necessary.
            foreach (DataRow row in dta.Tables[0].Rows)
            {
                //Format phone fields.
                if (!string.IsNullOrEmpty(row["PHONE"].ToString()))
                {
                    row["FORMAT_PHONE"] = row["PHONE"].ToString().FormatPhone();
                }
                if (row["FORMAT_PHONE"] != null && row["FORMAT_PHONE"].ToString().Length > 24)
                {
                    row["FORMAT_PHONE"] = row["FORMAT_PHONE"].ToString().Substring(0, 24);
                }

                if (!string.IsNullOrEmpty(row["FAX"].ToString()))
                {
                    row["FORMAT_FAX"] = row["FAX"].ToString().FormatPhone();
                }
                if (row["FORMAT_FAX"] != null && row["FORMAT_FAX"].ToString().Length > 24)
                {
                    row["FORMAT_FAX"] = row["FORMAT_FAX"].ToString().Substring(0, 24);
                }

                //Remove formatting characters.
                if (!string.IsNullOrEmpty(row["PHONE"].ToString()))
                {
                    Regex rgx = new Regex("[^0-9A-Za-z ~]");
                    row["PHONE"] = rgx.Replace(row["PHONE"].ToString().FormatPhone(), "").Trim();
                }
                if (!string.IsNullOrEmpty(row["FAX"].ToString()))
                {
                    Regex rgx = new Regex("[^0-9A-Za-z ~]");
                    row["FAX"] = rgx.Replace(row["FAX"].ToString().FormatPhone(), "").Trim();
                }

            }
        }

    }
}
