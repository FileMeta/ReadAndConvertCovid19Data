/*
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

/*
 * Coding style disclaimer:
 * This is a quick hack - intended to last a few months during the COVID-19
 * crisis. It's not intended to be an example of well-structured code.
 * 
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

        const string c_syntax =
@"Syntax:
   ReadAndConvertCovid19Data -o <filename> [options]

Multiple -o filenames may be specified each with different options thereby
creating multiple outputs with different filtering or aggregation levels.

Options:
  -h
  -?
    Show this help text;

  -country <name>
  -region <name>
    (These options are equivalent)
    Filter data to the specified region or country.

  -state <name>
  -province <name>
    (These options are equivalent)
    Filter data to the specified stateProvince field. It is recommended
    that, when using this option, you also specify -country or -region as the
    same state or province name may occur in multiple countries.

  -county <name>
  -district <name>
    (These options are equivalent)
    Filter data to the specified country, district, or administrative area.
    It is recommended that, when using this option, you also specify -state
    or -province as the same county name may occur in multiple countries.

  -bycountry
  -byregion
    (These options are equivalent)
    Report aggregated data by countryRegion. Breakdown by stateProvince, or
    countyDistrict will not be included.

  -bystate
  -byprovince
    (These options are equivalent)
    Report aggregated data by stateProvince. Breakdown by countyRegion will
    not be included.
    
  -bycounty
  -bydistrict
    (These options are equivalent)
    This is the default, data are reported at the most fine-grained level,
    the county, district, or other administrative area.

  -updated <filename>
    Write the date the of the last data element to the specified file. This
    may be used to keep a webpage updated as to when data have been updated.
";

        // UTF8 encoding with no byte-order mark.
        public static readonly Encoding s_UTF8_No_BOM = new UTF8Encoding(false, true);

        const string c_covidDataUrlPrefix = @"https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_daily_reports/";

        const string c_covid19OutputFilename = "COVID-19-Time-Series-csse.csv";
        const string c_updatedOutputFilename = "COVID-19-Updated.txt";

        enum DataType { Confirmed, Deaths/*, Recovered*/ };

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine(c_message);

                // Parse the command line
                var datasets = new List<DataSet>();
                string updatedPath = null;
                bool showHelp = args.Length == 0;
                {
                    DataSet currentDataset = null;
                    for (int i = 0; i < args.Length; ++i)
                    {
                        switch (args[i].ToLowerInvariant())
                        {
                            case "-o":
                                ++i;
                                if (i >= args.Length) throw new ArgumentException("Expected filename after -o command-line argument.");
                                currentDataset = new DataSet();
                                currentDataset.OutputPath = ArgumentToPath(args[i], c_covid19OutputFilename);
                                datasets.Add(currentDataset);
                                break;

                            case "-country":
                            case "-region":
                                ++i;
                                if (i >= args.Length) throw new ArgumentException("Expected name after -country or -region.");
                                if (currentDataset == null) throw new ArgumentException("Must specify -o filename before filtering.");
                                currentDataset.FilterCountryRegion = args[i];
                                break;

                            case "-state":
                            case "-province":
                                ++i;
                                if (i >= args.Length) throw new ArgumentException("Expected name after -state or -province.");
                                if (currentDataset == null) throw new ArgumentException("Must specify -o filename before filtering.");
                                currentDataset.FilterProvinceState = args[i];
                                break;

                            case "-county":
                            case "-district":
                                ++i;
                                if (i >= args.Length) throw new ArgumentException("Expected name after -county or district.");
                                if (currentDataset == null) throw new ArgumentException("Must specify -o filename before filtering.");
                                currentDataset.FilterCountyDistrict = args[i];
                                break;

                            case "-bycountry":
                            case "-byregion":
                                if (currentDataset == null) throw new ArgumentException("Must specify -o filename before aggregation.");
                                currentDataset.AggregationLevel = GeographicLevel.CountryRegion;
                                break;

                            case "-bystate":
                            case "-byprovince":
                                if (currentDataset == null) throw new ArgumentException("Must specify -o filename before aggregation.");
                                currentDataset.AggregationLevel = GeographicLevel.ProvinceState;
                                break;

                            case "-bycounty":
                            case "-bydistrict":
                                if (currentDataset == null) throw new ArgumentException("Must specify -o filename before aggregation.");
                                currentDataset.AggregationLevel = GeographicLevel.CountyDistrict;
                                break;

                            case "-updated":
                                ++i;
                                if (i >= args.Length) throw new ArgumentException("Expected filename after -updated.");
                                updatedPath = ArgumentToPath(args[i], c_updatedOutputFilename);
                                break;

                            case "-h":
                            case "-?":
                                showHelp = true;
                                break;

                            default:
                                throw new ArgumentException("Unexpected command-line argument: " + args[i]);
                        }
                    }
                }

                if (showHelp)
                {
                    Console.WriteLine(c_syntax);
                }
                else
                {
                    // Read the data
                    for (int dateIndex = 0; ReadData(dateIndex, datasets); ++dateIndex) ;

                    Console.WriteLine();

                    DateTime lastDate = DateTime.MinValue;
                    foreach (var dataset in datasets)
                    {
                        dataset.WriteData();
                        if (lastDate < dataset.LastDate) lastDate = dataset.LastDate;
                    }

                    if (!string.IsNullOrEmpty(updatedPath))
                    {
                        Console.WriteLine($"Writing updated date to '{updatedPath}'.");
                        using (var writer = new StreamWriter(updatedPath, false, s_UTF8_No_BOM))
                        {
                            writer.WriteLine(lastDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));
                        }
                    }
                }

                Console.WriteLine();
                Console.WriteLine("Done.");
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
                Console.WriteLine("Use \"-h\" command-line option to view syntax and help.");
            }

            Win32Interop.ConsoleHelper.PromptAndWaitIfSoleConsole();
        }

        static string ArgumentToPath(string arg, string defaultFilename)
        {
            string path = Path.GetFullPath(arg);
            if (Directory.Exists(path))
            {
                return Path.Combine(path, defaultFilename);
            }
            if (Directory.Exists(Path.GetDirectoryName(path)))
            {
                return path;
            }
            throw new Exception($"Invalid filename or path: {arg}");
        }

        // Even with this cache policy, the underlying system still caches for up to five minutes
        // There are a lot of discussions about this on StackOverflow. The only functional solution
        // is to p/invoke to DeleteUrlCacheEntry
        static readonly System.Net.Cache.HttpRequestCachePolicy s_cachePolicy =
            new System.Net.Cache.HttpRequestCachePolicy(System.Net.Cache.HttpRequestCacheLevel.BypassCache);

        static bool ReadData(int dateIndex, IEnumerable<DataSet> datasets)
        {
            string url = $"{c_covidDataUrlPrefix}{IndexToDate(dateIndex).ToString("MM-dd-yyyy")}.csv";
            Console.WriteLine("Reading from " + url);

            MemoryStream memStream;
            try
            {
                HttpWebRequest.DefaultCachePolicy = s_cachePolicy;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.CachePolicy = s_cachePolicy;
                request.Headers.Set("Cache-Control", "max-age=0, no-cache, no-store");
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                // StreamReader seems to have trouble with an HTTPWebResponse stream. When the response re-buffers
                // we get an end of file. To compensate, read to a memory stream first and then parse.
                memStream = new MemoryStream();
                using (var httpStream = response.GetResponseStream())
                {
                    httpStream.CopyTo(memStream);
                }
                memStream.Position = 0;
            }
            catch (WebException err)
            {
                var response = err.Response as HttpWebResponse;
                if (response != null && response.StatusCode == HttpStatusCode.NotFound) return false;
                throw;
            }

            //Expected Header: FIPS, Admin2, Province_State, Country, Last_Update, Lat, Long_, Confirmed, Deaths, Recovered, Active, Combined_Key
            using (var reader = new CsvReader(new StreamReader(memStream, Encoding.UTF8, true), true))
            {
                // Read the header line
                var header = reader.Read();

                /* Known layouts:
                 * Province/State, Country/Region, Last Update, Confirmed, Deaths, Recovered
                 * Province/State, Country/Region, Last Update, Confirmed, Deaths, Recovered, Latitude, Longitude
                 * FIPS, Admin2, Province_State, Country_Region, Last_Update, Lat, Long_, Confirmed, Deaths, Recovered, Active, Combined_Key
                 */

                int countyIndex = -1;
                int stateIndex = -1;
                int countryIndex = -1;
                int latIndex = -1;
                int longIndex = -1;
                int confirmedIndex = -1;
                int deathsIndex = -1;
                for (int i = 0; i < header.Length; ++i)
                {
                    switch (header[i].ToLowerInvariant())
                    {
                        case "admin2":
                            countyIndex = i;
                            break;

                        case "province_state":
                        case "province/state":
                            stateIndex = i;
                            break;

                        case "country_region":
                        case "country/region":
                            countryIndex = i;
                            break;

                        case "lat":
                        case "latitude":
                            latIndex = i;
                            break;

                        case "long":
                        case "long_":
                        case "longitude":
                            longIndex = i;
                            break;

                        case "confirmed":
                            confirmedIndex = i;
                            break;

                        case "deaths":
                            deathsIndex = i;
                            break;
                    }
                }

                // Ensure the fields we expect are present
                if (stateIndex < 0 || countryIndex < 0 || confirmedIndex < 0 || deathsIndex < 0)
                {
                    throw new Exception("Unexpected data header format: " + string.Join(",", header));
                }

                // Read the data
                for (; ; )
                {
                    var line = reader.Read();
                    if (line == null) break;

                    // Skip if counts don't parse (may be empty string)
                    int confirmed;
                    if (!int.TryParse(line[confirmedIndex], out confirmed)) continue;
                    int deaths;
                    if (!int.TryParse(line[deathsIndex], out deaths)) continue;

                    string countyDistrict = (countyIndex >= 0) ? line[countyIndex] : string.Empty;
                    string provinceState = line[stateIndex];
                    string countryRegion = line[countryIndex];
                    string latitude = (latIndex >= 0) ? line[latIndex] : string.Empty;
                    string longitude = (longIndex >= 0) ? line[longIndex] : string.Empty;

                    // Special Updates and Cleanup
                    if (countryRegion.Equals("Mainland China", StringComparison.OrdinalIgnoreCase))
                        countryRegion = "China";
                    if (countryRegion.Equals("South Korea", StringComparison.OrdinalIgnoreCase))
                        countryRegion = "Korea, South";
                    if (countryRegion.Equals("US") && countyIndex < 0)
                    {
                        int comma = provinceState.IndexOf(',');
                        if (comma >= 0)
                        {
                            countyDistrict = provinceState.Substring(0, comma).Trim();
                            provinceState = States.DeAbbreviate(provinceState.Substring(comma + 1).Trim());
                            if (countyDistrict.EndsWith(" County", StringComparison.OrdinalIgnoreCase))
                            {
                                countyDistrict = countyDistrict.Substring(0, countyDistrict.Length - 7);
                            }
                        }
                    }
                    if (countyDistrict.Equals("Virgin Islands", StringComparison.OrdinalIgnoreCase)
                        && countryRegion.Equals("US", StringComparison.OrdinalIgnoreCase))
                    {
                        provinceState = "Virgin Islands";
                        countyDistrict = string.Empty;
                    }

                    foreach (var dataset in datasets)
                    {
                        dataset.AddData(dateIndex, countyDistrict, provinceState, countryRegion, latitude, longitude, confirmed, deaths);
                    }
                }
            }

            return true;
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

        // Date of beginning of COVID-19 data
        static readonly DateTime ZeroDate = new DateTime(2020, 01, 22, 0, 0, 0, 0, DateTimeKind.Unspecified);

        static int DateToIndex(DateTime date)
        {
            return (int)date.Subtract(ZeroDate).TotalDays;
        }

        public static DateTime IndexToDate(int dateIndex)
        {
            return ZeroDate.AddDays(dateIndex);
        }


    } // Class Program

    class DataKey : IComparable<DataKey>
    {
        public string CountyDistrict { get; private set; }
        public string ProvinceState { get; private set; }
        public string CountryRegion { get; private set; }
        public string Latitude { get; private set; }
        public string Longitude { get; private set; }

        public DataKey(string countyDistrict, string provinceState, string countryRegion, string latitude, string longitude)
        {
            CountyDistrict = countyDistrict;
            ProvinceState = provinceState;
            CountryRegion = countryRegion;
            Latitude = latitude;
            Longitude = longitude;
        }

        public override int GetHashCode()
        {
            return CountyDistrict.GetHashCode()
                ^ ProvinceState.GetHashCode()
                ^ CountryRegion.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as DataKey;
            if (obj == null) return false;

            return CountyDistrict.Equals(other.CountyDistrict)
                && ProvinceState.Equals(other.ProvinceState)
                && CountryRegion.Equals(other.CountryRegion);
        }

        public int CompareTo(DataKey other)
        {
            int i = CountryRegion.CompareTo(other.CountryRegion);
            if (i != 0) return i;
            i = ProvinceState.CompareTo(other.ProvinceState);
            if (i != 0) return i;
            return CountyDistrict.CompareTo(other.CountyDistrict);
        }
    }

    class DataRecord
    {
        public int Confirmed;
        public int Deaths;
        //public int Recovered;
    }

    enum GeographicLevel
    {
        None = 0,
        CountyDistrict = 1,
        ProvinceState = 2,
        CountryRegion = 3
    }

    class DataSet
    {
        List<Dictionary<DataKey, DataRecord>> m_data = new List<Dictionary<DataKey, DataRecord>>();

        public string OutputPath { get; set; }
        public string FilterCountryRegion { get; set; }
        public string FilterProvinceState { get; set; }
        public string FilterCountyDistrict { get; set; }
        public GeographicLevel AggregationLevel { get; set; }

        public void AddData(int dateIndex, string countyDistrict, string provinceState, string countryRegion, string latitude, string longitude, int confirmed, int deaths)
        {
            // Apply Filters
            if (!string.IsNullOrEmpty(FilterCountryRegion) && !FilterCountryRegion.Equals(countryRegion, StringComparison.OrdinalIgnoreCase))
                return;
            if (!string.IsNullOrEmpty(FilterProvinceState) && !FilterProvinceState.Equals(provinceState, StringComparison.OrdinalIgnoreCase))
                return;
            if (!string.IsNullOrEmpty(FilterCountyDistrict) && !FilterCountyDistrict.Equals(countyDistrict, StringComparison.OrdinalIgnoreCase))
                return;

            // Apply Aggregation
            if (AggregationLevel > GeographicLevel.CountyDistrict)
                countyDistrict = string.Empty;
            if (AggregationLevel > GeographicLevel.ProvinceState)
                provinceState = string.Empty;

            // Locate the date record
            while (dateIndex >= m_data.Count) m_data.Add(new Dictionary<DataKey, DataRecord>());
            var dateDict = m_data[dateIndex];

            // Locate the data record
            var dataKey = new DataKey(countyDistrict, provinceState, countryRegion, latitude, longitude);
            DataRecord dataRecord;
            if (!dateDict.TryGetValue(dataKey, out dataRecord))
            {
                dataRecord = new DataRecord();
                dateDict.Add(dataKey, dataRecord);
            }

            dataRecord.Confirmed += confirmed;
            dataRecord.Deaths += deaths;
        }

        static readonly DataRecord s_zeroRecord = new DataRecord();

        public void WriteData()
        {
            Console.WriteLine($"Writing data to: {OutputPath}");

            using (var writer = new StreamWriter(OutputPath, false, Program.s_UTF8_No_BOM))
            {
                writer.NewLine = "\n";
                writer.WriteLine("\"Date\",\"CountyDistrict\",\"ProvinceState\",\"CountryRegion\",\"Lat\",\"Long\",\"TotalConfirmed\",\"TotalDeaths\",\"NewConfirmed\",\"NewDeaths\",\"Deltaconfirmed\",\"DeltaDeaths\"");

                // We start with two days into the data thereby letting us look back two days for deltas
                for (int i = 2; i < m_data.Count; ++i)
                {
                    var recordList = new List<KeyValuePair<DataKey, DataRecord>>(m_data[i]);
                    recordList.Sort((a, b) => a.Key.CompareTo(b.Key));

                    // Enumerate every record on this date
                    foreach (var recordPair in recordList)
                    {
                        // Look for the two previous records
                        DataRecord prevRecord = null;
                        int n = i - 1;
                        for (; ; )
                        {
                            if (n < 0)
                            {
                                prevRecord = s_zeroRecord;
                                break;
                            }
                            if (m_data[n].TryGetValue(recordPair.Key, out prevRecord)) break;
                            --n;
                        }
                        DataRecord prevPrevRecord = null;
                        --n;
                        for (; ; )
                        {
                            if (n < 0)
                            {
                                prevPrevRecord = s_zeroRecord;
                                break;
                            }
                            if (m_data[n].TryGetValue(recordPair.Key, out prevPrevRecord)) break;
                            --n;
                        }

                        int newConfirmed = recordPair.Value.Confirmed - prevRecord.Confirmed;
                        int deltaConfirmed = newConfirmed - (prevRecord.Confirmed - prevPrevRecord.Confirmed);
                        int newDeaths = recordPair.Value.Deaths - prevRecord.Deaths;
                        int deltaDeaths = newDeaths - (prevRecord.Deaths - prevPrevRecord.Deaths);

                        writer.WriteLine(String.Join(",",
                                Program.IndexToDate(i).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                                string.Concat("\"", recordPair.Key.CountyDistrict, "\""),
                                string.Concat("\"", recordPair.Key.ProvinceState, "\""),
                                string.Concat("\"", recordPair.Key.CountryRegion, "\""),
                                recordPair.Key.Latitude,
                                recordPair.Key.Longitude,
                                recordPair.Value.Confirmed,
                                recordPair.Value.Deaths,
                                newConfirmed,
                                newDeaths,
                                deltaConfirmed,
                                deltaDeaths
                                ));
                    }
                }
            }
        }

        public DateTime LastDate
        {
            get
            {
                return Program.IndexToDate(m_data.Count - 1);
            }
        }

    }

    static class States
    {
        static readonly Dictionary<string, string> s_StateAbbreviations = new Dictionary<string, string>()
        {
            {"AL", "Alabama"},
            {"AK", "Alaska"},
            {"AZ", "Arizona"},
            {"AR", "Arkansas"},
            {"CA", "California"},
            {"CO", "Colorado"},
            {"CT", "Connecticut"},
            {"DE", "Delaware"},
            {"FL", "Florida"},
            {"GA", "Georgia"},
            {"HI", "Hawaii"},
            {"ID", "Idaho"},
            {"IL", "Illinois"},
            {"IN", "Indiana"},
            {"IA", "Iowa"},
            {"KS", "Kansas"},
            {"KY", "Kentucky"},
            {"LA", "Louisiana"},
            {"ME", "Maine"},
            {"MD", "Maryland"},
            {"MA", "Massachusetts"},
            {"MI", "Michigan"},
            {"MN", "Minnesota"},
            {"MS", "Mississippi"},
            {"MO", "Missouri"},
            {"MT", "Montana"},
            {"NE", "Nebraska"},
            {"NV", "Nevada"},
            {"NH", "New Hampshire"},
            {"NJ", "New Jersey"},
            {"NM", "New Mexico"},
            {"NY", "New York"},
            {"NC", "North Carolina"},
            {"ND", "North Dakota"},
            {"OH", "Ohio"},
            {"OK", "Oklahoma"},
            {"OR", "Oregon"},
            {"PA", "Pennsylvania"},
            {"RI", "Rhode Island"},
            {"SC", "South Carolina"},
            {"SD", "South Dakota"},
            {"TN", "Tennessee"},
            {"TX", "Texas"},
            {"UT", "Utah"},
            {"VT", "Vermont"},
            {"VA", "Virginia"},
            {"WA", "Washington"},
            {"WV", "West Virginia"},
            {"WI", "Wisconsin"},
            {"WY", "Wyoming"},
            {"DC", "District of Columbia"},
            {"MH", "Marshall Islands"},
            {"D.C.", "District of Columbia" }
        };

        public static string DeAbbreviate(string ab)
        {
            string name;
            if (s_StateAbbreviations.TryGetValue(ab.ToUpperInvariant(), out name))
            {
                return name;
            }
            return ab;
        }
    }
}
