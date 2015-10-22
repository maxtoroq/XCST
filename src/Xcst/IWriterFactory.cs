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
using System.IO;
using System.Text;
using System.Xml;
using Xcst.Xml;

namespace Xcst {

   interface IWriterFactory : IDisposable {

      Uri OutputUri { get; }

      /// <param name="defaultParameters">Output definition (Usually the default definition, but can be a named definition when using <code>&lt;c:result-document format="name"></code>).</param>
      XmlWriter Create(OutputParameters defaultParameters);
   }

   static class WriterFactory {

      public static IWriterFactory CreateFactory(Stream output, Uri outputUri, OutputParameters overrideParameters = null, bool autoClose = false) {
         return new StreamXmlWriterFactory(output, outputUri, overrideParameters, autoClose);
      }

      public static IWriterFactory CreateFactory(TextWriter output, Uri outputUri, OutputParameters overrideParameters = null, bool autoClose = false) {
         return new TextXmlWriterFactory(output, outputUri, overrideParameters, autoClose);
      }

      public static IWriterFactory CreateFactory(StringBuilder output, OutputParameters overrideParameters = null) {
         return new StringXmlWriterFactory(output, overrideParameters);
      }

      public static IWriterFactory CreateFactory(Uri file, OutputParameters overrideParameters = null) {
         return new FileUriXmlWriterFactory(file, overrideParameters);
      }

      public static IWriterFactory CreateFactory(XmlWriter output, Uri outputUri) {
         return new InstanceXmlWriterFactory(output, outputUri);
      }
   }
}