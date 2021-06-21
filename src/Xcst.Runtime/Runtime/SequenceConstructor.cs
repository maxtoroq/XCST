// Copyright 2021 Max Toro Q.
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

using System.Collections.Generic;
using System.Diagnostics;

namespace Xcst.Runtime {

   static class SequenceConstructor {

      public static void
      BeginTrack(char cardinality, ref Stack<State>? _trackStack) {

         _trackStack ??= new Stack<State>();
         _trackStack.Push(new State(cardinality, false, false));
      }

      public static void
      OnItemWritten(Stack<State>? _trackStack) {

         State state;

         if (_trackStack != null
            && _trackStack.Count > 0) {

            state = _trackStack.Peek();

            if (!state.ItemWritten) {

               Debug.Assert(!state.EndReached);

               _trackStack.Pop();
               _trackStack.Push(new State(state.Cardinality, true, false));
               return;
            }

            if (state.Cardinality == ' ') {
               throw DynamicError.SequenceOverflow();
            }
         }
      }

      public static bool
      OnEmpty(Stack<State>? _trackStack) =>
         !_trackStack!.Peek().ItemWritten;

      public static void
      EndOfConstructor(Stack<State>? _trackStack) {

         if (_trackStack is null) {
            // See c:return
            return;
         }

         State state = _trackStack!.Pop();

         Debug.Assert(!state.EndReached);

         _trackStack.Push(new State(state.Cardinality, state.ItemWritten, true));
      }

      public static void
      EndTrack(Stack<State>? _trackStack) {

         State state = _trackStack!.Pop();

         if (!state.ItemWritten
            && state.Cardinality != '*'
            && state.EndReached) {

            throw DynamicError.SequenceUnderflow();
         }

         if (state.ItemWritten
            && _trackStack.Count > 0) {

            State parentState = _trackStack.Pop();

            Debug.Assert(!parentState.EndReached);

            _trackStack.Push(new State(parentState.Cardinality, true, false));
         }
      }

      public struct State {

         public char
         Cardinality { get; }

         public bool
         ItemWritten { get; }

         public bool
         EndReached { get; }

         public
         State(char cardinality, bool itemWritten, bool endReached) {
            this.Cardinality = cardinality;
            this.ItemWritten = itemWritten;
            this.EndReached = endReached;
         }
      }
   }
}
