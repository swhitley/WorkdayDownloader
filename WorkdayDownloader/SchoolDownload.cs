using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Configuration;
using WorkdayDownloader.WD_TalentService;

namespace WorkdayDownloader
{
    class Schools
    {
        public static void Download(string[] cred, string table, DataSet dta, string[] columns, AppConfig appConfig)
        {
            DataRow row = null;
            string version = ConfigurationManager.AppSettings["Version"];
            string tenant = ConfigurationManager.AppSettings["Tenant"];

            // Create then "client"
            TalentPortClient client = new TalentPortClient("Talent");

            // Set the WS-Security Credentials
            client.ClientCredentials.UserName.UserName = cred[0] + "@" + tenant;
            client.ClientCredentials.UserName.Password = cred[1];


            // Define the paging defaults
            decimal totalPages = 1;
            decimal currentPage = 1;
            decimal countSize = 200;

            // Set the current date/time
            //DateTime currentDateTime = DateTime.UtcNow;
            DateTime currentDateTime = DateTime.Now;

            // Loop over all of the pages in the web service response
            while (totalPages >= currentPage)
            {
                // Create a "request" object
                Get_Schools_RequestType request = new Get_Schools_RequestType();

                // Set the WWS version desired
                request.version = version;

                // Set the date/time & page parameters in the request
                request.Response_Filter = new Response_FilterType();
                request.Response_Filter.As_Of_Entry_DateTime = currentDateTime;
                request.Response_Filter.As_Of_Entry_DateTimeSpecified = true;
                request.Response_Filter.Page = currentPage;
                request.Response_Filter.PageSpecified = true;
                request.Response_Filter.Count = countSize;
                request.Response_Filter.CountSpecified = true;

                // Set the desired response group(s) to return
                request.Response_Group = new School_Response_GroupType();
                request.Response_Group.Include_Reference = true;
                request.Response_Group.Include_ReferenceSpecified = true;


                // Create a "response" object
                Get_Schools_ResponseType response = client.Get_Schools(request);

                // Access all schools
                for (int i = 0; i < response.Response_Data.Length; i++)
                {
                    if (response.Response_Data[i] != null)
                    {
                        row = dta.Tables[0].Rows.Add();
                        string country = "";
                        foreach (CountryObjectIDType countryId in response.Response_Data[i].School_Data.Country_Reference.ID)
                        {
                            if (countryId.type == "ISO_3166-1_Alpha-3_Code")
                            {
                                country = countryId.Value;
                            }
                        }
                        row["COUNTRY"] = country;
                        row["SCHOOL_CD"] = response.Response_Data[i].School_Data.ID;
                        string schoolDescr = response.Response_Data[i].School_Data.School_Name;
                        if (schoolDescr.Length > 30)
                        {
                            schoolDescr = schoolDescr.Substring(0, 30);
                        }
                        row["SCHOOL_DESC"] = schoolDescr;
                        row["DESCR_LONG"] = response.Response_Data[i].School_Data.School_Name;
                        string state = "";
                        if (response.Response_Data[i].School_Data.Country_Region_Reference != null)
                        {
                            state = response.Response_Data[i].School_Data.Country_Region_Reference.Descriptor;
                        }
                        row["STATE_DESCR"] = state;
                        row["COUNTRY_DESCR"] = response.Response_Data[i].School_Data.Country_Reference.Descriptor;
                        row["MODIFYDATE"] = DateTime.Now.ToString("s");
                        row["ACTIONFLAG"] = "I";
                    }
                }
                bool truncate = true;
                if (currentPage > 1)
                {
                    truncate = false;
                }
                Database.Write(dta, table, truncate, appConfig);
                dta.Tables[0].Rows.Clear();

                // Update page number
                if (totalPages == 1) totalPages = response.Response_Results.Total_Pages;
                currentPage++;

            }

            client.Close();
        }
    }
}
