using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;
using PreApplicationStartCode = Xcst.Web.PreApplicationStartCode;

[assembly: AssemblyTitle("Xcst.AspNet.dll")]
[assembly: AssemblyDescription("Xcst.AspNet.dll")]
[assembly: AssemblyVersion("1.0.0")]
[assembly: AssemblyFileVersion("0.15.1")]
[assembly: AssemblyInformationalVersion("0.15.1")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: PreApplicationStartMethod(typeof(PreApplicationStartCode), nameof(PreApplicationStartCode.Start))]
