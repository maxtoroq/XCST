[XCST] — eXtensible C-Sharp Templates
=====================================
XCST is a language optimized for the production of XML and other formats. It's a more general-purpose version of XSLT.

See the [project home][XCST] for more information.

[![Build status](https://ci.appveyor.com/api/projects/status/93bvxpo3x4bg2po8/branch/v2?svg=true)](https://ci.appveyor.com/project/maxtoroq/xcst/branch/v2) ![Tests](https://img.shields.io/appveyor/tests/maxtoroq/XCST/v2)

### Packages Built From This Repository

Package | Description
------- | -----------
[Xcst.Compiler] | Compilation API. Use this package to translate your XCST programs into C# or Visual Basic code.
[Xcst.Runtime] | Runtime and evaluation API.

System Requirements
-------------------
The runtime is written in **C# 8** and requires **.NET 4.6** , **.NET Core 2.0** or **.NET Standard 2.0**.

The compiler is written in XCST itself, ported from and compiled with the v1 compiler written in XSLT 2. Its API is written in C# 8 and requires .NET 4.6 or .NET Core 2.0. It produces code that is compatible with **C# 6** and **Visual Basic 14**.

The [release script](build/release.ps1) (which creates the NuGet packages) and other utility scripts are written in **PowerShell 5.1**.

The [XCST schema](schemas/xcst.rng) is written in **Relax NG** and converted to XSD using [Trang], which requires **Java**.

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
[Xcst.Compiler]: https://www.nuget.org/packages/Xcst.Compiler
[Xcst.Runtime]: https://www.nuget.org/packages/Xcst.Runtime
[Saxon-HE]: http://saxon.sf.net/
[Trang]: https://github.com/relaxng/jing-trang
