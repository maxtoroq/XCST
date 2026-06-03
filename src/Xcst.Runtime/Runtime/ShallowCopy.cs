// Copyright 2021 Max Toro Q.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Xcst.Runtime;

using ModeDelegate = Action<TemplateContext, ISequenceWriter<object?>, int>;

public static class ShallowCopy {

   public static void
   Copy<TBase>(
         IXcstPackage package,
         Action<TemplateContext, ISequenceWriter<object?>> currentMode,
         TemplateContext context,
         ISequenceWriter<TBase> output) =>
      Copy<TBase>(package, (c, o, of) => currentMode.Invoke(c, o), context, output, 0);

   public static void
   Copy<TBase>(
         IXcstPackage package,
         ModeDelegate currentMode,
         TemplateContext context,
         ISequenceWriter<TBase> output,
         int matchOffset) {

      switch (context.Input) {
         case var n and null:
            output.WriteObject((TBase)n!);
            break;

         case XElement el:
            CopyXElement(package, currentMode, el, context, (ISequenceWriter<XElement>)output, matchOffset);
            break;

         case XDocument doc:
            CopyXDocument(package, currentMode, doc, context, (ISequenceWriter<XDocument>)output, matchOffset);
            break;

         case Array arr:
            CopyArray(
               currentMode,
               arr,
               context,
               SequenceWriter.AdjustWriterDynamically<TBase, object>(output),
               matchOffset);
            break;

         case TBase item:
            output.CopyOf(item);
            break;

         case IEnumerable<TBase> seq:
            output.CopyOf(seq);
            break;

         default:
            throw new NotImplementedException();
      }
   }

   static void
   CopyXDocument(
         IXcstPackage package,
         ModeDelegate currentMode,
         XDocument doc,
         TemplateContext context,
         ISequenceWriter<XDocument> output,
         int matchOffset) {

      var docOutput = DocumentWriter.CastDocument(package, output);

      try {
         foreach (var item in doc.Nodes()) {
            currentMode.Invoke(
               TemplateContext.ForApplyTemplatesItem(context, context.Mode, item),
               docOutput,
               matchOffset);
         }
      } finally {
         if (output.TryCastToDocumentWriter() is null) {
            docOutput.Dispose();
         }
      }
   }

   static void
   CopyXElement(
         IXcstPackage package,
         ModeDelegate currentMode,
         XElement el,
         TemplateContext context,
         ISequenceWriter<XElement> output,
         int matchOffset) {

      var elOutput = DocumentWriter.CastElement(package, output);
      elOutput.WriteStartElement(el.Name.LocalName, el.Name.NamespaceName);

      try {

         foreach (var at in el.Attributes().Where(p => !p.IsNamespaceDeclaration)) {
            currentMode.Invoke(
               TemplateContext.ForApplyTemplatesItem(context, context.Mode, at),
               elOutput,
               matchOffset);
         }

         foreach (var item in el.Nodes()) {
            currentMode.Invoke(
               TemplateContext.ForApplyTemplatesItem(context, context.Mode, item),
               elOutput,
               matchOffset);
         }

      } finally {
         elOutput.WriteEndElement();
      }
   }

   static void
   CopyArray(
         ModeDelegate currentMode,
         Array arr,
         TemplateContext context,
         ISequenceWriter<Array> output,
         int matchOffset) {

      var arrType = arr.GetType();
      var elemType = arrType.GetElementType()!;
      var buffer = new ArrayList(arr.Length);

      var arrOutput = new StreamedSequenceWriter<object?>(item => buffer.Add(item));

      foreach (var item in arr) {
         currentMode.Invoke(
            TemplateContext.ForApplyTemplatesItem(context, context.Mode, item),
            arrOutput,
            matchOffset);
      }

      output.WriteObject(buffer.ToArray(elemType));
   }
}
