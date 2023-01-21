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
using System.Diagnostics;
using System.Globalization;

namespace Xcst.Runtime;

/// <exclude/>
public class ExecutionContext {

   readonly Func<IFormatProvider>
   _formatProviderFn;

   public IXcstPackage
   TopLevelPackage { get; init; }

   public PrimingContext
   PrimingContext { get; init; }

   public SimpleContent
   SimpleContent { get; }

   public Uri?
   StaticBaseUri { get; init; }

   public Uri?
   BaseOutputUri { get; init; }

   internal Action<MessageArgs>?
   MessageListener { get; init; }

   internal
#pragma warning disable CS8618
   ExecutionContext(
#pragma warning restore CS8618
         Func<IFormatProvider>? formatProviderFn) {

      _formatProviderFn = formatProviderFn ?? (() => CultureInfo.CurrentCulture);
      this.SimpleContent = new SimpleContent(_formatProviderFn);
   }

   public Uri
   ResolveUri(string relativeUri) {

      var relUri = new Uri(relativeUri, UriKind.RelativeOrAbsolute);

      if (relUri.IsAbsoluteUri) {
         return relUri;
      }

      if (this.StaticBaseUri is null) {
         throw new RuntimeException("Cannot resolve relative URI. Specify a base URI.");
      }

      return NewUri(this.StaticBaseUri, relativeUri);
   }

   public Uri
   ResolveOutputUri(string relativeUri) {

      var relUri = new Uri(relativeUri, UriKind.RelativeOrAbsolute);

      if (relUri.IsAbsoluteUri) {
         return relUri;
      }

      if (this.BaseOutputUri is null) {
         throw new RuntimeException("Cannot resolve relative URI. Specify a base output URI.");
      }

      return NewUri(this.BaseOutputUri, relativeUri);
   }

   static Uri
   NewUri(Uri baseUri, string relativeUri) {

      try {
         return new Uri(baseUri, relativeUri);
      } catch (UriFormatException ex) {
         throw new RuntimeException(ex.Message);
      }
   }

   public void
   LogMessage(MessageArgs args) {
      this.MessageListener?.Invoke(args);
   }
}
