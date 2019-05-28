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
using System.Runtime.Serialization;

namespace Xcst.Compiler {

   [Serializable]
   public class CompileException : Exception {

      readonly QualifiedName
      _ErrorCode;

      readonly string
      _ModuleUri;

      readonly int
      _LineNumber;

      public QualifiedName
      ErrorCode => _ErrorCode;

      public string
      ModuleUri => _ModuleUri;

      public int
      LineNumber => _LineNumber;

      public
      CompileException(string message,
         QualifiedName errorCode = null,
         string moduleUri = null,
         int lineNumber = -1) : base(message) {

         _ErrorCode = errorCode;
         _ModuleUri = moduleUri;
         _LineNumber = lineNumber;
      }

      protected
      CompileException(SerializationInfo info, StreamingContext context)
         : base(info, context) { }
   }
}
