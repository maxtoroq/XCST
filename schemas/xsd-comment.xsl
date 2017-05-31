<?xml version="1.0" encoding="utf-8"?>
<stylesheet version="2.0" xmlns="http://www.w3.org/1999/XSL/Transform">

   <template match="@* | node()">
      <copy>
         <apply-templates select="@* | node()"/>
      </copy>
   </template>

   <template match="/comment()">
      <text>&#xA;</text>
      <comment> Converted from Relax NG schema, using Trang. Use only with code completion tools that do not support Relax NG. </comment>
      <text>&#xA;</text>
   </template>

</stylesheet>