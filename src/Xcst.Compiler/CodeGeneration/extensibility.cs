// Copyright 2022 Max Toro Q.
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
using System.Xml.Linq;
using SequenceWriter = Xcst.Runtime.SequenceWriter;

namespace Xcst.Compiler;

partial class XcstCompilerPackage {

   Dictionary<string, IXcstPackage>
   _extensions;

   Dictionary<string, XcstEvaluator>
   _extensionsEvaluators = new();

   private Dictionary<string, IXcstPackage>
   Extensions {
      get {
         if (_extensions is null) {

            _extensions = new Dictionary<string, IXcstPackage>();

            if (src_extension_factories != null) {

               foreach (var pkgFn in src_extension_factories) {

                  var pkg = pkgFn.Invoke()
                     ?? throw new InvalidOperationException("The extension factory cannot return null.");

                  var ns = ExtensionNamespace(pkg);

                  _extensions[ns] = pkg;
               }
            }
         }

         return _extensions;
      }
   }

   static string
   ExtensionNamespace(IXcstPackage pkg) {

      const string nsProp = "ExtensionNamespace";

      var ns = pkg
         .GetType()
         .GetProperty(nsProp)?
         .GetValue(pkg) as string
         ?? throw new InvalidOperationException(
            $"The extension package must define an '{nsProp}' public property that returns the extension namespace as a string.");

      return ns;
   }

   IXcstPackage?
   ExtensionPackage(XElement el) {

      if (this.Extensions.TryGetValue(el.Name.NamespaceName, out var pkg)) {
         return pkg;
      }

      return null;
   }

   XcstEvaluator
   ExtensionEvaluator(string extensionNamespace) {

      if (_extensionsEvaluators.TryGetValue(extensionNamespace, out var ev)) {
         return ev;
      }

      if (this.Extensions.TryGetValue(extensionNamespace, out var pkg)) {

         var evaluator = XcstEvaluator.Using(pkg)
            .WithParam("xcst_is_value_template", (System.Func<object, bool>)xcst_is_value_template)
            .WithParam("xcst_require_output", xcst_require_output)
            .WithParam("src_base_types", src_base_types)
            .WithParam("src_doc_output", (System.Func<XObject?, XElement?, XElement>)src_doc_output)
            .WithParam("src_output_is_doc", (System.Func<XElement, bool>)src_output_is_doc)
            .WithParam("src_template_output", (System.Func<XElement?, XElement?, XElement>)src_template_output)
            .WithParam("src_helper_type", (System.Func<string, XElement>)src_helper_type)
            .WithParam("src_expand_attribute", src_expand_attribute)
            .WithParam("src_sequence_constructor", src_sequence_constructor)
            .WithParam("src_simple_content", src_simple_content)
            .WithParam("src_validation_arguments", src_validation_arguments)
            .WithParam("src_top_level_package_reference", src_top_level_package_reference)
            .WithParam("src_line_number", src_line_number)
            ;

         _extensionsEvaluators[extensionNamespace] = evaluator;

         return evaluator;
      }

      throw new ArgumentException("Unknown extension namespace.", nameof(extensionNamespace));
   }

   static bool
   HasTemplate<TItem>(IXcstPackage pkg, XName name) =>
      pkg.GetTemplate(name, SequenceWriter.Create<TItem>()) != null;

   static bool
   HasMode<TItem>(IXcstPackage pkg, XName mode) =>
      pkg.GetMode(mode, SequenceWriter.Create<TItem>()) != null;
}
