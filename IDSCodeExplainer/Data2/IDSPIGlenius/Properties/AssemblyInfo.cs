using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Rhino.PlugIns;

// Plug-In title and Guid are extracted from the following two attributes
[assembly: AssemblyTitle("IDS Glenius")] // Plug-In title is extracted from this
[assembly: Guid("83f455c6-c815-44c9-8aa2-1cf7ce780b91")] // This will also be the Guid of the Rhino plug-in

// Plug-in Description Attributes - all of these are optional
// These will show in Rhino's option dialog, in the tab Plug-ins
[assembly: PlugInDescription(DescriptionType.Address, "Technologielaan 15\n3001 Leuven (Heverlee)\nBelgium")]
[assembly: PlugInDescription(DescriptionType.Country, "Belgium")]
[assembly: PlugInDescription(DescriptionType.Email, "yurrit.avonds@materialise.be")]
[assembly: PlugInDescription(DescriptionType.Phone, "+32 (0)16 744 970")]
[assembly: PlugInDescription(DescriptionType.Organization, "Materialise")]
[assembly: PlugInDescription(DescriptionType.WebSite, "http://www.materialise.be")]


// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyDescription("Implant Design Suite for the Materialise Glenius Implant")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Materialise")]
[assembly: AssemblyProduct("")]
[assembly: AssemblyCopyright("Copyright © 2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// Version information for an assembly consists of the following four values:
//
// Major Version Minor Version Build Number Revision
//
// You can specify all the values or you can default the Revision and Build Numbers by using the '*'
// as shown below:
[assembly: AssemblyVersion("3.2.0.0")]
[assembly: AssemblyFileVersion("3.2.0.0")]
