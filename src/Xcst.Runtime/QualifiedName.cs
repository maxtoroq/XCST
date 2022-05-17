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
using Xcst.Runtime;

namespace Xcst;

public class QualifiedName : IEquatable<QualifiedName> {

   readonly string
   _name;

   readonly string
   _namespace;

   int
   _hash;

   public string
   Name => _name;

   public string
   Namespace => _namespace;

   public
   QualifiedName(string name)
      : this(name, null) { }

   public
   QualifiedName(string name, string? ns) {

      if (name is null) throw new ArgumentNullException(nameof(name));
      if (String.IsNullOrWhiteSpace(name)) throw new ArgumentException($"{nameof(name)} cannot be empty.", nameof(name));

      _name = name;
      _namespace = ns ?? String.Empty;
   }

   public override bool
   Equals(object? other) =>
      Equals(other as QualifiedName);

   public virtual bool
   Equals(QualifiedName? other) {

      if (other is null) {
         return false;
      }

      return this.Name == other.Name
         && this.Namespace == other.Namespace;
   }

   public override int
   GetHashCode() {

      if (_hash == 0) {
         _hash = ToString().GetHashCode();
      }

      return _hash;
   }

   public override string
   ToString() {

      if (this.Namespace.Length == 0) {
         return this.Name;
      }

      return ToUriQualifiedName();
   }

   public string
   ToUriQualifiedName() =>
      UriQualifiedName(this.Namespace, this.Name);

   internal static string
   UriQualifiedName(string? ns, string name) =>
      "Q{" + ns + "}" + name;

   public static QualifiedName
   Parse(string localOrUriQualifiedName) {

      if (localOrUriQualifiedName is null) throw new ArgumentNullException(nameof(localOrUriQualifiedName));
      if (String.IsNullOrWhiteSpace(localOrUriQualifiedName)) throw new ArgumentException($"{nameof(localOrUriQualifiedName)} cannot be empty.", nameof(localOrUriQualifiedName));

      if (localOrUriQualifiedName.Length > 2
         && localOrUriQualifiedName[0] == 'Q'
         && localOrUriQualifiedName[1] == '{') {

         var closeIndex = localOrUriQualifiedName.IndexOf('}');

         if (closeIndex < 0) {
            throw new ArgumentException("Closing brace not found.", nameof(localOrUriQualifiedName));
         }

         var ns = SimpleContent.Trim(localOrUriQualifiedName.Substring(2, closeIndex - 2));
         var local = localOrUriQualifiedName.Substring(closeIndex + 1);

         return new QualifiedName(local, ns);
      }

      return new QualifiedName(localOrUriQualifiedName);
   }

   public static bool operator
   ==(QualifiedName? left, QualifiedName? right) {

      if (Object.ReferenceEquals(left, right)) {
         return true;
      }

      if (left is null || right is null) {
         return false;
      }

      return left.Equals(right);
   }

   public static bool operator
   !=(QualifiedName? left, QualifiedName? right) => !(left == right);
}
