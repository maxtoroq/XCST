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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Xcst.Compiler.Reflection;

class TypeManifestReader {

   const string
   _ns = XmlNamespaces.XcstGrammar;

   const string
   _prefix = "xcst";

   readonly XmlWriter
   _writer;

   public
   TypeManifestReader(XmlWriter writer) {
      _writer = writer;
   }

   public void
   WritePackage(Type packageType) {

      _writer.WriteStartElement(_prefix, "package-manifest", _ns);
      _writer.WriteAttributeString("qualified-types", "true");

      WriteTypeReference(packageType);

      var pkgInterface = PackageInterface(packageType)!;
      var componentAttrType = ComponentAttributeType(pkgInterface, "XcstComponentAttribute");
      var requiredAttrType = ComponentAttributeType(pkgInterface, "RequiredAttribute");

      foreach (var member in packageType.GetMembers(BindingFlags.Instance | BindingFlags.Public)) {

         dynamic? attr = member.GetCustomAttribute(componentAttrType, inherit: false);

         if (attr != null) {
            switch ((byte)attr.Kind) {
               case 1:
                  WriteTemplate(attr, (MethodInfo)member, requiredAttrType);
                  break;

               case 2:
                  WriteAttributeSet(attr, (MethodInfo)member);
                  break;

               case 3:
                  WriteFunction((MethodInfo)member);
                  break;

               case 4:
                  WriteVariable((PropertyInfo)member);
                  break;

               case 5:
                  WriteParameter((PropertyInfo)member, requiredAttrType);
                  break;

               case 6:
                  WriteType((Type)member);
                  break;
            }
         }
      }

      _writer.WriteEndElement();
   }

   void
   WriteTemplate(dynamic attr, MethodInfo method, Type requiredAttrType) {

      var cardinality = (char)attr.Cardinality switch {
         ' ' => "One",
         _ => "ZeroOrMore"
      };

      _writer.WriteStartElement(_prefix, "template", _ns);
      _writer.WriteAttributeString("name", attr.Name);
      _writer.WriteAttributeString("visibility", ComponentVisibility(method));
      _writer.WriteAttributeString("member-name", method.Name);
      _writer.WriteAttributeString("cardinality", cardinality);

      var methodParams = method.GetParameters();
      var contextType = methodParams[0].ParameterType;

      var paramsType = (contextType.IsGenericType) ?
         contextType.GetGenericArguments()[0]
         : null;

      foreach (var property in paramsType?.GetProperties() ?? Array.Empty<PropertyInfo>()) {

         var required = property.GetCustomAttribute(requiredAttrType) != null;

         _writer.WriteStartElement(_prefix, "param", _ns);
         _writer.WriteAttributeString("name", property.Name);
         _writer.WriteAttributeString("required", XmlConvert.ToString(required));
         _writer.WriteAttributeString("tunnel", "false");

         WriteTypeReference(property.PropertyType);

         _writer.WriteEndElement();
      }

      var outputType = methodParams[1].ParameterType;
      Type itemType;

      if (outputType.IsGenericType
         && (itemType = outputType.GetGenericArguments()[0]) != typeof(object)) {

         _writer.WriteStartElement(_prefix, "item-type", _ns);
         WriteTypeReference(itemType);
         _writer.WriteEndElement();
      }

      _writer.WriteEndElement();
   }

   void
   WriteAttributeSet(dynamic attr, MethodInfo method) {

      _writer.WriteStartElement(_prefix, "attribute-set", _ns);
      _writer.WriteAttributeString("name", attr.Name);
      _writer.WriteAttributeString("visibility", ComponentVisibility(method));
      _writer.WriteAttributeString("member-name", method.Name);
      _writer.WriteEndElement();
   }

   void
   WriteFunction(MethodInfo method) {

      _writer.WriteStartElement(_prefix, "function", _ns);
      _writer.WriteAttributeString("name", method.Name);
      _writer.WriteAttributeString("visibility", ComponentVisibility(method));
      _writer.WriteAttributeString("member-name", method.Name);

      if (method.ReturnType != typeof(void)) {
         WriteTypeReference(method.ReturnType, method, NullableAttribute(method.ReturnParameter));
      }

      foreach (var param in method.GetParameters()) {

         _writer.WriteStartElement(_prefix, "param", _ns);
         _writer.WriteAttributeString("name", param.Name);

         WriteTypeReference(param.ParameterType, method, NullableAttribute(param));

         if (param.IsOptional) {
            WriteConstant(param.RawDefaultValue);
         }

         _writer.WriteEndElement();
      }

      _writer.WriteEndElement();
   }

   void
   WriteParameter(PropertyInfo property, Type requiredAttrType) {

      var required = property.GetCustomAttribute(requiredAttrType) != null;

      _writer.WriteStartElement(_prefix, "param", _ns);
      _writer.WriteAttributeString("name", property.Name);
      _writer.WriteAttributeString("required", XmlConvert.ToString(required));
      _writer.WriteAttributeString("visibility", ComponentVisibility(property));
      _writer.WriteAttributeString("member-name", property.Name);
      WriteTypeReference(property.PropertyType, property, NullableAttribute(property));
      _writer.WriteEndElement();
   }

   void
   WriteVariable(PropertyInfo property) {

      _writer.WriteStartElement(_prefix, "variable", _ns);
      _writer.WriteAttributeString("name", property.Name);
      _writer.WriteAttributeString("visibility", ComponentVisibility(property));
      _writer.WriteAttributeString("member-name", property.Name);
      WriteTypeReference(property.PropertyType, property, NullableAttribute(property));
      _writer.WriteEndElement();
   }

   void
   WriteType(Type type) {

      _writer.WriteStartElement(_prefix, "type", _ns);
      _writer.WriteAttributeString("name", type.Name);

      _writer.WriteAttributeString("visibility",
         type.IsAbstract ? "abstract"
         : type.IsSealed ? "final"
         : "public");

      _writer.WriteEndElement();
   }

   internal void
   WriteTypeReference(Type type, MemberInfo? member = null, byte[]? nullableAttr = null, int flagOffset = 0) {

      const string ns = XmlNamespaces.XcstCode;
      const string prefix = "code";

      _writer.WriteStartElement(prefix, "type-reference", ns);

      if (type.IsArray) {

         _writer.WriteAttributeString("array-dimensions", XmlConvert.ToString(type.GetArrayRank()));

         WriteNullable(member, nullableAttr, flagOffset);
         WriteTypeReference(type.GetElementType()!, member, nullableAttr, flagOffset + 1);

      } else {

         var typeArguments = type.GetGenericArguments();

         var name = (typeArguments.Length > 0) ?
            type.Name.Substring(0, type.Name.IndexOf('`'))
            : type.Name;

         _writer.WriteAttributeString("name", name);

         if (type.IsInterface) {
            _writer.WriteAttributeString("interface", "true");
         }

         if (!type.IsValueType) {
            WriteNullable(member, nullableAttr, flagOffset);
         }

         if (type.IsNested) {
            WriteTypeReference(type.DeclaringType!, member, nullableAttr, flagOffset);
         } else {
            _writer.WriteAttributeString("namespace", type.Namespace);
         }

         if (typeArguments.Length > 0) {

            _writer.WriteStartElement(prefix, "type-arguments", ns);

            for (int i = 0; i < typeArguments.Length; i++) {
               WriteTypeReference(typeArguments[i], member, nullableAttr, flagOffset + i + 1);
            }

            _writer.WriteEndElement();
         }
      }

      _writer.WriteEndElement();
   }

   void
   WriteNullable(MemberInfo? member, byte[]? nullableAttr, int flagOffset) {

      if ((nullableAttr?[flagOffset] ?? NullableContext(member)) == 2) {
         _writer.WriteAttributeString("nullable", "true");
      }
   }

   byte[]?
   NullableAttribute(MemberInfo member) =>
      NullableAttribute(member.GetCustomAttributesData());

   byte[]?
   NullableAttribute(ParameterInfo member) =>
      NullableAttribute(member.GetCustomAttributesData());

   static byte[]?
   NullableAttribute(IList<CustomAttributeData> attrData) {

      var nullableAttr = attrData?
         .FirstOrDefault(p => p.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");

      if (nullableAttr != null
         && nullableAttr.ConstructorArguments.Count == 1) {

         switch (nullableAttr.ConstructorArguments[0].Value) {
            case byte flag:
               return new byte[] { flag };

            case IList<CustomAttributeTypedArgument> flagsArgs:
               return flagsArgs
                  .Select(p => (byte)p.Value)
                  .ToArray();
         }
      }

      return null;
   }

   static byte?
   NullableContext(MemberInfo? member) {

      if (member is null) {
         return null;
      }

      var attr = member.GetCustomAttributesData()
         .FirstOrDefault(p => p.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute");

      if (attr != null
         && attr.ConstructorArguments.Count == 1
         && attr.ConstructorArguments[0].Value is byte flag) {

         return flag;
      }

      if (member.DeclaringType is Type t) {
         return NullableContext(t);
      }

      return null;
   }

   void
   WriteConstant(object? value) {

      const string ns = XmlNamespaces.XcstCode;
      const string prefix = "code";

      if (value is null) {
         _writer.WriteStartElement(prefix, "null", ns);
         _writer.WriteEndElement();
         return;
      }

      var str = Convert.ToString(value, CultureInfo.InvariantCulture)!;

      if (value is string) {
         _writer.WriteStartElement(prefix, "string", ns);
         _writer.WriteAttributeString("verbatim", "true");
         _writer.WriteString(str);
         _writer.WriteEndElement();
         return;
      }

      if (value is bool) {
         _writer.WriteStartElement(prefix, "bool", ns);
         _writer.WriteAttributeString("value", str.ToLowerInvariant());
         _writer.WriteEndElement();
         return;
      }

      if (value is char) {
         _writer.WriteStartElement(prefix, "char", ns);
         _writer.WriteAttributeString("value", str);
         _writer.WriteEndElement();
         return;
      }

      if (value is int) {
         _writer.WriteStartElement(prefix, "int", ns);
         _writer.WriteAttributeString("value", str);
         _writer.WriteEndElement();
         return;
      }

      if (value is uint) {
         _writer.WriteStartElement(prefix, "uint", ns);
         _writer.WriteAttributeString("value", str);
         _writer.WriteEndElement();
         return;
      }

      if (value is long) {
         _writer.WriteStartElement(prefix, "long", ns);
         _writer.WriteAttributeString("value", str);
         _writer.WriteEndElement();
         return;
      }

      if (value is ulong) {
         _writer.WriteStartElement(prefix, "ulong", ns);
         _writer.WriteAttributeString("value", str);
         _writer.WriteEndElement();
         return;
      }

      if (value is double) {
         _writer.WriteStartElement(prefix, "double", ns);
         _writer.WriteAttributeString("value", str);
         _writer.WriteEndElement();
         return;
      }

      if (value is float) {
         _writer.WriteStartElement(prefix, "float", ns);
         _writer.WriteAttributeString("value", str);
         _writer.WriteEndElement();
         return;
      }

      if (value is decimal) {
         _writer.WriteStartElement(prefix, "decimal", ns);
         _writer.WriteAttributeString("value", str);
         _writer.WriteEndElement();
         return;
      }

      _writer.WriteStartElement(prefix, "expression", ns);
      _writer.WriteAttributeString("value", str);
      _writer.WriteEndElement();
      return;
   }

   static string
   ComponentVisibility(PropertyInfo property) =>
      ComponentVisibility(property.GetGetMethod()!);

   static string
   ComponentVisibility(MethodBase method) =>
      (method.IsAbstract) ? "abstract"
      : (method.IsVirtual) ? "public"
      : "final";

   public static bool
   IsXcstPackage(Type t) =>
      PackageInterface(t) != null;

   static Type?
   PackageInterface(Type t) =>
      (t.GetInterface("Xcst.IXcstPackage") is Type pkgInterface
         && pkgInterface.Assembly.GetName().Name == "Xcst.Runtime") ? pkgInterface
      : null;

   static Type
   ComponentAttributeType(Type pkgInterface, string attributeName) =>
      pkgInterface.Assembly.GetType("Xcst.Runtime." + attributeName)!;
}
