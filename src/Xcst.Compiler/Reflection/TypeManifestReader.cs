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
using System.Globalization;
using System.Reflection;
using System.Xml;

namespace Xcst.Compiler.Reflection {

   static class TypeManifestReader {

      const string
      _ns = XmlNamespaces.XcstGrammar;

      const string
      _prefix = "xcst";

      public static void
      WritePackage(Type packageType, XmlWriter writer) {

         writer.WriteStartElement(_prefix, "package-manifest", _ns);
         writer.WriteAttributeString("qualified-types", "true");

         WriteTypeReference(packageType, writer);

         Type pkgInterface = PackageInterface(packageType)!;
         Type componentAttrType = ComponentAttributeType(pkgInterface, "XcstComponentAttribute");
         Type requiredAttrType = ComponentAttributeType(pkgInterface, "RequiredAttribute");

         foreach (MemberInfo member in packageType.GetMembers(BindingFlags.Instance | BindingFlags.Public)) {

            dynamic? attr = member.GetCustomAttribute(componentAttrType, inherit: false);

            if (attr != null) {
               switch ((byte)attr.Kind) {
                  case 1:
                     WriteTemplate(attr, (MethodInfo)member, requiredAttrType, writer);
                     break;

                  case 2:
                     WriteAttributeSet(attr, (MethodInfo)member, writer);
                     break;

                  case 3:
                     WriteFunction((MethodInfo)member, writer);
                     break;

                  case 4:
                     WriteVariable((PropertyInfo)member, writer);
                     break;

                  case 5:
                     WriteParameter((PropertyInfo)member, requiredAttrType, writer);
                     break;

                  case 6:
                     WriteType((Type)member, writer);
                     break;
               }
            }
         }

         writer.WriteEndElement();
      }

      static void
      WriteTemplate(dynamic attr, MethodInfo method, Type requiredAttrType, XmlWriter writer) {

         string cardinality = (char)attr.Cardinality switch {
            ' ' => "One",
            _ => "ZeroOrMore"
         };

         writer.WriteStartElement(_prefix, "template", _ns);
         writer.WriteAttributeString("name", attr.Name);
         writer.WriteAttributeString("visibility", ComponentVisibility(method));
         writer.WriteAttributeString("member-name", method.Name);
         writer.WriteAttributeString("cardinality", cardinality);

         ParameterInfo[] methodParams = method.GetParameters();
         Type contextType = methodParams[0].ParameterType;

         Type? paramsType = (contextType.IsGenericType) ?
            contextType.GetGenericArguments()[0]
            : null;

         foreach (PropertyInfo property in paramsType?.GetProperties() ?? Array.Empty<PropertyInfo>()) {

            bool required = property.GetCustomAttribute(requiredAttrType) != null;

            writer.WriteStartElement(_prefix, "param", _ns);
            writer.WriteAttributeString("name", property.Name);
            writer.WriteAttributeString("required", XmlConvert.ToString(required));
            writer.WriteAttributeString("tunnel", "false");

            WriteTypeReference(property.PropertyType, writer);

            writer.WriteEndElement();
         }

         Type outputType = methodParams[1].ParameterType;
         Type itemType;

         if (outputType.IsGenericType
            && (itemType = outputType.GetGenericArguments()[0]) != typeof(object)) {

            writer.WriteStartElement(_prefix, "item-type", _ns);
            WriteTypeReference(itemType, writer);
            writer.WriteEndElement();
         }

         writer.WriteEndElement();
      }

      static void
      WriteAttributeSet(dynamic attr, MethodInfo method, XmlWriter writer) {

         writer.WriteStartElement(_prefix, "attribute-set", _ns);
         writer.WriteAttributeString("name", attr.Name);
         writer.WriteAttributeString("visibility", ComponentVisibility(method));
         writer.WriteAttributeString("member-name", method.Name);
         writer.WriteEndElement();
      }

      static void
      WriteFunction(MethodInfo method, XmlWriter writer) {

         writer.WriteStartElement(_prefix, "function", _ns);
         writer.WriteAttributeString("name", method.Name);
         writer.WriteAttributeString("visibility", ComponentVisibility(method));
         writer.WriteAttributeString("member-name", method.Name);

         if (method.ReturnType != typeof(void)) {
            WriteTypeReference(method.ReturnType, writer);
         }

         foreach (ParameterInfo param in method.GetParameters()) {

            writer.WriteStartElement(_prefix, "param", _ns);
            writer.WriteAttributeString("name", param.Name);

            WriteTypeReference(param.ParameterType, writer);

            if (param.IsOptional) {
               WriteConstant(param.RawDefaultValue, writer);
            }

            writer.WriteEndElement();
         }

         writer.WriteEndElement();
      }

      static void
      WriteParameter(PropertyInfo property, Type requiredAttrType, XmlWriter writer) {

         bool required = property.GetCustomAttribute(requiredAttrType) != null;

         writer.WriteStartElement(_prefix, "param", _ns);
         writer.WriteAttributeString("name", property.Name);
         writer.WriteAttributeString("required", XmlConvert.ToString(required));
         writer.WriteAttributeString("visibility", ComponentVisibility(property));
         writer.WriteAttributeString("member-name", property.Name);
         WriteTypeReference(property.PropertyType, writer);
         writer.WriteEndElement();
      }

      static void
      WriteVariable(PropertyInfo property, XmlWriter writer) {

         writer.WriteStartElement(_prefix, "variable", _ns);
         writer.WriteAttributeString("name", property.Name);
         writer.WriteAttributeString("visibility", ComponentVisibility(property));
         writer.WriteAttributeString("member-name", property.Name);
         WriteTypeReference(property.PropertyType, writer);
         writer.WriteEndElement();
      }

      static void
      WriteType(Type type, XmlWriter writer) {

         writer.WriteStartElement(_prefix, "type", _ns);
         writer.WriteAttributeString("name", type.Name);

         writer.WriteAttributeString("visibility",
            type.IsAbstract ? "abstract"
            : type.IsSealed ? "final"
            : "public");

         writer.WriteEndElement();
      }

      internal static void
      WriteTypeReference(Type type, XmlWriter writer) {

         const string ns = XmlNamespaces.XcstCode;
         const string prefix = "code";

         writer.WriteStartElement(prefix, "type-reference", ns);

         if (type.IsArray) {

            writer.WriteAttributeString("array-dimensions", XmlConvert.ToString(type.GetArrayRank()));
            WriteTypeReference(type.GetElementType()!, writer);

         } else {

            Type[] typeArguments = type.GetGenericArguments();

            string name = (typeArguments.Length > 0) ?
               type.Name.Substring(0, type.Name.IndexOf('`'))
               : type.Name;

            writer.WriteAttributeString("name", name);

            if (type.IsInterface) {
               writer.WriteAttributeString("interface", "true");
            }

            if (type.IsNested) {
               WriteTypeReference(type.DeclaringType!, writer);
            } else {
               writer.WriteAttributeString("namespace", type.Namespace);
            }

            if (typeArguments.Length > 0) {

               writer.WriteStartElement(prefix, "type-arguments", ns);

               for (int i = 0; i < typeArguments.Length; i++) {
                  WriteTypeReference(typeArguments[i], writer);
               }

               writer.WriteEndElement();
            }
         }

         writer.WriteEndElement();
      }

      static void
      WriteConstant(object? value, XmlWriter writer) {

         const string ns = XmlNamespaces.XcstCode;
         const string prefix = "code";

         if (value is null) {
            writer.WriteStartElement(prefix, "null", ns);
            writer.WriteEndElement();
            return;
         }

         string str = Convert.ToString(value, CultureInfo.InvariantCulture)!;

         if (value is string) {
            writer.WriteStartElement(prefix, "string", ns);
            writer.WriteAttributeString("verbatim", "true");
            writer.WriteString(str);
            writer.WriteEndElement();
            return;
         }

         if (value is bool) {
            writer.WriteStartElement(prefix, "bool", ns);
            writer.WriteAttributeString("value", str.ToLowerInvariant());
            writer.WriteEndElement();
            return;
         }

         if (value is char) {
            writer.WriteStartElement(prefix, "char", ns);
            writer.WriteAttributeString("value", str);
            writer.WriteEndElement();
            return;
         }

         if (value is int) {
            writer.WriteStartElement(prefix, "int", ns);
            writer.WriteAttributeString("value", str);
            writer.WriteEndElement();
            return;
         }

         if (value is uint) {
            writer.WriteStartElement(prefix, "uint", ns);
            writer.WriteAttributeString("value", str);
            writer.WriteEndElement();
            return;
         }

         if (value is long) {
            writer.WriteStartElement(prefix, "long", ns);
            writer.WriteAttributeString("value", str);
            writer.WriteEndElement();
            return;
         }

         if (value is ulong) {
            writer.WriteStartElement(prefix, "ulong", ns);
            writer.WriteAttributeString("value", str);
            writer.WriteEndElement();
            return;
         }

         if (value is double) {
            writer.WriteStartElement(prefix, "double", ns);
            writer.WriteAttributeString("value", str);
            writer.WriteEndElement();
            return;
         }

         if (value is float) {
            writer.WriteStartElement(prefix, "float", ns);
            writer.WriteAttributeString("value", str);
            writer.WriteEndElement();
            return;
         }

         if (value is decimal) {
            writer.WriteStartElement(prefix, "decimal", ns);
            writer.WriteAttributeString("value", str);
            writer.WriteEndElement();
            return;
         }

         writer.WriteStartElement(prefix, "expression", ns);
         writer.WriteAttributeString("value", str);
         writer.WriteEndElement();
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
         (t.GetInterface("Xcst.PackageModel.IXcstPackage") is Type pkgInterface
            && pkgInterface.Assembly.GetName().Name == "Xcst.Runtime") ? pkgInterface
         : null;

      static Type
      ComponentAttributeType(Type pkgInterface, string attributeName) =>
         pkgInterface.Assembly.GetType("Xcst.PackageModel." + attributeName)!;
   }
}
