using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle( "RvtVa3c" )]
[assembly: AssemblyDescription( "Revit custom exporter add-in generating JSON output for the va3c three.js AEC viewer" )]
[assembly: AssemblyConfiguration( "" )]
[assembly: AssemblyCompany( "Autodesk Inc." )]
[assembly: AssemblyProduct( "RvtVa3c" )]
[assembly: AssemblyCopyright( "Copyright 2014 © Jeremy Tammik Autodesk Inc." )]
[assembly: AssemblyTrademark( "" )]
[assembly: AssemblyCulture( "" )]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible( false )]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid( "321044f7-b0b2-4b1c-af18-e71a19252be0" )]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
//
// History:
// 2014-09-02 2015.0.0.19 minor cleanup before removing scene definition
// 2014-09-03 2015.0.0.20 fixed bug in SelectFile, need to determine full output path
// 2014-09-03 2015.0.0.21 replace top level json container Scene for Object3D
// 2014-09-04 2015.0.0.23 added new models, theo confirmed it works, added name property to materials
//
[assembly: AssemblyVersion( "2015.0.0.25" )]
[assembly: AssemblyFileVersion( "2015.0.0.25" )]
