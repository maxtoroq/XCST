using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;
using PreApplicationStartCode = Xcst.Web.Mvc.PreApplicationStartCode;

[assembly: AssemblyTitle("Xcst.Web.Mvc.dll")]
[assembly: AssemblyDescription("Xcst.Web.Mvc.dll")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: PreApplicationStartMethod(typeof(PreApplicationStartCode), nameof(PreApplicationStartCode.Start))]