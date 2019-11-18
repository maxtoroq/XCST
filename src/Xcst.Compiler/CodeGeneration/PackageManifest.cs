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
using System.Linq;
using System.Xml;
//using Xcst.PackageModel;

namespace Xcst.Compiler.CodeGeneration {

   using static XcstCompiler;

   static class PackageManifest {

      public static void
      WriteManifest(Type packageType, XmlWriter writer) {

         const string ns = XmlNamespaces.XcstGrammar;
         const string prefix = "xcst";

         static string methodVisibility(MethodBase m) =>
            (m.IsAbstract) ? "abstract"
            : (m.IsVirtual) ? "public"
            : "final";

         static string memberVisibility(MemberInfo m) =>
            methodVisibility(m as MethodBase ?? ((PropertyInfo)m).GetGetMethod());

         writer.WriteStartElement(prefix, "package-manifest", ns);
         writer.WriteAttributeString("qualified-types", "true");

         WriteTypeReference(packageType, writer);

         Type pkgInterface = PackageInterface(packageType);
         Type componentAttrType = ComponentAttributeType(pkgInterface, "XcstComponentAttribute");

         foreach (MemberInfo member in packageType.GetMembers(BindingFlags.Instance | BindingFlags.Public)) {

            dynamic attr = member.GetCustomAttribute(componentAttrType, inherit: true);

            if (attr is null) {
               continue;
            }

            string name = attr.Name;
            string attrName = attr.GetType().Name;

            if (attrName == "XcstAttributeSetAttribute") {

               writer.WriteStartElement(prefix, "attribute-set", ns);
               writer.WriteAttributeString("name", name);
               writer.WriteAttributeString("visibility", memberVisibility(member));
               writer.WriteAttributeString("member-name", member.Name);
               writer.WriteEndElement();

            } else if (attrName == "XcstFunctionAttribute") {

               writer.WriteStartElement(prefix, "function", ns);
               writer.WriteAttributeString("name", name ?? member.Name);
               writer.WriteAttributeString("visibility", memberVisibility(member));
               writer.WriteAttributeString("member-name", member.Name);

               MethodInfo method = ((MethodInfo)member);

               if (method.ReturnType != typeof(void)) {
                  WriteTypeReference(method.ReturnType, writer);
               }

               foreach (ParameterInfo param in method.GetParameters()) {

                  writer.WriteStartElement(prefix, "param", ns);
                  writer.WriteAttributeString("name", param.Name);

                  WriteTypeReference(param.ParameterType, writer);

                  if (param.IsOptional) {
                     WriteConstant(param.RawDefaultValue, writer);
                  }

                  writer.WriteEndElement();
               }

               writer.WriteEndElement();

            } else if (attrName == "XcstParameterAttribute") {

               writer.WriteStartElement(prefix, "param", ns);
               writer.WriteAttributeString("name", name ?? member.Name);
               writer.WriteAttributeString("required", XmlConvert.ToString(attr.Required));
               writer.WriteAttributeString("visibility", memberVisibility(member));
               writer.WriteAttributeString("member-name", member.Name);
               WriteTypeReference(((PropertyInfo)member).PropertyType, writer);
               writer.WriteEndElement();

            } else if (attrName == "XcstTemplateAttribute") {

               writer.WriteStartElement(prefix, "template", ns);
               writer.WriteAttributeString("name", name);
               writer.WriteAttributeString("visibility", memberVisibility(member));
               writer.WriteAttributeString("member-name", member.Name);
               writer.WriteAttributeString("cardinality", attr.Cardinality.ToString());

               Type paramAttrType = ComponentAttributeType(pkgInterface, "XcstTemplateParameterAttribute");

               foreach (dynamic param in member.GetCustomAttributes(paramAttrType, inherit: true)) {

                  writer.WriteStartElement(prefix, "param", ns);
                  writer.WriteAttributeString("name", param.Name);
                  writer.WriteAttributeString("required", XmlConvert.ToString(param.Required));
                  writer.WriteAttributeString("tunnel", XmlConvert.ToString(param.Tunnel));
                  WriteTypeReference(param.Type, writer);
                  writer.WriteEndElement();
               }

               MethodInfo method = (MethodInfo)member;
               Type outputType = method.GetParameters()[1].ParameterType;
               Type itemType;

               if (outputType.IsGenericType
                  && (itemType = outputType.GetGenericArguments()[0]) != typeof(object)) {

                  writer.WriteStartElement(prefix, "item-type", ns);
                  WriteTypeReference(itemType, writer);
                  writer.WriteEndElement();
               }

               writer.WriteEndElement();

            } else if (attrName == "XcstTypeAttribute") {

               Type type = (Type)member;

               writer.WriteStartElement(prefix, "type", ns);
               writer.WriteAttributeString("name", name ?? member.Name);

               writer.WriteAttributeString("visibility",
                  type.IsAbstract ? "abstract"
                  : type.IsSealed ? "final"
                  : "public");

               writer.WriteEndElement();

            } else if (attrName == "XcstVariableAttribute") {

               writer.WriteStartElement(prefix, "variable", ns);
               writer.WriteAttributeString("name", name ?? member.Name);
               writer.WriteAttributeString("visibility", memberVisibility(member));
               writer.WriteAttributeString("member-name", member.Name);
               WriteTypeReference(((PropertyInfo)member).PropertyType, writer);
               writer.WriteEndElement();
            }
         }

         writer.WriteEndElement();
      }

      static void
      WriteConstant(object value, XmlWriter writer) {

         const string ns = XmlNamespaces.XcstCode;
         const string prefix = "code";

         if (value is null) {
            writer.WriteStartElement(prefix, "null", ns);
            writer.WriteEndElement();
            return;
         }

         string str = Convert.ToString(value, CultureInfo.InvariantCulture);

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

         if (value is decimal) {
            writer.WriteStartElement(prefix, "decimal", ns);
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

         if (value is uint) {
            writer.WriteStartElement(prefix, "uint", ns);
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

         writer.WriteStartElement(prefix, "expression", ns);
         writer.WriteAttributeString("value", str);
         writer.WriteEndElement();
         return;
      }

      public static bool IsXcstPackage(Type t) =>
         PackageInterface(t) != null;

      static Type? PackageInterface(Type t) =>
         (t.GetInterface("Xcst.PackageModel.IXcstPackage") is Type pkgInterface
            && pkgInterface.Assembly.GetName().Name == "Xcst.Runtime") ? pkgInterface
         : null;

      static Type ComponentAttributeType(Type pkgInterface, string attributeName) =>
         pkgInterface.Assembly.GetType("Xcst.PackageModel." + attributeName);
   }
}
