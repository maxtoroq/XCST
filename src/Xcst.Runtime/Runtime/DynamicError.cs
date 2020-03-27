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

namespace Xcst.Runtime {

   /// <exclude/>
   public static class DynamicError {

      internal static QualifiedName
      Code(string code) => new QualifiedName(code, XmlNamespaces.XcstErrors);

      public static Exception
      UnknownTemplate(QualifiedName templateName) {

         if (templateName is null) throw new ArgumentNullException(nameof(templateName));

         return new RuntimeException($"No template exists named {templateName.ToString()}.", Code("XTDE0040"));
      }

      public static Exception
      RequiredGlobalParameter(string parameterName) {

         if (parameterName is null) throw new ArgumentNullException(nameof(parameterName));

         return new RuntimeException($"No value supplied for required parameter '{parameterName}'.", Code("XTDE0050"));
      }

      public static Exception
      RequiredTemplateParameter(string parameterName) {

         if (parameterName is null) throw new ArgumentNullException(nameof(parameterName));

         return new RuntimeException($"No value supplied for required parameter '{parameterName}'.", Code("XTDE0700"));
      }

      public static Exception
      InvalidParameterCast(string parameterName) {

         if (parameterName is null) throw new ArgumentNullException(nameof(parameterName));

         return new RuntimeException($"Couldn't cast parameter '{parameterName}' to the required type.", Code("XTTE0590"));
      }

      public static Exception
      UnknownOutputDefinition(QualifiedName outputName) {

         if (outputName is null) throw new ArgumentNullException(nameof(outputName));

         return new RuntimeException($"No output definition exists named {outputName.ToString()}.", Code("XTDE1460"));
      }

      public static Exception
      Terminate(string message) => new RuntimeException(message, Code("XTMM9000"));

      public static Exception
      InferMethodIsNotMeantToBeCalled() => new RuntimeException("Infer method is not meant to be called.");
   }
}
