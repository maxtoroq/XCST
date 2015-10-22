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

#region XcstPageHttpModule is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Web;

namespace Xcst.Web {

   class XcstPageHttpModule : IHttpModule {

      static readonly object _hasBeenRegisteredKey = new object();

      public void Dispose() { }

      public void Init(HttpApplication application) {

         if (application.Context.Items[_hasBeenRegisteredKey] != null) {
            // registration for this module has already run for this HttpApplication instance
            return;
         }

         application.Context.Items[_hasBeenRegisteredKey] = true;

         InitApplication(application);
      }

      static void InitApplication(HttpApplication application) {
         application.PostResolveRequestCache += OnApplicationPostResolveRequestCache;
      }

      static void OnApplicationPostResolveRequestCache(object sender, EventArgs e) {

         HttpContextBase context = new HttpContextWrapper(((HttpApplication)sender).Context);

         new WebPageRoute().DoPostResolveRequestCache(context);
      }
   }
}
