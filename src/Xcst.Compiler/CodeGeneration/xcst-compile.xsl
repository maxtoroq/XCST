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
   xmlns:cs="http://maxtoroq.github.io/XCST/csharp"
   xmlns:vb="http://maxtoroq.github.io/XCST/visual-basic">

   <include href="xcst-metadata.xsl"/>
   <include href="xcst-core.xsl"/>
   <include href="xcst-csharp.xsl"/>
   <include href="xcst-vb.xsl"/>

   <param name="src:namespace" as="xs:string?"/>
   <param name="src:class" as="xs:string?"/>
   <param name="src:base-types" as="element(code:type-reference)*"/>
   <param name="src:visibility" select="'public'" as="xs:string"/>
   <param name="src:cls-compliant" select="false()" as="xs:boolean"/>

   <param name="src:named-package" select="false()" as="xs:boolean"/>
   <param name="src:use-package-base" as="xs:string?"/>
   <param name="src:manifest-only" select="false()" as="xs:boolean"/>

   <param name="src:package-type-resolver" as="item()?"/>
   <param name="src:package-location-resolver" as="item()?"/>
   <param name="src:packages-location" as="xs:string?"/>
   <param name="src:package-file-extension" as="xs:string?"/>
   <param name="src:module-resolver" as="item()?"/>

   <param name="src:new-line" select="'&#xA;'" as="xs:string"/>
   <param name="src:indent" select="'    '" as="xs:string"/>

   <variable name="src:package-interface" select="src:package-model-type('IXcstPackage', true())"/>

   <variable name="src:context-field" as="element()">
      <src:context>
         <sequence select="src:package-model-type('ExecutionContext')"/>
         <src:reference>
            <code:field-reference name="{src:aux-variable('exec_context')}">
               <code:this-reference/>
            </code:field-reference>
         </src:reference>
      </src:context>
   </variable>

   <variable name="xcst:csharp-lang" select="'C#'"/>
   <variable name="xcst:vb-lang" select="'VisualBasic'"/>
   <variable name="xcst:validation-or-type-attributes" select="'validation-resource-type'"/>
   <variable name="xcst:validation-or-member-attributes" select="'data-type-message', 'required-message', 'min-length-message', 'max-length-message', 'pattern-message', 'range-message', 'equal-to-message'"/>
   <variable name="xcst:type-or-member-attributes" select="'allow-empty-string', 'display-text-member'"/>

   <variable name="src:contextual-variable" select="'__xcst'"/>

   <output cdata-section-elements="src:compilation-unit"/>

   <template match="c:module | c:package" mode="src:main">
      <param name="namespace" select="$src:namespace" as="xs:string?"/>
      <param name="class" select="$src:class" as="xs:string?"/>
      <param name="named-package" select="$src:named-package"/>
      <param name="manifest-only" select="$src:manifest-only"/>

      <variable name="package-uri" select="document-uri(root())"/>
      <variable name="package-name" select="self::c:package/@name/xcst:name(.)"/>
      <variable name="package-name-parts" select="tokenize($package-name, '\.')"/>
      <variable name="language" select="@language/xcst:non-string(.)"/>

      <if test="$named-package
            and not($package-name)">
         <sequence select="error((), 'A named package is expected. Use the c:package element with a ''name'' attribute.', src:error-object(.))"/>
      </if>

      <variable name="ns" as="xs:string">
         <choose>
            <when test="$package-name">
               <choose>
                  <when test="count($package-name-parts) eq 1">
                     <if test="not($namespace)">
                        <sequence select="error((), 'The ''namespace'' parameter is required if the package name is not multipart.', src:error-object(.))"/>
                     </if>
                     <sequence select="$namespace"/>
                  </when>
                  <otherwise>
                     <if test="$namespace">
                        <sequence select="error((), 'The ''namespace'' parameter should be omitted if the package name is multipart.', src:error-object(.))"/>
                     </if>
                     <sequence select="string-join($package-name-parts[position() ne last()], '.')"/>
                  </otherwise>
               </choose>
            </when>
            <otherwise>
               <if test="not($namespace)">
                  <sequence select="error((), 'The ''namespace'' parameter is required for implicit and unnamed packages.', src:error-object(.))"/>
               </if>
               <sequence select="$namespace"/>
            </otherwise>
         </choose>
      </variable>

      <variable name="cl" as="xs:string">
         <choose>
            <when test="$package-name">
               <if test="$class">
                  <sequence select="error((), 'The ''class'' parameter should be omitted for named packages.', src:error-object(.))"/>
               </if>
               <sequence select="$package-name-parts[last()]"/>
            </when>
            <otherwise>
               <if test="not($class)">
                  <sequence select="error((), 'The ''class'' parameter is required for implicit and unnamed packages.', src:error-object(.))"/>
               </if>
               <sequence select="$class"/>
            </otherwise>
         </choose>
      </variable>

      <variable name="modules-and-uris" as="item()+">
         <apply-templates select="." mode="src:load-imports">
            <with-param name="language" select="$language" tunnel="yes"/>
            <with-param name="module-docs" select="root()" tunnel="yes"/>
         </apply-templates>
      </variable>

      <variable name="modules" select="$modules-and-uris[. instance of node()]" as="element()+"/>
      <variable name="refs" select="$modules-and-uris[not(. instance of node())], $modules//c:script[@src]/resolve-uri(@src, base-uri())"/>
      <variable name="implicit-package" select="self::c:module"/>

      <variable name="used-packages" as="element()*">
         <for-each-group select="for $m in $modules return $m/c:use-package" group-by="src:resolve-package-name(., $ns)">
            <variable name="used-package-name" select="current-grouping-key()"/>
            <variable name="manifest" as="element()">
               <variable name="man" select="src:package-manifest(
                  $used-package-name,
                  $src:package-type-resolver,
                  src:error-object(.))"/>
               <choose>
                  <when test="$man">
                     <sequence select="$man/xcst:package-manifest"/>
                  </when>
                  <otherwise>
                     <variable name="used-package-uri" select="src:package-location(
                        $used-package-name,
                        $src:package-location-resolver,
                        $package-uri,
                        $src:packages-location,
                        $src:package-file-extension)"/>
                     <if test="not($used-package-uri)">
                        <sequence select="error(xs:QName('err:XTSE3000'), concat('Cannot find package ''', $used-package-name, '''.'), src:error-object(.))"/>
                     </if>
                     <variable name="result">
                        <apply-templates select="doc(string($used-package-uri))/c:package" mode="src:main">
                           <with-param name="namespace" select="()"/>
                           <with-param name="class" select="()"/>
                           <with-param name="named-package" select="true()"/>
                           <with-param name="manifest-only" select="true()"/>
                        </apply-templates>
                     </variable>
                     <if test="not(xcst:language-equal($result/*/@language, $language))">
                        <sequence select="error((), 'Used packages that are not pre-compiled must use the same value for the ''language'' attribute as the top-level package.', src:error-object(.))"/>
                     </if>
                     <sequence select="$result/*/xcst:package-manifest"/>
                  </otherwise>
               </choose>
            </variable>
            <xcst:package-manifest package-id="{generate-id($manifest)}">
               <sequence select="$manifest/@*"/>
               <sequence select="$manifest/code:type-reference"/>
               <sequence select="$manifest/xcst:*[@visibility ne 'hidden']"/>
            </xcst:package-manifest>
         </for-each-group>
      </variable>

      <variable name="local-components" as="element()*">
         <apply-templates select="$modules" mode="xcst:package-manifest">
            <with-param name="modules" select="$modules" tunnel="yes"/>
            <with-param name="used-packages" select="$used-packages" tunnel="yes"/>
            <with-param name="namespace" select="$ns" tunnel="yes"/>
            <with-param name="implicit-package" select="$implicit-package" tunnel="yes"/>
            <with-param name="language" select="$language" tunnel="yes"/>
         </apply-templates>
      </variable>

      <variable name="package-manifest" as="element()">
         <xcst:package-manifest qualified-types="false">
            <code:type-reference name="{$cl}" namespace="{$ns}"/>
            <apply-templates select="for $p in $used-packages return $p/xcst:*" mode="xcst:accepted-component">
               <with-param name="modules" select="$modules" tunnel="yes"/>
               <with-param name="local-components" select="$local-components" tunnel="yes"/>
            </apply-templates>
            <copy-of select="$local-components"/>
            <call-template name="xcst:output-definitions">
               <with-param name="modules" select="$modules" tunnel="yes"/>
            </call-template>
         </xcst:package-manifest>
      </variable>

      <variable name="non-hidden-accepted"
         select="$package-manifest/xcst:*[@accepted/xs:boolean(.) and @visibility ne 'hidden']"/>

      <variable name="accepted-duplicates"
         select="$non-hidden-accepted[some $c in ($non-hidden-accepted except .) satisfies xcst:homonymous(., $c)]"/>

      <variable name="first-duplicate-pair" select="
         if (count($accepted-duplicates) gt 1) then
         ($accepted-duplicates[1], ($accepted-duplicates[position() gt 1])[xcst:homonymous(., $accepted-duplicates[1])])
         else ()"/>

      <if test="$first-duplicate-pair">
         <variable name="message" select="concat(
            'Cannot accept two or more homonymous components with a visibility other than hidden: ',
            string-join(
               for $c in $first-duplicate-pair
               return concat('''', $c/@name, ''' from ', src:qualified-type-name($c/xcst:package-type/code:type-reference)),
               ' and '),
            '.')
         "/>
         <sequence select="error(xs:QName('err:XTSE3050'), $message, src:error-object(.))"/>
      </if>

      <src:program language="{$language}">
         <for-each select="distinct-values($refs)">
            <src:ref href="{.}"/>
         </for-each>
         <sequence select="$package-manifest"/>
         <if test="not($manifest-only)">
            <call-template name="src:compilation-units">
               <with-param name="modules" select="$modules" tunnel="yes"/>
               <with-param name="package-manifest" select="$package-manifest" tunnel="yes"/>
               <with-param name="used-packages" select="$used-packages" tunnel="yes"/>
               <with-param name="language" select="$language" tunnel="yes"/>
            </call-template>
         </if>
      </src:program>
   </template>

   <template match="c:*" mode="src:main">
      <sequence select="error(xs:QName('err:XTSE0010'), concat('Unknown XCST element: ', local-name()), src:error-object(.))"/>
   </template>

   <template match="*[not(self::c:*)]" mode="src:main">
      <call-template name="xcst:check-document-element-attributes"/>
      <sequence select="error((), 'Simplified module not implemented yet.', src:error-object(.))"/>
   </template>

   <template match="c:module | c:package" mode="src:load-imports">
      <param name="language" required="yes" tunnel="yes"/>

      <call-template name="xcst:check-document-element-attributes"/>
      <apply-templates mode="xcst:check-top-level"/>

      <if test="not(xcst:language-equal(@language, $language))">
         <sequence select="error(xs:QName('err:XTSE0020'), 'Imported modules must use the same value for the ''language'' attribute as the principal module.', src:error-object(.))"/>
      </if>

      <apply-templates select="c:import" mode="#current"/>
      <sequence select="."/>
   </template>

   <template match="c:import" mode="src:load-imports">
      <param name="module-docs" as="document-node()+" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'href'"/>
      </call-template>

      <call-template name="xcst:no-children"/>

      <variable name="href" select="resolve-uri(xcst:uri(@href), base-uri())"/>

      <variable name="result" select="src:doc-with-uris($href, src:error-object(.), $src:module-resolver)"/>
      <variable name="imported" select="$result[1]"/>

      <if test="some $m in $module-docs satisfies $m is $imported">
         <sequence select="error(xs:QName('err:XTSE0210'), 'A module cannot directly or indirectly import itself.', src:error-object(.))"/>
      </if>

      <if test="not($imported/c:module)">
         <sequence select="error(xs:QName('err:XTSE0165'), 'Expecting c:module element.', src:error-object(.))"/>
      </if>

      <apply-templates select="$imported/c:module" mode="#current">
         <with-param name="module-docs" select="$module-docs, $imported" tunnel="yes"/>
      </apply-templates>

      <sequence select="$result[position() gt 1]"/>
   </template>

   <function name="src:resolve-package-name" as="xs:string">
      <param name="use-package" as="element(c:use-package)"/>
      <param name="namespace" as="xs:string"/>

      <variable name="name" select="xcst:name($use-package/@name)"/>
      <sequence select="
         if (contains($name, '.')) then $name
         else concat(($src:use-package-base, $namespace)[1], '.', $name)"/>
   </function>

   <template name="xcst:check-document-element-attributes">

      <variable name="required" select="'version', 'language'"/>

      <choose>
         <when test="self::c:*">
            <call-template name="xcst:validate-attribs">
               <with-param name="required" select="$required"/>
               <with-param name="optional" select="'name'[current()/self::c:package]"/>
            </call-template>
         </when>
         <otherwise>
            <call-template name="xcst:validate-attribs">
               <with-param name="optional" select="@*[not(namespace-uri())]/local-name()"/>
            </call-template>
            <variable name="current" select="."/>
            <variable name="attribs" select="@c:*"/>
            <for-each select="$required">
               <if test="not(some $a in $attribs satisfies . eq local-name($a))">
                  <sequence select="error(xs:QName('err:XTSE0010'), concat('Element must have an ''c:', .,''' attribute.'), src:error-object($current))"/>
               </if>
            </for-each>
         </otherwise>
      </choose>

      <variable name="attr-name" select="if (self::c:*) then QName('', 'language') else xs:QName('c:language')"/>
      <variable name="language-attr" select="@*[node-name(.) eq $attr-name]"/>

      <if test="not(xcst:language-equal($language-attr, $xcst:csharp-lang)
            or xcst:language-equal($language-attr, $xcst:vb-lang))">
         <sequence select="error(xs:QName('err:XTSE0020'), concat('Unsupported language. Use either ''', $xcst:csharp-lang, ''' or ''', $xcst:vb-lang, '''.'), src:error-object(.))"/>
      </if>
   </template>

   <function name="xcst:language-equal" as="xs:boolean">
      <param name="a" as="item()"/>
      <param name="b" as="item()"/>

      <variable name="strings" select="
         for $item in ($a, $b)
         return if ($item instance of node()) then xcst:non-string($item)
         else $item"/>

      <sequence select="upper-case($strings[1]) eq upper-case($strings[2])"/>
   </function>

   <template match="c:attribute-set | c:function | c:import | c:output | c:param | c:template | c:type | c:import-namespace | c:use-package | c:variable" mode="xcst:check-top-level"/>

   <template match="c:validation" mode="xcst:check-top-level">
      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="$xcst:validation-or-member-attributes, $xcst:validation-or-type-attributes"/>
      </call-template>
      <call-template name="xcst:no-children"/>
   </template>

   <template match="c:*" mode="xcst:check-top-level">
      <sequence select="error(xs:QName('err:XTSE0010'), concat('Unknown XCST element: ', local-name()), src:error-object(.))"/>
   </template>

   <template match="*[not(self::c:*) and namespace-uri()]" mode="xcst:check-top-level"/>

   <template match="*[not(namespace-uri())]" mode="xcst:check-top-level">
      <sequence select="error(xs:QName('err:XTSE0130'), 'Top level elements must have a non-null namespace URI.', src:error-object(.))"/>
   </template>

   <template match="text()[not(normalize-space())]" mode="xcst:check-top-level"/>

   <template match="text()" mode="xcst:check-top-level">
      <sequence select="error(xs:QName('err:XTSE0120'), 'No character data is allowed between top-level elements.', src:error-object(.))"/>
   </template>

   <function name="src:package-manifest" as="document-node()?">
      <param name="p1" as="xs:string"/>
      <param name="p2" as="item()?"/>
      <param name="p3" as="item()+"/>

      <sequence select="src:_package-manifest($p1, $p2, $p3)"/>
   </function>

   <function name="src:package-location" as="xs:anyURI?">
      <param name="p1" as="xs:string"/>
      <param name="p2" as="item()?"/>
      <param name="p3" as="xs:anyURI?"/>
      <param name="p4" as="xs:string?"/>
      <param name="p5" as="xs:string?"/>

      <sequence select="src:_package-location($p1, $p2, $p3, $p4, $p5)"/>
   </function>

   <function name="src:doc-with-uris" as="item()+">
      <param name="p1" as="xs:anyURI"/>
      <param name="p2" as="item()+"/>
      <param name="p3" as="item()?"/>

      <sequence select="src:_doc-with-uris($p1, $p2, $p3)"/>
   </function>

   <!--
      ## Package Analysis
   -->

   <template match="c:module | c:package" mode="xcst:package-manifest">
      <param name="modules" tunnel="yes"/>

      <variable name="module-pos" select="
         for $pos in (1 to count($modules)) 
         return if ($modules[$pos] is current()) then $pos else ()"/>

      <apply-templates select="c:*" mode="#current">
         <with-param name="module-pos" select="$module-pos" tunnel="yes"/>
      </apply-templates>
   </template>

   <template match="c:*" mode="xcst:package-manifest"/>

   <template match="c:use-package" mode="xcst:package-manifest">
      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
      </call-template>
      <call-template name="xcst:validate-children">
         <with-param name="allowed" select="'override'"/>
      </call-template>
      <if test="preceding-sibling::c:use-package[xcst:name-equal(@name, current()/@name)]">
         <sequence select="error((), 'Duplicate c:use-package declaration.', src:error-object(.))"/>
      </if>
      <apply-templates select="c:override" mode="#current"/>
   </template>

   <template match="c:use-package/c:override" mode="xcst:package-manifest">
      <call-template name="xcst:validate-attribs"/>
      <call-template name="xcst:validate-children">
         <with-param name="allowed" select="'template', 'function', 'attribute-set', 'param', 'variable'"/>
      </call-template>
      <apply-templates select="c:*" mode="#current"/>
   </template>

   <template match="c:param | c:variable" mode="xcst:package-manifest">
      <param name="modules" tunnel="yes"/>
      <param name="module-pos" tunnel="yes"/>
      <param name="language" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'value', 'as', ('required', 'tunnel')[current()/self::c:param], 'visibility'[current()/self::c:variable]"/>
      </call-template>

      <call-template name="xcst:value-or-sequence-constructor"/>

      <variable name="text" select="xcst:text(.)"/>
      <variable name="has-default-value" select="xcst:has-value(., $text)"/>
      <variable name="required" select="self::c:param/(@required/xcst:boolean(.), false())[1]"/>

      <if test="self::c:param">
         <if test="@tunnel/xcst:boolean(.)">
            <sequence select="error(xs:QName('err:XTSE0020'), 'For attribute ''tunnel'' within a global parameter, the only permitted values are: ''no'', ''false'', ''0''.', src:error-object(.))"/>
         </if>
         <if test="$has-default-value and $required">
            <sequence select="error(xs:QName('err:XTSE0010'), 'The ''value'' attribute or child element/text should be omitted when required=''yes''.', src:error-object(.))"/>
         </if>
      </if>

      <variable name="name-expr" select="xcst:name(@name)"/>
      <variable name="name" select="xcst:unescape-identifier($name-expr, $language)"/>
      <variable name="name-was-escaped" select="$name-expr ne $name"/>

      <if test="(preceding-sibling::c:*, (
            if (parent::c:override) then ../preceding-sibling::c:override/c:*
            else preceding-sibling::c:use-package/c:override/c:*
            ))[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE0630'), 'Duplicate global variable declaration.', src:error-object(.))"/>
      </if>

      <if test="not(parent::c:override)
            and $modules[position() ne $module-pos]/c:use-package/c:override/c:*[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE3055'), 'There is an homonymous overriding component.', src:error-object(.))"/>
      </if>

      <variable name="overridden-meta" as="element()?">
         <call-template name="xcst:overridden-component"/>
      </variable>

      <variable name="declared-visibility" select="('public'[current()/self::c:param], @visibility/xcst:visibility(.), 'private')[1]"/>

      <variable name="visibility" select="
         if (parent::c:override) then
            ('hidden'[$modules[position() gt $module-pos]/c:use-package[xcst:name-equal(@name, current()/parent::c:override/parent::c:use-package/@name)]/c:override/c:*[xcst:homonymous(., current())]]
            , $declared-visibility)[1]
         else if ($modules[position() gt $module-pos]/c:*[xcst:homonymous(., current())]) then 'hidden'
         else $declared-visibility"/>

      <if test="$has-default-value and $declared-visibility eq 'abstract'">
         <sequence select="error(xs:QName('err:XTSE0010'), 'The ''value'' attribute or child element/text should be omitted when visibility=''abstract''.', src:error-object(.))"/>
      </if>

      <element name="xcst:{local-name()}">
         <attribute name="name" select="$name"/>
         <attribute name="has-default-value" select="$has-default-value"/>
         <if test="self::c:param">
            <attribute name="required" select="$required"/>
         </if>
         <attribute name="visibility" select="$visibility"/>
         <attribute name="member-name" select="$name"/>
         <attribute name="member-name-was-escaped" select="$name-was-escaped"/>
         <if test="$overridden-meta">
            <attribute name="overrides" select="generate-id($overridden-meta)"/>
         </if>
         <attribute name="declaration-id" select="generate-id()"/>
         <attribute name="declaring-module-uri" select="document-uri(root())"/>
         <variable name="type" as="element()?">
            <call-template name="xcst:variable-type">
               <with-param name="el" select="."/>
               <with-param name="text" select="$text"/>
            </call-template>
         </variable>
         <sequence select="($type, $src:object-type)[1]"/>
      </element>
   </template>

   <template match="c:template" mode="xcst:package-manifest">
      <param name="modules" tunnel="yes"/>
      <param name="module-pos" tunnel="yes"/>
      <param name="implicit-package" tunnel="yes"/>
      <param name="language" required="yes" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'as', 'visibility'"/>
      </call-template>

      <variable name="qname" select="xcst:EQName(@name)"/>

      <if test="not($qname eq xs:QName('c:initial-template'))
            and xcst:is-reserved-namespace(namespace-uri-from-QName($qname))">
         <sequence select="error(xs:QName('err:XTSE0080'), concat('Namespace prefix ''', prefix-from-QName($qname), ''' refers to a reserved namespace.'), src:error-object(.))"/>
      </if>

      <if test="(preceding-sibling::c:*, (
            if (parent::c:override) then ../preceding-sibling::c:override/c:*
            else preceding-sibling::c:use-package/c:override/c:*
            ))[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE0660'), 'Duplicate c:template declaration.', src:error-object(.))"/>
      </if>

      <if test="not(parent::c:override)
            and $modules[position() ne $module-pos]/c:use-package/c:override/c:*[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE3055'), 'There is an homonymous overriding component.', src:error-object(.))"/>
      </if>

      <variable name="overridden-meta" as="element()?">
         <call-template name="xcst:overridden-component"/>
      </variable>

      <if test="$overridden-meta">
         <variable name="template" select="."/>
         <for-each select="$overridden-meta/xcst:param">
            <variable name="param" select="$template/c:param[xcst:name-equal(@name, current()/string(@name))]"/>
            <if test="not($param)
                  or xs:boolean(@required) ne ($param/@required/xcst:boolean(.), false())[1]
                  or xs:boolean(@tunnel) ne ($param/@tunnel/xcst:boolean(.), false())[1]">
               <sequence select="error(
                  xs:QName('err:XTSE3070'),
                  'For every parameter on the overridden template, there must be a parameter on the overriding template that has the same name and the same effective values for the tunnel and required attributes.',
                  src:error-object(($param, $template)[1]))"/>
            </if>
         </for-each>
      </if>

      <variable name="declared-visibility" select="
         (@visibility/xcst:visibility(.), 'public'[not(current()/parent::c:override) and $implicit-package], 'private')[1]"/>

      <variable name="visibility" select="
         if (parent::c:override) then
            ('hidden'[$modules[position() gt $module-pos]/c:use-package[xcst:name-equal(@name, current()/parent::c:override/parent::c:use-package/@name)]/c:override/c:*[xcst:homonymous(., current())]]
            , $declared-visibility)[1]
         else if ($modules[position() gt $module-pos]/c:*[xcst:homonymous(., current())]) then 'hidden'
         else $declared-visibility"/>

      <call-template name="xcst:validate-empty-abstract">
         <with-param name="visibility" select="$declared-visibility"/>
      </call-template>

      <variable name="public" select="$visibility = ('public', 'final', 'abstract')"/>
      <variable name="as" select="@as/xcst:type(.)"/>

      <xcst:template name="{xcst:uri-qualified-name($qname)}"
            visibility="{$visibility}"
            declared-visibility="{$declared-visibility}"
            member-name="{src:template-method-name(., $qname, 'tmpl', $public)}"
            declaration-id="{generate-id()}"
            declaring-module-uri="{document-uri(root())}"
            cardinality="{if ($as) then xcst:cardinality($as, $language) else 'ZeroOrMore'}">
         <if test="$overridden-meta">
            <attribute name="overrides" select="generate-id($overridden-meta)"/>
         </if>
         <for-each select="c:param">
            <call-template name="xcst:validate-attribs">
               <with-param name="required" select="'name'"/>
               <with-param name="optional" select="'value', 'as', 'required', 'tunnel'"/>
            </call-template>
            <call-template name="xcst:value-or-sequence-constructor"/>
            <call-template name="xcst:no-other-preceding"/>
            <variable name="required" select="(@required/xcst:boolean(.), false())[1]"/>
            <variable name="tunnel" select="(@tunnel/xcst:boolean(.), false())[1]"/>
            <variable name="text" select="xcst:text(.)"/>
            <variable name="has-default-value" select="xcst:has-value(., $text)"/>
            <if test="$has-default-value and $required">
               <sequence select="error(xs:QName('err:XTSE0010'), 'The ''value'' attribute or child element/text should be omitted when required=''yes''.', src:error-object(.))"/>
            </if>
            <variable name="param-name" select="xcst:unescape-identifier(xcst:name(@name), $language)"/>
            <if test="preceding-sibling::c:param[xcst:name-equal(@name, $param-name)]">
               <sequence select="error(xs:QName('err:XTSE0580'), 'The name of the parameter is not unique.', src:error-object(.))"/>
            </if>
            <if test="$overridden-meta
                  and not($overridden-meta/xcst:param[xcst:name-equal(string(@name), $param-name)])
                  and not($tunnel)
                  and (not(@required) or $required)">
               <sequence select="error(xs:QName('err:XTSE3070'), 'Any parameter on the overriding template for which there is no corresponding parameter on the overridden template must specify required=''no''.', src:error-object(.))"/>
            </if>
            <xcst:param name="{$param-name}" required="{$required}" tunnel="{$tunnel}">
               <variable name="type" as="element()?">
                  <call-template name="xcst:variable-type">
                     <with-param name="el" select="."/>
                     <with-param name="text" select="$text"/>
                  </call-template>
               </variable>
               <sequence select="($type, $src:object-type)[1]"/>
            </xcst:param>
         </for-each>
         <if test="$as">
            <xcst:item-type>
               <code:type-reference name="{xcst:item-type($as, $language)}"/>
            </xcst:item-type>
         </if>
      </xcst:template>
   </template>

   <template match="c:function" mode="xcst:package-manifest">
      <param name="modules" tunnel="yes"/>
      <param name="module-pos" tunnel="yes"/>
      <param name="language" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'as', 'visibility'"/>
      </call-template>

      <variable name="name-expr" select="xcst:name(@name)"/>
      <variable name="name" select="xcst:unescape-identifier($name-expr, $language)"/>
      <variable name="name-was-escaped" select="$name-expr ne $name"/>

      <if test="(preceding-sibling::c:*, (
            if (parent::c:override) then ../preceding-sibling::c:override/c:*
            else preceding-sibling::c:use-package/c:override/c:*
            ))[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE0770'), 'Duplicate c:function declaration.', src:error-object(.))"/>
      </if>

      <if test="not(parent::c:override)
            and $modules[position() ne $module-pos]/c:use-package/c:override/c:*[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE3055'), 'There is an homonymous overriding component.', src:error-object(.))"/>
      </if>

      <variable name="overridden-meta" as="element()?">
         <call-template name="xcst:overridden-component"/>
      </variable>

      <variable name="declared-visibility" select="(@visibility/xcst:visibility(.), 'private')[1]"/>

      <variable name="visibility" select="
         if (parent::c:override) then
            ('hidden'[$modules[position() gt $module-pos]/c:use-package[xcst:name-equal(@name, current()/parent::c:override/parent::c:use-package/@name)]/c:override/c:*[xcst:homonymous(., current())]]
            , $declared-visibility)[1]
         else if ($modules[position() gt $module-pos]/c:*[xcst:homonymous(., current())]) then 'hidden'
         else $declared-visibility"/>

      <call-template name="xcst:validate-empty-abstract">
         <with-param name="visibility" select="$declared-visibility"/>
      </call-template>

      <variable name="member-name" select="
         if ($visibility eq 'hidden') then 
            src:aux-variable(concat('fn_', $name, '_', generate-id()))
         else $name"/>

      <xcst:function name="{$name}"
            visibility="{$visibility}"
            declared-visibility="{$declared-visibility}"
            member-name="{$member-name}"
            declaration-id="{generate-id()}"
            declaring-module-uri="{document-uri(root())}">
         <if test="$name eq $member-name">
            <attribute name="member-name-was-escaped" select="$name-was-escaped"/>
         </if>
         <if test="$overridden-meta">
            <attribute name="overrides" select="generate-id($overridden-meta)"/>
         </if>
         <if test="@as">
            <code:type-reference name="{xcst:type(@as)}"/>
         </if>
         <for-each select="c:param">
            <call-template name="xcst:validate-attribs">
               <with-param name="required" select="'name'"/>
               <with-param name="optional" select="'value', 'as', 'required', 'tunnel'"/>
            </call-template>
            <call-template name="xcst:value-or-sequence-constructor"/>
            <call-template name="xcst:no-other-preceding"/>
            <variable name="param-name" select="xcst:unescape-identifier(xcst:name(@name), $language)"/>
            <if test="preceding-sibling::c:param[xcst:name-equal(@name, $param-name)]">
               <sequence select="error(xs:QName('err:XTSE0580'), 'The name of the parameter is not unique.', src:error-object(.))"/>
            </if>
            <if test="@tunnel/xcst:boolean(.)">
               <sequence select="error(xs:QName('err:XTSE0020'), 'For attribute ''tunnel'' within a c:function parameter, the only permitted values are: ''no'', ''false'', ''0''.', src:error-object(.))"/>
            </if>
            <variable name="text" select="xcst:text(.)"/>
            <variable name="has-value" select="xcst:has-value(., $text)"/>
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
            <xcst:param name="{$param-name}">
               <variable name="type" as="element()?">
                  <call-template name="xcst:variable-type">
                     <with-param name="el" select="."/>
                     <with-param name="text" select="$text"/>
                  </call-template>
               </variable>
               <sequence select="($type, $src:object-type)[1]"/>
               <if test="$has-value">
                  <call-template name="src:value">
                     <with-param name="text" select="$text"/>
                  </call-template>
               </if>
            </xcst:param>
         </for-each>
      </xcst:function>
   </template>

   <template match="c:attribute-set" mode="xcst:package-manifest">
      <param name="modules" tunnel="yes"/>
      <param name="module-pos" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'use-attribute-sets', 'visibility'"/>
      </call-template>

      <call-template name="xcst:validate-children">
         <with-param name="allowed" select="'attribute'"/>
      </call-template>

      <variable name="qname" select="xcst:EQName(@name)"/>

      <if test="xcst:is-reserved-namespace(namespace-uri-from-QName($qname))">
         <sequence select="error(xs:QName('err:XTSE0080'), concat('Namespace prefix ', prefix-from-QName($qname),' refers to a reserved namespace.'), src:error-object(.))"/>
      </if>

      <if test="(preceding-sibling::c:*, (
            if (parent::c:override) then ../preceding-sibling::c:override/c:*
            else preceding-sibling::c:use-package/c:override/c:*
            ))[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE0660'), 'Duplicate c:attribute-set declaration.', src:error-object(.))"/>
      </if>

      <if test="not(parent::c:override)
            and $modules[position() ne $module-pos]/c:use-package/c:override/c:*[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE3055'), 'There is an homonymous overriding component.', src:error-object(.))"/>
      </if>

      <variable name="overridden-meta" as="element()?">
         <call-template name="xcst:overridden-component"/>
      </variable>

      <variable name="declared-visibility" select="(@visibility/xcst:visibility(.), 'private')[1]"/>

      <variable name="visibility" select="
         if (parent::c:override) then
            ('hidden'[$modules[position() gt $module-pos]/c:use-package[xcst:name-equal(@name, current()/parent::c:override/parent::c:use-package/@name)]/c:override/c:*[xcst:homonymous(., current())]]
            , $declared-visibility)[1]
         else if ($modules[position() gt $module-pos]/c:*[xcst:homonymous(., current())]) then 'hidden'
         else $declared-visibility"/>

      <call-template name="xcst:validate-empty-abstract">
         <with-param name="visibility" select="$declared-visibility"/>
      </call-template>

      <variable name="public" select="$visibility = ('public', 'final', 'abstract')"/>

      <xcst:attribute-set name="{xcst:uri-qualified-name($qname)}"
            visibility="{$visibility}"
            declared-visibility="{$declared-visibility}"
            member-name="{src:template-method-name(., $qname, 'attribs', $public)}"
            declaration-id="{generate-id()}"
            declaring-module-uri="{document-uri(root())}">
         <if test="$overridden-meta">
            <attribute name="overrides" select="$overridden-meta/generate-id()"/>
         </if>
      </xcst:attribute-set>
   </template>

   <template match="c:type" mode="xcst:package-manifest">
      <param name="modules" tunnel="yes"/>
      <param name="module-pos" tunnel="yes"/>
      <param name="language" required="yes" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'visibility', 'resource-type', $xcst:type-or-member-attributes, $xcst:validation-or-type-attributes"/>
      </call-template>

      <call-template name="xcst:validate-children">
         <with-param name="allowed" select="'metadata', 'member'"/>
      </call-template>

      <variable name="name-expr" select="xcst:name(@name)"/>
      <variable name="name" select="xcst:unescape-identifier($name-expr, $language)"/>
      <variable name="name-was-escaped" select="$name-expr ne $name"/>

      <if test="preceding-sibling::c:type[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE0220'), 'Duplicate c:type declaration.', src:error-object(.))"/>
      </if>

      <if test="parent::c:override">
         <sequence select="error((), 'Cannot override a c:type component.', src:error-object(.))"/>
      </if>

      <variable name="declared-visibility" select="(@visibility/xcst:visibility(.), 'private')[1]"/>

      <variable name="visibility" select="
         if ($modules[position() gt $module-pos]/c:*[xcst:homonymous(., current())]) then 'hidden'
         else $declared-visibility"/>

      <if test="$declared-visibility eq 'abstract'">
         <sequence select="error((), 'visibility=''abstract'' is not a valid value for c:type declarations.', src:error-object(.))"/>
      </if>

      <xcst:type name="{$name}"
         visibility="{$visibility}"
         member-name="{$name}"
         member-name-was-escaped="{$name-was-escaped}"
         declaration-id="{generate-id()}"
         declaring-module-uri="{document-uri(root())}"/>
   </template>

   <template name="xcst:overridden-component" as="element()*">
      <param name="used-packages" tunnel="yes"/>
      <param name="namespace" tunnel="yes"/>

      <if test="parent::c:override">
         <variable name="pkg" select="
            $used-packages[src:qualified-type-name(code:type-reference)
               eq src:resolve-package-name(current()/../.., $namespace)]" as="element()"/>
         <variable name="meta" select="$pkg/xcst:*[xcst:homonymous(., current())]"/>
         <if test="not($meta)">
            <sequence select="error(xs:QName('err:XTSE3058'), 'Couldn''t find a matching component in the used package.', src:error-object(.))"/>
         </if>
         <if test="$meta[@visibility eq 'final']">
            <sequence select="error(xs:QName('err:XTSE3060'), 'Cannot override a component with final visibility.', src:error-object(.))"/>
         </if>
         <sequence select="$meta"/>
      </if>
   </template>

   <template match="@*|node()" mode="xcst:accepted-component">
      <copy>
         <apply-templates select="@*|node()" mode="#current"/>
      </copy>
   </template>

   <template match="xcst:package-manifest/xcst:*[@visibility eq 'private']" mode="xcst:accepted-component"/>

   <template match="xcst:package-manifest/xcst:*[@visibility ne 'private']" mode="xcst:accepted-component">
      <param name="modules" tunnel="yes"/>
      <param name="local-components" tunnel="yes"/>

      <variable name="visibility" select="
         if ($local-components[@overrides eq current()/generate-id()]) then 'hidden'
         else if (self::xcst:param) then 'public'
         else 'private'"/>

      <variable name="local-duplicate" select="
         if ($visibility ne 'hidden') then ($local-components[xcst:homonymous(., current())])[1]
         else ()"/>

      <variable name="package-type" select="../code:type-reference"/>

      <for-each select="$modules/(., c:use-package/c:override)/c:*[generate-id() eq $local-duplicate/@declaration-id]">
         <sequence select="error((), concat('Component is in conflict with an accepted component from ''', src:qualified-type-name($package-type), '''.'), src:error-object(.))"/>
      </for-each>

      <copy>
         <attribute name="id" select="generate-id()"/>
         <apply-templates select="@* except @visibility" mode="#current"/>
         <attribute name="visibility" select="$visibility"/>
         <attribute name="original-visibility" select="@visibility"/>
         <attribute name="accepted" select="true()"/>
         <attribute name="package-id" select="../@package-id"/>
         <attribute name="qualified-types" select="../@qualified-types"/>
         <apply-templates mode="#current"/>
         <xcst:package-type>
            <sequence select="$package-type"/>
         </xcst:package-type>
      </copy>
   </template>

   <template name="xcst:output-definitions">
      <param name="modules" tunnel="yes"/>

      <for-each-group select="for $m in reverse($modules) return $m/c:output" group-by="(@name/xcst:EQName(.), '')[1]">
         <sort select="current-grouping-key() instance of xs:QName"/>

         <variable name="output-name" select="
            if (current-grouping-key() instance of xs:QName) then current-grouping-key()
            else ()"/>

         <for-each select="current-group()">
            <call-template name="xcst:validate-attribs">
               <with-param name="optional" select="'name', $src:output-parameters/*[not(self::version) and not(self::output-version)]/local-name()"/>
            </call-template>
            <call-template name="xcst:no-children"/>
            <if test="preceding-sibling::c:output[(empty($output-name) and empty(@name)) or (xcst:EQName(@name) eq $output-name)]">
               <sequence select="error((), 'Duplicate c:output declaration.', src:error-object(.))"/>
            </if>
         </for-each>

         <if test="exists($output-name) and xcst:is-reserved-namespace(namespace-uri-from-QName($output-name))">
            <sequence select="error(xs:QName('err:XTSE0080'), concat('Namespace prefix ''', prefix-from-QName($output-name), ''' refers to a reserved namespace.'), src:error-object(.))"/>
         </if>

         <xcst:output member-name="{src:template-method-name(., $output-name, 'outputdef', false())}" declaration-ids="{current-group()/generate-id(.)}">
            <if test="exists($output-name)">
               <attribute name="name" select="xcst:uri-qualified-name($output-name)"/>
            </if>
         </xcst:output>
      </for-each-group>
   </template>

   <function name="xcst:visibility" as="xs:string">
      <param name="node" as="node()"/>

      <variable name="string" select="xcst:non-string($node)"/>

      <if test="not($string = ('public', 'private', 'final', 'abstract'))">
         <sequence select="error(xs:QName('err:XTSE0020'), concat('Invalid value for ''', name($node), '''. Must be one of (public|private|final|abstract).'), src:error-object($node))"/>
      </if>

      <sequence select="$string"/>
   </function>

   <template name="xcst:validate-empty-abstract">
      <param name="visibility" as="xs:string" required="yes"/>

      <if test="$visibility eq 'abstract'">
         <variable name="children" select="node()[not(self::c:param or following-sibling::c:param)]"/>
         <variable name="text" select="xcst:text(., $children)"/>
         <if test="$text or $children[self::*]">
            <sequence select="error(xs:QName('err:XTSE0010'), 'No content is allowed when visibility=''abstract''.', src:error-object(.))"/>
         </if>
      </if>
   </template>

   <function name="xcst:homonymous" as="xs:boolean">
      <param name="a" as="element()"/>
      <param name="b" as="element()"/>

      <choose>
         <when test="local-name($a) = ('param', 'variable')">
            <sequence select="local-name($b) = ('param', 'variable')
               and xcst:name-equal($a/@name, $b/@name)"/>
         </when>
         <when test="local-name($a) eq 'template'">
            <sequence select="local-name($b) eq 'template'
               and $a/xcst:EQName(@name) eq $b/xcst:EQName(@name)"/>
         </when>
         <when test="local-name($a) eq 'function'">
            <sequence select="local-name($b) eq 'function'
               and xcst:name-equal($a/@name, $b/@name)
               and $a/count(*:param) eq $b/count(*:param)"/>
         </when>
         <when test="local-name($a) eq 'attribute-set'">
            <sequence select="local-name($b) eq 'attribute-set'
               and $a/xcst:EQName(@name) eq $b/xcst:EQName(@name)"/>
         </when>
         <when test="local-name($a) eq 'type'">
            <sequence select="local-name($b) eq 'type'
               and xcst:name-equal($a/@name, $b/@name)"/>
         </when>
         <otherwise>
            <sequence select="false()"/>
         </otherwise>
      </choose>
   </function>

   <function name="src:template-method-name" as="xs:string">
      <param name="declaration" as="element()"/>
      <param name="qname" as="xs:QName?"/>
      <param name="component-kind" as="xs:string"/>
      <param name="deterministic" as="xs:boolean"/>

      <variable name="escaped-name" select="
         if (exists($qname)) then
            replace(string($qname), '[^A-Za-z0-9]', '_')
         else ()"/>

      <variable name="id" select="
         if ($deterministic and exists($qname)) then
            replace(string(src:qname-id($qname)), '-', '_')
         else
            generate-id($declaration)"/>

      <sequence select="
         if ($src:cls-compliant and $deterministic) then
            string-join(($escaped-name, $component-kind, $id), '_')
         else
            src:aux-variable(string-join(($component-kind, $escaped-name, $id), '_'))"/>
   </function>

   <function name="src:qname-id" as="xs:integer">
      <param name="p1" as="xs:QName"/>

      <sequence select="src:_qname-id($p1)"/>
   </function>

   <function name="xcst:item-type" as="xs:string">
      <param name="name" as="xs:string"/>
      <param name="language" as="xs:string"/>

      <choose>
         <when test="xcst:language-equal($language, $xcst:csharp-lang)">
            <sequence select="cs:item-type($name)"/>
         </when>
         <when test="xcst:language-equal($language, $xcst:vb-lang)">
            <sequence select="vb:item-type($name)"/>
         </when>
         <otherwise>
            <sequence select="error()"/>
         </otherwise>
      </choose>
   </function>

   <function name="xcst:cardinality" as="xs:string">
      <param name="name" as="xs:string"/>
      <param name="language" as="xs:string"/>

      <choose>
         <when test="xcst:language-equal($language, $xcst:csharp-lang)">
            <sequence select="cs:cardinality($name)"/>
         </when>
         <when test="xcst:language-equal($language, $xcst:vb-lang)">
            <sequence select="vb:cardinality($name)"/>
         </when>
         <otherwise>
            <sequence select="error()"/>
         </otherwise>
      </choose>
   </function>

   <function name="xcst:unescape-identifier" as="xs:string">
      <param name="name" as="xs:string"/>
      <param name="language" as="xs:string"/>

      <choose>
         <when test="xcst:language-equal($language, $xcst:csharp-lang)">
            <sequence select="cs:unescape-identifier($name)"/>
         </when>
         <when test="xcst:language-equal($language, $xcst:vb-lang)">
            <sequence select="vb:unescape-identifier($name)"/>
         </when>
         <otherwise>
            <sequence select="error()"/>
         </otherwise>
      </choose>
   </function>

   <function name="src:quotes-to-escape" as="xs:integer*">
      <param name="text" as="xs:string"/>
      <param name="context-node" as="node()"/>
      <param name="language" as="xs:string"/>

      <variable name="char-pos" select="1"/>
      <variable name="modes" select="'Text'"/>

      <choose>
         <when test="xcst:language-equal($language, $xcst:csharp-lang)">
            <sequence select="cs:quotes-to-escape($text, $context-node, $char-pos, $modes)"/>
         </when>
         <when test="xcst:language-equal($language, $xcst:vb-lang)">
            <sequence select="vb:quotes-to-escape($text, $context-node, $char-pos, $modes)"/>
         </when>
         <otherwise>
            <sequence select="error()"/>
         </otherwise>
      </choose>
   </function>

   <!--
      ## Code Generation
   -->

   <template name="src:compilation-units">
      <param name="modules" tunnel="yes"/>
      <param name="used-packages" tunnel="yes"/>
      <param name="language" required="yes" tunnel="yes"/>

      <variable name="namespaces" as="element(code:namespace)+">
         <apply-templates select="$used-packages, $modules" mode="src:namespace"/>
      </variable>

      <choose>
         <when test="xcst:language-equal($language, $xcst:csharp-lang)">
            <src:compilation-unit>
               <apply-templates select="$namespaces" mode="cs:source">
                  <with-param name="indent" select="0" tunnel="yes"/>
               </apply-templates>
            </src:compilation-unit>
         </when>
         <when test="xcst:language-equal($language, $xcst:vb-lang)">
            <for-each select="$namespaces">
               <variable name="comp-unit" as="element()">
                  <choose>
                     <when test="code:import">
                        <code:compilation-unit>
                           <sequence select="code:import"/>
                           <code:namespace>
                              <sequence select="@*"/>
                              <sequence select="code:* except code:import"/>
                           </code:namespace>
                        </code:compilation-unit>
                     </when>
                     <otherwise>
                        <sequence select="."/>
                     </otherwise>
                  </choose>
               </variable>
               <src:compilation-unit>
                  <apply-templates select="$comp-unit" mode="vb:source">
                     <with-param name="indent" select="0" tunnel="yes"/>
                  </apply-templates>
               </src:compilation-unit>
            </for-each>
         </when>
      </choose>
   </template>

   <function name="src:template-context" as="element()">
      <param name="meta" as="element()?"/>

      <sequence select="src:template-context($meta, ())"/>
   </function>

   <function name="src:template-context" as="element()">
      <param name="meta" as="element()?"/>
      <param name="el" as="element()?"/>

      <variable name="base-type" select="src:package-model-type('TemplateContext')"/>

      <src:context>
         <choose>
            <when test="$meta[self::xcst:template] and $meta/xcst:typed-params(.)">
               <code:type-reference>
                  <copy-of select="$base-type/@*"/>
                  <code:type-arguments>
                     <sequence select="src:params-type($meta)"/>
                  </code:type-arguments>
               </code:type-reference>
            </when>
            <otherwise>
               <sequence select="$base-type"/>
            </otherwise>
         </choose>
         <src:reference>
            <code:variable-reference name="{src:aux-variable('context')}{if ($el) then concat('_', generate-id($el)) else ()}"/>
         </src:reference>
      </src:context>
   </function>

   <function name="xcst:typed-params" as="xs:boolean">
      <param name="meta" as="element(xcst:template)"/>

      <sequence select="exists($meta/xcst:param[not(@tunnel/xs:boolean(.))])"/>
   </function>

   <function name="src:params-type" as="element()">
      <param name="meta" as="element()"/>

      <code:type-reference name="{src:params-type-name($meta)}">
         <if test="$meta/@accepted/xs:boolean(.)">
            <sequence select="$meta/xcst:package-type/code:type-reference"/>
         </if>
      </code:type-reference>
   </function>

   <function name="src:params-type-name" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="concat($meta/@member-name, '_params')"/>
   </function>

   <function name="src:params-type-init-name" as="xs:string">
      <param name="name" as="item()"/>

      <sequence select="src:aux-variable(concat('init_', $name))"/>
   </function>

   <function name="src:template-output" as="element()">
      <param name="meta" as="element()?"/>

      <sequence select="src:template-output($meta, ())"/>
   </function>

   <function name="src:template-output" as="element()">
      <param name="meta" as="element()?"/>
      <param name="el" as="element()?"/>

      <choose>
         <when test="$meta[self::xcst:attribute-set]">
            <sequence select="src:doc-output((), ())"/>
         </when>
         <otherwise>
            <variable name="item-type-ref" select="$meta/xcst:item-type/code:type-reference"/>
            <src:output kind="obj">
               <if test="not($item-type-ref)">
                  <attribute name="item-type-is-object" select="true()"/>
               </if>
               <code:type-reference>
                  <copy-of select="src:package-model-type('ISequenceWriter')/@*"/>
                  <code:type-arguments>
                     <sequence select="($item-type-ref, $src:object-type)[1]"/>
                  </code:type-arguments>
               </code:type-reference>
               <src:reference>
                  <code:variable-reference name="{src:aux-variable('output')}{if ($el) then concat('_', generate-id($el)) else ()}"/>
               </src:reference>
            </src:output>
         </otherwise>
      </choose>
   </function>

   <function name="src:doc-output" as="item()">
      <param name="node" as="node()?"/>
      <param name="output" as="item()?"/>

      <choose>
         <when test="$output and src:output-is-doc($output)">
            <sequence select="$output"/>
         </when>
         <otherwise>
            <src:output kind="doc">
               <code:type-reference name="XcstWriter" namespace="Xcst"/>
               <src:reference>
                  <code:variable-reference name="{src:aux-variable('output')}{if ($node) then concat('_', generate-id($node)) else ()}"/>
               </src:reference>
            </src:output>
         </otherwise>
      </choose>
   </function>

   <function name="src:output-is-doc" as="xs:boolean">
      <param name="output" as="item()"/>

      <sequence select="$output instance of element() and $output/@kind[. eq 'doc']"/>
   </function>

   <function name="src:output-is-obj" as="xs:boolean">
      <param name="output" as="item()"/>

      <sequence select="$output instance of element() and $output/@kind[. eq 'obj']"/>
   </function>

   <function name="src:item-type-inference-member-name" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="concat($meta/@member-name, '_infer')"/>
   </function>

   <function name="src:item-type-inference-member-ref" as="element(code:method-reference)">
      <param name="meta" as="element()"/>

      <choose>
         <when test="$meta/xcst:item-type">
            <code:method-reference name="{src:item-type-inference-member-name($meta)}">
               <sequence select="($meta/xcst:package-type/code:type-reference, $meta/../code:type-reference)[1]"/>
            </code:method-reference>
         </when>
         <otherwise>
            <code:method-reference name="DefaultInfer">
               <sequence select="src:helper-type('SequenceWriter')"/>
            </code:method-reference>
         </otherwise>
      </choose>
   </function>

   <function name="src:package-model-type" as="element()">
      <param name="type" as="xs:string"/>

      <sequence select="src:package-model-type($type, false())"/>
   </function>

   <function name="src:package-model-type" as="element()">
      <param name="type" as="xs:string"/>
      <param name="interface" as="xs:boolean"/>

      <code:type-reference name="{$type}" namespace="Xcst.PackageModel">
         <if test="$interface">
            <attribute name="interface" select="'true'"/>
         </if>
      </code:type-reference>
   </function>

   <template name="src:editor-browsable-never">
      <code:attribute>
         <code:type-reference name="EditorBrowsable" namespace="System.ComponentModel"/>
         <code:arguments>
            <code:field-reference name="Never">
               <code:type-reference name="EditorBrowsableState" namespace="System.ComponentModel"/>
            </code:field-reference>
         </code:arguments>
      </code:attribute>
   </template>

   <template name="src:new-line-indented">
      <param name="indent" select="0" tunnel="yes"/>

      <value-of select="$src:new-line"/>

      <value-of select="for $p in (1 to $indent) return $src:indent" separator=""/>
   </template>

   <function name="src:local-path" as="xs:string">
      <param name="p1" as="xs:anyURI"/>

      <sequence select="src:_local-path($p1)"/>
   </function>

   <function name="src:make-relative-uri" as="xs:anyURI">
      <param name="p1" as="xs:anyURI"/>
      <param name="p2" as="xs:anyURI"/>

      <!-- This function is used by XCST-a -->

      <sequence select="src:_make-relative-uri($p1, $p2)"/>
   </function>

   <!--
      ### Used Packages
   -->

   <template match="xcst:package-manifest[xs:boolean(@qualified-types)]" mode="src:namespace">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <code:namespace name="{$package-manifest/code:type-reference/@namespace}">
         <code:type name="{$package-manifest/code:type-reference/@name}" partial="true">
            <code:members>
               <code:field name="{src:used-package-field-name(.)}" readonly="true" line-hidden="true">
                  <code:type-reference name="{src:used-package-class-name(.)}"/>
                  <code:attributes>
                     <call-template name="src:editor-browsable-never"/>
                  </code:attributes>
               </code:field>
               <variable name="accepted-public" select="$package-manifest/xcst:*[
                  @package-id eq current()/@package-id
                  and @accepted/xs:boolean(.)
                  and @visibility ne 'hidden']"/>

               <apply-templates select="$accepted-public[not(self::xcst:type)]" mode="src:member">
                  <sort select="self::xcst:param or self::xcst:variable" order="descending"/>
               </apply-templates>

               <apply-templates select="." mode="src:used-package-class"/>
            </code:members>
         </code:type>
      </code:namespace>
   </template>

   <template match="xcst:package-manifest[not(xs:boolean(@qualified-types))]" mode="src:namespace">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <variable name="meta" select="."/>
      <variable name="accepted-public" select="$package-manifest/xcst:*[
         @package-id eq $meta/@package-id
         and @accepted/xs:boolean(.)
         and @visibility ne 'hidden']"/>
      <variable name="overridden" select="src:overridden-components(., $package-manifest)"/>
      <variable name="module-uris" select="distinct-values(($accepted-public, $overridden)/@declaring-module-uri)"/>

      <for-each select="if (exists($module-uris)) then $module-uris else ''">
         <variable name="module-uri" select="."/>

         <code:namespace name="{$package-manifest/code:type-reference/@namespace}">
            <if test="$module-uri">
               <apply-templates select="
                  $meta/xcst:type[@accepted/xs:boolean(.) and @visibility ne 'hidden'],
                  $accepted-public[self::xcst:type],
                  doc($module-uri)/*/c:import-namespace" mode="src:import-namespace"/>
            </if>
            <code:type name="{$package-manifest/code:type-reference/@name}" partial="true">
               <code:members>
                  <if test="position() eq 1">
                     <code:field name="{src:used-package-field-name($meta)}" readonly="true" line-hidden="true">
                        <code:type-reference name="{src:used-package-class-name($meta)}"/>
                        <code:attributes>
                           <call-template name="src:editor-browsable-never"/>
                        </code:attributes>
                     </code:field>
                  </if>
                  <apply-templates select="$accepted-public[not(self::xcst:type) and @declaring-module-uri eq $module-uri]" mode="src:member">
                     <sort select="self::xcst:param or self::xcst:variable" order="descending"/>
                  </apply-templates>
                  <apply-templates select="$meta" mode="src:used-package-class">
                     <with-param name="overridden" select="$overridden[@declaring-module-uri eq $module-uri]"/>
                  </apply-templates>
               </code:members>
            </code:type>
         </code:namespace>
      </for-each>
   </template>

   <template match="xcst:param | xcst:variable" mode="src:member">

      <variable name="public" select="@visibility ne 'private'"/>

      <variable name="used-pkg-field" as="element()">
         <code:field-reference name="{@member-name}" verbatim="true">
            <code:field-reference name="{src:used-package-field-name(.)}">
               <code:this-reference/>
            </code:field-reference>
         </code:field-reference>
      </variable>

      <code:property name="{@member-name}"
            visibility="{('public'[$public], 'private')[1]}"
            extensibility="{if (@visibility eq 'public') then 'virtual' else '#default'}"
            verbatim="true">
         <sequence select="code:type-reference"/>
         <code:attributes>
            <if test="$public">
               <code:attribute>
                  <sequence select="src:package-model-type(concat('Xcst', (if (self::xcst:param) then 'Parameter' else 'Variable')))"/>
                  <code:initializer>
                     <if test="self::xcst:param[xs:boolean(@required)]">
                        <code:member-initializer name="Required">
                           <code:bool value="true"/>
                        </code:member-initializer>
                     </if>
                  </code:initializer>
               </code:attribute>
            </if>
         </code:attributes>
         <code:getter>
            <code:block>
               <code:return>
                  <sequence select="$used-pkg-field"/>
               </code:return>
            </code:block>
         </code:getter>
         <code:setter>
            <code:block>
               <code:assign>
                  <sequence select="$used-pkg-field"/>
                  <code:setter-value/>
               </code:assign>
            </code:block>
         </code:setter>
      </code:property>
   </template>

   <template match="xcst:template" mode="src:member"/>

   <template match="xcst:function" mode="src:member">

      <variable name="public" select="@visibility ne 'private'"/>

      <code:method name="{@member-name}"
            visibility="{('public'[$public], 'private')[1]}"
            extensibility="{if (@visibility eq 'public') then 'virtual' else '#default'}"
            verbatim="true">
         <if test="code:type-reference">
            <attribute name="return-type-verbatim" select="true()"/>
         </if>
         <sequence select="code:type-reference"/>
         <code:attributes>
            <if test="$public">
               <sequence select="src:package-model-type('XcstFunction')"/>
            </if>
         </code:attributes>
         <code:parameters>
            <for-each select="xcst:param">
               <code:parameter name="{@name}" verbatim="true">
                  <sequence select="code:*"/>
               </code:parameter>
            </for-each>
         </code:parameters>
         <code:block>
            <variable name="method-call" as="element()">
               <code:method-call name="{@member-name}" verbatim="true">
                  <code:field-reference name="{src:used-package-field-name(.)}">
                     <code:this-reference/>
                  </code:field-reference>
                  <code:arguments>
                     <for-each select="xcst:param">
                        <code:variable-reference name="{@name}" verbatim="true"/>
                     </for-each>
                  </code:arguments>
               </code:method-call>
            </variable>
            <choose>
               <when test="code:type-reference">
                  <code:return>
                     <sequence select="$method-call"/>
                  </code:return>
               </when>
               <otherwise>
                  <sequence select="$method-call"/>
               </otherwise>
            </choose>
         </code:block>
      </code:method>
   </template>

   <template match="xcst:attribute-set" mode="src:member"/>

   <template match="xcst:package-manifest" mode="src:used-package-class">
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="overridden" select="src:overridden-components(., $package-manifest)"/>

      <variable name="qualified-types" select="xs:boolean(@qualified-types)"/>

      <code:type name="{src:used-package-class-name(.)}" visibility="private" partial="{not($qualified-types)}">
         <code:base-types>
            <sequence select="code:type-reference"/>
         </code:base-types>
         <code:attributes>
            <if test="$qualified-types">
               <call-template name="src:editor-browsable-never"/>
            </if>
         </code:attributes>
         <code:members>
            <apply-templates select="$overridden" mode="src:used-package-overriding"/>
            <apply-templates select="$overridden" mode="src:used-package-override"/>
            <apply-templates select="$overridden[@original-visibility ne 'abstract']" mode="src:used-package-original"/>
            <apply-templates select="$overridden[self::xcst:param or self::xcst:variable]" mode="src:used-package-tuple"/>
         </code:members>
      </code:type>
   </template>

   <template match="xcst:template | xcst:attribute-set | xcst:function | xcst:variable | xcst:param" mode="src:used-package-overriding">
      <code:field name="{src:overriding-field-name(.)}" visibility="public">
         <apply-templates select="." mode="src:used-package-overriding-type"/>
      </code:field>
   </template>

   <template match="xcst:param | xcst:variable" mode="src:used-package-overriding-type">
      <code:type-reference name="Tuple" namespace="System">
         <code:type-arguments>
            <code:type-reference name="Func" namespace="System">
               <code:type-arguments>
                  <sequence select="code:type-reference"/>
               </code:type-arguments>
            </code:type-reference>
            <code:type-reference name="Action" namespace="System">
               <code:type-arguments>
                  <sequence select="code:type-reference"/>
               </code:type-arguments>
            </code:type-reference>
         </code:type-arguments>
      </code:type-reference>
   </template>

   <template match="xcst:template | xcst:attribute-set" mode="src:used-package-overriding-type">
      <code:type-reference name="Action" namespace="System">
         <code:type-arguments>
            <sequence select="
               for $c in (src:template-context(.), src:template-output(.))
               return $c/code:type-reference"/>
         </code:type-arguments>
      </code:type-reference>
   </template>

   <template match="xcst:function" mode="src:used-package-overriding-type">
      <code:type-reference name="{if (code:type-reference) then 'Func' else 'Action'}" namespace="System">
         <code:type-arguments>
            <sequence select="
               for $x in (xcst:param, .)
               return $x/code:type-reference"/>
         </code:type-arguments>
      </code:type-reference>
   </template>

   <template match="xcst:variable | xcst:param" mode="src:used-package-override">

      <variable name="field" as="element()">
         <code:field-reference name="{src:overriding-field-name(.)}">
            <code:this-reference/>
         </code:field-reference>
      </variable>

      <code:property name="{@member-name}" visibility="public" extensibility="override">
         <sequence select="code:type-reference"/>
         <code:getter>
            <code:block>
               <code:return>
                  <code:method-call name="Invoke">
                     <code:property-reference name="Item1">
                        <sequence select="$field"/>
                     </code:property-reference>
                  </code:method-call>
               </code:return>
            </code:block>
         </code:getter>
         <code:setter>
            <code:block>
               <code:method-call name="Invoke">
                  <code:property-reference name="Item2">
                     <sequence select="$field"/>
                  </code:property-reference>
                  <code:arguments>
                     <code:setter-value/>
                  </code:arguments>
               </code:method-call>
            </code:block>
         </code:setter>
      </code:property>
   </template>

   <template match="xcst:template | xcst:attribute-set" mode="src:used-package-override">

      <variable name="context" select="src:template-context(.)"/>
      <variable name="output" select="src:template-output(.)"/>

      <code:method name="{@member-name}" visibility="public" extensibility="override">
         <code:parameters>
            <code:parameter name="{$context/src:reference/code:*/@name}">
               <sequence select="$context/code:type-reference"/>
            </code:parameter>
            <code:parameter name="{$output/src:reference/code:*/@name}">
               <sequence select="$output/code:type-reference"/>
            </code:parameter>
         </code:parameters>
         <code:block>
            <code:method-call name="{src:overriding-field-name(.)}">
               <code:this-reference/>
               <code:arguments>
                  <sequence select="$context/src:reference/code:*"/>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:block>
      </code:method>
   </template>

   <template match="xcst:function" mode="src:used-package-override">
      <code:method name="{@member-name}" visibility="public" extensibility="override">
         <sequence select="code:type-reference"/>
         <code:parameters>
            <for-each select="xcst:param">
               <code:parameter name="{@name}">
                  <sequence select="code:type-reference"/>
               </code:parameter>
            </for-each>
         </code:parameters>
         <code:block>
            <variable name="method-call" as="element()">
               <code:method-call name="{src:overriding-field-name(.)}">
                  <code:this-reference/>
                  <code:arguments>
                     <for-each select="xcst:param">
                        <code:variable-reference name="{@name}"/>
                     </for-each>
                  </code:arguments>
               </code:method-call>
            </variable>
            <choose>
               <when test="code:type-reference">
                  <code:return>
                     <sequence select="$method-call"/>
                  </code:return>
               </when>
               <otherwise>
                  <sequence select="$method-call"/>
               </otherwise>
            </choose>
         </code:block>
      </code:method>
   </template>

   <template match="xcst:template | xcst:attribute-set" mode="src:used-package-original">

      <variable name="context" select="src:template-context(.)"/>
      <variable name="output" select="src:template-output(.)"/>

      <code:method name="{src:original-member-name(.)}" visibility="internal">
         <code:parameters>
            <code:parameter name="{$context/src:reference/code:*/@name}">
               <sequence select="$context/code:type-reference"/>
            </code:parameter>
            <code:parameter name="{$output/src:reference/code:*/@name}">
               <sequence select="$output/code:type-reference"/>
            </code:parameter>
         </code:parameters>
         <code:block>
            <code:method-call name="{@member-name}">
               <code:base-reference/>
               <code:arguments>
                  <sequence select="$context/src:reference/code:*"/>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:block>
      </code:method>
   </template>

   <template match="xcst:function" mode="src:used-package-original">
      <code:method name="{src:original-member-name(.)}" visibility="internal">
         <code:type-reference name="{if (code:type-reference) then 'Func' else 'Action'}" namespace="System">
            <code:type-arguments>
               <sequence select="
                  for $x in (xcst:param, .)
                  return $x/code:type-reference"/>
            </code:type-arguments>
         </code:type-reference>
         <code:block>
            <code:return>
               <code:method-reference name="{@member-name}">
                  <code:base-reference/>
               </code:method-reference>
            </code:return>
         </code:block>
      </code:method>
   </template>

   <template match="xcst:param[not(xs:boolean(@required))] | xcst:variable" mode="src:used-package-original">
      <code:method name="{src:original-member-name(.)}" visibility="internal">
         <code:type-reference name="Func" namespace="System">
            <code:type-arguments>
               <sequence select="code:type-reference"/>
            </code:type-arguments>
         </code:type-reference>
         <code:block>
            <code:return>
               <code:lambda>
                  <code:property-reference name="{@member-name}">
                     <code:base-reference/>
                  </code:property-reference>
               </code:lambda>
            </code:return>
         </code:block>
      </code:method>
   </template>

   <template match="xcst:param | xcst:variable" mode="src:used-package-tuple">

      <variable name="getter" select="src:aux-variable('getter')"/>
      <variable name="setter" select="src:aux-variable('setter')"/>

      <code:method name="{src:tuple-member-name(.)}" visibility="internal" extensibility="static">
         <apply-templates select="." mode="src:used-package-overriding-type"/>
         <code:parameters>
            <code:parameter name="{$getter}">
               <code:type-reference name="Func" namespace="System">
                  <code:type-arguments>
                     <sequence select="code:type-reference"/>
                  </code:type-arguments>
               </code:type-reference>
            </code:parameter>
            <code:parameter name="{$setter}">
               <code:type-reference name="Action" namespace="System">
                  <code:type-arguments>
                     <sequence select="code:type-reference"/>
                  </code:type-arguments>
               </code:type-reference>
            </code:parameter>
         </code:parameters>
         <code:block>
            <code:return>
               <code:method-call name="Create">
                  <code:type-reference name="Tuple" namespace="System"/>
                  <code:arguments>
                     <code:variable-reference name="{$getter}"/>
                     <code:variable-reference name="{$setter}"/>
                  </code:arguments>
               </code:method-call>
            </code:return>
         </code:block>
      </code:method>
   </template>

   <function name="src:overridden-components" as="element()*">
      <param name="used-package" as="element(xcst:package-manifest)"/>
      <param name="package-manifest" as="element(xcst:package-manifest)"/>

      <sequence select="
         for $c in $package-manifest/xcst:*[@package-id eq $used-package/@package-id]
         return (if ($package-manifest/xcst:*[@overrides eq $c/@id]) then $c else ())"/>
   </function>

   <function name="src:overriding-field-name" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="src:aux-variable(concat('overr_', $meta/@id))"/>
   </function>

   <function name="src:original-member-name" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="src:aux-variable(concat('original_', $meta/@id))"/>
   </function>

   <function name="src:tuple-member-name" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="src:aux-variable(concat('tuple_', $meta/@id))"/>
   </function>

   <function name="src:original-member" as="element()">
      <param name="meta" as="element()"/>

      <code:method-reference name="{src:original-member-name($meta)}">
         <code:field-reference name="{src:used-package-field-name($meta)}">
            <code:this-reference/>
         </code:field-reference>
      </code:method-reference>
   </function>

   <function name="src:used-package-class-name" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="src:aux-variable(concat('package_', $meta/@package-id))"/>
   </function>

   <function name="src:used-package-field-name" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="src:aux-variable(concat('pkg_', $meta/@package-id))"/>
   </function>

   <!--
      ### Top-level Package
   -->

   <template match="c:module | c:package" mode="src:namespace">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <variable name="class" as="element()">
         <apply-templates select="." mode="src:class"/>
      </variable>

      <if test="exists($class/code:members/code:*/(if (self::code:region) then code:* else .))">
         <code:namespace name="{$package-manifest/code:type-reference/@namespace}">
            <apply-templates select="$package-manifest/xcst:type[@accepted/xs:boolean(.) and @visibility ne 'hidden']" mode="src:import-namespace"/>
            <apply-templates select="c:import-namespace" mode="src:import-namespace"/>
            <apply-templates select="." mode="src:import-namespace-extra"/>
            <sequence select="$class"/>
         </code:namespace>
      </if>
   </template>

   <template match="c:module/node() | c:package/node()" mode="src:import-namespace-extra"/>

   <template match="xcst:type" mode="src:import-namespace">
      <code:import alias="{@name}" line-hidden="true" verbatim="true" type-verbatim="true">
         <code:type-reference name="{@name}">
            <sequence select="xcst:package-type/code:type-reference"/>
         </code:type-reference>
      </code:import>
   </template>

   <template match="c:import-namespace" mode="src:import-namespace">

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'ns'"/>
         <with-param name="optional" select="'alias'"/>
      </call-template>

      <call-template name="xcst:no-children"/>
      <call-template name="xcst:no-other-preceding"/>

      <code:import namespace="{xcst:type(@ns)}">
         <call-template name="src:line-number"/>
         <if test="@alias">
            <attribute name="alias" select="xcst:type(@alias)"/>
         </if>
      </code:import>
   </template>

   <template match="c:module | c:package" mode="src:class">
      <param name="modules" tunnel="yes"/>
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <variable name="module-pos" select="
         for $pos in (1 to count($modules)) 
         return if ($modules[$pos] is current()) then $pos else ()"/>

      <variable name="principal-module" select="$module-pos eq count($modules)"/>
      <variable name="abstract" select="$principal-module and $package-manifest/xcst:*[@visibility eq 'abstract']"/>

      <code:type name="{$package-manifest/code:type-reference/@name}"
            visibility="{($src:visibility[$principal-module], '#default')[1]}"
            extensibility="{('abstract'[$abstract], '#default')[1]}"
            partial="true">

         <if test="$principal-module">
            <code:base-types>
               <variable name="base-types" as="element(code:type-reference)*">
                  <variable name="custom" as="element(code:type-reference)*">
                     <apply-templates select="." mode="src:base-types"/>
                  </variable>
                  <sequence select="if (exists($custom)) then $custom else $src:base-types"/>
               </variable>
               <sequence select="$base-types, $src:package-interface"/>
            </code:base-types>
         </if>

         <code:members>
            <variable name="global-vars" as="element()*">
               <for-each select="(., c:use-package/c:override)/(c:param | c:variable)">
                  <if test="$package-manifest/xcst:*[@declaration-id eq current()/generate-id()]/@visibility ne 'hidden'">
                     <sequence select="."/>
                  </if>
               </for-each>
            </variable>

            <apply-templates select="$global-vars" mode="src:member"/>

            <apply-templates select="(., c:use-package/c:override)/
               (c:attribute-set | c:function | c:template | c:type)" mode="src:member"/>

            <code:region name="Infrastructure">
               <if test="$principal-module">
                  <call-template name="src:execution-context"/>
                  <call-template name="src:constructor"/>
               </if>
               <call-template name="src:prime-method">
                  <with-param name="principal-module" select="$principal-module"/>
               </call-template>
               <if test="$principal-module">
                  <call-template name="src:get-template-method"/>
                  <call-template name="src:read-output-definition-method"/>
                  <apply-templates select="$package-manifest/xcst:output" mode="src:member"/>
               </if>
               <apply-templates select="." mode="src:infrastructure-extra"/>
            </code:region>
         </code:members>
      </code:type>
   </template>

   <template match="c:module/node() | c:package/node()" mode="src:base-types"/>

   <template match="c:module/node() | c:package/node()" mode="src:infrastructure-extra"/>

   <template match="c:param | c:variable" mode="src:member">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <variable name="meta" select="$package-manifest/xcst:*[@declaration-id eq current()/generate-id()]"/>
      <variable name="public" select="$meta/@visibility = ('public', 'final', 'abstract')"/>

      <variable name="use-backing-field" select="
         (self::c:param and $meta/not(xs:boolean(@required)))
         or ($meta/@visibility ne 'abstract' and $meta/xs:boolean(@has-default-value))"/>

      <variable name="backing-field-name" select="
         if ($use-backing-field) then src:backing-field($meta) else ()"/>

      <variable name="init-field-name" select="
         if ($use-backing-field) then src:aux-variable(concat('init_', $meta/@name)) else ()"/>

      <variable name="init-field" as="element()">
         <code:field-reference name="{$init-field-name}">
            <code:this-reference/>
         </code:field-reference>
      </variable>

      <variable name="backing-field" as="element()">
         <code:field-reference name="{$backing-field-name}">
            <code:this-reference/>
         </code:field-reference>
      </variable>

      <if test="$use-backing-field">
         <code:field name="{$backing-field-name}" line-hidden="true">
            <sequence select="$meta/code:type-reference"/>
            <code:attributes>
               <call-template name="src:editor-browsable-never"/>
            </code:attributes>
         </code:field>
         <code:field name="{$init-field-name}" line-hidden="true">
            <code:type-reference name="Boolean" namespace="System"/>
            <code:attributes>
               <call-template name="src:editor-browsable-never"/>
            </code:attributes>
         </code:field>
      </if>

      <code:property name="{$meta/@member-name}"
            visibility="{('public'[$public], 'private')[1]}"
            extensibility="{('virtual'[$meta/@visibility eq 'public'], 'abstract'[$meta/@visibility eq 'abstract'], '#default')[1]}">
         <if test="$meta/@member-name-was-escaped/xs:boolean(.)">
            <attribute name="verbatim" select="true()"/>
         </if>
         <call-template name="src:line-number"/>
         <sequence select="$meta/code:type-reference"/>
         <code:attributes>
            <if test="$public">
               <code:attribute>
                  <sequence select="src:package-model-type(concat('Xcst', (if (self::c:param) then 'Parameter' else 'Variable')))"/>
                  <code:initializer>
                     <if test="self::c:param and $meta/xs:boolean(@required)">
                        <code:member-initializer name="Required">
                           <code:bool value="true"/>
                        </code:member-initializer>
                     </if>
                  </code:initializer>
               </code:attribute>
            </if>
         </code:attributes>
         <code:getter>
            <if test="$use-backing-field">
               <code:block>
                  <code:if>
                     <code:not>
                        <sequence select="$init-field"/>
                     </code:not>
                     <code:block>
                        <if test="parent::c:override">
                           <variable name="original-meta" select="$package-manifest/xcst:*[@id eq $meta/@overrides]"/>
                           <variable name="original-ref" select="
                              if ($original-meta/@original-visibility ne 'abstract'
                                 and ($original-meta[self::xcst:variable] or not(xs:boolean($original-meta/@required)))) then
                                    src:original-member($original-meta)
                              else ()"/>
                           <code:variable name="{$src:contextual-variable}">
                              <code:new-object>
                                 <code:initializer>
                                    <if test="$original-ref">
                                       <code:member-initializer name="original">
                                          <code:method-call>
                                             <sequence select="$original-ref"/>
                                          </code:method-call>
                                       </code:member-initializer>
                                    </if>
                                 </code:initializer>
                              </code:new-object>
                           </code:variable>
                        </if>
                        <apply-templates select="." mode="src:statement">
                           <with-param name="context" as="element()" tunnel="yes">
                              <src:context>
                                 <sequence select="src:package-model-type('PrimingContext')"/>
                                 <src:reference>
                                    <code:property-reference name="PrimingContext">
                                       <sequence select="$src:context-field/src:reference/code:*"/>
                                    </code:property-reference>
                                 </src:reference>
                              </src:context>
                           </with-param>
                        </apply-templates>
                        <code:assign>
                           <sequence select="$init-field"/>
                           <code:bool value="true"/>
                        </code:assign>
                     </code:block>
                  </code:if>
                  <code:return>
                     <sequence select="$backing-field"/>
                  </code:return>
               </code:block>
            </if>
         </code:getter>
         <code:setter>
            <if test="$use-backing-field">
               <code:block>
                  <code:assign>
                     <sequence select="$backing-field"/>
                     <code:setter-value/>
                  </code:assign>
                  <code:assign>
                     <sequence select="$init-field"/>
                     <code:bool value="true"/>
                  </code:assign>
               </code:block>
            </if>
         </code:setter>
      </code:property>
   </template>

   <template match="c:template" mode="src:member">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <variable name="meta" select="$package-manifest/xcst:template[@declaration-id eq current()/generate-id()]"/>
      <variable name="public" select="$meta/@visibility = ('public', 'final', 'abstract')"/>
      <variable name="context" select="src:template-context($meta)"/>
      <variable name="output" select="src:template-output($meta)"/>

      <if test="not($meta/@visibility eq 'hidden' and $meta/@declared-visibility eq 'abstract')">

         <code:method name="{$meta/@member-name}"
               visibility="{('public'[$public], 'private')[1]}"
               extensibility="{('virtual'[$meta/@visibility eq 'public'], 'abstract'[$meta/@visibility eq 'abstract'], '#default')[1]}">
            <code:attributes>
               <if test="$public">
                  <variable name="qname" select="xcst:EQName($meta/@name)"/>

                  <code:attribute>
                     <sequence select="src:package-model-type('XcstTemplate')"/>
                     <code:initializer>
                        <code:member-initializer name="Name">
                           <code:string verbatim="true">
                              <value-of select="xcst:uri-qualified-name($qname)"/>
                           </code:string>
                        </code:member-initializer>
                        <if test="$meta/@cardinality[. ne 'ZeroOrMore']">
                           <code:member-initializer name="Cardinality">
                              <code:field-reference name="{$meta/@cardinality}">
                                 <sequence select="src:package-model-type('XcstSequenceCardinality')"/>
                              </code:field-reference>
                           </code:member-initializer>
                        </if>
                     </code:initializer>
                  </code:attribute>

                  <for-each select="$meta/xcst:param">
                     <code:attribute>
                        <sequence select="src:package-model-type('XcstTemplateParameter')"/>
                        <code:arguments>
                           <code:string literal="true">
                              <value-of select="@name"/>
                           </code:string>
                           <code:typeof>
                              <sequence select="code:type-reference"/>
                           </code:typeof>
                        </code:arguments>
                        <code:initializer>
                           <if test="@required/xs:boolean(.)">
                              <code:member-initializer name="Required">
                                 <code:bool value="true"/>
                              </code:member-initializer>
                           </if>
                           <if test="@tunnel/xs:boolean(.)">
                              <code:member-initializer name="Tunnel">
                                 <code:bool value="true"/>
                              </code:member-initializer>
                           </if>
                        </code:initializer>
                     </code:attribute>
                  </for-each>
               </if>
               <call-template name="src:editor-browsable-never"/>
            </code:attributes>
            <code:parameters>
               <code:parameter name="{$context/src:reference/code:*/@name}">
                  <sequence select="$context/code:type-reference"/>
               </code:parameter>
               <code:parameter name="{$output/src:reference/code:*/@name}">
                  <sequence select="$output/code:type-reference"/>
               </code:parameter>
            </code:parameters>
            <if test="$meta/@visibility ne 'abstract'">
               <code:block>
                  <apply-templates select="c:param" mode="src:statement">
                     <with-param name="context" select="$context" tunnel="yes"/>
                  </apply-templates>
                  <call-template name="src:sequence-constructor">
                     <with-param name="children" select="node()[not(self::c:param or following-sibling::c:param)]"/>
                     <with-param name="context" select="$context" tunnel="yes"/>
                     <with-param name="output" select="$output" tunnel="yes"/>
                  </call-template>
               </code:block>
            </if>
         </code:method>

         <call-template name="src:template-additional-members">
            <with-param name="meta" select="$meta"/>
         </call-template>
      </if>
   </template>

   <template name="src:template-additional-members">
      <param name="meta" required="yes"/>
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <variable name="public" select="$meta/@visibility = ('public', 'final', 'abstract')"/>
      <variable name="item-type-ref" select="$meta/xcst:item-type/code:type-reference"/>

      <if test="$item-type-ref">
         <code:method name="{src:item-type-inference-member-name($meta)}"
               visibility="{('public'[$public], 'private')[1]}"
               extensibility="static"
               line-hidden="true">
            <sequence select="$item-type-ref"/>
            <code:attributes>
               <call-template name="src:editor-browsable-never"/>
            </code:attributes>
            <code:block>
               <code:return>
                  <code:default>
                     <sequence select="$item-type-ref"/>
                  </code:default>
               </code:return>
            </code:block>
         </code:method>
      </if>

      <if test="xcst:typed-params($meta)">
         <variable name="params-type" select="src:params-type-name($meta)"/>
         <variable name="non-tunnel-params" select="$meta/xcst:param[not(@tunnel/xs:boolean(.))]"/>

         <code:type name="{$params-type}" struct="true" line-hidden="true" visibility="{('public'[$public], 'private')[1]}">
            <code:attributes>
               <call-template name="src:editor-browsable-never"/>
            </code:attributes>
            <code:members>
               <for-each select="$non-tunnel-params">
                  <variable name="type-ref" select="code:type-reference"/>
                  <variable name="value-field" select="src:aux-variable(concat('back_', @name))"/>
                  <variable name="init-field" select="src:params-type-init-name(@name)"/>

                  <code:field name="{$value-field}">
                     <sequence select="$type-ref"/>
                  </code:field>

                  <code:field name="{$init-field}" visibility="public" disable-warning="CS3008">
                     <code:type-reference name="Boolean" namespace="System"/>
                  </code:field>

                  <code:property name="{@name}" visibility="public" verbatim="true">
                     <sequence select="$type-ref"/>
                     <code:getter>
                        <code:block>
                           <code:return>
                              <code:field-reference name="{$value-field}">
                                 <code:this-reference/>
                              </code:field-reference>
                           </code:return>
                        </code:block>
                     </code:getter>
                     <code:setter>
                        <code:block>
                           <code:assign>
                              <code:field-reference name="{$value-field}">
                                 <code:this-reference/>
                              </code:field-reference>
                              <code:setter-value/>
                           </code:assign>
                           <code:assign>
                              <code:field-reference name="{$init-field}">
                                 <code:this-reference/>
                              </code:field-reference>
                              <code:bool value="true"/>
                           </code:assign>
                        </code:block>
                     </code:setter>
                  </code:property>
               </for-each>

               <code:method name="Create" visibility="public" extensibility="static">
                  <variable name="var" select="src:aux-variable('p')"/>
                  <variable name="context" select="src:template-context(())"/>
                  <code:type-reference name="{$params-type}"/>
                  <code:parameters>
                     <code:parameter name="{$context/src:reference/code:*/@name}">
                        <sequence select="$context/code:type-reference"/>
                     </code:parameter>
                  </code:parameters>
                  <code:block>
                     <code:variable name="{$var}">
                        <code:new-object>
                           <code:type-reference name="{$params-type}"/>
                        </code:new-object>
                     </code:variable>
                     <for-each select="$non-tunnel-params">
                        <code:if>
                           <code:method-call name="HasParam">
                              <sequence select="$context/src:reference/code:*"/>
                              <code:arguments>
                                 <code:string literal="true">
                                    <value-of select="@name"/>
                                 </code:string>
                              </code:arguments>
                           </code:method-call>
                           <code:block>
                              <code:assign>
                                 <code:property-reference name="{@name}" verbatim="true">
                                    <code:variable-reference name="{$var}"/>
                                 </code:property-reference>
                                 <code:method-call name="Param">
                                    <sequence select="$context/src:reference/code:*"/>
                                    <code:type-arguments>
                                       <sequence select="code:type-reference"/>
                                    </code:type-arguments>
                                    <code:arguments>
                                       <code:string literal="true">
                                          <value-of select="@name"/>
                                       </code:string>
                                    </code:arguments>
                                 </code:method-call>
                              </code:assign>
                           </code:block>
                        </code:if>
                     </for-each>
                     <code:return>
                        <code:variable-reference name="{$var}"/>
                     </code:return>
                  </code:block>
               </code:method>

               <if test="$meta/@overrides">
                  <variable name="original-meta" select="$package-manifest/xcst:template[@id eq $meta/@overrides]"/>
                  <variable name="var" select="src:aux-variable('p')"/>

                  <code:conversion implicit="true">
                     <code:type-reference name="{$params-type}"/>
                     <code:parameters>
                        <code:parameter name="{$var}">
                           <sequence select="src:params-type($original-meta)"/>
                        </code:parameter>
                     </code:parameters>
                     <code:block>
                        <code:return>
                           <code:new-object>
                              <code:type-reference name="{$params-type}"/>
                              <code:initializer>
                                 <for-each select="$original-meta/xcst:param[not(@tunnel/xs:boolean(.))]">
                                    <code:member-initializer name="{@name}" verbatim="true">
                                       <code:property-reference name="{@name}" verbatim="true">
                                          <code:variable-reference name="{$var}"/>
                                       </code:property-reference>
                                    </code:member-initializer>
                                 </for-each>
                              </code:initializer>
                           </code:new-object>
                        </code:return>
                     </code:block>
                  </code:conversion>
               </if>
            </code:members>
         </code:type>
      </if>
   </template>

   <template match="c:function" mode="src:member">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <variable name="meta" select="$package-manifest/xcst:function[@declaration-id eq current()/generate-id()]"/>
      <variable name="public" select="$meta/@visibility = ('public', 'final', 'abstract')"/>
      <variable name="abstract" select="$meta/@visibility eq 'abstract'"/>

      <if test="not($meta/@visibility eq 'hidden' and $meta/@declared-visibility eq 'abstract')">

         <code:method name="{$meta/@member-name}"
               visibility="{('public'[$public], 'private')[1]}"
               extensibility="{('virtual'[$meta/@visibility eq 'public'], 'abstract'[$abstract], '#default')[1]}">
            <if test="$meta/@member-name-was-escaped/xs:boolean(.)">
               <attribute name="verbatim" select="true()"/>
            </if>
            <call-template name="src:line-number"/>
            <sequence select="$meta/code:type-reference"/>
            <code:attributes>
               <if test="$public">
                  <code:attribute>
                     <sequence select="src:package-model-type('XcstFunction')"/>
                  </code:attribute>
               </if>
               <if test="$meta/@visibility eq 'hidden'">
                  <call-template name="src:editor-browsable-never"/>
               </if>
            </code:attributes>
            <code:parameters>
               <for-each select="c:param">
                  <variable name="pos" select="position()"/>
                  <variable name="param-meta" select="$meta/xcst:param[$pos]"/>
                  <code:parameter name="{xcst:name(@name)}">
                     <call-template name="src:line-number"/>
                     <sequence select="$param-meta/code:*"/>
                  </code:parameter>
               </for-each>
            </code:parameters>
            <if test="not($abstract)">
               <variable name="hidden-meta" select="$meta/preceding-sibling::xcst:*[xcst:homonymous(., $meta) and not(@accepted/xs:boolean(.))][1]"/>
               <code:block>
                  <if test="parent::c:override or $hidden-meta">
                     <variable name="original-meta" select="$package-manifest/xcst:function[@id eq $meta/@overrides]"/>

                     <code:variable name="{$src:contextual-variable}" line-hidden="true">
                        <code:new-object>
                           <code:initializer>
                              <if test="$original-meta/@original-visibility ne 'abstract'">
                                 <code:member-initializer name="original">
                                    <code:method-call>
                                       <sequence select="src:original-member($original-meta)"/>
                                    </code:method-call>
                                 </code:member-initializer>
                              </if>
                              <if test="$hidden-meta[@declared-visibility ne 'abstract']">
                                 <code:member-initializer name="next_function">
                                    <code:method-call name="{src:hidden-function-delegate-method-name($hidden-meta)}">
                                       <code:this-reference/>
                                    </code:method-call>
                                 </code:member-initializer>
                              </if>
                           </code:initializer>
                        </code:new-object>
                     </code:variable>
                  </if>
                  <call-template name="src:sequence-constructor">
                     <with-param name="children" select="node()[not(self::c:param or following-sibling::c:param)]"/>
                  </call-template>
               </code:block>
            </if>
         </code:method>

         <variable name="hiding-meta" select="
            $meta/following-sibling::xcst:*[xcst:homonymous(., $meta) and not(@accepted/xs:boolean(.))][1]"/>

         <if test="not($abstract) and $hiding-meta">
            <code:method name="{src:hidden-function-delegate-method-name($meta)}" line-hidden="true">
               <code:type-reference name="{if ($meta/code:type-reference) then 'Func' else 'Action'}" namespace="System">
                  <code:type-arguments>
                     <sequence select="
                        for $x in ($meta/xcst:param, $meta)
                        return $x/code:type-reference"/>
                  </code:type-arguments>
               </code:type-reference>
               <code:attributes>
                  <call-template name="src:editor-browsable-never"/>
               </code:attributes>
               <code:block>
                  <code:return>
                     <code:method-reference name="{$meta/@member-name}">
                        <code:this-reference/>
                     </code:method-reference>
                  </code:return>
               </code:block>
            </code:method>
         </if>
      </if>
   </template>

   <template match="c:attribute-set" mode="src:member">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <variable name="meta" select="$package-manifest/xcst:attribute-set[@declaration-id eq current()/generate-id()]"/>
      <variable name="public" select="$meta/@visibility = ('public', 'final', 'abstract')"/>
      <variable name="abstract" select="$meta/@visibility eq 'abstract'"/>
      <variable name="context" select="src:template-context($meta)"/>
      <variable name="output" select="src:template-output($meta)"/>

      <if test="not($meta/@visibility eq 'hidden' and $meta/@declared-visibility eq 'abstract')">
         <code:method name="{$meta/@member-name}"
               visibility="{('public'[$public], 'private')[1]}"
               extensibility="{('virtual'[$meta/@visibility eq 'public'], 'abstract'[$abstract], '#default')[1]}">
            <code:attributes>
               <if test="$public">
                  <variable name="qname" select="xcst:EQName($meta/@name)"/>
                  <code:attribute>
                     <sequence select="src:package-model-type('XcstAttributeSet')"/>
                     <code:initializer>
                        <code:member-initializer name="Name">
                           <code:string verbatim="true">
                              <value-of select="xcst:uri-qualified-name($qname)"/>
                           </code:string>
                        </code:member-initializer>
                     </code:initializer>
                  </code:attribute>
               </if>
               <call-template name="src:editor-browsable-never"/>
            </code:attributes>
            <code:parameters>
               <code:parameter name="{$context/src:reference/code:*/@name}">
                  <sequence select="$context/code:type-reference"/>
               </code:parameter>
               <code:parameter name="{$output/src:reference/code:*/@name}">
                  <sequence select="$output/code:type-reference"/>
               </code:parameter>
            </code:parameters>
            <if test="not($abstract)">
               <code:block>
                  <call-template name="src:use-attribute-sets">
                     <with-param name="attr" select="@use-attribute-sets"/>
                     <with-param name="context" select="$context" tunnel="yes"/>
                     <with-param name="output" select="$output" tunnel="yes"/>
                  </call-template>
                  <call-template name="src:sequence-constructor">
                     <with-param name="children" select="c:attribute"/>
                     <with-param name="context" select="$context" tunnel="yes"/>
                     <with-param name="output" select="$output" tunnel="yes"/>
                  </call-template>
               </code:block>
            </if>
         </code:method>
      </if>
   </template>

   <template match="c:type" mode="src:member">
      <param name="modules" tunnel="yes"/>
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <variable name="meta" select="$package-manifest/xcst:type[@declaration-id eq current()/generate-id()]"/>
      <variable name="public" select="$meta/@visibility = ('public', 'final', 'abstract')"/>

      <if test="$meta/@visibility ne 'hidden'">

         <variable name="validation-declarations" select="
            for $m in reverse($modules) 
            return reverse($m/c:validation)"/>

         <variable name="validation-attributes" as="attribute()*">
            <for-each-group select="for $v in $validation-declarations return $v/@*" group-by="node-name(.)">
               <sequence select="."/>
            </for-each-group>
         </variable>

         <code:type name="{$meta/@member-name}"
               visibility="{('public'[$public], 'private')[1]}"
               extensibility="{('sealed'[$meta/@visibility eq 'final'], '#default')[1]}">
            <if test="$meta/@member-name-was-escaped/xs:boolean(.)">
               <attribute name="verbatim" select="true()"/>
            </if>
            <call-template name="src:line-number"/>
            <code:attributes>
               <if test="$public">
                  <code:attribute>
                     <sequence select="src:package-model-type('XcstType')"/>
                  </code:attribute>
               </if>
               <call-template name="src:type-attributes"/>
               <call-template name="src:type-attribute-extra"/>
               <apply-templates select="c:metadata" mode="src:attribute"/>
            </code:attributes>
            <code:members>
               <apply-templates select="c:member" mode="#current">
                  <with-param name="src:validation-attributes" select="$validation-attributes" tunnel="yes"/>
               </apply-templates>
            </code:members>
         </code:type>

         <apply-templates select="c:member[c:member]" mode="src:anonymous-type">
            <with-param name="src:validation-attributes" select="$validation-attributes" tunnel="yes"/>
         </apply-templates>
      </if>
   </template>

   <template match="c:member" mode="src:member">

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'as', 'value', 'expression', 'auto-initialize', 'display', 'display-name', 'description', 'short-name', 'edit-hint', 'order', 'group', 'format', 'apply-format-in-edit-mode', 'disable-output-escaping', 'null-display-text', 'template', 'data-type', 'required', 'max-length', 'min-length', 'pattern', 'min', 'max', 'equal-to', $xcst:type-or-member-attributes, $xcst:validation-or-member-attributes"/>
      </call-template>

      <call-template name="xcst:validate-children">
         <with-param name="allowed" select="'metadata', 'member'"/>
      </call-template>

      <if test="@as and c:member">
         <sequence select="error((), 'The ''as'' attribute must be omitted when the member has child members.', src:error-object(.))"/>
      </if>

      <if test="count((@value, @expression)) gt 1">
         <sequence select="error((), 'The attributes ''value'' and ''expression'' are mutually exclusive.', src:error-object(.))"/>
      </if>

      <call-template name="xcst:no-other-following"/>

      <variable name="auto-init" select="(@auto-initialize/xcst:boolean(.), false())[1]"/>

      <if test="$auto-init and (@expression or @value)">
         <sequence select="error(xs:QName('err:XTSE0020'), 'When auto-initialize=''yes'' the ''expression'' and ''value'' attributes must be omitted.', src:error-object(@auto-initialize))"/>
      </if>

      <variable name="type" as="element()">
         <choose>
            <when test="c:member">
               <code:type-reference name="{src:anonymous-type-name(.)}"/>
            </when>
            <when test="@as">
               <code:type-reference name="{xcst:type(@as)}"/>
            </when>
            <otherwise>
               <sequence select="$src:object-type"/>
            </otherwise>
         </choose>
      </variable>

      <code:property name="{xcst:name(@name)}" visibility="public">
         <call-template name="src:line-number"/>
         <sequence select="$type"/>
         <code:attributes>
            <call-template name="src:member-attributes"/>
            <call-template name="src:member-attribute-extra"/>
            <apply-templates select="c:metadata" mode="src:attribute"/>
         </code:attributes>
         <code:getter>
            <if test="@expression">
               <code:block>
                  <code:return>
                     <code:expression value="{@expression}"/>
                  </code:return>
               </code:block>
            </if>
         </code:getter>
         <if test="not(@expression)">
            <code:setter/>
         </if>
         <choose>
            <when test="$auto-init">
               <code:expression>
                  <code:new-object>
                     <sequence select="$type"/>
                  </code:new-object>
               </code:expression>
            </when>
            <when test="@value">
               <code:expression value="{@value}"/>
            </when>
         </choose>
      </code:property>
   </template>

   <template match="c:member[c:member]" mode="src:anonymous-type">
      <code:type name="{src:anonymous-type-name(.)}" visibility="public">
         <call-template name="src:line-number"/>
         <code:attributes>
            <call-template name="src:type-attributes"/>
            <call-template name="src:type-attribute-extra"/>
         </code:attributes>
         <code:members>
            <apply-templates select="c:member" mode="src:member"/>
            <apply-templates select="c:member[c:member]" mode="#current"/>
         </code:members>
      </code:type>
   </template>

   <function name="src:anonymous-type-name" as="xs:string">
      <param name="member" as="element(c:member)"/>

      <sequence select="concat($member/@name/xcst:name(.), '_', generate-id($member))"/>
   </function>

   <function name="src:backing-field" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="src:aux-variable(concat('back_', $meta/@name))"/>
   </function>

   <function name="src:hidden-function-delegate-method-name" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="concat($meta/@member-name, '_del')"/>
   </function>

   <!--
      ### Infrastructure
   -->

   <template name="src:constructor">
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="used-packages" tunnel="yes"/>

      <if test="$used-packages">
         <code:constructor visibility="public" line-hidden="true">
            <code:block>
               <for-each select="$used-packages">
                  <variable name="overridden" select="src:overridden-components(., $package-manifest)"/>
                  <code:assign>
                     <code:field-reference name="{src:used-package-field-name(.)}">
                        <code:this-reference/>
                     </code:field-reference>
                     <code:new-object>
                        <code:type-reference name="{src:used-package-class-name(.)}"/>
                        <code:initializer>
                           <for-each select="$overridden">
                              <variable name="meta" select="$package-manifest/xcst:*[@overrides eq current()/@id and @visibility ne 'hidden']"/>
                              <code:member-initializer name="{src:overriding-field-name(.)}">
                                 <apply-templates select="." mode="src:used-package-overriding-value">
                                    <with-param name="meta" select="$meta"/>
                                 </apply-templates>
                              </code:member-initializer>
                           </for-each>
                        </code:initializer>
                     </code:new-object>
                  </code:assign>
               </for-each>
            </code:block>
         </code:constructor>
      </if>
   </template>

   <template match="xcst:variable | xcst:param" mode="src:used-package-overriding-value">
      <param name="meta" as="element()"/>

      <variable name="member-ref" as="element()">
         <code:property-reference name="{$meta/@member-name}">
            <code:this-reference/>
         </code:property-reference>
      </variable>

      <code:method-call name="{src:tuple-member-name(.)}">
         <code:type-reference name="{src:used-package-class-name(.)}"/>
         <code:arguments>
            <code:lambda>
               <sequence select="$member-ref"/>
            </code:lambda>
            <variable name="param" select="src:aux-variable(generate-id())"/>
            <code:lambda void="true">
               <code:parameters>
                  <code:parameter name="{$param}"/>
               </code:parameters>
               <code:assign>
                  <sequence select="$member-ref"/>
                  <code:variable-reference name="{$param}"/>
               </code:assign>
            </code:lambda>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="xcst:template" mode="src:used-package-overriding-value">
      <param name="meta" as="element()"/>

      <variable name="overriding-typed-params" select="xcst:typed-params($meta)"/>
      <variable name="overridden-typed-params" select="xcst:typed-params(.)"/>

      <choose>
         <when test="$overriding-typed-params or $overridden-typed-params">
            <variable name="context" select="src:template-context($meta)"/>
            <variable name="output" select="src:template-output($meta)"/>
            <code:lambda void="true">
               <code:parameters>
                  <code:parameter name="{$context/src:reference/code:*/@name}"/>
                  <code:parameter name="{$output/src:reference/code:*/@name}"/>
               </code:parameters>
               <code:method-call name="{$meta/@member-name}">
                  <code:this-reference/>
                  <code:arguments>
                     <choose>
                        <when test="$overriding-typed-params">
                           <code:method-call name="CreateTyped">
                              <sequence select="src:template-context(())/code:type-reference"/>
                              <code:arguments>
                                 <code:cast>
                                    <sequence select="src:params-type($meta)"/>
                                    <code:property-reference name="Parameters">
                                       <sequence select="$context/src:reference/code:*"/>
                                    </code:property-reference>
                                 </code:cast>
                                 <code:int value="0"/>
                                 <sequence select="$context/src:reference/code:*"/>
                              </code:arguments>
                           </code:method-call>
                        </when>
                        <otherwise>
                           <sequence select="$context/src:reference/code:*"/>
                        </otherwise>
                     </choose>
                     <sequence select="$output/src:reference/code:*"/>
                  </code:arguments>
               </code:method-call>
            </code:lambda>
         </when>
         <otherwise>
            <code:method-reference name="{$meta/@member-name}">
               <code:this-reference/>
            </code:method-reference>
         </otherwise>
      </choose>
   </template>

   <template match="xcst:attribute-set | xcst:function" mode="src:used-package-overriding-value">
      <param name="meta" as="element()"/>

      <code:method-reference name="{$meta/@member-name}">
         <code:this-reference/>
      </code:method-reference>
   </template>

   <template name="src:execution-context">
      <param name="used-packages" tunnel="yes"/>

      <code:field name="{$src:context-field/src:reference/code:field-reference/@name}">
         <sequence select="$src:context-field/code:type-reference"/>
      </code:field>

      <code:property name="Context" visibility="private">
         <sequence select="$src:context-field/code:type-reference"/>
         <code:implements-interface>
            <sequence select="$src:package-interface"/>
         </code:implements-interface>
         <code:attributes>
            <call-template name="src:editor-browsable-never"/>
         </code:attributes>
         <code:getter>
            <code:block>
               <code:return>
                  <sequence select="$src:context-field/src:reference/code:*"/>
               </code:return>
            </code:block>
         </code:getter>
         <code:setter>
            <code:block>
               <for-each select="$used-packages">
                  <code:assign>
                     <code:property-reference name="Context">
                        <code:cast>
                           <sequence select="$src:package-interface"/>
                           <code:field-reference name="{src:used-package-field-name(.)}">
                              <code:this-reference/>
                           </code:field-reference>
                        </code:cast>
                     </code:property-reference>
                     <code:setter-value/>
                  </code:assign>
               </for-each>
               <code:assign>
                  <sequence select="$src:context-field/src:reference/code:*"/>
                  <code:setter-value/>
               </code:assign>
            </code:block>
         </code:setter>
      </code:property>
   </template>

   <template name="src:prime-method">
      <param name="principal-module" as="xs:boolean"/>
      <param name="modules" tunnel="yes"/>
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="used-packages" tunnel="yes"/>

      <variable name="context" as="element()">
         <src:context>
            <sequence select="src:package-model-type('PrimingContext')"/>
            <src:reference>
               <code:variable-reference name="{src:aux-variable('context')}"/>
            </src:reference>
         </src:context>
      </variable>

      <variable name="overridden-params" as="element()">
         <code:variable-reference name="overriddenParams"/>
      </variable>

      <variable name="priming-params" select="
         src:priming-parameters(., $package-manifest)" as="element(c:param)*"/>

      <if test="$principal-module or exists($priming-params)">
         <code:method visibility="private">
            <choose>
               <when test="$principal-module">
                  <attribute name="name" select="'Prime'"/>
                  <code:implements-interface>
                     <sequence select="$src:package-interface"/>
                  </code:implements-interface>
               </when>
               <otherwise>
                  <attribute name="name" select="src:prime-method-name(.)"/>
                  <code:attributes>
                     <call-template name="src:editor-browsable-never"/>
                  </code:attributes>
               </otherwise>
            </choose>
            <code:parameters>
               <code:parameter name="{$context/src:reference/code:*/@name}">
                  <sequence select="$context/code:type-reference"/>
               </code:parameter>
               <code:parameter name="{$overridden-params/@name}">
                  <code:type-reference array-dimensions="1">
                     <code:type-reference name="String" namespace="System"/>
                  </code:type-reference>
               </code:parameter>
            </code:parameters>
            <code:block>
               <if test="$principal-module">
                  <for-each select="$used-packages">
                     <variable name="overridden" select="
                        src:overridden-components(., $package-manifest)[self::xcst:param and xs:boolean(@required)]"/>

                     <code:method-call name="Prime">
                        <code:cast>
                           <sequence select="$src:package-interface"/>
                           <code:field-reference name="{src:used-package-field-name(.)}">
                              <code:this-reference/>
                           </code:field-reference>
                        </code:cast>
                        <code:arguments>
                           <sequence select="$context/src:reference/code:*"/>
                           <choose>
                              <when test="$overridden">
                                 <code:new-array>
                                    <code:collection-initializer>
                                       <for-each select="$overridden">
                                          <code:string literal="true">
                                             <value-of select="@name"/>
                                          </code:string>
                                       </for-each>
                                    </code:collection-initializer>
                                 </code:new-array>
                              </when>
                              <otherwise>
                                 <code:null/>
                              </otherwise>
                           </choose>
                        </code:arguments>
                     </code:method-call>
                  </for-each>
                  <for-each select="$modules[position() ne last()][exists(src:priming-parameters(., $package-manifest))]">
                     <code:method-call name="{src:prime-method-name(.)}">
                        <code:this-reference/>
                        <code:arguments>
                           <sequence select="$context/src:reference/code:*, $overridden-params"/>
                        </code:arguments>
                     </code:method-call>
                  </for-each>
               </if>
               <for-each select="$priming-params">
                  <variable name="meta" select="$package-manifest/xcst:*[@declaration-id eq current()/generate-id()]"/>
                  <code:if>
                     <code:or-else>
                        <code:equal>
                           <sequence select="$overridden-params"/>
                           <code:null/>
                        </code:equal>
                        <code:equal>
                           <code:method-call name="IndexOf">
                              <code:type-reference name="Array" namespace="System"/>
                              <code:arguments>
                                 <sequence select="$overridden-params"/>
                                 <code:string literal="true">
                                    <value-of select="$meta/@name"/>
                                 </code:string>
                              </code:arguments>
                           </code:method-call>
                           <code:int value="-1"/>
                        </code:equal>
                     </code:or-else>
                     <code:block>
                        <apply-templates select="." mode="src:statement">
                           <with-param name="context" select="$context" tunnel="yes"/>
                        </apply-templates>
                     </code:block>
                  </code:if>
               </for-each>
            </code:block>
         </code:method>
      </if>
   </template>

   <function name="src:prime-method-name" as="xs:string">
      <param name="module" as="element()"/>

      <sequence select="concat(src:aux-variable('prime'), '_', generate-id($module))"/>
   </function>

   <function name="src:priming-parameters" as="element(c:param)*">
      <param name="module" as="element()"/>
      <param name="package-manifest" as="element(xcst:package-manifest)"/>

      <for-each select="($module, $module/c:use-package/c:override)/c:param">
         <variable name="meta" select="$package-manifest/xcst:*[@declaration-id eq current()/generate-id()]"/>
         <if test="$meta/@visibility ne 'hidden' and $meta/xs:boolean(@required)">
            <sequence select="."/>
         </if>
      </for-each>
   </function>

   <template name="src:get-template-method">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <variable name="name-param" as="element()">
         <code:variable-reference name="{src:aux-variable('name')}"/>
      </variable>

      <variable name="tbase-param" as="element()">
         <code:parameter name="TBase"/>
      </variable>

      <variable name="output" as="element()">
         <src:output kind="obj">
            <code:type-reference>
               <copy-of select="src:package-model-type('ISequenceWriter')/@*"/>
               <code:type-arguments>
                  <code:type-reference name="{$tbase-param/@name}"/>
               </code:type-arguments>
            </code:type-reference>
            <src:reference>
               <code:variable-reference name="{src:aux-variable('output')}"/>
            </src:reference>
         </src:output>
      </variable>

      <code:method name="GetTemplate" visibility="private">
         <code:type-reference name="Action" namespace="System">
            <code:type-arguments>
               <sequence select="src:template-context(())/code:type-reference"/>
            </code:type-arguments>
         </code:type-reference>
         <code:implements-interface>
            <sequence select="$src:package-interface"/>
         </code:implements-interface>
         <code:type-parameters>
            <sequence select="$tbase-param"/>
         </code:type-parameters>
         <code:parameters>
            <code:parameter name="{$name-param/@name}">
               <code:type-reference name="QualifiedName" namespace="Xcst"/>
            </code:parameter>
            <code:parameter name="{$output/src:reference/code:*/@name}">
               <sequence select="$output/code:type-reference"/>
            </code:parameter>
         </code:parameters>
         <code:block>

            <variable name="templates" select="
               $package-manifest/xcst:template[@visibility = ('public', 'final')]"/>

            <variable name="unknown-throw" as="element()">
               <code:throw>
                  <code:method-call name="UnknownTemplate">
                     <sequence select="src:helper-type('DynamicError')"/>
                     <code:arguments>
                        <sequence select="$name-param"/>
                     </code:arguments>
                  </code:method-call>
               </code:throw>
            </variable>

            <code:switch>
               <code:property-reference name="Namespace">
                  <sequence select="$name-param"/>
               </code:property-reference>
               <for-each-group select="$templates" group-by="namespace-uri-from-QName(xcst:EQName(@name))">
                  <sort select="xcst:is-reserved-namespace(current-grouping-key())" order="descending"/>
                  <sort select="count(current-group())" order="descending"/>

                  <code:case>
                     <code:string verbatim="true">
                        <value-of select="current-grouping-key()"/>
                     </code:string>
                     <code:switch>
                        <code:property-reference name="Name">
                           <sequence select="$name-param"/>
                        </code:property-reference>
                        <for-each select="current-group()">
                           <variable name="qname" select="xcst:EQName(@name)"/>
                           <variable name="context" select="src:template-context(())"/>

                           <code:case>
                              <code:string literal="true">
                                 <value-of select="local-name-from-QName($qname)"/>
                              </code:string>
                              <code:return>
                                 <code:lambda void="true">
                                    <code:parameters>
                                       <code:parameter name="{$context/src:reference/code:*/@name}"/>
                                    </code:parameters>
                                    <code:method-call name="{@member-name}">
                                       <code:this-reference/>
                                       <code:arguments>
                                          <choose>
                                             <when test="xcst:typed-params(.)">
                                                <code:method-call name="CreateTyped">
                                                   <sequence select="src:template-context(())/code:type-reference"/>
                                                   <code:arguments>
                                                      <code:method-call name="Create">
                                                         <sequence select="src:params-type(.)"/>
                                                         <code:arguments>
                                                            <sequence select="$context/src:reference/code:*"/>
                                                         </code:arguments>
                                                      </code:method-call>
                                                      <code:int value="0"/>
                                                      <sequence select="$context/src:reference/code:*"/>
                                                   </code:arguments>
                                                </code:method-call>
                                             </when>
                                             <otherwise>
                                                <sequence select="$context/src:reference/code:*"/>
                                             </otherwise>
                                          </choose>
                                          <call-template name="src:call-template-output">
                                             <with-param name="meta" select="."/>
                                             <with-param name="dynamic" select="true()"/>
                                             <with-param name="output" select="$output" tunnel="yes"/>
                                          </call-template>
                                       </code:arguments>
                                    </code:method-call>
                                 </code:lambda>
                              </code:return>
                           </code:case>
                        </for-each>
                        <code:case-default>
                           <sequence select="$unknown-throw"/>
                        </code:case-default>
                     </code:switch>
                  </code:case>
               </for-each-group>
               <code:case-default>
                  <sequence select="$unknown-throw"/>
               </code:case-default>
            </code:switch>
         </code:block>
      </code:method>
   </template>

   <template name="src:read-output-definition-method">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <variable name="name-param" as="element()">
         <code:variable-reference name="{src:aux-variable('name')}"/>
      </variable>

      <variable name="parameters-param" as="element()">
         <code:variable-reference name="{src:aux-variable('params')}"/>
      </variable>

      <code:method name="ReadOutputDefinition" visibility="private">
         <code:implements-interface>
            <sequence select="$src:package-interface"/>
         </code:implements-interface>
         <code:parameters>
            <code:parameter name="{$name-param/@name}">
               <code:type-reference name="QualifiedName" namespace="Xcst"/>
            </code:parameter>
            <code:parameter name="{$parameters-param/@name}">
               <code:type-reference name="OutputParameters" namespace="Xcst"/>
            </code:parameter>
         </code:parameters>
         <code:block>
            <variable name="outputs" select="$package-manifest/xcst:output"/>
            <variable name="unnamed" select="$outputs[not(@name)]"/>

            <code:if-else>
               <code:if>
                  <code:equal>
                     <sequence select="$name-param"/>
                     <code:null/>
                  </code:equal>
                  <code:block>
                     <if test="$unnamed">
                        <code:method-call name="{$unnamed/@member-name}">
                           <code:this-reference/>
                           <code:arguments>
                              <sequence select="$parameters-param"/>
                           </code:arguments>
                        </code:method-call>
                     </if>
                  </code:block>
               </code:if>
               <code:else>

                  <variable name="unknown-throw" as="element()">
                     <code:throw>
                        <code:method-call name="UnknownOutputDefinition">
                           <sequence select="src:helper-type('DynamicError')"/>
                           <code:arguments>
                              <sequence select="$name-param"/>
                           </code:arguments>
                        </code:method-call>
                     </code:throw>
                  </variable>

                  <code:switch>
                     <code:property-reference name="Namespace">
                        <sequence select="$name-param"/>
                     </code:property-reference>
                     <for-each-group select="$outputs[@name]" group-by="namespace-uri-from-QName(xcst:EQName(@name))">
                        <sort select="count(current-group())" order="descending"/>

                        <code:case>
                           <code:string verbatim="true">
                              <value-of select="current-grouping-key()"/>
                           </code:string>
                           <code:switch>
                              <code:property-reference name="Name">
                                 <sequence select="$name-param"/>
                              </code:property-reference>
                              <for-each select="current-group()">
                                 <variable name="qname" select="xcst:EQName(@name)"/>

                                 <code:case>
                                    <code:string literal="true">
                                       <value-of select="local-name-from-QName($qname)"/>
                                    </code:string>
                                    <code:method-call name="{@member-name}">
                                       <code:this-reference/>
                                       <code:arguments>
                                          <sequence select="$parameters-param"/>
                                       </code:arguments>
                                    </code:method-call>
                                    <code:break/>
                                 </code:case>
                              </for-each>
                              <code:case-default>
                                 <sequence select="$unknown-throw"/>
                              </code:case-default>
                           </code:switch>
                           <code:break/>
                        </code:case>
                     </for-each-group>
                     <code:case-default>
                        <sequence select="$unknown-throw"/>
                     </code:case-default>
                  </code:switch>
               </code:else>
            </code:if-else>
         </code:block>
      </code:method>
   </template>

   <template match="xcst:output" mode="src:member">
      <param name="modules" tunnel="yes"/>

      <variable name="parameters-param" as="element()">
         <code:variable-reference name="{src:aux-variable('params')}"/>
      </variable>

      <variable name="declarations" select="
         for $id in tokenize(@declaration-ids, '\s')
         return $modules/c:output[generate-id() eq $id]"/>

      <code:method name="{@member-name}" visibility="private">
         <code:attributes>
            <call-template name="src:editor-browsable-never"/>
         </code:attributes>
         <code:parameters>
            <code:parameter name="{$parameters-param/@name}">
               <code:type-reference name="OutputParameters" namespace="Xcst"/>
            </code:parameter>
         </code:parameters>
         <code:block>
            <for-each-group select="for $o in $declarations return $o/(@* except @name)" group-by="node-name(.)">
               <variable name="expr" as="element()?">
                  <apply-templates select="." mode="src:output-parameter">
                     <with-param name="merged-list" as="xs:QName*">
                        <if test="self::attribute(cdata-section-elements)
                              or self::attribute(suppress-indentation)">
                           <sequence select="distinct-values(
                              for $p in current-group()
                              return for $s in tokenize($p, '\s')[.]
                              return xcst:EQName($p, $s, true()))"/>
                        </if>
                     </with-param>
                  </apply-templates>
               </variable>
               <if test="$expr">
                  <code:assign>
                     <choose>
                        <when test="namespace-uri()">
                           <code:indexer-reference>
                              <sequence select="$parameters-param"/>
                              <code:arguments>
                                 <call-template name="src:QName">
                                    <with-param name="qname" select="node-name(.)"/>
                                 </call-template>
                              </code:arguments>
                           </code:indexer-reference>
                        </when>
                        <otherwise>
                           <code:property-reference name="{src:output-parameter-property(.)}">
                              <sequence select="$parameters-param"/>
                           </code:property-reference>
                        </otherwise>
                     </choose>
                     <sequence select="$expr"/>
                  </code:assign>
               </if>
            </for-each-group>
         </code:block>
      </code:method>
   </template>

</stylesheet>
