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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Xcst.Runtime;

public static class TextOnlyCopy {

   public static void
   Copy<TBase>(
         IXcstPackage package,
         Action<TemplateContext, ISequenceWriter<object?>, int> currentMode,
         TemplateContext context,
         ISequenceWriter<TBase> output,
         int matchOffset) {

      var value = context.Input;

      if (value is null) {
         return;
      }

      void currMode(TemplateContext c, ISequenceWriter<object?> o) =>
         currentMode.Invoke(c, o, matchOffset);

      switch (value) {
         case XAttribute attr:
            ((dynamic)output).WriteString(attr.Value);
            return;

         case XText txt:
            ((dynamic)output).WriteString(txt.Value);
            return;

         case XContainer node:
            ApplyXContainerChildren(currMode, node, context, SequenceWriter.AdjustWriterDynamically<TBase, object?>(output));
            return;

         case XProcessingInstruction or XComment:
            return;

         case Array arr:
            ApplyArrayMembers(currMode, arr, context, SequenceWriter.AdjustWriterDynamically<TBase, object?>(output));
            return;

         default:
            ((dynamic)output).WriteString(package.Context.SimpleContent.Join(String.Empty, value));
            return;
      }
   }

   static void
   ApplyXContainerChildren(
         Action<TemplateContext, ISequenceWriter<object?>> currentMode,
         XContainer node,
         TemplateContext context,
         ISequenceWriter<object?> output) {

      foreach (var child in node.Nodes()) {
         currentMode.Invoke(TemplateContext.ForApplyTemplatesItem(context, context.Mode, child), output);
      }
   }

   static void
   ApplyArrayMembers(
         Action<TemplateContext, ISequenceWriter<object?>> currentMode,
         Array arr,
         TemplateContext context,
         ISequenceWriter<object?> output) {

      foreach (var item in arr) {
         currentMode.Invoke(TemplateContext.ForApplyTemplatesItem(context, context.Mode, item), output);
      }
   }
}
