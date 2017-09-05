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

   <import href="xcst-extensions.xsl"/>

   <param name="src:use-line-directive" select="false()" as="xs:boolean"/>
   <param name="src:new-line" select="'&#xD;&#xA;'" as="xs:string"/>
   <param name="src:indent" select="'    '" as="xs:string"/>
   <param name="src:open-brace-on-new-line" select="false()" as="xs:boolean"/>

   <variable name="src:statement-delimiter" select="';'" as="xs:string"/>
   <variable name="src:output-parameters" as="element()">
      <data xmlns="foo">
         <byte-order-mark>ByteOrderMark</byte-order-mark>
         <cdata-section-elements>CdataSectionElements</cdata-section-elements>
         <doctype-public>DoctypePublic</doctype-public>
         <doctype-system>DoctypeSystem</doctype-system>
         <encoding>Encoding</encoding>
         <escape-uri-attributes>EscapeUriAttributes</escape-uri-attributes>
         <html-version>HtmlVersion</html-version>
         <include-content-type>IncludeContentType</include-content-type>
         <indent>Indent</indent>
         <indent-spaces>IndentSpaces</indent-spaces>
         <item-separator>ItemSeparator</item-separator>
         <media-type>MediaType</media-type>
         <method>Method</method>
         <omit-xml-declaration>OmitXmlDeclaration</omit-xml-declaration>
         <output-version>Version</output-version>
         <standalone>Standalone</standalone>
         <suppress-indentation>SuppressIndentation</suppress-indentation>
         <undeclare-prefixes>UndeclarePrefixes</undeclare-prefixes>
         <version>Version</version>
      </data>
   </variable>

   <template match="c:*" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <!--
         If statement template does not exist but expression does append value
         e.g. <c:object>
      -->

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$output"/>
      <text>.WriteObject(</text>
      <apply-templates select="." mode="src:expression"/>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:*" mode="src:expression">
      <sequence select="error((), concat('Element c:', local-name(), ' cannot be compiled into an expression.'), src:error-object(.))"/>
   </template>

   <template match="c:*" mode="xcst:instruction"/>

   <!--
      ## Creating Nodes and Objects
   -->

   <template match="c:attribute" mode="src:statement">
      <param name="output" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'namespace', 'separator', 'value'"/>
      </call-template>
      <call-template name="xcst:value-or-sequence-constructor"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$output"/>
      <text>.</text>
      <variable name="attrib-string" select="@value or not(*)"/>
      <variable name="separator" select="@separator/src:expand-attribute(.)"/>
      <variable name="include-separator" select="not($attrib-string) and $separator"/>
      <choose>
         <when test="$attrib-string">WriteAttributeString</when>
         <otherwise>WriteStartAttribute</otherwise>
      </choose>
      <choose>
         <when test="xcst:is-value-template(@name)">
            <text>Lexical(</text>
            <value-of select="src:expand-attribute(@name)"/>
            <text>, </text>
            <value-of select="src:expression-or-null(@namespace/src:expand-attribute(.))"/>
         </when>
         <otherwise>
            <text>(</text>
            <variable name="n" select="xcst:name(@name)"/>
            <variable name="name" select="if (@namespace) then QName('urn:foo', $n) else resolve-QName($n, .)"/>
            <variable name="prefix" select="prefix-from-QName($name)"/>
            <if test="$prefix or $include-separator">
               <value-of select="if ($prefix) then src:string($prefix) else 'null'"/>
               <text>, </text>
            </if>
            <value-of select="src:string(local-name-from-QName($name))"/>
            <choose>
               <when test="@namespace">
                  <text>, </text>
                  <value-of select="src:expand-attribute(@namespace)"/>
               </when>
               <when test="$prefix">
                  <text>, </text>
                  <value-of select="src:verbatim-string(namespace-uri-from-QName($name))"/>
               </when>
               <when test="$include-separator">
                  <text>, null</text>
               </when>
            </choose>
         </otherwise>
      </choose>
      <choose>
         <when test="$attrib-string">
            <text>, </text>
            <call-template name="src:simple-content">
               <with-param name="attribute" select="@value"/>
               <with-param name="separator" select="$separator"/>
            </call-template>
         </when>
         <when test="$include-separator">
            <text>, </text>
            <value-of select="$separator"/>
         </when>
      </choose>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <if test="not($attrib-string)">
         <variable name="new-indent" select="$indent + 1"/>
         <call-template name="src:line-hidden"/>
         <call-template name="src:new-line-indented"/>
         <text>try</text>
         <call-template name="src:sequence-constructor">
            <with-param name="ensure-block" select="true()"/>
         </call-template>
         <text> finally</text>
         <call-template name="src:open-brace"/>
         <call-template name="src:line-hidden">
            <with-param name="indent" select="$new-indent" tunnel="yes"/>
         </call-template>
         <call-template name="src:new-line-indented">
            <with-param name="indent" select="$new-indent" tunnel="yes"/>
         </call-template>
         <value-of select="$output"/>
         <text>.WriteEndAttribute()</text>
         <value-of select="$src:statement-delimiter"/>
         <call-template name="src:close-brace"/>
      </if>
   </template>

   <template match="c:comment" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'value'"/>
      </call-template>
      <call-template name="xcst:value-or-sequence-constructor"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$output"/>
      <text>.WriteComment(</text>
      <call-template name="src:simple-content">
         <with-param name="attribute" select="@value"/>
      </call-template>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:element" mode="src:statement">
      <param name="output" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'namespace', 'use-attribute-sets'"/>
      </call-template>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$output"/>
      <text>.WriteStartElement</text>
      <choose>
         <when test="xcst:is-value-template(@name)">
            <text>Lexical(</text>
            <value-of select="
               src:expand-attribute(@name),
               src:expression-or-null(@namespace/src:expand-attribute(.)),
               src:verbatim-string(namespace-uri-from-QName(resolve-QName('foo', .)))" separator=", "/>
         </when>
         <otherwise>
            <text>(</text>
            <variable name="n" select="xcst:name(@name)"/>
            <variable name="name" select="if (@namespace) then QName('urn:foo', $n) else resolve-QName($n, .)"/>
            <variable name="prefix" select="prefix-from-QName($name)"/>
            <if test="$prefix">
               <value-of select="src:string($prefix)"/>
               <text>, </text>
            </if>
            <value-of select="src:string(local-name-from-QName($name))"/>
            <text>, </text>
            <choose>
               <when test="@namespace">
                  <value-of select="src:expand-attribute(@namespace)"/>
               </when>
               <otherwise>
                  <value-of select="src:verbatim-string(namespace-uri-from-QName($name))"/>
               </otherwise>
            </choose>
         </otherwise>
      </choose>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <variable name="new-indent" select="$indent + 1"/>
      <call-template name="src:line-hidden"/>
      <call-template name="src:new-line-indented"/>
      <text>try</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:use-attribute-sets">
         <with-param name="attr" select="@use-attribute-sets"/>
         <with-param name="indent" select="$new-indent" tunnel="yes"/>
      </call-template>
      <call-template name="src:sequence-constructor">
         <with-param name="omit-block" select="true()"/>
         <with-param name="indent" select="$new-indent" tunnel="yes"/>
      </call-template>
      <call-template name="src:line-hidden">
         <with-param name="indent" select="$new-indent" tunnel="yes"/>
      </call-template>
      <call-template name="src:close-brace"/>
      <text> finally</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="indent" select="$new-indent" tunnel="yes"/>
      </call-template>
      <value-of select="$output"/>
      <text>.WriteEndElement()</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:close-brace"/>
   </template>

   <template match="c:namespace" mode="src:statement">
      <param name="output" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'value'"/>
      </call-template>
      <call-template name="xcst:value-or-sequence-constructor"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$output"/>
      <text>.</text>
      <variable name="attrib-string" select="@value or not(*)"/>
      <choose>
         <when test="$attrib-string">WriteAttributeString</when>
         <otherwise>WriteStartAttribute</otherwise>
      </choose>
      <text>("xmlns", </text>
      <value-of select="
         if (xcst:is-value-template(@name)) then src:expand-attribute(@name)
         else src:string(xcst:name(@name))"/>
      <text>, null</text>
      <if test="$attrib-string">
         <text>, </text>
         <call-template name="src:simple-content">
            <with-param name="attribute" select="@value"/>
         </call-template>
      </if>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <if test="not($attrib-string)">
         <variable name="new-indent" select="$indent + 1"/>
         <call-template name="src:line-hidden"/>
         <call-template name="src:new-line-indented"/>
         <text>try</text>
         <call-template name="src:sequence-constructor">
            <with-param name="ensure-block" select="true()"/>
         </call-template>
         <text> finally</text>
         <call-template name="src:open-brace"/>
         <call-template name="src:line-hidden">
            <with-param name="indent" select="$new-indent" tunnel="yes"/>
         </call-template>
         <call-template name="src:new-line-indented">
            <with-param name="indent" select="$new-indent" tunnel="yes"/>
         </call-template>
         <value-of select="$output"/>
         <text>.WriteEndAttribute()</text>
         <value-of select="$src:statement-delimiter"/>
         <call-template name="src:close-brace"/>
      </if>
   </template>

   <template match="c:processing-instruction" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'value'"/>
      </call-template>
      <call-template name="xcst:value-or-sequence-constructor"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$output"/>
      <text>.WriteProcessingInstruction(</text>
      <value-of select="
         if (xcst:is-value-template(@name)) then src:expand-attribute(@name)
         else src:string(xcst:name(@name))"/>
      <text>, </text>
      <call-template name="src:simple-content">
         <with-param name="attribute" select="@value"/>
      </call-template>
      <text>.TrimStart())</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:text | c:value-of" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$output"/>
      <text>.</text>
      <choose>
         <when test="@disable-output-escaping/xcst:boolean(.)">WriteRaw</when>
         <otherwise>WriteString</otherwise>
      </choose>
      <text>(</text>
      <apply-templates select="." mode="src:expression"/>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:text" mode="src:expression">
      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'disable-output-escaping'"/>
      </call-template>
      <call-template name="xcst:text-only"/>
      <variable name="text" select="xcst:text(.)"/>
      <value-of select="
         if ($text) then src:expand-text(., $text) 
         else src:string('')"/>
   </template>

   <template match="c:value-of" mode="src:expression">
      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'disable-output-escaping', 'value', 'separator'"/>
      </call-template>
      <call-template name="xcst:value-or-sequence-constructor"/>
      <call-template name="src:simple-content">
         <with-param name="attribute" select="@value"/>
         <with-param name="separator" select="@separator/src:expand-attribute(.)"/>
      </call-template>
   </template>

   <template match="c:value-of | c:text" mode="xcst:instruction">
      <element name="xcst:instruction">
         <attribute name="as" select="'System.String'"/>
         <attribute name="expression" select="true()"/>
      </element>
   </template>

   <template match="text()[xcst:insignificant-whitespace(.)]" mode="src:statement src:expression">
      <if test="xcst:preserve-whitespace(..)">
         <next-match/>
      </if>
   </template>

   <template match="text()" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$output"/>
      <text>.WriteString(</text>
      <apply-templates select="." mode="src:expression"/>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="text()" mode="src:expression">
      <value-of select="
         if (.. instance of element()) then
            src:expand-text(.., string())
         else 
            src:verbatim-string(string())"/>
   </template>

   <template name="src:literal-result-element">
      <param name="output" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="@*[not(namespace-uri())]/local-name()"/>
      </call-template>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$output"/>
      <text>.WriteStartElement(</text>
      <variable name="prefix" select="prefix-from-QName(node-name(.))"/>
      <if test="$prefix">
         <value-of select="src:string($prefix)"/>
         <text>, </text>
      </if>
      <value-of select="src:string(local-name())"/>
      <text>, </text>
      <value-of select="src:verbatim-string(namespace-uri())"/>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <variable name="new-indent" select="$indent + 1"/>
      <call-template name="src:line-hidden"/>
      <call-template name="src:new-line-indented"/>
      <text>try</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:use-attribute-sets">
         <with-param name="attr" select="@c:use-attribute-sets"/>
         <with-param name="indent" select="$new-indent" tunnel="yes"/>
      </call-template>
      <for-each select="@* except @c:*">
         <call-template name="src:new-line-indented">
            <with-param name="indent" select="$new-indent" tunnel="yes"/>
         </call-template>
         <value-of select="$output"/>
         <text>.WriteAttributeString(</text>
         <variable name="attr-prefix" select="prefix-from-QName(node-name(.))"/>
         <if test="$attr-prefix">
            <value-of select="src:string($attr-prefix)"/>
            <text>, </text>
         </if>
         <value-of select="src:string(local-name())"/>
         <text>, </text>
         <if test="$attr-prefix">
            <value-of select="src:string(namespace-uri())"/>
            <text>, </text>
         </if>
         <value-of select="src:expand-attribute(.)"/>
         <text>)</text>
         <value-of select="$src:statement-delimiter"/>
      </for-each>
      <call-template name="src:sequence-constructor">
         <with-param name="omit-block" select="true()"/>
         <with-param name="indent" select="$new-indent" tunnel="yes"/>
      </call-template>
      <call-template name="src:line-hidden">
         <with-param name="indent" select="$new-indent" tunnel="yes"/>
      </call-template>
      <call-template name="src:close-brace"/>
      <text> finally</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="indent" select="$new-indent" tunnel="yes"/>
      </call-template>
      <value-of select="$output"/>
      <text>.WriteEndElement()</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:close-brace"/>
   </template>

   <template name="src:use-attribute-sets">
      <param name="attr" as="attribute()?"/>
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="context" tunnel="yes"/>
      <param name="output" tunnel="yes"/>

      <if test="$attr">
         <variable name="names" select="
            for $s in tokenize($attr, '\s')[.]
            return xcst:EQName($attr, $s)"/>
         <variable name="sets" as="xs:string*">
            <variable name="current" select="."/>
            <for-each select="$names">
               <choose>
                  <when test="$current[self::c:attribute-set] and $current/parent::c:override and . eq xs:QName('c:original')">
                     <variable name="current-meta" select="$package-manifest/xcst:attribute-set[@declaration-id eq generate-id($current)]"/>
                     <variable name="original-meta" select="$package-manifest/xcst:attribute-set[@id eq $current-meta/@overrides]"/>
                     <if test="$original-meta/@original-visibility eq 'abstract'">
                        <sequence select="error(xs:QName('err:XTSE3075'), 'Cannot use the component reference c:original when the overridden component has visibility=''abstract''.', src:error-object($attr))"/>
                     </if>
                     <sequence select="src:original-member($original-meta)"/>
                  </when>
                  <otherwise>
                     <variable name="meta" select="$package-manifest/xcst:attribute-set[@visibility ne 'hidden' and xcst:EQName(@name) eq current()]"/>
                     <if test="not($meta)">
                        <sequence select="error(xs:QName('err:XTSE0710'), concat('No attribute set exists named ', current(), '.'), src:error-object($current))"/>
                     </if>
                     <sequence select="$meta/@member-name"/>
                  </otherwise>
               </choose>
            </for-each>
         </variable>
         <for-each select="$sets">
            <call-template name="src:new-line-indented"/>
            <text>this.</text>
            <value-of select="."/>
            <text>(</text>
            <value-of select="src:expression-or-null($context), $output" separator=", "/>
            <text>)</text>
            <value-of select="$src:statement-delimiter"/>
         </for-each>
      </if>
   </template>

   <template match="c:object" mode="src:expression">
      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'value'"/>
      </call-template>
      <call-template name="xcst:no-children"/>
      <value-of select="xcst:expression(@value)"/>
   </template>

   <template match="c:object" mode="xcst:instruction">
      <element name="xcst:instruction">
         <attribute name="expression" select="true()"/>
      </element>
   </template>

   <template match="c:map" mode="src:statement">
      <param name="output" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <call-template name="xcst:validate-attribs"/>

      <variable name="output-is-map" select="src:output-is-map($output)"/>
      <variable name="map-output" select="src:map-output(., $output)"/>
      <variable name="new-indent" select="$indent + 1"/>

      <if test="not($output-is-map)">
         <value-of select="$src:new-line"/>
         <call-template name="src:line-hidden"/>
         <call-template name="src:new-line-indented"/>
         <text>var </text>
         <value-of select="$map-output"/>
         <text> = </text>
         <value-of select="src:fully-qualified-helper('MapWriter')"/>
         <text>.Create(</text>
         <value-of select="$output"/>
         <text>)</text>
         <value-of select="$src:statement-delimiter"/>
         <value-of select="$src:new-line"/>
      </if>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$map-output"/>
      <text>.WriteStartMap()</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:line-hidden"/>
      <call-template name="src:new-line-indented"/>
      <text>try</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:sequence-constructor">
         <with-param name="omit-block" select="true()"/>
         <with-param name="indent" select="$new-indent" tunnel="yes"/>
         <with-param name="output" select="$map-output" tunnel="yes"/>
      </call-template>
      <call-template name="src:line-hidden">
         <with-param name="indent" select="$new-indent" tunnel="yes"/>
      </call-template>
      <call-template name="src:close-brace"/>
      <text> finally</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="indent" select="$new-indent" tunnel="yes"/>
      </call-template>
      <value-of select="$map-output"/>
      <text>.WriteEndMap()</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:close-brace"/>
   </template>

   <template match="c:map" mode="xcst:instruction">
      <element name="xcst:instruction">
         <attribute name="as" select="'System.Object'"/>
      </element>
   </template>

   <template match="c:map-entry" mode="src:statement">
      <param name="output" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'key'"/>
         <with-param name="optional" select="'value'"/>
      </call-template>
      <call-template name="xcst:value-or-sequence-constructor"/>

      <variable name="output-is-map" select="src:output-is-map($output)"/>
      <variable name="map-output" select="src:map-output(., $output)"/>
      <variable name="new-indent" select="$indent + 1"/>

      <if test="not($output-is-map)">
         <value-of select="$src:new-line"/>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>var </text>
         <value-of select="$map-output"/>
         <text> = </text>
         <value-of select="src:fully-qualified-helper('MapWriter')"/>
         <text>.Cast(</text>
         <value-of select="$output"/>
         <text>)</text>
         <value-of select="$src:statement-delimiter"/>
         <value-of select="$src:new-line"/>
      </if>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$map-output"/>
      <text>.WriteStartMapEntry(</text>
      <value-of select="xcst:expression(@key)"/>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:line-hidden"/>
      <call-template name="src:new-line-indented"/>
      <text>try</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:sequence-constructor">
         <with-param name="omit-block" select="true()"/>
         <with-param name="indent" select="$new-indent" tunnel="yes"/>
         <with-param name="value" select="@value"/>
         <with-param name="output" select="$map-output" tunnel="yes"/>
      </call-template>
      <call-template name="src:line-hidden">
         <with-param name="indent" select="$new-indent" tunnel="yes"/>
      </call-template>
      <call-template name="src:close-brace"/>
      <text> finally</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="indent" select="$new-indent" tunnel="yes"/>
      </call-template>
      <value-of select="$map-output"/>
      <text>.WriteEndMapEntry()</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:close-brace"/>
   </template>

   <template match="c:array" mode="src:statement">
      <param name="output" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <call-template name="xcst:validate-attribs"/>

      <variable name="output-is-map" select="src:output-is-map($output)"/>
      <variable name="map-output" select="src:map-output(., $output)"/>
      <variable name="new-indent" select="$indent + 1"/>

      <if test="not($output-is-map)">
         <value-of select="$src:new-line"/>
         <call-template name="src:line-hidden"/>
         <call-template name="src:new-line-indented"/>
         <text>var </text>
         <value-of select="$map-output"/>
         <text> = </text>
         <value-of select="src:fully-qualified-helper('MapWriter')"/>
         <text>.CreateArray(</text>
         <value-of select="$output"/>
         <text>)</text>
         <value-of select="$src:statement-delimiter"/>
         <value-of select="$src:new-line"/>
      </if>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$map-output"/>
      <text>.WriteStartArray()</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:line-hidden"/>
      <call-template name="src:new-line-indented"/>
      <text>try</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:sequence-constructor">
         <with-param name="omit-block" select="true()"/>
         <with-param name="indent" select="$new-indent" tunnel="yes"/>
         <with-param name="output" select="$map-output" tunnel="yes"/>
      </call-template>
      <call-template name="src:line-hidden">
         <with-param name="indent" select="$new-indent" tunnel="yes"/>
      </call-template>
      <call-template name="src:close-brace"/>
      <text> finally</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="indent" select="$new-indent" tunnel="yes"/>
      </call-template>
      <value-of select="$map-output"/>
      <text>.WriteEndArray()</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:close-brace"/>
   </template>

   <template match="c:array" mode="xcst:instruction">
      <element name="xcst:instruction">
         <attribute name="as" select="'System.Object'"/>
      </element>
   </template>

   <function name="src:map-output" as="item()">
      <param name="el" as="element()"/>
      <param name="output" as="item()"/>

      <choose>
         <when test="src:output-is-map($output)">
            <sequence select="$output"/>
         </when>
         <otherwise>
            <element name="output">
               <attribute name="map" select="true()"/>
               <value-of select="concat(src:aux-variable('output'), '_', generate-id($el))"/>
            </element>
         </otherwise>
      </choose>
   </function>

   <function name="src:output-is-map" as="xs:boolean">
      <param name="output" as="item()"/>

      <sequence select="
         if ($output instance of element()) then boolean($output/@map/xs:boolean(.))
         else false()"/>
   </function>

   <!--
      ## Repetition
   -->

   <template match="c:for-each" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name', 'in'"/>
         <with-param name="optional" select="'as'"/>
      </call-template>
      <variable name="name" select="xcst:name(@name)"/>
      <variable name="in" select="xcst:expression(@in)"/>
      <value-of select="$src:new-line"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>foreach (</text>
      <value-of select="(@as/xcst:type(.), 'var')[1]"/>
      <text> </text>
      <value-of select="$name"/>
      <text> in </text>
      <choose>
         <when test="c:sort">
            <call-template name="src:sort">
               <with-param name="name" select="$name"/>
               <with-param name="in" select="$in"/>
            </call-template>
         </when>
         <otherwise>
            <value-of select="$in"/>
         </otherwise>
      </choose>
      <text>)</text>
      <call-template name="src:sequence-constructor">
         <with-param name="children" select="node()[not(self::c:sort or following-sibling::c:sort)]"/>
         <with-param name="ensure-block" select="true()"/>
      </call-template>
   </template>

   <template match="c:while" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'test'"/>
      </call-template>
      <value-of select="$src:new-line"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>while (</text>
      <value-of select="xcst:expression(@test)"/>
      <text>)</text>
      <call-template name="src:sequence-constructor">
         <with-param name="ensure-block" select="true()"/>
      </call-template>
   </template>

   <!--
      ## Conditional Processing
   -->

   <template match="c:choose" mode="src:statement">
      <call-template name="xcst:validate-attribs"/>
      <call-template name="xcst:validate-children">
         <with-param name="allowed" select="'when', 'otherwise'"/>
      </call-template>
      <if test="not(c:when)">
         <sequence select="error(xs:QName('err:XTSE0010'), 'At least one c:when element is required within c:choose', src:error-object(.))"/>
      </if>
      <apply-templates select="c:when | c:otherwise" mode="#current"/>
   </template>

   <template match="c:choose/c:when" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'test'"/>
      </call-template>
      <call-template name="xcst:no-other-preceding"/>
      <variable name="pos" select="position()"/>
      <if test="$pos eq 1">
         <value-of select="$src:new-line"/>
      </if>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <if test="$pos gt 1">else </if>
      <text>if (</text>
      <value-of select="xcst:expression(@test)"/>
      <text>)</text>
      <call-template name="src:sequence-constructor">
         <with-param name="ensure-block" select="true()"/>
      </call-template>
   </template>

   <template match="c:choose/c:otherwise" mode="src:statement">
      <call-template name="xcst:validate-attribs"/>
      <call-template name="xcst:no-other-following">
         <with-param name="except" select="()"/>
      </call-template>
      <text> else</text>
      <call-template name="src:sequence-constructor">
         <with-param name="ensure-block" select="true()"/>
      </call-template>
   </template>

   <template match="c:if" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'test'"/>
      </call-template>
      <value-of select="$src:new-line"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>if (</text>
      <value-of select="xcst:expression(@test)"/>
      <text>)</text>
      <call-template name="src:sequence-constructor">
         <with-param name="ensure-block" select="true()"/>
      </call-template>
   </template>

   <template match="c:try" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'rollback-output', 'value'"/>
      </call-template>
      <variable name="children" select="
         node()[not(self::c:catch
            or preceding-sibling::c:catch
            or self::c:finally
            or preceding-sibling::c:finally)]"/>
      <call-template name="xcst:value-or-sequence-constructor">
         <with-param name="children" select="$children"/>
      </call-template>
      <if test="not(c:catch) and not(c:finally)">
         <sequence select="error(xs:QName('err:XTSE0010'), 'At least one c:catch or c:finally element is required within c:try', src:error-object(.))"/>
      </if>
      <variable name="rollback" select="(@rollback-output/xcst:boolean(.), true())[1]"/>
      <if test="$rollback">
         <!-- TODO: Buffering -->
         <sequence select="error((), 'Buffering not supported yet. Use rollback-output=''no''.', src:error-object(.))"/>
      </if>
      <value-of select="$src:new-line"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>try</text>
      <call-template name="src:sequence-constructor">
         <with-param name="value" select="@value"/>
         <with-param name="children" select="$children"/>
         <with-param name="ensure-block" select="true()"/>
      </call-template>
      <apply-templates select="c:catch | c:finally" mode="#current"/>
   </template>

   <template match="c:try/c:catch" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'exception', 'when', 'value'"/>
      </call-template>
      <call-template name="xcst:value-or-sequence-constructor"/>
      <call-template name="xcst:no-other-following">
         <with-param name="except" select="xs:QName('c:catch'), xs:QName('c:finally')"/>
      </call-template>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>catch</text>
      <if test="@exception">
         <text> (</text>
         <value-of select="xcst:expression(@exception)"/>
         <text>)</text>
      </if>
      <if test="@when">
         <text> when (</text>
         <value-of select="xcst:expression(@when)"/>
         <text>)</text>
      </if>
      <call-template name="src:sequence-constructor">
         <with-param name="value" select="@value"/>
         <with-param name="ensure-block" select="true()"/>
      </call-template>
   </template>

   <template match="c:try/c:finally" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'value'"/>
      </call-template>
      <call-template name="xcst:value-or-sequence-constructor"/>
      <call-template name="xcst:no-other-following">
         <with-param name="except" select="()"/>
      </call-template>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>finally</text>
      <call-template name="src:sequence-constructor">
         <with-param name="value" select="@value"/>
         <with-param name="ensure-block" select="true()"/>
      </call-template>
   </template>

   <template match="c:return" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'value'"/>
      </call-template>
      <call-template name="xcst:value-or-sequence-constructor"/>
      <variable name="disallowed-ancestor"
         select="ancestor::*[self::c:param or self::c:with-param or self::c:variable or self::c:value-of or self::c:serialize][1]"/>
      <variable name="allowed-ancestor" select="ancestor::c:delegate[1]"/>
      <if test="$disallowed-ancestor
         and (not($allowed-ancestor) or $disallowed-ancestor >> $allowed-ancestor)">
         <sequence select="error(xs:QName('err:XTSE0010'), 'Cannot return while materializing a sequence constructor.', src:error-object(.))"/>
      </if>

      <choose>
         <when test="$output">
            <call-template name="src:sequence-constructor">
               <with-param name="value" select="@value"/>
            </call-template>
            <call-template name="src:line-number"/>
            <call-template name="src:new-line-indented"/>
            <text>return</text>
            <value-of select="$src:statement-delimiter"/>
         </when>
         <otherwise>
            <variable name="text" select="xcst:text(.)"/>
            <call-template name="src:line-number"/>
            <call-template name="src:new-line-indented"/>
            <text>return</text>
            <if test="xcst:has-value(., $text)">
               <text> </text>
               <call-template name="src:value">
                  <with-param name="text" select="$text"/>
               </call-template>
            </if>
            <value-of select="$src:statement-delimiter"/>
         </otherwise>
      </choose>
   </template>

   <template match="c:break | c:continue" mode="src:statement">
      <call-template name="xcst:validate-attribs"/>
      <call-template name="xcst:no-children"/>
      <variable name="required-ancestor" select="ancestor::*[self::c:for-each or self::c:for-each-group or self::c:while][1]"/>
      <variable name="disallowed-ancestor"
         select="ancestor::*[self::c:delegate or self::c:with-param or self::c:variable or self::c:value-of or self::c:serialize][1]"/>
      <if test="not($required-ancestor)
         or ($disallowed-ancestor and $disallowed-ancestor >> $required-ancestor)">
         <sequence select="error(xs:QName('err:XTSE0010'), concat('c:', local-name(), ' instruction can only be used within a c:for-each, c:for-each-group or c:while instruction.'), src:error-object(.))"/>
      </if>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="local-name()"/>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:using" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'value'"/>
         <with-param name="optional" select="'name', 'as'"/>
      </call-template>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>using (</text>
      <choose>
         <when test="@name">
            <value-of select="(@as/xcst:type(.), 'var')[1], xcst:name(@name)"/>
            <text> = </text>
         </when>
         <when test="@as">
            <value-of select="'(', xcst:type(@as), ')'" separator=""/>
         </when>
      </choose>
      <value-of select="xcst:expression(@value)"/>
      <text>)</text>
      <call-template name="src:sequence-constructor">
         <with-param name="ensure-block" select="true()"/>
      </call-template>
   </template>

   <!--
      ## Variables and Parameters
   -->

   <template match="c:module/c:param | c:package/c:param | c:override/c:param | c:template/c:param | c:delegate/c:param" mode="src:statement">
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="context" tunnel="yes"/>

      <!-- TODO: $c:original -->

      <variable name="global" select="parent::c:module
         or parent::c:package
         or parent::c:override"/>

      <variable name="name" select="xcst:name(@name)"/>
      <variable name="name-str" select="src:string(src:strip-verbatim-prefix($name))"/>
      <variable name="text" select="xcst:text(.)"/>
      <variable name="has-default-value" select="xcst:has-value(., $text)"/>
      <variable name="type" select="(@as/xcst:type(.), 'object')[1]"/>
      <variable name="required" select="(@required/xcst:boolean(.), false())[1]"/>
      <variable name="tunnel" select="(@tunnel/xcst:boolean(.), false())[1]"/>
      <variable name="template-meta" select="
         if (parent::c:template) then
            $package-manifest/xcst:template[@declaration-id eq generate-id(current()/..)]
         else ()"/>

      <if test="parent::c:delegate">
         <if test="$has-default-value and $required">
            <sequence select="error(xs:QName('err:XTSE0010'), 'The ''value'' attribute or child element/text should be omitted when required=''yes''.', src:error-object(.))"/>
         </if>
      </if>

      <variable name="default-value-lambda-body">
         <choose>
            <when test="$required">
               <call-template name="src:open-brace"/>
               <text> throw </text>
               <value-of select="src:fully-qualified-helper('DynamicError')"/>
               <text>.</text>
               <choose>
                  <when test="$global">RequiredGlobalParameter</when>
                  <otherwise>RequiredTemplateParameter</otherwise>
               </choose>
               <text>(</text>
               <value-of select="$name-str"/>
               <text>)</text>
               <value-of select="$src:statement-delimiter"/>
               <call-template name="src:close-brace"/>
            </when>
            <otherwise>
               <call-template name="src:value">
                  <with-param name="text" select="$text"/>
               </call-template>
            </otherwise>
         </choose>
      </variable>

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>

      <choose>
         <when test="$global">
            <variable name="meta" select="$package-manifest/xcst:*[@declaration-id eq current()/generate-id()]"/>
            <text>this.</text>
            <value-of select="src:backing-field($meta)"/>
         </when>
         <otherwise>
            <value-of select="(@as/$type, 'var')[1], $name"/>
         </otherwise>
      </choose>
      <text> = </text>
      <choose>
         <when test="$template-meta/xcst:typed-params(.) and not($tunnel)">
            <choose>
               <when test="not($required) and not($has-default-value)">
                  <value-of select="$context, 'Parameters', $name" separator="."/>
               </when>
               <otherwise>
                  <value-of select="src:template-context-type(())"/>
                  <text>.TypedParam</text>
                  <if test="$required">
                     <text>&lt;</text>
                     <value-of select="$type, $type" separator=", "/>
                     <text>></text>
                  </if>
                  <text>(</text>
                  <value-of select="$name-str"/>
                  <text>, </text>
                  <value-of select="$context, 'Parameters', src:params-type-set-name(src:strip-verbatim-prefix($name))" separator="."/>
                  <text>, </text>
                  <value-of select="$context, 'Parameters', $name" separator="."/>
                  <text>, () => </text>
                  <value-of select="$default-value-lambda-body"/>
                  <text>)</text>
               </otherwise>
            </choose>
         </when>
         <otherwise>
            <value-of select="$context"/>
            <text>.Param</text>
            <if test="$required">
               <text>&lt;</text>
               <value-of select="$type"/>
               <text>></text>
            </if>
            <text>(</text>
            <value-of select="$name-str"/>
            <text>, () => </text>
            <value-of select="$default-value-lambda-body"/>
            <if test="$tunnel">
               <text>, tunnel: </text>
               <value-of select="src:boolean(true())"/>
            </if>
            <text>)</text>
         </otherwise>
      </choose>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:variable" mode="src:statement">

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'value', 'as'"/>
      </call-template>
      <call-template name="xcst:value-or-sequence-constructor"/>

      <variable name="text" select="xcst:text(.)"/>
      <variable name="has-value" select="xcst:has-value(., $text)"/>

      <variable name="type" as="xs:string?">
         <call-template name="xcst:variable-type">
            <with-param name="el" select="."/>
            <with-param name="text" select="$text"/>
            <with-param name="ignore-seqctor" select="true()"/>
         </call-template>
      </variable>

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="($type, 'var')[1], xcst:name(@name)"/>
      <if test="$has-value">
         <text> = </text>
         <call-template name="src:value">
            <with-param name="text" select="$text"/>
         </call-template>
      </if>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:variable" mode="xcst:instruction">
      <element name="xcst:instruction">
         <attribute name="void" select="true()"/>
      </element>
   </template>

   <template match="c:module/c:variable | c:package/c:variable | c:override/c:variable" mode="src:statement">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <!-- TODO: $c:original -->
      <variable name="text" select="xcst:text(.)"/>

      <if test="xcst:has-value(., $text)">
         <variable name="meta" select="$package-manifest/xcst:*[@declaration-id eq current()/generate-id()]"/>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>this.</text>
         <value-of select="if ($meta/@visibility = ('public', 'final')) then src:backing-field($meta) else xcst:name(@name)"/>
         <text> = </text>
         <call-template name="src:value">
            <with-param name="text" select="$text"/>
         </call-template>
         <value-of select="$src:statement-delimiter"/>
      </if>
   </template>

   <template match="c:set" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'member'"/>
         <with-param name="optional" select="'as', 'value'"/>
      </call-template>
      <call-template name="xcst:value-or-sequence-constructor"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="xcst:expression(@member)"/>
      <text> = </text>
      <call-template name="src:value"/>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:set" mode="xcst:instruction">
      <element name="xcst:instruction">
         <attribute name="void" select="true()"/>
      </element>
   </template>

   <!--
      ## Callable Components
   -->

   <template match="c:call-template" mode="src:statement">

      <variable name="result" as="item()+">
         <call-template name="xcst:validate-call-template"/>
      </variable>
      <variable name="meta" select="$result[1]" as="element(xcst:template)"/>
      <variable name="original" select="$result[2]" as="xs:boolean"/>

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>this.</text>
      <value-of select="if ($original) then src:original-member($meta) else $meta/@member-name"/>
      <text>(</text>
      <call-template name="src:call-template-context">
         <with-param name="meta" select="$meta"/>
      </call-template>
      <text>, </text>
      <call-template name="src:call-template-output">
         <with-param name="meta" select="$meta"/>
      </call-template>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:call-template" mode="src:expression">

      <variable name="result" as="item()+">
         <call-template name="xcst:validate-call-template"/>
      </variable>
      <variable name="meta" select="$result[1]" as="element(xcst:template)"/>
      <variable name="original" select="$result[2]" as="xs:boolean"/>

      <value-of select="src:fully-qualified-helper('SequenceWriter'), 'Create'" separator="."/>
      <text>(</text>
      <value-of select="src:global-identifier($meta/(@package-type, ../@package-type)[1]), src:item-type-inference-member-name($meta/@member-name)" separator="."/>
      <text>).WriteTemplate(this.</text>
      <value-of select="if ($original) then src:original-member($meta) else $meta/@member-name"/>
      <text>, </text>
      <call-template name="src:call-template-context">
         <with-param name="meta" select="$meta"/>
      </call-template>
      <text>).Flush</text>
      <if test="$meta/@cardinality eq 'One'">Single</if>
      <text>()</text>
   </template>

   <template name="xcst:validate-call-template">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
      </call-template>
      <call-template name="xcst:validate-children">
         <with-param name="allowed" select="'with-param'"/>
      </call-template>

      <for-each select="c:with-param">
         <call-template name="xcst:validate-attribs">
            <with-param name="required" select="'name'"/>
            <with-param name="optional" select="'value', 'as', 'tunnel'"/>
         </call-template>
         <call-template name="xcst:value-or-sequence-constructor"/>
         <if test="preceding-sibling::c:with-param[xcst:name-equals(@name, current()/@name)]">
            <sequence select="error(xs:QName('err:XTSE0670'), 'Duplicate parameter name.', src:error-object(.))"/>
         </if>
      </for-each>

      <variable name="qname" select="xcst:EQName(@name)"/>
      <variable name="original" select="$qname eq xs:QName('c:original') and ancestor::c:override"/>
      <variable name="meta" as="element(xcst:template)">
         <choose>
            <when test="$original">
               <variable name="current-template" select="ancestor::c:template[1]"/>
               <variable name="current-meta" select="$package-manifest/xcst:template[@declaration-id eq generate-id($current-template)]"/>
               <variable name="original-meta" select="$package-manifest/xcst:template[@id eq $current-meta/@overrides]"/>
               <if test="$original-meta/@original-visibility eq 'abstract'">
                  <sequence select="error(xs:QName('err:XTSE3075'), 'Cannot use the component reference c:original when the overridden component has visibility=''abstract''.', src:error-object(.))"/>
               </if>
               <sequence select="$original-meta"/>
            </when>
            <otherwise>
               <variable name="meta" select="
                  (reverse($package-manifest/xcst:template)[
                     xcst:EQName(@name) eq $qname
                     and @visibility ne 'hidden'])[1]"/>
               <if test="not($meta)">
                  <sequence select="error(xs:QName('err:XTSE0650'), concat('No template exists named ''', $qname, '''.'), src:error-object(.))"/>
               </if>
               <sequence select="$meta"/>
            </otherwise>
         </choose>
      </variable>

      <variable name="current" select="."/>
      <for-each select="$meta/xcst:param[@required/xs:boolean(.) and not(@tunnel/xs:boolean(.))]">
         <if test="not($current/c:with-param[xcst:name-equals(@name, current()/string(@name))])">
            <sequence select="error(xs:QName('err:XTSE0690'), concat('No value supplied for required parameter ''', @name, '''.'), src:error-object($current))"/>
         </if>
      </for-each>
      <for-each select="c:with-param[not((@tunnel/xcst:boolean(.), false())[1])]">
         <variable name="param-name" select="xcst:name(@name)"/>
         <if test="not($meta/xcst:param[string(@name) eq $param-name])">
            <sequence select="error(xs:QName('err:XTSE0680'), concat('Parameter ''', $param-name, ''' is not declared in the called template.'), src:error-object(.))"/>
         </if>
      </for-each>

      <sequence select="$meta, $original"/>
   </template>

   <template match="c:call-template" mode="xcst:instruction">
      <variable name="result" as="item()+">
         <call-template name="xcst:validate-call-template"/>
      </variable>
      <variable name="meta" select="$result[1]" as="element(xcst:template)"/>
      <variable name="original" select="$result[2]" as="xs:boolean"/>
      <element name="xcst:instruction">
         <if test="$meta/@item-type">
            <attribute name="expression" select="true()"/>
            <if test="$meta/(@qualified-types, ../@qualified-types)[1]/xs:boolean(.)">
               <attribute name="as" select="$meta/@item-type"/>
            </if>
         </if>
      </element>
   </template>

   <template name="src:call-template-context">
      <param name="meta" as="element()?"/>
      <param name="context" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="typed-params" select="boolean($meta/xcst:typed-params(.))"/>

      <text>new </text>
      <value-of select="src:template-context-type($meta)"/>
      <text>(</text>
      <if test="$typed-params">
         <text>new </text>
         <value-of select="src:params-type($meta)"/>
         <call-template name="src:open-brace"/>
         <for-each select="c:with-param[not(@tunnel/xcst:boolean(.))]">
            <call-template name="src:line-number">
               <with-param name="indent" select="$indent + 2" tunnel="yes"/>
            </call-template>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="2"/>
            </call-template>
            <text>@</text>
            <value-of select="src:strip-verbatim-prefix(xcst:name(@name))"/>
            <text> = </text>
            <call-template name="src:value"/>
            <if test="position() ne last()">,</if>
         </for-each>
         <call-template name="src:close-brace">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
         <if test="$context">, </if>
      </if>
      <value-of select="$context"/>
      <text>)</text>
      <apply-templates select="c:with-param[not($typed-params) or @tunnel/xcst:boolean(.)]" mode="src:with-param">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
   </template>

   <template name="src:call-template-output">
      <param name="meta" as="element()"/>
      <param name="dynamic" select="false()" as="xs:boolean"/>
      <param name="output" tunnel="yes"/>

      <choose>
         <when test="$meta/@item-type">
            <value-of select="src:fully-qualified-helper('SequenceWriter')"/>
            <text>.AdjustWriter</text>
            <if test="$dynamic">Dynamically</if>
            <text>(</text>
            <value-of select="$output"/>
            <text>, </text>
            <value-of select="src:global-identifier($meta/(@package-type, ../@package-type)[1]), src:item-type-inference-member-name($meta/@member-name)" separator="."/>
            <text>)</text>
         </when>
         <otherwise>
            <value-of select="$output"/>
         </otherwise>
      </choose>
   </template>

   <template match="c:next-template" mode="src:statement">

      <variable name="meta" as="element(xcst:template)">
         <call-template name="xcst:validate-next-template"/>
      </variable>

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>this.</text>
      <value-of select="$meta/@member-name"/>
      <text>(</text>
      <call-template name="src:call-template-context">
         <with-param name="meta" select="$meta"/>
      </call-template>
      <text>, </text>
      <call-template name="src:call-template-output">
         <with-param name="meta" select="$meta"/>
      </call-template>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:next-template" mode="src:expression">

      <variable name="meta" as="element(xcst:template)">
         <call-template name="xcst:validate-next-template"/>
      </variable>

      <value-of select="src:fully-qualified-helper('SequenceWriter'), 'Create'" separator="."/>
      <text>(</text>
      <value-of select="src:global-identifier($meta/(@package-type, ../@package-type)[1]), src:item-type-inference-member-name($meta/@member-name)" separator="."/>
      <text>).WriteTemplate(this.</text>
      <value-of select="$meta/@member-name"/>
      <text>, </text>
      <call-template name="src:call-template-context">
         <with-param name="meta" select="$meta"/>
      </call-template>
      <text>).Flush</text>
      <if test="$meta/@cardinality eq 'One'">Single</if>
      <text>()</text>
   </template>

   <template name="xcst:validate-next-template">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <call-template name="xcst:validate-attribs"/>
      <call-template name="xcst:validate-children">
         <with-param name="allowed" select="'with-param'"/>
      </call-template>

      <for-each select="c:with-param">
         <call-template name="xcst:validate-attribs">
            <with-param name="required" select="'name'"/>
            <with-param name="optional" select="'value', 'as', 'tunnel'"/>
         </call-template>
         <call-template name="xcst:value-or-sequence-constructor"/>
         <if test="preceding-sibling::c:with-param[xcst:name-equals(@name, current()/@name)]">
            <sequence select="error(xs:QName('err:XTSE0670'), 'Duplicate parameter name.', src:error-object(.))"/>
         </if>
      </for-each>

      <variable name="current-template" select="ancestor::c:template[1]"/>

      <if test="not($current-template)">
         <sequence select="error(xs:QName('err:XTSE0010'), concat('c:', local-name(), ' instruction can only be used within a c:template declaration.'), src:error-object(.))"/>
      </if>

      <variable name="current-meta" select="$package-manifest/xcst:template[@declaration-id eq generate-id($current-template)]"/>

      <variable name="meta" select="
         $current-meta/preceding-sibling::xcst:*[
            xcst:homonymous(., $current-meta) and not(@accepted/xs:boolean(.))][1]"/>

      <if test="not($meta)">
         <sequence select="error(xs:QName('err:XTSE0650'), 'There are no more templates to call.', src:error-object(.))"/>
      </if>

      <variable name="current" select="."/>
      <for-each select="$meta/xcst:param[@required/xs:boolean(.) and not(@tunnel/xs:boolean(.))]">
         <if test="not($current/c:with-param[xcst:name-equals(@name, current()/string(@name))])">
            <sequence select="error(xs:QName('err:XTSE0690'), concat('No value supplied for required parameter ''', @name, '''.'), src:error-object($current))"/>
         </if>
      </for-each>
      <for-each select="c:with-param[not((@tunnel/xcst:boolean(.), false())[1])]">
         <variable name="param-name" select="xcst:name(@name)"/>
         <if test="not($meta/xcst:param[string(@name) eq $param-name])">
            <sequence select="error(xs:QName('err:XTSE0680'), concat('Parameter ''', $param-name, ''' is not declared in the called template.'), src:error-object(.))"/>
         </if>
      </for-each>
      <sequence select="$meta"/>
   </template>

   <template match="c:next-template" mode="xcst:instruction">
      <variable name="meta" as="element(xcst:template)">
         <call-template name="xcst:validate-next-template"/>
      </variable>
      <element name="xcst:instruction">
         <if test="$meta/@item-type">
            <attribute name="expression" select="true()"/>
         </if>
      </element>
   </template>

   <template match="c:with-param" mode="src:with-param">
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>.WithParam(</text>
      <value-of select="src:string(src:strip-verbatim-prefix(xcst:name(@name)))"/>
      <text>, </text>
      <call-template name="src:value"/>
      <if test="@tunnel">
         <text>, tunnel: </text>
         <value-of select="src:boolean(xcst:boolean(@tunnel))"/>
      </if>
      <text>)</text>
   </template>

   <template match="c:next-function" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <variable name="meta" as="element()">
         <call-template name="xcst:validate-next-function"/>
      </variable>
      <variable name="write-object" select="$output and $meta/@as"/>

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <if test="$write-object">
         <value-of select="$output"/>
         <text>.WriteObject(</text>
      </if>
      <apply-templates select="." mode="src:expression">
         <with-param name="meta" select="$meta"/>
         <with-param name="statement" select="true()"/>
      </apply-templates>
      <if test="$write-object">
         <text>)</text>
      </if>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:next-function" mode="src:expression">
      <param name="meta" as="element()">
         <call-template name="xcst:validate-next-function"/>
      </param>
      <param name="statement" select="false()"/>

      <if test="not($statement) and not($meta/@as)">
         <sequence select="error((), 'Cannot compile a void function into an expression.', src:error-object(.))"/>
      </if>
      <value-of select="$meta/@member-name"/>
      <text>(</text>
      <value-of select="@arguments/xcst:expression(.)"/>
      <text>)</text>
   </template>

   <template name="xcst:validate-next-function">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <call-template name="xcst:validate-attribs"/>
      <call-template name="xcst:no-children"/>

      <variable name="current-function" select="ancestor::c:function[1]"/>

      <if test="not($current-function)">
         <sequence select="error(xs:QName('err:XTSE0010'), concat('c:', local-name(), ' instruction can only be used within a c:function declaration.'), src:error-object(.))"/>
      </if>

      <variable name="current-meta" select="$package-manifest/xcst:function[@declaration-id eq generate-id($current-function)]"/>

      <variable name="meta" select="
         $current-meta/preceding-sibling::xcst:*[
            xcst:homonymous(., $current-meta) and not(@accepted/xs:boolean(.))][1]"/>

      <if test="not($meta)">
         <sequence select="error(xs:QName('err:XTSE0650'), 'There are no more functions to call.', src:error-object(.))"/>
      </if>

      <sequence select="$meta"/>
   </template>

   <template match="c:next-function" mode="xcst:instruction">
      <variable name="function-meta" as="element()">
         <call-template name="xcst:validate-next-function"/>
      </variable>
      <element name="xcst:instruction">
         <choose>
            <when test="$function-meta/@as">
               <attribute name="expression" select="true()"/>
            </when>
            <otherwise>
               <attribute name="void" select="true()"/>
            </otherwise>
         </choose>
      </element>
   </template>

   <template match="c:evaluate-package" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'package'"/>
         <with-param name="optional" select="'global-params', 'initial-template', 'template-params', 'tunnel-params', 'value'"/>
      </call-template>
      <call-template name="xcst:no-children"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="src:global-identifier('Xcst.XcstEvaluator')"/>
      <text>.Using</text>
      <text>(</text>
      <value-of select="xcst:expression(@package)"/>
      <text>)</text>
      <if test="@global-params">
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>.WithParams(</text>
         <value-of select="xcst:expression(@global-params)"/>
         <text>)</text>
      </if>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>.Call</text>
      <if test="not(@initial-template)">Initial</if>
      <text>Template(</text>
      <if test="@initial-template">
         <value-of select="src:QName(xcst:EQName(@initial-template, (), false(), true()), src:expand-attribute(@initial-template))"/>
      </if>
      <text>)</text>
      <if test="@template-params">
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>.WithParams(</text>
         <value-of select="xcst:expression(@template-params)"/>
         <text>)</text>
      </if>
      <if test="@tunnel-params">
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>.WithTunnelParams(</text>
         <value-of select="xcst:expression(@tunnel-params)"/>
         <text>)</text>
      </if>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>.OutputTo(</text>
      <value-of select="$output"/>
      <text>)</text>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>.Run()</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <!--
      ## Delegated Templates
   -->

   <template match="c:delegate" mode="src:expression">
      <param name="indent" tunnel="yes"/>

      <call-template name="xcst:validate-attribs"/>
      <variable name="new-context" select="concat(src:aux-variable('context'), '_', generate-id())"/>
      <variable name="new-output" select="concat(src:aux-variable('output'), '_', generate-id())"/>
      <text>new </text>
      <value-of select="src:global-identifier('System.Action')"/>
      <text>&lt;</text>
      <value-of select="src:template-context-type(()), src:template-output-type(())" separator=", "/>
      <text>>((</text>
      <value-of select="$new-context, $new-output" separator=", "/>
      <text>) => </text>
      <call-template name="src:open-brace"/>
      <for-each select="c:param">
         <call-template name="xcst:validate-attribs">
            <with-param name="required" select="'name'"/>
            <with-param name="optional" select="'value', 'as', 'required', 'tunnel'"/>
         </call-template>
         <call-template name="xcst:value-or-sequence-constructor"/>
         <call-template name="xcst:no-other-preceding"/>
         <if test="preceding-sibling::c:param[xcst:name-equals(@name, current()/@name)]">
            <sequence select="error(xs:QName('err:XTSE0580'), 'The name of the parameter is not unique.', src:error-object(.))"/>
         </if>
         <apply-templates select="." mode="src:statement">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            <with-param name="context" select="$new-context" tunnel="yes"/>
         </apply-templates>
      </for-each>
      <call-template name="src:sequence-constructor">
         <with-param name="children" select="node()[not(self::c:param or following-sibling::c:param)]"/>
         <with-param name="omit-block" select="true()"/>
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         <with-param name="context" select="$new-context" tunnel="yes"/>
         <with-param name="output" select="$new-output" tunnel="yes"/>
      </call-template>
      <call-template name="src:close-brace"/>
      <text>)</text>
   </template>

   <template match="c:delegate" mode="xcst:instruction">
      <element name="xcst:instruction">
         <attribute name="as" select="'System.Delegate'"/>
         <attribute name="expression" select="true()"/>
      </element>
   </template>

   <template match="c:evaluate-delegate" mode="src:statement">
      <param name="indent" tunnel="yes"/>
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'delegate'"/>
         <with-param name="optional" select="'with-params'"/>
      </call-template>
      <call-template name="xcst:validate-children">
         <with-param name="allowed" select="'with-param'"/>
      </call-template>
      <for-each select="c:with-param">
         <call-template name="xcst:validate-attribs">
            <with-param name="required" select="'name'"/>
            <with-param name="optional" select="'value', 'as', 'tunnel'"/>
         </call-template>
         <call-template name="xcst:value-or-sequence-constructor"/>
         <if test="preceding-sibling::c:with-param[xcst:name-equals(@name, current()/@name)]">
            <sequence select="error(xs:QName('err:XTSE0670'), 'Duplicate parameter name.', src:error-object(.))"/>
         </if>
      </for-each>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="src:fully-qualified-helper('EvaluateDelegate'), 'Invoke'" separator="."/>
      <text>(</text>
      <value-of select="xcst:expression(@delegate)"/>
      <text>, </text>
      <call-template name="src:call-template-context"/>
      <if test="@with-params">
         <call-template name="src:line-number">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>.WithParams(</text>
         <value-of select="xcst:expression(@with-params)"/>
         <text>)</text>
      </if>
      <text>, </text>
      <value-of select="$output"/>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <!--
      ## Sorting
   -->

   <template name="src:sort">
      <param name="name" required="yes"/>
      <param name="in" required="yes"/>
      <param name="indent" tunnel="yes"/>

      <for-each select="c:sort">
         <call-template name="xcst:validate-attribs">
            <with-param name="optional" select="'value', 'order'"/>
         </call-template>
         <call-template name="xcst:no-children"/>
         <call-template name="xcst:no-other-preceding"/>
         <variable name="indent-increase" select="2"/>
         <call-template name="src:line-number">
            <with-param name="indent" select="$indent + $indent-increase" tunnel="yes"/>
         </call-template>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="if (position() eq 1) then $indent-increase else $indent-increase + 1"/>
         </call-template>
         <choose>
            <when test="position() eq 1">
               <value-of select="src:fully-qualified-helper('Sorting')"/>
               <text>.SortBy(</text>
               <value-of select="$in"/>
               <text>, </text>
            </when>
            <otherwise>.CreateOrderedEnumerable(</otherwise>
         </choose>
         <value-of select="$name, '=>', (@value/xcst:expression(.), $name)[1]"/>
         <if test="position() gt 1">, null</if>
         <text>, </text>
         <value-of select="
            if (@order) then
               src:sort-order-descending(xcst:sort-order-descending(@order, true()), src:expand-attribute(@order))
            else
               src:sort-order-descending(false())"/>
         <text>)</text>
      </for-each>
   </template>

   <!--
      ## Grouping
   -->

   <template match="c:for-each-group[not(@group-size)]" mode="src:statement">

      <call-template name="xcst:validate-for-each-group"/>

      <variable name="grouped-aux">
         <value-of select="src:fully-qualified-helper('Grouping')"/>
         <text>.GroupBy(</text>
         <value-of select="xcst:expression(@in)"/>
         <text>, </text>
         <variable name="param" select="src:aux-variable(generate-id())"/>
         <value-of select="$param, '=>', $param"/>
         <if test="@group-by">
            <text>.</text>
            <value-of select="xcst:expression(@group-by)"/>
         </if>
         <text>)</text>
      </variable>

      <variable name="grouped" select="string($grouped-aux)"/>
      <variable name="name" select="xcst:name(@name)"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>foreach (var </text>
      <value-of select="$name"/>
      <text> in </text>
      <choose>
         <when test="c:sort">
            <call-template name="src:sort">
               <with-param name="name" select="$name"/>
               <with-param name="in" select="$grouped"/>
            </call-template>
         </when>
         <otherwise>
            <value-of select="$grouped"/>
         </otherwise>
      </choose>
      <text>)</text>
      <call-template name="src:sequence-constructor">
         <with-param name="children" select="node()[not(self::c:sort or following-sibling::c:sort)]"/>
         <with-param name="ensure-block" select="true()"/>
      </call-template>
   </template>

   <template match="c:for-each-group[@group-size]" mode="src:statement">
      <param name="indent" tunnel="yes"/>

      <call-template name="xcst:validate-for-each-group"/>

      <if test="c:sort">
         <sequence select="error((), 'c:sort is currently not supported when using ''group-size''.', src:error-object(c:sort[1]))"/>
      </if>

      <variable name="in" select="xcst:expression(@in)"/>
      <variable name="iter" select="concat(src:aux-variable('iter'), '_', generate-id())"/>
      <variable name="helper" select="src:fully-qualified-helper('Grouping')"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="'var', $iter, '=', concat($helper, '.GetEnumerator(', $in, ')')"/>
      <value-of select="$src:statement-delimiter"/>

      <call-template name="src:line-hidden"/>
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>try</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:group-size-try">
         <with-param name="iter" select="$iter"/>
         <with-param name="helper" select="$helper"/>
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </call-template>
      <call-template name="src:close-brace"/>
      <text> finally</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:group-size-finally">
         <with-param name="iter" select="$iter"/>
         <with-param name="helper" select="$helper"/>
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </call-template>
      <call-template name="src:close-brace"/>
   </template>

   <template name="xcst:validate-for-each-group">
      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name', 'in'"/>
         <with-param name="optional" select="'group-by', 'group-size'"/>
      </call-template>
      <if test="count((@group-by, @group-size)) gt 1">
         <sequence select="error(xs:QName('err:XTSE1080'), 'The attributes ''group-by'' and ''group-size'' are mutually exclusive.', src:error-object(.))"/>
      </if>
   </template>

   <template name="src:group-size-try">
      <param name="iter" required="yes"/>
      <param name="helper" required="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="cols" select="concat(src:aux-variable('cols'), '_', generate-id())"/>
      <variable name="buff" select="concat(src:aux-variable('buff'), '_', generate-id())"/>
      <variable name="eof" select="concat(src:aux-variable('eof'), '_', generate-id())"/>

      <call-template name="src:new-line-indented"/>
      <value-of select="'int', $cols, '=', @group-size/src:integer(xcst:integer(., true()), src:expand-attribute(.))"/>
      <value-of select="$src:statement-delimiter"/>

      <call-template name="src:new-line-indented"/>
      <value-of select="'var', $buff, '=', concat($helper, '.CreateMutable(', $iter, ', ', $cols, ')')"/>
      <value-of select="$src:statement-delimiter"/>

      <call-template name="src:new-line-indented"/>
      <value-of select="'bool', $eof, '= false'"/>
      <value-of select="$src:statement-delimiter"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>while (!</text>
      <value-of select="$eof"/>
      <text>)</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:group-size-while">
         <with-param name="iter" select="$iter"/>
         <with-param name="cols" select="$cols"/>
         <with-param name="buff" select="$buff"/>
         <with-param name="eof" select="$eof"/>
         <with-param name="helper" select="$helper"/>
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </call-template>
      <call-template name="src:close-brace"/>
   </template>

   <template name="src:group-size-while">
      <param name="iter" required="yes"/>
      <param name="cols" required="yes"/>
      <param name="buff" required="yes"/>
      <param name="eof" required="yes"/>
      <param name="helper" required="yes"/>
      <param name="indent" tunnel="yes"/>

      <call-template name="src:new-line-indented"/>
      <text>if (!(</text>
      <value-of select="$eof"/>
      <text> = !</text>
      <value-of select="$iter"/>
      <text>.MoveNext()))</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <value-of select="$buff, '.Add(', $iter, '.Current)'" separator=""/>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:close-brace"/>

      <call-template name="src:new-line-indented"/>
      <text>if (</text>
      <value-of select="$buff"/>
      <text>.Count == </text>
      <value-of select="$cols"/>
      <text> || </text>
      <value-of select="$eof"/>
      <text>)</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:group-size-if">
         <with-param name="buff" select="$buff"/>
         <with-param name="helper" select="$helper"/>
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </call-template>
      <call-template name="src:close-brace"/>
   </template>

   <template name="src:group-size-if">
      <param name="buff" required="yes"/>
      <param name="helper" required="yes"/>
      <param name="indent" tunnel="yes"/>

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="'var', xcst:name(@name), '=', concat($helper, '.CreateImmutable(', $buff, ')')"/>
      <value-of select="$src:statement-delimiter"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>try</text>
      <call-template name="src:sequence-constructor">
         <with-param name="children" select="node()[not(self::c:sort or following-sibling::c:sort)]"/>
         <with-param name="ensure-block" select="true()"/>
      </call-template>
      <text> finally</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:line-hidden">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </call-template>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <value-of select="$buff, '.Clear()'" separator=""/>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:close-brace"/>
   </template>

   <template name="src:group-size-finally">
      <param name="iter" required="yes"/>
      <param name="helper" required="yes"/>

      <call-template name="src:new-line-indented"/>
      <value-of select="concat($helper, '.Dispose(', $iter,')')"/>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <!--
      ## Diagnostics
   -->

   <template match="c:assert" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'test'"/>
         <with-param name="optional" select="'value'"/>
      </call-template>
      <call-template name="xcst:value-or-sequence-constructor"/>
      <variable name="text" select="xcst:text(.)"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="src:global-identifier('System.Diagnostics.Debug')"/>
      <text>.Assert(</text>
      <value-of select="xcst:expression(@test)"/>
      <if test="xcst:has-value(., $text)">
         <text>, </text>
         <call-template name="src:simple-content">
            <with-param name="attribute" select="@value"/>
            <with-param name="text" select="$text"/>
         </call-template>
      </if>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:assert" mode="xcst:instruction">
      <element name="xcst:instruction">
         <attribute name="void" select="true()"/>
      </element>
   </template>

   <template match="c:message" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'terminate', 'value'"/>
      </call-template>
      <call-template name="xcst:value-or-sequence-constructor"/>
      <variable name="never-terminate" select="not(@terminate) or xcst:boolean(@terminate, true()) eq false()"/>
      <variable name="always-terminate" select="boolean(@terminate/xcst:boolean(., true()))"/>
      <variable name="use-if" select="not($never-terminate) and not($always-terminate)"/>
      <variable name="terminate-expr" select="
         if (@terminate) then
            src:boolean(xcst:boolean(@terminate, true()), src:expand-attribute(@terminate))
         else
            src:boolean(false())"/>

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="src:global-identifier('System.Diagnostics.Trace')"/>
      <text>.WriteLine(</text>
      <call-template name="src:simple-content">
         <with-param name="attribute" select="@value"/>
      </call-template>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>

      <if test="$use-if">
         <value-of select="$src:new-line"/>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>if (</text>
         <value-of select="$terminate-expr"/>
         <text>)</text>
         <call-template name="src:open-brace"/>
      </if>
      <if test="$use-if or $always-terminate">
         <variable name="err-obj" select="src:error-object(.)"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="if ($use-if) then 1 else 0"/>
         </call-template>
         <text>throw </text>
         <value-of select="src:fully-qualified-helper('DynamicError')"/>
         <text>.Terminate(</text>
         <value-of select="src:verbatim-string(concat('Processing terminated by c:', local-name(), ' at line ', $err-obj[2], ' in ', tokenize($err-obj[1], '/')[last()]))"/>
         <text>)</text>
         <value-of select="$src:statement-delimiter"/>
      </if>
      <if test="$use-if">
         <call-template name="src:close-brace"/>
      </if>
   </template>

   <template match="c:message" mode="xcst:instruction">
      <element name="xcst:instruction">
         <attribute name="void" select="true()"/>
      </element>
   </template>

   <!--
      ## Extensibility and Fallback
   -->

   <template match="c:fallback" mode="src:statement src:expression">
      <call-template name="xcst:validate-attribs"/>
   </template>

   <template match="*[not(self::c:*)]" mode="src:statement">
      <call-template name="src:unknown-element">
         <with-param name="current-mode" select="xs:QName('src:statement')"/>
      </call-template>
   </template>

   <template match="*[not(self::c:*)]" mode="src:expression">
      <call-template name="src:unknown-element">
         <with-param name="current-mode" select="xs:QName('src:expression')"/>
      </call-template>
   </template>

   <template name="src:unknown-element">
      <param name="current-mode" as="xs:QName" required="yes"/>

      <choose>
         <when test="xcst:is-extension-instruction(.)">
            <variable name="result" as="node()*">
               <apply-templates select="." mode="src:extension-instruction">
                  <with-param name="src:current-mode" select="$current-mode" tunnel="yes"/>
               </apply-templates>
            </variable>
            <choose>
               <when test="not(empty($result))">
                  <variable name="instruction" select="."/>
                  <for-each select="$result">
                     <choose>
                        <when test="self::text()">
                           <!--
                              Text is treated as output
                           -->
                           <sequence select="."/>
                        </when>
                        <otherwise>
                           <variable name="node-from-source" select="root(.) is root($instruction)"/>
                           <apply-templates select="." mode="#current">
                              <with-param name="line-number-offset" select="
                                 if ($node-from-source) then 0 
                                 else (src:line-number(.) * -1) + src:line-number($instruction)"
                                 tunnel="yes"/>
                              <with-param name="line-uri" select="
                                 if ($node-from-source) then ()
                                 else document-uri(root($instruction))"
                                 tunnel="yes"/>
                           </apply-templates>
                        </otherwise>
                     </choose>
                  </for-each>
               </when>
               <when test="c:fallback">
                  <for-each select="c:fallback">
                     <call-template name="xcst:validate-attribs"/>
                     <call-template name="src:sequence-constructor"/>
                  </for-each>
               </when>
               <otherwise>
                  <sequence select="error(xs:QName('err:XTDE1450'), 'Unknown extension instruction.', src:error-object(.))"/>
               </otherwise>
            </choose>
         </when>
         <otherwise>
            <call-template name="src:literal-result-element"/>
         </otherwise>
      </choose>
   </template>

   <template match="*" mode="xcst:extension-instruction src:extension-instruction">
      <param name="src:extension-recurse" select="false()"/>

      <if test="not($src:extension-recurse)">
         <apply-imports>
            <with-param name="src:extension-recurse" select="true()"/>
         </apply-imports>
      </if>
   </template>

   <template match="text()" mode="xcst:extension-instruction src:extension-instruction"/>

   <template match="c:void" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'value'"/>
      </call-template>
      <call-template name="xcst:value-or-sequence-constructor"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <choose>
         <when test="@value">
            <value-of select="xcst:expression(@value)"/>
            <value-of select="$src:statement-delimiter"/>
         </when>
         <otherwise>
            <variable name="new-output" select="concat(src:aux-variable('output'), '_', generate-id())"/>
            <text>using (var </text>
            <value-of select="$new-output"/>
            <text> = </text>
            <value-of select="src:fully-qualified-helper('Serialization')"/>
            <text>.Void(this))</text>
            <call-template name="src:sequence-constructor">
               <with-param name="ensure-block" select="true()"/>
               <with-param name="output" select="$new-output" tunnel="yes"/>
            </call-template>
         </otherwise>
      </choose>
   </template>

   <template match="c:void" mode="xcst:instruction">
      <element name="xcst:instruction">
         <attribute name="void" select="true()"/>
      </element>
   </template>

   <template match="c:script" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'src'"/>
      </call-template>
      <call-template name="xcst:text-only"/>
      <variable name="text" select="xcst:text(.)"/>
      <if test="@src or $text">
         <choose>
            <when test="@src">
               <if test="$text">
                  <sequence select="error(xs:QName('err:XTSE0010'), 'The ''src'' attribute must be omitted if the element has content.', src:error-object(.))"/>
               </if>
               <variable name="src" select="resolve-uri(@src, base-uri())"/>
               <if test="not(unparsed-text-available($src))">
                  <sequence select="error((), 'Cannot retrieve script.', src:error-object(.))"/>
               </if>
               <call-template name="src:line-number">
                  <with-param name="line-uri" select="$src" tunnel="yes"/>
                  <with-param name="line-number-offset" select="(src:line-number(.) * -1) + 1" tunnel="yes"/>
               </call-template>
               <value-of select="$src:new-line"/>
               <value-of select="unparsed-text($src)"/>
            </when>
            <otherwise>
               <call-template name="src:line-number"/>
               <value-of select="$src:new-line"/>
               <value-of select="$text"/>
            </otherwise>
         </choose>
         <!-- Make sure following output is not on same line -->
         <call-template name="src:new-line-indented"/>
      </if>
   </template>

   <template match="c:script" mode="xcst:instruction">
      <element name="xcst:instruction">
         <attribute name="void" select="true()"/>
      </element>
   </template>

   <!--
      ## Final Result Trees and Serialization
   -->

   <template match="c:result-document" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'href', 'output', 'format', $src:output-parameters/*[not(self::version)]/local-name()"/>
      </call-template>
      <if test="not(@href) and not(@output)">
         <sequence select="error(xs:QName('err:XTSE0010'), 'At least one of the attributes ''href'' and ''output'' must be specified.', src:error-object(.))"/>
      </if>
      <value-of select="$src:new-line"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <variable name="new-output" select="concat(src:aux-variable('output'), '_', generate-id())"/>
      <text>using (var </text>
      <value-of select="$new-output"/>
      <text> = </text>
      <value-of select="src:fully-qualified-helper('Serialization')"/>
      <text>.ChangeOutput(this, </text>
      <text>new </text>
      <value-of select="src:global-identifier('Xcst.OutputParameters')"/>
      <call-template name="src:open-brace"/>
      <for-each select="@* except (@format, @href, @version)">
         <variable name="setter" as="item()*">
            <apply-templates select="." mode="src:output-parameter-setter"/>
         </variable>
         <!-- can still include standard attributes -->
         <if test="$setter">
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="1"/>
            </call-template>
         </if>
         <sequence select="$setter"/>
         <if test="$setter">,</if>
      </for-each>
      <call-template name="src:close-brace"/>
      <text>, </text>
      <call-template name="src:format-QName"/>
      <text>, (</text>
      <value-of select="src:expression-or-null($output)"/>
      <text>) as </text>
      <value-of select="src:global-identifier('Xcst.XcstWriter')"/>
      <text>, </text>
      <choose>
         <when test="@href">
            <value-of select="src:fully-qualified-helper('DataType')"/>
            <text>.Uri(</text>
            <value-of select="src:expand-attribute(@href)"/>
            <text>)</text>
         </when>
         <otherwise>null</otherwise>
      </choose>
      <if test="@output">
         <text>, </text>
         <value-of select="xcst:expression(@output)"/>
      </if>
      <text>))</text>
      <call-template name="src:sequence-constructor">
         <with-param name="ensure-block" select="true()"/>
         <with-param name="output" select="$new-output" tunnel="yes"/>
      </call-template>
   </template>

   <template match="c:result-document" mode="xcst:instruction">
      <element name="xcst:instruction">
         <attribute name="void" select="true()"/>
      </element>
   </template>

   <template match="c:serialize" mode="src:expression">
      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'format', $src:output-parameters/*[not(self::version)]/local-name()"/>
      </call-template>
      <variable name="new-output" select="concat(src:aux-variable('output'), '_', generate-id())"/>
      <value-of select="src:fully-qualified-helper('Serialization')"/>
      <text>.Serialize(this, </text>
      <call-template name="src:format-QName"/>
      <text>, new </text>
      <value-of select="src:global-identifier('Xcst.OutputParameters')"/>
      <call-template name="src:open-brace"/>
      <for-each select="@* except (@format, @version)">
         <variable name="setter" as="item()*">
            <apply-templates select="." mode="src:output-parameter-setter"/>
         </variable>
         <!-- can still include standard attributes -->
         <if test="$setter">
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="1"/>
            </call-template>
         </if>
         <sequence select="$setter"/>
         <if test="$setter">,</if>
      </for-each>
      <call-template name="src:close-brace"/>
      <text>, (</text>
      <value-of select="$new-output"/>
      <text>) => </text>
      <call-template name="src:sequence-constructor">
         <with-param name="ensure-block" select="true()"/>
         <with-param name="output" select="$new-output" tunnel="yes"/>
      </call-template>
      <text>)</text>
   </template>

   <template match="c:serialize" mode="xcst:instruction">
      <element name="xcst:instruction">
         <attribute name="as" select="'System.String'"/>
         <attribute name="expression" select="true()"/>
      </element>
   </template>

   <template name="src:format-QName">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <choose>
         <when test="@format">
            <variable name="format" select="xcst:EQName(@format, (), false(), true())"/>
            <if test="not(empty($format)) and not($package-manifest/xcst:output[xcst:EQName(@name) eq $format])">
               <sequence select="error(xs:QName('err:XTDE1460'), concat('No output definition exists named ', $format, '.'), src:error-object(.))"/>
            </if>
            <value-of select="src:QName($format, src:expand-attribute(@format))"/>
         </when>
         <otherwise>null</otherwise>
      </choose>
   </template>

   <template match="@*" mode="src:output-parameter-setter"/>

   <template match="@*[namespace-uri()]" mode="src:output-parameter-setter">
      <text>[</text>
      <value-of select="src:QName(node-name(.))"/>
      <text>] = </text>
      <value-of select="src:verbatim-string(string())"/>
   </template>

   <template match="@byte-order-mark | @escape-uri-attributes | @include-content-type | @indent | @omit-xml-declaration | @undeclare-prefixes" mode="src:output-parameter-setter">
      <value-of select="src:output-parameter-property(.)"/>
      <text> = </text>
      <value-of select="src:boolean(xcst:boolean(., not(parent::c:output)), src:expand-attribute(.))"/>
   </template>

   <template match="@cdata-section-elements | @suppress-indentation" mode="src:output-parameter-setter">
      <param name="merged-list" as="xs:QName*"/>

      <value-of select="src:output-parameter-property(.)"/>
      <text> = </text>
      <choose>
         <when test="parent::c:output or not(xcst:is-value-template(.))">
            <variable name="list" select="
               if (parent::c:output) then $merged-list
               else for $s in tokenize(., '\s')[.]
                  return xcst:EQName(., $s, true())
            "/>
            <text>new </text>
            <value-of select="src:global-identifier('Xcst.QualifiedName')"/>
            <text>[] { </text>
            <for-each select="$list">
               <if test="position() gt 1">, </if>
               <sequence select="src:QName(.)"/>
            </for-each>
            <text> }</text>
         </when>
         <otherwise>
            <value-of select="src:fully-qualified-helper('DataType')"/>
            <text>.List(</text>
            <value-of select="src:expand-attribute(.)"/>
            <text>, </text>
            <value-of select="src:fully-qualified-helper('DataType'), 'QName'" separator="."/>
            <text>)</text>
         </otherwise>
      </choose>
   </template>

   <template match="@doctype-public | @doctype-system | @item-separator | @media-type" mode="src:output-parameter-setter">
      <value-of select="src:output-parameter-property(.)"/>
      <text> = </text>
      <value-of select="if (parent::c:output) then src:verbatim-string(string()) else src:expand-attribute(.)"/>
   </template>

   <template match="@encoding" mode="src:output-parameter-setter">
      <value-of select="src:output-parameter-property(.)"/>
      <text> = </text>
      <value-of select="src:global-identifier('System.Text.Encoding')"/>
      <text>.GetEncoding(</text>
      <value-of select="if (parent::c:output) then src:verbatim-string(string()) else src:expand-attribute(.)"/>
      <text>)</text>
   </template>

   <template match="@html-version" mode="src:output-parameter-setter">
      <value-of select="src:output-parameter-property(.)"/>
      <text> = </text>
      <value-of select="src:decimal(xcst:decimal(., not(parent::c:output)), src:expand-attribute(.))"/>
   </template>

   <template match="@indent-spaces" mode="src:output-parameter-setter">
      <value-of select="src:output-parameter-property(.)"/>
      <text> = </text>
      <value-of select="src:integer(xcst:integer(., not(parent::c:output)), src:expand-attribute(.))"/>
   </template>

   <template match="@method" mode="src:output-parameter-setter">
      <value-of select="src:output-parameter-property(.)"/>
      <text> = </text>
      <variable name="string" select="xcst:non-string(.)"/>
      <variable name="qname" select="xcst:EQName(., (), false(), not(parent::c:output))"/>
      <if test="not(empty($qname)) and not(namespace-uri-from-QName($qname)) and not(local-name-from-QName($qname) = ('xml', 'html', 'xhtml', 'text'))">
         <sequence select="error(xs:QName('err:XTSE1570'), concat('Invalid value for ''', name(), '''. Must be one of (xml|html|xhtml|text).'), src:error-object(.))"/>
      </if>
      <value-of select="src:QName($qname, src:expand-attribute(.))"/>
   </template>

   <template match="@standalone" mode="src:output-parameter-setter">
      <value-of select="src:output-parameter-property(.)"/>
      <text> = </text>
      <choose>
         <when test="parent::c:output or not(xcst:is-value-template(.))">
            <value-of select="src:global-identifier('Xcst.XmlStandalone')"/>
            <text>.</text>
            <choose>
               <when test="xcst:non-string(.) eq 'omit'">Omit</when>
               <when test="xcst:boolean(.)">Yes</when>
               <otherwise>No</otherwise>
            </choose>
         </when>
         <otherwise>
            <sequence select="concat(src:fully-qualified-helper('DataType'), '.Standalone(', src:expand-attribute(.), ')')"/>
         </otherwise>
      </choose>
   </template>

   <template match="@version | @output-version" mode="src:output-parameter-setter">
      <value-of select="src:output-parameter-property(.)"/>
      <text> = </text>
      <value-of select="if (parent::c:output) then src:string(xcst:non-string(.)) else src:expand-attribute(.)"/>
   </template>

   <function name="src:output-parameter-property" as="xs:string">
      <param name="node" as="node()"/>

      <sequence select="$src:output-parameters/*[local-name() eq local-name($node)]/string()"/>
   </function>

   <!--
      ## Syntax
   -->

   <template name="xcst:validate-attribs">
      <param name="required" as="xs:string*"/>
      <param name="optional" as="xs:string*"/>
      <param name="extension" select="false()"/>

      <variable name="allowed" select="$required, $optional"/>
      <variable name="root" select="not(parent::*)"/>

      <variable name="std-names" select="
         if (self::c:*) then (QName('', 'version')[not(current()/self::c:output)], QName('', 'expand-text'), QName('', 'extension-element-prefixes'), QName('', 'transform-text'))
         else (xs:QName('c:version'), xs:QName('c:expand-text'), xs:QName('c:extension-element-prefixes'), xs:QName('c:transform-text'), xs:QName('c:use-attribute-sets')[not($extension)])"/>

      <for-each select="if (self::c:*) then @*[node-name(.) = $std-names] else @c:*[not(local-name() eq 'language' and $root)]">
         <if test="not(node-name(.) = $std-names)">
            <sequence select="error(xs:QName('err:XTSE0805'), concat('Unknown XCST attribute ''', name(), '''.'), src:error-object(.))"/>
         </if>
         <choose>
            <when test="local-name() eq 'version' and not(xcst:decimal(.) ge 1.0)">
               <sequence select="error(xs:QName('err:XTSE0020'), concat('Attribute ''', name(), ''' should be 1.0 or greater.'), src:error-object(.))"/>
            </when>
         </choose>
      </for-each>

      <variable name="attribs" select="@*[not(namespace-uri())]
         except (if (self::c:*) then @*[node-name(.) = $std-names and not(local-name() eq 'version' and $root)] else ())"/>

      <variable name="current" select="."/>

      <for-each select="$attribs">
         <if test="not(local-name() = $allowed)">
            <sequence select="error(xs:QName('err:XTSE0090'), concat('Attribute ''', local-name(), ''' is not allowed on element ', name($current)), src:error-object($current))"/>
         </if>
      </for-each>

      <for-each select="$required">
         <if test="not(some $a in $attribs satisfies . eq local-name($a))">
            <sequence select="error(xs:QName('err:XTSE0010'), concat('Element must have an ''', .,''' attribute.'), src:error-object($current))"/>
         </if>
      </for-each>
   </template>

   <template name="xcst:validate-children">
      <param name="allowed" as="xs:string+" required="yes"/>

      <if test="*[not(self::c:*[local-name() = $allowed])] or text()[normalize-space()]">
         <sequence select="error(
            xs:QName('err:XTSE0010'),
            concat('Only ',
               string-join(
                  for $p in 1 to count($allowed)
                  return concat(
                     if ($p[. gt 1] eq count($allowed)) then ' and '
                     else if ($p gt 1) then ', '
                     else '', 'c:', $allowed[$p])
                  , ''),
               ' allowed within c:',
               local-name()),
            src:error-object(.))"/>
      </if>
   </template>

   <template name="xcst:no-other-preceding">
      <variable name="disallowed-preceding"
         select="preceding-sibling::node()[self::text()[normalize-space()] or self::*[node-name(.) ne node-name(current())]][1]"/>
      <if test="$disallowed-preceding">
         <sequence select="error(
            xs:QName('err:XTSE0010'),
            concat('c:', local-name(), ' element cannot be preceded ',
               if ($disallowed-preceding instance of element()) then 'by a different element' else 'with text', '.'),
            src:error-object(.))"/>
      </if>
   </template>

   <template name="xcst:no-other-following">
      <param name="except" select="node-name(.)"/>

      <variable name="disallowed-following"
         select="following-sibling::node()[
            self::text()[normalize-space()]
               or self::*[not(node-name(.) = $except)]
         ][1]"/>
      <if test="$disallowed-following">
         <sequence select="error(
            xs:QName('err:XTSE0010'),
            concat('c:', local-name(), ' element cannot be followed ',
               if ($disallowed-following instance of element()) then 'by a different element' else 'with text', '.'),
            src:error-object(.))"/>
      </if>
   </template>

   <template name="xcst:no-children">
      <if test="* or text()[normalize-space() or xcst:preserve-whitespace(..)]">
         <sequence select="error(xs:QName('err:XTSE0260'), 'Element must be empty.', src:error-object(.))"/>
      </if>
   </template>

   <template name="xcst:text-only">
      <if test="*">
         <sequence select="error(xs:QName('err:XTSE0010'), 'Element can only contain text.', src:error-object(.))"/>
      </if>
   </template>

   <template name="xcst:value-or-sequence-constructor">
      <param name="children" select="node()"/>

      <if test="@value and $children[self::* or self::text()[normalize-space() or xcst:preserve-whitespace(..)]]">
         <sequence select="error(xs:QName('err:XTSE0010'), 'The ''value'' attribute must be omitted if the element has content.', src:error-object(.))"/>
      </if>
   </template>

   <function name="xcst:has-value" as="xs:boolean">
      <param name="el" as="element()"/>
      <param name="text" as="xs:string?"/>

      <sequence select="$el/@value or $text or $el/*"/>
   </function>

   <function name="xcst:text" as="xs:string?">
      <param name="el" as="element()"/>

      <sequence select="xcst:text($el, $el/node())"/>
   </function>

   <function name="xcst:text" as="xs:string?">
      <param name="el" as="element()"/>
      <param name="children" as="node()*"/>

      <if test="not($children[self::*]) and $children[self::text()]">
         <variable name="joined" select="string-join($children[self::text()], '')"/>
         <sequence select="
            if (xcst:preserve-whitespace($el) or normalize-space($joined)) then 
            $joined else ()"/>
      </if>
   </function>

   <function name="xcst:preserve-whitespace">
      <param name="el" as="element()"/>

      <sequence select="$el[self::c:text or self::c:script]
         or $el/ancestor-or-self::*[@xml:space][1]/@xml:space = 'preserve'"/>
   </function>

   <function name="xcst:insignificant-whitespace" as="xs:boolean">
      <param name="t" as="text()"/>

      <sequence select="boolean($t[parent::*[* and not(text()[normalize-space()])]])"/>
   </function>

   <function name="xcst:item-type" as="xs:string">
      <param name="as" as="xs:string"/>

      <sequence select="replace($as, '\[\]$', '')"/>
   </function>

   <function name="xcst:cardinality" as="xs:string">
      <param name="as" as="xs:string?"/>

      <sequence select="if ($as) then ('ZeroOrMore'[ends-with($as, '[]')], 'One')[1] else 'ZeroOrMore'"/>
   </function>

   <template name="xcst:sequence-constructor" as="element(xcst:sequence-constructor)">
      <param name="text" as="xs:string?" required="yes"/>
      <param name="children" select="node()" as="node()*"/>

      <!-- This is a template and not a function to allow access to tunnel parameters -->

      <variable name="text-meta" as="element()">
         <element name="xcst:instruction">
            <attribute name="as" select="'System.String'"/>
            <attribute name="expression" select="true()"/>
         </element>
      </variable>

      <variable name="default-meta" as="element()">
         <element name="xcst:instruction"/>
      </variable>

      <variable name="instructions" as="element(xcst:instruction)*">
         <choose>
            <when test="$text">
               <sequence select="$text-meta"/>
            </when>
            <otherwise>
               <for-each select="$children[self::* or self::text()[not(xcst:insignificant-whitespace(.)) or xcst:preserve-whitespace(..)]]">
                  <choose>
                     <when test="self::text()">
                        <sequence select="$text-meta"/>
                     </when>
                     <otherwise>
                        <variable name="i" as="element(xcst:instruction)?">
                           <choose>
                              <when test="self::c:*">
                                 <apply-templates select="." mode="xcst:instruction"/>
                              </when>
                              <when test="xcst:is-extension-instruction(.)">
                                 <apply-templates select="." mode="xcst:extension-instruction"/>
                              </when>
                           </choose>
                        </variable>
                        <sequence select="($i, $default-meta)[1]"/>
                     </otherwise>
                  </choose>
               </for-each>
            </otherwise>
         </choose>
      </variable>
      <element name="xcst:sequence-constructor">
         <variable name="voids" select="$instructions[@void/xs:boolean(.)]"/>
         <variable name="non-void" select="$instructions except $voids"/>
         <variable name="item-types" select="$non-void[(@qualified-types/xs:boolean(.), true())[1]]/(@as/xcst:item-type(.))"/>
         <variable name="item-type" select="
            if ((count($item-types) + count($voids)) eq count($instructions)
               and count(distinct-values($item-types)) eq 1) then $item-types[1]
            else ()"/>
         <variable name="cardinality" select="
            if ($item-type and count($non-void) eq 1 and xcst:cardinality($non-void[1]/@as) eq 'One') then 'One'
            else if (not($item-type) and count($instructions) eq 1) then ()
            else 'ZeroOrMore'">
         </variable>
         <if test="$item-type">
            <attribute name="item-type" select="$item-type"/>
         </if>
         <if test="count($instructions) eq 1 and $instructions/@expression/xs:boolean(.)">
            <attribute name="expression" select="true()"/>
         </if>
         <if test="$cardinality">
            <attribute name="cardinality" select="$cardinality"/>
         </if>
      </element>
   </template>

   <function name="xcst:non-string" as="xs:string">
      <param name="node" as="node()"/>

      <variable name="string" select="xcst:trim($node)"/>

      <if test="not($string)">
         <sequence select="error(xs:QName('err:XTSE0020'), concat('Value of ''', name($node), ''' must be a non-empty string.'), src:error-object($node))"/>
      </if>

      <sequence select="$string"/>
   </function>

   <function name="xcst:boolean" as="xs:boolean">
      <param name="node" as="node()"/>

      <sequence select="xcst:boolean($node, false())"/>
   </function>

   <function name="xcst:boolean" as="xs:boolean?">
      <param name="node" as="node()"/>
      <param name="avt" as="xs:boolean"/>

      <variable name="string" select="xcst:non-string($node)"/>
      <sequence select="
         if ($string = ('yes', 'true', '1')) then 
            true()
         else if ($string = ('no', 'false', '0')) then
            false()
         else if ($avt and xcst:is-value-template($node)) then
            ()
         else
            error(xs:QName('err:XTSE0020'), concat('Invalid boolean for ', name($node), '.'), src:error-object($node))
      "/>
   </function>

   <function name="xcst:decimal" as="xs:decimal">
      <param name="node" as="node()"/>

      <sequence select="xcst:decimal($node, false())"/>
   </function>

   <function name="xcst:decimal" as="xs:decimal?">
      <param name="node" as="node()"/>
      <param name="avt" as="xs:boolean"/>

      <variable name="string" select="xcst:non-string($node)"/>
      <sequence select="
         if ($avt and xcst:is-value-template($node)) then
            ()
         else if ($string castable as xs:decimal) then
            xs:decimal($string)
         else
            error(xs:QName('err:XTSE0020'), concat('Invalid value for ''', name($node), '''.'), src:error-object($node))
      "/>
   </function>

   <function name="xcst:integer" as="xs:integer">
      <param name="node" as="node()"/>

      <sequence select="xcst:integer($node, false())"/>
   </function>

   <function name="xcst:integer" as="xs:integer?">
      <param name="node" as="node()"/>
      <param name="avt" as="xs:boolean"/>

      <variable name="string" select="xcst:non-string($node)"/>
      <sequence select="
         if ($avt and xcst:is-value-template($node)) then
            ()
         else if ($string castable as xs:integer) then
            xs:integer($string)
         else
            error(xs:QName('err:XTSE0020'), concat('Invalid value for ''', name($node), '''.'), src:error-object($node))
      "/>
   </function>

   <function name="xcst:sort-order-descending" as="xs:boolean">
      <param name="node" as="node()"/>

      <sequence select="xcst:sort-order-descending($node, false())"/>
   </function>

   <function name="xcst:sort-order-descending" as="xs:boolean?">
      <param name="node" as="node()"/>
      <param name="avt" as="xs:boolean"/>

      <variable name="string" select="xcst:non-string($node)"/>
      <sequence select="
         if ($string = ('ascending')) then 
            false()
         else if ($string = ('descending')) then
            true()
         else if ($avt and xcst:is-value-template($node)) then
            ()
         else
            error(xs:QName('err:XTSE0020'), concat('Invalid value for ''', name($node), '''. Must be one of (ascending|descending).'), src:error-object($node))
      "/>
   </function>

   <function name="xcst:type" as="xs:string">
      <param name="node" as="node()"/>

      <variable name="string" select="xcst:non-string($node)"/>
      <sequence select="$string"/>
   </function>

   <function name="xcst:name" as="xs:string">
      <param name="node" as="node()"/>

      <variable name="string" select="xcst:non-string($node)"/>
      <sequence select="$string"/>
   </function>

   <function name="xcst:expression" as="xs:string">
      <param name="node" as="node()"/>

      <if test="not(xcst:non-string($node))">
         <sequence select="error(xs:QName('err:XTSE0020'), concat('Value of ''', name($node), ''' must be a non-empty string.'), src:error-object($node))"/>
      </if>

      <sequence select="string($node)"/>
   </function>

   <function name="xcst:name-equals" as="xs:boolean">
      <param name="a" as="item()"/>
      <param name="b" as="item()"/>

      <variable name="strings" select="
         for $item in ($a, $b)
         return if ($item instance of node()) then xcst:name($item)
         else $item"/>

      <sequence select="$strings[1] eq $strings[2]"/>
   </function>

   <function name="xcst:is-value-template" as="xs:boolean">
      <param name="item" as="item()"/>

      <sequence select="contains(string($item), '{')"/>
   </function>

   <function name="xcst:tvt-enabled" as="xs:boolean">
      <param name="el" as="element()"/>

      <variable name="expand" select="$el/ancestor-or-self::*[(self::c:* and @expand-text) or (not(self::c:*) and @c:expand-text)][1]/(if (self::c:*) then @expand-text else @c:expand-text)"/>
      <sequence select="($expand/xcst:boolean(.), false())[1]"/>
   </function>

   <function name="xcst:transform-text" as="xs:string?">
      <param name="el" as="element()"/>

      <variable name="transform" select="$el/ancestor-or-self::*[(self::c:* and @transform-text) or (not(self::c:*) and @c:transform-text)][1]/(if (self::c:*) then @transform-text else @c:transform-text)"/>
      <variable name="value" select="$transform/xcst:non-string(.)"/>

      <if test="not(empty($value)) and not($value = ('none', 'normalize-space', 'trim'))">
         <sequence select="error(xs:QName('err:XTSE0020'), concat('Invalid value for ''', name($transform), '''. Must be one of (none|normalize-space|trim).'), src:error-object($transform))"/>
      </if>

      <sequence select="$value[. ne 'none']"/>
   </function>

   <function name="xcst:is-reserved-namespace" as="xs:boolean">
      <param name="ns" as="xs:string"/>

      <variable name="core-ns" select="namespace-uri-from-QName(xs:QName('c:foo'))"/>
      <sequence select="$ns eq $core-ns
         or starts-with($ns, concat($ns, '/'))"/>
   </function>

   <function name="xcst:is-extension-instruction" as="xs:boolean">
      <param name="elem" as="element()"/>

      <variable name="extension-namespaces" as="xs:string*">
         <variable name="ext-ns" as="xs:string*">
            <for-each select="$elem/ancestor-or-self::*[(self::c:* and @extension-element-prefixes) or (not(self::c:*) and @c:extension-element-prefixes)]
               /(if (self::c:*) then @extension-element-prefixes else @c:extension-element-prefixes)">
               <variable name="el" select=".."/>
               <for-each select="tokenize(., '\s')[.]">
                  <variable name="default" select=". eq '#default'"/>
                  <variable name="ns" select="namespace-uri-for-prefix((if ($default) then '' else .), $el)"/>
                  <if test="empty($ns)">
                     <sequence select="error(xs:QName('err:XTSE1430'), concat(if ($default) then 'Default namespace' else concat('Namespace prefix ''', ., ''''), ' has not been declared.'), src:error-object($el))"/>
                  </if>
                  <sequence select="$ns"/>
               </for-each>
            </for-each>
         </variable>
         <sequence select="distinct-values($ext-ns)"/>
      </variable>
      <sequence select="namespace-uri($elem) = $extension-namespaces"/>
   </function>

   <function name="xcst:EQName" as="xs:QName?">
      <param name="node" as="node()?"/>

      <sequence select="xcst:EQName($node, ())"/>
   </function>

   <function name="xcst:EQName" as="xs:QName?">
      <param name="node" as="node()?"/>
      <param name="value" as="xs:string?"/>

      <sequence select="xcst:EQName($node, $value, false())"/>
   </function>

   <function name="xcst:EQName" as="xs:QName?">
      <param name="node" as="node()?"/>
      <param name="value" as="xs:string?"/>
      <param name="default" as="xs:boolean"/>

      <sequence select="xcst:EQName($node, $value, $default, false())"/>
   </function>

   <function name="xcst:EQName" as="xs:QName?">
      <param name="node" as="node()?"/>
      <param name="value" as="xs:string?"/>
      <param name="default" as="xs:boolean"/>
      <param name="avt" as="xs:boolean"/>

      <if test="not(empty($node))">
         <variable name="string" select="($value, xcst:non-string($node))[1]"/>
         <variable name="qname-pattern" select="'([^:\{\}]+:)?[^:\{\}]+'"/>
         <choose>
            <when test="matches($string, concat('^Q\{[^\{\}]*\}', $qname-pattern, '$'))">
               <variable name="ns" select="xcst:trim(substring(substring-before($string, '}'), 3))"/>
               <variable name="lexical" select="substring-after($string, '}')"/>
               <sequence select="QName($ns, $lexical)"/>
            </when>
            <when test="matches($string, concat('^', $qname-pattern, '$'))">
               <choose>
                  <when test="$default or contains($string, ':')">
                     <sequence select="resolve-QName($string, $node/..)"/>
                  </when>
                  <otherwise>
                     <sequence select="QName('', $string)"/>
                  </otherwise>
               </choose>
            </when>
            <when test="$avt and xcst:is-value-template($node)"/>
            <otherwise>
               <sequence select="error(xs:QName('err:XTSE0020'), concat('Invalid value for ''', name($node), '''.'), src:error-object($node))"/>
            </otherwise>
         </choose>
      </if>
   </function>

   <function name="xcst:uri-qualified-name" as="xs:string">
      <param name="qname" as="xs:QName"/>

      <sequence select="concat('Q{', namespace-uri-from-QName($qname), '}', local-name-from-QName($qname))"/>
   </function>

   <function name="xcst:trim" as="xs:string">
      <param name="item" as="item()"/>

      <sequence select="replace($item, '^\s*(.+?)\s*$', '$1')"/>
   </function>

   <!--
      ## Expressions
   -->

   <template name="src:simple-content">
      <param name="attribute" as="attribute()?"/>
      <param name="text" select="xcst:text(.)"/>
      <param name="separator" select="()"/>

      <choose>
         <when test="$text">
            <value-of select="src:expand-text(., $text)"/>
         </when>
         <when test="$attribute">
            <value-of select="$src:context-field, 'SimpleContent'" separator="."/>
            <text>.Join(</text>
            <value-of select="($separator, src:string(' '))[1]"/>
            <text>, </text>
            <value-of select="xcst:expression($attribute)"/>
            <text>)</text>
         </when>
         <when test="*">
            <variable name="new-output" select="concat(src:aux-variable('output'), '_', generate-id())"/>
            <value-of select="src:fully-qualified-helper('Serialization')"/>
            <text>.SimpleContent(this, </text>
            <value-of select="($separator, src:string(''))[1]"/>
            <text>, (</text>
            <value-of select="$new-output"/>
            <text>) => </text>
            <call-template name="src:sequence-constructor">
               <with-param name="ensure-block" select="true()"/>
               <with-param name="output" select="$new-output" tunnel="yes"/>
            </call-template>
            <text>)</text>
         </when>
         <otherwise>
            <value-of select="src:string('')"/>
         </otherwise>
      </choose>
   </template>

   <template name="src:value">
      <param name="attribute" select="@value" as="attribute()?"/>
      <param name="text" select="xcst:text(.)"/>

      <variable name="as" select="(self::c:param, self::c:set, self::c:variable, self::c:with-param)/@as/xcst:type(.)"/>

      <choose>
         <when test="$attribute or $text">
            <if test="$as">
               <text>(</text>
               <value-of select="$as"/>
               <text>)(</text>
            </if>
            <value-of select="
               if ($attribute) then xcst:expression($attribute)
               else src:expand-text(., $text)
            "/>
            <if test="$as">)</if>
         </when>
         <when test="*">
            <variable name="children" select="node()"/>
            <variable name="seqctor-meta" as="element()">
               <call-template name="xcst:sequence-constructor">
                  <with-param name="children" select="$children"/>
                  <with-param name="text" select="$text"/>
               </call-template>
            </variable>
            <variable name="item-type" select="if ($as) then xcst:item-type($as) else $seqctor-meta/@item-type/src:global-identifier(string())"/>
            <choose>
               <when test="$seqctor-meta/@expression/xs:boolean(.)">
                  <if test="$as">
                     <text>(</text>
                     <value-of select="$as"/>
                     <text>)(</text>
                  </if>
                  <apply-templates select="$children" mode="src:expression"/>
                  <if test="$as">)</if>
               </when>
               <otherwise>
                  <variable name="new-output" select="concat(src:aux-variable('output'), '_', generate-id())"/>
                  <value-of select="src:fully-qualified-helper('SequenceWriter'), 'Create'" separator="."/>
                  <text>&lt;</text>
                  <value-of select="($item-type, 'object')[1]"/>
                  <text>>().WriteSequenceConstructor((</text>
                  <value-of select="$new-output"/>
                  <text>) => </text>
                  <call-template name="src:sequence-constructor">
                     <with-param name="children" select="$children"/>
                     <with-param name="ensure-block" select="true()"/>
                     <with-param name="output" select="$new-output" tunnel="yes"/>
                  </call-template>
                  <text>).Flush</text>
                  <if test="(if ($as) then xcst:cardinality($as) else $seqctor-meta/@cardinality/string()) eq 'One'">Single</if>
                  <text>()</text>
               </otherwise>
            </choose>
         </when>
         <otherwise>
            <text>default(</text>
            <value-of select="($as, 'object')[1]"/>
            <text>)</text>
         </otherwise>
      </choose>
   </template>

   <template name="src:sequence-constructor">
      <param name="children" select="node()"/>
      <param name="value" as="node()?"/>
      <param name="text" select="xcst:text(., $children)"/>
      <param name="ensure-block" select="false()"/>
      <param name="omit-block" select="false()"/>
      <param name="output" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="complex-content" select="boolean($children[self::*])"/>
      <variable name="use-block" select="not($omit-block) and ($ensure-block or $complex-content)"/>
      <variable name="new-indent" select="if ($use-block) then $indent + 1 else $indent"/>

      <if test="$use-block">
         <call-template name="src:open-brace"/>
      </if>
      <choose>
         <when test="$complex-content">
            <apply-templates select="$children" mode="src:statement">
               <with-param name="indent" select="$new-indent" tunnel="yes"/>
            </apply-templates>
         </when>
         <when test="$value">
            <call-template name="src:line-number">
               <with-param name="indent" select="$new-indent" tunnel="yes"/>
            </call-template>
            <call-template name="src:new-line-indented">
               <with-param name="indent" select="$new-indent" tunnel="yes"/>
            </call-template>
            <value-of select="$output"/>
            <text>.WriteObject(</text>
            <value-of select="xcst:expression($value)"/>
            <text>)</text>
            <value-of select="$src:statement-delimiter"/>
         </when>
         <when test="$text">
            <call-template name="src:line-number">
               <with-param name="indent" select="$new-indent" tunnel="yes"/>
            </call-template>
            <call-template name="src:new-line-indented">
               <with-param name="indent" select="$new-indent" tunnel="yes"/>
            </call-template>
            <value-of select="$output"/>
            <text>.WriteString(</text>
            <value-of select="src:expand-text(., $text)"/>
            <text>)</text>
            <value-of select="$src:statement-delimiter"/>
         </when>
      </choose>
      <if test="$use-block">
         <call-template name="src:close-brace"/>
      </if>
   </template>

   <function name="src:expand-text" as="xs:string">
      <param name="el" as="element()"/>
      <param name="text" as="xs:string"/>

      <variable name="tvt" select="xcst:tvt-enabled($el) and xcst:is-value-template($text)"/>
      <variable name="tt" select="xcst:transform-text($el)"/>

      <variable name="result">
         <choose>
            <when test="$tvt">
               <if test="$tt">
                  <value-of select="src:fully-qualified-helper('SimpleContent')"/>
                  <text>.</text>
                  <value-of select="if ($tt eq 'trim') then 'Trim' else 'NormalizeSpace'"/>
                  <text>(</text>
               </if>
               <value-of select="src:format-value-template($text)"/>
               <if test="$tt">)</if>
            </when>
            <otherwise>
               <value-of select="src:verbatim-string(
                  if ($tt eq 'trim') then xcst:trim($text)
                  else if ($tt eq 'normalize-space') then normalize-space($text)
                  else $text
               )"/>
            </otherwise>
         </choose>
      </variable>

      <sequence select="string($result)"/>
   </function>

   <function name="src:expand-attribute" as="xs:string">
      <param name="attr" as="attribute()"/>

      <variable name="text" select="string($attr)"/>

      <choose>
         <when test="xcst:is-value-template($text)">
            <sequence select="src:format-value-template($text)"/>
         </when>
         <otherwise>
            <sequence select="src:verbatim-string($text)"/>
         </otherwise>
      </choose>
   </function>

   <function name="src:format-value-template" as="xs:string">
      <param name="text" as="xs:string"/>

      <sequence select="concat($src:context-field, '.SimpleContent.FormatValueTemplate(', src:interpolated-string($text), ')')"/>
   </function>

   <function name="src:string" as="xs:string">
      <param name="item" as="item()"/>

      <sequence select="concat('&quot;', $item, '&quot;')"/>
   </function>

   <function name="src:verbatim-string" as="xs:string">
      <param name="item" as="item()"/>

      <variable name="str" select="string($item)"/>

      <choose>
         <when test="string-length($str) eq 0">
            <sequence select="src:string($str)"/>
         </when>
         <otherwise>
            <sequence select="concat('@', src:string(replace($str, '&quot;', '&quot;&quot;')))"/>
         </otherwise>
      </choose>
   </function>

   <function name="src:boolean" as="xs:string">
      <param name="bool" as="xs:boolean"/>

      <sequence select="string($bool)"/>
   </function>

   <function name="src:boolean" as="xs:string">
      <param name="bool" as="xs:boolean?"/>
      <param name="string" as="xs:string"/>

      <choose>
         <when test="$bool instance of xs:boolean">
            <sequence select="src:boolean($bool)"/>
         </when>
         <otherwise>
            <sequence select="concat(src:fully-qualified-helper('DataType'), '.Boolean(', $string, ')')"/>
         </otherwise>
      </choose>
   </function>

   <function name="src:decimal" as="xs:string">
      <param name="decimal" as="xs:decimal"/>

      <sequence select="concat(string($decimal), 'm')"/>
   </function>

   <function name="src:decimal" as="xs:string">
      <param name="decimal" as="xs:decimal?"/>
      <param name="string" as="xs:string"/>

      <choose>
         <when test="$decimal instance of xs:decimal">
            <sequence select="src:decimal($decimal)"/>
         </when>
         <otherwise>
            <sequence select="concat(src:fully-qualified-helper('DataType'), '.Decimal(', $string, ')')"/>
         </otherwise>
      </choose>
   </function>

   <function name="src:integer" as="xs:string">
      <param name="int" as="xs:integer"/>

      <sequence select="string($int)"/>
   </function>

   <function name="src:integer" as="xs:string">
      <param name="integer" as="xs:integer?"/>
      <param name="string" as="xs:string"/>

      <choose>
         <when test="$integer instance of xs:integer">
            <sequence select="src:integer($integer)"/>
         </when>
         <otherwise>
            <sequence select="concat(src:fully-qualified-helper('DataType'), '.Integer(', $string, ')')"/>
         </otherwise>
      </choose>
   </function>

   <function name="src:QName" as="xs:string">
      <param name="qname" as="xs:QName"/>

      <sequence select="concat(
         src:fully-qualified-helper('DataType'), 
         '.QName(', 
         string-join((src:verbatim-string(namespace-uri-from-QName($qname)), src:string(local-name-from-QName($qname))), ', '),
         ')'
      )"/>
   </function>

   <function name="src:QName" as="xs:string">
      <param name="qname" as="xs:QName?"/>
      <param name="string" as="xs:string"/>

      <choose>
         <when test="$qname instance of xs:QName">
            <sequence select="src:QName($qname)"/>
         </when>
         <otherwise>
            <sequence select="concat(src:fully-qualified-helper('DataType'), '.QName(', $string, ')')"/>
         </otherwise>
      </choose>
   </function>

   <function name="src:sort-order-descending" as="xs:string">
      <param name="bool" as="xs:boolean"/>

      <sequence select="src:boolean($bool)"/>
   </function>

   <function name="src:sort-order-descending" as="xs:string">
      <param name="bool" as="xs:boolean?"/>
      <param name="string" as="xs:string"/>

      <choose>
         <when test="$bool instance of xs:boolean">
            <sequence select="src:sort-order-descending($bool)"/>
         </when>
         <otherwise>
            <sequence select="concat(src:fully-qualified-helper('DataType'), '.SortOrderDescending(', $string, ')')"/>
         </otherwise>
      </choose>
   </function>

   <function name="src:string-equals-literal" as="xs:string">
      <param name="left-expr" as="xs:string"/>
      <param name="right-string" as="xs:string"/>

      <choose>
         <when test="$right-string">
            <sequence select="concat($left-expr, ' == ', src:verbatim-string($right-string))"/>
         </when>
         <otherwise>
            <sequence select="concat(src:global-identifier('System.String'), '.IsNullOrEmpty(', $left-expr, ')')"/>
         </otherwise>
      </choose>
   </function>

   <function name="src:expression-or-null" as="xs:string">
      <param name="expr" as="item()?"/>

      <sequence select="if ($expr) then string($expr) else 'null'"/>
   </function>

   <!--
      ## Helpers
   -->

   <template name="src:open-brace">
      <choose>
         <when test="$src:open-brace-on-new-line">
            <call-template name="src:new-line-indented"/>
         </when>
         <otherwise>
            <text> </text>
         </otherwise>
      </choose>
      <text>{</text>
   </template>

   <template name="src:close-brace">
      <call-template name="src:new-line-indented"/>
      <text>}</text>
   </template>

   <template name="src:new-line-indented">
      <param name="indent" select="0" tunnel="yes"/>
      <param name="increase" select="0" as="xs:integer"/>

      <value-of select="$src:new-line"/>
      <value-of select="for $p in (1 to ($indent + $increase)) return $src:indent" separator=""/>
   </template>

   <template name="src:line-number">
      <param name="line-number-offset" select="0" as="xs:integer" tunnel="yes"/>
      <param name="line-uri" as="xs:anyURI?" tunnel="yes"/>

      <if test="$src:use-line-directive">
         <call-template name="src:new-line-indented"/>
         <text>#line </text>
         <value-of select="src:line-number(.) + $line-number-offset"/>
         <text> </text>
         <value-of select="src:string(src:local-path(($line-uri, document-uri(root(.)), base-uri())[1]))"/>
      </if>
   </template>

   <template name="src:line-hidden">
      <if test="$src:use-line-directive">
         <call-template name="src:new-line-indented"/>
         <text>#line hidden</text>
      </if>
   </template>

   <function name="src:fully-qualified-helper" as="xs:string">
      <param name="helper" as="xs:string"/>

      <sequence select="concat(src:global-identifier('Xcst.Runtime'), '.', $helper)"/>
   </function>

   <function name="src:aux-variable" as="xs:string">
      <param name="name" as="xs:string"/>

      <sequence select="concat('__xcst_', $name)"/>
   </function>

   <function name="src:global-identifier" as="xs:string">
      <param name="identifier" as="item()"/>

      <sequence select="concat('global::', $identifier)"/>
   </function>

   <function name="src:strip-verbatim-prefix" as="xs:string">
      <param name="name" as="xs:string"/>

      <sequence select="if (starts-with($name, '@')) then substring($name, 2) else $name"/>
   </function>

   <function name="src:error-object" as="item()+">
      <param name="node" as="node()"/>

      <sequence select="(document-uri(root($node)), xs:anyURI(''))[1], src:line-number($node)"/>
   </function>

</stylesheet>
