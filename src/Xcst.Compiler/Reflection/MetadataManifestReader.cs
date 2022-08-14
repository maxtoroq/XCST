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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml;

namespace Xcst.Compiler.Reflection;

partial class MetadataManifestReader {

   const string
   _prefix = "xcst";

   const string
   _ns = XmlNamespaces.XcstGrammar;

   const string
   _packageModelNs = "Xcst.Runtime";

   static readonly ICustomAttributeTypeProvider<TypeSpec>
   _attrTypeProvider = new TypeSpecTypeProvider();

   static readonly ISignatureTypeProvider<TypeSpec, object?>
   _signatureTypeProvider = new TypeSpecSignatureTypeProvider();

   readonly MetadataReader
   _reader;

   readonly XmlWriter
   _writer;

   public static void
   ReadAssembly(Stream assemblySource, Func<string, XmlWriter> writerFn) {

      using var peReader = new PEReader(assemblySource);

      var reader = peReader.GetMetadataReader();

      foreach (var typeDef in reader.TypeDefinitions.Select(reader.GetTypeDefinition)) {

         if (!typeDef.Attributes.HasFlag(TypeAttributes.Public)) {
            continue;
         }

         if (typeDef.Attributes.HasFlag(TypeAttributes.ClassSemanticsMask)) {
            // is interface
            continue;
         }

         var isPkg =
            (from ih in typeDef.GetInterfaceImplementations()
             let i = reader.GetInterfaceImplementation(ih)
             where i.Interface.Kind == HandleKind.TypeReference
             let t = reader.GetTypeReference(((TypeReferenceHandle)i.Interface))
             select t).Any(t => reader.GetString(t.Name) == "IXcstPackage"
                && reader.GetString(t.Namespace) == "Xcst");

         if (isPkg) {

            var pkgName = reader.GetString(typeDef.Name);

            if (!typeDef.Namespace.IsNil) {
               pkgName = reader.GetString(typeDef.Namespace) + "." + pkgName;
            }

            using var writer = writerFn(pkgName);

            new MetadataManifestReader(reader, writer)
               .WritePackage(typeDef);
         }
      }
   }

   public
   MetadataManifestReader(MetadataReader reader, XmlWriter writer) {
      _reader = reader;
      _writer = writer;
   }

   void
   WritePackage(TypeDefinition typeDef) {

      _writer.WriteStartElement(_prefix, "package-manifest", _ns);
      _writer.WriteAttributeString("qualified-types", "true");
      _writer.WriteAttributeString("xmlns", "code", null, XmlNamespaces.XcstCode);

      WriteTypeReference(typeDef);

      var nullableContext = NullableContextAttribute(typeDef.GetCustomAttributes());

      foreach (var methodDef in typeDef.GetMethods().Select(_reader.GetMethodDefinition)) {

         if (FindComponentAttribute(methodDef.GetCustomAttributes())
            is ComponentAttributeData componentData) {

            switch (componentData.Kind) {
               case 1:
                  WriteTemplate(methodDef, componentData);
                  break;

               case 2:
                  WriteAttributeSet(methodDef, componentData);
                  break;

               case 3:
                  WriteFunction(methodDef, nullableContext);
                  break;
            }
         }
      }

      foreach (var propDef in typeDef.GetProperties().Select(_reader.GetPropertyDefinition)) {

         if (FindComponentAttribute(propDef.GetCustomAttributes())
            is ComponentAttributeData componentData) {

            switch (componentData.Kind) {
               case 4:
               case 5:
                  var getterDef = _reader.GetMethodDefinition(propDef.GetAccessors().Getter);
                  WriteVariable(propDef, getterDef, componentData, nullableContext);
                  break;
            }
         }
      }

      foreach (var nestedTypeDef in typeDef.GetNestedTypes().Select(_reader.GetTypeDefinition)) {

         if (FindComponentAttribute(nestedTypeDef.GetCustomAttributes())
            is ComponentAttributeData componentData) {

            switch (componentData.Kind) {
               case 6:
                  WriteType(nestedTypeDef);
                  break;
            }
         }
      }

      _writer.WriteEndElement();
   }

   void
   WriteTemplate(MethodDefinition methodDef, ComponentAttributeData componentData) {

      _writer.WriteStartElement(_prefix, "template", _ns);
      _writer.WriteAttributeString("name", componentData.Name);
      _writer.WriteAttributeString("visibility", ComponentVisibility(methodDef));
      _writer.WriteAttributeString("member-name", _reader.GetString(methodDef.Name));
      _writer.WriteAttributeString("cardinality", SequenceCardinality(componentData.Cardinality));

      var signature = methodDef.DecodeSignature(_signatureTypeProvider, null);
      var contextType = signature.ParameterTypes[0];
      var packageType = _reader.GetTypeDefinition(methodDef.GetDeclaringType());

      var paramsType = (contextType.HasGenericParameters) ?
         contextType.GenericParameters[0]
         : null;

      if (paramsType != null) {

         var paramsTypeDef =
            (from t in _reader.TypeDefinitions.Select(_reader.GetTypeDefinition)
             where t.IsNested
                && _reader.GetString(t.Name) == paramsType.Nested.Last().DisplayName
             let pt = _reader.GetTypeDefinition(t.GetDeclaringType())
             where pt.Name == packageType.Name && pt.Namespace == packageType.Namespace
             select t)
            .First();

         foreach (var propDef in paramsTypeDef.GetProperties().Select(_reader.GetPropertyDefinition)) {
            WriteTemplateParameter(propDef);
         }
      }

      var outputType = signature.ParameterTypes[1];
      TypeSpec itemType;

      if (outputType.HasGenericParameters
         && (itemType = outputType.GenericParameters[0]).Name.DisplayName != "System.Object") {

         _writer.WriteStartElement(_prefix, "item-type", _ns);
         WriteTypeReference(itemType);
         _writer.WriteEndElement();
      }

      _writer.WriteEndElement();
   }

   void
   WriteTemplateParameter(PropertyDefinition propDef) {

      var name = _reader.GetString(propDef.Name);

      var required = propDef.GetCustomAttributes()
         .Select(_reader.GetCustomAttribute)
         .Any(c => CustomAttributeName(c) == $"{_packageModelNs}.RequiredAttribute");

      _writer.WriteStartElement(_prefix, "param", _ns);
      _writer.WriteAttributeString("name", name);
      _writer.WriteAttributeString("required", XmlConvert.ToString(required));
      _writer.WriteAttributeString("tunnel", "false");

      var propSign = propDef.DecodeSignature(_signatureTypeProvider, null);

      WriteTypeReference(propSign.ReturnType);
      _writer.WriteEndElement();
   }

   void
   WriteAttributeSet(MethodDefinition methodDef, ComponentAttributeData componentData) {

      _writer.WriteStartElement(_prefix, "attribute-set", _ns);
      _writer.WriteAttributeString("name", componentData.Name);
      _writer.WriteAttributeString("visibility", ComponentVisibility(methodDef));
      _writer.WriteAttributeString("member-name", _reader.GetString(methodDef.Name));
      _writer.WriteEndElement();
   }

   void
   WriteFunction(MethodDefinition methodDef, byte? nullableContext) {

      var memberName = _reader.GetString(methodDef.Name);

      _writer.WriteStartElement(_prefix, "function", _ns);

      _writer.WriteAttributeString("name", memberName);
      _writer.WriteAttributeString("visibility", ComponentVisibility(methodDef));
      _writer.WriteAttributeString("member-name", memberName);

      nullableContext = NullableContextAttribute(methodDef.GetCustomAttributes()) ?? nullableContext;

      var signature = methodDef.DecodeSignature(_signatureTypeProvider, null);

      Parameter? returnParam = null;

      var parameters = methodDef.GetParameters()
         .Select(_reader.GetParameter)
         // "empty" parameter has return value metadata
         .Where(p => String.IsNullOrEmpty(_reader.GetString(p.Name)) ?
            (returnParam = p) is null
            : true)
         .ToArray();

      if (signature.ReturnType.Name.DisplayName != "System.Void") {

         WriteTypeReference(
            signature.ReturnType,
            nullableContext,
            (returnParam != null ? NullableAttribute(returnParam.Value.GetCustomAttributes()) : null)
         );
      }

      Debug.Assert(parameters.Length == signature.ParameterTypes.Length);

      for (int i = 0; i < parameters.Length; i++) {

         var param = parameters[i];
         WriteFunctionParameter(param, i, signature, nullableContext);
      }

      _writer.WriteEndElement();
   }

   void
   WriteFunctionParameter(Parameter param, int i, MethodSignature<TypeSpec> signature, byte? nullableContext) {

      var paramType = signature.ParameterTypes[i];

      _writer.WriteStartElement(_prefix, "param", _ns);
      _writer.WriteAttributeString("name", _reader.GetString(param.Name));

      var attribsH = param.GetCustomAttributes();

      WriteTypeReference(paramType, nullableContext, NullableAttribute(attribsH));

      if (param.Attributes.HasFlag(ParameterAttributes.HasDefault)) {

         var defaultVal = _reader.GetConstant(param.GetDefaultValue());
         WriteConstant(defaultVal);

      } else if (param.Attributes.HasFlag(ParameterAttributes.Optional)) {

         var defaultVal = attribsH
            .Select(_reader.GetCustomAttribute)
            .Where(p => CustomAttributeName(p) == "System.Runtime.CompilerServices.DecimalConstantAttribute")
            .Select(p => TryDecodeDecimalConstantAttribute(p))
            .FirstOrDefault(p => p != null);

         if (defaultVal != null) {

            _writer.WriteStartElement(_prefix, "decimal", _ns);
            _writer.WriteAttributeString("value", defaultVal.Value.ToString(CultureInfo.InvariantCulture));
            _writer.WriteEndElement();
         }
      }

      _writer.WriteEndElement();
   }

   void
   WriteVariable(PropertyDefinition propDef, MethodDefinition getterDef, ComponentAttributeData componentData, byte? nullableContext) {

      var isParam = componentData.Kind == 5;
      var memberName = _reader.GetString(propDef.Name);

      _writer.WriteStartElement(_prefix, (isParam) ? "param" : "variable", _ns);
      _writer.WriteAttributeString("name", memberName);

      if (isParam) {

         var required = propDef.GetCustomAttributes()
            .Select(_reader.GetCustomAttribute)
            .Any(c => CustomAttributeName(c) == $"{_packageModelNs}.RequiredAttribute");

         _writer.WriteAttributeString("required", XmlConvert.ToString(required));
      }

      _writer.WriteAttributeString("visibility", ComponentVisibility(getterDef));
      _writer.WriteAttributeString("member-name", memberName);

      var signature = propDef.DecodeSignature(_signatureTypeProvider, null);

      WriteTypeReference(
         signature.ReturnType,
         NullableContextAttribute(propDef.GetCustomAttributes()) ?? nullableContext,
         NullableAttribute(propDef.GetCustomAttributes())
      );

      _writer.WriteEndElement();
   }

   void
   WriteType(TypeDefinition typeDef) {

      _writer.WriteStartElement(_prefix, "type", _ns);
      _writer.WriteAttributeString("name", _reader.GetString(typeDef.Name));

      _writer.WriteAttributeString("visibility",
         typeDef.Attributes.HasFlag(TypeAttributes.Abstract) ? "abstract"
         : typeDef.Attributes.HasFlag(TypeAttributes.Sealed) ? "final"
         : "public");

      _writer.WriteEndElement();
   }

   void
   WriteTypeReference(TypeDefinition typeDef) {

      const string ns = XmlNamespaces.XcstCode;
      const string prefix = "code";

      _writer.WriteStartElement(prefix, "type-reference", ns);

      var name = _reader.GetString(typeDef.Name);

      _writer.WriteAttributeString("name", name);

      if (typeDef.Attributes.HasFlag(TypeAttributes.ClassSemanticsMask)) {
         _writer.WriteAttributeString("interface", "true");
      }

      if (typeDef.IsNested) {
         WriteTypeReference(_reader.GetTypeDefinition(typeDef.GetDeclaringType()));
      } else {
         _writer.WriteAttributeString("namespace", _reader.GetString(typeDef.Namespace));
      }

      _writer.WriteEndElement();
   }

   void
   WriteTypeReference(TypeSpec type, byte? nullableContext = null, byte[]? nullableAttr = null, int flagOffset = 0) {

      const string ns = XmlNamespaces.XcstCode;
      const string prefix = "code";

      if (type.HasModifiers) {

         foreach (var mod in type.Modifiers.Reverse()) {

            if (mod is ArraySpec array) {
               _writer.WriteStartElement(prefix, "type-reference", ns);
               _writer.WriteAttributeString("array-dimensions", XmlConvert.ToString(array.Rank));
               WriteNullable(nullableContext, nullableAttr, flagOffset);
               flagOffset++;
            } else {
               throw new NotImplementedException();
            }
         }
      }

      if (type.IsNested) {

         var nestedWritten = false;

         foreach (var nest in type.Nested.Reverse()) {

            _writer.WriteStartElement(prefix, "type-reference", ns);
            _writer.WriteAttributeString("name", nest.DisplayName);

            if (!nestedWritten) {

               nestedWritten = true;

               if (type.IsReferenceType == true) {
                  WriteNullable(nullableContext, nullableAttr, flagOffset);
               }

               if (type.HasGenericParameters) {

                  _writer.WriteStartElement(prefix, "type-arguments", ns);

                  for (int i = 0; i < type.GenericParameters.Count; i++) {
                     var gp = type.GenericParameters[i];
                     WriteTypeReference(gp, nullableContext, nullableAttr, flagOffset + i + 1);
                  }

                  _writer.WriteEndElement();
               }
            }
         }
      }

      var fullName = type.Name.DisplayName;

      if (type.HasGenericParameters) {
         fullName = fullName.Substring(0, fullName.IndexOf('`'));
      }

      var nspace = (fullName.Contains(".")) ?
         fullName.Substring(0, fullName.LastIndexOf('.'))
         : "";

      var name = (nspace.Length > 0) ?
         fullName.Substring(fullName.LastIndexOf('.') + 1)
         : fullName;

      _writer.WriteStartElement(prefix, "type-reference", ns);
      _writer.WriteAttributeString("name", name);
      _writer.WriteAttributeString("namespace", nspace);

      if (!type.IsNested) {

         if (type.IsReferenceType == true) {
            WriteNullable(nullableContext, nullableAttr, flagOffset);
         }

         if (type.HasGenericParameters) {

            _writer.WriteStartElement(prefix, "type-arguments", ns);

            for (int i = 0; i < type.GenericParameters.Count; i++) {
               var gp = type.GenericParameters[i];
               WriteTypeReference(gp, nullableContext, nullableAttr, flagOffset + i + 1);
            }

            _writer.WriteEndElement();
         }
      }

      _writer.WriteEndElement(); // </type-reference>

      if (type.IsNested) {

         foreach (var _ in type.Nested) {
            _writer.WriteEndElement(); // </type-reference>
         }
      }

      if (type.HasModifiers) {

         foreach (var mod in type.Modifiers) {

            if (mod is ArraySpec) {
               _writer.WriteEndElement(); // </type-reference>
            }
         }
      }
   }

   void
   WriteNullable(byte? nullableContext, byte[]? nullableAttr, int flagOffset) {

      if ((nullableAttr?[flagOffset] ?? nullableContext) == 2) {
         _writer.WriteAttributeString("nullable", "true");
      }
   }

   byte[]?
   NullableAttribute(CustomAttributeHandleCollection attrData) {

      var nullableAttr = attrData
         .Select(c => (CustomAttribute?)_reader.GetCustomAttribute(c))
         .FirstOrDefault(c => CustomAttributeName(c!.Value) == "System.Runtime.CompilerServices.NullableAttribute");

      if (nullableAttr != null) {

         var value = nullableAttr.Value.DecodeValue(_attrTypeProvider);

         if (value.FixedArguments.Length == 1) {

            switch (value.FixedArguments[0].Value) {
               case byte flag:
                  return new byte[] { flag };

               case IList<CustomAttributeTypedArgument<TypeSpec>> flags:
                  return flags
                     .Select(p => (byte)p.Value)
                     .ToArray();
            }
         }
      }

      return null;
   }

   byte?
   NullableContextAttribute(CustomAttributeHandleCollection attrData) {

      var nullableAttr = attrData
         .Select(c => (CustomAttribute?)_reader.GetCustomAttribute(c))
         .FirstOrDefault(c => CustomAttributeName(c!.Value) == "System.Runtime.CompilerServices.NullableContextAttribute");

      if (nullableAttr != null) {

         var value = nullableAttr.Value.DecodeValue(_attrTypeProvider);

         if (value.FixedArguments.Length == 1
            && value.FixedArguments[0].Value is byte flag) {

            return flag;
         }
      }

      return null;
   }

   void
   WriteConstant(Constant constant) {

      const string ns = XmlNamespaces.XcstCode;
      const string prefix = "code";

      BlobReader blobReader = _reader.GetBlobReader(constant.Value);

      var str = Convert.ToString(
         blobReader.ReadConstant(constant.TypeCode),
         CultureInfo.InvariantCulture
      )!;

      switch (constant.TypeCode) {
         case ConstantTypeCode.NullReference:
            _writer.WriteStartElement(prefix, "null", ns);
            _writer.WriteEndElement();
            return;

         case ConstantTypeCode.String:
            _writer.WriteStartElement(prefix, "string", ns);
            _writer.WriteAttributeString("verbatim", "true");
            _writer.WriteString(str);
            _writer.WriteEndElement();
            return;

         case ConstantTypeCode.Boolean:
            _writer.WriteStartElement(prefix, "bool", ns);
            _writer.WriteAttributeString("value", str.ToLowerInvariant());
            _writer.WriteEndElement();
            return;

         case ConstantTypeCode.Char:
            _writer.WriteStartElement(prefix, "char", ns);
            _writer.WriteAttributeString("value", str);
            _writer.WriteEndElement();
            return;

         case ConstantTypeCode.Int32:
            _writer.WriteStartElement(prefix, "int", ns);
            _writer.WriteAttributeString("value", str);
            _writer.WriteEndElement();
            return;

         case ConstantTypeCode.UInt32:
            _writer.WriteStartElement(prefix, "uint", ns);
            _writer.WriteAttributeString("value", str);
            _writer.WriteEndElement();
            return;

         case ConstantTypeCode.Int64:
            _writer.WriteStartElement(prefix, "long", ns);
            _writer.WriteAttributeString("value", str);
            _writer.WriteEndElement();
            return;

         case ConstantTypeCode.UInt64:
            _writer.WriteStartElement(prefix, "ulong", ns);
            _writer.WriteAttributeString("value", str);
            _writer.WriteEndElement();
            return;

         case ConstantTypeCode.Double:
            _writer.WriteStartElement(prefix, "double", ns);
            _writer.WriteAttributeString("value", str);
            _writer.WriteEndElement();
            return;

         case ConstantTypeCode.Single:
            _writer.WriteStartElement(prefix, "float", ns);
            _writer.WriteAttributeString("value", str);
            _writer.WriteEndElement();
            return;

         default:
            _writer.WriteStartElement(prefix, "expression", ns);
            _writer.WriteAttributeString("value", str);
            _writer.WriteEndElement();
            return;
      }
   }

   ComponentAttributeData?
   FindComponentAttribute(CustomAttributeHandleCollection attributeHandles) {

      var componentAttrName = $"{_packageModelNs}.XcstComponentAttribute";

      return attributeHandles
         .Select(_reader.GetCustomAttribute)
         .Where(c => CustomAttributeName(c) == componentAttrName)
         .Select(c => ParseComponentAttribute(c))
         .FirstOrDefault();
   }

   static ComponentAttributeData
   ParseComponentAttribute(CustomAttribute attrib) {

      var atValue = attrib.DecodeValue(_attrTypeProvider);
      var kind = (byte)atValue.FixedArguments[0].Value!;

      var name = atValue.NamedArguments
         .Where(n => n.Name == "Name")
         .Select(n => (string?)n.Value)
         .FirstOrDefault();

      var cardinality = atValue.NamedArguments
         .Where(n => n.Name == "Cardinality")
         .Select(n => (char)n.Value!)
         .FirstOrDefault();

      return new ComponentAttributeData {
         Kind = kind,
         Name = name,
         Cardinality = cardinality
      };
   }

   static string
   ComponentVisibility(MethodDefinition definition) =>
      definition.Attributes.HasFlag(MethodAttributes.Abstract) ? "abstract"
      : definition.Attributes.HasFlag(MethodAttributes.Virtual) ? "public"
      : "final";

   static string
   SequenceCardinality(char c) =>
      (c == ' ') ? "One"
      : "ZeroOrMore";

   string
   CustomAttributeName(CustomAttribute attrib) {

      StringHandle nsHandl;
      StringHandle nameHandl;

      if (attrib.Constructor.Kind == HandleKind.MemberReference) {

         var ctor = _reader.GetMemberReference((MemberReferenceHandle)attrib.Constructor);
         var typeRef = _reader.GetTypeReference((TypeReferenceHandle)ctor.Parent);

         nsHandl = typeRef.Namespace;
         nameHandl = typeRef.Name;

      } else if (attrib.Constructor.Kind == HandleKind.MethodDefinition) {

         var ctor = _reader.GetMethodDefinition((MethodDefinitionHandle)attrib.Constructor);
         var typeDef = _reader.GetTypeDefinition(ctor.GetDeclaringType());

         nsHandl = typeDef.Namespace;
         nameHandl = typeDef.Name;

      } else {
         throw new NotImplementedException();
      }

      var fullName = _reader.GetString(nameHandl);

      if (!nsHandl.IsNil) {
         fullName = _reader.GetString(nsHandl) + "." + fullName;
      }

      return fullName;
   }

   class ComponentAttributeData {

      public byte
      Kind { get; set; }

      public string?
      Name { get; set; }

      public char
      Cardinality { get; set; }
   }
}
