using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;


namespace WorkdayDownloader
{
    public static class DataExtensions
    {
        public static bool AreAllColsEmpty(this DataRow row)
        {
            var itemArray = row.ItemArray;
            if (itemArray == null)
                return true;
            return itemArray.All(x => string.IsNullOrWhiteSpace(x.ToString()));
        }

        public static bool FillKeyCols(this DataRowCollection rows, int keys)
        {
            DataRow prevRow = null;
            List<DataRow> emptyRows = new List<DataRow>();

            //Loop through all of the rows
            foreach (DataRow row in rows)
            {
                //If all cols are empty, remove the row.
                if (row.AreAllColsEmpty())
                {
                    emptyRows.Add(row);
                    continue;
                }
                //Check for key cols that need data from the previous row.
                for (int ctr = 0; ctr < keys; ctr++)
                {
                    if (string.IsNullOrEmpty(row[ctr].ToString()))
                    {
                        row[ctr] = prevRow[ctr];
                    }
                }
                prevRow = row;
            }
            foreach (DataRow removeRow in emptyRows)
            {
                rows.Remove(removeRow);
            }

            return true;
        }

        public static Stream ToStream(this string str)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        //Alter the phone format.
        public static string FormatPhone(this string phone)
        {
            phone = phone.Replace("+1", "").Replace("+", "");
            phone = phone.Replace("(000)", "");
            phone = phone.Replace(" (", ".").Replace(") ", ".");
            phone = phone.Trim();
            if (phone.Length > 0)
            {
                if (phone.Substring(0, 1) == ".")
                {
                    phone = phone.Substring(1).Trim();
                }
            }
            return phone;
        }

        //Remove non-alpha-numeric values with no exceptions.
        public static string AlphaNumericOnly(this string text)
        {
            return AlphaNumericOnly(text, "");
        }

        //Remove non-alpha-numeric values but allow exceptions.
        public static string AlphaNumericOnly(this string text, string exceptions)
        {
            if (text == null) throw new ArgumentNullException("text");

            if (text.Length > 0)
            {
                Regex rgx = new Regex("[^a-zA-Z0-9" + exceptions + "]");
                text = rgx.Replace(text, "");

                return text;
            }

            return text;
        }

    }

        // Removes diacritics from a string
        //
        // Original version by Michael Kaplan: 
        // -> http://blogs.msdn.com/b/michkap/archive/2007/05/14/2629747.aspx
        //
        // Optimized version by Tommy Carlier:
        // -> http://blog.tcx.be/2011/11/recently-i-had-to-write-function-that.html
        public static class DiacriticsRemover
        {
            public static string RemoveDiacritics(this string text)
            {
                if (text == null) throw new ArgumentNullException("text");

                if (text.Length > 0)
                {
                    char[] chars = new char[text.Length];
                    int charIndex = 0;

                    text = text.Normalize(NormalizationForm.FormD);
                    for (int i = 0; i < text.Length; i++)
                    {
                        char c = text[i];
                        if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                            chars[charIndex++] = c;
                    }

                    return new string(chars, 0, charIndex).Normalize(NormalizationForm.FormC);
                }

                return text;
            }
        }
}
