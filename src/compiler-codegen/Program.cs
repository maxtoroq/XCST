﻿using System;
using System.IO;
using System.Linq;
using Xcst;
using Xcst.Compiler;

namespace XcstCodeGen;

class Program {

   public required Uri
   ProjectUri { get; init; }

   public required string[]
   SourceFiles { get; init; }

   // Show compilation errors on Visual Studio's Error List
   // Also makes the error on the Output window clickable
   static void
   VisualStudioErrorLog(RuntimeException ex) {

      dynamic? errorData = ex.ErrorData;

      if (errorData != null) {

         string uriString = errorData.ModuleUri;
         string path = (Uri.TryCreate(uriString, UriKind.Absolute, out var uri) && uri.IsFile) ?
            uri.LocalPath
            : uriString;

         Console.WriteLine($"{path}({errorData.LineNumber}): XCST error {ex.ErrorCode}: {ex.Message}");
      }
   }

   void
   Run(TextWriter output) {

      var startUri = new Uri(ProjectUri, ".");

      var compiler = new XcstCompiler {
         PackageFileDirectory = startUri.LocalPath,
         NullableAnnotate = true,
         NullableContext = "enable",
         IndentChars = "   ",
         CompilationUnitHandler = href => output
      };

      output.WriteLine("//------------------------------------------------------------------------------");
      output.WriteLine("// <auto-generated>");
      output.WriteLine("//     This code was generated by a tool.");
      output.WriteLine("//");
      output.WriteLine("//     Changes to this file may cause incorrect behavior and will be lost if");
      output.WriteLine("//     the code is regenerated.");
      output.WriteLine("// </auto-generated>");
      output.WriteLine("//------------------------------------------------------------------------------");

      foreach (var file in SourceFiles) {

         var fileUri = new Uri(file, UriKind.Absolute);
         var fileName = Path.GetFileName(file);

         // Ignore files starting with underscore
         if (fileName[0] == '_') {
            continue;
         }

         try {
            compiler.Compile(fileUri);

         } catch (RuntimeException ex) {
            VisualStudioErrorLog(ex);
            throw;
         }
      }
   }

   public static void
   Main(string[] args) {

      var currentDir = Environment.CurrentDirectory;

      if (currentDir[^1] != Path.DirectorySeparatorChar) {
         currentDir += Path.DirectorySeparatorChar;
      }

      var callerBaseUri = new Uri(currentDir, UriKind.Absolute);
      var projectUri = new Uri(callerBaseUri, args[0]);
      var outputUri = new Uri(projectUri, args[1]);

      using var output = File.CreateText(outputUri.LocalPath);

      // Because XML parsers normalize CRLF to LF,
      // we want to be consistent with the additional content we create
      output.NewLine = "\n";

      var program = new Program {
         ProjectUri = projectUri,
         SourceFiles = args.Skip(2)
            .Select(p => new Uri(projectUri, p).LocalPath)
            .ToArray()
      };

      program.Run(output);
   }
}
