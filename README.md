[XCST] — eXtensible C-Sharp Templates
=====================================
XCST is a language optimized for the production of XML and other formats. It's a more general-purpose version of XSLT.

See the [project home][XCST] for more information.

[![Build status](https://ci.appveyor.com/api/projects/status/93bvxpo3x4bg2po8/branch/v2?svg=true)](https://ci.appveyor.com/project/maxtoroq/xcst/branch/v2) ![Tests](https://img.shields.io/appveyor/tests/maxtoroq/XCST/v2)

### NuGet Packages Built From This Repository

Package | Targets
------- | -------
**[Xcst.Compiler]**<br/>Compilation API. Use this package to translate your XCST programs into C# or Visual Basic code. | .NET Framework 4.6 / .NET Core 2.0 / .NET Standard 2.0
**[Xcst.Runtime]**<br/>Runtime and evaluation API. | .NET Framework 4.6 / .NET Core 2.0 / .NET Standard 2.0

Documentation
-------------
The documentation can be found at the [project home][XCST].

About v2
--------
*v2* is the main branch for major version 2. See *v1* for version 1 (no longer maintained).

The main focuses of v2 has been porting the compiler from XSLT to XCST and refining the language. While the runtime is not backwards compatible (programs compiled against v1 must be recompiled to run on v2), it is largely compatible because the implementation code is basically the same.

The compiler can generate code for runtime v1 or v2 (the default), but it supports only the latest language, and there are breaking changes. New language features that require special runtime support are not supported when targeting the v1 runtime.

Tests now run on .NET Core and compatibility with this framework is the priority.

What's New
----------
- [`c:mode` declarations](https://github.com/maxtoroq/XCST/commit/815e91e954088fea4691701145b148fd0aca474b)
- [Copy null for `on-no-match='fail'`](https://github.com/maxtoroq/XCST/commit/1e70551da04a14813726fcea56d340f5f73659b4)
- [`c:apply-templates` and `c:next-match` 'with-params' attribute](https://github.com/maxtoroq/XCST/commit/a8b68b305b7b4e3abd3ca2acf98a58678cb01d13)
- [`c:type` now include partial modifier](https://github.com/maxtoroq/XCST/commit/7ebeb0e21d3bddba3bb11f3eaafc6c52bd2f9a31)
- [`c:member` 'serialize' attribute](https://github.com/maxtoroq/XCST/commit/fa62618c6850a21dfcdc51725dc320ccc6a23393)
- [`c:function` 'partial' attribute](https://github.com/maxtoroq/XCST/commit/7804313092397482a5f2d450e15a6cf09912e84c)
- [`c:if` 'value' attribute](https://github.com/maxtoroq/XCST/commit/448a8ade7d6657d97af89f4f5b05209d9dd60806)
- [`c:module` and `c:package` 'inherits' attribute](https://github.com/maxtoroq/XCST/commit/01c56cc2d0962dca1ec6a3f7d66d4fcefa69a2d8)
- [Allow text on `c:object`](https://github.com/maxtoroq/XCST/commit/85348f04e82e8249ddd00494c6fbf2b1604cb546)
- [Implicit `c:on-empty` when sequence type is nullable](https://github.com/maxtoroq/XCST/commit/c68c5a8817bcf0dd335aad56020367597aefc92b)
- [`c:message` listener](https://github.com/maxtoroq/XCST/commit/11fac889ce169ead34c903cd03d5103f98b83f41)
- [`c:use-package/c:with-param`](https://github.com/maxtoroq/XCST/commit/4e188f71cbc104ce19089f81a2a4406474e15b33)
- [Use package file extension to find library packages](https://github.com/maxtoroq/XCST/commit/db12fb2d11d94f320b918772a49eced6709a20c6)

Breaking Changes
----------------
### Language
- [Removed implicit unnamed mode](https://github.com/maxtoroq/XCST/commit/bb1269abf695410a112e5851194768cc6f7b9f88)
- [Changed default built-in template rule to fail](https://github.com/maxtoroq/XCST/commit/bf8a3319cf120c1b19c3e257a1f643cc0522d994)
- [Fail compilation for `c:apply-templates` and `c:next-match` when there are no modes in the current package](https://github.com/maxtoroq/XCST/commit/8c5c87556e9ce44e82e5350a75f4397d0657f3af)
- [Changed default visibility of `c:template` on implicit packages to final](https://github.com/maxtoroq/XCST/commit/3bd2f8abe3a86254e02af28b786d4a4ea2a3ba79)
- [Changed the default separator for sequence constructors of `c:attribute` and `c:value-of` to single space](https://github.com/maxtoroq/XCST/commit/f64f86a22444bfa736f58b4d5b587808b97d4d90)
- [Renamed `c:metadata` to `c:meta` and 'name' attribute to 'type'](https://github.com/maxtoroq/XCST/commit/1607566fd799b36bc5034e5097831810c9325e14)
- [Renamed 'display-text-member' to 'text-member'](https://github.com/maxtoroq/XCST/commit/2eee63272488034c2536ba81b61087bb692e0356)
- [Renamed 'null-display-text' to 'null-text'](https://github.com/maxtoroq/XCST/commit/df54dad48d4315c2adf67e4e2ccbafca7e08dc34)
- [Renamed 'format' to 'use-format' (serialization)](https://github.com/maxtoroq/XCST/commit/c97aa4ed7e3e2a905e1f30d9d126fa3b7ac85ab5)
- [Don't assign local variable without value](https://github.com/maxtoroq/XCST/commit/216d0e6fa5fa72e8e5ec3fdf01e6a747787319f9)
- [Resolve 'validation-resource-type' from `c:validation` against package namespace, or treat as fully-qualified](https://github.com/maxtoroq/XCST/commit/8a42ce48473a62c94ba3f75248a621eacddbc070)
- [Not using 'data-type' for validation](https://github.com/maxtoroq/XCST/commit/944fc8de7741c21e078a64708395032fa1deb1a3)
- [Deprecated System.Delegate fallback on `c:invoke-delegate` as it hides programming errors](https://github.com/maxtoroq/XCST/commit/0e25b838ffcdc4aabf5a29de76c4848dadeec06d)
- [Package parameters are not visible to using package (set with `c:use-package/c:with-param`)](https://github.com/maxtoroq/XCST/commit/4e188f71cbc104ce19089f81a2a4406474e15b33)

### Compiler
- [Removed XcstCompilerFactory](https://github.com/maxtoroq/XCST/commit/493f489671bd364ac1dd412e5947aa693d75f185)
- [Switched CompileResult.Templates to XName](https://github.com/maxtoroq/XCST/commit/e4709f7e7d754d4ee37c212943a455c1debc34a2)
- [Removed PackageTypeResolver](https://github.com/maxtoroq/XCST/commit/888cead3a83d26a4a7545139c53aa2a5d45297d5)

### Runtime
- [Replaced QualifiedName with XName](https://github.com/maxtoroq/XCST/commit/d5ab75484241f7d1cb349aef3573434cdd1786c7)
- [Moved IXcstPackage to root and merged PackageModel with Runtime](https://github.com/maxtoroq/XCST/commit/099b042aa9a20d68ee628ab5fe0da76f2c816e57)
- [C# 10 is required when targeting .NET 7+ (interpolated string handlers)](https://github.com/maxtoroq/XCST/commit/258711852a2045335160aea0142e423c8f7b4a75)
- [On .NET 7+, dynamic expressions are not allowed in value templates (interpolated string handler implementation limitation)](https://github.com/maxtoroq/XCST/commit/30e353aaff34f3b779aff087bdfd8ae14b9c64f8)
- [New simple content writer outputs the atomized value of all simple content instructions](https://github.com/maxtoroq/XCST/commit/733a3541d7aa63b10cfbd9b620bb1067b1128db6)

System Requirements
-------------------
The compiler produces code that is compatible with **C# 6** and **Visual Basic 14**, although template rules are not useful unless you use C# 7 or higher.

The [XCST schema](schemas/xcst.rng) is written in **Relax NG** and converted to XSD using [Trang], which requires **Java**.


[XCST]: https://maxtoroq.github.io/XCST/
[Xcst.Compiler]: https://www.nuget.org/packages/Xcst.Compiler
[Xcst.Runtime]: https://www.nuget.org/packages/Xcst.Runtime
[Trang]: https://github.com/relaxng/jing-trang
