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

      readonly string?
      _errorCode;

      readonly string?
      _moduleUri;

      readonly int
      _lineNumber;

      public string?
      ErrorCode => _errorCode;

      public string?
      ModuleUri => _moduleUri;

      public int
      LineNumber => _lineNumber;

      public
      CompileException(string message,
         string? errorCode = null,
         string? moduleUri = null,
         int lineNumber = -1) : base(message) {

         _errorCode = errorCode;
         _moduleUri = moduleUri;
         _lineNumber = lineNumber;
      }

      protected
      CompileException(SerializationInfo info, StreamingContext context)
         : base(info, context) { }
   }
}
