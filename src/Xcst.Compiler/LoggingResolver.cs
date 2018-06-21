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

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Xml;

namespace Xcst.Compiler {

   class LoggingResolver : XmlResolver {

      readonly XmlResolver wrappedResolver;

      public HashSet<Uri> ResolvedUris { get; } = new HashSet<Uri>();

      public override ICredentials Credentials {
         set { wrappedResolver.Credentials = value; }
      }

      public LoggingResolver(XmlResolver wrappedResolver) {

         if (wrappedResolver == null) throw new ArgumentNullException(nameof(wrappedResolver));

         this.wrappedResolver = wrappedResolver;
      }

      public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn) {

         object entity = this.wrappedResolver.GetEntity(absoluteUri, role, ofObjectToReturn);

         this.ResolvedUris.Add(absoluteUri);

         return entity;
      }

      public override Task<object> GetEntityAsync(Uri absoluteUri, string role, Type ofObjectToReturn) {

         var task = this.wrappedResolver.GetEntityAsync(absoluteUri, role, ofObjectToReturn);

         this.ResolvedUris.Add(absoluteUri);

         return task;
      }

      public override bool SupportsType(Uri absoluteUri, Type type) {
         return this.wrappedResolver.SupportsType(absoluteUri, type);
      }

      public override Uri ResolveUri(Uri baseUri, string relativeUri) {
         return this.wrappedResolver.ResolveUri(baseUri, relativeUri);
      }
   }
}
