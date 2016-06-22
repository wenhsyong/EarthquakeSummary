Description
===========
 
EarthquakeSummary is a WinForm application. It requires the following file for checking city location nearby a earthquake location:

C:\USGS Earthquake Summary\worldcities.csv

Notice that above file name and path is configurable in application configuration file, App.config, as the value for key "WorldCitiesFile".

Build EarthquakeSummary aplication
==================================

The application uses the following specific references other than the standard ones:
1. KdTreeLib.dll (An open source C# kd-tree library. Available at "https://github.com/codeandcats/KdTree")
2. Microsoft.VisualBasic
3. Netwonsoft.Json (part of VS2013 installation, standard location: C:\Program Files (x86)\Microsoft Visual Studio 12.0\Blend)
4. System.Configuration

The "EarthquakeSummary.sln" contains two projects:
1. EarthquakeSummary.csproj
2. KdTreeLib.csproj

Follow the steps below to build the application:

1. Download open source KdTree C# implementation from "https://github.com/codeandcats/KdTree". 
2. Open "EarthquakeSummary.sln" in VS2013.
3. Add KdTreeLib.csproj into the solution.
4. Make sure all references listed above are added.
5. Build "Debug" or "Release" comfiguration of "Any CPU" platform.
