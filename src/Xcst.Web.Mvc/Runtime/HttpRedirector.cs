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
using System.Web;
using System.Web.Mvc;

namespace Xcst.Web.Mvc.Runtime {

   /// <exclude/>
   public static class HttpRedirector {

      public static void Redirect(HttpResponseBase response, UrlHelper urlHelper, string href, int statusCode = 302, bool terminate = false, TempDataDictionary tempData = null) {

         if (response == null) throw new ArgumentNullException(nameof(response));
         if (urlHelper == null) throw new ArgumentNullException(nameof(urlHelper));

         href = urlHelper.Content(href);

         tempData?.Keep();

         switch (statusCode) {
            case 301:
               response.RedirectPermanent(href, terminate);
               break;

            case 302:
               response.Redirect(href, terminate);
               break;

            default:
               response.Redirect(href, endResponse: false);
               response.StatusCode = statusCode;

               if (terminate) {
                  response.End();
               }
               break;
         }
      }
   }
}
