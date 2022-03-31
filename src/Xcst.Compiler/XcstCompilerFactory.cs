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

      readonly Dictionary<Uri, IXcstPackage>
      _extensions = new();

      public bool
      EnableExtensions { get; set; }

      public XcstCompiler
      CreateCompiler() =>
         new XcstCompiler((this.EnableExtensions) ?
            new Dictionary<Uri, IXcstPackage>(_extensions)
            : null);

      public void
      RegisterExtension(Uri extensionNamespace, IXcstPackage extensionPackage) {

         if (extensionNamespace is null) throw new ArgumentNullException(nameof(extensionNamespace));
         if (extensionPackage is null) throw new ArgumentNullException(nameof(extensionPackage));

         if (!extensionNamespace.IsAbsoluteUri) {
            throw new ArgumentException($"{nameof(extensionNamespace)} must be an absolute URI.", nameof(extensionNamespace));
         }

         _extensions[extensionNamespace] = extensionPackage;
      }
   }
}
