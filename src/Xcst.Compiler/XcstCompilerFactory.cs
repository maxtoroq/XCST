// Copyright 2021 Max Toro Q.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using Xcst.PackageModel;

namespace Xcst.Compiler {

   public class XcstCompilerFactory {

      readonly Dictionary<string, IXcstPackage>
      _extensions = new();

      public bool
      EnableExtensions { get; set; }

      public XcstCompiler
      CreateCompiler() =>
         new XcstCompiler((this.EnableExtensions) ?
            new Dictionary<string, IXcstPackage>(_extensions)
            : null);

      public void
      RegisterExtension(IXcstPackage extensionPackage) {

         if (extensionPackage is null) throw new ArgumentNullException(nameof(extensionPackage));

         const string nsProp = "ExtensionNamespace";

         var ns = extensionPackage
            .GetType()
            .GetProperty(nsProp)?
            .GetValue(extensionPackage) as string
            ?? throw new ArgumentException(
               $"The extension package must define an '{nsProp}' public property that returns the extension namespace as a string.", nameof(extensionPackage));

         _extensions[ns] = extensionPackage;
      }
   }
}
