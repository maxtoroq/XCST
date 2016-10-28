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
using System.ComponentModel;
using System.Web.Compilation;
using System.Web.Mvc;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Xcst.Compiler;
using Xcst.Web.Compilation;
using Xcst.Web.Mvc;

namespace Xcst.Web {

   /// <exclude/>
   [EditorBrowsable(EditorBrowsableState.Never)]
   public static class PreApplicationStartCode {

      static bool startWasCalled;

      public static void Start() {

         if (!startWasCalled) {

            startWasCalled = true;

            XcstWebConfiguration config = XcstWebConfiguration.Instance;

#if ASPNETLIB
            AspNetLib.Mvc.PreApplicationStartCode.Start();

            config.RegisterHandlerFactory(XcstPageHttpHandler.Create);
            config.RegisterHandlerFactory(XcstViewPageHttpHandler.Create);
#else
            System.Web.Mvc.PreApplicationStartCode.Start();
#endif

            config.CompilerFactory.RegisterApplicationExtension();

            BuildProvider.RegisterBuildProvider("." + XcstWebConfiguration.FileExtension, typeof(ViewPageBuildProvider<XcstViewPage>));
            ViewEngines.Engines.Add(new XcstViewEngine());

#if ASPNETLIB
            DynamicModuleUtility.RegisterModule(typeof(XcstPageHttpModule));

            HtmlHelper.ClientValidationEnabled = true;
            HtmlHelper.UnobtrusiveJavaScriptEnabled = true;
#endif
         }
      }
   }
}
