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
using System.Xml.Linq;

namespace Xcst.Runtime;

public class DeepSkip {

   public static void
   Skip<TBase>(
         IXcstPackage package,
         Action<TemplateContext, ISequenceWriter<object?>, int> currentMode,
         TemplateContext context,
         ISequenceWriter<TBase> output,
         int matchOffset) {

      var value = context.Input;

      if (value is null) {
         return;
      }

      Action<TemplateContext, ISequenceWriter<object?>> currMode = (c, o) =>
         currentMode.Invoke(c, o, matchOffset);

      switch (value) {
         case XDocument doc:
            ApplyXDocumentChildren(currMode, doc, context, SequenceWriter.AdjustWriterDynamically<TBase, object?>(output));
            return;
      }
   }

   static void
   ApplyXDocumentChildren(
         Action<TemplateContext, ISequenceWriter<object?>> currentMode,
         XDocument doc,
         TemplateContext context,
         ISequenceWriter<object?> output) {

      foreach (var child in doc.Nodes()) {
         currentMode.Invoke(TemplateContext.ForApplyTemplatesItem(context, context.Mode, child), output);
      }
   }
}
