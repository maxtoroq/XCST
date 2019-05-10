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
using System.Linq;
using System.Text;
using System.Xml;

namespace Xcst.Xml {

   static class XmlWriterSettingsFactory {

      static readonly Action<XmlWriterSettings, XmlOutputMethod> setOutputMethod;
      static readonly Action<XmlWriterSettings, string> setDocTypePublic;
      static readonly Action<XmlWriterSettings, string> setDocTypeSystem;
      static readonly Action<XmlWriterSettings, string> setMediaType;
      static readonly FieldInfo cdataSectionsField;

      static XmlWriterSettingsFactory() {

         Type settingsType = typeof(XmlWriterSettings);

         setOutputMethod = (Action<XmlWriterSettings, XmlOutputMethod>)
            Delegate.CreateDelegate(typeof(Action<XmlWriterSettings, XmlOutputMethod>), settingsType.GetProperty(nameof(XmlWriterSettings.OutputMethod), BindingFlags.Instance | BindingFlags.Public).GetSetMethod(true));

         setDocTypePublic = (Action<XmlWriterSettings, string>)
            Delegate.CreateDelegate(typeof(Action<XmlWriterSettings, string>), settingsType.GetProperty("DocTypePublic", BindingFlags.Instance | BindingFlags.NonPublic).GetSetMethod(true));

         setDocTypeSystem = (Action<XmlWriterSettings, string>)
            Delegate.CreateDelegate(typeof(Action<XmlWriterSettings, string>), settingsType.GetProperty("DocTypeSystem", BindingFlags.Instance | BindingFlags.NonPublic).GetSetMethod(true));

         setMediaType = (Action<XmlWriterSettings, string>)
            Delegate.CreateDelegate(typeof(Action<XmlWriterSettings, string>), settingsType.GetProperty("MediaType", BindingFlags.Instance | BindingFlags.NonPublic).GetSetMethod(true));

         cdataSectionsField = settingsType.GetField("cdataSections", BindingFlags.Instance | BindingFlags.NonPublic);
      }

      public static XmlWriterSettings Create(OutputParameters parameters) {

         var settings = new XmlWriterSettings {
            ConformanceLevel = ConformanceLevel.Auto
         };

         if (parameters.Method != null
            && parameters.Method != OutputParameters.Methods.Xml) {

            if (parameters.Method == OutputParameters.Methods.Html) {
               setOutputMethod(settings, XmlOutputMethod.Html);

            } else if (parameters.Method == OutputParameters.Methods.Text) {
               setOutputMethod(settings, XmlOutputMethod.Text);
            }
         }

         if (parameters.CdataSectionElements?.Count > 0) {

            cdataSectionsField.SetValue(
               settings,
               parameters.CdataSectionElements
                  .Select(qn => new XmlQualifiedName(qn.Name, qn.Namespace))
                  .ToList()
            );
         }

         if (parameters.DoctypePublic != null) {
            setDocTypePublic(settings, parameters.DoctypePublic);
         }

         if (parameters.DoctypeSystem != null) {
            setDocTypeSystem(settings, parameters.DoctypeSystem);
         }

         if (parameters.EscapeUriAttributes != null) {
            settings.DoNotEscapeUriAttributes = !parameters.EscapeUriAttributes.Value;
         }

         if (parameters.Encoding != null) {
            settings.Encoding = parameters.Encoding;
         }

         if (parameters.Indent != null) {
            settings.Indent = parameters.Indent.Value;
         }

         if (parameters.IndentSpaces != null) {
            settings.IndentChars = new string(' ', parameters.IndentSpaces.Value);
         }

         if (parameters.MediaType != null) {
            setMediaType(settings, parameters.MediaType);
         }

         if (parameters.OmitXmlDeclaration != null) {
            settings.OmitXmlDeclaration = parameters.OmitXmlDeclaration.Value;
         }

         Encoding enc = settings.Encoding;

         if (parameters.ByteOrderMark != null
            && !parameters.ByteOrderMark.Value) {

            if (enc is UTF8Encoding) {
               settings.Encoding = new UTF8Encoding(false);
            }
         }

         if (parameters.SkipCharacterCheck != null) {
            settings.CheckCharacters = !parameters.SkipCharacterCheck.Value;
         }

         return settings;
      }
   }
}