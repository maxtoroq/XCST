#region ITypeName and related types are based on code from Mono
//
// System.TypeIdentifier.cs
//
// Author:
//   Aleksey Kliger <aleksey@xamarin.com>
//
//
// Copyright (C) 2015 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

using System;

namespace Xcst.Compiler.Reflection {

   // A ITypeName is wrapper around type names in display form
   // (that is, with special characters escaped).
   //
   // Note that in general if you unescape a type name, you will
   // lose information: If the type name's DisplayName is
   // Foo\+Bar+Baz (outer class ``Foo+Bar``, inner class Baz)
   // unescaping the first plus will give you (outer class Foo,
   // inner class Bar, innermost class Baz).
   //
   // The correct way to take a ITypeName apart is to feed its
   // DisplayName to TypeSpec.Parse()

   interface ITypeName : IEquatable<ITypeName> {

      string
      DisplayName { get; }

      // add a nested name under this one.
      ITypeName
      NestedName(ITypeIdentifier innerName);
   }

   // A type identifier is a single component of a type name.
   // Unlike a general typename, a type identifier can be be
   // converted to internal form without loss of information.
   interface ITypeIdentifier : ITypeName {

      string
      InternalName { get; }
   }

   class TypeNames {

      internal static ITypeName
      FromDisplay(string displayName) =>
         new Display(displayName);

      internal abstract class ATypeName : ITypeName {

         public abstract string
         DisplayName { get; }

         public abstract ITypeName
         NestedName(ITypeIdentifier innerName);

         public bool
         Equals(ITypeName? other) =>
            other != null
               && DisplayName == other.DisplayName;

         public override int
         GetHashCode() =>
            DisplayName.GetHashCode();

         public override bool
         Equals(object? other) =>
            Equals(other as ITypeName);
      }

      class Display : ATypeName {

         readonly string
         _displayName;

         internal
         Display(string displayName) {
            _displayName = displayName;
         }

         public override string
         DisplayName => _displayName;

         public override ITypeName
         NestedName(ITypeIdentifier innerName) =>
            new Display(DisplayName + "+" + innerName.DisplayName);
      }
   }

   internal class TypeIdentifiers {

      internal static ITypeIdentifier
      FromDisplay(string displayName) =>
         new Display(displayName);

      internal static ITypeIdentifier
      FromInternal(string internalName) =>
         new Internal(internalName);

      internal static ITypeIdentifier
      FromInternal(string internalNameSpace, ITypeIdentifier typeName) =>
         new Internal(internalNameSpace, typeName);

      // Only use if simpleName is certain not to contain
      // unexpected characters that ordinarily require
      // escaping: ,+*&[]\
      internal static ITypeIdentifier
      WithoutEscape(string simpleName) =>
         new NoEscape(simpleName);

      class Display : TypeNames.ATypeName, ITypeIdentifier {

         readonly string
         _displayName;

         string?
         _internalName; //cached

         public override string
         DisplayName => _displayName;

         public string
         InternalName => _internalName
            ?? (_internalName = GetInternalName());

         internal
         Display(string displayName) {

            _displayName = displayName;
            _internalName = null;
         }

         string
         GetInternalName() =>
            TypeSpec.UnescapeInternalName(_displayName);

         public override ITypeName
         NestedName(ITypeIdentifier innerName) =>
            TypeNames.FromDisplay(DisplayName + "+" + innerName.DisplayName);
      }

      class Internal : TypeNames.ATypeName, ITypeIdentifier {

         readonly string
         _internalName;

         string?
         _displayName; //cached

         public override string
         DisplayName => _displayName ??= GetDisplayName();

         public string
         InternalName => _internalName;

         internal
         Internal(string internalName) {

            _internalName = internalName;
            _displayName = null;
         }

         internal
         Internal(string nameSpaceInternal, ITypeIdentifier typeName) {

            _internalName = nameSpaceInternal + "." + typeName.InternalName;
            _displayName = null;
         }

         string
         GetDisplayName() =>
            TypeSpec.EscapeDisplayName(_internalName);

         public override ITypeName
         NestedName(ITypeIdentifier innerName) =>
            TypeNames.FromDisplay(DisplayName + "+" + innerName.DisplayName);
      }

      class NoEscape : TypeNames.ATypeName, ITypeIdentifier {

         readonly string
         _simpleName;

         public override string
         DisplayName => _simpleName;

         public string
         InternalName => _simpleName;

         internal
         NoEscape(string simpleName) {

            _simpleName = simpleName;
#if DEBUG
            checkNoBadChars(simpleName);
#endif
         }

#if DEBUG
         static private void
         checkNoBadChars(string s) {

            if (TypeSpec.NeedsEscaping(s)) {
               throw new ArgumentException("simpleName");
            }
         }
#endif

         public override ITypeName
         NestedName(ITypeIdentifier innerName) =>
            TypeNames.FromDisplay(DisplayName + "+" + innerName.DisplayName);
      }
   }
}
