using System;
using System.Reflection;
using static Xcst.AssemblyInfo;

[assembly: AssemblyVersion(XcstAssemblyVersion)]
[assembly: AssemblyFileVersion(XcstAssemblyFileVersion)]
[assembly: AssemblyInformationalVersion(XcstAssemblyInformationalVersion)]

namespace Xcst {

   partial class AssemblyInfo {

      public const string
      XcstMajorMinor = "0.0";

      public const string
      XcstAssemblyVersion = "2.0.0";

      public const string
      XcstAssemblyFileVersion = XcstMajorMinor + "." + XcstPatch;

      public const string
      XcstAssemblyInformationalVersion = XcstAssemblyFileVersion;
   }
}
