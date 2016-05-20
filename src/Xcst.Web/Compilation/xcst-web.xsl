<?xml version="1.0" encoding="utf-8"?>
<!--
 Copyright 2015 Max Toro Q.

 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
-->
<stylesheet version="2.0" exclude-result-prefixes="#all"
   xmlns="http://www.w3.org/1999/XSL/Transform"
   xmlns:xs="http://www.w3.org/2001/XMLSchema"
   xmlns:c="http://maxtoroq.github.io/XCST"
   xmlns:web="http://maxtoroq.github.io/XCST/web"
   xmlns:src="http://maxtoroq.github.io/XCST/compiled">

   <param name="web:application-uri" as="xs:anyURI"/>

   <template match="c:module | c:package" mode="src:import-namespace-extra">
      <param name="class" tunnel="yes"/>
      <param name="library-package" tunnel="yes"/>

      <next-match/>
      <if test="not($library-package)">
         <call-template name="src:new-line-indented"/>
         <text>using static </text>
         <value-of select="$class, web:functions-type-name(.)" separator="."/>
         <value-of select="$src:statement-delimiter"/>
      </if>
   </template>

   <template match="c:module | c:package" mode="src:infrastructure-extra">
      <param name="indent" tunnel="yes"/>
      <param name="library-package" tunnel="yes"/>

      <next-match/>
      <if test="not($library-package)">
         <variable name="module-uri" select="document-uri(root(.))"/>
         <variable name="functions-type" select="web:functions-type-name(.)"/>
         <value-of select="$src:new-line"/>
         <call-template name="src:new-line-indented"/>
         <text>internal static class </text>
         <value-of select="$functions-type"/>
         <call-template name="src:open-brace"/>
         <value-of select="$src:new-line"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>static readonly string BasePath = </text>
         <value-of select="src:global-identifier('System.Web.VirtualPathUtility')"/>
         <text>.ToAbsolute(</text>
         <value-of select="src:verbatim-string(concat('~/', src:make-relative-uri($web:application-uri, $module-uri)))"/>
         <text>)</text>
         <value-of select="$src:statement-delimiter"/>
         <value-of select="$src:new-line"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>public static string Href(string path, params object[] pathParts)</text>
         <call-template name="src:open-brace"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="2"/>
         </call-template>
         <text>return </text>
         <value-of select="src:global-identifier('Xcst.Web.Runtime'), 'UrlUtil'" separator="."/>
         <text>.GenerateClientUrl(</text>
         <value-of select="$functions-type, 'BasePath'" separator="."/>
         <text>, path, pathParts)</text>
         <value-of select="$src:statement-delimiter"/>
         <call-template name="src:close-brace">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
         <call-template name="src:close-brace"/>
      </if>
   </template>

   <function name="web:functions-type-name">
      <param name="module" as="element()"/>

      <sequence select="concat('__xcst_functions_', generate-id($module))"/>
   </function>

</stylesheet>