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
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Xcst.Runtime;

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
         Action<TemplateContext, ISequenceWriter<object?>, int> currentMode,
         TemplateContext context,
         ISequenceWriter<TBase> output,
         int matchOffset) {

      var value = context.Input;

      if (value is null) {
         ((dynamic)output).WriteObject(value);
         return;
      }

      void currMode(TemplateContext c, ISequenceWriter<object?> o) =>
         currentMode.Invoke(c, o, matchOffset);

      if (TryCopy(package, currMode, value, context, output)) {
         return;
      }

      if (value is TBase item) {
         output.CopyOf(item);
         return;
      }

      if (value is IEnumerable<TBase> seq) {
         output.CopyOf(seq);
         return;
      }

      throw new NotImplementedException();
   }

   static bool
   TryCopy<TBase>(
         IXcstPackage package,
         Action<TemplateContext, ISequenceWriter<object?>> currentMode,
         object value,
         TemplateContext context,
         ISequenceWriter<TBase> output) {

      switch (value) {
         case XElement el:
            CopyXElement(package, currentMode, el, context, (ISequenceWriter<XElement>)output);
            return true;

         case XDocument doc:
            CopyXDocument(package, currentMode, doc, context, (ISequenceWriter<XDocument>)output);
            return true;

         case object[] arr:
            CopyArray(package, currentMode, arr, context, (ISequenceWriter<object[]>)output);
            return true;

         default:
            return false;
      }
   }

   static void
   CopyXDocument(
         IXcstPackage package,
         Action<TemplateContext, ISequenceWriter<object?>> currentMode,
         XDocument doc,
         TemplateContext context,
         ISequenceWriter<XDocument> output) {

      var docOutput = DocumentWriter.CastDocument(package, output);

      try {
         foreach (var child in doc.Nodes()) {
            currentMode.Invoke(TemplateContext.ForApplyTemplatesItem(context, context.Mode, child), docOutput);
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
         Action<TemplateContext, ISequenceWriter<object?>> currentMode,
         XElement el,
         TemplateContext context,
         ISequenceWriter<XElement> output) {

      var elOutput = DocumentWriter.CastElement(package, output);
      elOutput.WriteStartElement(el.Name.LocalName, el.Name.NamespaceName);

      try {

         foreach (var at in el.Attributes().Where(p => !p.IsNamespaceDeclaration)) {
            currentMode.Invoke(TemplateContext.ForApplyTemplatesItem(context, context.Mode, at), elOutput);
         }

         foreach (var child in el.Nodes()) {
            currentMode.Invoke(TemplateContext.ForApplyTemplatesItem(context, context.Mode, child), elOutput);
         }

      } finally {
         elOutput.WriteEndElement();
      }
   }

   static void
   CopyArray(
         IXcstPackage package,
         Action<TemplateContext, ISequenceWriter<object?>> currentMode,
         object[] arr,
         TemplateContext context,
         ISequenceWriter<object[]> output) {

      var arrType = arr.GetType();
      var elemType = arrType.GetElementType();
      var elemTypeObj = elemType == typeof(System.Object);
      dynamic buffer = Activator.CreateInstance(typeof(List<>).MakeGenericType(elemType));

      var arrOutput = new StreamedSequenceWriter<object?>(item => {
         buffer.Add((elemTypeObj ? item : DynamicCast.Cast(item, elemType)));
      });

      foreach (var item in arr) {
         currentMode.Invoke(TemplateContext.ForApplyTemplatesItem(context, context.Mode, item), arrOutput);
      }

      output.WriteObject((object[])buffer.ToArray());
   }
}
