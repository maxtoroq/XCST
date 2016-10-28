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

#region UrlUtil and UrlRewriterHelper are based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.Routing;

// Code generation uses static method for Href function,
// therefore HttpContextBase cannot be provided dynamically
using HttpContextBase = System.Web.HttpContext;

namespace Xcst.Web.Runtime {

   /// <exclude/>
   public static class UrlUtil {

      static UrlRewriterHelper _urlRewriterHelper = new UrlRewriterHelper();

      // this method can accept an app-relative path or an absolute path for contentPath
      internal static string GenerateClientUrl(HttpContextBase httpContext, string contentPath) {

         if (String.IsNullOrEmpty(contentPath)) {
            return contentPath;
         }

         // many of the methods we call internally can't handle query strings properly, so just strip it out for
         // the time being
         string query;
         contentPath = StripQuery(contentPath, out query);

         // many of the methods we call internally can't handle query strings properly, so tack it on after processing
         // the virtual app path and url rewrites
         if (String.IsNullOrEmpty(query)) {
            return GenerateClientUrlInternal(httpContext, contentPath);
         } else {
            return GenerateClientUrlInternal(httpContext, contentPath) + query;
         }
      }

      public static string GenerateClientUrl(string basePath, string path, params object[] pathParts) {
         return GenerateClientUrl(HttpContext.Current, basePath, path, pathParts);
      }

      internal static string GenerateClientUrl(HttpContextBase httpContext, string basePath, string path, params object[] pathParts) {

         if (String.IsNullOrEmpty(path)) {
            return path;
         }

         if (basePath != null) {
            path = VirtualPathUtility.Combine(basePath, path);
         }

         string query;
         string processedPath = BuildUrl(path, out query, pathParts);

         // many of the methods we call internally can't handle query strings properly, so tack it on after processing
         // the virtual app path and url rewrites
         if (String.IsNullOrEmpty(query)) {
            return GenerateClientUrlInternal(httpContext, processedPath);
         } else {
            return GenerateClientUrlInternal(httpContext, processedPath) + query;
         }
      }

      static string GenerateClientUrlInternal(HttpContextBase httpContext, string contentPath) {

         if (String.IsNullOrEmpty(contentPath)) {
            return contentPath;
         }

         // can't call VirtualPathUtility.IsAppRelative since it throws on some inputs
         bool isAppRelative = contentPath[0] == '~';
         if (isAppRelative) {
            string absoluteContentPath = VirtualPathUtility.ToAbsolute(contentPath, httpContext.Request.ApplicationPath);
            return GenerateClientUrlInternal(httpContext, absoluteContentPath);
         }

         // we only want to manipulate the path if URL rewriting is active for this request, else we risk breaking the generated URL
         bool wasRequestRewritten = _urlRewriterHelper.WasRequestRewritten(httpContext);
         if (!wasRequestRewritten) {
            return contentPath;
         }

         // Since the rawUrl represents what the user sees in his browser, it is what we want to use as the base
         // of our absolute paths. For example, consider mysite.example.com/foo, which is internally
         // rewritten to content.example.com/mysite/foo. When we want to generate a link to ~/bar, we want to
         // base it from / instead of /foo, otherwise the user ends up seeing mysite.example.com/foo/bar,
         // which is incorrect.
         string relativeUrlToDestination = MakeRelative(httpContext.Request.Path, contentPath);
         string absoluteUrlToDestination = MakeAbsolute(httpContext.Request.RawUrl, relativeUrlToDestination);
         return absoluteUrlToDestination;
      }

      internal static string MakeAbsolute(string basePath, string relativePath) {

         // The Combine() method can't handle query strings on the base path, so we trim it off.
         string query;
         basePath = StripQuery(basePath, out query);
         return VirtualPathUtility.Combine(basePath, relativePath);
      }

      internal static string MakeRelative(string fromPath, string toPath) {

         string relativeUrl = VirtualPathUtility.MakeRelative(fromPath, toPath);
         if (String.IsNullOrEmpty(relativeUrl) || relativeUrl[0] == '?') {
            // Sometimes VirtualPathUtility.MakeRelative() will return an empty string when it meant to return '.',
            // but links to {empty string} are browser dependent. We replace it with an explicit path to force
            // consistency across browsers.
            relativeUrl = "./" + relativeUrl;
         }
         return relativeUrl;
      }

      static string StripQuery(string path, out string query) {

         int queryIndex = path.IndexOf('?');
         if (queryIndex >= 0) {
            query = path.Substring(queryIndex);
            return path.Substring(0, queryIndex);
         } else {
            query = null;
            return path;
         }
      }

      internal static string BuildUrl(string path, out string query, params object[] pathParts) {

         // Performance senstive 
         // 
         // This code branches on the number of path-parts to either favor string.Concat or StringBuilder 
         // for performance. The most common case (for WebPages) will provide a single int value as a 
         // path-part - string.Concat can be more efficient when we know the number of strings to join.

         string finalPath;

         if (pathParts == null
            || pathParts.Length == 0) {

            query = String.Empty;
            finalPath = path;

         } else if (pathParts.Length == 1) {

            object pathPart = pathParts[0];

            if (pathPart == null) {
               query = String.Empty;
               finalPath = path;

            } else if (IsDisplayableType(pathPart.GetType())) {

               string displayablePath = Convert.ToString(pathPart, CultureInfo.InvariantCulture);
               path = path + "/" + displayablePath;
               query = String.Empty;
               finalPath = path;

            } else {

               var queryBuilder = new StringBuilder();
               AppendToQueryString(queryBuilder, pathPart);

               query = queryBuilder.ToString();
               finalPath = path;
            }

         } else {

            var pathBuilder = new StringBuilder(path);
            var queryBuilder = new StringBuilder();

            for (int i = 0; i < pathParts.Length; i++) {

               object pathPart = pathParts[i];

               if (pathPart == null) {
                  continue;
               }

               if (IsDisplayableType(pathPart.GetType())) {

                  var displayablePath = Convert.ToString(pathPart, CultureInfo.InvariantCulture);
                  pathBuilder.Append('/');
                  pathBuilder.Append(displayablePath);

               } else {
                  AppendToQueryString(queryBuilder, pathPart);
               }
            }

            query = queryBuilder.ToString();
            finalPath = pathBuilder.ToString();
         }

         return HttpUtility.UrlPathEncode(finalPath);
      }

      /// <summary>
      /// Determines if a type is displayable as part of a Url path.
      /// </summary>
      /// <remarks>
      /// If a type is a displayable type, then we format values of that type as part of the Url Path. If not, then
      /// we attempt to create a RouteValueDictionary, and encode the value as key-value pairs in the query string.
      /// 
      /// We determine if a type is displayable by whether or not it implements any interfaces. The built-in simple
      /// types like Int32 implement IFormattable, which will be used to convert it to a string. 
      /// 
      /// Primarily we do this check to allow anonymous types to represent key-value pairs (anonymous types don't 
      /// implement any interfaces). 
      /// </remarks>
      static bool IsDisplayableType(Type t) {
         return t.GetInterfaces().Length > 0;
      }

      static void AppendToQueryString(StringBuilder queryString, object obj) {

         // If this method is called, then obj isn't a type that we can put in the path, instead
         // we want to format it as key-value pairs for the query string. The mostly likely 
         // user scenario for this is an anonymous type.
         IDictionary<string, object> dictionary = ObjectToDictionary(obj);

         foreach (var item in dictionary) {
            if (queryString.Length == 0) {
               queryString.Append('?');
            } else {
               queryString.Append('&');
            }

            string stringValue = Convert.ToString(item.Value, CultureInfo.InvariantCulture);

            queryString.Append(HttpUtility.UrlEncode(item.Key))
                .Append('=')
                .Append(HttpUtility.UrlEncode(stringValue));
         }
      }

      static RouteValueDictionary ObjectToDictionary(object value) {

         RouteValueDictionary dictionary = new RouteValueDictionary();

         if (value != null) {
            foreach (PropertyHelper helper in PropertyHelper.GetProperties(value)) {
               dictionary.Add(helper.Name, helper.GetValue(value));
            }
         }

         return dictionary;
      }
   }

   class UrlRewriterHelper {

      public const string UrlWasRewrittenServerVar = "IIS_WasUrlRewritten";
      public const string UrlRewriterEnabledServerVar = "IIS_UrlRewriteModule";

      public const string UrlWasRequestRewrittenTrueValue = "true";
      public const string UrlWasRequestRewrittenFalseValue = "false";

      object _lockObject = new object();
      bool _urlRewriterIsTurnedOnValue;
      volatile bool _urlRewriterIsTurnedOnCalculated = false;

      public virtual bool WasRequestRewritten(HttpContextBase httpContext) {
         return IsUrlRewriterTurnedOn(httpContext) && WasThisRequestRewritten(httpContext);
      }

      bool IsUrlRewriterTurnedOn(HttpContextBase httpContext) {

         // Need to do double-check locking because a single instance of this class is shared in the entire app domain (see PathHelpers)
         if (!_urlRewriterIsTurnedOnCalculated) {
            lock (_lockObject) {
               if (!_urlRewriterIsTurnedOnCalculated) {
                  HttpWorkerRequest httpWorkerRequest = GetService<HttpWorkerRequest>(httpContext);
                  bool urlRewriterIsEnabled = (httpWorkerRequest != null && httpWorkerRequest.GetServerVariable(UrlRewriterEnabledServerVar) != null);
                  _urlRewriterIsTurnedOnValue = urlRewriterIsEnabled;
                  _urlRewriterIsTurnedOnCalculated = true;
               }
            }
         }
         return _urlRewriterIsTurnedOnValue;
      }

      static bool WasThisRequestRewritten(HttpContextBase httpContext) {

         if (httpContext.Items.Contains(UrlWasRewrittenServerVar)) {
            return Object.Equals(httpContext.Items[UrlWasRewrittenServerVar], UrlWasRequestRewrittenTrueValue);
         } else {
            HttpWorkerRequest httpWorkerRequest = GetService<HttpWorkerRequest>(httpContext);
            bool requestWasRewritten = (httpWorkerRequest != null && httpWorkerRequest.GetServerVariable(UrlWasRewrittenServerVar) != null);

            if (requestWasRewritten) {
               httpContext.Items.Add(UrlWasRewrittenServerVar, UrlWasRequestRewrittenTrueValue);
            } else {
               httpContext.Items.Add(UrlWasRewrittenServerVar, UrlWasRequestRewrittenFalseValue);
            }

            return requestWasRewritten;
         }
      }

      static TService GetService<TService>(IServiceProvider httpContext) {
         return (TService)httpContext.GetService(typeof(TService));
      }
   }
}
