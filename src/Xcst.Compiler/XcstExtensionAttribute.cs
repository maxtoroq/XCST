// Copyright 2019 Max Toro Q.
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
using System.IO;

namespace Xcst.Compiler {

   [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
   public sealed class XcstExtensionAttribute : Attribute {

      public Uri
      ExtensionNamespace { get; }

      public Type
      ExtensionLoaderType { get; set; }

      public
      XcstExtensionAttribute(string extensionNamespace, Type extensionLoaderType) {

         if (extensionNamespace is null) throw new ArgumentNullException(nameof(extensionNamespace));
         if (extensionLoaderType is null) throw new ArgumentNullException(nameof(extensionLoaderType));

         Type expectedType = typeof(XcstExtensionLoader);

         if (!expectedType.IsAssignableFrom(extensionLoaderType)) {
            throw new ArgumentException($"extensionLoaderType must inherit from '{expectedType}'.");
         }

         this.ExtensionNamespace = new Uri(extensionNamespace, UriKind.Absolute);
         this.ExtensionLoaderType = extensionLoaderType;
      }
   }

   public abstract class XcstExtensionLoader {

      public abstract Stream
      LoadSource();
   }
}
