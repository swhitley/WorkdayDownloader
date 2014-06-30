
Workday* Downloader
==================


General
-------

This console application connects to a Workday RaaS (CSV) endpoint and downloads the data to a Microsoft SQL Server table. 

If the Workday report fields and SQL server columns match, minimal configuration is required.


Installation
------------

A sample app.config file is provided.  Most of the configuration is done in app.config.

The application can also point to an AppConfig table for easier maintenance.  The AppConfig table is designed to hold the report-related items only.  It is not meant to replace app.config.

`CREATE TABLE [AppConfig](
	[Group] [varchar](30),
	[Key] [varchar](255),
	[Value] [varchar](8000),
	[Comment] [varchar](255),
	[LastUpdated] [datetime]
)
`
Example AppConfig Data:

	INSERT INTO AppConfig VALUES ('WorkdayDownloader','SAMPLE_TABLE_URL','{user name}/sample_report?format=csv',NULL,GETDATE())
	

Data Handlers

	Additional code can be added to the project very easily under the \DataHandlers folder.  See the Sample.cs file.
	The program looks for a data handling class that matches the name of the table.  If that class is found, the additional code is automatically executed.



Operations
----------

The application can be run as follows:  
	
	WorkdayDownloader.exe {Table Name}

	Example: WorkdayDownloader.exe Employee_Data

Additional parameters can be included: 

	WorkdayDownloader.exe Benefit_Deductions FromDate=2014-01-01,ThruDate=2014-12-31
	
	The parameters are used to replace values in the URL.  The url would contain the matching replacement values, %%FromDate%% and %%ThruDate%%
	
		Example:  {user name}/sample_report?format=csv&ParmFromDate=%%FromDate%%&ParmThruDate=%%ThruDate%%
	
	


<nowiki>*</nowiki> This code has not been endorsed by Workday.
