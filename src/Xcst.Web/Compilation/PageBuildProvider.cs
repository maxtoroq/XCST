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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Compilation;
using System.Web.Hosting;
using Xcst.Compiler;

namespace Xcst.Web.Compilation {

   [BuildProviderAppliesTo(BuildProviderAppliesTo.Web | BuildProviderAppliesTo.Code)]
   public class PageBuildProvider<TPage> : BaseBuildProvider where TPage : class {

      readonly Uri applicationUri = new Uri(HostingEnvironment.ApplicationPhysicalPath, UriKind.Absolute);
      CompileResult result;
      bool? _IgnoreFile;

      public override ICollection VirtualPathDependencies {
         get {

            if (result != null) {
               return
                  new[] { VirtualPath }.Concat(
                     from u in result.Dependencies
                     let rel = applicationUri.MakeRelativeUri(u)
                     where !rel.IsAbsoluteUri
                        && !rel.OriginalString.StartsWith("..")
                     let vp = VirtualPathUtility.ToAbsolute("~/" + rel.OriginalString)
                     select vp)
                  .ToArray();
            }

            return base.VirtualPathDependencies;
         }
      }

      protected override string GeneratedTypeNamePrefix => "_Page_";

      protected override bool IgnoreFile {
         get {
            return _IgnoreFile
               ?? (_IgnoreFile = VirtualPath.Split('/').Last()[0] == '_').Value;
         }
      }

      protected override string ParsePath() {

         using (Stream source = OpenStream()) {

            XcstCompiler compiler = XcstWebConfiguration.Instance
               .CompilerFactory
               .CreateCompiler();

            ConfigureCompiler(compiler);

            this.result = compiler.Compile(source, baseUri: this.PhysicalPath);

            return this.result.Language;
         }
      }

      protected virtual void ConfigureCompiler(XcstCompiler compiler) {

         if (this.IsFileInCodeDir) {
            compiler.LibraryPackage = true;
         } else {
            compiler.SetTargetBaseTypes(typeof(TPage));
            compiler.TargetNamespace = this.GeneratedTypeNamespace;
            compiler.TargetClass = this.GeneratedTypeName;
         }

         compiler.UseLineDirective = true;

         compiler.SetParameter(
            new QualifiedName("application-uri", XmlNamespaces.XcstApplication),
            this.applicationUri
         );
      }

      protected override IEnumerable<CodeCompileUnit> BuildCompileUnits() {

         // The 'Show Complete Compilation Source' feature of the ASP.NET server error page
         // shows the last compile unit. Returning IFileDependent partial first.

         if (!this.IsFileInCodeDir
            && !typeof(IFileDependent).IsAssignableFrom(typeof(TPage))) {

            yield return FileDependentPartial();
         }

         foreach (string unit in this.result.CompilationUnits) {
            yield return new CodeSnippetCompileUnit(unit);
         }
      }

      CodeCompileUnit FileDependentPartial() {

         var fileArray = new CodeArrayCreateExpression(typeof(string));
         fileArray.Initializers.Add(new CodePrimitiveExpression(this.PhysicalPath.LocalPath));

         foreach (Uri uri in this.result.Dependencies.Where(u => u.IsFile)) {
            fileArray.Initializers.Add(new CodePrimitiveExpression(uri.LocalPath));
         }

         return new CodeCompileUnit {
            Namespaces = {
               new CodeNamespace(this.GeneratedTypeNamespace) {
                  Types = {
                     new CodeTypeDeclaration(this.GeneratedTypeName) {
                        IsClass = true,
                        IsPartial = true,
                        BaseTypes = {
                           new CodeTypeReference(typeof(IFileDependent))
                        },
                        Members = {
                           new CodeMemberProperty {
                              Name = "FileDependencies",
                              PrivateImplementationType = new CodeTypeReference(typeof(IFileDependent)),
                              Type = new CodeTypeReference(fileArray.CreateType, 1),
                              GetStatements = {
                                 new CodeMethodReturnStatement(fileArray)
                              }
                           }
                        }
                     }
                  }
               }
            }
         };
      }

      public override void GenerateCode(AssemblyBuilder assemblyBuilder) {

         if (this.IgnoreFile) {
            return;
         }

         base.GenerateCode(assemblyBuilder);

         assemblyBuilder.AddAssemblyReference(typeof(Xcst.IXcstPackage).Assembly);

         if (!this.IsFileInCodeDir) {
            assemblyBuilder.AddAssemblyReference(typeof(TPage).Assembly);
            assemblyBuilder.GenerateTypeFactory(this.GeneratedTypeFullName);
         }
      }
   }
}
