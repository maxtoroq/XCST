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

#region WebPageRoute is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Web;
using System.Web.Hosting;

namespace Xcst.Web {

   sealed class WebPageRoute {

      static readonly Lazy<bool> _isRootExplicitlyDisabled = new Lazy<bool>(() => false /*WebPagesDeployment.IsExplicitlyDisabled("~/")*/);
      bool? _isExplicitlyDisabled;

      public bool IsExplicitlyDisabled {
         get { return _isExplicitlyDisabled ?? _isRootExplicitlyDisabled.Value; }
         set { _isExplicitlyDisabled = value; }
      }

      public void DoPostResolveRequestCache(HttpContextBase context) {

         if (this.IsExplicitlyDisabled) {
            // If the root config is explicitly disabled, do not process the request.
            return;
         }

         HttpRequestBase request = context.Request;

         // Parse incoming URL (we trim off the first two chars since they're always "~/")
         string requestPath = request.AppRelativeCurrentExecutionFilePath.Substring(2) + request.PathInfo;
         string[] registeredExtensions = { "xcst" };

         // Check if this request matches a file in the app
         WebPageMatch webpageRouteMatch = MatchRequest(requestPath, registeredExtensions, HostingEnvironment.VirtualPathProvider.FileExists /*VirtualPathFactoryManager.InstancePathExists*/, context/*, DisplayModeProvider.Instance*/);

         if (webpageRouteMatch != null) {

            // If it matches then save some data for the WebPage's UrlData
            context.Items[typeof(WebPageMatch)] = webpageRouteMatch;

            string virtualPath = "~/" + webpageRouteMatch.MatchedPath;

            // Verify that this path is enabled before remapping
            //if (!WebPagesDeployment.IsExplicitlyDisabled(virtualPath)) {

            IHttpHandler handler = XcstPageHttpHandler.CreateFromVirtualPath(virtualPath);

            if (handler != null) {
               //SessionStateUtil.SetUpSessionState(context, handler);
               // Remap to our handler
               context.RemapHandler(handler);
            }
            //}
         } else {

            // Bug:904704 If its not a match, but to a supported extension, we want to return a 404 instead of a 403
            string extension = PathUtil.GetExtension(requestPath);

            foreach (string supportedExt in registeredExtensions) {
               if (String.Equals("." + supportedExt, extension, StringComparison.OrdinalIgnoreCase)) {
                  throw new HttpException(404, null);
               }
            }
         }
      }

      public static WebPageMatch GetWebPageMatch(HttpContextBase context) {
         WebPageMatch webPageMatch = (WebPageMatch)context.Items[typeof(WebPageMatch)];
         return webPageMatch;
      }

      public static WebPageMatch MatchRequest(string pathValue, string[] supportedExtensions, Func<string, bool> virtualPathExists, HttpContextBase context/*, DisplayModeProvider displayModes*/) {

         string currentLevel = String.Empty;
         string currentPathInfo = pathValue;

         // We can skip the file exists check and normal lookup for empty paths, but we still need to look for default pages
         if (!String.IsNullOrEmpty(pathValue)) {

            // If the file exists and its not a supported extension, let the request go through
            if (FileExists(pathValue, virtualPathExists)) {

               // TODO: Look into switching to RawURL to eliminate the need for this issue
               bool foundSupportedExtension = false;

               for (int i = 0; i < supportedExtensions.Length; i++) {
                  string supportedExtension = supportedExtensions[i];
                  if (PathHelpers.EndsWithExtension(pathValue, supportedExtension)) {
                     foundSupportedExtension = true;
                     break;
                  }
               }

               if (!foundSupportedExtension) {
                  return null;
               }
            }

            // For each trimmed part of the path try to add a known extension and
            // check if it matches a file in the application.
            currentLevel = pathValue;
            currentPathInfo = String.Empty;

            while (true) {

               // Does the current route level patch any supported extension?
               string routeLevelMatch = GetRouteLevelMatch(currentLevel, supportedExtensions, virtualPathExists, context/*, displayModes*/);

               if (routeLevelMatch != null) {
                  return new WebPageMatch(routeLevelMatch, currentPathInfo);
               }

               // Try to remove the last path segment (e.g. go from /foo/bar to /foo)
               int indexOfLastSlash = currentLevel.LastIndexOf('/');

               if (indexOfLastSlash == -1) {
                  // If there are no more slashes, we're done
                  break;
               } else {
                  // Chop off the last path segment to get to the next one
                  currentLevel = currentLevel.Substring(0, indexOfLastSlash);

                  // And save the path info in case there is a match
                  currentPathInfo = pathValue.Substring(indexOfLastSlash + 1);
               }
            }
         }

         return MatchDefaultFiles(pathValue, supportedExtensions, virtualPathExists, context/*, displayModes*/, currentLevel);
      }

      static string GetRouteLevelMatch(string pathValue, string[] supportedExtensions, Func<string, bool> virtualPathExists, HttpContextBase context/*, DisplayModeProvider displayModeProvider*/) {

         for (int i = 0; i < supportedExtensions.Length; i++) {
            string supportedExtension = supportedExtensions[i];

            // For performance, avoid multiple calls to String.Concat
            string virtualPath;
            // Only add the extension if it's not already there
            if (!PathHelpers.EndsWithExtension(pathValue, supportedExtension)) {
               virtualPath = "~/" + pathValue + "." + supportedExtension;
            } else {
               virtualPath = "~/" + pathValue;
            }

            //DisplayInfo virtualPathDisplayInfo = displayModeProvider.GetDisplayInfoForVirtualPath(virtualPath, context, virtualPathExists, currentDisplayMode: null);
            var virtualPathDisplayInfo = virtualPathExists(virtualPath) ?
               new { FilePath = virtualPath }
               : null;

            if (virtualPathDisplayInfo != null) {
               // If there's an exact match on disk, return it unless it starts with an underscore
               if (Path.GetFileName(virtualPathDisplayInfo.FilePath).StartsWith("_", StringComparison.OrdinalIgnoreCase)) {
                  throw new HttpException(404, "Files with leading underscores (\"_\") cannot be served.");
               }

               string resolvedVirtualPath = virtualPathDisplayInfo.FilePath;

               // Matches are not expected to be virtual paths so remove the ~/ from the match
               if (resolvedVirtualPath.StartsWith("~/", StringComparison.OrdinalIgnoreCase)) {
                  resolvedVirtualPath = resolvedVirtualPath.Remove(0, 2);
               }

               //DisplayModeProvider.SetDisplayMode(context, virtualPathDisplayInfo.DisplayMode);

               return resolvedVirtualPath;
            }
         }

         return null;
      }

      static WebPageMatch MatchDefaultFiles(string pathValue, string[] supportedExtensions, Func<string, bool> virtualPathExists, HttpContextBase context/*, DisplayModeProvider displayModes*/, string currentLevel) {

         // If we haven't found anything yet, now try looking for default.* or index.* at the current url
         currentLevel = pathValue;
         string currentLevelDefault;
         string currentLevelIndex;

         if (String.IsNullOrEmpty(currentLevel)) {
            currentLevelDefault = "default";
            currentLevelIndex = "index";
         } else {

            if (currentLevel[currentLevel.Length - 1] != '/') {
               currentLevel += "/";
            }

            currentLevelDefault = currentLevel + "default";
            currentLevelIndex = currentLevel + "index";
         }

         // Does the current route level match any supported extension?
         //string defaultMatch = GetRouteLevelMatch(currentLevelDefault, supportedExtensions, virtualPathExists, context/*, displayModes*/);

         //if (defaultMatch != null) {
         //   return new WebPageMatch(defaultMatch, String.Empty);
         //}

         string indexMatch = GetRouteLevelMatch(currentLevelIndex, supportedExtensions, virtualPathExists, context/*, displayModes*/);

         if (indexMatch != null) {
            return new WebPageMatch(indexMatch, String.Empty);
         }

         return null;
      }

      static bool FileExists(string virtualPath, Func<string, bool> virtualPathExists) {
         var path = "~/" + virtualPath;
         return virtualPathExists(path);
      }

      static class PathHelpers {

         public static bool EndsWithExtension(string path, string extension) {

            Contract.Assert(path != null);
            Contract.Assert(extension != null && extension.Length > 0);

            if (path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) {
               int extensionLength = extension.Length;
               int pathLength = path.Length;
               return (pathLength > extensionLength && path[pathLength - extensionLength - 1] == '.');
            }

            return false;
         }
      }

      static class PathUtil {

         /// <summary>
         /// Path.GetExtension performs a CheckInvalidPathChars(path) which blows up for paths that do not translate to valid physical paths but are valid paths in ASP.NET
         /// This method is a near clone of Path.GetExtension without a call to CheckInvalidPathChars(path);
         /// </summary>
         internal static string GetExtension(string path) {
            if (String.IsNullOrEmpty(path)) {
               return path;
            }
            int current = path.Length;
            while (--current >= 0) {
               char ch = path[current];
               if (ch == '.') {
                  if (current == path.Length - 1) {
                     break;
                  }
                  return path.Substring(current);
               }
               if (ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar) {
                  break;
               }
            }
            return String.Empty;
         }

         internal static bool IsWithinAppRoot(string appDomainAppVirtualPath, string virtualPath) {
            if (appDomainAppVirtualPath == null) {
               // If the runtime has not been initialized, just return true.
               return true;
            }

            var absPath = virtualPath;
            if (!VirtualPathUtility.IsAbsolute(absPath)) {
               absPath = VirtualPathUtility.ToAbsolute(absPath);
            }
            // We need to call this overload because it returns null if the path is not within the application root.
            // The overload calls into MakeVirtualPathAppRelative(string virtualPath, string applicationPath, bool nullIfNotInApp), with 
            // nullIfNotInApp set to true.
            return VirtualPathUtility.ToAppRelative(absPath, appDomainAppVirtualPath) != null;
         }

         /// <summary>
         /// Determines true if the path is simply "MyPath", and not app-relative "~/MyPath" or absolute "/MyApp/MyPath" or relative "../Test/MyPath"
         /// </summary>
         /// <returns>True if it is a not app-relative, absolute or relative.</returns>
         internal static bool IsSimpleName(string path) {
            if (VirtualPathUtility.IsAbsolute(path) || VirtualPathUtility.IsAppRelative(path)) {
               return false;
            }
            if (path.StartsWith(".", StringComparison.OrdinalIgnoreCase)) {
               return false;
            }
            return true;
         }
      }
   }

   sealed class WebPageMatch {

      public string MatchedPath { get; }

      public string PathInfo { get; }

      public WebPageMatch(string matchedPath, string pathInfo) {
         MatchedPath = matchedPath;
         PathInfo = pathInfo;
      }
   }
}
