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
   public static class Diagnostics {

      public static Exception AssertFail(object value, QualifiedName errorCode = null) {
         return new RuntimeException(ValueToString(value), errorCode ?? DynamicError.Code("XTMM9001"), value);
      }

      public static Exception Message(object value, bool terminate, QualifiedName errorCode = null) {

         // TODO: log message

         if (terminate) {
            return new RuntimeException(ValueToString(value), errorCode ?? DynamicError.Code("XTMM9000"), value);
         }

         return null;
      }

      static string ValueToString(object value) {

         if (value == null) {
            return "Error signaled by application.";
         }

         return Convert.ToString(value);
      }
   }
}