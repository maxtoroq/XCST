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

namespace Xcst.Compiler.CodeGeneration {

   static class Helpers {

      public static string LocalPath(Uri uri) {

         if (uri == null) throw new ArgumentNullException(nameof(uri));

         if (!uri.IsAbsoluteUri) {
            return uri.OriginalString;
         }

         if (uri.IsFile) {
            return uri.LocalPath;
         }

         return uri.AbsoluteUri;
      }

      public static Uri MakeRelativeUri(Uri current, Uri compare) {
         return current.MakeRelativeUri(compare);
      }

      public static int QNameId(QualifiedName name) {
         return name.ToUriQualifiedName().GetHashCode();
      }
   }
}
