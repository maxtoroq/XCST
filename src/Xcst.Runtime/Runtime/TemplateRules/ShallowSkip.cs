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
using System.Linq;
using System.Xml.Linq;

namespace Xcst.Runtime;

using ModeDelegate = Action<TemplateContext, ISequenceWriter<object?>, int>;

public static class ShallowSkip {

   public static void
   Skip<TBase>(
         IXcstPackage package,
         ModeDelegate currentMode,
         TemplateContext context,
         ISequenceWriter<TBase> output,
         int matchOffset) {

      switch (context.Input) {
         case null:
            break;

         case XContainer node:
            ApplyXContainerChildren(
               currentMode,
               node,
               context,
               SequenceWriter.AdjustWriterDynamically<TBase, object?>(output),
               matchOffset);
            break;

         case Array arr:
            ApplyArrayMembers(
               currentMode,
               arr,
               context,
               SequenceWriter.AdjustWriterDynamically<TBase, object?>(output),
               matchOffset);
            break;
      }
   }

   static void
   ApplyXContainerChildren(
         ModeDelegate currentMode,
         XContainer node,
         TemplateContext context,
         ISequenceWriter<object?> output,
         int matchOffset) {

      if (node is XElement el) {
         foreach (var attr in el.Attributes().Where(p => !p.IsNamespaceDeclaration)) {
            currentMode.Invoke(
               TemplateContext.ForApplyTemplatesItem(context, context.Mode, attr),
               output,
               matchOffset);
         }
      }

      foreach (var item in node.Nodes()) {
         currentMode.Invoke(
            TemplateContext.ForApplyTemplatesItem(context, context.Mode, item),
               output,
               matchOffset);
      }
   }

   static void
   ApplyArrayMembers(
         ModeDelegate currentMode,
         Array arr,
         TemplateContext context,
         ISequenceWriter<object?> output,
         int matchOffset) {

      foreach (var item in arr) {
         currentMode.Invoke(
            TemplateContext.ForApplyTemplatesItem(context, context.Mode, item),
            output,
            matchOffset);
      }
   }
}
