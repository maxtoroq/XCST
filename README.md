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

Breaking Changes
----------------
### Language
- [renamed c:metadata to c:meta and 'name' attribute to 'type'](https://github.com/maxtoroq/XCST/commit/1607566fd799b36bc5034e5097831810c9325e14)
- [removed 'html-version' since xhtml output is not supported](https://github.com/maxtoroq/XCST/commit/b36590e9a212dc405e5e25a91c744d8c8bd64ef6)
- [renamed 'display-text-member' to 'text-member'](https://github.com/maxtoroq/XCST/commit/2eee63272488034c2536ba81b61087bb692e0356)
- [renamed 'null-display-text' to 'null-text'](https://github.com/maxtoroq/XCST/commit/df54dad48d4315c2adf67e4e2ccbafca7e08dc34)
- [removed implicit unnamed mode](https://github.com/maxtoroq/XCST/commit/bb1269abf695410a112e5851194768cc6f7b9f88)
- [changed default built-in template rule to fail](https://github.com/maxtoroq/XCST/commit/bf8a3319cf120c1b19c3e257a1f643cc0522d994)
- [don't assign local variable without value](https://github.com/maxtoroq/XCST/commit/216d0e6fa5fa72e8e5ec3fdf01e6a747787319f9)
- [resolve 'validation-resource-type' from c:validation against package namespace, or treat as fully-qualified](https://github.com/maxtoroq/XCST/commit/8a42ce48473a62c94ba3f75248a621eacddbc070)
- [not using 'data-type' for validation](https://github.com/maxtoroq/XCST/commit/944fc8de7741c21e078a64708395032fa1deb1a3)
- [deprecated System.Delegate fallback on invoke-delegate as it hides programming errors](https://github.com/maxtoroq/XCST/commit/0e25b838ffcdc4aabf5a29de76c4848dadeec06d)

### Compiler
- [removed XcstCompilerFactory](https://github.com/maxtoroq/XCST/commit/493f489671bd364ac1dd412e5947aa693d75f185)
- [switched CompileResult.Templates to XName](https://github.com/maxtoroq/XCST/commit/e4709f7e7d754d4ee37c212943a455c1debc34a2)
- [removed PackageTypeResolver](https://github.com/maxtoroq/XCST/commit/888cead3a83d26a4a7545139c53aa2a5d45297d5)

### Runtime
- [replaced QualifiedName with XName](https://github.com/maxtoroq/XCST/commit/d5ab75484241f7d1cb349aef3573434cdd1786c7)
- [moved IXcstPackage to root and merged PackageModel with Runtime](https://github.com/maxtoroq/XCST/commit/099b042aa9a20d68ee628ab5fe0da76f2c816e57)

System Requirements
-------------------
The compiler produces code that is compatible with **C# 6** and **Visual Basic 14**, although template rules are not useful unless you use C# 7 or higher.

The [XCST schema](schemas/xcst.rng) is written in **Relax NG** and converted to XSD using [Trang], which requires **Java**.


[XCST]: https://maxtoroq.github.io/XCST/
[Xcst.Compiler]: https://www.nuget.org/packages/Xcst.Compiler
[Xcst.Runtime]: https://www.nuget.org/packages/Xcst.Runtime
[Trang]: https://github.com/relaxng/jing-trang
