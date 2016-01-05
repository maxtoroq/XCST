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
   xmlns:src="http://maxtoroq.github.io/XCST/compiled"
   xmlns:xcst="http://maxtoroq.github.io/XCST/syntax"
   xmlns:err="http://maxtoroq.github.io/XCST/errors">

   <import href="xcst-metadata.xsl"/>
   <import href="xcst-core.xsl"/>

   <param name="src:namespace" as="xs:string"/>
   <param name="src:class" as="xs:string"/>
   <param name="src:base-types" as="xs:string*"/>
   <param name="src:alternate-first-base-type" as="xs:string?"/>
   <param name="src:alternate-first-base-type-if-exists-type" as="xs:string?"/>

   <variable name="src:context-field" select="concat('this.', src:aux-variable('execution_context'))"/>

   <output cdata-section-elements="src:compilation-unit"/>

   <template match="document-node()" mode="src:main">
      <apply-templates select="(*, node())[1]" mode="#current"/>
   </template>

   <template match="c:module" mode="src:main">

      <call-template name="xcst:check-document-element-attributes"/>
      <apply-templates mode="xcst:check-top-level"/>

      <variable name="modules" as="element(c:module)+">
         <apply-templates select="." mode="src:load-modules">
            <with-param name="language" select="string(@language)" tunnel="yes"/>
         </apply-templates>
      </variable>

      <src:program language="{@language}">
         <for-each select="$modules[position() lt last()]">
            <src:import href="{document-uri(root())}"/>
         </for-each>
         <for-each-group select="for $m in reverse($modules) return $m/c:param" group-by="@name/xcst:name(.)">
            <src:param>
               <copy-of select="namespace::*, @* except @value"/>
            </src:param>
         </for-each-group>
         <for-each-group select="for $m in reverse($modules) return $m/c:template" group-by="resolve-QName(@name, .)">
            <src:template>
               <copy-of select="namespace::*, @name, @*[namespace-uri() ne '']"/>
               <for-each select="c:param">
                  <src:param>
                     <copy-of select="namespace::*, @* except @value"/>
                  </src:param>
               </for-each>
            </src:template>
         </for-each-group>
         <for-each-group select="for $m in reverse($modules) return $m/c:type" group-by="@name/xcst:name(.)">
            <src:type>
               <copy-of select="@name"/>
            </src:type>
         </for-each-group>
         <call-template name="src:compilation-units">
            <with-param name="modules" select="$modules" tunnel="yes"/>
         </call-template>
      </src:program>
   </template>

   <template match="c:*" mode="src:main">
      <sequence select="error(xs:QName('err:XTSE0010'), concat('Unknown XCST element: ', local-name(), '.'), src:error-object(.))"/>
   </template>

   <template match="*[not(self::c:*)]" mode="src:main">
      <call-template name="xcst:check-document-element-attributes"/>
      <sequence select="error((), 'Simplified module not implemented yet.', src:error-object(.))"/>
   </template>

   <template match="node()" mode="src:main">
      <sequence select="error((), 'Expecting element.', src:error-object(.))"/>
   </template>

   <template match="c:module" mode="src:load-modules">
      <param name="language" tunnel="yes"/>

      <call-template name="xcst:check-document-element-attributes"/>
      <apply-templates mode="xcst:check-top-level"/>

      <if test="upper-case(string(@language)) ne upper-case($language)">
         <sequence select="error(xs:QName('err:XTSE0020'), 'Imported modules must declare the same @language as the principal module.', src:error-object(.))"/>
      </if>

      <apply-templates select="c:import" mode="#current"/>
      <sequence select="."/>
   </template>

   <template match="c:import" mode="src:load-modules">
      <param name="module-docs" select="root()" as="document-node()*" tunnel="yes"/>

      <variable name="href" select="resolve-uri(@href, base-uri())"/>

      <if test="not(doc-available($href))">
         <sequence select="error(xs:QName('err:XTSE0165'), 'Cannot retrieve imported module.', src:error-object(.))"/>
      </if>

      <variable name="imported" select="doc($href)"/>

      <if test="some $m in $module-docs satisfies $m is $imported">
         <sequence select="error(xs:QName('err:XTSE0210'), 'A module cannot directly or indirectly import itself.', src:error-object(.))"/>
      </if>

      <if test="not($imported/c:module)">
         <sequence select="error(xs:QName('err:XTSE0165'), 'Expecting &lt;c:module> element.', src:error-object(.))"/>
      </if>

      <apply-templates select="$imported/c:module" mode="#current">
         <with-param name="module-docs" select="$module-docs, $imported" tunnel="yes"/>
      </apply-templates>
   </template>

   <template name="src:compilation-units">
      <param name="modules" as="element(c:module)+" tunnel="yes"/>

      <src:compilation-unit>
         <apply-templates select="$modules" mode="src:namespace">
            <with-param name="indent" select="0" tunnel="yes"/>
         </apply-templates>
      </src:compilation-unit>
   </template>

   <template name="xcst:check-document-element-attributes">

      <variable name="attr-name" select="if (self::c:*) then QName('', 'language') else xs:QName('c:language')"/>
      <variable name="language-attr" select="@*[node-name() eq $attr-name]"/>

      <if test="not($language-attr)">
         <sequence select="error(xs:QName('err:XTSE0010'), concat('Element must have a @', $attr-name,' attribute.'), src:error-object(.))"/>
      </if>

      <if test="not(upper-case(string($language-attr)) eq 'C#')">
         <sequence select="error(xs:QName('err:XTSE0020'), concat('This implementation supports only ''C#'' (@', $attr-name, ' attribute).'), src:error-object(.))"/>
      </if>
   </template>

   <template match="c:use-functions | c:import | c:param | c:variable | c:output | c:validation | c:template | c:function | c:type" mode="xcst:check-top-level"/>

   <template match="c:*" mode="xcst:check-top-level">
      <sequence select="error(xs:QName('err:XTSE0010'), concat('Unknown XCST element: ', local-name(), '.'), src:error-object(.))"/>
   </template>

   <template match="*[not(self::c:*) and namespace-uri()]" mode="xcst:check-top-level"/>

   <template match="*[not(namespace-uri())]" mode="xcst:check-top-level">
      <sequence select="error(xs:QName('err:XTSE0130'), 'Top level elements must have a non-null namespace URI.', src:error-object(.))"/>
   </template>

   <template match="text()[not(normalize-space())]" mode="xcst:check-top-level"/>

   <template match="text()" mode="xcst:check-top-level">
      <sequence select="error(xs:QName('err:XTSE0120'), 'No character data is allowed between top-level elements.', src:error-object(.))"/>
   </template>

   <!--
      ## Declarations
   -->

   <template match="c:module" mode="src:namespace">
      <param name="indent" tunnel="yes"/>

      <value-of select="$src:new-line"/>
      <value-of select="$src:new-line"/>
      <text>namespace </text>
      <value-of select="$src:namespace"/>
      <call-template name="src:open-brace"/>
      <apply-templates select="c:use-functions" mode="src:import-namespace">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <apply-templates select="." mode="src:import-namespace-extra">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <apply-templates select="." mode="src:class">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <call-template name="src:close-brace"/>
   </template>

   <template match="c:module/node()" mode="src:import-namespace-extra"/>

   <template match="c:use-functions" mode="src:import-namespace">
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>using </text>
      <if test="@static-only/xcst:boolean(.)">static </if>
      <if test="@alias">
         <value-of select="@alias"/>
         <text> = </text>
      </if>
      <value-of select="@in"/>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:line-default"/>
   </template>

   <template match="c:module" mode="src:class">
      <param name="modules" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="module-pos" select="
         for $pos in (1 to count($modules)) 
         return if ($modules[$pos] is current()) then $pos else ()"/>
      <variable name="principal-module" select="$module-pos eq count($modules)"/>
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>public partial class </text>
      <value-of select="$src:class"/>
      <if test="$principal-module">
         <text> : </text>
         <variable name="base-types" as="xs:string+">
            <choose>
               <when test="$src:alternate-first-base-type
                  and $src:alternate-first-base-type-if-exists-type
                  and $modules/c:type[@name/xcst:name(.) = $src:alternate-first-base-type-if-exists-type]">
                  <sequence select="$src:alternate-first-base-type, $src:base-types[position() gt 1]"/>
               </when>
               <otherwise>
                  <sequence select="$src:base-types"/>
               </otherwise>
            </choose>
            <sequence select="src:fully-qualified-helper('IXcstExecutable')"/>
         </variable>
         <value-of select="$base-types" separator=", "/>
      </if>
      <call-template name="src:open-brace"/>
      <for-each select="c:param | c:variable">
         <if test="not($modules[position() gt $module-pos]/c:*[local-name() eq current()/local-name() and @name/xcst:name(.) = current()/@name/xcst:name(.)])">
            <apply-templates select="." mode="src:member">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            </apply-templates>
         </if>
      </for-each>
      <apply-templates select="c:template | c:function | c:type" mode="src:member">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>#region Infrastructure</text>
      <if test="$principal-module">
         <apply-templates select="." mode="src:execution-context">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </apply-templates>
      </if>
      <apply-templates select="." mode="src:prime-method">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <if test="$principal-module">
         <apply-templates select="." mode="src:call-template-method">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </apply-templates>
         <apply-templates select="." mode="src:read-output-definition-method">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </apply-templates>
      </if>
      <apply-templates select="." mode="src:infrastructure-extra">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>#endregion </text>
      <call-template name="src:close-brace"/>
   </template>

   <template match="c:module/node()" mode="src:infrastructure-extra"/>

   <template match="c:param | c:variable" mode="src:member">

      <if test="preceding-sibling::*[node-name(.) eq node-name(current()) and @name/xcst:name(.) = current()/@name/xcst:name(.)]">
         <sequence select="error(xs:QName('err:XTSE0630'), 'Duplicate global variable declaration.', src:error-object(.))"/>
      </if>

      <if test="self::c:param and @tunnel/xcst:boolean(.)">
         <sequence select="error(xs:QName('err:XTSE0020'), 'For attribute ''tunnel'' within a c:module parameter, the only permitted values are: ''no'', ''false'', ''0''.', src:error-object(.))"/>
      </if>

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <variable name="text" select="xcst:text(.)"/>
      <choose>
         <when test="@as">
            <value-of select="xcst:type(@as)"/>
         </when>
         <when test="not(@value) and $text">string</when>
         <otherwise>object</otherwise>
      </choose>
      <text> </text>
      <value-of select="xcst:name(@name)"/>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:line-default"/>
   </template>

   <template match="c:template" mode="src:member">
      <param name="indent" tunnel="yes"/>

      <variable name="qname" select="resolve-QName(@name, .)"/>

      <if test="not($qname eq xs:QName('c:initial-template'))
         and xcst:is-reserved-namespace(namespace-uri-from-QName($qname))">
         <sequence select="error(xs:QName('err:XTSE0080'), concat('Namespace prefix ', prefix-from-QName($qname),' refers to a reserved namespace.'), src:error-object(.))"/>
      </if>

      <if test="preceding-sibling::c:template[resolve-QName(@name, .) eq $qname]">
         <sequence select="error(xs:QName('err:XTSE0660'), 'Duplicate named template.', src:error-object(.))"/>
      </if>

      <variable name="context-param" select="src:aux-variable('context')"/>
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>/* </text>
      <value-of select="@name"/>
      <text> */</text>
      <call-template name="src:new-line-indented"/>
      <text>[</text>
      <value-of select="src:global-identifier('System.ComponentModel.EditorBrowsable')"/>
      <text>(</text>
      <value-of select="src:global-identifier('System.ComponentModel.EditorBrowsableState.Never')"/>
      <text>)]</text>
      <call-template name="src:new-line-indented"/>
      <text>void </text>
      <value-of select="src:template-method-name(.)"/>
      <text>(</text>
      <value-of select="src:fully-qualified-helper('DynamicContext'), $context-param"/>
      <text>)</text>
      <call-template name="src:open-brace"/>
      <apply-templates select="c:param" mode="src:statement">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         <with-param name="context-param" select="$context-param" tunnel="yes"/>
      </apply-templates>
      <call-template name="src:apply-children">
         <with-param name="children" select="node()[not(self::c:param or following-sibling::c:param)]"/>
         <with-param name="mode" select="'statement'"/>
         <with-param name="omit-block" select="true()"/>
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         <with-param name="context-param" select="$context-param" tunnel="yes"/>
         <with-param name="output" select="concat($context-param, '.Output')" tunnel="yes"/>
      </call-template>
      <call-template name="src:close-brace"/>
   </template>

   <template match="c:function" mode="src:member">
      <param name="modules" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="current-function" select="."/>
      <variable name="functions" select="
         for $m in reverse($modules) 
         return $m/c:function[@name/xcst:name(.) eq current()/@name/xcst:name(.) and count(c:param) eq count(current()/c:param)]"/>
      <variable name="current-index" select="
         for $pos in (1 to count($functions)) 
         return (if ($functions[$pos] is current()) then $pos else ())"/>

      <value-of select="$src:new-line"/>
      <if test="$current-index gt 1">
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.ComponentModel.EditorBrowsable')"/>
         <text>(</text>
         <value-of select="src:global-identifier('System.ComponentModel.EditorBrowsableState.Never')"/>
         <text>)]</text>
      </if>
      <call-template name="src:new-line-indented"/>
      <value-of select="(@as/xcst:type(.), 'void')[1]"/>
      <text> </text>
      <value-of select="if ($current-index gt 1) then 
         src:overriden-function-method-name(.)
         else xcst:name(@name)"/>
      <text>(</text>
      <for-each select="c:param">
         <if test="preceding-sibling::c:param[@name/xcst:name(.) = current()/@name/xcst:name(.)]">
            <sequence select="error(xs:QName('err:XTSE0580'), 'The name of the parameter is not unique.', src:error-object(.))"/>
         </if>
         <if test="@tunnel/xcst:boolean(.)">
            <sequence select="error(xs:QName('err:XTSE0020'), 'For attribute ''tunnel'' within a c:function parameter, the only permitted values are: ''no'', ''false'', ''0''.', src:error-object(.))"/>
         </if>
         <variable name="text" select="xcst:text(.)"/>
         <variable name="has-value" select="xcst:has-value(., $text)"/>
         <variable name="type" select="
            if (@as) then @as/xcst:type(.)
            else if (not(@value) and $text) then 'string'
            else 'object'
         "/>
         <choose>
            <when test="$has-value">
               <if test="@required/xcst:boolean(.)">
                  <sequence select="error(xs:QName('err:XTSE0020'), 'For attribute ''required'' within a c:function parameter that has either a ''value'' attribute or child element/text, the only permitted values are: ''no'', ''false'', ''0''.', src:error-object(.))"/>
               </if>
            </when>
            <otherwise>
               <if test="@required/not(xcst:boolean(.))">
                  <sequence select="error(xs:QName('err:XTSE0020'), 'For attribute ''required'' within a c:function parameter that does not have either a ''value'' attribute or child element/text, the only permitted values are: ''yes'', ''true'', ''1''.', src:error-object(.))"/>
               </if>
            </otherwise>
         </choose>
         <if test="position() gt 1">, </if>
         <value-of select="$type"/>
         <text> </text>
         <value-of select="xcst:name(@name)"/>
         <if test="$has-value">
            <text> = </text>
            <call-template name="src:value">
               <with-param name="text" select="$text"/>
            </call-template>
         </if>
      </for-each>
      <text>)</text>
      <call-template name="src:apply-children">
         <with-param name="children" select="node()[not(self::c:param or following-sibling::c:param)]"/>
         <with-param name="mode" select="'statement'"/>
         <with-param name="ensure-block" select="true()"/>
         <with-param name="next-function" select="subsequence($functions, $current-index + 1)[1]" tunnel="yes"/>
      </call-template>
   </template>

   <template match="c:type" mode="src:member">
      <param name="modules" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="current-type" select="."/>
      <variable name="types" select="
         for $m in reverse($modules) 
         return $m/c:type[@name/xcst:name(.) eq current()/@name/xcst:name(.)]"/>
      <variable name="current-index" select="
         for $pos in (1 to count($types)) 
         return (if ($types[$pos] is current()) then $pos else ())"/>

      <if test="$current-index eq 1">
         <variable name="validation-definitions" select="
            for $m in reverse($modules) 
            return reverse($m/c:validation)"/>
         <variable name="validation-attributes" as="attribute()*">
            <for-each-group select="for $v in $validation-definitions return $v/@*[not(namespace-uri())]" group-by="node-name(.)">
               <sequence select="."/>
            </for-each-group>
         </variable>
         <value-of select="$src:new-line"/>
         <apply-templates select="c:metadata" mode="src:attribute"/>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>public class </text>
         <value-of select="xcst:name(@name)"/>
         <call-template name="src:open-brace"/>
         <call-template name="src:line-default">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
         <apply-templates select="c:member" mode="#current">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            <with-param name="src:validation-attributes" select="$validation-attributes" tunnel="yes"/>
         </apply-templates>
         <apply-templates select="c:member[not(@as)]" mode="src:anonymous-type">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            <with-param name="src:validation-attributes" select="$validation-attributes" tunnel="yes"/>
         </apply-templates>
         <call-template name="src:close-brace"/>
      </if>
   </template>

   <template match="c:member" mode="src:member">
      <value-of select="$src:new-line"/>
      <call-template name="src:member-attributes"/>
      <apply-templates select="c:metadata" mode="src:attribute"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>public </text>
      <value-of select="(@as/xcst:type(.), src:anonymous-type-name(.))[1]"/>
      <text> </text>
      <value-of select="xcst:name(@name)"/>
      <choose>
         <when test="@expression">
            <text> => </text>
            <value-of select="@expression"/>
            <text>;</text>
         </when>
         <otherwise>
            <text> { get; set; }</text>
            <if test="@value">
               <text> = </text>
               <value-of select="@value"/>
               <text>;</text>
            </if>
         </otherwise>
      </choose>
      <call-template name="src:line-default"/>
   </template>

   <template match="c:member[not(@as)]" mode="src:anonymous-type">
      <param name="indent" tunnel="yes"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>public class </text>
      <value-of select="src:anonymous-type-name(.)"/>
      <call-template name="src:open-brace"/>
      <call-template name="src:line-default"/>
      <apply-templates select="c:member" mode="src:member">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <apply-templates select="c:member[not(@as)]" mode="#current">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <call-template name="src:close-brace"/>
   </template>

   <function name="src:anonymous-type-name" as="xs:string">
      <param name="member" as="element(c:member)"/>

      <sequence select="concat($member/@name/xcst:name(.), '_', generate-id($member))"/>
   </function>

   <!--
      ## Infrastructure
   -->

   <template match="c:module" mode="src:execution-context">
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>[</text>
      <value-of select="src:global-identifier('System.ComponentModel.EditorBrowsable')"/>
      <text>(</text>
      <value-of select="src:global-identifier('System.ComponentModel.EditorBrowsableState.Never')"/>
      <text>)]</text>
      <call-template name="src:new-line-indented"/>
      <value-of select="src:fully-qualified-helper('ExecutionContext')"/>
      <text> </text>
      <value-of select="substring-after($src:context-field, 'this.')"/>
      <value-of select="$src:statement-delimiter"/>
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="src:fully-qualified-helper('ExecutionContext')"/>
      <text> </text>
      <value-of select="src:fully-qualified-helper('IXcstExecutable')"/>
      <text>.Context</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>set { </text>
      <value-of select="$src:context-field"/>
      <text> = value; }</text>
      <call-template name="src:close-brace"/>
   </template>

   <template match="c:module" mode="src:prime-method">
      <param name="modules" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="module-pos" select="
         for $pos in (1 to count($modules)) 
         return if ($modules[$pos] is current()) then $pos else ()"/>
      <variable name="principal-module" select="$module-pos eq count($modules)"/>
      <variable name="context-param" select="src:aux-variable('context')"/>
      <value-of select="$src:new-line"/>
      <if test="not($principal-module)">
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.ComponentModel.EditorBrowsable')"/>
         <text>(</text>
         <value-of select="src:global-identifier('System.ComponentModel.EditorBrowsableState.Never')"/>
         <text>)]</text>
      </if>
      <call-template name="src:new-line-indented"/>
      <text>void </text>
      <choose>
         <when test="$principal-module">
            <value-of select="src:fully-qualified-helper('IXcstExecutable')"/>
            <text>.Prime</text>
         </when>
         <otherwise>
            <value-of select="src:prime-method-name(.)"/>
         </otherwise>
      </choose>
      <text>(</text>
      <value-of select="src:fully-qualified-helper('PrimingContext')"/>
      <text> </text>
      <value-of select="$context-param"/>
      <text>)</text>
      <call-template name="src:open-brace"/>
      <if test="$principal-module">
         <for-each select="$modules[position() ne last()]">
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="1"/>
            </call-template>
            <text>this.</text>
            <value-of select="src:prime-method-name(.)"/>
            <text>(</text>
            <value-of select="$context-param"/>
            <text>)</text>
            <value-of select="$src:statement-delimiter"/>
         </for-each>
      </if>
      <for-each select="c:param | c:variable">
         <if test="not($modules[position() gt $module-pos]/c:*[local-name() eq current()/local-name() and @name/xcst:name(.) = current()/@name/xcst:name(.)])">
            <apply-templates select="." mode="src:statement">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
               <with-param name="context-param" select="$context-param" tunnel="yes"/>
            </apply-templates>
         </if>
      </for-each>
      <call-template name="src:close-brace"/>
   </template>

   <function name="src:prime-method-name" as="xs:string">
      <param name="module" as="element(c:module)"/>

      <sequence select="concat(src:aux-variable('prime'), '_', generate-id($module))"/>
   </function>

   <template match="c:module" mode="src:call-template-method">
      <param name="indent" tunnel="yes"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <variable name="name-param" select="src:aux-variable('name')"/>
      <variable name="context-param" select="src:aux-variable('context')"/>
      <text>void </text>
      <value-of select="src:fully-qualified-helper('IXcstExecutable')"/>
      <text>.CallTemplate(</text>
      <value-of select="src:global-identifier('Xcst.QualifiedName')"/>
      <text> </text>
      <value-of select="$name-param"/>
      <text>, </text>
      <value-of select="src:fully-qualified-helper('DynamicContext')"/>
      <text> </text>
      <value-of select="$context-param"/>
      <text>)</text>
      <call-template name="src:open-brace"/>
      <apply-templates select="." mode="src:call-template-method-body">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         <with-param name="name-param" select="$name-param"/>
         <with-param name="context-param" select="$context-param" tunnel="yes"/>
      </apply-templates>
      <call-template name="src:close-brace"/>
   </template>

   <template match="c:module" mode="src:call-template-method-body">
      <param name="modules" tunnel="yes"/>
      <param name="name-param"/>
      <param name="context-param" tunnel="yes"/>

      <value-of select="$src:new-line"/>
      <for-each-group select="for $m in reverse($modules) return $m/c:template" group-by="resolve-QName(@name, .)">
         <choose>
            <when test="position() eq 1">
               <call-template name="src:new-line-indented"/>
            </when>
            <otherwise> else </otherwise>
         </choose>
         <text>if (</text>
         <value-of select="$name-param"/>
         <text>.Name</text>
         <text> == </text>
         <value-of select="src:string(local-name-from-QName(current-grouping-key()))"/>
         <text> &amp;&amp; </text>
         <value-of select="src:string-equals-literal(concat($name-param, '.Namespace'), namespace-uri-from-QName(current-grouping-key()))"/>
         <text>)</text>
         <call-template name="src:open-brace"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>this.</text>
         <value-of select="src:template-method-name(.)"/>
         <text>(</text>
         <value-of select="$context-param"/>
         <text>)</text>
         <value-of select="$src:statement-delimiter"/>
         <call-template name="src:close-brace"/>
      </for-each-group>
      <if test="$modules/c:template">
         <text> else</text>
         <call-template name="src:open-brace"/>
      </if>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="if ($modules/c:template) then 1 else 0"/>
      </call-template>
      <text>throw </text>
      <value-of select="src:fully-qualified-helper('DynamicError')"/>
      <text>.UnknownTemplate(</text>
      <value-of select="$name-param"/>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <if test="$modules/c:template">
         <call-template name="src:close-brace"/>
      </if>
   </template>

   <template match="c:module" mode="src:read-output-definition-method">
      <param name="indent" tunnel="yes"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <variable name="name-param" select="src:aux-variable('name')"/>
      <variable name="parameters-param" select="src:aux-variable('parameters')"/>
      <text>void </text>
      <value-of select="src:fully-qualified-helper('IXcstExecutable')"/>
      <text>.ReadOutputDefinition(</text>
      <value-of select="src:global-identifier('Xcst.QualifiedName')"/>
      <text> </text>
      <value-of select="$name-param"/>
      <text>, </text>
      <value-of select="src:global-identifier('Xcst.OutputParameters')"/>
      <text> </text>
      <value-of select="$parameters-param"/>
      <text>)</text>
      <call-template name="src:open-brace"/>
      <apply-templates select="." mode="src:read-output-definition-method-body">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         <with-param name="name-param" select="$name-param"/>
         <with-param name="parameters-param" select="$parameters-param"/>
      </apply-templates>
      <call-template name="src:close-brace"/>
   </template>

   <template match="c:module" mode="src:read-output-definition-method-body">
      <param name="indent" tunnel="yes"/>
      <param name="modules" tunnel="yes"/>
      <param name="name-param"/>
      <param name="parameters-param"/>

      <!-- TODO: XTSE1560: Conflicting values for output parameter {name()} -->

      <value-of select="$src:new-line"/>
      <for-each-group select="for $m in reverse($modules) return $m/c:output" group-by="(resolve-QName(@name, .), '')[1]">
         <variable name="output-name" select="
            if (current-grouping-key() instance of xs:QName) then current-grouping-key()
            else ()"/>
         <if test="$output-name and xcst:is-reserved-namespace(namespace-uri-from-QName($output-name))">
            <sequence select="error(xs:QName('err:XTSE0080'), concat('Namespace prefix ', prefix-from-QName($output-name),' refers to a reserved namespace.'), src:error-object(.))"/>
         </if>
         <choose>
            <when test="position() eq 1">
               <call-template name="src:new-line-indented"/>
            </when>
            <otherwise> else </otherwise>
         </choose>
         <text>if (</text>
         <choose>
            <when test="empty($output-name)">
               <value-of select="$name-param"/>
               <text> == null</text>
            </when>
            <otherwise>
               <value-of select="$name-param"/>
               <text>.Name</text>
               <text> == </text>
               <value-of select="src:string(local-name-from-QName($output-name))"/>
               <text> &amp;&amp; </text>
               <value-of select="src:string-equals-literal(concat($name-param, '.Namespace'), namespace-uri-from-QName($output-name))"/>
            </otherwise>
         </choose>
         <text>)</text>
         <call-template name="src:open-brace"/>
         <for-each-group select="for $o in current-group() return $o/@*[not(self::attribute(name))]" group-by="node-name(.)">
            <if test="self::attribute(parameter-document)">
               <sequence select="error((), concat(name(), ' parameter not supported yet.'), src:error-object(.))"/>
            </if>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="1"/>
            </call-template>
            <value-of select="$parameters-param"/>
            <if test="not(namespace-uri())">.</if>
            <apply-templates select="." mode="src:output-parameter-setter">
               <with-param name="indent" select="$indent + 2" tunnel="yes"/>
               <with-param name="list-value" as="xs:QName*">
                  <if test="self::attribute(cdata-section-elements) 
                     or self::attribute(suppress-indentation)
                     or self::attribute(use-character-maps)">
                     <sequence select="distinct-values(
                        for $p in current-group()
                        return for $s in tokenize($p, '\s')[.]
                        return resolve-QName($s, $p/..)
                     )"/>
                  </if>
               </with-param>
            </apply-templates>
            <value-of select="$src:statement-delimiter"/>
         </for-each-group>
         <call-template name="src:close-brace"/>
      </for-each-group>
      <if test="not($modules/c:output[not(@name)])">
         <choose>
            <when test="$modules/c:output"> else </when>
            <otherwise>
               <call-template name="src:new-line-indented"/>
            </otherwise>
         </choose>
         <text>if (</text>
         <value-of select="$name-param"/>
         <text> == null</text>
         <text>)</text>
         <call-template name="src:open-brace"/>
         <call-template name="src:close-brace"/>
      </if>
      <text> else</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>throw </text>
      <value-of select="src:fully-qualified-helper('DynamicError')"/>
      <text>.UnknownOutputDefinition(</text>
      <value-of select="$name-param"/>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:close-brace"/>
   </template>

</stylesheet>
