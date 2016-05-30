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

   <param name="src:omit-assertions" select="false()" as="xs:boolean"/>
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

   <template match="*[* and not(text()[normalize-space()])]/text()" mode="src:statement"/>

   <template match="*" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <!--
         If statement template does not exist but expression does create text node
         e.g. <c:object>
      -->

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$output"/>
      <text>.WriteString(</text>
      <apply-templates select="." mode="src:expression"/>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="*" mode="src:expression">
      <sequence select="error((), concat('Element &lt;', name(), '> cannot be compiled into an expression.'), src:error-object(.))"/>
   </template>

   <!--
      ## Creating Nodes and Objects
   -->

   <template match="c:attribute" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'name', 'namespace', 'separator', 'value'"/>
         <with-param name="required" select="'name'"/>
      </call-template>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$output"/>
      <text>.</text>
      <variable name="simple-content" select="@value or not(*)"/>
      <choose>
         <when test="$simple-content">WriteAttributeString</when>
         <otherwise>WriteStartAttribute</otherwise>
      </choose>
      <choose>
         <when test="xcst:is-value-template(@name)">
            <text>Lexical(</text>
            <value-of select="src:expand-attribute(@name), (@namespace/src:expand-attribute(.), 'null')[1]" separator=", "/>
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
            <choose>
               <when test="@namespace">
                  <text>, </text>
                  <value-of select="src:expand-attribute(@namespace)"/>
               </when>
               <when test="$prefix">
                  <text>, </text>
                  <value-of select="src:verbatim-string(namespace-uri-from-QName($name))"/>
               </when>
            </choose>
         </otherwise>
      </choose>
      <if test="$simple-content">
         <text>, </text>
         <call-template name="src:simple-content">
            <with-param name="attribute" select="@value"/>
            <with-param name="separator" select="@separator/src:expand-attribute(.)"/>
         </call-template>
      </if>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <if test="not($simple-content)">
         <call-template name="src:apply-children"/>
         <call-template name="src:new-line-indented"/>
         <value-of select="$output"/>
         <text>.</text>
         <text>WriteEndAttribute()</text>
         <value-of select="$src:statement-delimiter"/>
      </if>
   </template>

   <template match="c:comment" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'value'"/>
         <with-param name="required" select="()"/>
      </call-template>
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

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'name', 'namespace', 'use-attribute-sets'"/>
         <with-param name="required" select="'name'"/>
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
               (@namespace/src:expand-attribute(.), 'null')[1],
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
      <call-template name="src:use-attribute-sets">
         <with-param name="attr" select="@use-attribute-sets"/>
      </call-template>
      <call-template name="src:apply-children"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$output"/>
      <text>.</text>
      <text>WriteEndElement()</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:namespace" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'name', 'value'"/>
         <with-param name="required" select="'name'"/>
      </call-template>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$output"/>
      <text>.</text>
      <variable name="simple-content" select="@value or not(*)"/>
      <choose>
         <when test="$simple-content">WriteAttributeString</when>
         <otherwise>WriteStartAttribute</otherwise>
      </choose>
      <text>("xmlns", </text>
      <value-of select="
         if (xcst:is-value-template(@name)) then src:expand-attribute(@name)
         else src:string(xcst:name(@name))"/>
      <text>, null</text>
      <if test="$simple-content">
         <text>, </text>
         <call-template name="src:simple-content">
            <with-param name="attribute" select="@value"/>
         </call-template>
      </if>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <if test="not($simple-content)">
         <call-template name="src:apply-children"/>
         <call-template name="src:new-line-indented"/>
         <value-of select="$output"/>
         <text>.</text>
         <text>WriteEndAttribute()</text>
         <value-of select="$src:statement-delimiter"/>
      </if>
   </template>

   <template match="c:processing-instruction" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'name', 'value'"/>
         <with-param name="required" select="'name'"/>
      </call-template>
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
         <with-param name="allowed" select="'disable-output-escaping'"/>
         <with-param name="required" select="()"/>
      </call-template>
      <variable name="text" select="xcst:text(., false())"/>
      <value-of select="
         if ($text) then src:expand-text(., $text) 
         else src:string('')"/>
   </template>

   <template match="c:value-of" mode="src:expression">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'disable-output-escaping', 'value', 'separator'"/>
         <with-param name="required" select="()"/>
      </call-template>
      <call-template name="src:simple-content">
         <with-param name="attribute" select="@value"/>
         <with-param name="separator" select="@separator/src:expand-attribute(.)"/>
      </call-template>
   </template>

   <template match="c:normalize-space" mode="src:expression">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'value'"/>
         <with-param name="required" select="()"/>
      </call-template>
      <choose>
         <when test="@value or * or xcst:tvt-enabled(.)">
            <value-of select="src:fully-qualified-helper('SimpleContent')"/>
            <text>.NormalizeSpace(</text>
            <call-template name="src:simple-content">
               <with-param name="attribute" select="@value"/>
            </call-template>
            <text>)</text>
         </when>
         <otherwise>
            <variable name="text" select="xcst:text(.)"/>
            <value-of select="src:verbatim-string(normalize-space($text))"/>
         </otherwise>
      </choose>
   </template>

   <template match="text()" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$output"/>
      <text>.WriteString(</text>
      <value-of select="
         if (.. instance of element()) then
            src:expand-text(.., string())
         else 
            src:verbatim-string(string())"/>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template name="src:literal-result-element">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="@*[not(namespace-uri())]/local-name()"/>
         <with-param name="required" select="()"/>
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
      <call-template name="src:use-attribute-sets">
         <with-param name="attr" select="@c:use-attribute-sets"/>
      </call-template>
      <for-each select="@* except @c:*">
         <call-template name="src:new-line-indented"/>
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
      <call-template name="src:apply-children"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$output"/>
      <text>.WriteEndElement()</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template name="src:use-attribute-sets">
      <param name="attr" as="attribute()?"/>
      <param name="package-manifest" tunnel="yes"/>
      <param name="context-param" tunnel="yes"/>

      <if test="$attr">
         <variable name="names" select="
            for $s in tokenize($attr, '\s')[.]
            return xcst:resolve-QName-ignore-default($s, .)"/>
         <variable name="sets" as="xs:string*">
            <variable name="current" select="."/>
            <for-each select="$names">
               <choose>
                  <when test="$current[self::c:attribute-set] and $current/parent::c:override and . eq xs:QName('c:original')">
                     <variable name="current-meta" select="$package-manifest/xcst:attribute-set[@declaration-id eq generate-id($current)]"/>
                     <variable name="original-meta" select="$package-manifest/xcst:attribute-set[@id eq $current-meta/@overrides]"/>
                     <if test="$original-meta/@original-visibility eq 'abstract'">
                        <sequence select="error(xs:QName('err:XTSE3075'), 'Cannot use the component reference c:original when the overridden component has visibility=&quot;abstract&quot;.', src:error-object($attr))"/>
                     </if>
                     <sequence select="src:original-member($original-meta)"/>
                  </when>
                  <otherwise>
                     <variable name="meta" select="$package-manifest/xcst:attribute-set[@visibility ne 'hidden' and xcst:resolve-QName-ignore-default(@name, .) eq current()]"/>
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
            <text>(new </text>
            <value-of select="src:fully-qualified-helper('DynamicContext')"/>
            <text>(</text>
            <value-of select="$context-param"/>
            <text>))</text>
            <value-of select="$src:statement-delimiter"/>
         </for-each>
      </if>
   </template>

   <template match="c:object" mode="src:expression">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'value'"/>
         <with-param name="required" select="()"/>
      </call-template>
      <call-template name="src:value"/>
   </template>

   <!--
      ## Repetition
   -->

   <template match="c:for-each" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'name', 'in', 'as'"/>
         <with-param name="required" select="'name', 'in'"/>
      </call-template>
      <variable name="in" select="xcst:expression(@in)"/>
      <value-of select="$src:new-line"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>foreach (</text>
      <value-of select="(@as/xcst:type(.), 'var')[1]"/>
      <text> </text>
      <value-of select="xcst:name(@name)"/>
      <text> in </text>
      <choose>
         <when test="c:sort">
            <for-each select="c:sort">
               <call-template name="xcst:validate-attribs">
                  <with-param name="allowed" select="'value', 'order'"/>
                  <with-param name="required" select="()"/>
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
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param, '=>', $param"/>
               <if test="@value">.</if>
               <value-of select="@value/xcst:expression(.)"/>
               <if test="position() gt 1">, null</if>
               <text>, </text>
               <variable name="descending-expr" select="
                  if (@order) then
                     src:sort-order-descending(xcst:avt-sort-order-descending(@order), src:expand-attribute(@order))
                  else
                     src:sort-order-descending(false())"/>
               <value-of select="$descending-expr"/>
               <text>)</text>
            </for-each>
         </when>
         <otherwise>
            <value-of select="$in"/>
         </otherwise>
      </choose>
      <text>)</text>
      <call-template name="src:apply-children">
         <with-param name="children" select="node()[not(self::c:sort or following-sibling::c:sort)]"/>
         <with-param name="ensure-block" select="true()"/>
      </call-template>
   </template>

   <template match="c:for-each//c:continue | c:while//c:continue" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="()"/>
         <with-param name="required" select="()"/>
      </call-template>
      <call-template name="src:new-line-indented"/>
      <text>continue</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:for-each//c:break | c:while//c:break" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="()"/>
         <with-param name="required" select="()"/>
      </call-template>
      <call-template name="src:new-line-indented"/>
      <text>break</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:while" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'test'"/>
         <with-param name="required" select="'test'"/>
      </call-template>
      <value-of select="$src:new-line"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>while (</text>
      <value-of select="xcst:expression(@test)"/>
      <text>)</text>
      <call-template name="src:apply-children">
         <with-param name="ensure-block" select="true()"/>
      </call-template>
   </template>

   <!--
      ## Conditional Processing
   -->

   <template match="c:choose" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="()"/>
         <with-param name="required" select="()"/>
      </call-template>
      <apply-templates select="c:*" mode="#current"/>
   </template>

   <template match="c:choose/c:when" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'test'"/>
         <with-param name="required" select="'test'"/>
      </call-template>
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
      <call-template name="src:apply-children">
         <with-param name="ensure-block" select="true()"/>
      </call-template>
   </template>

   <template match="c:choose/c:otherwise" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="()"/>
         <with-param name="required" select="()"/>
      </call-template>
      <text> else</text>
      <call-template name="src:apply-children">
         <with-param name="ensure-block" select="true()"/>
      </call-template>
   </template>

   <template match="c:choose" mode="src:expression">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="()"/>
         <with-param name="required" select="()"/>
      </call-template>
      <text>(</text>
      <apply-templates select="c:*" mode="#current"/>
      <text>)</text>
   </template>

   <template match="c:choose/c:when" mode="src:expression">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'test'"/>
         <with-param name="required" select="'test'"/>
      </call-template>
      <if test="position() gt 1"> : </if>
      <text>(</text>
      <value-of select="xcst:expression(@test)"/>
      <text>) ? </text>
      <call-template name="src:apply-children">
         <with-param name="mode" select="'expression'"/>
      </call-template>
   </template>

   <template match="c:choose/c:otherwise" mode="src:expression">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="()"/>
         <with-param name="required" select="()"/>
      </call-template>
      <text> : </text>
      <call-template name="src:apply-children">
         <with-param name="mode" select="'expression'"/>
      </call-template>
   </template>

   <template match="c:if" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'test'"/>
         <with-param name="required" select="'test'"/>
      </call-template>
      <value-of select="$src:new-line"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>if (</text>
      <value-of select="xcst:expression(@test)"/>
      <text>)</text>
      <call-template name="src:apply-children">
         <with-param name="ensure-block" select="true()"/>
      </call-template>
   </template>

   <template match="c:try" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'rollback-output', 'value'"/>
         <with-param name="required" select="()"/>
      </call-template>
      <variable name="rollback" select="(@rollback-output/xcst:boolean(.), true())[1]"/>
      <if test="$rollback">
         <!-- TODO: Buffering -->
         <sequence select="error((), 'Buffering not supported yet. Use @rollback-output=''no''.', src:error-object(.))"/>
      </if>
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>try</text>
      <call-template name="src:apply-children">
         <with-param name="value" select="@value"/>
         <with-param name="children" select="
            node()[not(self::c:catch
               or preceding-sibling::c:catch
               or self::c:finally
               or preceding-sibling::c:finally)]"/>
         <with-param name="ensure-block" select="true()"/>
      </call-template>
      <apply-templates select="c:catch, c:finally" mode="#current"/>
   </template>

   <template match="c:try/c:catch" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'exception', 'when', 'value'"/>
         <with-param name="required" select="()"/>
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
      <call-template name="src:apply-children">
         <with-param name="value" select="@value"/>
         <with-param name="ensure-block" select="true()"/>
      </call-template>
   </template>

   <template match="c:try/c:finally" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'value'"/>
         <with-param name="required" select="()"/>
      </call-template>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>finally</text>
      <call-template name="src:apply-children">
         <with-param name="value" select="@value"/>
         <with-param name="ensure-block" select="true()"/>
      </call-template>
   </template>

   <!--
      ## Variables and Parameters
   -->

   <template match="c:module/c:param | c:package/c:param | c:override/c:param | c:template/c:param | c:delegate/c:param" mode="src:statement">
      <param name="package-manifest" tunnel="yes"/>
      <param name="context-param" tunnel="yes"/>

      <!-- TODO: $c:original -->

      <variable name="global" select="parent::c:module
         or parent::c:package
         or parent::c:override"/>

      <if test="not($global) and preceding-sibling::c:param[xcst:name-equals(@name, current()/@name)]">
         <sequence select="error(xs:QName('err:XTSE0580'), 'The name of the parameter is not unique.', src:error-object(.))"/>
      </if>

      <variable name="name" select="xcst:name(@name)"/>
      <variable name="text" select="xcst:text(.)"/>
      <variable name="has-default-value" select="xcst:has-value(., $text)"/>
      <variable name="type" select="(@as/xcst:type(.), 'object')[1]"/>
      <variable name="required" select="(@required/xcst:boolean(.), false())[1]"/>

      <if test="$has-default-value and $required">
         <sequence select="error(xs:QName('err:XTSE0010'), 'The ''value'' attribute or child element/text should be omitted when required=''yes''.', src:error-object(.))"/>
      </if>

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
      <value-of select="$context-param"/>
      <text>.Param</text>
      <if test="$required and not($has-default-value)">
         <text>&lt;</text>
         <value-of select="$type"/>
         <text>></text>
      </if>
      <text>(</text>
      <value-of select="src:string(src:strip-verbatim-prefix($name))"/>
      <text>, () => </text>
      <choose>
         <when test="$required and not($has-default-value)">
            <call-template name="src:open-brace"/>
            <text> throw </text>
            <value-of select="src:fully-qualified-helper('DynamicError')"/>
            <text>.</text>
            <choose>
               <when test="$global">RequiredGlobalParameter</when>
               <otherwise>RequiredTemplateParameter</otherwise>
            </choose>
            <text>(</text>
            <value-of select="src:string($name)"/>
            <text>)</text>
            <value-of select="$src:statement-delimiter"/>
            <call-template name="src:close-brace"/>
         </when>
         <otherwise>
            <if test="@as">
               <text>(</text>
               <value-of select="$type"/>
               <text>)</text>
            </if>
            <text>(</text>
            <call-template name="src:value">
               <with-param name="text" select="$text"/>
               <with-param name="fallback" select="concat('default(', $type, ')')"/>
            </call-template>
            <text>)</text>
         </otherwise>
      </choose>
      <if test="@tunnel/xcst:boolean(.)">
         <text>, tunnel: </text>
         <value-of select="src:boolean(true())"/>
      </if>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:variable" mode="src:statement">

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'name', 'value', 'as'"/>
         <with-param name="required" select="'name'"/>
      </call-template>

      <variable name="text" select="xcst:text(.)"/>
      <variable name="has-value" select="xcst:has-value(., $text)"/>

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <choose>
         <when test="@as">
            <value-of select="xcst:type(@as)"/>
         </when>
         <when test="not(@value) and $text">string</when>
         <when test="$has-value">var</when>
         <otherwise>object</otherwise>
      </choose>
      <text> </text>
      <value-of select="xcst:name(@name)"/>
      <if test="$has-value">
         <text> = </text>
         <call-template name="src:value">
            <with-param name="text" select="$text"/>
         </call-template>
      </if>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:module/c:variable | c:package/c:variable | c:override/c:variable" mode="src:statement">
      <param name="package-manifest" tunnel="yes"/>

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

   <!--
      ## Callable Components
   -->

   <template match="c:call-template" mode="src:statement">
      <param name="package-manifest" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>
      <param name="context-param" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'name'"/>
         <with-param name="required" select="'name'"/>
      </call-template>

      <for-each select="c:with-param">
         <call-template name="xcst:validate-attribs">
            <with-param name="allowed" select="'name', 'value', 'as', 'tunnel'"/>
            <with-param name="required" select="'name'"/>
         </call-template>
      </for-each>

      <variable name="qname" select="xcst:resolve-QName-ignore-default(xcst:name(@name), .)"/>
      <variable name="original" select="$qname eq xs:QName('c:original') and ancestor::c:override"/>
      <variable name="meta" as="element(xcst:template)">
         <choose>
            <when test="$original">
               <variable name="current-template" select="ancestor::c:template[1]"/>
               <variable name="current-meta" select="$package-manifest/xcst:template[@declaration-id eq generate-id($current-template)]"/>
               <variable name="original-meta" select="$package-manifest/xcst:template[@id eq $current-meta/@overrides]"/>
               <if test="$original-meta/@original-visibility eq 'abstract'">
                  <sequence select="error(xs:QName('err:XTSE3075'), 'Cannot use the component reference c:original when the overridden component has visibility=&quot;abstract&quot;.', src:error-object(.))"/>
               </if>
               <sequence select="$original-meta"/>
            </when>
            <otherwise>
               <variable name="meta" select="
                  (reverse($package-manifest/xcst:template)[
                     xcst:resolve-QName-ignore-default(@name, .) eq $qname
                     and @visibility ne 'hidden'])[1]"/>
               <if test="not($meta)">
                  <sequence select="error(xs:QName('err:XTSE0650'), concat('No template exists named ', $qname, '.'), src:error-object(.))"/>
               </if>
               <sequence select="$meta"/>
            </otherwise>
         </choose>
      </variable>

      <variable name="current" select="."/>
      <for-each select="$meta/xcst:param[@required/xs:boolean(.) and not(@tunnel/xs:boolean(.))]">
         <if test="not($current/c:with-param[xcst:name-equals(@name, current()/string(@name))])">
            <sequence select="error(xs:QName('err:XTSE0690'), concat('No value supplied for required parameter ', @name, '.'), src:error-object($current))"/>
         </if>
      </for-each>
      <for-each select="c:with-param[not((@tunnel/xcst:boolean(.), false())[1])]">
         <variable name="param-name" select="xcst:name(@name)"/>
         <if test="not($meta/xcst:param[string(@name) eq $param-name])">
            <sequence select="error(xs:QName('err:XTSE0680'), concat('Parameter ', $param-name, ' is not declared in the called template.'), src:error-object(.))"/>
         </if>
      </for-each>

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>this.</text>
      <value-of select="if ($original) then src:original-member($meta) else $meta/@member-name"/>
      <text>(new </text>
      <value-of select="src:fully-qualified-helper('DynamicContext')"/>
      <text>(</text>
      <value-of select="$context-param"/>
      <text>)</text>
      <apply-templates select="c:with-param" mode="src:with-param-for-templates">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:next-template" mode="src:statement">
      <param name="package-manifest" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>
      <param name="context-param" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="()"/>
         <with-param name="required" select="()"/>
      </call-template>

      <for-each select="c:with-param">
         <call-template name="xcst:validate-attribs">
            <with-param name="allowed" select="'name', 'value', 'as', 'tunnel'"/>
            <with-param name="required" select="'name'"/>
         </call-template>
      </for-each>

      <variable name="current-template" select="ancestor::c:template[1]"/>

      <if test="not($current-template)">
         <sequence select="error(xs:QName('err:XTSE0010'), concat('&lt;', name(), '> instruction can only be used within a &lt;c:template> declaration.'), src:error-object(.))"/>
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
            <sequence select="error(xs:QName('err:XTSE0690'), concat('No value supplied for required parameter ', @name, '.'), src:error-object($current))"/>
         </if>
      </for-each>
      <for-each select="c:with-param[not((@tunnel/xcst:boolean(.), false())[1])]">
         <variable name="param-name" select="xcst:name(@name)"/>
         <if test="not($meta/xcst:param[string(@name) eq $param-name])">
            <sequence select="error(xs:QName('err:XTSE0680'), concat('Parameter ', $param-name, ' is not declared in the called template.'), src:error-object(.))"/>
         </if>
      </for-each>

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>this.</text>
      <value-of select="$meta/@member-name"/>
      <text>(new </text>
      <value-of select="src:fully-qualified-helper('DynamicContext')"/>
      <text>(</text>
      <value-of select="$context-param"/>
      <text>)</text>
      <apply-templates select="c:with-param" mode="src:with-param-for-templates">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:with-param" mode="src:with-param-for-templates">

      <if test="preceding-sibling::c:with-param[xcst:name-equals(@name, current()/@name)]">
         <sequence select="error(xs:QName('err:XTSE0670'), 'Duplicate parameter name.', src:error-object(.))"/>
      </if>

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>.WithParam(</text>
      <value-of select="src:string(src:strip-verbatim-prefix(xcst:name(@name)))"/>
      <text>, </text>
      <if test="@as">
         <value-of select="'(', xcst:type(@as), ')'" separator=""/>
      </if>
      <call-template name="src:value"/>
      <if test="@tunnel">
         <text>, tunnel: </text>
         <value-of select="src:boolean(xcst:boolean(@tunnel))"/>
      </if>
      <text>)</text>
   </template>

   <template match="c:next-function" mode="src:statement">
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <apply-templates select="." mode="src:expression"/>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:next-function" mode="src:expression">
      <param name="package-manifest" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="()"/>
         <with-param name="required" select="()"/>
      </call-template>

      <variable name="current-function" select="ancestor::c:function[1]"/>

      <if test="not($current-function)">
         <sequence select="error(xs:QName('err:XTSE0010'), concat('&lt;', name(), '> instruction can only be used within a &lt;c:function> declaration.'), src:error-object(.))"/>
      </if>

      <variable name="current-meta" select="$package-manifest/xcst:function[@declaration-id eq generate-id($current-function)]"/>

      <variable name="function" select="
         $current-meta/preceding-sibling::xcst:*[
            xcst:homonymous(., $current-meta) and not(@accepted/xs:boolean(.))][1]"/>

      <if test="not($function)">
         <sequence select="error(xs:QName('err:XTSE0650'), 'There are no more functions to call.', src:error-object(.))"/>
      </if>

      <value-of select="$function/@member-name"/>
      <text>(</text>
      <for-each select="c:with-param">
         <call-template name="xcst:validate-attribs">
            <with-param name="allowed" select="'name', 'value', 'as', 'tunnel'"/>
            <with-param name="required" select="()"/>
         </call-template>
         <if test="@name and preceding-sibling::c:with-param[@name and xcst:name-equals(@name, current()/@name)]">
            <sequence select="error(xs:QName('err:XTSE0670'), 'Duplicate parameter name.', src:error-object(.))"/>
         </if>
         <if test="@tunnel/xcst:boolean(.)">
            <sequence select="error(xs:QName('err:XTSE0020'), 'For attribute ''tunnel'' on c:next-function/c:with-param, the only permitted values are: ''no'', ''false'', ''0''.', src:error-object(.))"/>
         </if>
         <if test="position() gt 1">, </if>
         <if test="@name">
            <value-of select="xcst:name(@name)"/>
            <text>: </text>
         </if>
         <if test="@as">
            <value-of select="'(', xcst:type(@as), ')'" separator=""/>
         </if>
         <call-template name="src:value"/>
      </for-each>
      <text>)</text>
   </template>

   <template match="c:using-module" mode="src:statement">
      <param name="indent" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'value', 'with-params'"/>
         <with-param name="required" select="'value'"/>
      </call-template>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="src:global-identifier('Xcst.XcstEvaluator')"/>
      <text>.Using(</text>
      <value-of select="xcst:expression(@value)"/>
      <text>)</text>
      <for-each select="c:with-param">
         <call-template name="xcst:validate-attribs">
            <with-param name="allowed" select="'name', 'value', 'as', 'tunnel'"/>
            <with-param name="required" select="'name'"/>
         </call-template>
         <if test="preceding-sibling::c:with-param[xcst:name-equals(@name, current()/@name)]">
            <sequence select="error(xs:QName('err:XTSE0670'), 'Duplicate parameter name.', src:error-object(.))"/>
         </if>
         <if test="@tunnel/xcst:boolean(.)">
            <sequence select="error(xs:QName('err:XTSE0020'), 'For attribute ''tunnel'' on c:using-module/c:with-param, the only permitted values are: ''no'', ''false'', ''0''.', src:error-object(.))"/>
         </if>
         <call-template name="src:line-number">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>.WithParam(</text>
         <value-of select="src:string(src:strip-verbatim-prefix(xcst:name(@name)))"/>
         <text>, </text>
         <if test="@as">
            <value-of select="'(', xcst:type(@as), ')'" separator=""/>
         </if>
         <call-template name="src:value"/>
         <text>)</text>
      </for-each>
      <if test="@with-params">
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>.WithParams(</text>
         <value-of select="xcst:expression(@with-params)"/>
         <text>)</text>
      </if>
      <apply-templates select="c:call-template" mode="src:call-template">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>.Run()</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:using-module/c:call-template" mode="src:call-template">
      <param name="context-param" tunnel="yes"/>
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'name', 'with-params'"/>
         <with-param name="required" select="'name'"/>
      </call-template>

      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>.CallTemplate(</text>
      <!-- TODO: @name AVT -->
      <value-of select="src:QName(xcst:resolve-QName-ignore-default(xcst:name(@name), .))"/>
      <text>)</text>
      <for-each select="c:with-param">
         <call-template name="xcst:validate-attribs">
            <with-param name="allowed" select="'name', 'value', 'as', 'tunnel'"/>
            <with-param name="required" select="'name'"/>
         </call-template>
         <apply-templates select="." mode="src:with-param-for-templates"/>
      </for-each>
      <if test="@with-params">
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>.WithParams(</text>
         <value-of select="xcst:expression(@with-params)"/>
         <text>)</text>
      </if>
      <call-template name="src:new-line-indented"/>
      <text>.OutputTo(</text>
      <value-of select="$output"/>
      <text>, outputUri: </text>
      <value-of select="$context-param"/>
      <text>.CurrentOutputUri)</text>
   </template>

   <!--
      ## Diagnostics
   -->

   <template match="c:assert" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'test', 'error-code', 'value'"/>
         <with-param name="required" select="'test'"/>
      </call-template>
      <if test="not($src:omit-assertions)">
         <value-of select="$src:new-line"/>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>if (!(</text>
         <value-of select="xcst:expression(@test)"/>
         <text>))</text>
         <call-template name="src:open-brace"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>throw </text>
         <value-of select="src:fully-qualified-helper('Diagnostics')"/>
         <text>.AssertFail(</text>
         <call-template name="src:value">
            <with-param name="fallback" select="'null'"/>
         </call-template>
         <if test="@error-code">
            <text>, </text>
            <variable name="error-code" select="xcst:resolve-QName-ignore-default(@error-code, .)"/>
            <value-of select="src:QName($error-code)"/>
         </if>
         <text>)</text>
         <value-of select="$src:statement-delimiter"/>
         <call-template name="src:close-brace"/>
      </if>
   </template>

   <template match="c:message" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'terminate', 'error-code', 'value'"/>
         <with-param name="required" select="()"/>
      </call-template>
      <variable name="never-terminate" select="not(@terminate) or xcst:avt-boolean(@terminate) eq false()"/>
      <variable name="always-terminate" select="boolean(@terminate/xcst:avt-boolean(.))"/>
      <variable name="use-if" select="not($never-terminate) and not($always-terminate)"/>
      <variable name="terminate-expr" select="
         if (@terminate) then
            src:boolean(xcst:avt-boolean(@terminate), src:expand-attribute(@terminate))
         else
            src:boolean(false())"/>
      <variable name="value-expr" as="text()">
         <call-template name="src:value">
            <with-param name="fallback" select="'null'"/>
         </call-template>
      </variable>
      <variable name="error-code-expr">
         <if test="@error-code">
            <variable name="error-code" select="xcst:resolve-QName-ignore-default(@error-code, .)"/>
            <value-of select="src:QName($error-code)"/>
         </if>
      </variable>
      <if test="$use-if">
         <value-of select="$src:new-line"/>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>if (</text>
         <value-of select="$terminate-expr"/>
         <text>)</text>
         <call-template name="src:open-brace"/>
      </if>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="if ($use-if) then 1 else 0"/>
      </call-template>
      <if test="$use-if or $always-terminate">throw </if>
      <value-of select="src:fully-qualified-helper('Diagnostics')"/>
      <text>.Message(</text>
      <value-of select="$value-expr"/>
      <text>, </text>
      <choose>
         <when test="$use-if or $always-terminate">true</when>
         <when test="$never-terminate">false</when>
         <otherwise>
            <value-of select="$terminate-expr"/>
         </otherwise>
      </choose>
      <if test="@error-code">
         <text>, </text>
         <value-of select="$error-code-expr"/>
      </if>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <if test="$use-if">
         <call-template name="src:close-brace"/>
         <call-template name="src:line-number"/>
         <text> else</text>
         <call-template name="src:open-brace"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <value-of select="src:fully-qualified-helper('Diagnostics')"/>
         <text>.Message(</text>
         <value-of select="$value-expr"/>
         <text>, false</text>
         <if test="@error-code">
            <text>, </text>
            <value-of select="$error-code-expr"/>
         </if>
         <text>)</text>
         <value-of select="$src:statement-delimiter"/>
         <call-template name="src:close-brace"/>
      </if>
   </template>

   <!--
      ## Extensibility and Fallback
   -->

   <template match="c:fallback" mode="src:statement src:expression">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="()"/>
         <with-param name="required" select="()"/>
      </call-template>
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

      <variable name="extension-namespaces" select="distinct-values(
         ancestor-or-self::*[(self::c:* and @extension-element-prefixes) or (not(self::c:*) and @c:extension-element-prefixes)]
         /(if (self::c:*) then @extension-element-prefixes else @c:extension-element-prefixes)
         /(for $prefix in tokenize(., '\s')[.] return namespace-uri-for-prefix((if ($prefix eq '#default') then '' else $prefix), ..))
      )"/>
      <choose>
         <when test="namespace-uri() = $extension-namespaces">
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
                     <call-template name="xcst:validate-attribs">
                        <with-param name="allowed" select="()"/>
                        <with-param name="required" select="()"/>
                     </call-template>
                     <call-template name="src:apply-children"/>
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

   <template match="*" mode="src:extension-instruction">
      <param name="src:extension-recurse" select="false()"/>

      <if test="not($src:extension-recurse)">
         <apply-imports>
            <with-param name="src:extension-recurse" select="true()"/>
         </apply-imports>
      </if>
   </template>

   <template match="text()" mode="src:extension-instruction"/>

   <!--
      ## Final Result Trees and Serialization
   -->

   <template match="c:result-document" mode="src:statement">
      <param name="context-param" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'format', $src:output-parameters/*[not(self::version)]/local-name()"/>
         <with-param name="required" select="'href'"/>
      </call-template>
      <value-of select="$src:new-line"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <variable name="new-context" select="concat(src:aux-variable('context'), '_', generate-id())"/>
      <text>using (</text>
      <value-of select="src:fully-qualified-helper('DynamicContext')"/>
      <text> </text>
      <value-of select="$new-context"/>
      <text> = </text>
      <value-of select="src:fully-qualified-helper('Serialization')"/>
      <text>.ChangeOutput(this, </text>
      <value-of select="src:fully-qualified-helper('DataType')"/>
      <text>.Uri(</text>
      <value-of select="src:expand-attribute(@href)"/>
      <text>), </text>
      <call-template name="src:format-QName"/>
      <text>, new </text>
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
      <value-of select="src:expression-or-null($context-param)"/>
      <text>))</text>
      <call-template name="src:apply-children">
         <with-param name="ensure-block" select="true()"/>
         <with-param name="context-param" select="$new-context" tunnel="yes"/>
         <with-param name="output" select="concat($new-context, '.Output')" tunnel="yes"/>
      </call-template>
   </template>

   <template match="c:serialize" mode="src:expression">
      <param name="context-param" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'format', $src:output-parameters/*[not(self::version)]/local-name()"/>
         <with-param name="required" select="()"/>
      </call-template>
      <variable name="new-context" select="concat(src:aux-variable('context'), '_', generate-id())"/>
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
      <text>, </text>
      <value-of select="src:expression-or-null($context-param)"/>
      <text>, (</text>
      <value-of select="$new-context"/>
      <text>) => </text>
      <call-template name="src:apply-children">
         <with-param name="ensure-block" select="true()"/>
         <with-param name="mode" select="'statement'"/>
         <with-param name="context-param" select="$new-context" tunnel="yes"/>
         <with-param name="output" select="concat($new-context, '.Output')" tunnel="yes"/>
      </call-template>
      <text>)</text>
   </template>

   <template name="src:format-QName">
      <param name="package-manifest" tunnel="yes"/>

      <choose>
         <when test="@format">
            <choose>
               <when test="xcst:is-value-template(@format)">
                  <!-- TODO: @format AVT -->
                  <sequence select="error((), 'Attribute value template for @format not supported yet.', src:error-object(.))"/>
               </when>
               <otherwise>
                  <variable name="format" select="xcst:resolve-QName-ignore-default(@format, .)"/>
                  <if test="not($package-manifest/xcst:output[xcst:resolve-QName-ignore-default(@name, .) eq $format])">
                     <sequence select="error(xs:QName('err:XTDE1460'), concat('No output definition exists named ', $format, '.'), src:error-object(.))"/>
                  </if>
                  <value-of select="src:QName($format)"/>
               </otherwise>
            </choose>
         </when>
         <otherwise>null</otherwise>
      </choose>
   </template>

   <template match="@*" mode="src:output-parameter-setter"/>

   <!-- TODO: AVT for <c:result-document> and <c:serialize>, but not for <c:output> -->

   <template match="@*[namespace-uri()]" mode="src:output-parameter-setter">
      <text>[</text>
      <value-of select="src:QName(node-name(.))"/>
      <text>] = </text>
      <value-of select="src:verbatim-string(string())"/>
   </template>

   <template match="@byte-order-mark | @escape-uri-attributes | @include-content-type | @indent | @omit-xml-declaration | @undeclare-prefixes" mode="src:output-parameter-setter">
      <value-of select="src:output-parameter-property(.)"/>
      <text> = </text>
      <value-of select="src:boolean(xcst:boolean(.))"/>
   </template>

   <template match="@cdata-section-elements | @suppress-indentation" mode="src:output-parameter-setter">
      <param name="list-value" select="
         for $s in tokenize(., '\s')[.]
         return resolve-QName($s, ..)"/>

      <value-of select="src:output-parameter-property(.)"/>
      <text> = new </text>
      <value-of select="src:global-identifier('Xcst.QualifiedName')"/>
      <text>[] { </text>
      <for-each select="$list-value">
         <if test="position() gt 1">, </if>
         <sequence select="src:QName(.)"/>
      </for-each>
      <text> }</text>
   </template>

   <template match="@doctype-public | @doctype-system | @item-separator | @media-type" mode="src:output-parameter-setter">
      <value-of select="src:output-parameter-property(.)"/>
      <text> = </text>
      <value-of select="src:verbatim-string(string())"/>
   </template>

   <template match="@encoding" mode="src:output-parameter-setter">
      <value-of select="src:output-parameter-property(.)"/>
      <text> = </text>
      <value-of select="src:global-identifier('System.Text.Encoding')"/>
      <text>.GetEncoding(</text>
      <value-of select="src:verbatim-string(string())"/>
      <text>)</text>
   </template>

   <template match="@html-version" mode="src:output-parameter-setter">
      <value-of select="src:output-parameter-property(.)"/>
      <text> = </text>
      <value-of select="src:decimal(xcst:decimal(.))"/>
   </template>

   <template match="@method" mode="src:output-parameter-setter">
      <value-of select="src:output-parameter-property(.)"/>
      <text> = </text>
      <variable name="string" select="xcst:non-string(.)"/>
      <choose>
         <when test="$string = ('xml', 'html', 'xhtml', 'text')">
            <value-of select="src:QName(QName('', $string))"/>
         </when>
         <otherwise>
            <value-of select="src:QName(resolve-QName(., ..))"/>
         </otherwise>
      </choose>
   </template>

   <template match="@standalone" mode="src:output-parameter-setter">
      <value-of select="src:output-parameter-property(.)"/>
      <text> = </text>
      <value-of select="src:global-identifier('Xcst.XmlStandalone')"/>
      <text>.</text>
      <choose>
         <when test="xcst:non-string(.) eq 'omit'">Omit</when>
         <when test="xcst:boolean(.)">Yes</when>
         <otherwise>No</otherwise>
      </choose>
   </template>

   <template match="@version | @output-version" mode="src:output-parameter-setter">
      <value-of select="src:output-parameter-property(.)"/>
      <text> = </text>
      <value-of select="src:string(xcst:non-string(.))"/>
   </template>

   <function name="src:output-parameter-property" as="xs:string">
      <param name="node" as="node()"/>

      <sequence select="$src:output-parameters/*[local-name() eq local-name($node)]/string()"/>
   </function>

   <!--
      ## Delegated Templates
   -->

   <template match="c:delegate" mode="src:expression">
      <param name="indent" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'as'"/>
         <with-param name="required" select="()"/>
      </call-template>
      <variable name="new-context" select="concat(src:aux-variable('context'), '_', generate-id())"/>
      <text>new </text>
      <value-of select="src:global-identifier(concat('System.', if (@as) then 'Func' else 'Action'))"/>
      <text>&lt;</text>
      <value-of select="src:fully-qualified-helper('DynamicContext'), @as/xcst:type(.)" separator=", "/>
      <text>>((</text>
      <value-of select="$new-context"/>
      <text>) => </text>
      <call-template name="src:open-brace"/>
      <for-each select="c:param">
         <call-template name="xcst:validate-attribs">
            <with-param name="allowed" select="'name', 'value', 'as', 'required', 'tunnel'"/>
            <with-param name="required" select="'name'"/>
         </call-template>
         <apply-templates select="." mode="src:statement">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            <with-param name="context-param" select="$new-context" tunnel="yes"/>
         </apply-templates>
      </for-each>
      <call-template name="src:apply-children">
         <with-param name="children" select="node()[not(self::c:param or following-sibling::c:param)]"/>
         <with-param name="mode" select="'statement'"/>
         <with-param name="omit-block" select="true()"/>
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         <with-param name="context-param" select="$new-context" tunnel="yes"/>
         <with-param name="output" select="concat($new-context, '.Output')" tunnel="yes"/>
      </call-template>
      <call-template name="src:close-brace"/>
      <text>)</text>
   </template>

   <template match="c:delegate | c:evaluate-delegate" mode="src:statement">
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <apply-templates select="." mode="src:expression"/>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:evaluate-delegate" mode="src:expression">
      <param name="indent" tunnel="yes"/>
      <param name="context-param" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'value', 'with-params'"/>
         <with-param name="required" select="'value'"/>
      </call-template>
      <value-of select="xcst:expression(@value)"/>
      <text>(new </text>
      <value-of select="src:fully-qualified-helper('DynamicContext')"/>
      <text>(</text>
      <value-of select="src:expression-or-null($context-param)"/>
      <text>)</text>
      <for-each select="c:with-param">
         <call-template name="xcst:validate-attribs">
            <with-param name="allowed" select="'name', 'value', 'as', 'tunnel'"/>
            <with-param name="required" select="'name'"/>
         </call-template>
         <apply-templates select="." mode="src:with-param-for-templates">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </apply-templates>
      </for-each>
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
      <text>)</text>
   </template>

   <!--
      ## Other
   -->

   <template match="c:return" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'value'"/>
         <with-param name="required" select="()"/>
      </call-template>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="local-name()"/>
      <variable name="text" select="xcst:text(.)"/>
      <if test="xcst:has-value(., $text)">
         <text> </text>
         <call-template name="src:value">
            <with-param name="text" select="$text"/>
         </call-template>
      </if>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:set" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'member', 'value'"/>
         <with-param name="required" select="'member'"/>
      </call-template>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="xcst:expression(@member)"/>
      <text> = </text>
      <call-template name="src:value"/>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:void" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'value'"/>
         <with-param name="required" select="()"/>
      </call-template>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <call-template name="src:value"/>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:using" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'name', 'as', 'value'"/>
         <with-param name="required" select="'value'"/>
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
      <call-template name="src:apply-children">
         <with-param name="ensure-block" select="true()"/>
      </call-template>
   </template>

   <template match="c:script" mode="src:statement">
      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'src'"/>
         <with-param name="required" select="()"/>
      </call-template>
      <choose>
         <when test="@src">
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
            <value-of select="text()"/>
         </otherwise>
      </choose>
      <!-- Make sure following output is not on same line -->
      <call-template name="src:new-line-indented"/>
   </template>

   <!--
      ## Syntax
   -->

   <template name="xcst:validate-attribs">
      <param name="allowed" as="xs:string*" required="yes"/>
      <param name="required" as="xs:string*" required="yes"/>
      <param name="extension" select="false()"/>

      <variable name="std-names" select="
         if (self::c:*) then (QName('', 'version')[not(current()/self::c:output)], QName('', 'expand-text'), QName('', 'extension-element-prefixes'))
         else (xs:QName('c:version'), xs:QName('c:expand-text'), xs:QName('c:extension-element-prefixes'), xs:QName('c:use-attribute-sets')[not($extension)])"/>

      <for-each select="if (self::c:*) then @*[node-name() = $std-names] else @c:*">
         <if test="not(node-name() = $std-names)">
            <sequence select="error(xs:QName('err:XTSE0805'), concat('Unknown XCST attribute @', name(), '.'))"/>
         </if>
         <choose>
            <when test="local-name() eq 'version' and not(xcst:decimal(.) ge 1.0)">
               <sequence select="error(xs:QName('err:XTSE0020'), concat('Attribute @', name(), ' should be 1.0 or greater.'))"/>
            </when>
         </choose>
      </for-each>

      <variable name="attribs" select="@*[not(namespace-uri())]
         except (if (self::c:*) then @*[node-name() = $std-names and (not(local-name() eq 'version') or current()/parent::*)] else ())"/>

      <variable name="context" select="."/>

      <for-each select="$attribs">
         <if test="not(local-name() = $allowed)">
            <sequence select="error(xs:QName('err:XTSE0090'), concat('Attribute @', local-name(), ' is not allowed on element &lt;', name($context), '>.'), src:error-object($context))"/>
         </if>
      </for-each>

      <for-each select="$required">
         <if test="not(some $a in $attribs satisfies . eq local-name($a))">
            <sequence select="error(xs:QName('err:XTSE0010'), concat('Element must have an @', .,' attribute.'), src:error-object($context))"/>
         </if>
      </for-each>
   </template>

   <function name="xcst:has-value" as="xs:boolean">
      <param name="el" as="element()"/>
      <param name="text" as="xs:string?"/>

      <sequence select="$el/@value or $text or $el/*"/>
   </function>

   <function name="xcst:text" as="xs:string?">
      <param name="el" as="element()"/>

      <sequence select="xcst:text($el, true())"/>
   </function>

   <function name="xcst:text" as="xs:string?">
      <param name="el" as="element()"/>
      <param name="ignore-whitespace" as="xs:boolean"/>

      <sequence select="xcst:text($el, $ignore-whitespace, $el/node())"/>
   </function>

   <function name="xcst:text" as="xs:string?">
      <param name="el" as="element()"/>
      <param name="ignore-whitespace" as="xs:boolean"/>
      <param name="children" as="node()*"/>

      <if test="not($children[self::*]) and $children[self::text()]">
         <variable name="joined" select="string-join($children[self::text()], '')"/>
         <sequence select="
            if (not($ignore-whitespace) or normalize-space($joined)) then 
            $joined else ()"/>
      </if>
   </function>

   <function name="xcst:non-string" as="xs:string">
      <param name="node" as="node()"/>

      <sequence select="replace($node, '^\s*(.+?)\s*$', '$1')"/>
   </function>

   <function name="xcst:boolean" as="xs:boolean">
      <param name="node" as="node()"/>

      <variable name="bool" select="xcst:avt-boolean($node)"/>
      <choose>
         <when test="not(empty($bool))">
            <sequence select="$bool"/>
         </when>
         <otherwise>
            <sequence select="error(xs:QName('err:XTSE0020'), concat('Invalid boolean for ', name($node), '.'), src:error-object($node))"/>
         </otherwise>
      </choose>
   </function>

   <function name="xcst:avt-boolean" as="xs:boolean?">
      <param name="node" as="node()"/>

      <variable name="string" select="xcst:non-string($node)"/>
      <sequence select="
         if ($string = ('yes', 'true', '1')) then 
            true()
         else if ($string = ('no', 'false', '0')) then
            false()
         else if (xcst:is-value-template($node)) then
            ()
         else
            error(xs:QName('err:XTSE0020'), concat('Invalid boolean for ', name($node), '.'), src:error-object($node))
      "/>
   </function>

   <function name="xcst:decimal" as="xs:decimal">
      <param name="node" as="node()"/>

      <variable name="string" select="xcst:non-string($node)"/>
      <sequence select="xs:decimal($string)"/>
   </function>

   <function name="xcst:integer" as="xs:integer">
      <param name="node" as="node()"/>

      <variable name="string" select="xcst:non-string($node)"/>
      <sequence select="xs:integer($string)"/>
   </function>

   <function name="xcst:sort-order-descending" as="xs:boolean">
      <param name="node" as="node()"/>

      <variable name="bool" select="xcst:avt-sort-order-descending($node)"/>
      <choose>
         <when test="not(empty($bool))">
            <sequence select="$bool"/>
         </when>
         <otherwise>
            <sequence select="error(xs:QName('err:XTSE0020'), concat('Invalid value for ', name($node), '. Must be one of (ascending|descending)'), src:error-object($node))"/>
         </otherwise>
      </choose>
   </function>

   <function name="xcst:avt-sort-order-descending" as="xs:boolean?">
      <param name="node" as="node()"/>

      <variable name="string" select="xcst:non-string($node)"/>
      <sequence select="
         if ($string = ('ascending')) then 
            false()
         else if ($string = ('descending')) then
            true()
         else if (xcst:is-value-template($node)) then
            ()
         else
            error(xs:QName('err:XTSE0020'), concat('Invalid value for ', name($node), '. Must be one of (ascending|descending)'), src:error-object($node))
      "/>
   </function>

   <function name="xcst:type" as="xs:string">
      <param name="node" as="node()"/>

      <variable name="string" select="xcst:non-string($node)"/>

      <if test="not($string)">
         <sequence select="error(xs:QName('err:XTSE0020'), concat('Value of ', '@'[$node instance of attribute()], name($node), ' must be a non-empty string.'), src:error-object($node))"/>
      </if>

      <sequence select="$string"/>
   </function>

   <function name="xcst:name" as="xs:string">
      <param name="node" as="node()"/>

      <variable name="string" select="xcst:non-string($node)"/>

      <if test="not($string)">
         <sequence select="error(xs:QName('err:XTSE0020'), concat('Value of ', '@'[$node instance of attribute()], name($node), ' must be a non-empty string.'), src:error-object($node))"/>
      </if>

      <sequence select="$string"/>
   </function>

   <function name="xcst:expression" as="xs:string">
      <param name="node" as="node()"/>

      <if test="not(xcst:non-string($node))">
         <sequence select="error(xs:QName('err:XTSE0020'), concat('Value of ', '@'[$node instance of attribute()], name($node), ' must be a non-empty string.'), src:error-object($node))"/>
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

   <function name="xcst:is-reserved-namespace" as="xs:boolean">
      <param name="ns" as="xs:string"/>

      <variable name="core-ns" select="namespace-uri-from-QName(xs:QName('c:foo'))"/>
      <sequence select="$ns eq $core-ns
         or starts-with($ns, concat($ns, '/'))"/>
   </function>

   <function name="xcst:resolve-QName-ignore-default" as="xs:QName?">
      <param name="qname" as="xs:string?"/>
      <param name="element" as="element()"/>

      <if test="not(empty($qname))">
         <sequence select="
            if (contains($qname, ':')) then resolve-QName($qname, $element)
            else QName('', $qname)"/>
      </if>
   </function>

   <!--
      ## Expressions
   -->

   <template name="src:simple-content">
      <param name="attribute" as="attribute()?"/>
      <param name="separator" select="()"/>

      <value-of select="$src:context-field, 'SimpleContent'" separator="."/>
      <text>.Join(</text>
      <value-of select="if ($attribute) then ($separator, src:string(' '))[1] else src:string('')"/>
      <text>, </text>
      <call-template name="src:value">
         <with-param name="attribute" select="$attribute"/>
         <with-param name="fallback" select="src:string('')"/>
      </call-template>
      <text>)</text>
   </template>

   <template name="src:value">
      <param name="attribute" select="@value" as="attribute()?"/>
      <param name="text" select="xcst:text(.)"/>
      <param name="fallback"/>

      <choose>
         <when test="$attribute">
            <value-of select="xcst:expression($attribute)"/>
         </when>
         <when test="$text">
            <value-of select="src:expand-text(., $text)"/>
         </when>
         <!-- TODO: Mixed content -->
         <when test="*">
            <apply-templates select="*" mode="src:expression"/>
         </when>
         <when test="$fallback">
            <value-of select="$fallback"/>
         </when>
      </choose>
   </template>

   <function name="src:expand-text" as="xs:string">
      <param name="el" as="element()"/>
      <param name="text" as="xs:string"/>

      <sequence select="
         if (xcst:tvt-enabled($el) and xcst:is-value-template($text)) then
            src:format-value-template($text)
         else
            src:verbatim-string($text)"/>
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

      <sequence select="concat($src:context-field, '.SimpleContent.FormatValueTemplate(', '$@', src:string(src:escape-value-template($text)), ')')"/>
   </function>

   <function name="src:string" as="xs:string">
      <param name="item" as="item()"/>

      <sequence select="concat('&quot;', $item, '&quot;')"/>
   </function>

   <function name="src:verbatim-string" as="xs:string">
      <param name="item" as="item()"/>

      <sequence select="concat('@', src:string(replace($item, '&quot;', '&quot;&quot;')))"/>
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

   <function name="src:QName" as="xs:string">
      <param name="qname" as="xs:QName"/>

      <sequence select="concat(
         src:fully-qualified-helper('DataType'), 
         '.QName(', 
         string-join((src:verbatim-string(namespace-uri-from-QName($qname)), src:string(local-name-from-QName($qname))), ', '),
         ')'
      )"/>
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

   <template name="src:apply-children">
      <param name="children" select="node()"/>
      <param name="value" as="node()?"/>
      <param name="text" select="xcst:text(., true(), $children)"/>
      <param name="ensure-block" select="false()"/>
      <param name="omit-block" select="false()"/>
      <param name="mode" select="()" as="xs:string?"/>
      <param name="output" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="complex-content" select="boolean($children[self::*])"/>
      <variable name="mode-expression" select="not(empty($mode)) and $mode eq 'expression'"/>
      <variable name="use-block" select="not($omit-block) and ($ensure-block or ($complex-content and not($mode-expression)))"/>
      <variable name="new-indent" select="if ($use-block) then $indent + 1 else $indent"/>

      <if test="$use-block">
         <call-template name="src:open-brace"/>
      </if>
      <choose>
         <when test="$complex-content">
            <choose>
               <when test="empty($mode)">
                  <apply-templates select="$children" mode="#current">
                     <with-param name="indent" select="$new-indent" tunnel="yes"/>
                  </apply-templates>
               </when>
               <when test="$mode eq 'statement'">
                  <apply-templates select="$children" mode="src:statement">
                     <with-param name="indent" select="$new-indent" tunnel="yes"/>
                  </apply-templates>
               </when>
               <when test="$mode eq 'expression'">
                  <apply-templates select="$children" mode="src:expression">
                     <with-param name="indent" select="$new-indent" tunnel="yes"/>
                  </apply-templates>
               </when>
            </choose>
         </when>
         <when test="$value or $text">
            <if test="not($mode-expression)">
               <call-template name="src:line-number">
                  <with-param name="indent" select="$new-indent" tunnel="yes"/>
               </call-template>
               <call-template name="src:new-line-indented">
                  <with-param name="indent" select="$new-indent" tunnel="yes"/>
               </call-template>
               <value-of select="$output"/>
               <text>.WriteString(</text>
            </if>
            <value-of select="if ($value) then xcst:expression($value) else src:expand-text(., $text)"/>
            <if test="not($mode-expression)">
               <text>)</text>
               <value-of select="$src:statement-delimiter"/>
            </if>
         </when>
      </choose>
      <if test="$use-block">
         <call-template name="src:close-brace"/>
      </if>
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
