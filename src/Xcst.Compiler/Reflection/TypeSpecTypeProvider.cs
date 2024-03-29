﻿// Copyright 2020 Max Toro Q.
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
using System.Reflection.Metadata.Ecma335;

namespace Xcst.Compiler.Reflection;

class TypeSpecTypeProvider
      : ISimpleTypeProvider<TypeSpec>
      , ISZArrayTypeProvider<TypeSpec>
      , ICustomAttributeTypeProvider<TypeSpec> {

   static readonly TypeSpec
   _systemType = new("System.Type", isReferenceType: true);

   static Dictionary<PrimitiveTypeCode, TypeSpec>
   _primitiveTypes = new() {
      { PrimitiveTypeCode.Void, new TypeSpec("System.Void") },
      { PrimitiveTypeCode.Boolean, new TypeSpec("System.Boolean") },
      { PrimitiveTypeCode.Char, new TypeSpec("System.Char") },
      { PrimitiveTypeCode.SByte, new TypeSpec("System.SByte") },
      { PrimitiveTypeCode.Byte, new TypeSpec("System.Byte") },
      { PrimitiveTypeCode.Int16, new TypeSpec("System.Int16") },
      { PrimitiveTypeCode.UInt16, new TypeSpec("System.UInt16") },
      { PrimitiveTypeCode.Int32, new TypeSpec("System.Int32") },
      { PrimitiveTypeCode.UInt32, new TypeSpec("System.UInt32") },
      { PrimitiveTypeCode.Int64, new TypeSpec("System.Int64") },
      { PrimitiveTypeCode.UInt64, new TypeSpec("System.UInt64") },
      { PrimitiveTypeCode.Single, new TypeSpec("System.Single") },
      { PrimitiveTypeCode.Double, new TypeSpec("System.Double") },
      { PrimitiveTypeCode.String, new TypeSpec("System.String", isReferenceType: true) },
      { PrimitiveTypeCode.TypedReference, new TypeSpec("System.TypedReference") },
      { PrimitiveTypeCode.IntPtr, new TypeSpec("System.IntPtr") },
      { PrimitiveTypeCode.UIntPtr, new TypeSpec("System.UIntPtr") },
      { PrimitiveTypeCode.Object, new TypeSpec("System.Object", isReferenceType: true) }
   };

   public TypeSpec
   GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) {

      var definition = reader.GetTypeDefinition(handle);

      var name = (definition.Namespace.IsNil) ?
         reader.GetString(definition.Name)
         : reader.GetString(definition.Namespace) + "." + reader.GetString(definition.Name);

      var refType = IsReferenceType(reader, handle, rawTypeKind);

      if (IsNested(definition.Attributes)) {

         var declaringTypeHandle = definition.GetDeclaringType();

         var parentTypeSpec = GetTypeFromDefinition(reader, declaringTypeHandle, 0);
         parentTypeSpec.AddName(name);
         parentTypeSpec.IsReferenceType = refType;

         return parentTypeSpec;
      }

      return new TypeSpec(name) {
         IsReferenceType = refType
      };
   }

   static bool
   IsNested(TypeAttributes flags) {

      const TypeAttributes nestedMask = TypeAttributes.NestedFamily | TypeAttributes.NestedPublic;

      return (flags & nestedMask) != 0;
   }

   public TypeSpec
   GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) {

      var reference = reader.GetTypeReference(handle);

      var name = (reference.Namespace.IsNil) ?
         reader.GetString(reference.Name)
         : reader.GetString(reference.Namespace) + "." + reader.GetString(reference.Name);

      var refType = IsReferenceType(reader, handle, rawTypeKind);
      var scope = reference.ResolutionScope;

      switch (scope.Kind) {
         case HandleKind.TypeReference:

            var parentTypeSpec = GetTypeFromReference(reader, (TypeReferenceHandle)scope, 0);
            parentTypeSpec.AddName(name);
            parentTypeSpec.IsReferenceType = refType;

            return parentTypeSpec;

         default:
            return new TypeSpec(name) {
               IsReferenceType = refType
            };
      }
   }

   static bool?
   IsReferenceType(MetadataReader reader, EntityHandle handle, byte rawTypeKind) =>
      reader.ResolveSignatureTypeKind(handle, rawTypeKind) switch {
         SignatureTypeKind.ValueType => false,
         SignatureTypeKind.Class => true,
         _ => null,
      };

   public TypeSpec
   GetSZArrayType(TypeSpec elementType) {

      var arrayTypeSpec = elementType.Clone();
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

      if (_primitiveTypes.TryGetValue(typeCode, out var type)) {
         return type;
      }

      throw new ArgumentOutOfRangeException(nameof(typeCode));
   }

   public PrimitiveTypeCode
   GetUnderlyingEnumType(TypeSpec type) {

      var runtimeType = Type.GetType(type.GetDisplayFullName(DisplayNameFormat.WANT_ASSEMBLY), throwOnError: false);

      if (runtimeType != null) {

         var underlyingType = runtimeType.GetEnumUnderlyingType();
         var underlyingTypeSpec = new TypeSpec(underlyingType.FullName!);

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

      var closedGeneric = genericType.Clone();
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
