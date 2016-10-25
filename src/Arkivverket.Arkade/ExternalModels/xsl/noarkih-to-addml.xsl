<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:in="NOARKIH"
    xmlns:out="addml"
    version="1.0"
    xmlns="http://www.arkivverket.no/standarder/addml">

<!--
        Denne transformering skjer fra en NoarkIH.xml og gjør denne om til en addml.xml.
        
        Foreløpig er det følgende kjente feil:
            dataType i structureTypes er hardkodet i dag og alle elementene er angitt som string. Test relatert til Noark-4 del 2 mangler.
            Muligens mangler det også noe i definisjonen
        
-->
    <xsl:output method="xml" version="1.0"
        encoding="UTF-8" indent="yes"/>

    <xsl:template match="/">
        <xsl:apply-templates/>
    </xsl:template>
    
    <xsl:template match="NOARK.IH">
        <addml 
            xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
            xsi:schemaLocation="http://www.arkivverket.no/standarder/addml addml.xsd">
            <dataset>
                <reference>
                        <xsl:apply-templates select="EKSPORTINFO"/>
                </reference>
                <flatFiles>
                    <xsl:apply-templates select="TABELLINFO"/>
                    <xsl:for-each select="TABELLINFO">
                        <xsl:apply-templates select="ATTRIBUTTER"/>
                    </xsl:for-each>
                </flatFiles>
                <structureTypes>
                    <flatFileTypes>
                        <flatFileType name="filref"/>
                    </flatFileTypes>
                    <recordTypes>
                        <recordType name="recref"/>
                    </recordTypes>
                    <fieldTypes>
                        <fieldType name="string">
                            <dataType>string</dataType>
                        </fieldType>
                        <fieldType name="integer">
                            <dataType>intger</dataType>
                        </fieldType>
                        <fieldType name="float">
                            <dataType>float</dataType>
                        </fieldType>
                        <fieldType name="date">
                            <dataType>date</dataType>
                        </fieldType>
                        <fieldType name="boolean">
                            <dataType>boolean</dataType>
                        </fieldType>
                    </fieldTypes>
                </structureTypes>
<!--            <processes>
                    Her skal det legges inn for de prosessene som skal utføres.
                </processes>
-->
            </dataset>
        </addml>
    </xsl:template>
    
    <xsl:template match="EKSPORTINFO">
        <context>
            <xsl:choose>
            <xsl:when test="EI.ARKSKAPER">
                <additionalElements>
                    <additionalElement name="agents">
                        <additionalElements>
                            <additionalElement name="agent">
                                <additionalElements>
                                    <additionalElement name="role">
                                        <value>recordCreator</value>
                                    </additionalElement>
                                    <additionalElement name="type">
                                        <value>institution</value>
                                    </additionalElement>
                                    <additionalElement name="value">
                                        <value><xsl:value-of select="EI.ARKSKAPER"/></value>
                                    </additionalElement>
                                </additionalElements>
                            </additionalElement>
                        </additionalElements>
                    </additionalElement>
                </additionalElements>
            </xsl:when>
            <xsl:when test="EI.SYSTEMNAVN">
                <additionalElements>
                    <additionalElement name="system">
                        <additionalElements>
                            <additionalElement name="systemType">
                                <value>Noark 4</value>
                            </additionalElement>
                            <additionalElement name="systemNavn">
                                <value><xsl:value-of select="EI.SYSTEMNAVN"/></value>
                            </additionalElement>
                        </additionalElements>
                    </additionalElement>
                </additionalElements>
            </xsl:when>
        </xsl:choose>
        </context>
        <content>
            <xsl:choose>
            <xsl:when test="EI.FRADATO">
                <additionalElements>
                    <additionalElement name="archivalPeriod">
                        <additionalElements>
                            <additionalElement name="startDate">
                                <value><xsl:value-of select="EI.FRADATO"/></value>
                            </additionalElement>
                            <additionalElement name="date">
                                <value><xsl:value-of select="EI.TILDATO"/></value>
                            </additionalElement>
                            <additionalElement name="type">
                                <value>Noark 4</value>
                            </additionalElement>
                        </additionalElements>
                    </additionalElement>
                </additionalElements>
            </xsl:when>
            <xsl:when test="EI.PRODDATO">
                <additionalElements>
                    <additionalElement name="archivalDataset">
                        <additionalElements>
                            <additionalElement name="date">
                                <value><xsl:value-of select="EI.PRODDATO"/></value>
                            </additionalElement>
                            <additionalElement name="type">
                                <value>Noark 4</value>
                            </additionalElement>
                        </additionalElements>
                    </additionalElement>
                </additionalElements>
            </xsl:when>
            <xsl:otherwise>
                <comment>Hva har skjedd nå?</comment>
            </xsl:otherwise>
        </xsl:choose>
        </content>
    </xsl:template>
    
    <xsl:template match="TABELLINFO">
        <xsl:variable name="filnavn" select="TI.TABELL"/>
        <flatFile definitionReference="{$filnavn}" name="{$filnavn}">
            <value><xsl:value-of select="TI.TABELL"/></value>
            <properties>
                <property name="fileName">
                    <value><xsl:value-of select="FIL/TI.FILNAVN"/></value>
                </property>
                <property name="numberOfOccurrences">
                    <value><xsl:value-of select="FIL/TI.ANTPOSTER"/></value>
                </property>
            </properties>
        </flatFile>
    </xsl:template>
    
    <xsl:template match="ATTRIBUTTER">
        <xsl:variable name="filnavn" select="../TI.TABELL"/>
        <flatFileDefinitions>
            <flatFileDefinition definitionReference="{$filnavn}" name="{$filnavn}">
                <recordDefinitions>
                    <recordDefinition name="{$filnavn}">
                        <fieldDefinitions>
                            <xsl:for-each select="TI.ATTR">
                                <xsl:variable name="feltnavn" select="."/>
                                <fieldDefinition name="{$feltnavn}" typeReference="string"/>
                            </xsl:for-each>
                        </fieldDefinitions>
                    </recordDefinition>
                </recordDefinitions>
            </flatFileDefinition>
        </flatFileDefinitions>
    </xsl:template>

    <xsl:template match="TI.ATTR">
        <fieldDefinitions>
            <xsl:variable name="feltnavn">
                <value><xsl:value-of select="TI.ATTR"/></value>
            </xsl:variable>
            <fieldDefinition name="{$feltnavn}" typeReference="string"/>
            <!--<xsl:variable name="datatype">
                <value><xsl:value-of select="TI.ATTR"/></value>
            </xsl:variable>
            <fieldDefinition name="{$feltnavn}" typeReference="{$datatype}"/>-->
        </fieldDefinitions>
    </xsl:template>
</xsl:stylesheet>