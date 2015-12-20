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

namespace Xcst.Web {

   public abstract class XcstPage {

      HttpContextBase _Context;
      HttpRequestBase _Request;
      HttpResponseBase _Response;
      HttpSessionStateBase _Session;
      HttpServerUtilityBase _Server;

      IList<string> _UrlData;
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
            _UrlData = null;
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

      public virtual IList<string> UrlData {
         get {
            if (_UrlData == null
               && Context != null) {

               _UrlData = new UrlDataList(WebPageRoute.GetWebPageMatch(Context)?.PathInfo);
            }
            return _UrlData;
         }
         set { _UrlData = value; }
      }

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

      public virtual string Href(string path, params object[] pathParts) {
         return UrlUtil.GenerateClientUrl(this.Context, this.VirtualPath, path, pathParts);
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
   }
}
