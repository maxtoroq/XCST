#region GetHashCodeDeterministic is based on code from Sandcastle Help File Builder
//===============================================================================================================
// System  : Sandcastle Tools - Sandcastle Tools Core Class Library
// File    : ComponentUtilities.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/31/2021
// Note    : Copyright 2007-2021, Eric Woodruff, All rights reserved
//
// This file contains a class containing properties and methods used to locate and work with build components,
// plug-ins, syntax generators, and presentation styles.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://GitHub.com/EWSoftware/SHFB.  This
// notice, the author's name, and all copyright notices must remain intact in all applications, documentation,
// and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 11/01/2007  EFW  Created the code
// 10/06/2008  EFW  Changed the default location of custom components
// 07/04/2009  EFW  Merged build component and plug-in folder
// 11/10/2009  EFW  Added support for custom syntax filter components
// 03/07/2010  EFW  Added support for SHFBCOMPONENTROOT
// 12/17/2013  EFW  Removed the SandcastlePath property and all references to it.  Updated to use MEF to load
//                  plug-ins.
// 12/20/2013  EFW  Updated to use MEF to load the syntax filters and removed support for SHFBCOMPONENTROOT
// 12/26/2013  EFW  Updated to use MEF to load BuildAssembler build components
// 01/02/2014  EFW  Moved the component manager class to Sandcastle.Core
// 08/05/2014  EFW  Added support for getting a list of syntax generator resource item files
//===============================================================================================================
#endregion

using System;

namespace Xcst.Compiler;

partial class XcstCompilerPackage {

   /// <summary>
   /// This returns a deterministic hash code that is the same in the full .NET Framework and in .NET Core
   /// in every session given the same string to hash.
   /// </summary>
   /// <param name="hashString">The string to hash</param>
   /// <returns>The deterministic hash code</returns>
   /// <remarks>The hashing algorithm differs in .NET Core and returns different hash codes for each session.
   /// This was done for security to prevent DoS attacks. For the help file builder, we're just using it to
   /// generate a short filenames or other constant IDs.  As such, we need a deterministic hash code to keep
   /// generating the same hash code for the same IDs in all sessions regardless of platform so that the
   /// filenames and other IDs stay the same for backward compatibility.</remarks>
   static int
   GetHashCodeDeterministic(string hashString) {

      if (hashString is null) throw new ArgumentNullException(nameof(hashString));

      // This is equivalent to the .NET Framework hashing algorithm but doesn't use unsafe code.  It
      // will generate the same value as the .NET Framework version given the same string.
      unchecked {

         int hash1 = (5381 << 16) + 5381;
         int hash2 = hash1;

         int len = hashString.Length, i = 0, h1, h2;

         while (len > 2) {

            h1 = (hashString[i + 1] << 16) + hashString[i];
            h2 = 0;

            if (len >= 3) {

               if (len >= 4) {
                  h2 = hashString[i + 3] << 16;
               }

               h2 += hashString[i + 2];
            }

            hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ h1;
            hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ h2;

            i += 4;
            len -= 4;
         }

         if (len > 0) {

            h1 = 0;

            if (len >= 2) {
               h1 = hashString[i + 1] << 16;
            }

            h1 += hashString[i];

            hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ h1;
         }

         return hash1 + (hash2 * 1566083941);
      }
   }
}
