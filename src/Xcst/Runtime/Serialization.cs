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

      public static string Serialize(IXcstPackage package, QualifiedName outputName, OutputParameters parameters, DynamicContext currentContext, Action<DynamicContext> action) {

         if (package == null) throw new ArgumentNullException(nameof(package));
         if (parameters == null) throw new ArgumentNullException(nameof(parameters));
         if (action == null) throw new ArgumentNullException(nameof(action));

         var sb = new StringBuilder();

         var defaultParameters = new OutputParameters();

         if (outputName != null) {
            package.ReadOutputDefinition(outputName, defaultParameters);
         }

         using (IWriterFactory writerFactory = WriterFactory.CreateFactory(sb, parameters)) {
            using (var newContext = new DynamicContext(writerFactory, defaultParameters, package.Context, currentContext)) {
               action(newContext);
            }
         }

         return sb.ToString();
      }

      public static DynamicContext ChangeOutput(IXcstPackage package, Uri outputUri, QualifiedName outputName, OutputParameters parameters, DynamicContext currentContext = null) {

         if (outputUri == null) throw new ArgumentNullException(nameof(outputUri));
         if (parameters == null) throw new ArgumentNullException(nameof(parameters));

         if (!outputUri.IsAbsoluteUri
            && currentContext != null
            && currentContext.CurrentOutputUri.IsAbsoluteUri) {

            outputUri = new Uri(currentContext.CurrentOutputUri, outputUri);
         }

         if (!outputUri.IsAbsoluteUri) {
            throw new RuntimeException($"Cannot resolve {outputUri.OriginalString}. Specify an output URI.", DynamicError.Code(""));
         }

         if (!outputUri.IsFile) {
            throw new RuntimeException($"Can write to file URIs only ({outputUri.OriginalString}).", DynamicError.Code(""));
         }

         var defaultParameters = new OutputParameters();
         package.ReadOutputDefinition(outputName, defaultParameters);

         IWriterFactory writerFactory = WriterFactory.CreateFactory(outputUri, parameters);

         return new DynamicContext(writerFactory, defaultParameters, package.Context, currentContext);
      }
   }
}
