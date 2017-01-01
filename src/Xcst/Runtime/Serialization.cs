// Copyright 2016 Max Toro Q.
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
using System.Text;

namespace Xcst.Runtime {

   /// <exclude/>

   public static class Serialization {

      public static string Serialize(IXcstPackage package, QualifiedName outputName, OutputParameters parameters, Action<XcstWriter> action) {

         if (package == null) throw new ArgumentNullException(nameof(package));
         if (parameters == null) throw new ArgumentNullException(nameof(parameters));
         if (action == null) throw new ArgumentNullException(nameof(action));

         var sb = new StringBuilder();

         var defaultParams = new OutputParameters();

         if (outputName != null) {
            package.ReadOutputDefinition(outputName, defaultParams);
         }

         using (XcstWriter writer = WriterFactory.CreateWriter(sb)(defaultParams, parameters, package.Context.SimpleContent)) {
            action(writer);
         }

         return sb.ToString();
      }

      public static XcstWriter ChangeOutput(IXcstPackage package, Uri outputUri, QualifiedName outputName, OutputParameters parameters, XcstWriter currentOutput = null) {

         if (package == null) throw new ArgumentNullException(nameof(package));
         if (outputUri == null) throw new ArgumentNullException(nameof(outputUri));
         if (parameters == null) throw new ArgumentNullException(nameof(parameters));

         if (!outputUri.IsAbsoluteUri
            && currentOutput != null
            && currentOutput.OutputUri.IsAbsoluteUri) {

            outputUri = new Uri(currentOutput.OutputUri, outputUri);
         }

         if (!outputUri.IsAbsoluteUri) {
            throw new RuntimeException($"Cannot resolve {outputUri.OriginalString}. Specify an output URI.", DynamicError.Code(""));
         }

         if (!outputUri.IsFile) {
            throw new RuntimeException($"Can write to file URIs only ({outputUri.OriginalString}).", DynamicError.Code(""));
         }

         var defaultParams = new OutputParameters();
         package.ReadOutputDefinition(outputName, defaultParams);

         return WriterFactory.CreateWriter(outputUri)(defaultParams, parameters, package.Context.SimpleContent);
      }
   }
}
