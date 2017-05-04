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
using Xcst.PackageModel;

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

      public static string SimpleContent(IXcstPackage package, string separator, Action<XcstWriter> action) {

         return Serialize(
            package,
            null,
            new OutputParameters {
               Method = OutputParameters.StandardMethods.Text,
               ItemSeparator = separator
            },
            action
         );
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
            throw new RuntimeException($"Cannot resolve {outputUri.OriginalString}. Specify an output URI.");
         }

         if (!outputUri.IsFile) {
            throw new RuntimeException($"Can write to file URIs only ({outputUri.OriginalString}).");
         }

         var defaultParams = new OutputParameters();
         package.ReadOutputDefinition(outputName, defaultParams);

         return WriterFactory.CreateWriter(outputUri)
            (defaultParams, parameters, package.Context.SimpleContent);
      }

      public static XcstWriter Void(IXcstPackage package) {

         return WriterFactory.CreateWriter(new NullWriter(WriterFactory.DefaultOuputUri))
            (null, null, package.Context.SimpleContent);
      }
   }

   class NullWriter : XcstWriter {

      public NullWriter(Uri outputUri)
         : base(outputUri) { }

      public override void Flush() { }

      public override void WriteComment(string text) { }

      public override void WriteEndAttribute() { }

      public override void WriteEndElement() { }

      public override void WriteProcessingInstruction(string name, string text) { }

      public override void WriteRaw(string data) { }

      public override void WriteStartAttribute(string prefix, string localName, string ns, string separator) { }

      public override void WriteStartElement(string prefix, string localName, string ns) { }

      public override void WriteString(string text) { }
   }
}
