[XCST] — eXtensible C-Sharp Templates
=====================================
XCST is a language optimized for the production of XML and other formats. It's a more general-purpose version of XSLT.

See the [project home][XCST] for more information.

[![Build status](https://ci.appveyor.com/api/projects/status/93bvxpo3x4bg2po8/branch/v2?svg=true)](https://ci.appveyor.com/project/maxtoroq/xcst/branch/v2) ![Tests](https://img.shields.io/appveyor/tests/maxtoroq/XCST/v2)

### Packages Built From This Repository

Package | Description | Targets
------- | ----------- | -------
[Xcst.Compiler] | Compilation API. Use this package to translate your XCST programs into C# or Visual Basic code. | .NET 4.6, .NET Core 2.0
[Xcst.Runtime] | Runtime and evaluation API. | .NET 4.6, .NET Core 2.0, .NET Standard 2.0

Documentation
-------------
The documentation can be found at the [project home][XCST].

About v2
--------
*v2* is the main branch for major version 2. See *v1* for version 1 (no longer maintained).

On v2, the compiler can generate code for runtime v1 or v2 (the default). The runtime is not backwards compatible, programs compiled against v1 must be recompiled to run on v2.

The compiler was rewritten in XCST itself, ported from the v1 compiler written in XSLT 2. Consequently, compiler extensions such as extension instructions and extension attributes must now be implemented in XCST.

The XCST language is still version `1.0` and continues to be refined. Breaking changes are rare and have low impact (e.g. renaming an attribute or element). One of the big new features in v2 are `c:mode` declarations. New language features that require special runtime support are not supported when targeting the v1 runtime.

Tests now run on .NET Core and compatibility with this framework is the priority. Support for .NET Framework and .NET Standard remains for the time being.

System Requirements
-------------------
The compiler produces code that is compatible with **C# 6** and **Visual Basic 14**, although template rules are not useful unless you use C# 7 or higher.

The [release script](build/release.ps1) (which creates the NuGet packages) and other utility scripts require **PowerShell 5.1** or **PowerShell Core**.

The [XCST schema](schemas/xcst.rng) is written in **Relax NG** and converted to XSD using [Trang], which requires **Java**.


[XCST]: https://maxtoroq.github.io/XCST/
[Xcst.Compiler]: https://www.nuget.org/packages/Xcst.Compiler
[Xcst.Runtime]: https://www.nuget.org/packages/Xcst.Runtime
[Trang]: https://github.com/relaxng/jing-trang
