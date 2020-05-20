using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Xcst.Compiler;

namespace Xcst.Tests.API.Compilation.Extensibility {

   [TestFixture]
   public partial class ExtensibilityTests {

      const string
      TestCategory = nameof(API) + "." + nameof(Compilation) + "." + nameof(Extensibility);

      [Test]
      [Category(TestCategory)]
      public void
      External_Function() {

         const string extNs = "http://localhost/ns/ext";

         var factory = new XcstCompilerFactory {
            EnableExtensions = true
         };

         factory.RegisterExtension(new Uri(extNs), extensionLoader);

         var compiler = factory.CreateCompiler();
         compiler.TargetClass = "FooPackage";
         compiler.TargetNamespace = typeof(ExtensibilityTests).Namespace;

         compiler.SetParameter(extNs, "make-relative-uri", new Func<Uri, Uri, Uri>(makeRelativeUri));

         var module = new StringReader(@"
<c:package version='1.0' language='C#' xmlns:c='http://maxtoroq.github.io/XCST'>
</c:package>
");

         CompileResult result = compiler.Compile(module, baseUri: new Uri("http://localhost"));

         static Stream extensionLoader() =>
            new MemoryStream(new UTF8Encoding(false).GetBytes(@"
<stylesheet version='2.0'
      xmlns='http://www.w3.org/1999/XSL/Transform'
      xmlns:xs='http://www.w3.org/2001/XMLSchema'
      xmlns:c='http://maxtoroq.github.io/XCST'
      xmlns:src='http://maxtoroq.github.io/XCST/compiled'
      xmlns:ext='http://localhost/ns/ext'>
   
   <param name='ext:make-relative-uri' required='yes'/>
   
   <template match='c:module | c:package' mode='src:import-namespace-extra'>
      <variable name='current-uri' select='xs:anyURI(&quot;http://localhost/foo/bar&quot;)'/>
      <variable name='compare-uri' select='xs:anyURI(&quot;http://localhost/baz&quot;)'/>
      <if test='src:invoke-external-function($ext:make-relative-uri, ($current-uri, $compare-uri)) ne xs:anyURI(&quot;../baz&quot;)'>
         <message terminate='yes'>test failed</message>
      </if>
      <next-match/>
   </template>
</stylesheet>
"));

         static Uri makeRelativeUri(Uri current, Uri compare) =>
            current.MakeRelativeUri(compare);
      }
   }
}
