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
using System.Xml;
using System.Net;
using System.Reflection;
using System.IO;

namespace Xcst.Compiler {

   class XmlEmbeddedResourceResolver : XmlResolver {

      public static readonly string UriSchemeClires = "clires";

      readonly Assembly assembly;
      readonly Uri principalModuleUri;
      readonly Uri extensionsModuleUri;

      readonly Action<Stream> buildExtensionsModuleFn;
      readonly Func<Uri, Stream> loadExtensionXslt;

      public override ICredentials Credentials { set { } }

      public XmlEmbeddedResourceResolver(Assembly assembly, Uri principalModuleUri, Action<Stream> buildExtensionsModuleFn, Func<Uri, Stream> loadExtensionXslt) {

         if (assembly == null) throw new ArgumentNullException(nameof(assembly));
         if (principalModuleUri == null) throw new ArgumentNullException(nameof(principalModuleUri));
         if (buildExtensionsModuleFn == null) throw new ArgumentNullException(nameof(buildExtensionsModuleFn));
         if (loadExtensionXslt == null) throw new ArgumentNullException(nameof(loadExtensionXslt));

         this.assembly = assembly;
         this.principalModuleUri = principalModuleUri;
         this.extensionsModuleUri = new Uri(principalModuleUri, "xcst-extensions.xsl");

         this.buildExtensionsModuleFn = buildExtensionsModuleFn;
         this.loadExtensionXslt = loadExtensionXslt;
      }

      public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn) {

         if (absoluteUri == null) throw new ArgumentNullException(nameof(absoluteUri));
         if (absoluteUri.AbsolutePath.Length <= 1) throw new ArgumentException("The embedded resource name must be specified in the AbsolutePath portion of the supplied Uri.", nameof(absoluteUri));

         if (absoluteUri.Scheme != UriSchemeClires) {
            return LoadExtensionXslt(absoluteUri, role, ofObjectToReturn);

         } else if (absoluteUri == this.extensionsModuleUri) {
            return BuildExtensionsModule();
         }
 
         string host = absoluteUri.Host;

         if (String.IsNullOrEmpty(host)) {
            host = null;
         }

         string resourceName = ((host != null) ?
            absoluteUri.GetComponents(UriComponents.Host | UriComponents.Path, UriFormat.Unescaped)
            : absoluteUri.AbsolutePath)
            .Replace('/', '.');

         return this.assembly.GetManifestResourceStream(resourceName);
      }

      object BuildExtensionsModule() {

         var stream = new MemoryStream();
         this.buildExtensionsModuleFn(stream);
         stream.Position = 0;

         return stream;
      }

      object LoadExtensionXslt(Uri absoluteUri, string role, Type ofObjectToReturn) {
         return this.loadExtensionXslt(absoluteUri);
      }
   }
}
