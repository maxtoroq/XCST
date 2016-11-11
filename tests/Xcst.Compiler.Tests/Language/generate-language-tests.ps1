﻿param(
   $Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

$singleIndent = "   "
$indent = ""

function PushIndent {
   $script:indent = $indent + $singleIndent
}

function PopIndent {
   $script:indent = $indent.Substring($singleIndent.Length)
}

function WriteLine($line = "") {
   $indent + $line
}

function GenerateTests {

   Add-Type -Path ..\..\..\src\Xcst.Compiler\bin\$Configuration\Xcst.Compiler.dll

   $compilerFactory = New-Object Xcst.Compiler.XcstCompilerFactory
   $startDirectory = Get-Item .

@"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using static Xcst.Compiler.Tests.Language.LanguageTestsHelper;

namespace Xcst.Compiler.Tests {
"@
   PushIndent
   GenerateTestsForDirectory $startDirectory $startDirectory.Name
   PopIndent
   "}"
}

function GenerateTestsForDirectory([IO.DirectoryInfo]$directory, $category) {

   foreach ($file in ls $directory.FullName *.pxcst) {

      $fileStream = [IO.File]::OpenRead($file.FullName)

      try {

         $compiler = $compilerFactory.CreateCompiler()
         $compiler.TargetNamespace = $directory.Name
         $compiler.LibraryPackage = $true
         $compiler.IndentChars = $singleIndent

         $xcstResult = $compiler.Compile($fileStream, (New-Object Uri $file.FullName))

         foreach ($src in $xcstResult.CompilationUnits) {
            WriteLine $src
         }

      } finally {
         
         $fileStream.Dispose()
      }
   }

   WriteLine
   WriteLine "namespace $($directory.Name) {"
   PushIndent

   $tests = ls $directory.FullName *.xcst

   if ($tests.Length -gt 0) {
   
      WriteLine
      WriteLine "[TestClass]"
      WriteLine "public class $($directory.Name)Tests {"
      PushIndent

      foreach ($file in $tests) { 

         $fileName = [IO.Path]::GetFileNameWithoutExtension($file.Name)

         if ($fileName[0] -eq '_') {
            continue
         }

         $correct = $fileName.EndsWith(".c")
         $fileName2 = $fileName.Substring(0, $fileName.LastIndexOf("."))
         $testName = [Text.RegularExpressions.Regex]::Replace($fileName.Replace(".", "_").Replace("-", "_"), '([a-z])([A-Z])', '$1_$2')

         WriteLine
         WriteLine "#line 1 ""$($file.FullName)"""
         WriteLine "[TestMethod, TestCategory(""$category"")]"

         if (!$correct) {
            WriteLine "[ExpectedException(typeof(Xcst.Compiler.CompileException))]"
         }

         WriteLine "public void $testName() {"
         PushIndent
         WriteLine
         WriteLine "var result = CompileFromFile(@""$($file.FullName)"", correct: $($correct.ToString().ToLower()));"
         WriteLine "var moduleType = result.Item1;"

         if ($correct) {

            foreach ($testCase in ls $directory.FullName "$fileName2.*.xml") {

               $testFileName = [IO.Path]::GetFileNameWithoutExtension($testCase.Name)

               if ($testFileName.EndsWith(".p") -or $testFileName.EndsWith(".f")) {

                  WriteLine "Is$($testFileName.EndsWith(".p"))(OutputEqualsToDoc(moduleType, @""$($testCase.FullName)""));"
               }
            }

            WriteLine
            WriteLine "if (result.Item2.Templates.Contains(new QualifiedName(""expected""))) {"
            PushIndent
            WriteLine "IsTrue(OutputEqualsToExpected(moduleType));"
            PopIndent
            WriteLine "}"
         }

         PopIndent
         WriteLine "}"
      }

      PopIndent
      WriteLine "}"
   }

   foreach ($subDirectory in ls $directory.FullName -Directory) {
      GenerateTestsForDirectory $subDirectory ($category + "." + $subDirectory.Name)
   }

   PopIndent
   WriteLine "}"
}

try {

   GenerateTests | Out-File LanguageTests.generated.cs -Encoding utf8

} finally {
   Pop-Location
}