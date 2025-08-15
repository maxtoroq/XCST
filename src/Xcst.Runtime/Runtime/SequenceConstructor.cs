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

namespace Xcst.Runtime;

static class SequenceConstructor {

   public record struct State(char Cardinality, int Depth, bool ItemWritten, bool EndReached);

   public static void
   BeginTrack(char cardinality, int depth, ref Stack<State>? _trackStack) {

      _trackStack ??= new Stack<State>();
      _trackStack.Push(new State(cardinality, depth, false, false));
   }

   public static void
   OnItemWritting(Stack<State>? _trackStack, int depth) {

      if (_trackStack is { Count: > 0 }) {

         var state = _trackStack.Peek();

         if (state.Depth != depth) {
            return;
         }

         if (state is { ItemWritten: true, Cardinality: ' ' }) {
            throw DynamicError.SequenceOverflow();
         }
      }
   }

   public static void
   OnItemWritten(Stack<State>? _trackStack, int depth) {

      if (_trackStack is { Count: > 0 }) {

         var state = _trackStack.Peek();

         if (state.Depth != depth) {
            return;
         }

         if (!state.ItemWritten) {
            _trackStack.Pop();
            _trackStack.Push(state with { ItemWritten = true });
         }
      }
   }

   public static bool
   OnEmpty(Stack<State>? _trackStack) =>
      !_trackStack!.Peek().ItemWritten;

   public static void
   EndOfConstructor(Stack<State>? _trackStack) {

      if (_trackStack is null or { Count: 0 }) {
         // See c:return
         return;
      }

      var state = _trackStack!.Pop();

      Debug.Assert(!state.EndReached);

      _trackStack.Push(state with { EndReached = true });
   }

   public static void
   EndTrack(Stack<State>? _trackStack) {

      var state = _trackStack!.Pop();

      if (state is { ItemWritten: false, Cardinality: not '*', EndReached: true }) {
         throw DynamicError.SequenceUnderflow();
      }

      if (state.ItemWritten
         && _trackStack.Count > 0) {

         var parentState = _trackStack.Pop();

         Debug.Assert(!parentState.EndReached);

         _trackStack.Push(parentState with { ItemWritten = true });
      }
   }
}
