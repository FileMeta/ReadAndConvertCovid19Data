# ReadAndConvertCovid19Data

Johns Hopkins University posts counts of confirmed cases of COVID-19 infection, deaths due to the virus, and recovered cases on GitHub. As of 20 October 2020 they are posting daily updates to the data.

Please see the [2019 Novel Coronavirus COVID-19 (2019-nCoV) Data Repository by Johns Hopkins CSSE Github Page](https://github.com/CSSEGISandData/COVID-19) for details about the project and the sources of the data. The page also has links to web-based visual dashboards showing the progress of disease spread.

The time series data are a good source for doing your own analysis using tools like Microsoft Excel and Tableau. However the layout used in the original posting is not convenient for those tools.

This program downloads the latest time series data and converts it into a single table .csv file.

## How to Use This Tool

* Download the [latest release](https://github.com/FileMeta/ReadAndConvertCovid19Data/releases) of the application - [ReadAndConvertDovid19Data.exe](https://github.com/FileMeta/ReadAndConvertCovid19Data/releases).
* Execute the app - either from the command-line or by double-clicking in file explorer. It will show you the command-line syntax.

## Sample Command-Lines

* `ReadAndConvertCovid19Data -o Australia.csv -country Australia -bystate`  
Produce a dataset for Australia with breakdown by state.
* `ReadAndConvertCovid19Data -o World.csv -bycountry`  
Produce a dataset for the world with breakdown by country.

## Data Format

Data are in a single table with one row per date-region. The columns are:

* **Date:** Date of this observation.
* **CountyDistrict:** The county or district of this observation. Reporting categories may change over time.
* **ProvinceState:** The Province, or state of this observation.
* **CountryRegion:** The country or region of the observation.
* **Lat:** The latitude of the observation. Not present in early data.
* **Long:** The longitude of the observation. Not present in early data
* **TotalConfirmed:** The cumulative number of confirmed COVID-19 cases to-date in the specified location.
* **TotalDeaths:** The cumulative number of deaths due to COVID-19 to-date in the specified location.
* **NewConfirmed:** New confirmed cases on the specified date.
* **NewDeaths:** Deaths reported on the specified date.
* **SevenDayAvgConfirmed:** Average new confirmed cases in the seven days ending on the specified date.
* **SevenDayAvgDeaths:** Average deaths due to COVID-19 in the seven days ending on the specified date.

Data are sorted by date, then CountryRegion, then ProvinceState, then CountyDistrict

## About the Code
The program is written in C# and the .NET Framework for use on Microsoft Windows. For those using MacOS or Linux it would be very easy to port to .NET Core.

Code is open source released under a [BSD 3-Clause License](https://opensource.org/licenses/BSD-3-Clause).

[CodeBits](https://www.filemeta.org/CodeBit.html) used in this project:

* [CsvReader](https://github.com/FileMeta/CsvReader)
* [ConsoleHelper](https://github.com/FileMeta/ConsoleHelper)