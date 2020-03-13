﻿/*
BSD 3-Clause License

Copyright (c) 2020, Brandt Redd
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

* Neither the name of the copyright holder nor the names of its
  contributors may be used to endorse or promote products derived from
  this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using FileMeta;

namespace ReadAndConvertCovid19Data
{

    using DataSet = Dictionary<DateTime, Dictionary<DataKey, DataRecord>>;

    class Program
    {
        const string c_message =
@"Source is the the current COVID-19 data posted by Johns Hopkins University
on GitHub at https://github.com/CSSEGISandData/COVID-19

Data are reorganized into a .csv file suitable for analysis in Microsoft
Excel or Tableau or other analytics tools. For more information, see the
open source project at
https://github.com/FileMeta/ReadAndConvertCovid19Data
";

        const string c_covid19ConfirmedUrl = "https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/time_series_19-covid-Confirmed.csv";
        const string c_covid19DeathsUrl = "https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/time_series_19-covid-Deaths.csv";
        const string c_covid19RecoveredUrl = "https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/time_series_19-covid-Recovered.csv";
        const string c_covid19OutputFilename = "COVID-19-Time-Series-csse.csv";

        // UTF8 encoding with now byte-order mark.
        static Encoding s_UTF8_No_BOM = new UTF8Encoding(false, true);

        enum DataType { Confirmed, Deaths, Recovered };

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine(c_message);

                var dataSet = new DataSet();
                Console.WriteLine("Reading from " + c_covid19ConfirmedUrl);
                ReadData(c_covid19ConfirmedUrl, dataSet, DataType.Confirmed);
                Console.WriteLine("Reading from " + c_covid19DeathsUrl);
                ReadData(c_covid19DeathsUrl, dataSet, DataType.Deaths);
                Console.WriteLine("Reading from " + c_covid19RecoveredUrl);
                ReadData(c_covid19RecoveredUrl, dataSet, DataType.Recovered);

                // Generate filename
                string outPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), c_covid19OutputFilename);

                Console.WriteLine();
                Console.WriteLine($"Writing combined time series data to '{outPath}'.");
                WriteData(dataSet, outPath);

                Console.WriteLine($"Done.");
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }

            Win32Interop.ConsoleHelper.PromptAndWaitIfSoleConsole();
        }

        // Even with this cache policy, the underlying system still caches for up to five minutes
        // There are a lot of discussions about this on StackOverflow. The only functional solution
        // is to p/invoke to DeleteUrlCacheEntry
        static readonly System.Net.Cache.HttpRequestCachePolicy s_cachePolicy =
            new System.Net.Cache.HttpRequestCachePolicy(System.Net.Cache.HttpRequestCacheLevel.BypassCache);

        static void ReadData(string url, DataSet dataSet, DataType dataType)
        {
            HttpWebRequest.DefaultCachePolicy = s_cachePolicy;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.CachePolicy = s_cachePolicy;
            request.Headers.Set("Cache-Control", "max-age=0, no-cache, no-store");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            //Expected Header: Province/State,Country/Region,Lat,Long,1/22/20,other dates
            using (var reader = new CsvReader(new StreamReader(response.GetResponseStream(), Encoding.UTF8, true), true))
            {
                // Read the header line
                var header = reader.Read();

                // Ensure the format is what we expect
                if (header[0] != "Province/State"
                    || header[1] != "Country/Region"
                    || header[2] != "Lat"
                    || header[3] != "Long"
                )
                {
                    throw new Exception("Unexpected data header format.");
                }

                // Read the dates
                var dates = new List<DateTime>();
                for (int i=4; i<header.Length; ++i)
                {
                    dates.Add(ParseSimpleDate(header[i]));
                }

                // Read the data
                for (; ; )
                {
                    var line = reader.Read();
                    if (line == null) break;

                    if (line.Length != dates.Count + 4)
                    {
                        throw new Exception($"Unexpected input data. Expected {dates.Count} entries in the date series. Found {line.Length - 4}.");
                    }

                    for (int i=0; i<dates.Count; ++i)
                    {
                        AddData(dataSet, dates[i], line[0], line[1], line[2], line[3], int.Parse(line[i + 4]), dataType);
                    }
                }
            }
        }

        static void WriteData(DataSet dataSet, string path)
        {
            using (var writer = new StreamWriter(path, false, s_UTF8_No_BOM))
            {
                writer.NewLine = "\n";
                writer.WriteLine("\"Date\",\"ProvinceState\",\"CountryRegion\",\"Lat\",\"Long\",\"Confirmed\",\"Deaths\",\"Recovered\"");

                var dateList = new List<KeyValuePair<DateTime, Dictionary<DataKey, DataRecord>>>(dataSet);
                dateList.Sort((a, b) => a.Key.CompareTo(b.Key));
                foreach(var datePair in dateList)
                {
                    var recordList = new List<KeyValuePair<DataKey, DataRecord>>(datePair.Value);
                    recordList.Sort((a, b) => a.Key.CompareTo(b.Key));
                    foreach(var recordPair in recordList)
                    {
                        writer.WriteLine(ToString(recordPair));
                    }
                }
            }
        }

        private static DateTime ParseSimpleDate(string strDate)
        {
            string[] parts = strDate.Split('/');
            if (parts.Length != 3) throw new Exception($"Unexpected date format: '{strDate}'");
            int year = int.Parse(parts[2]) + 2000;
            int month = int.Parse(parts[0]);
            int day = int.Parse(parts[1]);

            var date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
            //Console.WriteLine(date);
            return date;
        }

        private static void AddData(DataSet dataset, DateTime date, string provinceState, string countryRegion, string latitude, string longitude, int data, DataType dataType)
        {
            Dictionary<DataKey, DataRecord> dateDict;
            if (!dataset.TryGetValue(date, out dateDict))
            {
                dateDict = new Dictionary<DataKey, DataRecord>();
                dataset.Add(date, dateDict);
            }

            var dataKey = new DataKey(date, provinceState, countryRegion, latitude, longitude);

            DataRecord dataRecord;
            if (!dateDict.TryGetValue(dataKey, out dataRecord))
            {
                dataRecord = new DataRecord();
                dateDict.Add(dataKey, dataRecord);
            }

            switch (dataType)
            {
                case DataType.Confirmed:
                    dataRecord.Confirmed = data;
                    break;

                case DataType.Deaths:
                    dataRecord.Deaths = data;
                    break;

                case DataType.Recovered:
                    dataRecord.Recovered = data;
                    break;

                default:
                    Debug.Fail("Unexpected DataType");
                    break;
            }
        }

        static string ToString(KeyValuePair<DataKey, DataRecord> pair)
        {
            return String.Join(",",
                pair.Key.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                string.Concat("\"", pair.Key.ProvinceState, "\""),
                string.Concat("\"", pair.Key.CountryRegion, "\""),
                pair.Key.Latitude,
                pair.Key.Longitude,
                pair.Value.Confirmed,
                pair.Value.Deaths,
                pair.Value.Recovered);
        }

    } // Class Program

    class DataKey : IComparable<DataKey>
    {
        public DateTime Date { get; private set; }
        public string ProvinceState { get; private set; }
        public string CountryRegion { get; private set; }
        public string Latitude { get; private set; }
        public string Longitude { get; private set; }

        public DataKey(DateTime datex, string provinceState, string countryRegion, string latitude, string longitude)
        {
            Date = datex;
            ProvinceState = provinceState;
            CountryRegion = countryRegion;
            Latitude = latitude;
            Longitude = longitude;
        }

        public override int GetHashCode()
        {
            return Date.GetHashCode()
                ^ ProvinceState.GetHashCode()
                ^ CountryRegion.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as DataKey;
            if (obj == null) return false;

            return Date.Equals(other.Date)
                && ProvinceState.Equals(other.ProvinceState)
                && CountryRegion.Equals(other.CountryRegion);
        }

        public int CompareTo(DataKey other)
        {
            int i = Date.CompareTo(other.Date);
            if (i != 0) return i;
            i = CountryRegion.CompareTo(other.CountryRegion);
            if (i != 0) return i;
            return ProvinceState.CompareTo(other.ProvinceState);
        }
    }

    class DataRecord
    {
        public int Confirmed;
        public int Deaths;
        public int Recovered;
    }
}