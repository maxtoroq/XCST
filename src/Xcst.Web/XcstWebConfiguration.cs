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
using System.Web;
using System.Web.Compilation;
using System.Web.Hosting;
using System.Xml;
using Xcst.Compiler;

namespace Xcst.Web {

   public sealed class XcstWebConfiguration {

      public const string FileExtension = "xcst";

      public static XcstWebConfiguration Instance { get; } = new XcstWebConfiguration();

      public XcstCompilerFactory CompilerFactory { get; } = new XcstCompilerFactory {
         EnableExtensions = true,
         PackageTypeResolver = typeName => BuildManager.GetType(typeName, throwOnError: false),
         PackageLocationResolver = FindLibraryPackage
      };

      internal IList<Func<object, IHttpHandler>> HttpHandlerFactories { get; } = new List<Func<object, IHttpHandler>>();

      static Uri FindLibraryPackage(string packageName) {

         foreach (string path in Directory.EnumerateFiles(HostingEnvironment.MapPath("~/App_Code"), "*." + FileExtension, SearchOption.AllDirectories)) {

            if (Path.GetFileNameWithoutExtension(path)[0] == '_') {
               continue;
            }

            using (var reader = XmlReader.Create(path)) {

               while (reader.Read()) {

                  if (reader.NodeType == XmlNodeType.Element) {

                     if (reader.LocalName == "package"
                        && reader.NamespaceURI == XmlNamespaces.Xcst
                        && reader.GetAttribute("name") == packageName) {

                        return new Uri(path);
                     }

                     break;
                  }
               }
            }
         }

         return null;
      }

      private XcstWebConfiguration() { }

      public void RegisterHandlerFactory(Func<object, IHttpHandler> handlerFactory) {
         this.HttpHandlerFactories.Insert(0, handlerFactory);
      }
   }
}
