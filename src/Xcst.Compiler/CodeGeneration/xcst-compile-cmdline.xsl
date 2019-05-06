<?xml version="1.0" encoding="utf-8"?>
<!--
 Copyright 2019 Max Toro Q.

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
<stylesheet version="2.0"
   xmlns="http://www.w3.org/1999/XSL/Transform"
   xmlns:xs="http://www.w3.org/2001/XMLSchema"
   xmlns:src="http://maxtoroq.github.io/XCST/compiled">

   <!--
      This file is used to compile from the command line.
      It wouldn't be needed if Saxon implemented xsl:function/@override="no"
      See <https://saxonica.plan.io/issues/4111>
   -->

   <include href="xcst-compile.xsl"/>

   <function name="src:_package-manifest" as="document-node()?">
      <param name="p1" as="xs:string"/>
      <param name="p2" as="item()?"/>
      <param name="p3" as="item()+"/>

      <sequence select="error()"/>
   </function>

   <function name="src:_package-location" as="xs:anyURI?">
      <param name="p1" as="xs:string"/>
      <param name="p2" as="item()?"/>
      <param name="p3" as="xs:anyURI?"/>
      <param name="p4" as="xs:string?"/>
      <param name="p5" as="xs:string?"/>

      <sequence select="error()"/>
   </function>

   <function name="src:_doc-with-uris" as="item()+">
      <param name="p1" as="xs:anyURI"/>
      <param name="p2" as="item()+"/>
      <param name="p3" as="item()?"/>

      <sequence select="error()"/>
   </function>

   <function name="src:_qname-id" as="xs:integer">
      <param name="p1" as="xs:QName"/>

      <sequence select="error()"/>
   </function>

   <function name="src:_line-number" as="xs:integer">
      <param name="p1" as="node()"/>

      <sequence select="error()"/>
   </function>

   <function name="src:_local-path" as="xs:string">
      <param name="p1" as="xs:anyURI"/>

      <sequence select="error()"/>
   </function>

   <function name="src:_make-relative-uri" as="xs:anyURI">
      <param name="p1" as="xs:anyURI"/>
      <param name="p2" as="xs:anyURI"/>

      <sequence select="error()"/>
   </function>

</stylesheet>
