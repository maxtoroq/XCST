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
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Compilation;

namespace Xcst.Web {

   public abstract class XcstPage {

      HttpContextBase _Context;
      HttpRequestBase _Request;
      HttpResponseBase _Response;
      HttpSessionStateBase _Session;
      HttpServerUtilityBase _Server;

#if ASPNETLIB
      IList<string> _UrlData;
#endif
      IPrincipal _User;

      public virtual string VirtualPath { get; set; }

      // HttpContextWrapper Request/Response/Session/Server return a new instance every time
      // need to cache result

      public virtual HttpContextBase Context {
         get { return _Context; }
         set {
            _Context = value;
            _Request = null;
            _Response = null;
            _Session = null;
            _Server = null;
#if ASPNETLIB
            _UrlData = null;
#endif
         }
      }

      public HttpRequestBase Request {
         get {
            return _Request
               ?? (_Request = Context?.Request);
         }
      }

      public HttpResponseBase Response {
         get {
            return _Response
               ?? (_Response = Context?.Response);
         }
      }

      public HttpSessionStateBase Session {
         get {
            return _Session
               ?? (_Session = Context?.Session);
         }
      }

#if ASPNETLIB
      public virtual IList<string> UrlData {
         get {
            if (_UrlData == null
               && Context != null) {

               _UrlData = new UrlDataList(XcstWebConfiguration.GetPathInfo(Context));
            }
            return _UrlData;
         }
         set { _UrlData = value; }
      }
#endif

      public virtual IPrincipal User {
         get {
            return _User
               ?? (_User = Context?.User);
         }
         set { _User = value; }
      }

      public virtual bool IsPost {
         get {
            return Request?.HttpMethod == "POST";
         }
      }

      public virtual bool IsAjax {
         get {
            var request = Request;

            if (request == null) {
               return false;
            }

            return (request["X-Requested-With"] == "XMLHttpRequest")
               || ((request.Headers != null) && (request.Headers["X-Requested-With"] == "XMLHttpRequest"));
         }
      }

      public virtual bool TryAuthorize(string[] users = null, string[] roles = null) {

         if (IsAuthorized(this.User, users, roles)) {

            // see System.Web.Mvc.AuthorizeAttribute

            HttpCachePolicyBase cachePolicy = this.Response.Cache;
            cachePolicy.SetProxyMaxAge(new TimeSpan(0));
            cachePolicy.AddValidationCallback(CacheValidateHandler, new object[2] { users, roles });

            return true;

         } else {

            this.Response.StatusCode = 401;
            return false;
         }
      }

      void CacheValidateHandler(HttpContext context, object data, ref HttpValidationStatus validationStatus) {

         object[] dataArr = data as object[];

         bool isAuthorized = IsAuthorized(context.User, dataArr?[0] as string[], dataArr?[1] as string[]);

         validationStatus = (isAuthorized) ? HttpValidationStatus.Valid : HttpValidationStatus.IgnoreThisRequest;
      }

      static bool IsAuthorized(IPrincipal user, string[] users, string[] roles) {

         if (user == null
            || !user.Identity.IsAuthenticated) {

            return false;
         }

         if (users != null
            && users.Length > 0
            && !users.Contains(user.Identity.Name, StringComparer.OrdinalIgnoreCase)) {

            return false;
         }

         if (roles != null
            && roles.Length > 0
            && !roles.Any(user.IsInRole)) {

            return false;
         }

         return true;
      }

      protected XcstPage LoadPage(string path) {

         if (path == null) throw new ArgumentNullException(nameof(path));

         string absolutePath = VirtualPathUtility.Combine(this.VirtualPath, path);
         Type pageType = BuildManager.GetCompiledType(absolutePath);

         if (pageType == null) {
            throw new ArgumentException($"A page at '{absolutePath}' was not found.", nameof(path));
         }

         object pageInstance = Activator.CreateInstance(pageType);

         XcstPage page = pageInstance as XcstPage;

         if (page == null) {
            throw new ArgumentException($"The page at '{absolutePath}' must derive from {nameof(XcstPage)}.", nameof(path));
         }

         page.VirtualPath = absolutePath;

         CopyState(page);

         return page;
      }

      protected virtual void CopyState(XcstPage page) {
         page.Context = this.Context;
      }
   }
}
