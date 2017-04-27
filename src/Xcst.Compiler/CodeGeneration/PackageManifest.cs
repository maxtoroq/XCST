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
using System.Reflection;
using System.Xml;
using Xcst.PackageModel;

namespace Xcst.Compiler.CodeGeneration {

   using static CSharpExpression;

   static class PackageManifest {

      public static void WriteManifest(Type packageType, XmlWriter writer) {

         const string ns = XmlNamespaces.XcstSyntax;
         const string prefix = "xcst";

         Func<MethodBase, string> methodVisibility = m =>
            (m.IsAbstract) ? "abstract"
            : (m.IsVirtual) ? "public"
            : "final";

         Func<MemberInfo, string> memberVisibility = m =>
            methodVisibility(m as MethodBase ?? ((PropertyInfo)m).GetGetMethod());

         writer.WriteStartElement(prefix, "package-manifest", ns);
         writer.WriteAttributeString("package-type", TypeReference(packageType));
         writer.WriteAttributeString("qualified-types", "true");

         foreach (MemberInfo member in packageType.GetMembers(BindingFlags.Instance | BindingFlags.Public)) {

            XcstComponentAttribute attr = member.GetCustomAttribute<XcstComponentAttribute>(inherit: true);

            if (attr is XcstAttributeSetAttribute) {

               writer.WriteStartElement(prefix, "attribute-set", ns);
               writer.WriteAttributeString("name", attr.Name);
               writer.WriteAttributeString("visibility", memberVisibility(member));
               writer.WriteAttributeString("member-name", member.Name);
               writer.WriteEndElement();

            } else if (attr is XcstFunctionAttribute) {

               writer.WriteStartElement(prefix, "function", ns);
               writer.WriteAttributeString("name", attr.Name ?? member.Name);
               writer.WriteAttributeString("visibility", memberVisibility(member));
               writer.WriteAttributeString("member-name", member.Name);

               MethodInfo method = ((MethodInfo)member);

               if (method.ReturnType != typeof(void)) {
                  writer.WriteAttributeString("as", TypeReference(method.ReturnType));
               }

               foreach (ParameterInfo param in method.GetParameters()) {

                  writer.WriteStartElement(prefix, "param", ns);
                  writer.WriteAttributeString("name", param.Name);
                  writer.WriteAttributeString("as", TypeReference(param.ParameterType));

                  if (param.IsOptional) {
                     writer.WriteAttributeString("value", Constant(param.RawDefaultValue));
                  }

                  writer.WriteEndElement();
               }

               writer.WriteEndElement();

            } else if (attr is XcstParameterAttribute) {

               writer.WriteStartElement(prefix, "param", ns);
               writer.WriteAttributeString("name", attr.Name ?? member.Name);
               writer.WriteAttributeString("as", TypeReference(((PropertyInfo)member).PropertyType));
               writer.WriteAttributeString("visibility", memberVisibility(member));
               writer.WriteAttributeString("member-name", member.Name);
               writer.WriteEndElement();

            } else if (attr is XcstTemplateAttribute) {

               writer.WriteStartElement(prefix, "template", ns);
               writer.WriteAttributeString("name", attr.Name);
               writer.WriteAttributeString("visibility", memberVisibility(member));
               writer.WriteAttributeString("member-name", member.Name);

               XcstTemplateAttribute tmplAttr = (XcstTemplateAttribute)attr;
               MethodInfo method = ((MethodInfo)member);

               Type outputType = method.GetParameters()[1].ParameterType;

               if (outputType.IsGenericType) {
                  writer.WriteAttributeString("item-type", TypeReference(outputType.GetGenericArguments()[0]));
               }

               writer.WriteAttributeString("cardinality", tmplAttr.Cardinality.ToString());

               foreach (var param in member.GetCustomAttributes<XcstTemplateParameterAttribute>(inherit: true)) {

                  writer.WriteStartElement(prefix, "param", ns);
                  writer.WriteAttributeString("name", param.Name);
                  writer.WriteAttributeString("as", TypeReference(param.Type));
                  writer.WriteAttributeString("required", XmlConvert.ToString(param.Required));
                  writer.WriteAttributeString("tunnel", XmlConvert.ToString(param.Tunnel));
                  writer.WriteEndElement();
               }

               writer.WriteEndElement();

            } else if (attr is XcstTypeAttribute) {

               Type type = (Type)member;

               writer.WriteStartElement(prefix, "type", ns);
               writer.WriteAttributeString("name", attr.Name ?? member.Name);

               writer.WriteAttributeString("visibility",
                  type.IsAbstract ? "abstract"
                  : type.IsSealed ? "final"
                  : "public");

               writer.WriteEndElement();

            } else if (attr is XcstVariableAttribute) {

               writer.WriteStartElement(prefix, "variable", ns);
               writer.WriteAttributeString("name", attr.Name ?? member.Name);
               writer.WriteAttributeString("as", TypeReference(((PropertyInfo)member).PropertyType));
               writer.WriteAttributeString("visibility", memberVisibility(member));
               writer.WriteAttributeString("member-name", member.Name);
               writer.WriteEndElement();
            }
         }

         writer.WriteEndElement();
      }
   }
}
