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
   xmlns:xcst="http://maxtoroq.github.io/XCST/grammar"
   xmlns:err="http://maxtoroq.github.io/XCST/errors"
   xmlns:code="http://maxtoroq.github.io/XCST/code"
   xmlns:src="http://maxtoroq.github.io/XCST/compiled"
   xmlns:cs="http://maxtoroq.github.io/XCST/csharp">

   <import href="xcst-extensions.xsl"/>

   <param name="src:use-line-directive" select="false()" as="xs:boolean"/>

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
         <skip-character-check>SkipCharacterCheck</skip-character-check>
         <standalone>Standalone</standalone>
         <suppress-indentation>SuppressIndentation</suppress-indentation>
         <undeclare-prefixes>UndeclarePrefixes</undeclare-prefixes>
         <version>Version</version>
      </data>
   </variable>

   <variable name="src:object-type" as="element()">
      <code:type-reference name="Object" namespace="System"/>
   </variable>

   <variable name="src:nullable-object-type" as="element()">
      <code:type-reference name="Object" namespace="System">
         <if test="$src:nullable-annotate">
            <attribute name="nullable" select="'true'"/>
         </if>
      </code:type-reference>
   </variable>

   <variable name="src:default-template-type" as="element()">
      <code:type-reference array-dimensions="1">
         <sequence select="$src:nullable-object-type"/>
      </code:type-reference>
   </variable>

   <template match="c:*" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <!--
         If statement template does not exist but expression does append value
         e.g. <c:object>
      -->

      <code:method-call name="WriteObject">
         <call-template name="src:line-number"/>
         <sequence select="$output/src:reference/code:*"/>
         <code:arguments>
            <apply-templates select="." mode="src:expression"/>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="c:*" mode="src:expression">
      <sequence select="error(xs:QName('err:XTSE0010'), concat('Element c:', local-name(), ' cannot be compiled into an expression.'), src:error-object(.))"/>
   </template>

   <template match="c:*" mode="xcst:instruction"/>

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
            <call-template name="src:extension-instruction">
               <with-param name="current-mode" select="$current-mode"/>
            </call-template>
         </when>
         <otherwise>
            <call-template name="src:literal-result-element"/>
         </otherwise>
      </choose>
   </template>


   <!-- ## Creating Nodes and Objects -->

   <template match="c:attribute" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'namespace', 'separator', 'value'"/>
      </call-template>

      <call-template name="xcst:value-or-sequence-constructor"/>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <code:variable name="{$doc-output/src:reference/code:*/@name}">
            <call-template name="src:line-number"/>
            <code:method-call name="CastAttribute">
               <sequence select="src:helper-type('DocumentWriter')"/>
               <code:arguments>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
      </if>

      <variable name="attrib-string" select="@value or not(*)"/>
      <variable name="include-separator" select="not($attrib-string) and @separator"/>
      <variable name="name-avt" select="xcst:is-value-template(@name)"/>

      <code:method-call>
         <attribute name="name">
            <choose>
               <when test="$attrib-string">WriteAttributeString</when>
               <otherwise>WriteStartAttribute</otherwise>
            </choose>
            <if test="$name-avt">Lexical</if>
         </attribute>
         <call-template name="src:line-number"/>
         <sequence select="$doc-output/src:reference/code:*"/>
         <code:arguments>
            <choose>
               <when test="$name-avt">
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="@name"/>
                  </call-template>
                  <choose>
                     <when test="@namespace">
                        <call-template name="src:uri-string">
                           <with-param name="uri" select="xcst:uri(@namespace, true())"/>
                           <with-param name="avt" select="@namespace"/>
                        </call-template>
                     </when>
                     <otherwise>
                        <code:null/>
                     </otherwise>
                  </choose>
               </when>
               <otherwise>
                  <variable name="n" select="xcst:name(@name)"/>
                  <variable name="name" select="if (@namespace) then QName('urn:foo', $n) else resolve-QName($n, .)"/>
                  <variable name="prefix" select="prefix-from-QName($name)"/>
                  <if test="$prefix or $include-separator">
                     <choose>
                        <when test="$prefix">
                           <code:string literal="true">
                              <value-of select="$prefix"/>
                           </code:string>
                        </when>
                        <otherwise>
                           <code:null/>
                        </otherwise>
                     </choose>
                  </if>
                  <code:string literal="true">
                     <value-of select="local-name-from-QName($name)"/>
                  </code:string>
                  <choose>
                     <when test="@namespace">
                        <call-template name="src:uri-string">
                           <with-param name="uri" select="xcst:uri(@namespace, true())"/>
                           <with-param name="avt" select="@namespace"/>
                        </call-template>
                     </when>
                     <when test="$prefix">
                        <code:string verbatim="true">
                           <value-of select="namespace-uri-from-QName($name)"/>
                        </code:string>
                     </when>
                     <when test="$include-separator">
                        <code:null/>
                     </when>
                  </choose>
               </otherwise>
            </choose>
            <choose>
               <when test="$attrib-string">
                  <call-template name="src:simple-content">
                     <with-param name="attribute" select="@value"/>
                     <with-param name="separator" select="@separator"/>
                  </call-template>
               </when>
               <when test="$include-separator">
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="@separator"/>
                  </call-template>
               </when>
            </choose>
         </code:arguments>
      </code:method-call>

      <if test="not($attrib-string)">
         <code:try line-hidden="true">
            <code:block>
               <call-template name="src:sequence-constructor">
                  <with-param name="output" select="$doc-output" tunnel="yes"/>
               </call-template>
            </code:block>
            <code:finally line-hidden="true">
               <code:method-call name="WriteEndAttribute">
                  <sequence select="$doc-output/src:reference/code:*"/>
               </code:method-call>
            </code:finally>
         </code:try>
      </if>
   </template>

   <template match="c:comment" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'value'"/>
      </call-template>

      <call-template name="xcst:value-or-sequence-constructor"/>

      <code:method-call name="WriteComment">
         <call-template name="src:line-number"/>
         <sequence select="$output/src:reference/code:*"/>
         <code:arguments>
            <call-template name="src:simple-content">
               <with-param name="attribute" select="@value"/>
            </call-template>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="c:copy-of" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'value'"/>
      </call-template>

      <code:method-call name="CopyOf">
         <call-template name="src:line-number"/>
         <sequence select="$output/src:reference/code:*"/>
         <code:arguments>
            <code:expression value="{xcst:expression(@value)}"/>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="c:document" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs"/>

      <variable name="doc-output" select="src:doc-output(., ())"/>

      <code:using>
         <call-template name="src:line-number"/>
         <code:variable name="{$doc-output/src:reference/code:*/@name}">
            <code:method-call name="CreateDocument">
               <sequence select="src:helper-type('DocumentWriter')"/>
               <code:arguments>
                  <code:this-reference/>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
         <code:block>
            <call-template name="src:sequence-constructor">
               <with-param name="output" select="$doc-output" tunnel="yes"/>
            </call-template>
         </code:block>
      </code:using>
   </template>

   <template match="c:document" mode="xcst:instruction">
      <xcst:instruction>
         <code:type-reference name="XDocument" namespace="System.Xml.Linq"/>
      </xcst:instruction>
   </template>

   <template match="c:element" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'namespace', 'use-attribute-sets'"/>
      </call-template>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <code:variable name="{$doc-output/src:reference/code:*/@name}">
            <call-template name="src:line-number"/>
            <code:method-call name="CastElement">
               <sequence select="src:helper-type('DocumentWriter')"/>
               <code:arguments>
                  <code:this-reference/>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
      </if>

      <variable name="name-avt" select="xcst:is-value-template(@name)"/>

      <code:method-call>
         <attribute name="name">
            <text>WriteStartElement</text>
            <if test="$name-avt">Lexical</if>
         </attribute>
         <call-template name="src:line-number"/>
         <sequence select="$doc-output/src:reference/code:*"/>
         <code:arguments>
            <choose>
               <when test="$name-avt">
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="@name"/>
                  </call-template>
                  <choose>
                     <when test="@namespace">
                        <call-template name="src:uri-string">
                           <with-param name="uri" select="xcst:uri(@namespace, true())"/>
                           <with-param name="avt" select="@namespace"/>
                        </call-template>
                     </when>
                     <otherwise>
                        <code:null/>
                     </otherwise>
                  </choose>
                  <code:string verbatim="true">
                     <value-of select="namespace-uri-from-QName(resolve-QName('foo', .))"/>
                  </code:string>
               </when>
               <otherwise>
                  <variable name="n" select="xcst:name(@name)"/>
                  <variable name="name" select="if (@namespace) then QName('urn:foo', $n) else resolve-QName($n, .)"/>
                  <variable name="prefix" select="prefix-from-QName($name)"/>
                  <if test="$prefix">
                     <code:string literal="true">
                        <value-of select="$prefix"/>
                     </code:string>
                  </if>
                  <code:string literal="true">
                     <value-of select="local-name-from-QName($name)"/>
                  </code:string>
                  <choose>
                     <when test="@namespace">
                        <call-template name="src:uri-string">
                           <with-param name="uri" select="xcst:uri(@namespace, true())"/>
                           <with-param name="avt" select="@namespace"/>
                        </call-template>
                     </when>
                     <otherwise>
                        <code:string verbatim="true">
                           <value-of select="namespace-uri-from-QName($name)"/>
                        </code:string>
                     </otherwise>
                  </choose>
               </otherwise>
            </choose>
         </code:arguments>
      </code:method-call>

      <code:try line-hidden="true">
         <code:block>
            <call-template name="src:use-attribute-sets">
               <with-param name="attr" select="@use-attribute-sets"/>
               <with-param name="output" select="$doc-output" tunnel="yes"/>
            </call-template>
            <call-template name="src:sequence-constructor">
               <with-param name="output" select="$doc-output" tunnel="yes"/>
            </call-template>
         </code:block>
         <code:finally line-hidden="true">
            <code:method-call name="WriteEndElement">
               <sequence select="$doc-output/src:reference/code:*"/>
            </code:method-call>
         </code:finally>
      </code:try>
   </template>

   <template match="c:element" mode="xcst:instruction">
      <xcst:instruction>
         <code:type-reference name="XElement" namespace="System.Xml.Linq"/>
      </xcst:instruction>
   </template>

   <template match="c:namespace" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'value'"/>
      </call-template>

      <call-template name="xcst:value-or-sequence-constructor"/>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <code:variable name="{$doc-output/src:reference/code:*/@name}">
            <call-template name="src:line-number"/>
            <code:method-call name="CastNamespace">
               <sequence select="src:helper-type('DocumentWriter')"/>
               <code:arguments>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
      </if>

      <variable name="attrib-string" select="@value or not(*)"/>

      <code:method-call name="{if ($attrib-string) then 'WriteAttributeString' else 'WriteStartAttribute'}">
         <call-template name="src:line-number"/>
         <sequence select="$doc-output/src:reference/code:*"/>
         <code:arguments>
            <code:string literal="true">xmlns</code:string>
            <call-template name="src:ncname-string">
               <with-param name="ncname" select="xcst:ncname(@name, true())"/>
               <with-param name="avt" select="@name"/>
            </call-template>
            <code:null/>
            <if test="$attrib-string">
               <call-template name="src:simple-content">
                  <with-param name="attribute" select="@value"/>
               </call-template>
            </if>
         </code:arguments>
      </code:method-call>

      <if test="not($attrib-string)">
         <code:try line-hidden="true">
            <code:block>
               <call-template name="src:sequence-constructor">
                  <with-param name="output" select="$doc-output" tunnel="yes"/>
               </call-template>
            </code:block>
            <code:finally line-hidden="true">
               <code:method-call name="WriteEndAttribute">
                  <sequence select="$doc-output/src:reference/code:*"/>
               </code:method-call>
            </code:finally>
         </code:try>
      </if>
   </template>

   <template match="c:processing-instruction" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'value'"/>
      </call-template>

      <call-template name="xcst:value-or-sequence-constructor"/>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <!--
         Using 'name' attribute to generate unique output name because src:simple-content
         uses element for the same purpose
      -->
      <variable name="doc-output" select="src:doc-output(@name, $output)"/>

      <if test="not($output-is-doc)">
         <code:variable name="{$doc-output/src:reference/code:*/@name}">
            <call-template name="src:line-number"/>
            <code:method-call name="CastProcessingInstruction">
               <sequence select="src:helper-type('DocumentWriter')"/>
               <code:arguments>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
      </if>

      <code:method-call name="WriteProcessingInstruction">
         <call-template name="src:line-number"/>
         <sequence select="$doc-output/src:reference/code:*"/>
         <code:arguments>
            <call-template name="src:ncname-string">
               <with-param name="ncname" select="xcst:ncname(@name, true())"/>
               <with-param name="avt" select="@name"/>
            </call-template>
            <code:method-call name="TrimStart">
               <call-template name="src:simple-content">
                  <with-param name="attribute" select="@value"/>
               </call-template>
            </code:method-call>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="c:text | c:value-of" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <code:method-call name="{if (@disable-output-escaping/xcst:boolean(.)) then 'WriteRaw' else 'WriteString'}">
         <call-template name="src:line-number"/>
         <sequence select="$output/src:reference/code:*"/>
         <code:arguments>
            <apply-templates select="." mode="src:expression"/>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="c:text" mode="src:expression">

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'disable-output-escaping'"/>
      </call-template>

      <call-template name="xcst:text-only"/>

      <variable name="text" select="xcst:text(.)"/>

      <choose>
         <when test="$text">
            <call-template name="src:expand-text">
               <with-param name="el" select="."/>
               <with-param name="text" select="$text"/>
            </call-template>
         </when>
         <otherwise>
            <code:string/>
         </otherwise>
      </choose>
   </template>

   <template match="c:value-of" mode="src:expression">

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'disable-output-escaping', 'value', 'separator'"/>
      </call-template>

      <call-template name="xcst:value-or-sequence-constructor"/>

      <call-template name="src:simple-content">
         <with-param name="attribute" select="@value"/>
         <with-param name="separator" select="@separator"/>
      </call-template>
   </template>

   <template match="c:value-of | c:text" mode="xcst:instruction">
      <xcst:instruction expression="true">
         <code:type-reference name="String" namespace="System"/>
      </xcst:instruction>
   </template>

   <template match="text()[xcst:insignificant-whitespace(.)]" mode="src:statement src:expression">
      <if test="xcst:preserve-whitespace(..)">
         <next-match/>
      </if>
   </template>

   <template match="text()" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <code:method-call name="WriteString">
         <call-template name="src:line-number"/>
         <sequence select="$output/src:reference/code:*"/>
         <code:arguments>
            <apply-templates select="." mode="src:expression"/>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="text()" mode="src:expression">
      <variable name="text" select="string()"/>
      <choose>
         <when test=".. instance of element()">
            <call-template name="src:expand-text">
               <with-param name="el" select=".."/>
               <with-param name="text" select="$text"/>
            </call-template>
         </when>
         <otherwise>
            <code:string verbatim="true">
               <attribute name="xml:space" select="'preserve'"/>
               <value-of select="$text"/>
            </code:string>
         </otherwise>
      </choose>
   </template>

   <template name="src:literal-result-element">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="@*[not(namespace-uri())]/local-name()"/>
      </call-template>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <code:variable name="{$doc-output/src:reference/code:*/@name}">
            <call-template name="src:line-number"/>
            <code:method-call name="CastElement">
               <sequence select="src:helper-type('DocumentWriter')"/>
               <code:arguments>
                  <code:this-reference/>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
      </if>

      <variable name="prefix" select="prefix-from-QName(node-name(.))"/>

      <code:method-call name="WriteStartElement">
         <call-template name="src:line-number"/>
         <sequence select="$doc-output/src:reference/code:*"/>
         <code:arguments>
            <if test="$prefix">
               <code:string literal="true">
                  <value-of select="$prefix"/>
               </code:string>
            </if>
            <code:string literal="true">
               <value-of select="local-name()"/>
            </code:string>
            <code:string verbatim="true">
               <value-of select="namespace-uri()"/>
            </code:string>
         </code:arguments>
      </code:method-call>

      <code:try line-hidden="true">
         <code:block>
            <call-template name="src:use-attribute-sets">
               <with-param name="attr" select="@c:use-attribute-sets"/>
               <with-param name="output" select="$doc-output" tunnel="yes"/>
            </call-template>
            <for-each select="@* except @c:*">
               <code:method-call name="WriteAttributeString">
                  <sequence select="$doc-output/src:reference/code:*"/>
                  <code:arguments>
                     <variable name="attr-prefix" select="prefix-from-QName(node-name(.))"/>
                     <if test="$attr-prefix">
                        <code:string literal="true">
                           <value-of select="$attr-prefix"/>
                        </code:string>
                     </if>
                     <code:string literal="true">
                        <value-of select="local-name()"/>
                     </code:string>
                     <if test="$attr-prefix">
                        <code:string verbatim="true">
                           <value-of select="namespace-uri()"/>
                        </code:string>
                     </if>
                     <call-template name="src:expand-attribute">
                        <with-param name="attr" select="."/>
                        <with-param name="lre" select="true()"/>
                     </call-template>
                  </code:arguments>
               </code:method-call>
            </for-each>
            <call-template name="src:sequence-constructor">
               <with-param name="output" select="$doc-output" tunnel="yes"/>
            </call-template>
         </code:block>
         <code:finally line-hidden="true">
            <code:method-call name="WriteEndElement">
               <sequence select="$doc-output/src:reference/code:*"/>
            </code:method-call>
         </code:finally>
      </code:try>
   </template>

   <template name="xcst:literal-result-element-instruction">
      <xcst:instruction>
         <code:type-reference name="XElement" namespace="System.Xml.Linq"/>
      </xcst:instruction>
   </template>

   <template name="src:use-attribute-sets">
      <param name="attr" as="attribute()?"/>
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="context" tunnel="yes"/>
      <param name="output" tunnel="yes"/>

      <if test="$attr">
         <variable name="names" select="
            for $s in xcst:list($attr)
            return xcst:EQName($attr, $s)"/>
         <variable name="sets" as="element()*">
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
                     <sequence select="src:template-member($meta)"/>
                  </otherwise>
               </choose>
            </for-each>
         </variable>
         <for-each select="$sets">
            <code:method-call>
               <sequence select="."/>
               <code:arguments>
                  <choose>
                     <when test="$context">
                        <sequence select="$context/src:reference/code:*"/>
                     </when>
                     <otherwise>
                        <code:null/>
                     </otherwise>
                  </choose>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </for-each>
      </if>
   </template>

   <template match="c:object" mode="src:expression">

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'value'"/>
      </call-template>

      <call-template name="xcst:no-children"/>

      <code:expression value="{xcst:expression(@value)}"/>
   </template>

   <template match="c:object" mode="xcst:instruction">
      <xcst:instruction expression="true"/>
   </template>

   <template match="c:map" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs"/>

      <variable name="output-is-map" select="src:output-is-map($output)"/>
      <variable name="map-output" select="src:map-output(., $output)"/>

      <if test="not($output-is-map)">
         <code:variable name="{$map-output/src:reference/code:*/@name}">
            <call-template name="src:line-number"/>
            <code:method-call name="Create">
               <sequence select="src:helper-type('MapWriter')"/>
               <code:arguments>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
      </if>

      <code:method-call name="WriteStartMap">
         <call-template name="src:line-number"/>
         <sequence select="$map-output/src:reference/code:*"/>
      </code:method-call>

      <code:try line-hidden="true">
         <code:block>
            <call-template name="src:sequence-constructor">
               <with-param name="output" select="$map-output" tunnel="yes"/>
            </call-template>
         </code:block>
         <code:finally line-hidden="true">
            <code:method-call name="WriteEndMap">
               <sequence select="$map-output/src:reference/code:*"/>
            </code:method-call>
         </code:finally>
      </code:try>
   </template>

   <template match="c:map" mode="xcst:instruction">
      <xcst:instruction>
         <sequence select="$src:object-type"/>
      </xcst:instruction>
   </template>

   <template match="c:map-entry" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'key'"/>
         <with-param name="optional" select="'value'"/>
      </call-template>

      <call-template name="xcst:value-or-sequence-constructor"/>

      <call-template name="xcst:validate-output">
         <with-param name="kind" select="'map', 'obj'"/>
      </call-template>

      <variable name="output-is-map" select="src:output-is-map($output)"/>
      <variable name="map-output" select="src:map-output(., $output)"/>

      <if test="not($output-is-map)">
         <code:variable name="{$map-output/src:reference/code:*/@name}">
            <call-template name="src:line-number"/>
            <code:method-call name="CastMapEntry">
               <sequence select="src:helper-type('MapWriter')"/>
               <code:arguments>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
      </if>

      <code:method-call name="WriteStartMapEntry">
         <call-template name="src:line-number"/>
         <sequence select="$map-output/src:reference/code:*"/>
         <code:arguments>
            <code:expression value="{xcst:expression(@key)}"/>
         </code:arguments>
      </code:method-call>

      <code:try line-hidden="true">
         <code:block>
            <call-template name="src:sequence-constructor">
               <with-param name="value" select="@value"/>
               <with-param name="output" select="$map-output" tunnel="yes"/>
            </call-template>
         </code:block>
         <code:finally line-hidden="true">
            <code:method-call name="WriteEndMapEntry">
               <sequence select="$map-output/src:reference/code:*"/>
            </code:method-call>
         </code:finally>
      </code:try>
   </template>

   <template match="c:array" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs"/>

      <variable name="output-is-map" select="src:output-is-map($output)"/>
      <variable name="map-output" select="src:map-output(., $output)"/>

      <if test="not($output-is-map)">
         <code:variable name="{$map-output/src:reference/code:*/@name}">
            <call-template name="src:line-number"/>
            <code:method-call name="CreateArray">
               <sequence select="src:helper-type('MapWriter')"/>
               <code:arguments>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
      </if>

      <code:method-call name="WriteStartArray">
         <call-template name="src:line-number"/>
         <sequence select="$map-output/src:reference/code:*"/>
      </code:method-call>

      <code:try line-hidden="true">
         <code:block>
            <call-template name="src:sequence-constructor">
               <with-param name="output" select="$map-output" tunnel="yes"/>
            </call-template>
         </code:block>
         <code:finally line-hidden="true">
            <code:method-call name="WriteEndArray">
               <call-template name="src:line-number"/>
               <sequence select="$map-output/src:reference/code:*"/>
            </code:method-call>
         </code:finally>
      </code:try>
   </template>

   <template match="c:array" mode="xcst:instruction">
      <xcst:instruction>
         <sequence select="$src:object-type"/>
      </xcst:instruction>
   </template>

   <function name="src:map-output" as="item()">
      <param name="el" as="element()"/>
      <param name="output" as="item()"/>

      <choose>
         <when test="src:output-is-map($output)">
            <sequence select="$output"/>
         </when>
         <otherwise>
            <src:output kind="map">
               <src:reference>
                  <code:variable-reference name="{concat(src:aux-variable('output'), '_', generate-id($el))}"/>
               </src:reference>
            </src:output>
         </otherwise>
      </choose>
   </function>

   <function name="src:output-is-map" as="xs:boolean">
      <param name="output" as="item()"/>

      <sequence select="$output instance of element() and $output/@kind[. eq 'map']"/>
   </function>


   <!-- ## Repetition -->

   <template match="c:for-each" mode="src:statement">

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name', 'in'"/>
         <with-param name="optional" select="'as'"/>
      </call-template>

      <variable name="name" select="xcst:name(@name)"/>

      <variable name="in" as="element()">
         <code:expression value="{xcst:expression(@in)}"/>
      </variable>

      <code:for-each>
         <call-template name="src:line-number"/>
         <code:variable name="{$name}">
            <if test="@as">
               <code:type-reference name="{xcst:type(@as)}"/>
            </if>
            <choose>
               <when test="c:sort">
                  <call-template name="src:sort">
                     <with-param name="in" select="$in"/>
                  </call-template>
               </when>
               <otherwise>
                  <sequence select="$in"/>
               </otherwise>
            </choose>
         </code:variable>
         <code:block>
            <call-template name="src:sequence-constructor">
               <with-param name="children" select="node()[not(self::c:sort or following-sibling::c:sort)]"/>
            </call-template>
         </code:block>
      </code:for-each>
   </template>

   <template match="c:while" mode="src:statement">

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'test'"/>
      </call-template>

      <code:while>
         <call-template name="src:line-number"/>
         <code:expression value="{xcst:expression(@test)}"/>
         <code:block>
            <call-template name="src:sequence-constructor"/>
         </code:block>
      </code:while>
   </template>


   <!-- ## Conditional Processing -->

   <template match="c:choose" mode="src:statement">

      <call-template name="xcst:validate-attribs"/>

      <call-template name="xcst:validate-children">
         <with-param name="allowed" select="'when', 'otherwise'"/>
      </call-template>

      <if test="not(c:when)">
         <sequence select="error(xs:QName('err:XTSE0010'), 'At least one c:when element is required within c:choose', src:error-object(.))"/>
      </if>

      <code:if-else>
         <apply-templates select="c:when | c:otherwise" mode="#current"/>
      </code:if-else>
   </template>

   <template match="c:choose/c:when" mode="src:statement">

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'test'"/>
      </call-template>

      <call-template name="xcst:no-other-preceding"/>

      <code:if>
         <call-template name="src:line-number"/>
         <code:expression value="{xcst:expression(@test)}"/>
         <code:block>
            <call-template name="src:sequence-constructor"/>
         </code:block>
      </code:if>
   </template>

   <template match="c:choose/c:otherwise" mode="src:statement">

      <call-template name="xcst:validate-attribs"/>

      <call-template name="xcst:no-other-following">
         <with-param name="except" select="()"/>
      </call-template>

      <code:else>
         <call-template name="src:sequence-constructor"/>
      </code:else>
   </template>

   <template match="c:if" mode="src:statement">

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'test'"/>
      </call-template>

      <code:if>
         <call-template name="src:line-number"/>
         <code:expression value="{xcst:expression(@test)}"/>
         <code:block>
            <call-template name="src:sequence-constructor"/>
         </code:block>
      </code:if>
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
         <sequence select="error(xs:QName('err:XTSE0020'), 'Buffering not supported yet. Use rollback-output=''no''.', src:error-object(.))"/>
      </if>

      <code:try>
         <call-template name="src:line-number"/>
         <code:block>
            <call-template name="src:sequence-constructor">
               <with-param name="value" select="@value"/>
               <with-param name="children" select="$children"/>
            </call-template>
         </code:block>
         <apply-templates select="c:catch | c:finally" mode="#current"/>
      </code:try>
   </template>

   <template match="c:try/c:catch" mode="src:statement">

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'exception', 'when', 'value'"/>
      </call-template>

      <call-template name="xcst:value-or-sequence-constructor"/>

      <call-template name="xcst:no-other-following">
         <with-param name="except" select="xs:QName('c:catch'), xs:QName('c:finally')"/>
      </call-template>

      <code:catch>
         <call-template name="src:line-number"/>
         <if test="@exception">
            <code:exception>
               <code:expression value="{xcst:expression(@exception)}"/>
            </code:exception>
         </if>
         <if test="@when">
            <code:when>
               <code:expression value="{xcst:expression(@when)}"/>
            </code:when>
         </if>
         <code:block>
            <call-template name="src:sequence-constructor">
               <with-param name="value" select="@value"/>
            </call-template>
         </code:block>
      </code:catch>
   </template>

   <template match="c:try/c:finally" mode="src:statement">

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'value'"/>
      </call-template>

      <call-template name="xcst:value-or-sequence-constructor"/>

      <call-template name="xcst:no-other-following">
         <with-param name="except" select="()"/>
      </call-template>

      <code:finally>
         <call-template name="src:line-number"/>
         <call-template name="src:sequence-constructor">
            <with-param name="value" select="@value"/>
         </call-template>
      </code:finally>
   </template>

   <template match="c:return" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'value'"/>
      </call-template>

      <call-template name="xcst:value-or-sequence-constructor"/>

      <variable name="disallowed-ancestor" select="
         ancestor::*[self::c:param or self::c:with-param or self::c:variable or self::c:value-of or self::c:serialize][1]"/>

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
            <code:return>
               <call-template name="src:line-number"/>
            </code:return>
         </when>
         <otherwise>
            <code:return>
               <call-template name="src:line-number"/>
               <variable name="text" select="xcst:text(.)"/>
               <if test="xcst:has-value(., $text)">
                  <call-template name="src:value">
                     <with-param name="text" select="$text"/>
                  </call-template>
               </if>
            </code:return>
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

      <element name="code:{local-name()}">
         <call-template name="src:line-number"/>
      </element>
   </template>

   <template match="c:using" mode="src:statement">

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'value'"/>
         <with-param name="optional" select="'name', 'as'"/>
      </call-template>

      <variable name="value" select="xcst:expression(@value)"/>
      <variable name="as" select="@as/xcst:type(.)"/>

      <code:using>
         <call-template name="src:line-number"/>
         <choose>
            <when test="@name">
               <code:variable name="{xcst:name(@name)}">
                  <if test="@as">
                     <code:type-reference name="{$as}"/>
                  </if>
                  <code:expression value="{$value}"/>
               </code:variable>
            </when>
            <when test="@as">
               <code:cast>
                  <code:type-reference name="{$as}"/>
                  <code:expression value="{$value}"/>
               </code:cast>
            </when>
            <otherwise>
               <code:expression value="{$value}"/>
            </otherwise>
         </choose>
         <code:block>
            <call-template name="src:sequence-constructor"/>
         </code:block>
      </code:using>
   </template>


   <!-- ## Variables and Parameters -->

   <template match="c:module/c:param | c:package/c:param | c:override/c:param | c:template/c:param | c:delegate/c:param" mode="src:statement">
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="context" tunnel="yes"/>
      <param name="language" required="yes" tunnel="yes"/>

      <variable name="global" select="parent::c:module
         or parent::c:package
         or parent::c:override"/>

      <variable name="meta" select="
         if ($global) then
            $package-manifest/xcst:*[@declaration-id eq current()/generate-id()]
         else ()"/>

      <variable name="name" select="xcst:name(@name)"/>

      <variable name="name-str" as="element()">
         <code:string literal="true">
            <value-of select="xcst:unescape-identifier($name, $language)"/>
         </code:string>
      </variable>

      <variable name="text" select="xcst:text(.)"/>
      <variable name="has-default-value" select="xcst:has-value(., $text)"/>

      <variable name="type" as="element()">
         <choose>
            <when test="$global">
               <sequence select="$meta/code:type-reference"/>
            </when>
            <when test="@as">
               <code:type-reference name="{xcst:type(@as)}"/>
            </when>
            <otherwise>
               <sequence select="$src:nullable-object-type"/>
            </otherwise>
         </choose>
      </variable>

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

      <variable name="default-value" as="element()?">
         <if test="$has-default-value">
            <call-template name="src:value">
               <with-param name="text" select="$text"/>
            </call-template>
         </if>
      </variable>

      <variable name="expression" as="element()">
         <choose>
            <when test="$template-meta/xcst:typed-params(.) and not($tunnel)">
               <variable name="typed-ref" as="element()">
                  <code:property-reference name="Parameters">
                     <sequence select="$context/src:reference/code:*"/>
                  </code:property-reference>
               </variable>
               <choose>
                  <when test="not($required) and not($has-default-value)">
                     <code:property-reference name="{$name}">
                        <sequence select="$typed-ref"/>
                     </code:property-reference>
                  </when>
                  <otherwise>
                     <code:method-call name="TypedParam">
                        <sequence select="src:template-context(())/code:type-reference"/>
                        <if test="$required or not($has-default-value)">
                           <code:type-arguments>
                              <sequence select="$type, $type"/>
                           </code:type-arguments>
                        </if>
                        <code:arguments>
                           <sequence select="$name-str"/>
                           <code:property-reference name="{src:params-type-init-name(xcst:unescape-identifier($name, $language))}">
                              <sequence select="$typed-ref"/>
                           </code:property-reference>
                           <code:property-reference name="{$name}">
                              <sequence select="$typed-ref"/>
                           </code:property-reference>
                           <if test="$has-default-value">
                              <code:lambda>
                                 <sequence select="$default-value"/>
                              </code:lambda>
                           </if>
                           <if test="$required">
                              <code:argument name="required">
                                 <code:bool value="true"/>
                              </code:argument>
                           </if>
                        </code:arguments>
                     </code:method-call>
                  </otherwise>
               </choose>
            </when>
            <otherwise>
               <code:method-call name="Param">
                  <sequence select="$context/src:reference/code:*"/>
                  <if test="$required
                        or not($has-default-value)
                        or $global">
                     <code:type-arguments>
                        <sequence select="$type"/>
                     </code:type-arguments>
                  </if>
                  <code:arguments>
                     <sequence select="$name-str"/>
                     <if test="$has-default-value">
                        <code:lambda>
                           <sequence select="$default-value"/>
                        </code:lambda>
                     </if>
                     <if test="$required">
                        <code:argument name="required">
                           <code:bool value="true"/>
                        </code:argument>
                     </if>
                     <if test="$tunnel">
                        <code:argument name="tunnel">
                           <code:bool value="true"/>
                        </code:argument>
                     </if>
                  </code:arguments>
               </code:method-call>
            </otherwise>
         </choose>
      </variable>

      <choose>
         <when test="$global">
            <code:assign>
               <call-template name="src:line-number"/>
               <choose>
                  <when test="$meta/xs:boolean(@required)">
                     <code:property-reference name="{$name}">
                        <code:this-reference/>
                     </code:property-reference>
                  </when>
                  <otherwise>
                     <code:field-reference name="{src:backing-field($meta)}">
                        <code:this-reference/>
                     </code:field-reference>
                  </otherwise>
               </choose>
               <sequence select="$expression"/>
            </code:assign>
         </when>
         <otherwise>
            <code:variable name="{$name}">
               <call-template name="src:line-number"/>
               <if test="@as">
                  <sequence select="$type"/>
               </if>
               <sequence select="$expression"/>
            </code:variable>
         </otherwise>
      </choose>
   </template>

   <template match="c:variable" mode="src:statement">

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'value', 'as'"/>
      </call-template>

      <call-template name="xcst:value-or-sequence-constructor"/>

      <variable name="text" select="xcst:text(.)"/>
      <variable name="has-value" select="xcst:has-value(., $text)"/>

      <code:variable name="{xcst:name(@name)}">
         <call-template name="src:line-number"/>
         <call-template name="xcst:variable-type">
            <with-param name="el" select="."/>
            <with-param name="text" select="$text"/>
            <with-param name="ignore-seqctor" select="true()"/>
         </call-template>
         <call-template name="src:value">
            <with-param name="text" select="$text"/>
         </call-template>
      </code:variable>
   </template>

   <template match="c:variable" mode="xcst:instruction">
      <xcst:instruction void="true"/>
   </template>

   <template match="c:module/c:variable | c:package/c:variable | c:override/c:variable" mode="src:statement">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <variable name="text" select="xcst:text(.)"/>

      <if test="xcst:has-value(., $text)">
         <variable name="meta" select="$package-manifest/xcst:*[@declaration-id eq current()/generate-id()]"/>

         <code:assign>
            <call-template name="src:line-number"/>
            <code:field-reference name="{src:backing-field($meta)}">
               <code:this-reference/>
            </code:field-reference>
            <call-template name="src:value">
               <with-param name="text" select="$text"/>
            </call-template>
         </code:assign>
      </if>
   </template>

   <template match="c:set" mode="src:statement">

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'ref'"/>
         <with-param name="optional" select="'as', 'value'"/>
      </call-template>

      <call-template name="xcst:value-or-sequence-constructor"/>

      <code:assign>
         <call-template name="src:line-number"/>
         <code:expression value="{xcst:expression(@ref)}"/>
         <call-template name="src:value"/>
      </code:assign>
   </template>

   <template match="c:set" mode="xcst:instruction">
      <xcst:instruction void="true"/>
   </template>


   <!-- ## Callable Components -->

   <template match="c:call-template" mode="src:statement">

      <variable name="result" as="item()+">
         <call-template name="xcst:validate-call-template"/>
      </variable>

      <variable name="meta" select="$result[1]" as="element(xcst:template)"/>
      <variable name="original" select="$result[2]" as="xs:boolean"/>

      <code:method-call>
         <call-template name="src:line-number"/>
         <sequence select="if ($original) then src:original-member($meta) else src:template-member($meta)"/>
         <code:arguments>
            <call-template name="src:call-template-context">
               <with-param name="meta" select="$meta"/>
            </call-template>
            <call-template name="src:call-template-output">
               <with-param name="meta" select="$meta"/>
            </call-template>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="c:call-template" mode="src:expression">

      <variable name="result" as="item()+">
         <call-template name="xcst:validate-call-template"/>
      </variable>

      <variable name="meta" select="$result[1]" as="element(xcst:template)"/>
      <variable name="original" select="$result[2]" as="xs:boolean"/>

      <call-template name="src:write-template-expr">
         <with-param name="meta" select="$meta"/>
         <with-param name="template-method" select="if ($original) then src:original-member($meta) else src:template-member($meta)"/>
      </call-template>
   </template>

   <template name="src:write-template-expr">
      <param name="meta" as="element()"/>
      <param name="template-method" select="src:template-member($meta)" as="element(code:method-reference)"/>

      <code:method-call name="Flush{'Single'[$meta/@cardinality eq 'One']}">
         <code:method-call name="WriteTemplate">
            <code:method-call name="Create">
               <sequence select="src:helper-type('SequenceWriter')"/>
               <code:arguments>
                  <sequence select="src:item-type-inference-member-ref($meta)"/>
               </code:arguments>
            </code:method-call>
            <code:arguments>
               <sequence select="$template-method"/>
               <call-template name="src:call-template-context">
                  <with-param name="meta" select="$meta"/>
               </call-template>
            </code:arguments>
         </code:method-call>
      </code:method-call>
   </template>

   <template name="xcst:validate-call-template">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'tunnel-params'"/>
      </call-template>

      <call-template name="xcst:validate-children">
         <with-param name="allowed" select="'with-param'"/>
      </call-template>

      <call-template name="xcst:validate-with-param"/>

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
         <if test="not($current/c:with-param[xcst:name-equal(@name, current()/string(@name))])">
            <sequence select="error(xs:QName('err:XTSE0690'), concat('No value supplied for required parameter ''', @name, '''.'), src:error-object($current))"/>
         </if>
      </for-each>

      <for-each select="c:with-param[not((@tunnel/xcst:boolean(.), false())[1])]">
         <variable name="param-name" select="xcst:name(@name)"/>
         <if test="not($meta/xcst:param[string(@name) eq $param-name and not(xs:boolean(@tunnel))])">
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
      <variable name="item-type" select="$meta/xcst:item-type/code:type-reference"/>
      <variable name="qualified-types" select="$meta/(@qualified-types, ../@qualified-types)[1]/xs:boolean(.)"/>

      <xcst:instruction expression="true">
         <choose>
            <when test="$item-type">
               <if test="$qualified-types">
                  <sequence select="
                     if ($meta/@cardinality eq 'ZeroOrMore') then
                        src:item-to-sequence-type($item-type)
                     else $item-type"/>
               </if>
            </when>
            <otherwise>
               <sequence select="$src:default-template-type"/>
            </otherwise>
         </choose>
      </xcst:instruction>
   </template>

   <template name="src:call-template-context">
      <param name="meta" as="element()?"/>
      <param name="context" tunnel="yes"/>
      <param name="language" required="yes" tunnel="yes"/>
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <variable name="typed-params" select="boolean($meta/xcst:typed-params(.))"/>
      <variable name="tunnel-params" select="(
         self::c:call-template
         | self::c:next-template
         | self::c:apply-templates
         | self::c:next-match
         | self::c:invoke-delegate)/@tunnel-params"/>

      <code:chain>
         <code:method-call>
            <attribute name="name">
               <choose>
                  <when test="self::c:apply-templates">ForApplyTemplates</when>
                  <when test="self::c:next-match">ForNextMatch</when>
                  <otherwise>
                     <text>Create</text>
                     <if test="$typed-params">Typed</if>
                  </otherwise>
               </choose>
            </attribute>
            <sequence select="src:template-context(())/code:type-reference"/>
            <code:arguments>
               <choose>
                  <when test="$typed-params">
                     <code:new-object>
                        <sequence select="src:params-type($meta)"/>
                        <code:initializer>
                           <for-each select="c:with-param[not(@tunnel/xcst:boolean(.))]">
                              <code:member-initializer name="{xcst:unescape-identifier(xcst:name(@name), $language)}" verbatim="true">
                                 <call-template name="src:value"/>
                              </code:member-initializer>
                           </for-each>
                        </code:initializer>
                     </code:new-object>
                  </when>
                  <otherwise>
                     <variable name="tmpl-params-count" select="count(c:with-param[not(@tunnel/xcst:boolean(.))])"/>
                     <variable name="params-count" select="
                        if ($tmpl-params-count gt 0) then
                           $tmpl-params-count
                        else
                           count(self::c:invoke-delegate/@with-params)"/>
                     <code:int value="{$params-count}"/>
                  </otherwise>
               </choose>
               <code:int value="{count(c:with-param[@tunnel/xcst:boolean(.)]) + count($tunnel-params)}"/>
               <if test="$context">
                  <sequence select="$context/src:reference/code:*"/>
               </if>
            </code:arguments>
         </code:method-call>
         <for-each select="c:with-param[not($typed-params) or @tunnel/xcst:boolean(.)]">
            <code:method-call name="WithParam">
               <call-template name="src:line-number"/>
               <code:chain-reference/>
               <code:arguments>
                  <code:string literal="true">
                     <value-of select="xcst:unescape-identifier(xcst:name(@name), $language)"/>
                  </code:string>
                  <call-template name="src:value"/>
                  <if test="@tunnel">
                     <code:argument name="tunnel">
                        <code:bool value="{xcst:boolean(@tunnel)}"/>
                     </code:argument>
                  </if>
               </code:arguments>
            </code:method-call>
         </for-each>
         <if test="$tunnel-params">
            <code:method-call name="WithTunnelParams">
               <call-template name="src:line-number"/>
               <code:chain-reference/>
               <code:arguments>
                  <code:expression value="{xcst:expression($tunnel-params)}"/>
               </code:arguments>
            </code:method-call>
         </if>
      </code:chain>
   </template>

   <template name="src:call-template-output">
      <param name="meta" as="element()"/>
      <param name="dynamic" select="false()" as="xs:boolean"/>
      <param name="output" tunnel="yes"/>

      <variable name="output-item-type-is-object" select="
         not(src:output-is-obj($output))
         or $output/@item-type-is-object/xs:boolean(.)"/>

      <choose>
         <when test="$meta/xcst:item-type or not($output-item-type-is-object)">
            <code:method-call name="AdjustWriter{'Dynamically'[$dynamic]}">
               <sequence select="src:helper-type('SequenceWriter')"/>
               <code:arguments>
                  <sequence select="$output/src:reference/code:*"/>
                  <sequence select="src:item-type-inference-member-ref($meta)"/>
               </code:arguments>
            </code:method-call>
         </when>
         <otherwise>
            <sequence select="$output/src:reference/code:*"/>
         </otherwise>
      </choose>
   </template>

   <template match="c:next-template" mode="src:statement">

      <variable name="meta" as="element(xcst:template)">
         <call-template name="xcst:validate-next-template"/>
      </variable>

      <code:method-call name="{$meta/@member-name}">
         <call-template name="src:line-number"/>
         <code:this-reference/>
         <code:arguments>
            <call-template name="src:call-template-context">
               <with-param name="meta" select="$meta"/>
            </call-template>
            <call-template name="src:call-template-output">
               <with-param name="meta" select="$meta"/>
            </call-template>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="c:next-template" mode="src:expression">
      <variable name="meta" as="element(xcst:template)">
         <call-template name="xcst:validate-next-template"/>
      </variable>
      <call-template name="src:write-template-expr">
         <with-param name="meta" select="$meta"/>
      </call-template>
   </template>

   <template name="xcst:validate-next-template">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'tunnel-params'"/>
      </call-template>

      <call-template name="xcst:validate-children">
         <with-param name="allowed" select="'with-param'"/>
      </call-template>

      <call-template name="xcst:validate-with-param"/>

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

      <if test="$meta/@declared-visibility eq 'abstract'">
         <sequence select="error(xs:QName('err:XTSE3075'), 'Cannot call a next template with visibility=''abstract''.', src:error-object(.))"/>
      </if>

      <variable name="current" select="."/>

      <for-each select="$meta/xcst:param[@required/xs:boolean(.) and not(@tunnel/xs:boolean(.))]">
         <if test="not($current/c:with-param[xcst:name-equal(@name, current()/string(@name))])">
            <sequence select="error(xs:QName('err:XTSE0690'), concat('No value supplied for required parameter ''', @name, '''.'), src:error-object($current))"/>
         </if>
      </for-each>

      <for-each select="c:with-param[not((@tunnel/xcst:boolean(.), false())[1])]">
         <variable name="param-name" select="xcst:name(@name)"/>
         <if test="not($meta/xcst:param[string(@name) eq $param-name and not(xs:boolean(@tunnel))])">
            <sequence select="error(xs:QName('err:XTSE0680'), concat('Parameter ''', $param-name, ''' is not declared in the called template.'), src:error-object(.))"/>
         </if>
      </for-each>

      <sequence select="$meta"/>
   </template>

   <template match="c:next-template" mode="xcst:instruction">
      <variable name="meta" as="element(xcst:template)">
         <call-template name="xcst:validate-next-template"/>
      </variable>
      <xcst:instruction expression="true">
         <if test="not($meta/xcst:item-type)">
            <sequence select="$src:default-template-type"/>
         </if>
      </xcst:instruction>
   </template>

   <template match="c:invoke-package" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'package'"/>
         <with-param name="optional" select="'base-output-uri', 'base-uri', 'package-params', 'initial-match-selection', 'initial-mode', 'initial-template', 'template-params', 'tunnel-params'"/>
      </call-template>

      <call-template name="xcst:no-children"/>

      <code:chain>
         <code:method-call name="Using">
            <call-template name="src:line-number"/>
            <code:type-reference name="XcstEvaluator" namespace="Xcst"/>
            <code:arguments>
               <code:cast>
                  <sequence select="$src:object-type"/>
                  <code:expression value="{xcst:expression(@package)}"/>
               </code:cast>
            </code:arguments>
         </code:method-call>
         <if test="@package-params">
            <code:method-call name="WithParams">
               <code:chain-reference/>
               <code:arguments>
                  <code:expression value="{xcst:expression(@package-params)}"/>
               </code:arguments>
            </code:method-call>
         </if>
         <choose>
            <when test="@initial-match-selection">
               <code:method-call name="ApplyTemplates">
                  <code:chain-reference/>
                  <code:arguments>
                     <code:expression value="{xcst:expression(@initial-match-selection)}"/>
                     <if test="@initial-mode">
                        <call-template name="src:QName">
                           <with-param name="qname" select="xcst:EQName(@initial-mode, (), false(), true())"/>
                           <with-param name="avt" select="@initial-mode"/>
                        </call-template>
                     </if>
                  </code:arguments>
               </code:method-call>
            </when>
            <otherwise>
               <code:method-call name="Call{if (not(@initial-template)) then 'Initial' else ()}Template">
                  <code:chain-reference/>
                  <code:arguments>
                     <if test="@initial-template">
                        <call-template name="src:QName">
                           <with-param name="qname" select="xcst:EQName(@initial-template, (), false(), true())"/>
                           <with-param name="avt" select="@initial-template"/>
                        </call-template>
                     </if>
                  </code:arguments>
               </code:method-call>
            </otherwise>
         </choose>
         <if test="@template-params">
            <code:method-call name="WithParams">
               <code:chain-reference/>
               <code:arguments>
                  <code:expression value="{xcst:expression(@template-params)}"/>
               </code:arguments>
            </code:method-call>
         </if>
         <if test="@tunnel-params">
            <code:method-call name="WithTunnelParams">
               <code:chain-reference/>
               <code:arguments>
                  <code:expression value="{xcst:expression(@tunnel-params)}"/>
               </code:arguments>
            </code:method-call>
         </if>
         <code:method-call name="OutputToRaw">
            <code:chain-reference/>
            <code:arguments>
               <sequence select="$output/src:reference/code:*"/>
            </code:arguments>
         </code:method-call>
         <if test="@base-uri">
            <code:method-call name="WithBaseUri">
               <code:chain-reference/>
               <code:arguments>
                  <call-template name="src:uri-resolve">
                     <with-param name="uri" select="xcst:uri(@base-uri, true())"/>
                     <with-param name="avt" select="@base-uri"/>
                  </call-template>
               </code:arguments>
            </code:method-call>
         </if>
         <if test="@base-output-uri">
            <code:method-call name="WithBaseOutputUri">
               <code:chain-reference/>
               <code:arguments>
                  <call-template name="src:uri-resolve">
                     <with-param name="uri" select="xcst:uri(@base-output-uri, true())"/>
                     <with-param name="avt" select="@base-output-uri"/>
                  </call-template>
               </code:arguments>
            </code:method-call>
         </if>
         <code:method-call name="Run">
            <code:chain-reference/>
         </code:method-call>
      </code:chain>
   </template>

   <function name="src:template-member" as="element(code:method-reference)">
      <param name="meta" as="element()"/>

      <code:method-reference name="{$meta/@member-name}">
         <choose>
            <when test="$meta/@accepted/xs:boolean(.)">
               <code:field-reference name="{src:used-package-field-name($meta)}">
                  <code:this-reference/>
               </code:field-reference>
            </when>
            <otherwise>
               <code:this-reference/>
            </otherwise>
         </choose>
      </code:method-reference>
   </function>

   <template name="xcst:validate-with-param">
      <for-each select="c:with-param">
         <call-template name="xcst:validate-attribs">
            <with-param name="required" select="'name'"/>
            <with-param name="optional" select="'value', 'as', 'tunnel'"/>
         </call-template>
         <call-template name="xcst:value-or-sequence-constructor"/>
         <if test="preceding-sibling::c:with-param[xcst:name-equal(@name, current()/@name)]">
            <sequence select="error(xs:QName('err:XTSE0670'), 'Duplicate parameter name.', src:error-object(.))"/>
         </if>
      </for-each>
   </template>


   <!-- ## Delegated Templates -->

   <template match="c:delegate" mode="src:expression">
      <param name="language" required="yes" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'as'"/>
      </call-template>

      <variable name="meta" as="element()">
         <xcst:delegate>
            <if test="@as">
               <xcst:item-type>
                  <code:type-reference name="{xcst:item-type(xcst:type(@as), $language)}"/>
               </xcst:item-type>
            </if>
         </xcst:delegate>
      </variable>

      <variable name="item-type" select="$meta/xcst:item-type/code:type-reference"/>

      <variable name="new-context" select="src:template-context($meta, .)"/>
      <variable name="new-output" select="src:template-output($meta, .)"/>

      <code:new-object>
         <code:type-reference name="XcstDelegate" namespace="Xcst">
            <code:type-arguments>
               <sequence select="($item-type, $src:nullable-object-type)[1]"/>
            </code:type-arguments>
         </code:type-reference>
         <code:arguments>
            <code:lambda void="true">
               <code:parameters>
                  <code:parameter name="{$new-context/src:reference/code:*/@name}"/>
                  <code:parameter name="{$new-output/src:reference/code:*/@name}"/>
               </code:parameters>
               <code:block>
                  <for-each select="c:param">
                     <call-template name="xcst:validate-attribs">
                        <with-param name="required" select="'name'"/>
                        <with-param name="optional" select="'value', 'as', 'required', 'tunnel'"/>
                     </call-template>
                     <call-template name="xcst:value-or-sequence-constructor"/>
                     <call-template name="xcst:no-other-preceding"/>
                     <if test="preceding-sibling::c:param[xcst:name-equal(@name, current()/@name)]">
                        <sequence select="error(xs:QName('err:XTSE0580'), 'The name of the parameter is not unique.', src:error-object(.))"/>
                     </if>
                     <apply-templates select="." mode="src:statement">
                        <with-param name="context" select="$new-context" tunnel="yes"/>
                     </apply-templates>
                  </for-each>
                  <call-template name="src:sequence-constructor">
                     <with-param name="children" select="node()[not(self::c:param or following-sibling::c:param)]"/>
                     <with-param name="context" select="$new-context" tunnel="yes"/>
                     <with-param name="output" select="$new-output" tunnel="yes"/>
                  </call-template>
               </code:block>
            </code:lambda>
         </code:arguments>
      </code:new-object>
   </template>

   <template match="c:delegate" mode="xcst:instruction">
      <xcst:instruction expression="true">
         <code:type-reference name="Delegate" namespace="System"/>
      </xcst:instruction>
   </template>

   <template match="c:invoke-delegate" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'delegate'"/>
         <with-param name="optional" select="'with-params', 'tunnel-params'"/>
      </call-template>

      <call-template name="xcst:validate-children">
         <with-param name="allowed" select="'with-param'"/>
      </call-template>

      <call-template name="xcst:validate-with-param"/>

      <code:method-call name="Invoke">
         <call-template name="src:line-number"/>
         <sequence select="src:helper-type('EvaluateDelegate')"/>
         <code:arguments>
            <code:expression value="{xcst:expression(@delegate)}"/>
            <code:chain>
               <call-template name="src:call-template-context"/>
               <if test="@with-params">
                  <code:method-call name="WithParams">
                     <call-template name="src:line-number"/>
                     <code:chain-reference/>
                     <code:arguments>
                        <code:expression value="{xcst:expression(@with-params)}"/>
                     </code:arguments>
                  </code:method-call>
               </if>
            </code:chain>
            <sequence select="$output/src:reference/code:*"/>
         </code:arguments>
      </code:method-call>
   </template>


   <!-- ## Sorting -->

   <template name="src:sort">
      <param name="in" required="yes"/>

      <for-each select="c:sort">
         <call-template name="xcst:validate-attribs">
            <with-param name="optional" select="'by', 'order'"/>
         </call-template>
         <call-template name="xcst:no-children"/>
         <call-template name="xcst:no-other-preceding"/>
      </for-each>

      <code:chain>
         <for-each select="c:sort">
            <variable name="first" select="position() eq 1"/>
            <code:method-call name="{if ($first) then 'SortBy' else 'CreateOrderedEnumerable'}">
               <call-template name="src:line-number"/>
               <choose>
                  <when test="$first">
                     <sequence select="src:helper-type('Sorting')"/>
                  </when>
                  <otherwise>
                     <code:chain-reference/>
                  </otherwise>
               </choose>
               <code:arguments>
                  <if test="$first">
                     <sequence select="$in"/>
                  </if>
                  <choose>
                     <when test="@by">
                        <code:expression value="{xcst:expression(@by)}"/>
                     </when>
                     <otherwise>
                        <variable name="param" select="src:aux-variable(generate-id())"/>
                        <code:lambda>
                           <code:parameters>
                              <code:parameter name="{$param}"/>
                           </code:parameters>
                           <code:variable-reference name="{$param}"/>
                        </code:lambda>
                     </otherwise>
                  </choose>
                  <if test="not($first)">
                     <code:null/>
                  </if>
                  <choose>
                     <when test="@order">
                        <call-template name="src:sort-order-descending">
                           <with-param name="bool" select="xcst:sort-order-descending(@order, true())"/>
                           <with-param name="avt" select="@order"/>
                        </call-template>
                     </when>
                     <otherwise>
                        <code:bool value="false"/>
                     </otherwise>
                  </choose>
               </code:arguments>
            </code:method-call>
         </for-each>
      </code:chain>
   </template>


   <!-- ## Grouping -->

   <template match="c:for-each-group[not(@group-size and not(c:sort))]" mode="src:statement">

      <call-template name="xcst:validate-for-each-group"/>

      <variable name="in" as="element()">
         <code:method-call name="Group{if (@group-size) then 'Size' else 'By'}">
            <sequence select="src:helper-type('Grouping')"/>
            <code:arguments>
               <code:expression value="{xcst:expression(@in)}"/>
               <choose>
                  <when test="@group-size">
                     <call-template name="src:integer">
                        <with-param name="integer" select="xcst:positive-integer(@group-size, true())"/>
                        <with-param name="avt" select="@group-size"/>
                     </call-template>
                  </when>
                  <when test="@group-by">
                     <code:expression value="{xcst:expression(@group-by)}"/>
                  </when>
                  <otherwise>
                     <variable name="param" select="src:aux-variable(generate-id())"/>
                     <code:lambda>
                        <code:parameters>
                           <code:parameter name="{$param}"/>
                        </code:parameters>
                        <code:variable-reference name="{$param}"/>
                     </code:lambda>
                  </otherwise>
               </choose>
            </code:arguments>
         </code:method-call>
      </variable>

      <code:for-each>
         <call-template name="src:line-number"/>
         <code:variable name="{xcst:name(@name)}">
            <choose>
               <when test="c:sort">
                  <call-template name="src:sort">
                     <with-param name="in" select="$in"/>
                  </call-template>
               </when>
               <otherwise>
                  <sequence select="$in"/>
               </otherwise>
            </choose>
         </code:variable>
         <code:block>
            <call-template name="src:sequence-constructor">
               <with-param name="children" select="node()[not(self::c:sort or following-sibling::c:sort)]"/>
            </call-template>
         </code:block>
      </code:for-each>
   </template>

   <template match="c:for-each-group[@group-size and not(c:sort)]" mode="src:statement">

      <call-template name="xcst:validate-for-each-group"/>

      <variable name="iter" select="concat(src:aux-variable('iter'), '_', generate-id())"/>
      <variable name="helper" select="src:helper-type('Grouping')"/>

      <code:variable name="{$iter}">
         <call-template name="src:line-number"/>
         <code:method-call name="GetEnumerator">
            <sequence select="$helper"/>
            <code:arguments>
               <code:expression value="{xcst:expression(@in)}"/>
            </code:arguments>
         </code:method-call>
      </code:variable>

      <code:try line-hidden="true">
         <variable name="cols" select="concat(src:aux-variable('cols'), '_', generate-id())"/>
         <variable name="buff" select="concat(src:aux-variable('buff'), '_', generate-id())"/>
         <variable name="eof" select="concat(src:aux-variable('eof'), '_', generate-id())"/>
         <code:block>
            <code:variable name="{$cols}">
               <code:type-reference name="Int32" namespace="System"/>
               <call-template name="src:integer">
                  <with-param name="integer" select="xcst:positive-integer(@group-size, true())"/>
                  <with-param name="avt" select="@group-size"/>
               </call-template>
            </code:variable>
            <code:variable name="{$buff}">
               <code:method-call name="CreateMutable">
                  <sequence select="$helper"/>
                  <code:arguments>
                     <code:variable-reference name="{$iter}"/>
                     <code:variable-reference name="{$cols}"/>
                  </code:arguments>
               </code:method-call>
            </code:variable>
            <code:variable name="{$eof}">
               <code:type-reference name="Boolean" namespace="System"/>
               <code:bool value="false"/>
            </code:variable>
            <code:while>
               <code:not>
                  <code:variable-reference name="{$eof}"/>
               </code:not>
               <code:block>
                  <code:assign>
                     <code:variable-reference name="{$eof}"/>
                     <code:not>
                        <code:method-call name="MoveNext">
                           <code:variable-reference name="{$iter}"/>
                        </code:method-call>
                     </code:not>
                  </code:assign>
                  <code:if>
                     <code:not>
                        <code:variable-reference name="{$eof}"/>
                     </code:not>
                     <code:block>
                        <code:method-call name="Add">
                           <code:variable-reference name="{$buff}"/>
                           <code:arguments>
                              <code:property-reference name="Current">
                                 <code:variable-reference name="{$iter}"/>
                              </code:property-reference>
                           </code:arguments>
                        </code:method-call>
                     </code:block>
                  </code:if>
                  <code:if>
                     <code:or-else>
                        <code:equal>
                           <code:property-reference name="Count">
                              <code:variable-reference name="{$buff}"/>
                           </code:property-reference>
                           <code:variable-reference name="{$cols}"/>
                        </code:equal>
                        <code:and-also>
                           <code:variable-reference name="{$eof}"/>
                           <code:greater-than>
                              <code:property-reference name="Count">
                                 <code:variable-reference name="{$buff}"/>
                              </code:property-reference>
                              <code:int value="0"/>
                           </code:greater-than>
                        </code:and-also>
                     </code:or-else>
                     <code:block>
                        <code:variable name="{xcst:name(@name)}">
                           <call-template name="src:line-number"/>
                           <code:method-call name="CreateImmutable">
                              <sequence select="$helper"/>
                              <code:arguments>
                                 <code:variable-reference name="{$buff}"/>
                              </code:arguments>
                           </code:method-call>
                        </code:variable>
                        <code:try>
                           <code:block>
                              <call-template name="src:sequence-constructor">
                                 <with-param name="children" select="node()[not(self::c:sort or following-sibling::c:sort)]"/>
                              </call-template>
                           </code:block>
                           <code:finally line-hidden="true">
                              <code:method-call name="Clear">
                                 <code:variable-reference name="{$buff}"/>
                              </code:method-call>
                           </code:finally>
                        </code:try>
                     </code:block>
                  </code:if>
               </code:block>
            </code:while>
         </code:block>
         <code:finally>
            <code:method-call name="Dispose">
               <sequence select="$helper"/>
               <code:arguments>
                  <code:variable-reference name="{$iter}"/>
               </code:arguments>
            </code:method-call>
         </code:finally>
      </code:try>
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


   <!-- ## Diagnostics -->

   <template match="c:assert" mode="src:statement">

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'test'"/>
         <with-param name="optional" select="'value'"/>
      </call-template>

      <call-template name="xcst:value-or-sequence-constructor"/>

      <variable name="text" select="xcst:text(.)"/>

      <code:method-call name="Assert">
         <code:type-reference name="Debug" namespace="System.Diagnostics"/>
         <code:arguments>
            <code:expression value="{xcst:expression(@test)}"/>
            <if test="xcst:has-value(., $text)">
               <call-template name="src:simple-content">
                  <with-param name="attribute" select="@value"/>
                  <with-param name="text" select="$text"/>
               </call-template>
            </if>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="c:assert" mode="xcst:instruction">
      <xcst:instruction void="true"/>
   </template>

   <template match="c:message" mode="src:statement">

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'terminate', 'value'"/>
      </call-template>

      <call-template name="xcst:value-or-sequence-constructor"/>

      <variable name="never-terminate" select="not(@terminate) or xcst:boolean(@terminate, true()) eq false()"/>
      <variable name="always-terminate" select="boolean(@terminate/xcst:boolean(., true()))"/>
      <variable name="use-if" select="not($never-terminate) and not($always-terminate)"/>

      <code:method-call name="WriteLine">
         <call-template name="src:line-number"/>
         <code:type-reference name="Trace" namespace="System.Diagnostics"/>
         <code:arguments>
            <call-template name="src:simple-content">
               <with-param name="attribute" select="@value"/>
            </call-template>
         </code:arguments>
      </code:method-call>

      <variable name="throw" as="element()">
         <variable name="err-obj" select="src:error-object(.)"/>
         <code:throw>
            <code:method-call name="Terminate">
               <sequence select="src:helper-type('DynamicError')"/>
               <code:arguments>
                  <code:string verbatim="true">
                     <value-of select="concat('Processing terminated by c:', local-name(), ' at line ', $err-obj[2], ' in ', tokenize($err-obj[1], '/')[last()])"/>
                  </code:string>
               </code:arguments>
            </code:method-call>
         </code:throw>
      </variable>

      <choose>
         <when test="$use-if">
            <code:if>
               <call-template name="src:line-number"/>
               <choose>
                  <when test="@terminate">
                     <call-template name="src:boolean">
                        <with-param name="bool" select="xcst:boolean(@terminate, true())"/>
                        <with-param name="avt" select="@terminate"/>
                     </call-template>
                  </when>
                  <otherwise>
                     <code:bool value="false"/>
                  </otherwise>
               </choose>
               <code:block>
                  <sequence select="$throw"/>
               </code:block>
            </code:if>
         </when>
         <when test="$always-terminate">
            <sequence select="$throw"/>
         </when>
      </choose>
   </template>

   <template match="c:message" mode="xcst:instruction">
      <xcst:instruction void="true"/>
   </template>


   <!-- ## Extensibility and Fallback -->

   <template match="c:fallback" mode="src:statement src:expression">
      <call-template name="xcst:validate-attribs"/>
   </template>

   <template name="src:extension-instruction">
      <param name="current-mode" as="xs:QName" required="yes"/>

      <variable name="result" as="element()*">
         <apply-templates select="." mode="src:extension-instruction">
            <with-param name="src:current-mode" select="$current-mode" tunnel="yes"/>
         </apply-templates>
      </variable>

      <choose>
         <when test="exists($result)">
            <sequence select="$result"/>
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

      <choose>
         <when test="@value">
            <code:expression value="{xcst:expression(@value)}">
               <call-template name="src:line-number"/>
            </code:expression>
         </when>
         <otherwise>
            <variable name="new-output" select="src:doc-output(., ())"/>
            <code:using>
               <call-template name="src:line-number"/>
               <code:variable name="{$new-output/src:reference/code:*/@name}">
                  <code:method-call name="Void">
                     <sequence select="src:helper-type('Serialization')"/>
                     <code:arguments>
                        <code:this-reference/>
                     </code:arguments>
                  </code:method-call>
               </code:variable>
               <code:block>
                  <call-template name="src:sequence-constructor">
                     <with-param name="output" select="$new-output" tunnel="yes"/>
                  </call-template>
               </code:block>
            </code:using>
         </otherwise>
      </choose>
   </template>

   <template match="c:void" mode="xcst:instruction">
      <xcst:instruction void="true"/>
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
               <variable name="src" select="resolve-uri(xcst:uri(@src), base-uri())"/>
               <if test="not(unparsed-text-available($src))">
                  <sequence select="error((), 'Cannot retrieve script.', src:error-object(.))"/>
               </if>
               <code:script>
                  <call-template name="src:line-number">
                     <with-param name="line-uri" select="$src" tunnel="yes"/>
                     <with-param name="line-number-offset" select="(src:line-number(.) * -1) + 1" tunnel="yes"/>
                  </call-template>
                  <value-of select="unparsed-text($src)"/>
               </code:script>
            </when>
            <otherwise>
               <code:script>
                  <call-template name="src:line-number"/>
                  <value-of select="$text"/>
               </code:script>
            </otherwise>
         </choose>
      </if>
   </template>

   <template match="c:script" mode="xcst:instruction">
      <xcst:instruction void="true"/>
   </template>


   <!-- ## Final Result Trees and Serialization -->

   <template match="c:result-document" mode="src:statement">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'href', 'output', 'format', $src:output-parameters/*[not(self::version)]/local-name()"/>
      </call-template>

      <if test="not(@href) and not(@output)">
         <sequence select="error(xs:QName('err:XTSE0010'), 'At least one of the attributes ''href'' and ''output'' must be specified.', src:error-object(.))"/>
      </if>

      <variable name="new-output" select="src:doc-output(., ())"/>

      <code:using>
         <call-template name="src:line-number"/>
         <code:variable name="{$new-output/src:reference/code:*/@name}">
            <code:method-call name="ResultDocument">
               <sequence select="src:helper-type('Serialization')"/>
               <code:arguments>
                  <code:this-reference/>
                  <code:new-object>
                     <code:type-reference name="OutputParameters" namespace="Xcst"/>
                     <code:initializer>
                        <for-each select="@* except (@format, @href, @output)">
                           <call-template name="src:output-parameter-initializer"/>
                        </for-each>
                     </code:initializer>
                  </code:new-object>
                  <call-template name="src:format-QName"/>
                  <choose>
                     <when test="@href">
                        <code:method-call name="ResolveOutputUri">
                           <sequence select="$src:context-field/src:reference/code:*"/>
                           <code:arguments>
                              <call-template name="src:uri-string">
                                 <with-param name="uri" select="xcst:uri(@href, true())"/>
                                 <with-param name="avt" select="@href"/>
                              </call-template>
                           </code:arguments>
                        </code:method-call>
                     </when>
                     <otherwise>
                        <code:null/>
                     </otherwise>
                  </choose>
                  <if test="@output">
                     <code:expression value="{xcst:expression(@output)}"/>
                  </if>
               </code:arguments>
            </code:method-call>
         </code:variable>
         <code:block>
            <call-template name="src:sequence-constructor">
               <with-param name="output" select="$new-output" tunnel="yes"/>
            </call-template>
         </code:block>
      </code:using>
   </template>

   <template match="c:result-document" mode="xcst:instruction">
      <xcst:instruction void="true"/>
   </template>

   <template match="c:serialize" mode="src:expression">

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'format', $src:output-parameters/*[not(self::version)]/local-name()"/>
      </call-template>

      <variable name="new-output" select="src:doc-output(., ())"/>

      <code:method-call name="Serialize">
         <sequence select="src:helper-type('Serialization')"/>
         <code:arguments>
            <code:this-reference/>
            <call-template name="src:format-QName"/>
            <code:new-object>
               <code:type-reference name="OutputParameters" namespace="Xcst"/>
               <code:initializer>
                  <for-each select="@* except @format">
                     <call-template name="src:output-parameter-initializer"/>
                  </for-each>
               </code:initializer>
            </code:new-object>
            <code:lambda void="true">
               <code:parameters>
                  <code:parameter name="{$new-output/src:reference/code:*/@name}"/>
               </code:parameters>
               <code:block>
                  <call-template name="src:sequence-constructor">
                     <with-param name="output" select="$new-output" tunnel="yes"/>
                  </call-template>
               </code:block>
            </code:lambda>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="c:serialize" mode="xcst:instruction">
      <xcst:instruction expression="true">
         <code:type-reference name="String" namespace="System"/>
      </xcst:instruction>
   </template>

   <template name="src:format-QName">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <choose>
         <when test="@format">
            <variable name="format" select="xcst:EQName(@format, (), false(), true())"/>
            <if test="exists($format) and not($package-manifest/xcst:output[xcst:EQName(@name) eq $format])">
               <sequence select="error(xs:QName('err:XTDE1460'), concat('No output definition exists named ''', $format, '''.'), src:error-object(.))"/>
            </if>
            <call-template name="src:QName">
               <with-param name="qname" select="$format"/>
               <with-param name="avt" select="@format"/>
            </call-template>
         </when>
         <otherwise>
            <code:null/>
         </otherwise>
      </choose>
   </template>

   <template name="src:output-parameter-initializer">
      <choose>
         <when test="namespace-uri()">
            <code:indexer-initializer>
               <code:arguments>
                  <call-template name="src:QName">
                     <with-param name="qname" select="node-name(.)"/>
                  </call-template>
               </code:arguments>
               <apply-templates select="." mode="src:output-parameter"/>
            </code:indexer-initializer>
         </when>
         <otherwise>
            <variable name="expr" as="element()?">
               <apply-templates select="." mode="src:output-parameter"/>
            </variable>
            <if test="$expr">
               <code:member-initializer name="{src:output-parameter-property(.)}">
                  <sequence select="$expr"/>
               </code:member-initializer>
            </if>
         </otherwise>
      </choose>
   </template>

   <template match="@*" mode="src:output-parameter"/>

   <template match="@*[namespace-uri()]" mode="src:output-parameter">
      <code:string verbatim="true">
         <value-of select="string()"/>
      </code:string>
   </template>

   <template match="@byte-order-mark | @escape-uri-attributes | @include-content-type | @indent | @omit-xml-declaration | @skip-character-check | @undeclare-prefixes" mode="src:output-parameter">
      <call-template name="src:boolean">
         <with-param name="bool" select="xcst:boolean(., not(parent::c:output))"/>
         <with-param name="avt" select="."/>
      </call-template>
   </template>

   <template match="@cdata-section-elements | @suppress-indentation" mode="src:output-parameter">
      <param name="merged-list" as="xs:QName*"/>

      <choose>
         <when test="parent::c:output or not(xcst:is-value-template(.))">
            <variable name="list" select="
               if (parent::c:output) then
                  $merged-list
               else
                  for $s in xcst:list(.)
                  return xcst:EQName(., $s, true())"/>
            <code:new-array>
               <code:type-reference name="QualifiedName" namespace="Xcst"/>
               <code:collection-initializer>
                  <for-each select="$list">
                     <call-template name="src:QName">
                        <with-param name="qname" select="."/>
                     </call-template>
                  </for-each>
               </code:collection-initializer>
            </code:new-array>
         </when>
         <otherwise>
            <code:method-call name="List">
               <sequence select="src:helper-type('DataType')"/>
               <code:arguments>
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="."/>
                  </call-template>
                  <code:method-reference name="QName">
                     <sequence select="src:helper-type('DataType')"/>
                  </code:method-reference>
               </code:arguments>
            </code:method-call>
         </otherwise>
      </choose>
   </template>

   <template match="@doctype-public | @doctype-system | @media-type" mode="src:output-parameter">
      <choose>
         <when test="parent::c:output">
            <code:string verbatim="true">
               <value-of select="string()"/>
            </code:string>
         </when>
         <otherwise>
            <call-template name="src:expand-attribute">
               <with-param name="attr" select="."/>
            </call-template>
         </otherwise>
      </choose>
   </template>

   <template match="@encoding" mode="src:output-parameter">
      <code:method-call name="GetEncoding">
         <code:type-reference name="Encoding" namespace="System.Text"/>
         <code:arguments>
            <choose>
               <when test="parent::c:output">
                  <code:string verbatim="true">
                     <value-of select="string()"/>
                  </code:string>
               </when>
               <otherwise>
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="."/>
                  </call-template>
               </otherwise>
            </choose>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="@html-version" mode="src:output-parameter">
      <choose>
         <when test="parent::c:output">
            <call-template name="src:decimal">
               <with-param name="decimal" select="xcst:decimal(., not(parent::c:output))"/>
               <with-param name="avt" select="."/>
            </call-template>
         </when>
         <otherwise>
            <code:method-call name="Decimal">
               <sequence select="src:helper-type('DataType')"/>
               <code:arguments>
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="."/>
                  </call-template>
               </code:arguments>
            </code:method-call>
         </otherwise>
      </choose>
   </template>

   <template match="@indent-spaces" mode="src:output-parameter">
      <choose>
         <when test="parent::c:output">
            <call-template name="src:integer">
               <with-param name="integer" select="xcst:integer(., not(parent::c:output))"/>
               <with-param name="avt" select="."/>
            </call-template>
         </when>
         <otherwise>
            <call-template name="src:expand-attribute">
               <with-param name="attr" select="."/>
            </call-template>
         </otherwise>
      </choose>
   </template>

   <template match="@item-separator" mode="src:output-parameter">
      <variable name="separator" select="xcst:item-separator(., not(parent::c:output))"/>
      <choose>
         <when test="$separator eq '#absent'">
            <code:null/>
         </when>
         <otherwise>
            <call-template name="src:item-separator">
               <with-param name="separator" select="$separator"/>
               <with-param name="avt" select="."/>
            </call-template>
         </otherwise>
      </choose>
   </template>

   <template match="@method" mode="src:output-parameter">
      <variable name="string" select="xcst:non-string(.)"/>
      <variable name="qname" select="xcst:EQName(., (), false(), not(parent::c:output))"/>
      <if test="exists($qname) and not(namespace-uri-from-QName($qname)) and not(local-name-from-QName($qname) = ('xml', 'html', 'xhtml', 'text'))">
         <sequence select="error(xs:QName('err:XTSE1570'), concat('Invalid value for ''', name(), '''. Must be one of (xml|html|xhtml|text).'), src:error-object(.))"/>
      </if>
      <choose>
         <when test="exists($qname) and not(namespace-uri-from-QName($qname))">
            <code:property-reference>
               <attribute name="name">
                  <variable name="local" select="local-name-from-QName($qname)"/>
                  <choose>
                     <when test="$local eq 'xml'">
                        <sequence select="'Xml'"/>
                     </when>
                     <when test="$local eq 'html'">
                        <sequence select="'Html'"/>
                     </when>
                     <when test="$local eq 'xhtml'">
                        <sequence select="'XHtml'"/>
                     </when>
                     <when test="$local eq 'text'">
                        <sequence select="'Text'"/>
                     </when>
                     <otherwise>
                        <sequence select="error()"/>
                     </otherwise>
                  </choose>
               </attribute>
               <code:type-reference name="Methods">
                  <code:type-reference name="OutputParameters" namespace="Xcst"/>
               </code:type-reference>
            </code:property-reference>
         </when>
         <otherwise>
            <call-template name="src:QName">
               <with-param name="qname" select="$qname"/>
               <with-param name="avt" select="."/>
            </call-template>
         </otherwise>
      </choose>
   </template>

   <template match="@output-version" mode="src:output-parameter">
      <call-template name="src:expand-attribute">
         <with-param name="attr" select="."/>
      </call-template>
   </template>

   <template match="@standalone" mode="src:output-parameter">
      <choose>
         <when test="parent::c:output or not(xcst:is-value-template(.))">
            <code:field-reference>
               <attribute name="name" select="
                  if (xcst:non-string(.) eq 'omit') then 'Omit'
                  else if (xcst:boolean(.)) then 'Yes'
                  else 'No'">
               </attribute>
               <code:type-reference name="XmlStandalone" namespace="Xcst"/>
            </code:field-reference>
         </when>
         <otherwise>
            <code:method-call name="Standalone">
               <sequence select="src:helper-type('DataType')"/>
               <code:arguments>
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="."/>
                  </call-template>
               </code:arguments>
            </code:method-call>
         </otherwise>
      </choose>
   </template>

   <template match="c:output/@version" mode="src:output-parameter">
      <code:string literal="true">
         <value-of select="xcst:non-string(.)"/>
      </code:string>
   </template>

   <function name="src:output-parameter-property" as="xs:string?">
      <param name="attr" as="attribute()"/>

      <sequence select="$src:output-parameters/*[local-name() eq local-name($attr)]/string()"/>
   </function>


   <!-- ## Grammar -->

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

   <template name="xcst:validate-output">
      <param name="output" tunnel="yes"/>
      <param name="kind" as="xs:string*"/>

      <if test="not($output)">
         <sequence select="error(xs:QName('err:XTSE0010'), 'Output required.', src:error-object(.))"/>
      </if>
      <if test="exists($kind) and not($output instance of element() and $output/@kind = $kind)">
         <sequence select="error(xs:QName('err:XTSE0010'), 'Incompatible output.', src:error-object(.))"/>
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

   <template name="xcst:variable-type" as="element(code:type-reference)?">
      <param name="el" as="element()" required="yes"/>
      <param name="text" select="xcst:text($el)" as="xs:string?"/>
      <param name="ignore-seqctor" select="false()"/>

      <!-- This is a template and not a function to allow access to tunnel parameters -->

      <choose>
         <when test="$el/@as">
            <code:type-reference name="{$el/@as}"/>
         </when>
         <when test="$text">
            <code:type-reference name="String" namespace="System"/>
         </when>
         <when test="xcst:has-value($el, $text)">
            <choose>
               <when test="$el/@value"/>
               <when test="not($ignore-seqctor)">
                  <variable name="seqctor-meta" as="element()">
                     <call-template name="xcst:sequence-constructor">
                        <with-param name="text" select="$text"/>
                     </call-template>
                  </variable>
                  <variable name="item-type" select="
                     ($seqctor-meta/xcst:item-type/code:type-reference, $src:nullable-object-type)[1]"/>
                  <sequence select="
                     if ($seqctor-meta/@cardinality eq 'ZeroOrMore') then
                        src:item-to-sequence-type($item-type)
                     else
                        $item-type"/>
               </when>
            </choose>
         </when>
         <otherwise>
            <sequence select="$src:nullable-object-type"/>
         </otherwise>
      </choose>
   </template>

   <template name="xcst:sequence-constructor" as="element(xcst:sequence-constructor)">
      <param name="text" as="xs:string?" required="yes"/>
      <param name="children" select="node()" as="node()*"/>
      <param name="language" required="yes" tunnel="yes"/>

      <!-- This is a template and not a function to allow access to tunnel parameters -->

      <variable name="text-meta" as="element()">
         <xcst:instruction expression="true">
            <code:type-reference name="String" namespace="System"/>
         </xcst:instruction>
      </variable>

      <variable name="default-meta" as="element()">
         <xcst:instruction/>
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
                              <otherwise>
                                 <call-template name="xcst:literal-result-element-instruction"/>
                              </otherwise>
                           </choose>
                        </variable>
                        <sequence select="($i, $default-meta)[1]"/>
                     </otherwise>
                  </choose>
               </for-each>
            </otherwise>
         </choose>
      </variable>

      <xcst:sequence-constructor>
         <variable name="voids" select="$instructions[@void/xs:boolean(.)]"/>
         <variable name="non-void" select="$instructions except $voids"/>
         <variable name="types" select="$non-void/code:type-reference[src:qualified-type(.)]"/>
         <variable name="item-type" as="element()?">
            <variable name="item-types" select="$types/src:sequence-to-item-type(., $language)"/>
            <if test="(count($types) + count($voids)) eq count($instructions)
                  and (every $t in $item-types[position() gt 1] satisfies src:type-reference-equal($t, $item-types[1]))">
               <sequence select="$item-types[1]"/>
            </if>
         </variable>
         <variable name="cardinality" select="
            if ($item-type
               and count($non-void) eq 1
               and src:type-cardinality($types[1], $language) eq 'One') then 'One'
            else if (not($item-type) and count($instructions) eq 1) then ()
            else 'ZeroOrMore'">
         </variable>
         <if test="count($instructions) eq 1 and $instructions[1]/@expression/xs:boolean(.)">
            <attribute name="expression" select="true()"/>
         </if>
         <if test="$cardinality">
            <attribute name="cardinality" select="$cardinality"/>
         </if>
         <if test="$item-type">
            <xcst:item-type>
               <sequence select="$item-type"/>
            </xcst:item-type>
         </if>
      </xcst:sequence-constructor>
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

   <function name="xcst:positive-integer" as="xs:integer?">
      <param name="node" as="node()"/>
      <param name="avt" as="xs:boolean"/>

      <variable name="value" select="xcst:integer($node, $avt)"/>
      <sequence select="
         if (exists($value) and $value le 0) then
            error(xs:QName('err:XTSE0020'), concat('Value of ''', name($node), ''' must be a positive integer.'), src:error-object($node))
         else
            $value
      "/>
   </function>

   <function name="xcst:uri" as="xs:anyURI">
      <param name="node" as="node()"/>

      <sequence select="xcst:uri($node, false())"/>
   </function>

   <function name="xcst:uri" as="xs:anyURI?">
      <param name="node" as="node()"/>
      <param name="avt" as="xs:boolean"/>

      <variable name="string" select="string($node)"/>
      <sequence select="
         if ($avt and xcst:is-value-template($node)) then
            ()
         else if ($string castable as xs:anyURI) then
            xs:anyURI($string)
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

   <function name="xcst:item-separator" as="xs:string?">
      <param name="node" as="node()"/>
      <param name="avt" as="xs:boolean"/>

      <variable name="string" select="string($node)"/>
      <sequence select="
         if ($avt and xcst:is-value-template($node)) then ()
         else $string
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

   <function name="xcst:name-equal" as="xs:boolean">
      <!-- xcst:homonymous might pass empty values -->
      <param name="a" as="item()?"/>
      <param name="b" as="item()?"/>

      <variable name="strings" select="
         for $item in ($a, $b)
         return if ($item instance of node()) then xcst:name($item)
         else $item"/>

      <sequence select="boolean($strings[1] eq $strings[2])"/>
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

      <if test="exists($value) and not($value = ('none', 'normalize-space', 'trim'))">
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
               <variable name="attr" select="."/>
               <variable name="el" select=".."/>
               <for-each select="xcst:list(.)">
                  <variable name="default" select=". eq '#default'"/>
                  <variable name="prefix" select="if ($default) then '' else string(xcst:ncname($attr, false(), .))"/>
                  <variable name="ns" select="namespace-uri-for-prefix($prefix, $el)"/>
                  <if test="empty($ns)">
                     <sequence select="error(xs:QName('err:XTSE1430'), concat(if ($default) then 'Default namespace' else concat('Namespace prefix ''', $prefix, ''''), ' has not been declared.'), src:error-object($el))"/>
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

      <if test="exists($node)">
         <variable name="string" select="($value, xcst:non-string($node))[1]"/>
         <variable name="qname-pattern" select="'([^:\{\}]+:)?[^:\{\}]+'"/>
         <choose>
            <when test="matches($string, concat('^Q\{[^\{\}]*\}', $qname-pattern, '$'))">
               <sequence select="xcst:URIQualifiedName($string)"/>
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

   <function name="xcst:URIQualifiedName" as="xs:QName">
      <param name="string" as="xs:string"/>

      <variable name="ns" select="xcst:trim(substring(substring-before($string, '}'), 3))"/>
      <variable name="lexical" select="substring-after($string, '}')"/>
      <sequence select="QName($ns, $lexical)"/>
   </function>

   <!-- xs:NCName not available in XSLT 2.0, using xs:QName instead -->

   <function name="xcst:ncname" as="xs:QName">
      <param name="node" as="node()"/>

      <sequence select="xcst:ncname($node, false())"/>
   </function>

   <function name="xcst:ncname" as="xs:QName?">
      <param name="node" as="node()"/>
      <param name="avt" as="xs:boolean"/>

      <sequence select="xcst:ncname($node, $avt, ())"/>
   </function>

   <function name="xcst:ncname" as="xs:QName?">
      <param name="node" as="node()"/>
      <param name="avt" as="xs:boolean"/>
      <param name="value" as="xs:string?"/>

      <variable name="string" select="($value, xcst:non-string($node))[1]"/>
      <sequence select="
         if ($avt and xcst:is-value-template($node)) then
            ()
         else if ($string castable as xs:QName and not(contains($string, ':'))) then
            QName('', $string)
         else
            error(xs:QName('err:XTSE0020'), concat('Invalid value for ''', name($node), '''.'), src:error-object($node))
      "/>
   </function>

   <function name="xcst:uri-qualified-name" as="xs:string">
      <param name="qname" as="xs:QName"/>

      <sequence select="concat('Q{', namespace-uri-from-QName($qname), '}', local-name-from-QName($qname))"/>
   </function>

   <function name="xcst:trim" as="xs:string">
      <param name="item" as="item()"/>

      <sequence select="replace($item, '^\s*(.+?)\s*$', '$1')"/>
   </function>

   <function name="xcst:list" as="xs:string*">
      <param name="attr" as="item()"/>

      <sequence select="tokenize($attr, '\s')[.]"/>
   </function>


   <!-- ## Code Helpers -->

   <function name="src:sequence-to-item-type" as="element(code:type-reference)">
      <param name="type" as="element(code:type-reference)"/>
      <param name="language" as="xs:string"/>

      <choose>
         <when test="$type/@array-dimensions">
            <sequence select="$type/code:type-reference"/>
         </when>
         <otherwise>
            <code:type-reference name="{$type/xcst:item-type(@name, $language)}">
               <sequence select="$type/@* except $type/@name"/>
               <sequence select="$type/*"/>
            </code:type-reference>
         </otherwise>
      </choose>
   </function>

   <function name="src:item-to-sequence-type" as="element(code:type-reference)">
      <param name="item-type" as="element(code:type-reference)"/>

      <code:type-reference array-dimensions="1">
         <sequence select="$item-type"/>
      </code:type-reference>
   </function>

   <function name="src:non-nullable-type" as="element(code:type-reference)">
      <param name="type" as="element(code:type-reference)"/>
      <param name="language" as="xs:string"/>

      <choose>
         <when test="$type/@nullable">
            <choose>
               <when test="$type/xs:boolean(@nullable)">
                  <code:type-reference nullable="false">
                     <copy-of select="$type/@* except $type/@nullable"/>
                     <copy-of select="$type/node()"/>
                  </code:type-reference>
               </when>
               <otherwise>
                  <sequence select="$type"/>
               </otherwise>
            </choose>
         </when>
         <when test="$type/@name">
            <variable name="non-nullable" select="xcst:non-nullable-type($type/@name, $language)"/>
            <code:type-reference name="{$non-nullable}">
               <copy-of select="$type/@* except $type/@name"/>
               <copy-of select="$type/node()"/>
            </code:type-reference>
         </when>
         <otherwise>
            <sequence select="$type"/>
         </otherwise>
      </choose>
   </function>

   <function name="src:type-cardinality" as="xs:string">
      <param name="type" as="element(code:type-reference)"/>
      <param name="language" as="xs:string"/>

      <sequence select="
         if ($type/@array-dimensions) then 'ZeroOrMore'
         else xcst:cardinality($type/@name, $language)"/>
   </function>

   <function name="src:type-reference-equal" as="xs:boolean">
      <param name="t1" as="element(code:type-reference)?"/>
      <param name="t2" as="element(code:type-reference)?"/>

      <sequence select="src:type-reference-equal($t1, $t2, false())"/>
   </function>

   <function name="src:type-reference-equal" as="xs:boolean">
      <param name="t1" as="element(code:type-reference)?"/>
      <param name="t2" as="element(code:type-reference)?"/>
      <param name="check-nullability" as="xs:boolean"/>

      <choose>
         <when test="count(($t1, $t2)) eq 2">
            <variable name="array-dims" select="$t1/@array-dimensions, $t2/@array-dimensions"/>
            <variable name="names" select="$t1/@name, $t2/@name"/>
            <variable name="namespaces" select="$t1/@namespace, $t2/@namespace"/>
            <variable name="types" select="$t1/code:type-reference, $t2/code:type-reference"/>
            <variable name="targs1" select="$t1/code:type-arguments/code:*"/>
            <variable name="targs2" select="$t2/code:type-arguments/code:*"/>

            <choose>
               <when test="count($array-dims) eq 2">
                  <sequence select="
                     $array-dims[1]/xs:integer(.) eq $array-dims[2]/xs:integer(.)
                     and src:type-reference-equal($types[1], $types[2], $check-nullability)"/>
               </when>
               <otherwise>
                  <variable name="nullables" select="($t1/@nullable, $t2/@nullable)/xs:boolean(.)"/>
                  <sequence select="
                     empty($array-dims)
                     and $names[1] eq $names[2]
                     and (empty($namespaces) or $namespaces[1] eq $namespaces[2])
                     and (not($check-nullability)
                        or empty($nullables)
                        (: if both types have @nullable, then must be equal :)
                        or (count($nullables) eq 2 and count(distinct-values($nullables)) eq 1)
                        (: if only one type has @nullable, it must be false :)
                        or (count($nullables) eq 1 and not($nullables[1])))
                     and src:type-reference-equal($types[1], $types[2], $check-nullability)
                     and (count($targs1) eq count($targs2)
                        and (every $b in (
                           for $p in 1 to count($targs1)
                           return src:type-reference-equal($targs1[$p], $targs2[$p], $check-nullability))
                           satisfies $b))"/>
               </otherwise>
            </choose>
         </when>
         <otherwise>
            <sequence select="empty(($t1, $t2))"/>
         </otherwise>
      </choose>
   </function>

   <function name="src:qualified-type" as="xs:boolean">
      <param name="type" as="element(code:type-reference)"/>

      <sequence select="
         if ($type/@array-dimensions) then
            $type/code:type-reference/src:qualified-type(.)
         else
            (exists($type/@namespace)
               and (every $t in $type/code:type-arguments/code:type-reference satisfies src:qualified-type($t)))
            or $type/code:type-reference/src:qualified-type(.)"/>
   </function>

   <function name="src:qualified-type-name" as="xs:string">
      <param name="ref" as="element(code:type-reference)"/>

      <value-of>
         <apply-templates select="$ref" mode="cs:source">
            <with-param name="omit-namespace-alias" select="true()"/>
         </apply-templates>
      </value-of>
   </function>


   <!-- ## Expressions -->

   <template name="src:simple-content" as="element()">
      <param name="attribute" as="attribute()?"/>
      <param name="text" select="xcst:text(.)"/>
      <param name="separator" as="attribute()?"/>

      <choose>
         <when test="$text">
            <call-template name="src:expand-text">
               <with-param name="el" select="."/>
               <with-param name="text" select="$text"/>
            </call-template>
         </when>
         <when test="$attribute">
            <code:method-call name="Join">
               <code:property-reference name="SimpleContent">
                  <sequence select="$src:context-field/src:reference/code:*"/>
               </code:property-reference>
               <code:arguments>
                  <choose>
                     <when test="$separator">
                        <call-template name="src:expand-attribute">
                           <with-param name="attr" select="$separator"/>
                        </call-template>
                     </when>
                     <otherwise>
                        <code:string literal="true">
                           <attribute name="xml:space" select="'preserve'"/>
                           <text> </text>
                        </code:string>
                     </otherwise>
                  </choose>
                  <code:expression value="{xcst:expression($attribute)}"/>
               </code:arguments>
            </code:method-call>
         </when>
         <when test="*">
            <variable name="new-output" select="src:doc-output(., ())"/>
            <variable name="default-sep" select="
               if (self::c:value-of or self::c:attribute) then ''
               else ' '"/>
            <code:method-call name="SimpleContent">
               <sequence select="src:helper-type('Serialization')"/>
               <code:arguments>
                  <code:this-reference/>
                  <choose>
                     <when test="$separator">
                        <call-template name="src:expand-attribute">
                           <with-param name="attr" select="$separator"/>
                        </call-template>
                     </when>
                     <otherwise>
                        <code:string literal="true">
                           <attribute name="xml:space" select="'preserve'"/>
                           <value-of select="$default-sep"/>
                        </code:string>
                     </otherwise>
                  </choose>
                  <code:lambda void="true">
                     <code:parameters>
                        <code:parameter name="{$new-output/src:reference/code:*/@name}"/>
                     </code:parameters>
                     <code:block>
                        <call-template name="src:sequence-constructor">
                           <with-param name="output" select="$new-output" tunnel="yes"/>
                        </call-template>
                     </code:block>
                  </code:lambda>
               </code:arguments>
            </code:method-call>
         </when>
         <otherwise>
            <code:string/>
         </otherwise>
      </choose>
   </template>

   <template name="src:value" as="element()">
      <param name="attribute" select="@value" as="attribute()?"/>
      <param name="text" select="xcst:text(.)"/>
      <param name="language" required="yes" tunnel="yes"/>

      <variable name="as" select="(self::c:param, self::c:set, self::c:variable, self::c:with-param)/@as/xcst:type(.)"/>

      <choose>
         <when test="$attribute or $text">
            <variable name="value" as="element()">
               <choose>
                  <when test="$attribute">
                     <code:expression value="{xcst:expression($attribute)}"/>
                  </when>
                  <otherwise>
                     <call-template name="src:expand-text">
                        <with-param name="el" select="."/>
                        <with-param name="text" select="$text"/>
                     </call-template>
                  </otherwise>
               </choose>
            </variable>
            <choose>
               <when test="$as">
                  <code:cast>
                     <code:type-reference name="{$as}"/>
                     <sequence select="$value"/>
                  </code:cast>
               </when>
               <otherwise>
                  <sequence select="$value"/>
               </otherwise>
            </choose>
         </when>
         <when test="*">
            <variable name="children" select="node()"/>
            <variable name="seqctor-meta" as="element()">
               <call-template name="xcst:sequence-constructor">
                  <with-param name="children" select="$children"/>
                  <with-param name="text" select="$text"/>
               </call-template>
            </variable>
            <choose>
               <when test="$seqctor-meta/@expression/xs:boolean(.) and not($as)">
                  <variable name="value" as="element()">
                     <apply-templates select="$children" mode="src:expression"/>
                  </variable>
                  <choose>
                     <when test="$as">
                        <code:cast>
                           <code:type-reference name="{$as}"/>
                           <sequence select="$value"/>
                        </code:cast>
                     </when>
                     <otherwise>
                        <sequence select="$value"/>
                     </otherwise>
                  </choose>
               </when>
               <otherwise>
                  <variable name="item-type-ref" as="element()?">
                     <choose>
                        <when test="$as">
                           <code:type-reference name="{xcst:item-type($as, $language)}"/>
                        </when>
                        <otherwise>
                           <sequence select="$seqctor-meta/xcst:item-type/code:type-reference"/>
                        </otherwise>
                     </choose>
                  </variable>
                  <variable name="new-output" as="element()">
                     <src:output kind="obj">
                        <if test="not($item-type-ref)">
                           <attribute name="item-type-is-object" select="true()"/>
                        </if>
                        <src:reference>
                           <code:variable-reference name="{concat(src:aux-variable('output'), '_', generate-id())}"/>
                        </src:reference>
                     </src:output>
                  </variable>
                  <variable name="flush-single" select="
                     (if ($as) then xcst:cardinality($as, $language) else $seqctor-meta/@cardinality/string()) eq 'One'"/>

                  <code:method-call name="Flush{'Single'[$flush-single]}">
                     <code:method-call name="WriteSequenceConstructor">
                        <code:method-call name="Create">
                           <sequence select="src:helper-type('SequenceWriter')"/>
                           <code:type-arguments>
                              <sequence select="($item-type-ref, $src:nullable-object-type)[1]"/>
                           </code:type-arguments>
                        </code:method-call>
                        <code:arguments>
                           <code:lambda void="true">
                              <code:parameters>
                                 <code:parameter name="{$new-output/src:reference/code:*/@name}"/>
                              </code:parameters>
                              <code:block>
                                 <call-template name="src:sequence-constructor">
                                    <with-param name="children" select="$children"/>
                                    <with-param name="output" select="$new-output" tunnel="yes"/>
                                 </call-template>
                              </code:block>
                           </code:lambda>
                        </code:arguments>
                     </code:method-call>
                  </code:method-call>
               </otherwise>
            </choose>
         </when>
         <otherwise>
            <code:default>
               <choose>
                  <when test="$as">
                     <code:type-reference name="{$as}"/>
                  </when>
                  <otherwise>
                     <sequence select="$src:object-type"/>
                  </otherwise>
               </choose>
            </code:default>
         </otherwise>
      </choose>
   </template>

   <template name="src:sequence-constructor">
      <param name="children" select="node()"/>
      <param name="value" as="node()?"/>
      <param name="text" select="xcst:text(., $children)"/>
      <param name="output" tunnel="yes"/>

      <variable name="complex-content" select="boolean($children[self::*])"/>

      <choose>
         <when test="$complex-content">
            <apply-templates select="$children" mode="src:statement"/>
         </when>
         <when test="$value">
            <code:method-call name="WriteObject">
               <call-template name="src:line-number"/>
               <sequence select="$output/src:reference/code:*"/>
               <code:arguments>
                  <code:expression value="{xcst:expression($value)}"/>
               </code:arguments>
            </code:method-call>
         </when>
         <when test="$text">
            <code:method-call name="WriteString">
               <call-template name="src:line-number"/>
               <sequence select="$output/src:reference/code:*"/>
               <code:arguments>
                  <call-template name="src:expand-text">
                     <with-param name="el" select="."/>
                     <with-param name="text" select="$text"/>
                  </call-template>
               </code:arguments>
            </code:method-call>
         </when>
      </choose>
   </template>

   <template name="src:expand-text">
      <param name="el" as="element()"/>
      <param name="text" as="xs:string"/>

      <variable name="tvt" select="xcst:tvt-enabled($el) and xcst:is-value-template($text)"/>
      <variable name="tt" select="xcst:transform-text($el)"/>

      <choose>
         <when test="$tvt">
            <variable name="format-expr" as="element()">
               <call-template name="src:format-value-template">
                  <with-param name="context-node" select="$el"/>
                  <with-param name="text" select="$text"/>
                  <with-param name="lre" select="true()"/>
               </call-template>
            </variable>
            <choose>
               <when test="$tt">
                  <code:method-call name="{if ($tt eq 'trim') then 'Trim' else 'NormalizeSpace'}">
                     <sequence select="src:helper-type('SimpleContent')"/>
                     <code:arguments>
                        <sequence select="$format-expr"/>
                     </code:arguments>
                  </code:method-call>
               </when>
               <otherwise>
                  <sequence select="$format-expr"/>
               </otherwise>
            </choose>
         </when>
         <otherwise>
            <code:string verbatim="true">
               <value-of select="
                  if ($tt eq 'trim') then xcst:trim($text)
                  else if ($tt eq 'normalize-space') then normalize-space($text)
                  else $text"/>
            </code:string>
         </otherwise>
      </choose>
   </template>

   <template name="src:expand-attribute">
      <param name="attr" as="attribute()"/>
      <param name="lre" select="false()" as="xs:boolean"/>

      <variable name="text" select="string($attr)"/>

      <choose>
         <when test="xcst:is-value-template($text)">
            <call-template name="src:format-value-template">
               <with-param name="context-node" select="$attr"/>
               <with-param name="text" select="$text"/>
               <with-param name="lre" select="$lre"/>
            </call-template>
         </when>
         <otherwise>
            <code:string verbatim="true">
               <attribute name="xml:space" select="'preserve'"/>
               <value-of select="$text"/>
            </code:string>
         </otherwise>
      </choose>
   </template>

   <template name="src:format-value-template">
      <param name="text" as="xs:string"/>
      <param name="context-node" as="node()"/>
      <param name="lre" select="false()" as="xs:boolean"/>
      <param name="language" tunnel="yes"/>

      <code:method-call name="FormatValueTemplate">
         <choose>
            <when test="$lre">
               <code:property-reference name="SimpleContent">
                  <sequence select="$src:context-field/src:reference/code:*"/>
               </code:property-reference>
            </when>
            <otherwise>
               <code:property-reference name="Invariant">
                  <sequence select="src:helper-type('SimpleContent')"/>
               </code:property-reference>
            </otherwise>
         </choose>
         <code:arguments>
            <code:string verbatim="true" interpolated="true" quotes-to-escape="{src:quotes-to-escape($text, $context-node, $language)}">
               <value-of select="$text"/>
            </code:string>
         </code:arguments>
      </code:method-call>
   </template>

   <template name="src:boolean">
      <param name="bool" as="xs:boolean?"/>
      <param name="avt" as="attribute()?"/>

      <choose>
         <when test="$bool instance of xs:boolean">
            <code:bool value="{$bool}"/>
         </when>
         <otherwise>
            <code:method-call name="Boolean">
               <sequence select="src:helper-type('DataType')"/>
               <code:arguments>
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="$avt"/>
                  </call-template>
               </code:arguments>
            </code:method-call>
         </otherwise>
      </choose>
   </template>

   <template name="src:decimal">
      <param name="decimal" as="xs:decimal?"/>
      <param name="avt" as="attribute()?"/>

      <choose>
         <when test="$decimal instance of xs:decimal">
            <code:decimal value="{$decimal}"/>
         </when>
         <otherwise>
            <code:method-call name="Decimal">
               <sequence select="src:helper-type('DataType')"/>
               <code:arguments>
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="$avt"/>
                  </call-template>
               </code:arguments>
            </code:method-call>
         </otherwise>
      </choose>
   </template>

   <template name="src:integer">
      <param name="integer" as="xs:integer?"/>
      <param name="avt" as="attribute()?"/>

      <choose>
         <when test="$integer instance of xs:integer">
            <code:int value="{$integer}"/>
         </when>
         <otherwise>
            <code:method-call name="Integer">
               <sequence select="src:helper-type('DataType')"/>
               <code:arguments>
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="$avt"/>
                  </call-template>
               </code:arguments>
            </code:method-call>
         </otherwise>
      </choose>
   </template>

   <template name="src:QName">
      <param name="qname" as="xs:QName?"/>
      <param name="string" as="xs:string?"/>
      <param name="avt" as="attribute()?"/>

      <code:method-call name="QName">
         <sequence select="src:helper-type('DataType')"/>
         <code:arguments>
            <choose>
               <when test="$qname instance of xs:QName">
                  <code:string verbatim="true">
                     <value-of select="namespace-uri-from-QName($qname)"/>
                  </code:string>
                  <code:string literal="true">
                     <value-of select="local-name-from-QName($qname)"/>
                  </code:string>
               </when>
               <when test="$string">
                  <code:string literal="true">
                     <value-of select="$string"/>
                  </code:string>
               </when>
               <otherwise>
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="$avt"/>
                  </call-template>
               </otherwise>
            </choose>
         </code:arguments>
      </code:method-call>
   </template>

   <template name="src:ncname-string">
      <param name="ncname" as="xs:QName?"/>
      <param name="avt" as="attribute()?"/>

      <choose>
         <when test="$ncname instance of xs:QName">
            <code:string literal="true">
               <value-of select="$ncname"/>
            </code:string>
         </when>
         <otherwise>
            <call-template name="src:expand-attribute">
               <with-param name="attr" select="$avt"/>
            </call-template>
         </otherwise>
      </choose>
   </template>

   <template name="src:uri-string">
      <param name="uri" as="xs:anyURI?"/>
      <param name="avt" as="attribute()?"/>

      <choose>
         <when test="$uri instance of xs:anyURI">
            <code:string verbatim="true">
               <value-of select="$uri"/>
            </code:string>
         </when>
         <otherwise>
            <call-template name="src:expand-attribute">
               <with-param name="attr" select="$avt"/>
            </call-template>
         </otherwise>
      </choose>
   </template>

   <template name="src:uri-resolve">
      <param name="uri" as="xs:anyURI?"/>
      <param name="avt" as="attribute()?"/>

      <choose>
         <when test="$avt/ancestor::*[@xml:base]">
            <variable name="base-uri" select="base-uri($avt)"/>
            <code:method-call name="Uri">
               <sequence select="src:helper-type('DataType')"/>
               <code:arguments>
                  <choose>
                     <when test="exists($uri)">
                        <call-template name="src:uri-string">
                           <with-param name="uri" select="resolve-uri($uri, $base-uri)"/>
                        </call-template>
                     </when>
                     <otherwise>
                        <call-template name="src:uri-string">
                           <with-param name="uri" select="$base-uri"/>
                        </call-template>
                        <call-template name="src:expand-attribute">
                           <with-param name="attr" select="$avt"/>
                        </call-template>
                     </otherwise>
                  </choose>
               </code:arguments>
            </code:method-call>
         </when>
         <otherwise>
            <code:method-call name="ResolveUri">
               <sequence select="$src:context-field/src:reference/code:*"/>
               <code:arguments>
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="$avt"/>
                  </call-template>
               </code:arguments>
            </code:method-call>
         </otherwise>
      </choose>
   </template>

   <template name="src:sort-order-descending">
      <param name="bool" as="xs:boolean?"/>
      <param name="avt" as="attribute()?"/>

      <choose>
         <when test="$bool instance of xs:boolean">
            <code:bool value="{$bool}"/>
         </when>
         <otherwise>
            <code:method-call name="SortOrderDescending">
               <sequence select="src:helper-type('DataType')"/>
               <code:arguments>
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="$avt"/>
                  </call-template>
               </code:arguments>
            </code:method-call>
         </otherwise>
      </choose>
   </template>

   <template name="src:item-separator">
      <param name="separator" as="xs:string?"/>
      <param name="avt" as="attribute()?"/>

      <choose>
         <when test="$separator instance of xs:string">
            <code:string verbatim="true">
               <attribute name="xml:space" select="'preserve'"/>
               <value-of select="$separator"/>
            </code:string>
         </when>
         <otherwise>
            <code:method-call name="ItemSeparator">
               <sequence select="src:helper-type('DataType')"/>
               <code:arguments>
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="$avt"/>
                  </call-template>
               </code:arguments>
            </code:method-call>
         </otherwise>
      </choose>
   </template>


   <!-- ## Helpers -->

   <template name="src:line-number">
      <param name="line-number-offset" select="0" as="xs:integer" tunnel="yes"/>
      <param name="line-uri" as="xs:anyURI?" tunnel="yes"/>

      <if test="$src:use-line-directive">
         <attribute name="line-number" select="src:line-number(.) + $line-number-offset"/>
         <attribute name="line-uri" select="($line-uri, document-uri(root(.)), base-uri())[1]"/>
      </if>
   </template>

   <function name="src:helper-type" as="element(code:type-reference)">
      <param name="helper" as="xs:string"/>

      <code:type-reference name="{$helper}" namespace="Xcst.Runtime"/>
   </function>

   <function name="src:aux-variable" as="xs:string">
      <param name="name" as="xs:string"/>

      <sequence select="concat('__xcst_', $name)"/>
   </function>

   <function name="src:error-object" as="item()+">
      <param name="node" as="node()"/>

      <sequence select="(document-uri(root($node)), xs:anyURI(''))[1], src:line-number($node)"/>
   </function>

   <function name="src:line-number" as="xs:integer" override="no">
      <param name="p1" as="node()"/>

      <sequence select="error()"/>
   </function>

</stylesheet>
