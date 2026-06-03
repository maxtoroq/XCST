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

      var objOutput = SequenceWriter.AdjustWriterDynamically<TBase, object?>(output);

      switch (value) {
         case XAttribute attr:
            objOutput.WriteString(attr.Value);
            break;

         case XText txt:
            objOutput.WriteString(txt.Value);
            break;

         case XContainer node:
            foreach (var item in node.Nodes()) {
               currentMode.Invoke(
                  TemplateContext.ForApplyTemplatesItem(context, context.Mode, item),
                  objOutput,
                  matchOffset);
            }
            break;

         case XProcessingInstruction or XComment:
            break;

         case Array arr:
            foreach (var item in arr) {
               currentMode.Invoke(
                  TemplateContext.ForApplyTemplatesItem(context, context.Mode, item),
                  objOutput,
                  matchOffset);
            }
            break;

         default:
            objOutput.WriteString(package.Context.SimpleContent.Join(String.Empty, value));
            break;
      }
   }
}
