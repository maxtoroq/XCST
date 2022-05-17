#region TypeSpec and related types are based on code from Mono
//
// System.Type.cs
//
// Author:
//   Rodrigo Kumpera <kumpera@gmail.com>
//
//
// Copyright (C) 2010 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xcst.Compiler.Reflection;

class TypeSpec {

   ITypeIdentifier
   _name;

   string?
   _assemblyName;

   IList<ITypeIdentifier>?
   _nested;

   IList<TypeSpec>?
   _genericParams;

   IList<IModifierSpec>?
   _modifierSpec;

   bool
   _isByref;

   string?
   _displayFullname; // cache

   public bool
   HasModifiers => _modifierSpec != null;

   public bool
   HasGenericParameters => _genericParams != null;

   public bool
   IsNested => _nested != null
      && _nested.Count > 0;

   public bool
   IsByRef => _isByref;

   public bool?
   IsReferenceType { get; set; }

   public ITypeName
   Name => _name;

   public IEnumerable<ITypeName>
   Nested => _nested
      ?? (IEnumerable<ITypeName>)Array.Empty<ITypeName>();

   public IList<IModifierSpec>
   Modifiers => _modifierSpec
      ?? Array.Empty<IModifierSpec>();

   public IList<TypeSpec>
   GenericParameters => _genericParams
      ?? Array.Empty<TypeSpec>();

   internal string
   DisplayFullName =>
      _displayFullname ??= GetDisplayFullName(DisplayNameFormat.Default);

#pragma warning disable CS8618
   private
   TypeSpec() { }
#pragma warning restore CS8618

   internal
   TypeSpec(string name) {
      _name = TypeIdentifiers.FromDisplay(name);
   }

   internal
   TypeSpec(string name, bool isReferenceType)
      : this(name) {

      this.IsReferenceType = isReferenceType;
   }

   public TypeSpec
   Clone() =>
      new TypeSpec {
         _name = ParsedTypeIdentifier(_name.DisplayName),
         _assemblyName = _assemblyName,
         _nested = _nested?.ToList(),
         _modifierSpec = _modifierSpec?.ToList(),
         _genericParams = _genericParams?.ToList(),
         _isByref = _isByref,
         IsReferenceType = this.IsReferenceType
      };

   internal string
   GetDisplayFullName(DisplayNameFormat flags) {

      var wantAssembly = (flags & DisplayNameFormat.WANT_ASSEMBLY) != 0;
      var wantModifiers = (flags & DisplayNameFormat.NO_MODIFIERS) == 0;

      var sb = new StringBuilder(_name.DisplayName);

      if (_nested != null) {

         foreach (var n in _nested) {

            sb.Append('+')
               .Append(n.DisplayName);
         }
      }

      if (_genericParams != null) {

         sb.Append('[');

         for (int i = 0; i < _genericParams.Count; ++i) {

            if (i > 0) {
               sb.Append(", ");
            }

            if (_genericParams[i]._assemblyName != null) {

               sb.Append('[')
                  .Append(_genericParams[i].DisplayFullName)
                  .Append(']');

            } else {

               sb.Append(_genericParams[i].DisplayFullName);
            }
         }

         sb.Append(']');
      }

      if (wantModifiers) {
         GetModifierString(sb);
      }

      if (_assemblyName != null
         && wantAssembly) {

         sb.Append(", ")
            .Append(_assemblyName);
      }

      return sb.ToString();
   }

   internal string
   ModifierString() =>
      GetModifierString(new StringBuilder())
         .ToString();

   StringBuilder
   GetModifierString(StringBuilder sb) {

      if (_modifierSpec != null) {
         foreach (var md in _modifierSpec) {
            md.Append(sb);
         }
      }

      if (_isByref) {
         sb.Append('&');
      }

      return sb;
   }

   public static TypeSpec
   Parse(string typeName) {

      if (typeName is null) throw new ArgumentNullException(nameof(typeName));

      var pos = 0;
      var res = Parse(typeName, ref pos, false, true);

      if (pos < typeName.Length) {
         throw new ArgumentException("Count not parse the whole type name", nameof(typeName));
      }

      return res;
   }

   static TypeSpec
   Parse(string typeName, ref int p, bool isRecurse, bool allowAqn) {

      // Invariants:
      //  - On exit p, is updated to pos the current unconsumed character.
      //
      //  - The callee peeks at but does not consume delimiters following
      //    recurisve parse (so for a recursive call like the args of "Foo[P,Q]"
      //    we'll return with p either on ',' or on ']'.  If the name was aqn'd
      //    "Foo[[P,assmblystuff],Q]" on return p with be on the ']' just
      //    after the "assmblystuff")
      //
      //  - If allowAqn is True, assembly qualification is optional.
      //    If allowAqn is False, assembly qualification is prohibited.

      var pos = p;
      int nameStart;
      var inModifiers = false;
      var data = new TypeSpec();

      SkipSpace(typeName, ref pos);

      nameStart = pos;

      for (; pos < typeName.Length; ++pos) {

         switch (typeName[pos]) {
            case '+':
               data.AddName(typeName.Substring(nameStart, pos - nameStart));
               nameStart = pos + 1;
               break;

            case ',':
            case ']':
               data.AddName(typeName.Substring(nameStart, pos - nameStart));
               nameStart = pos + 1;
               inModifiers = true;

               if (isRecurse && !allowAqn) {
                  p = pos;
                  return data;
               }

               break;

            case '&':
            case '*':
            case '[':

               if (typeName[pos] != '[' && isRecurse) {
                  throw new ArgumentException("Generic argument can't be byref or pointer type", nameof(typeName));
               }

               data.AddName(typeName.Substring(nameStart, pos - nameStart));
               nameStart = pos + 1;
               inModifiers = true;
               break;

            case '\\':
               pos++;
               break;
         }

         if (inModifiers) {
            break;
         }
      }

      if (nameStart < pos) {
         data.AddName(typeName.Substring(nameStart, pos - nameStart));

      } else if (nameStart == pos) {
         data.AddName(String.Empty);
      }

      if (inModifiers) {

         for (; pos < typeName.Length; ++pos) {

            switch (typeName[pos]) {
               case '&':
                  if (data._isByref) {
                     throw new ArgumentException("Can't have a byref of a byref", nameof(typeName));
                  }

                  data._isByref = true;
                  break;

               case '*':
                  if (data._isByref) {
                     throw new ArgumentException("Can't have a pointer to a byref type", nameof(typeName));
                  }

                  // take subsequent '*'s too
                  var pointer_level = 1;

                  while (pos + 1 < typeName.Length && typeName[pos + 1] == '*') {
                     ++pos;
                     ++pointer_level;
                  }

                  data.AddModifier(new PointerSpec(pointer_level));
                  break;

               case ',':

                  if (isRecurse && allowAqn) {

                     var end = pos;

                     while (end < typeName.Length && typeName[end] != ']') {
                        ++end;
                     }

                     if (end >= typeName.Length) {
                        throw new ArgumentException("Unmatched ']' while parsing generic argument assembly name");
                     }

                     data._assemblyName = typeName.Substring(pos + 1, end - pos - 1).Trim();
                     p = end;

                     return data;
                  }

                  if (isRecurse) {
                     p = pos;
                     return data;
                  }

                  if (allowAqn) {
                     data._assemblyName = typeName.Substring(pos + 1).Trim();
                     pos = typeName.Length;
                  }

                  break;

               case '[':

                  if (data._isByref) {
                     throw new ArgumentException("Byref qualifier must be the last one of a type", nameof(typeName));
                  }

                  ++pos;

                  if (pos >= typeName.Length) {
                     throw new ArgumentException("Invalid array/generic spec", nameof(typeName));
                  }

                  SkipSpace(typeName, ref pos);

                  if (typeName[pos] != ','
                     && typeName[pos] != '*'
                     && typeName[pos] != ']') { //generic args

                     var args = new List<TypeSpec>();

                     if (data.HasModifiers) {
                        throw new ArgumentException("generic args after array spec or pointer type", nameof(typeName));
                     }

                     while (pos < typeName.Length) {

                        SkipSpace(typeName, ref pos);

                        var aqn = typeName[pos] == '[';

                        if (aqn) {
                           ++pos; //skip '[' to the start of the type
                        }

                        args.Add(Parse(typeName, ref pos, true, aqn));

                        BoundCheck(pos, typeName);

                        if (aqn) {

                           if (typeName[pos] == ']') {
                              ++pos;
                           } else {
                              throw new ArgumentException("Unclosed assembly-qualified type name at " + typeName[pos], nameof(typeName));
                           }

                           BoundCheck(pos, typeName);
                        }

                        if (typeName[pos] == ']') {
                           break;
                        }

                        if (typeName[pos] == ',') {
                           ++pos; // skip ',' to the start of the next arg
                        } else {
                           throw new ArgumentException("Invalid generic arguments separator " + typeName[pos], nameof(typeName));
                        }
                     }

                     if (pos >= typeName.Length
                        || typeName[pos] != ']') {

                        throw new ArgumentException("Error parsing generic params spec", nameof(typeName));
                     }

                     data.AddGenericParams(args);

                  } else { //array spec

                     var dimensions = 1;
                     var bound = false;

                     while (pos < typeName.Length && typeName[pos] != ']') {

                        if (typeName[pos] == '*') {

                           if (bound) {
                              throw new ArgumentException("Array spec cannot have 2 bound dimensions", nameof(typeName));
                           }

                           bound = true;

                        } else if (typeName[pos] != ',') {
                           throw new ArgumentException("Invalid character in array spec " + typeName[pos], nameof(typeName));
                        } else {
                           ++dimensions;
                        }

                        ++pos;
                        SkipSpace(typeName, ref pos);
                     }

                     if (pos >= typeName.Length || typeName[pos] != ']') {
                        throw new ArgumentException("Error parsing array spec", nameof(typeName));
                     }

                     if (dimensions > 1 && bound) {
                        throw new ArgumentException("Invalid array spec, multi-dimensional array cannot be bound", nameof(typeName));
                     }

                     data.AddModifier(new ArraySpec(dimensions, bound));
                  }

                  break;

               case ']':

                  if (isRecurse) {
                     p = pos;
                     return data;
                  }

                  throw new ArgumentException("Unmatched ']'", nameof(typeName));

               default:
                  throw new ArgumentException("Bad type def, can't handle '" + typeName[pos] + "'" + " at " + pos, nameof(typeName));
            }
         }
      }

      p = pos;
      return data;
   }

   internal static string
   EscapeDisplayName(string internalName) {

      // initial capacity = length of internalName.
      // Maybe we won't have to escape anything.
      var res = new StringBuilder(internalName.Length);

      foreach (char c in internalName) {
         switch (c) {
            case '+':
            case ',':
            case '[':
            case ']':
            case '*':
            case '&':
            case '\\':
               res.Append('\\')
                  .Append(c);
               break;

            default:
               res.Append(c);
               break;
         }
      }

      return res.ToString();
   }

   internal static string
   UnescapeInternalName(string displayName) {

      var res = new StringBuilder(displayName.Length);

      for (int i = 0; i < displayName.Length; ++i) {

         var c = displayName[i];

         if (c == '\\') {
            if (++i < displayName.Length) {
               c = displayName[i];
            }
         }

         res.Append(c);
      }

      return res.ToString();
   }

   internal static bool
   NeedsEscaping(string internalName) {

      foreach (var c in internalName) {
         switch (c) {
            case ',':
            case '+':
            case '*':
            case '&':
            case '[':
            case ']':
            case '\\':
               return true;

            default:
               break;
         }
      }

      return false;
   }

   internal void
   AddName(string typeName) {

      if (_name is null) {
         _name = ParsedTypeIdentifier(typeName);

      } else {

         if (_nested is null) {
            _nested = new List<ITypeIdentifier>();
         }

         _nested.Add(ParsedTypeIdentifier(typeName));
      }
   }

   internal void
   AddGenericParams(IList<TypeSpec> args) {
      _genericParams = args;
   }

   internal void
   AddModifier(IModifierSpec md) {

      if (_modifierSpec is null) {
         _modifierSpec = new List<IModifierSpec>();
      }

      _modifierSpec.Add(md);
   }

   static void
   SkipSpace(string name, ref int pos) {

      int p = pos;

      while (p < name.Length && Char.IsWhiteSpace(name[p])) {
         ++p;
      }

      pos = p;
   }

   static void
   BoundCheck(int idx, string s) {

      if (idx >= s.Length) {
         throw new ArgumentException("Invalid generic arguments spec", "typeName");
      }
   }

   static ITypeIdentifier
   ParsedTypeIdentifier(string displayName) =>
      TypeIdentifiers.FromDisplay(displayName);

   internal ITypeName
   TypeNameWithoutModifiers() =>
      new TypeSpecTypeName(this, false);

   internal ITypeName
   TypeName() => new TypeSpecTypeName(this, true);

#if DEBUG
   public override string
   ToString() =>
      GetDisplayFullName(DisplayNameFormat.WANT_ASSEMBLY);
#endif

   class TypeSpecTypeName : TypeNames.ATypeName, ITypeName {

      readonly TypeSpec
      _ts;

      readonly bool
      _wantModifiers;

      public override string
      DisplayName => (_wantModifiers) ?
         _ts.DisplayFullName
         : _ts.GetDisplayFullName(DisplayNameFormat.NO_MODIFIERS);

      internal
      TypeSpecTypeName(TypeSpec ts, bool wantModifiers) {

         _ts = ts;
         _wantModifiers = wantModifiers;
      }

      public override ITypeName
      NestedName(ITypeIdentifier innerName) =>
         TypeNames.FromDisplay(DisplayName + "+" + innerName.DisplayName);
   }
}

[Flags]
enum DisplayNameFormat {
   Default = 0x0,
   WANT_ASSEMBLY = 0x1,
   NO_MODIFIERS = 0x2,
}

interface IModifierSpec {

   Type
   Resolve(Type type);

   StringBuilder
   Append(StringBuilder sb);
}

class ArraySpec : IModifierSpec {

   // dimensions == 1 and bound, or dimensions > 1 and !bound
   readonly int
   _dimensions;

   readonly bool
   _bound;

   public int
   Rank => _dimensions;

   public bool
   IsBound => _bound;

   internal
   ArraySpec(int dimensions, bool bound) {

      _dimensions = dimensions;
      _bound = bound;
   }

   public Type
   Resolve(Type type) {

      if (_bound) {
         return type.MakeArrayType(1);
      }

      if (_dimensions == 1) {
         return type.MakeArrayType();
      }

      return type.MakeArrayType(_dimensions);
   }

   public StringBuilder
   Append(StringBuilder sb) {

      if (_bound) {
         return sb.Append("[*]");
      }

      return sb.Append('[')
         .Append(',', _dimensions - 1)
         .Append(']');
   }

   public override string
   ToString() =>
      Append(new StringBuilder())
         .ToString();
}

class PointerSpec : IModifierSpec {

   readonly int
   _pointerLevel;

   internal
   PointerSpec(int pointerLevel) {
      _pointerLevel = pointerLevel;
   }

   public Type
   Resolve(Type type) {

      for (int i = 0; i < _pointerLevel; ++i) {
         type = type.MakePointerType();
      }

      return type;
   }

   public StringBuilder
   Append(StringBuilder sb) =>
      sb.Append('*', _pointerLevel);

   public override string
   ToString() =>
      Append(new StringBuilder())
         .ToString();
}
