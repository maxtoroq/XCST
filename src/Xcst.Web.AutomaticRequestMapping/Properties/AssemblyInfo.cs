using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;
using PreApplicationStartCode = Xcst.Web.AutomaticRequestMapping.PreApplicationStartCode;

[assembly: AssemblyTitle("Xcst.Web.AutomaticRequestMapping.dll")]
[assembly: AssemblyDescription("Xcst.Web.AutomaticRequestMapping.dll")]
[assembly: AssemblyVersion("1.0.0")]
[assembly: AssemblyFileVersion("0.13.0")]
[assembly: AssemblyInformationalVersion("0.13.0")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: PreApplicationStartMethod(typeof(PreApplicationStartCode), nameof(PreApplicationStartCode.Start))]