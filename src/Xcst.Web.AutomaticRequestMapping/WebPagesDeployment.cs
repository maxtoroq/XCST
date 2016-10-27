// Copyright 2016 Max Toro Q.
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

#region WebPagesDeployment is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Web.Configuration;

namespace Xcst.Web {

   static class WebPagesDeployment {

      const string AppSettingsEnabledKey = "xcst:Enabled";

      /// <remarks>
      /// In a non-hosted scenario, this method would only look at a web.config that is present at the current path. Any config settings at an
      /// ancestor directory would not be considered.
      /// </remarks>
      public static bool IsExplicitlyDisabled(string path) {

         if (String.IsNullOrEmpty(path)) {
            throw new ArgumentNullException(nameof(path));
         }

         return IsExplicitlyDisabled(GetAppSettings(path));
      }

      static bool IsExplicitlyDisabled(NameValueCollection appSettings) {
         bool? enabled = GetEnabled(appSettings);
         return enabled.HasValue && enabled.Value == false;
      }

      /// <summary>
      /// Returns the value for webPages:Enabled AppSetting value in web.config.
      /// </summary>
      static bool? GetEnabled(NameValueCollection appSettings) {

         string enabledSetting = appSettings.Get(AppSettingsEnabledKey);

         if (String.IsNullOrEmpty(enabledSetting)) {
            return null;
         } else {
            return Boolean.Parse(enabledSetting);
         }
      }

      static NameValueCollection GetAppSettings(string path) {

         if (path.StartsWith("~/", StringComparison.Ordinal)) {

            // Path is virtual, assume we're hosted
            return (NameValueCollection)WebConfigurationManager.GetSection("appSettings", path);

         } else {

            // Path is physical, map it to an application
            WebConfigurationFileMap fileMap = new WebConfigurationFileMap();
            fileMap.VirtualDirectories.Add("/", new VirtualDirectoryMapping(path, true));
            var config = WebConfigurationManager.OpenMappedWebConfiguration(fileMap, "/");

            var appSettingsSection = config.AppSettings;
            var appSettings = new NameValueCollection();

            foreach (KeyValueConfigurationElement element in appSettingsSection.Settings) {
               appSettings.Add(element.Key, element.Value);
            }

            return appSettings;
         }
      }
   }
}
