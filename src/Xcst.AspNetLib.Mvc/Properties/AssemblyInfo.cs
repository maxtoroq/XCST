using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;
using PreApplicationStartCode = Xcst.Web.Mvc.PreApplicationStartCode;

[assembly: AssemblyTitle("Xcst.AspNetLib.Mvc.dll")]
[assembly: AssemblyDescription("Xcst.AspNetLib.Mvc.dll")]
[assembly: AssemblyVersion("1.0.0")]
[assembly: AssemblyFileVersion("0.11.0")]
[assembly: AssemblyInformationalVersion("0.11.0")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: PreApplicationStartMethod(typeof(PreApplicationStartCode), nameof(PreApplicationStartCode.Start))]
