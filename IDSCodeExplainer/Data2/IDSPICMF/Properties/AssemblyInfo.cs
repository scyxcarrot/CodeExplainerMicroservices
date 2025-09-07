using Rhino.PlugIns;
using System.Reflection;
using System.Runtime.InteropServices;

// Plug-In title and Guid are extracted from the following two attributes
[assembly: AssemblyTitle("IDS CMF")]
[assembly: Guid("57DA5397-C27E-4EEE-B820-B4A51D282D3A")]

// Plug-in Description Attributes - all of these are optional
[assembly: PlugInDescription(DescriptionType.Address, "Technologielaan 15\n3001 Leuven (Heverlee)\nBelgium")]
[assembly: PlugInDescription(DescriptionType.Country, "Belgium")]
[assembly: PlugInDescription(DescriptionType.Email, "simon.lejaegere@materialise.be")]
[assembly: PlugInDescription(DescriptionType.Phone, "+32 (0)16 396 611")]
[assembly: PlugInDescription(DescriptionType.Organization, "Materialise")]
[assembly: PlugInDescription(DescriptionType.WebSite, "http://www.materialise.be")]

// GeneralConstants Information about an assembly is controlled through the following set of attributes.
// Change these attribute values to modify the information associated with an assembly.
[assembly: AssemblyDescription("ImplantConstants Design Suite for the Materialise CMF")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Materialise")]
[assembly: AssemblyProduct("")]
[assembly: AssemblyCopyright("Copyright © 2025")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible to COM components. If you
// need to access a type in this assembly from COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// Version information for an assembly consists of the following four values:
//
// Major Version Minor Version Build Number Revision
//
// You can specify all the values or you can default the Revision and Build Numbers by using the '*'
// as shown below:
[assembly: AssemblyVersion("4.610.0.0")]
[assembly: AssemblyFileVersion("4.610.0.0")]