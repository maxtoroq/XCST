<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

   <xsl:param name="projectName" required="yes"/>

   <xsl:strip-space elements="*"/>

   <xsl:output method="xml" indent="yes" cdata-section-elements="license"/>

   <xsl:template match="@* | node()">
      <xsl:copy>
         <xsl:apply-templates select="@* | node()"/>
      </xsl:copy>
   </xsl:template>

   <xsl:template match="/notice/license[@src]/@url">
      <!-- no need for URL when the license is included -->
   </xsl:template>

   <xsl:template match="/notice/foreign-work[@type = 'source']">
      <!-- only include notices for dependencies used by the project -->
      <xsl:if test="used-by-source[string-join(tokenize(@path, '/')[.], '/') = concat('src/', $projectName)]">
         <xsl:next-match/>
      </xsl:if>
   </xsl:template>

   <xsl:template match="foreign-work[@type = 'object']">
      <!-- referenced packages should include own notices -->
   </xsl:template>

   <xsl:template match="foreign-work/notice/license[@src]">
      <!-- include license -->
      <xsl:copy>
         <xsl:apply-templates select="@* except (@src, @url)"/>
         <xsl:value-of select="unparsed-text(resolve-uri(@src, base-uri()))"/>
      </xsl:copy>
   </xsl:template>

   <xsl:template match="foreign-work/used-by-source"/>

</xsl:stylesheet>
