[XCST] — eXtensible C-Sharp Templates
=====================================
XCST is a language optimized for the production of XML and other formats. It's based on a subset of XSLT, the main difference being there are no special features to work with XML data, instead of XPath you use C# or Visual Basic. XCST is therefore better suited when your primary source of data is not XML.

See the [project home][XCST] for more information.

[![Build status](https://ci.appveyor.com/api/projects/status/93bvxpo3x4bg2po8?svg=true)](https://ci.appveyor.com/project/maxtoroq/xcst)
[![NuGet](https://img.shields.io/nuget/v/Xcst.Runtime.svg?label=Xcst.Runtime)](https://www.nuget.org/packages/Xcst.Runtime)
[![NuGet](https://img.shields.io/nuget/v/Xcst.Compiler.svg?label=Xcst.Compiler)](https://www.nuget.org/packages/Xcst.Compiler)

System Requirements
-------------------
The runtime and APIs are written in C# 8 and require .NET 4.6 or higher. The compiler is written in XSLT 2 and depends on [Saxon-HE].

The [release script](build/release.ps1) (which creates the NuGet packages) and other utility scripts are written in PowerShell 3.

The [XCST schema](schemas/xcst.rng) is written in Relax NG and converted to XSD using [Trang], which requires Java.

The compiler produces code that is compatible with C# 6 and Visual Basic 14.

Building
--------
Run the following commands in PowerShell to build everything (source and tests).

```powershell
# clone
git clone https://github.com/maxtoroq/XCST.git
cd XCST

# restore packages
.\build\restore-packages.ps1

# build solution
MSBuild
```

[XCST]: https://maxtoroq.github.io/XCST/
[Saxon-HE]: http://saxon.sf.net/
[Trang]: https://github.com/relaxng/jing-trang
