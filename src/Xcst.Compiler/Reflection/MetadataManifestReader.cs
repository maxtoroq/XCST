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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml;

namespace Xcst.Compiler.Reflection {

   partial class MetadataManifestReader {

      const string
      _prefix = "xcst";

      const string
      _ns = XmlNamespaces.XcstGrammar;

      const string
      _packageModelNs = "Xcst.PackageModel";

      static readonly ICustomAttributeTypeProvider<TypeSpec>
      _attrTypeProvider = new TypeSpecTypeProvider();

      static readonly ISignatureTypeProvider<TypeSpec, object?>
      _signatureTypeProvider = new TypeSpecSignatureTypeProvider();

      public static void
      ReadAssembly(Stream assemblySource, Func<string, XmlWriter> writerFn) {

         using (var peReader = new PEReader(assemblySource)) {

            MetadataReader reader = peReader.GetMetadataReader();

            foreach (var typeDef in reader.TypeDefinitions.Select(reader.GetTypeDefinition)) {

               if (!typeDef.Attributes.HasFlag(TypeAttributes.Public)) {
                  continue;
               }

               if (typeDef.Attributes.HasFlag(TypeAttributes.ClassSemanticsMask)) {
                  // is interface
                  continue;
               }

               bool isPkg =
                  (from ih in typeDef.GetInterfaceImplementations()
                   let i = reader.GetInterfaceImplementation(ih)
                   where i.Interface.Kind == HandleKind.TypeReference
                   let t = reader.GetTypeReference(((TypeReferenceHandle)i.Interface))
                   select t).Any(t => reader.GetString(t.Name) == "IXcstPackage"
                      && reader.GetString(t.Namespace) == _packageModelNs);

               if (isPkg) {

                  string pkgName = reader.GetString(typeDef.Name);

                  if (!typeDef.Namespace.IsNil) {
                     pkgName = reader.GetString(typeDef.Namespace) + "." + pkgName;
                  }

                  using (XmlWriter writer = writerFn(pkgName)) {
                     WritePackage(typeDef, reader, writer);
                  }
               }
            }
         }
      }

      static void
      WritePackage(TypeDefinition typeDef, MetadataReader reader, XmlWriter writer) {

         writer.WriteStartElement(_prefix, "package-manifest", _ns);
         writer.WriteAttributeString("qualified-types", "true");
         writer.WriteAttributeString("xmlns", "code", null, XmlNamespaces.XcstCode);

         WriteTypeReference(typeDef, reader, writer);

         foreach (var methodDef in typeDef.GetMethods().Select(reader.GetMethodDefinition)) {

            if (FindComponentAttribute(methodDef.GetCustomAttributes(), reader)
               is ComponentAttributeData componentData) {

               switch (componentData.Kind) {
                  case 1:
                     WriteTemplate(methodDef, componentData, reader, writer);
                     break;

                  case 2:
                     WriteAttributeSet(methodDef, componentData, reader, writer);
                     break;

                  case 3:
                     WriteFunction(methodDef, reader, writer);
                     break;
               }
            }
         }

         foreach (var propDef in typeDef.GetProperties().Select(reader.GetPropertyDefinition)) {

            if (FindComponentAttribute(propDef.GetCustomAttributes(), reader)
               is ComponentAttributeData componentData) {

               switch (componentData.Kind) {
                  case 4:
                  case 5:
                     MethodDefinition getterDef = reader.GetMethodDefinition(propDef.GetAccessors().Getter);
                     WriteVariable(propDef, getterDef, componentData, reader, writer);
                     break;
               }
            }
         }

         foreach (var nestedTypeDef in typeDef.GetNestedTypes().Select(reader.GetTypeDefinition)) {

            if (FindComponentAttribute(nestedTypeDef.GetCustomAttributes(), reader)
               is ComponentAttributeData componentData) {

               switch (componentData.Kind) {
                  case 6:
                     WriteType(nestedTypeDef, reader, writer);
                     break;
               }
            }
         }

         writer.WriteEndElement();
      }

      static void
      WriteTemplate(
            MethodDefinition methodDef, ComponentAttributeData componentData, MetadataReader reader, XmlWriter writer) {

         writer.WriteStartElement(_prefix, "template", _ns);
         writer.WriteAttributeString("name", componentData.Name);
         writer.WriteAttributeString("visibility", ComponentVisibility(methodDef));
         writer.WriteAttributeString("member-name", reader.GetString(methodDef.Name));
         writer.WriteAttributeString("cardinality", SequenceCardinality(componentData.Cardinality));

         MethodSignature<TypeSpec> signature = methodDef.DecodeSignature(_signatureTypeProvider, null);

         TypeSpec contextType = signature.ParameterTypes[0];
         TypeDefinition packageType = reader.GetTypeDefinition(methodDef.GetDeclaringType());

         TypeSpec? paramsType = (contextType.HasGenericParameters) ?
            contextType.GenericParameters[0]
            : null;

         if (paramsType != null) {

            TypeDefinition paramsTypeDef =
               (from t in reader.TypeDefinitions.Select(reader.GetTypeDefinition)
                where t.IsNested
                   && reader.GetString(t.Name) == paramsType.Nested.Last().DisplayName
                let pt = reader.GetTypeDefinition(t.GetDeclaringType())
                where pt.Name == packageType.Name && pt.Namespace == packageType.Namespace
                select t)
               .First();

            foreach (var propDef in paramsTypeDef.GetProperties().Select(reader.GetPropertyDefinition)) {
               WriteTemplateParameter(propDef, reader, writer);
            }
         }

         TypeSpec outputType = signature.ParameterTypes[1];
         TypeSpec itemType;

         if (outputType.HasGenericParameters
            && (itemType = outputType.GenericParameters[0]).Name.DisplayName != "System.Object") {

            writer.WriteStartElement(_prefix, "item-type", _ns);
            WriteTypeReference(itemType, writer);
            writer.WriteEndElement();
         }

         writer.WriteEndElement();
      }

      static void
      WriteTemplateParameter(PropertyDefinition propDef, MetadataReader reader, XmlWriter writer) {

         string name = reader.GetString(propDef.Name);

         bool required = propDef.GetCustomAttributes()
            .Select(reader.GetCustomAttribute)
            .Any(c => CustomAttributeName(c, reader) == $"{_packageModelNs}.RequiredAttribute");

         writer.WriteStartElement(_prefix, "param", _ns);
         writer.WriteAttributeString("name", name);
         writer.WriteAttributeString("required", XmlConvert.ToString(required));
         writer.WriteAttributeString("tunnel", "false");

         MethodSignature<TypeSpec> propSign = propDef.DecodeSignature(_signatureTypeProvider, null);

         WriteTypeReference(propSign.ReturnType, writer);
         writer.WriteEndElement();
      }

      static void
      WriteAttributeSet(
            MethodDefinition methodDef, ComponentAttributeData componentData, MetadataReader reader, XmlWriter writer) {

         writer.WriteStartElement(_prefix, "attribute-set", _ns);
         writer.WriteAttributeString("name", componentData.Name);
         writer.WriteAttributeString("visibility", ComponentVisibility(methodDef));
         writer.WriteAttributeString("member-name", reader.GetString(methodDef.Name));
         writer.WriteEndElement();
      }

      static void
      WriteFunction(MethodDefinition methodDef, MetadataReader reader, XmlWriter writer) {

         string memberName = reader.GetString(methodDef.Name);

         writer.WriteStartElement(_prefix, "function", _ns);

         writer.WriteAttributeString("name", memberName);
         writer.WriteAttributeString("visibility", ComponentVisibility(methodDef));
         writer.WriteAttributeString("member-name", memberName);

         MethodSignature<TypeSpec> signature = methodDef.DecodeSignature(_signatureTypeProvider, null);

         if (signature.ReturnType.Name.DisplayName != "System.Void") {
            WriteTypeReference(signature.ReturnType, writer);
         }

         Parameter[] parameters = methodDef.GetParameters()
            .Select(reader.GetParameter)
            // strangely, GetParameters may return an extra "empty" parameter
            .Where(p => !String.IsNullOrEmpty(reader.GetString(p.Name)))
            .ToArray();

         Debug.Assert(parameters.Length == signature.ParameterTypes.Length);

         for (int i = 0; i < parameters.Length; i++) {

            Parameter param = parameters[i];
            WriteFunctionParameter(param, i, signature, reader, writer);
         }

         writer.WriteEndElement();
      }

      static void
      WriteFunctionParameter(Parameter param, int i, MethodSignature<TypeSpec> signature, MetadataReader reader, XmlWriter writer) {

         TypeSpec paramType = signature.ParameterTypes[i];

         writer.WriteStartElement(_prefix, "param", _ns);
         writer.WriteAttributeString("name", reader.GetString(param.Name));

         WriteTypeReference(paramType, writer);

         if (param.Attributes.HasFlag(ParameterAttributes.HasDefault)) {

            Constant defaultVal = reader.GetConstant(param.GetDefaultValue());
            WriteConstant(defaultVal, reader, writer);

         } else if (param.Attributes.HasFlag(ParameterAttributes.Optional)) {

            CustomAttribute[] paramAttribs = param.GetCustomAttributes()
               .Select(reader.GetCustomAttribute)
               .ToArray();

            foreach (var paramAttrib in paramAttribs) {

               string atName = CustomAttributeName(paramAttrib, reader);

               if (atName == "System.Runtime.CompilerServices.DecimalConstantAttribute") {

                  decimal? defaultVal = TryDecodeDecimalConstantAttribute(paramAttrib);

                  if (defaultVal != null) {

                     writer.WriteStartElement(_prefix, "decimal", _ns);
                     writer.WriteAttributeString("value", defaultVal.Value.ToString(CultureInfo.InvariantCulture));
                     writer.WriteEndElement();
                  }

                  break;
               }
            }
         }

         writer.WriteEndElement();
      }

      static void
      WriteVariable(
            PropertyDefinition propDef, MethodDefinition getterDef, ComponentAttributeData componentData,
            MetadataReader reader, XmlWriter writer) {

         bool isParam = componentData.Kind == 5;
         string memberName = reader.GetString(propDef.Name);

         writer.WriteStartElement(_prefix, (isParam) ? "param" : "variable", _ns);
         writer.WriteAttributeString("name", memberName);

         if (isParam) {

            bool required = propDef.GetCustomAttributes()
               .Select(reader.GetCustomAttribute)
               .Any(c => CustomAttributeName(c, reader) == $"{_packageModelNs}.RequiredAttribute");

            writer.WriteAttributeString("required", XmlConvert.ToString(required));
         }

         writer.WriteAttributeString("visibility", ComponentVisibility(getterDef));
         writer.WriteAttributeString("member-name", memberName);

         MethodSignature<TypeSpec> signature = propDef.DecodeSignature(_signatureTypeProvider, null);

         WriteTypeReference(signature.ReturnType, writer);

         writer.WriteEndElement();
      }

      static void
      WriteType(TypeDefinition typeDef, MetadataReader reader, XmlWriter writer) {

         writer.WriteStartElement(_prefix, "type", _ns);
         writer.WriteAttributeString("name", reader.GetString(typeDef.Name));

         writer.WriteAttributeString("visibility",
            typeDef.Attributes.HasFlag(TypeAttributes.Abstract) ? "abstract"
            : typeDef.Attributes.HasFlag(TypeAttributes.Sealed) ? "final"
            : "public");

         writer.WriteEndElement();
      }

      static void
      WriteTypeReference(TypeDefinition typeDef, MetadataReader reader, XmlWriter writer) {

         const string ns = XmlNamespaces.XcstCode;
         const string prefix = "code";

         writer.WriteStartElement(prefix, "type-reference", ns);

         string name = reader.GetString(typeDef.Name);

         writer.WriteAttributeString("name", name);

         if (typeDef.Attributes.HasFlag(TypeAttributes.ClassSemanticsMask)) {
            writer.WriteAttributeString("interface", "true");
         }

         if (typeDef.IsNested) {
            WriteTypeReference(reader.GetTypeDefinition(typeDef.GetDeclaringType()), reader, writer);
         } else {
            writer.WriteAttributeString("namespace", reader.GetString(typeDef.Namespace));
         }

         writer.WriteEndElement();
      }

      static void
      WriteTypeReference(TypeSpec type, XmlWriter writer) {

         const string ns = XmlNamespaces.XcstCode;
         const string prefix = "code";

         if (type.HasModifiers) {

            foreach (var mod in type.Modifiers.Reverse()) {

               if (mod is ArraySpec array) {
                  writer.WriteStartElement(prefix, "type-reference", ns);
                  writer.WriteAttributeString("array-dimensions", XmlConvert.ToString(array.Rank));
               } else {
                  throw new NotImplementedException();
               }
            }
         }

         if (type.IsNested) {

            bool nestedWritten = false;

            foreach (var nest in type.Nested.Reverse()) {

               writer.WriteStartElement(prefix, "type-reference", ns);
               writer.WriteAttributeString("name", nest.DisplayName);

               if (!nestedWritten) {

                  nestedWritten = true;

                  if (type.HasGenericParameters) {

                     writer.WriteStartElement(prefix, "type-arguments", ns);

                     foreach (var gp in type.GenericParameters) {
                        WriteTypeReference(gp, writer);
                     }

                     writer.WriteEndElement();
                  }
               }
            }
         }

         string fullName = type.Name.DisplayName;

         if (type.HasGenericParameters) {
            fullName = fullName.Substring(0, fullName.IndexOf('`'));
         }

         string nspace = (fullName.Contains(".")) ?
            fullName.Substring(0, fullName.LastIndexOf('.'))
            : "";

         string name = (nspace.Length > 0) ?
            fullName.Substring(fullName.LastIndexOf('.') + 1)
            : fullName;

         writer.WriteStartElement(prefix, "type-reference", ns);
         writer.WriteAttributeString("name", name);
         writer.WriteAttributeString("namespace", nspace);

         if (!type.IsNested
            && type.HasGenericParameters) {

            writer.WriteStartElement(prefix, "type-arguments", ns);

            foreach (var gp in type.GenericParameters) {
               WriteTypeReference(gp, writer);
            }

            writer.WriteEndElement();
         }

         writer.WriteEndElement(); // </type-reference>

         if (type.IsNested) {

            foreach (var nest in type.Nested) {
               writer.WriteEndElement(); // </type-reference>
            }
         }

         if (type.HasModifiers) {

            foreach (var mod in type.Modifiers) {

               if (mod is ArraySpec array) {
                  writer.WriteEndElement(); // </type-reference>
               }
            }
         }
      }

      static void
      WriteConstant(Constant constant, MetadataReader reader, XmlWriter writer) {

         const string ns = XmlNamespaces.XcstCode;
         const string prefix = "code";

         BlobReader blobReader = reader.GetBlobReader(constant.Value);

         string str = Convert.ToString(
            blobReader.ReadConstant(constant.TypeCode),
            CultureInfo.InvariantCulture
         )!;

         switch (constant.TypeCode) {
            case ConstantTypeCode.NullReference:
               writer.WriteStartElement(prefix, "null", ns);
               writer.WriteEndElement();
               return;

            case ConstantTypeCode.String:
               writer.WriteStartElement(prefix, "string", ns);
               writer.WriteAttributeString("verbatim", "true");
               writer.WriteString(str);
               writer.WriteEndElement();
               return;

            case ConstantTypeCode.Boolean:
               writer.WriteStartElement(prefix, "bool", ns);
               writer.WriteAttributeString("value", str.ToLowerInvariant());
               writer.WriteEndElement();
               return;

            case ConstantTypeCode.Char:
               writer.WriteStartElement(prefix, "char", ns);
               writer.WriteAttributeString("value", str);
               writer.WriteEndElement();
               return;

            case ConstantTypeCode.Int32:
               writer.WriteStartElement(prefix, "int", ns);
               writer.WriteAttributeString("value", str);
               writer.WriteEndElement();
               return;

            case ConstantTypeCode.UInt32:
               writer.WriteStartElement(prefix, "uint", ns);
               writer.WriteAttributeString("value", str);
               writer.WriteEndElement();
               return;

            case ConstantTypeCode.Int64:
               writer.WriteStartElement(prefix, "long", ns);
               writer.WriteAttributeString("value", str);
               writer.WriteEndElement();
               return;

            case ConstantTypeCode.UInt64:
               writer.WriteStartElement(prefix, "ulong", ns);
               writer.WriteAttributeString("value", str);
               writer.WriteEndElement();
               return;

            case ConstantTypeCode.Double:
               writer.WriteStartElement(prefix, "double", ns);
               writer.WriteAttributeString("value", str);
               writer.WriteEndElement();
               return;

            case ConstantTypeCode.Single:
               writer.WriteStartElement(prefix, "float", ns);
               writer.WriteAttributeString("value", str);
               writer.WriteEndElement();
               return;

            default:
               writer.WriteStartElement(prefix, "expression", ns);
               writer.WriteAttributeString("value", str);
               writer.WriteEndElement();
               return;
         }
      }

      static ComponentAttributeData?
      FindComponentAttribute(CustomAttributeHandleCollection attributeHandles, MetadataReader reader) {

         string componentAttrName = $"{_packageModelNs}.XcstComponentAttribute";

         return attributeHandles
            .Select(reader.GetCustomAttribute)
            .Where(c => CustomAttributeName(c, reader) == componentAttrName)
            .Select(c => ParseComponentAttribute(c))
            .FirstOrDefault();
      }

      static ComponentAttributeData
      ParseComponentAttribute(CustomAttribute attrib) {

         CustomAttributeValue<TypeSpec> atValue = attrib.DecodeValue(_attrTypeProvider);

         byte kind = (byte)atValue.FixedArguments[0].Value!;

         string? name = atValue.NamedArguments
            .Where(n => n.Name == "Name")
            .Select(n => (string?)n.Value)
            .FirstOrDefault();

         char cardinality = atValue.NamedArguments
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

      static string
      CustomAttributeName(CustomAttribute attrib, MetadataReader reader) {

         StringHandle nsHandl;
         StringHandle nameHandl;

         if (attrib.Constructor.Kind == HandleKind.MemberReference) {

            MemberReference ctor = reader.GetMemberReference((MemberReferenceHandle)attrib.Constructor);
            TypeReference typeRef = reader.GetTypeReference((TypeReferenceHandle)ctor.Parent);

            nsHandl = typeRef.Namespace;
            nameHandl = typeRef.Name;

         } else if (attrib.Constructor.Kind == HandleKind.MethodDefinition) {

            MethodDefinition ctor = reader.GetMethodDefinition((MethodDefinitionHandle)attrib.Constructor);
            TypeDefinition typeDef = reader.GetTypeDefinition(ctor.GetDeclaringType());

            nsHandl = typeDef.Namespace;
            nameHandl = typeDef.Name;

         } else {
            throw new NotImplementedException();
         }

         string fullName = reader.GetString(nameHandl);

         if (!nsHandl.IsNil) {
            fullName = reader.GetString(nsHandl) + "." + fullName;
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
}
