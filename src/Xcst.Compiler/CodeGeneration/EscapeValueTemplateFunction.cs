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
using System.Linq;
using Saxon.Api;
using static Xcst.Compiler.XcstCompiler;

namespace Xcst.Compiler.CodeGeneration {

   class EscapeValueTemplateFunction : ExtensionFunctionDefinition {

      public override QName FunctionName { get; } = CompilerQName("escape-value-template");

      public override XdmSequenceType[] ArgumentTypes { get; } = {
         new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_STRING), ' ')
      };

      public override int MinimumNumberOfArguments => 1;

      public override int MaximumNumberOfArguments => MinimumNumberOfArguments;

      public override ExtensionFunctionCall MakeFunctionCall() {
         return new FunctionCall();
      }

      public override XdmSequenceType ResultType(XdmSequenceType[] ArgumentTypes) {
         return new XdmSequenceType(XdmAtomicType.BuiltInAtomicType(QName.XS_STRING), ' ');
      }

      class FunctionCall : ExtensionFunctionCall {

         public override IXdmEnumerator Call(IXdmEnumerator[] arguments, DynamicContext context) {

            XdmAtomicValue value = arguments[0].AsAtomicValues().Single();

            return ValueTemplateEscaper.EscapeValueTemplate(value.ToString())
               .ToXdmAtomicValue()
               .GetXdmEnumerator();
         }
      }
   }
}
