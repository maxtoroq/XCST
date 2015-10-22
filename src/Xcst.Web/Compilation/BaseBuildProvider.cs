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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Compilation;
using System.Web.Hosting;
using Xcst.Compiler;

namespace Xcst.Web.Compilation {

   public abstract class BaseBuildProvider : BuildProvider {

      string _GeneratedTypeName, _GeneratedTypeNamespace, _GeneratedTypeFullName;
      Uri _PhysicalPath, _ApplicationPhysicalPath;
      string _AppRelativeVirtualPath;
      bool? _IsFileInCodeDir;

      bool parsed;
      CompilerType _CodeCompilerType;

      protected string AppRelativeVirtualPath {
         get {
            return _AppRelativeVirtualPath
               ?? (_AppRelativeVirtualPath = VirtualPathUtility.ToAppRelative(VirtualPath));
         }
      }

      protected Uri PhysicalPath {
         get {
            return _PhysicalPath
               ?? (_PhysicalPath = new Uri(HostingEnvironment.MapPath(VirtualPath), UriKind.Absolute));
         }
      }

      protected bool IsFileInCodeDir {
         get {
            return _IsFileInCodeDir
               ?? (_IsFileInCodeDir = AppRelativeVirtualPath
                  .Remove(0, 2)
                  .Split('/')[0]
                  .Equals("App_Code", StringComparison.OrdinalIgnoreCase)).Value;
         }
      }

      protected string GeneratedTypeName {
         get {
            if (_GeneratedTypeName == null) {

               string typeName;

               _GeneratedTypeNamespace = GetNamespaceAndTypeNameFromVirtualPath((IsFileInCodeDir) ? 1 : 0, out typeName);
               _GeneratedTypeName = GeneratedTypeNamePrefix + typeName;
            }
            return _GeneratedTypeName;
         }
      }

      protected virtual string GeneratedTypeNamePrefix => null;

      protected string GeneratedTypeNamespace {
         get {
            if (_GeneratedTypeNamespace == null) {
               // getting GeneratedTypeName will initialize _GeneratedTypeNamespace
               string s = GeneratedTypeName;
            }
            return _GeneratedTypeNamespace;
         }
      }

      protected string GeneratedTypeFullName {
         get {
            return _GeneratedTypeFullName
               ?? (_GeneratedTypeFullName = (GeneratedTypeNamespace.Length == 0) ? GeneratedTypeName
                  : String.Concat(GeneratedTypeNamespace, ".", GeneratedTypeName));
         }
         set {
            if (String.IsNullOrEmpty(value)) {
               throw new ArgumentException("value cannot be null or empty", nameof(value));
            }

            _GeneratedTypeName = _GeneratedTypeNamespace = _GeneratedTypeFullName = null;

            if (value.Contains(".")) {
               string[] segments = value.Split('.');
               _GeneratedTypeName = segments[segments.Length - 1];
               _GeneratedTypeNamespace = String.Join(".", segments, 0, segments.Length - 1);
            } else {
               _GeneratedTypeName = value;
               _GeneratedTypeNamespace = "";
            }
         }
      }

      protected virtual bool IgnoreFile => false;

      public override CompilerType CodeCompilerType {
         get {
            if (IgnoreFile) {
               return null;
            }

            if (!parsed) {

               string language = Parse();

               _CodeCompilerType = (!String.IsNullOrEmpty(language)) ?
                  GetDefaultCompilerTypeForLanguage(language)
                  : null;

               parsed = true;
            }

            return _CodeCompilerType;
         }
      }

      string Parse() {

         try {
            return ParsePath();

         } catch (CompileException ex) {

            string moduleUri = ex.ModuleUri;

            if (moduleUri != null) {

               Uri uri;

               if (Uri.TryCreate(moduleUri, UriKind.Absolute, out uri)
                  && uri.IsFile) {

                  moduleUri = uri.LocalPath;
               }
            }

            throw new HttpParseException(ex.Message, ex, moduleUri ?? this.VirtualPath, null, ex.LineNumber);
         }
      }

      protected abstract string ParsePath();

      protected abstract IEnumerable<CodeCompileUnit> BuildCompileUnits();

      public override void GenerateCode(AssemblyBuilder assemblyBuilder) {

         if (this.IgnoreFile) {
            return;
         }

         foreach (CodeCompileUnit compileUnit in BuildCompileUnits()) {
            assemblyBuilder.AddCodeCompileUnit(this, compileUnit);
         }
      }

      public override Type GetGeneratedType(CompilerResults results) {

         if (this.IgnoreFile) {
            return null;
         }

         return results.CompiledAssembly.GetType(this.GeneratedTypeFullName);
      }

      protected Exception CreateParseException(string message, int line, string virtualPath = null, Exception innerException = null) {
         return new HttpParseException(message, innerException, virtualPath ?? this.VirtualPath, null, line);
      }

      string GetNamespaceAndTypeNameFromVirtualPath(int chunksToIgnore, out string typeName) {

         string fileName = (this.IsFileInCodeDir) ?
            VirtualPathUtility.GetFileName(this.VirtualPath) :
            this.AppRelativeVirtualPath.Remove(0, 2);

         string[] strArray = fileName.Split(new char[] { '.', '/', '\\' });
         int num = strArray.Length - chunksToIgnore;

         if (strArray[num - 1].Trim().Length == 0) {
            throw new HttpException($"The file name '{fileName}' is not supported.");
         }

         typeName = MakeValidTypeNameFromString(
            (this.IsFileInCodeDir) ? strArray[num - 1]
               : String.Join("_", strArray, 0, num).ToLowerInvariant()
         );

         if (!this.IsFileInCodeDir) {
            return "ASP";
         }

         for (int i = 0; i < (num - 1); i++) {

            if (strArray[i].Trim().Length == 0) {
               throw new HttpException($"The file name '{fileName}' is not supported.");
            }

            strArray[i] = MakeValidTypeNameFromString(strArray[i]);
         }

         return String.Join(".", strArray, 0, num - 1);
      }

      string MakeValidTypeNameFromString(string s) {

         var builder = new StringBuilder();

         for (int i = 0; i < s.Length; i++) {

            if ((i == 0) && char.IsDigit(s[0])) {
               builder.Append('_');
            }

            builder.Append(char.IsLetterOrDigit(s[i]) ? s[i] : '_');
         }
         return builder.ToString();
      }
   }
}