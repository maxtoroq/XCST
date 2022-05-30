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
using System.Xml.Linq;
using Xcst.Runtime;

namespace Xcst;

[Serializable]
public class RuntimeException : Exception {

   static readonly XName
   _defaultCode = DynamicError.Code("XTDE0000");

   readonly XName?
   _errorCode;

   readonly object?
   _errorData;

   public XName
   ErrorCode => _errorCode ?? _defaultCode;

   public object?
   ErrorData => _errorData;

   public
   RuntimeException(string message, XName? errorCode = null, object? errorData = null)
      : base(message) {

      _errorCode = errorCode;
      _errorData = errorData;
   }

   protected
   RuntimeException(SerializationInfo info, StreamingContext context)
      : base(info, context) { }
}
