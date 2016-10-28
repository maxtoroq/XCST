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
using System.IO;
using System.Xml;
using Xcst.Compiler;
using Xcst.Web.Compilation;

namespace Xcst.Web.Compilation {

   public class ViewPageBuildProvider<TViewPage> : PageBuildProvider<TViewPage> where TViewPage : class {

      string model;
      string inherits;

      protected override string ParsePath() {

         if (!this.IsFileInCodeDir) {

            using (Stream source = OpenStream()) {

               var readerSettings = new XmlReaderSettings {
                  IgnoreComments = true,
                  IgnoreWhitespace = true
               };

               using (XmlReader reader = XmlReader.Create(source, readerSettings, baseUri: this.PhysicalPath.LocalPath)) {

                  IXmlLineInfo lineInfo = reader as IXmlLineInfo;

                  Func<Exception> duplicateException = () =>
                     CreateParseException($"Only one '{reader.LocalName}' directive is allowed.", lineInfo.LineNumber);

                  Func<Exception> mutuallyExclusiveException = () =>
                     CreateParseException($"'{nameof(this.inherits)}' and '{nameof(this.model)}' directives are mutually exclusive.", lineInfo.LineNumber);

                  while (reader.Read() && reader.NodeType != XmlNodeType.Element) {

                     if (reader.NodeType == XmlNodeType.ProcessingInstruction) {

                        switch (reader.LocalName) {
                           case nameof(this.model):
                              if (this.model != null) {
                                 throw duplicateException();
                              }
                              if (this.inherits != null) {
                                 throw mutuallyExclusiveException();
                              }
                              this.model = reader.Value.Trim();
                              break;

                           case nameof(this.inherits):
                              if (this.inherits != null) {
                                 throw duplicateException();
                              }
                              if (this.model != null) {
                                 throw mutuallyExclusiveException();
                              }
                              this.inherits = reader.Value.Trim();
                              break;
                        }
                     }
                  }
               }
            }
         }

         return base.ParsePath();
      }

      protected override void ConfigureCompiler(XcstCompiler compiler) {

         base.ConfigureCompiler(compiler);

         if (!this.IsFileInCodeDir) {

            var baseTypes = new List<string>(compiler.TargetBaseTypes);
            baseTypes.RemoveAt(0);

            if (!String.IsNullOrEmpty(this.inherits)) {
               baseTypes.Insert(0, this.inherits);
            } else {

               string modelType;

               if (!String.IsNullOrEmpty(this.model)) {

                  modelType = this.model;

                  if (!this.model.Contains(".")) {
                     compiler.AlternateFirstBaseType = $"{typeof(TViewPage).FullName}<{this.GeneratedTypeFullName}.{this.model}>";
                     compiler.AlternateFirstBaseTypeIfExistsType = this.model;
                  }

               } else {
                  modelType = "dynamic";
               }

               baseTypes.Insert(0, $"{typeof(TViewPage).FullName}<{modelType}>");
            }

            compiler.TargetBaseTypes = baseTypes.ToArray();
         }
      }
   }
}
