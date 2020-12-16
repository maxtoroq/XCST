// Copyright 2020 Max Toro Q.
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
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;

namespace Xcst.Compiler.Reflection {

   class TypeSpecTypeProvider
         : ISimpleTypeProvider<TypeSpec>
         , ISZArrayTypeProvider<TypeSpec>
         , ICustomAttributeTypeProvider<TypeSpec> {

      static readonly TypeSpec
      _systemType = new TypeSpec("System.Type");

      static Dictionary<PrimitiveTypeCode, TypeSpec>
      _primitiveTypes = new Dictionary<PrimitiveTypeCode, TypeSpec> {
         { PrimitiveTypeCode.Void, new TypeSpec(typeof(void).FullName) },
         { PrimitiveTypeCode.Boolean, new TypeSpec(typeof(bool).FullName) },
         { PrimitiveTypeCode.Char, new TypeSpec(typeof(char).FullName) },
         { PrimitiveTypeCode.SByte, new TypeSpec(typeof(sbyte).FullName) },
         { PrimitiveTypeCode.Byte, new TypeSpec(typeof(byte).FullName) },
         { PrimitiveTypeCode.Int16, new TypeSpec(typeof(short).FullName) },
         { PrimitiveTypeCode.UInt16, new TypeSpec(typeof(ushort).FullName) },
         { PrimitiveTypeCode.Int32, new TypeSpec(typeof(int).FullName) },
         { PrimitiveTypeCode.UInt32, new TypeSpec(typeof(uint).FullName) },
         { PrimitiveTypeCode.Int64, new TypeSpec(typeof(long).FullName) },
         { PrimitiveTypeCode.UInt64, new TypeSpec(typeof(ulong).FullName) },
         { PrimitiveTypeCode.Single, new TypeSpec(typeof(float).FullName) },
         { PrimitiveTypeCode.Double, new TypeSpec(typeof(double).FullName) },
         { PrimitiveTypeCode.String, new TypeSpec(typeof(string).FullName) },
         { PrimitiveTypeCode.TypedReference, new TypeSpec(typeof(TypedReference).FullName) },
         { PrimitiveTypeCode.IntPtr, new TypeSpec(typeof(IntPtr).FullName) },
         { PrimitiveTypeCode.UIntPtr, new TypeSpec(typeof(UIntPtr).FullName) },
         { PrimitiveTypeCode.Object, new TypeSpec(typeof(object).FullName) }
      };

      public TypeSpec
      GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) {

         TypeDefinition definition = reader.GetTypeDefinition(handle);

         string name = (definition.Namespace.IsNil) ?
            reader.GetString(definition.Name)
            : reader.GetString(definition.Namespace) + "." + reader.GetString(definition.Name);

         if (IsNested(definition.Attributes)) {

            TypeDefinitionHandle declaringTypeHandle = definition.GetDeclaringType();

            TypeSpec parentTypeSpec = GetTypeFromDefinition(reader, declaringTypeHandle, 0);
            parentTypeSpec.AddName(name);

            return parentTypeSpec;
         }

         return new TypeSpec(name);
      }

      static bool
      IsNested(TypeAttributes flags) {

         const TypeAttributes nestedMask = TypeAttributes.NestedFamily | TypeAttributes.NestedPublic;

         return (flags & nestedMask) != 0;
      }

      public TypeSpec
      GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) {

         TypeReference reference = reader.GetTypeReference(handle);

         string name = (reference.Namespace.IsNil) ?
            reader.GetString(reference.Name)
            : reader.GetString(reference.Namespace) + "." + reader.GetString(reference.Name);

         Handle scope = reference.ResolutionScope;

         switch (scope.Kind) {
            case HandleKind.TypeReference:

               TypeSpec parentTypeSpec = GetTypeFromReference(reader, (TypeReferenceHandle)scope, 0);
               parentTypeSpec.AddName(name);

               return parentTypeSpec;

            default:
               return new TypeSpec(name);
         }
      }

      public TypeSpec
      GetSZArrayType(TypeSpec elementType) {

         TypeSpec arrayTypeSpec = elementType.Clone();
         arrayTypeSpec.AddModifier(new ArraySpec(1, false));

         return arrayTypeSpec;
      }

      public TypeSpec
      GetSystemType() => _systemType;

      public bool
      IsSystemType(TypeSpec type) =>
         type.Name.Equals(_systemType.Name);

      public TypeSpec
      GetTypeFromSerializedName(string name) =>
         TypeSpec.Parse(name);

      public TypeSpec
      GetPrimitiveType(PrimitiveTypeCode typeCode) {

         if (_primitiveTypes.TryGetValue(typeCode, out TypeSpec type)) {
            return type;
         }

         throw new ArgumentOutOfRangeException(nameof(typeCode));
      }

      public PrimitiveTypeCode
      GetUnderlyingEnumType(TypeSpec type) {

         Type runtimeType = Type.GetType(type.GetDisplayFullName(DisplayNameFormat.WANT_ASSEMBLY), false);

         if (runtimeType != null) {

            Type underlyingType = runtimeType.GetEnumUnderlyingType();
            TypeSpec underlyingTypeSpec = new TypeSpec(underlyingType.FullName);

            foreach (var pair in _primitiveTypes) {
               if (pair.Value.Name.Equals(underlyingTypeSpec.Name)) {
                  return pair.Key;
               }
            }
         }

         // guess
         return PrimitiveTypeCode.Int32;
      }
   }

   class TypeSpecSignatureTypeProvider : TypeSpecTypeProvider, ISignatureTypeProvider<TypeSpec, object?> {

      public TypeSpec
      GetArrayType(TypeSpec elementType, ArrayShape shape) {
         throw new NotImplementedException();
      }

      public TypeSpec
      GetByReferenceType(TypeSpec elementType) {
         throw new NotImplementedException();
      }

      public TypeSpec
      GetFunctionPointerType(MethodSignature<TypeSpec> signature) {
         throw new NotImplementedException();
      }

      public TypeSpec
      GetGenericInstantiation(TypeSpec genericType, ImmutableArray<TypeSpec> typeArguments) {

         TypeSpec closedGeneric = genericType.Clone();
         closedGeneric.AddGenericParams(typeArguments);

         return closedGeneric;
      }

      public TypeSpec
      GetGenericMethodParameter(object? genericContext, int index) {
         throw new NotImplementedException();
      }

      public TypeSpec
      GetGenericTypeParameter(object? genericContext, int index) {
         throw new NotImplementedException();
      }

      public TypeSpec
      GetModifiedType(TypeSpec modifier, TypeSpec unmodifiedType, bool isRequired) {
         throw new NotImplementedException();
      }

      public TypeSpec
      GetPinnedType(TypeSpec elementType) {
         throw new NotImplementedException();
      }

      public TypeSpec
      GetPointerType(TypeSpec elementType) {
         throw new NotImplementedException();
      }

      public TypeSpec
      GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) {
         throw new NotImplementedException();
      }
   }
}
