# ReadAndConvertCovid19Data

Johns Hopkins University posts counts of confirmed cases of COVID-19 infection, deaths due to the virus, and recovered cases on GitHub. As of 13 March 2020 they are posting daily updates to the data.

Please see the [2019 Novel Coronavirus COVID-19 (2019-nCoV) Data Repository by Johns Hopkins CSSE Github Page](https://github.com/CSSEGISandData/COVID-19) for details about the project and the sources of the data. The page also has links to web-based visual dashboards showing the progress of disease spread.

The time series data are a good source for doing your own analysis using tools like Microsoft Excel and Tableau. However the layout used in the original posting is not convenient for those tools.

This program downloads the latest time series data, converts into a single-table.csv file, and posts it in the user's default Documents directory.

## How to Use This Tool

* Download the [latest release](https://github.com/FileMeta/ReadAndConvertCovid19Data/releases) of the application - [ReadAndConvertDovid19Data.exe](https://github.com/FileMeta/ReadAndConvertCovid19Data/releases).
* Execute the app - either from the command-line or by double-clicking in file explorer.
* The aggregate data will be posted in &lt;your documents folder&gt;\COVID-19-Time-Series-csse.csv

## Data Format

Data are in a single table with one row per date-region. Numbers are cumulative. The columns are:

* **Date:** Date of this observation.
* **ProvinceState:** The Province, region, or state of this observation. Categories change over time. For example, early data in the U.S. state of Washington are broken down by county whereas later data are for the State of Washington overall. Likewise, there are temporary regions like the "Grand Princess" cruise ship.
* **CountryRegion:** The country or region of the observation.
* **Lat:** The latitude of the observation.
* **Long:** The longitude of the observation.
* **Confirmed:** The cumulative number of confirmed COVID-19 cases to-date in the specified location.
* **Deaths:** The number of deaths due to COVID-19 to-date in the specified location.
* **Recovered:** The number of recovered COVID-19 patients to-date in the specified location.

Data are sorted by date, then CountryRegion, then Province/State.

## About the Code
The program is written in C# and the .NET Framework for use on Microsoft Windows. For those using MacOS or Linux it would be very easy to port to .NET Core.

Code is open source released under a [BSD 3-Clause License](https://opensource.org/licenses/BSD-3-Clause).

[CodeBits](https://www.filemeta.org/CodeBit.html) used in this project:

* [MicroYaml](https://github.com/FileMeta/MicroYaml)
* [ConsoleHelper](https://github.com/FileMeta/ConsoleHelper)

