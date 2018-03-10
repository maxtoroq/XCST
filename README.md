[XCST][1] — eXtensible C-Sharp Templates
========================================
XCST is a language for transforming objects into XML documents. It is based on a subset of XSLT (no `apply-templates`) but using C# as expression language and data model, instead of XPath.

See the [project home][1] for more information.

[![Build status](https://ci.appveyor.com/api/projects/status/93bvxpo3x4bg2po8?svg=true)](https://ci.appveyor.com/project/maxtoroq/xcst)
[![NuGet](https://img.shields.io/nuget/v/Xcst.Runtime.svg?label=Xcst.Runtime)](https://www.nuget.org/packages/Xcst.Runtime)
[![NuGet](https://img.shields.io/nuget/v/Xcst.Compiler.svg?label=Xcst.Compiler)](https://www.nuget.org/packages/Xcst.Compiler)

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

[1]: http://maxtoroq.github.io/XCST/
