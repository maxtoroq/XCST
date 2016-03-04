// Copyright 2015 Max Toro Q.
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
using System.Globalization;
using System.Text;

namespace Xcst.Runtime {

   /// <exclude/>
   public class ExecutionContext {

      readonly IXcstPackage package;
      readonly HashSet<Uri> outputUris = new HashSet<Uri>();
      readonly IFormatProvider formatProvider;

      public SimpleContent SimpleContent { get; }

      internal ExecutionContext(IXcstPackage package, IFormatProvider formatProvider) {

         if (package == null) throw new ArgumentNullException(nameof(package));

         this.package = package;
         this.formatProvider = formatProvider ?? CultureInfo.CurrentCulture;
         this.SimpleContent = new SimpleContent(this.formatProvider);
      }

      internal DynamicContext CreateDynamicContext(IWriterFactory writerFactory, QualifiedName outputName = null, DynamicContext currentContext = null) {

         if (writerFactory == null) throw new ArgumentNullException(nameof(writerFactory));

         this.outputUris.Add(writerFactory.OutputUri);

         var outputParams = new OutputParameters();
         this.package.ReadOutputDefinition(outputName, outputParams);

         return new DynamicContext(writerFactory, outputParams, this, currentContext);
      }

      public DynamicContext ChangeOutput(Uri outputUri, QualifiedName outputName, OutputParameters parameters, DynamicContext currentContext = null) {

         if (outputUri == null) throw new ArgumentNullException(nameof(outputUri));
         if (parameters == null) throw new ArgumentNullException(nameof(parameters));

         if (!outputUri.IsAbsoluteUri
            && currentContext != null
            && currentContext.CurrentOutputUri.IsAbsoluteUri) {

            outputUri = new Uri(currentContext.CurrentOutputUri, outputUri);
         }

         if (this.outputUris.Contains(outputUri)) {
            throw new RuntimeException($"Cannot write to the same URI more than once ({outputUri.OriginalString}).", DynamicError.Code("XTDE1490"));
         }

         if (!outputUri.IsAbsoluteUri) {
            throw new RuntimeException($"Cannot resolve {outputUri.OriginalString}. Specify an output URI.", DynamicError.Code(""));
         }

         if (!outputUri.IsFile) {
            throw new RuntimeException($"Can write to file URIs only ({outputUri.OriginalString}).", DynamicError.Code(""));
         }

         return CreateDynamicContext(WriterFactory.CreateFactory(outputUri, parameters), outputName, currentContext);
      }

      public string Serialize(QualifiedName outputName, OutputParameters parameters, DynamicContext currentContext, Action<DynamicContext> action) {

         if (parameters == null) throw new ArgumentNullException(nameof(parameters));
         if (action == null) throw new ArgumentNullException(nameof(action));

         var sb = new StringBuilder();

         var defaultParameters = new OutputParameters();

         if (outputName != null) {
            this.package.ReadOutputDefinition(outputName, defaultParameters);
         }

         using (IWriterFactory writerFactory = WriterFactory.CreateFactory(sb, parameters)) {
            using (var newContext = new DynamicContext(writerFactory, defaultParameters, this, currentContext)) {
               action(newContext);
            }
         }

         return sb.ToString();
      }
   }
}