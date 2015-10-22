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

namespace Xcst {

   public class QualifiedName : IEquatable<QualifiedName> {

      readonly string _Name;
      readonly string _Namespace;

      int hash;

      public string Name => _Name;

      public string Namespace => _Namespace;

      public QualifiedName(string name)
         : this(name, String.Empty) { }

      public QualifiedName(string name, string ns) {

         if (name == null) throw new ArgumentNullException(nameof(name));
         if (String.IsNullOrWhiteSpace(name)) throw new ArgumentException($"{nameof(name)} cannot be empty.", nameof(name));

         _Name = name;
         _Namespace = ns ?? String.Empty;
      }

      public override bool Equals(object other) {
         return Equals(other as QualifiedName);
      }

      public virtual bool Equals(QualifiedName other) {

         if (other == null) {
            return false;
         }

         return this.Name == other.Name
            && this.Namespace == other.Namespace;
      }

      public override int GetHashCode() {

         if (this.hash == 0) {
            this.hash = ToString().GetHashCode();
         }

         return this.hash;
      }

      public override string ToString() {

         if (this.Namespace.Length == 0) {
            return this.Name;
         }

         return $"{{{this.Namespace}}}{this.Name}";
      }

      public static bool operator ==(QualifiedName left, QualifiedName right) {

         if (Object.ReferenceEquals(left, right)) {
            return true;
         }

         if ((object)left == null || (object)right == null) {
            return false;
         }

         return left.Equals(right);
      }

      public static bool operator !=(QualifiedName left, QualifiedName right) {
         return !(left == right);
      }
   }
}
