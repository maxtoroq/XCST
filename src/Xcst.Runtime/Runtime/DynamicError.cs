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
using System.Xml.Linq;

namespace Xcst.Runtime;

/// <exclude/>
public static class DynamicError {

   internal static XName
   Code(string code) => XName.Get(code, XmlNamespaces.XcstErrors);

   public static Exception
   UnknownTemplate(XName templateName) {

      if (templateName is null) throw new ArgumentNullException(nameof(templateName));

      return new RuntimeException(
         $"No template exists named {DataType.QNameString(templateName)}.", Code("XTDE0040"));
   }

   public static Exception
   RequiredGlobalParameter(string parameterName) {

      if (parameterName is null) throw new ArgumentNullException(nameof(parameterName));

      return new RuntimeException(
         $"No value supplied for required parameter '{parameterName}'.", Code("XTDE0050"));
   }

   public static Exception
   RequiredTemplateParameter(string parameterName) {

      if (parameterName is null) throw new ArgumentNullException(nameof(parameterName));

      return new RuntimeException(
         $"No value supplied for required parameter '{parameterName}'.", Code("XTDE0700"));
   }

   public static Exception
   InvalidParameterCast(string parameterName) {

      if (parameterName is null) throw new ArgumentNullException(nameof(parameterName));

      return new RuntimeException(
         $"Couldn't cast parameter '{parameterName}' to the required type.", Code("XTTE0590"));
   }

   public static Exception
   UnknownOutputDefinition(XName outputName) {

      if (outputName is null) throw new ArgumentNullException(nameof(outputName));

      return new RuntimeException(
         $"No output definition exists named {DataType.QNameString(outputName)}.", Code("XTDE1460"));
   }

   public static Exception
   Terminate(
         string message,
         string defaultMessage,
         XName? errorCode = null,
         object? errorData = null) =>
      new RuntimeException(
         (!String.IsNullOrEmpty(message) ? message : defaultMessage),
         errorCode ?? Code("XTMM9000"),
         errorData
      );

   public static Exception
   InferMethodIsNotMeantToBeCalled() =>
      new RuntimeException("Infer method is not meant to be called.");

   public static Exception
   UnknownMode(XName? mode) =>
      new RuntimeException($"The mode '{(mode != null ? DataType.UriQualifiedName(mode) : null)}' does not exist.", Code("XCST9103"));

   public static Exception
   AbsentCurrentTemplateRule() =>
      new RuntimeException("The current template rule is absent.", Code("XTDE0560"));

   internal static Exception
   SequenceOverflow() =>
      new RuntimeException("A sequence of more than one item is not allowed.", Code("XTTE0505"));

   internal static Exception
   SequenceUnderflow() =>
      new RuntimeException("An empty sequence is not allowed.", Code("XTTE0505"));
}
