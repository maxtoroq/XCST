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
using System.Web;
using System.Web.Compilation;
using System.Web.Hosting;
using Xcst.Compiler;

namespace Xcst.Web {

   public sealed class XcstWebConfiguration {

      public const string FileExtension = "xcst";

      public static XcstWebConfiguration Instance { get; } = new XcstWebConfiguration();

      public XcstCompilerFactory CompilerFactory { get; } = new XcstCompilerFactory {
         EnableExtensions = true,
         PackageTypeResolver = typeName => BuildManager.GetType(typeName, throwOnError: false),
         PackagesLocation = HostingEnvironment.MapPath("~/App_Code"),
         PackageFileExtension = FileExtension
      };

      internal IList<Func<object, IHttpHandler>> HttpHandlerFactories { get; } = new List<Func<object, IHttpHandler>>();

      private XcstWebConfiguration() { }

      public void RegisterHandlerFactory(Func<object, IHttpHandler> handlerFactory) {
         this.HttpHandlerFactories.Insert(0, handlerFactory);
      }
   }
}
