﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arkivverket.Arkade.Core.Addml.Definitions.DataTypes;
using Arkivverket.Arkade.ExternalModels.Addml;
using Arkivverket.Arkade.Util;
using System.Text;

namespace Arkivverket.Arkade.Core.Addml.Definitions
{
    // TODO: This class should be split into different parsers for different parts of the ADDML. E.g. AddmlFieldDefinitionParser AddmlRecordDefinitionParser, etc
    public class AddmlDefinitionParser
    {
        private readonly AddmlInfo _addmlInfo;
        private readonly WorkingDirectory _workingDirectory;

        private readonly Dictionary<string, flatFileType> _flatFileTypes = new Dictionary<string, flatFileType>();
        private readonly Dictionary<string, DataType> _fieldTypes = new Dictionary<string, DataType>();

        private readonly Dictionary<PropertyIndex, string> _flatFileProperties = new Dictionary<PropertyIndex, string>();



        private readonly Dictionary<FieldIndex, AddmlFieldDefinition> _allFieldDefinitions =
            new Dictionary<FieldIndex, AddmlFieldDefinition>();

        public AddmlDefinitionParser(AddmlInfo addmlInfo, WorkingDirectory workingDirectory)
        {
            Assert.AssertNotNull("addmlInfo", addmlInfo);
            _addmlInfo = addmlInfo;
            _workingDirectory = workingDirectory;

            PopulateFlatFileTypes();
            PopulateFieldTypes();
            PopulateFlatFileProperties();
        }

        private void PopulateFlatFileProperties()
        {
            foreach (flatFile flatFile in GetFlatFiles())
            {
                PopulateFlatFileProperties(flatFile.definitionReference, new List<string>(), flatFile.properties);
            }
        }

        private void PopulateFlatFileProperties(string definitionReference, List<string> propertyName, property[] properties)
        {
            if (properties == null)
            {
                return;
            }

            foreach (property property in properties)
            {
                var l = new List<string>(propertyName);
                l.Add(property.name);

                if (property.value != null)
                {
                    _flatFileProperties.Add(new PropertyIndex(definitionReference, l), property.value);
                }

                PopulateFlatFileProperties(definitionReference, l, property.properties);
            }
        }

        private void PopulateFieldTypes()
        {
            fieldType[] fieldTypes = GetFieldTypes();
            if (fieldTypes != null)
            {
                foreach (fieldType fieldType in fieldTypes)
                {
                    DataType newDataType = CreateFieldType(fieldType);
                    if (newDataType != null)
                    {
                        _fieldTypes.Add(fieldType.name, newDataType);
                    }
                }
            }
        }

        private DataType CreateFieldType(fieldType fieldType)
        {
            string format = fieldType.fieldFormat;
            List<string> nullValues = fieldType.nullValues == null ? null : new List<string>(fieldType.nullValues);

            string dataType = fieldType.dataType;
            if (dataType == null)
            {
                return null;
            }

            switch (dataType.ToLower())
            {

                case "string":
                    return new StringDataType(format, nullValues);
                case "integer":
                    return new IntegerDataType(format, nullValues);
                case "float":
                    return new FloatDataType(format, nullValues);
                case "date":
                    return new DateDataType(format, nullValues);
                case "boolean":
                    return new BooleanDataType(format, nullValues);
                case "link":
                    return new LinkDataType();
                default:
                    throw new AddmlDefinitionParseException("Unknown datatype " + fieldType.dataType);
            }
        }

        private fieldType[] GetFieldTypes()
        {
            return GetDataset()?.flatFiles?.structureTypes?.fieldTypes;
        }

        private void PopulateFlatFileTypes()
        {
            flatFileType[] flatFileTypes = GetFlatFileTypes();
            if (flatFileTypes != null)
            {
                foreach (flatFileType flatFileType in flatFileTypes)
                {
                    _flatFileTypes.Add(flatFileType.name, flatFileType);
                }
            }
        }

        private flatFileType[] GetFlatFileTypes()
        {
            return GetDataset()?.flatFiles?.structureTypes?.flatFileTypes;
        }

        public AddmlDefinition GetAddmlDefinition()
        {
            List<AddmlFlatFileDefinition> addmlFlatFileDefinitions = GetAddmlFlatFileDefinitions();
            return new AddmlDefinition(addmlFlatFileDefinitions);
        }

        private List<AddmlFlatFileDefinition> GetAddmlFlatFileDefinitions()
        {
            List<AddmlFlatFileDefinition> addmlFlatFileDefinitions = new List<AddmlFlatFileDefinition>();

            flatFileDefinition[] flatFileDefinitions = GetFlatFileDefinitions();
            foreach (flatFileDefinition flatFileDefinition in flatFileDefinitions)
            {
                string name = flatFileDefinition.name;
                string recordSeparator = GetRecordSeparator(flatFileDefinition.typeReference);
                string fieldSeparator = GetFieldSeparator(flatFileDefinition.typeReference);
                string fileName = GetFileName(flatFileDefinition.name);
                FileInfo fileInfo = _workingDirectory.Content().WithFile(fileName);
                string charset = GetCharset(flatFileDefinition.typeReference);
                string recordDefinitionFieldIdentifier = flatFileDefinition.recordDefinitionFieldIdentifier;
                int? numberOfRecords = GetNumberOfRecords(flatFileDefinition.name);
                Checksum checksum = GetChecksum(flatFileDefinition.name);
                AddmlFlatFileFormat format = GetFlatFileFormat(flatFileDefinition.typeReference);
                List<string> flatFileProcesses = GetFlatFileProcessNames(flatFileDefinition.name);

                AddmlFlatFileDefinition addmlFlatFileDefinition =
                    new AddmlFlatFileDefinition(name, fileName, fileInfo, recordSeparator, fieldSeparator, charset,
                        recordDefinitionFieldIdentifier, numberOfRecords, checksum, format, flatFileProcesses);

                AddAddmlFieldDefinitions(addmlFlatFileDefinition, flatFileDefinition);

                addmlFlatFileDefinitions.Add(addmlFlatFileDefinition);
            }

            SetForeginKeyReferences(addmlFlatFileDefinitions);

            return addmlFlatFileDefinitions;
        }

        private Checksum GetChecksum(string definitionReference)
        {
            string algorithm = GetProperty(definitionReference, "checksum", "algorithm");
            string value = GetProperty(definitionReference, "checksum", "value");

            if (algorithm == null || value == null)
            {
                return null;
            }

            return new Checksum(algorithm, value);
        }

        private void SetForeginKeyReferences(List<AddmlFlatFileDefinition> addmlFlatFileDefinitions)
        {
            foreach (AddmlFlatFileDefinition fileDefinitions in addmlFlatFileDefinitions)
            {
                foreach (AddmlRecordDefinition recordDefinition in fileDefinitions.AddmlRecordDefinitions)
                {
                    foreach (AddmlFieldDefinition fieldDefinition in recordDefinition.AddmlFieldDefinitions)
                    {
                        FieldIndex index = fieldDefinition.ForeignKeyIndex;

                        if (index != null) {
                            if (!_allFieldDefinitions.ContainsKey(index))
                            {
                                throw new AddmlDefinitionParseException("Could not find foreign reference " + index);
                            }

                            fieldDefinition.ForeignKey = _allFieldDefinitions[index];
                        }
                    }
                }
            }



        }

        private AddmlFlatFileFormat GetFlatFileFormat(string flatFileTypeName)
        {
            flatFileType flatFileType = GetFlatFileType(flatFileTypeName);
            Type type = flatFileType.Item.GetType();
            if (type == typeof(fixedFileFormat))
            {
                return AddmlFlatFileFormat.Fixed;
            }
            else if (type == typeof(delimFileFormat))
            {
                return AddmlFlatFileFormat.Delimiter;
            }
            else
            {
                throw new AddmlDefinitionParseException("Unkown flatFileType: " + type);
            }
        }

        private List<string> GetFlatFileProcessNames(string flatFileDefinitionName)
        {
            List<string> ret = new List<string>();
            flatFileProcesses p = GetFlatFileProcesses(flatFileDefinitionName);
            if (p?.processes != null)
            {
                foreach (process process in p.processes)
                {
                    ret.Add(process.name);
                }
            }
            return ret;
        }

        private flatFileProcesses GetFlatFileProcesses(string flatFileDefinitionName)
        {
            flatFileProcesses[] flatFileProcesses = GetDataset()?.flatFiles?.flatFileProcesses;
            if (flatFileProcesses == null)
            {
                return null;
            }

            foreach (flatFileProcesses p in flatFileProcesses)
            {
                if (flatFileDefinitionName.Equals(p.flatFileReference))
                {
                    return p;
                }
            }

            return null;
        }

        private List<string> GetFieldProcessNames(string flatFileDefinitionName, string recordDefinitionName,
            string fieldDefinitionName)
        {
            flatFileProcesses flatFileProcesses = GetFlatFileProcesses(flatFileDefinitionName);
            recordProcesses recordProcesses = GetRecordProcesses(flatFileProcesses, recordDefinitionName);
            fieldProcesses fieldProcesses = GetFieldProcesses(recordProcesses, fieldDefinitionName);

            return fieldProcesses?.processes?.Select(p => p.name).ToList() ?? new List<string>();
        }

        private List<string> GetRecordProcessNames(string flatFileDefinitionName, string recordDefinitionName)
        {
            flatFileProcesses flatFileProcesses = GetFlatFileProcesses(flatFileDefinitionName);
            recordProcesses recordProcesses = GetRecordProcesses(flatFileProcesses, recordDefinitionName);

            return recordProcesses?.processes?.Select(p => p.name).ToList() ?? new List<string>();
        }

        private fieldProcesses GetFieldProcesses(recordProcesses recordProcesses, string fieldDefinitionName)
        {
            if (recordProcesses?.fieldProcesses != null)
            {
                foreach (fieldProcesses p in recordProcesses.fieldProcesses)
                {
                    if (p.definitionReference.Equals(fieldDefinitionName))
                    {
                        return p;
                    }
                }
            }
            return null;
        }

        private recordProcesses GetRecordProcesses(flatFileProcesses flatFileProcesses, string recordDefinitionName)
        {
            if (flatFileProcesses?.recordProcesses != null)
            {
                foreach (recordProcesses p in flatFileProcesses.recordProcesses)
                {
                    if (p.definitionReference.Equals(recordDefinitionName))
                    {
                        return p;
                    }
                }
            }
            return null;
        }


        private void AddAddmlFieldDefinitions(AddmlFlatFileDefinition addmlFlatFileDefinition,
            flatFileDefinition flatFileDefinition)
        {
            List<recordDefinition> recordDefinitions = GetRecordDefinitions(flatFileDefinition);
            foreach (recordDefinition recordDefinition in recordDefinitions)
            {
                string recordDefinitionName = recordDefinition.name;
                int? recordLength = GetRecordLength(recordDefinition);
                string recordDefinitionFieldValue = recordDefinition.recordDefinitionFieldValue;
                List<string> recordProcesses = GetRecordProcessNames(addmlFlatFileDefinition.Name, recordDefinition.name);

                AddmlRecordDefinition addmlRecordDefinition = 
                    addmlFlatFileDefinition.AddAddmlRecordDefinition(recordDefinitionName, recordLength, recordDefinitionFieldValue, recordProcesses);

                List<fieldDefinition> fieldDefinitions = GetFieldDefinitions(recordDefinition);
                foreach (fieldDefinition fieldDefinition in fieldDefinitions)
                {
                    string name = fieldDefinition.name;
                    int? startPosition = GetStartPosition(fieldDefinition);
                    int? fixedLength = GetFixedLength(fieldDefinition);
                    DataType dataType = GetFieldType(fieldDefinition.typeReference);
                    bool isPartOfPrimaryKey = IsPartOfPrimaryKey(recordDefinition, fieldDefinition);
                    bool isUnique = IsUnique(fieldDefinition);
                    bool isNullable = IsNullable(fieldDefinition);
                    int? minLength = GetMinLength(fieldDefinition);
                    int? maxLength = GetMaxLength(fieldDefinition);
                    FieldIndex foreignKeyIndex = GetForeignKeyIndex(recordDefinition, fieldDefinition);
                    List<string> processes = GetFieldProcessNames(flatFileDefinition.name, recordDefinition.name,
                        fieldDefinition.name);
                    List<AddmlCode> addmlCodes = GetCodes(fieldDefinition);

                    AddmlFieldDefinition addAddmlFieldDefinition = addmlRecordDefinition.AddAddmlFieldDefinition(
                        name, startPosition, fixedLength, dataType, isUnique, isNullable, minLength,
                        maxLength, foreignKeyIndex, processes, addmlCodes, isPartOfPrimaryKey);

                    FieldIndex fieldIndex = new FieldIndex(flatFileDefinition, recordDefinition, fieldDefinition);
                    if (_allFieldDefinitions.ContainsKey(fieldIndex))
                    {
                        throw new AddmlDefinitionParseException("ADDML file already contains a field definition with same index: " + fieldIndex);
                    }
                    _allFieldDefinitions.Add(fieldIndex, addAddmlFieldDefinition);
                }
            }
        }

        private List<AddmlCode> GetCodes(fieldDefinition fieldDefinition)
        {
            code[] codes = fieldDefinition.codes;
            if (codes == null)
            {
                return null;
            }

            List<AddmlCode> addmlCodes = new List<AddmlCode>();
            foreach (code c in codes)
            {
                addmlCodes.Add(new AddmlCode(c.codeValue, c.explan));
            }
            return addmlCodes;
        }

        private int? GetMaxLength(fieldDefinition fieldDefinition)
        {
            return fieldDefinition.maxLength == null ? (int?) null : int.Parse(fieldDefinition.maxLength);
        }

        private int? GetFixedLength(fieldDefinition fieldDefinition)
        {
            return fieldDefinition.fixedLength == null ? (int?) null : int.Parse(fieldDefinition.fixedLength);
        }

        private int? GetStartPosition(fieldDefinition fieldDefinition)
        {
            return fieldDefinition.startPos == null ? (int?) null : int.Parse(fieldDefinition.startPos);
        }

        private int? GetMinLength(fieldDefinition fieldDefinition)
        {
            return fieldDefinition.minLength == null ? (int?) null : int.Parse(fieldDefinition.minLength);
        }


        private bool IsNullable(fieldDefinition fieldDefinition)
        {
            return fieldDefinition.notNull == null;
        }

        private bool IsUnique(fieldDefinition fieldDefinition)
        {
            return fieldDefinition.unique != null;
        }

        private List<fieldDefinition> GetFieldDefinitions(recordDefinition recordDefinition)
        {
            return new List<fieldDefinition>(recordDefinition.fieldDefinitions);
        }

        private string GetCharset(string flatFileTypeName)
        {
            return GetFlatFileType(flatFileTypeName).charset;
        }

        private string GetRecordSeparator(string flatFileTypeName)
        {
            flatFileType flatFileType = GetFlatFileType(flatFileTypeName);
            Type type = flatFileType.Item.GetType();
            if (type == typeof(fixedFileFormat))
            {
                return ((fixedFileFormat)flatFileType.Item).recordSeparator;
            } else if (type == typeof(delimFileFormat))
            {
                return ((delimFileFormat) flatFileType.Item).recordSeparator;
            }
            else
            {
                throw new AddmlDefinitionParseException("Unkown flatFileType: " + type);
            }
        }

        private string GetFieldSeparator(string flatFileTypeName)
        {
            flatFileType flatFileType = GetFlatFileType(flatFileTypeName);
            Type type = flatFileType.Item.GetType();
            if (type == typeof(delimFileFormat))
            {
                return ((delimFileFormat) flatFileType.Item).fieldSeparatingChar;
            }
            else
            {
                return null;
            }
        }


        private flatFileType GetFlatFileType(string flatFileTypeName)
        {
            flatFileType flatFileType = _flatFileTypes[flatFileTypeName];

            if (flatFileType == null)
            {
                throw new AddmlDefinitionParseException("No flatFileType with name " + flatFileTypeName);
            }

            return flatFileType;
        }

        private DataType GetFieldType(string typeReference)
        {
            DataType dataType = _fieldTypes[typeReference];

            if (dataType == null)
            {
                throw new AddmlDefinitionParseException("No FieldType with name " + typeReference);
            }

            return dataType;
        }


        private int? GetRecordLength(recordDefinition recordDefinition)
        {
            return recordDefinition.fixedLength == null ? (int?)null : int.Parse(recordDefinition.fixedLength);
        }

        private FieldIndex GetForeignKeyIndex(recordDefinition recordDefinition,
            fieldDefinition fieldDefinition)
        {
            key[] keys = recordDefinition.keys;
            if (keys != null)
            {
                foreach (key key in keys)
                {
                    fieldDefinitionReference[] keyFieldDefinitionReferences = key.fieldDefinitionReferences;
                    foreach (fieldDefinitionReference fieldDefinitionReference in keyFieldDefinitionReferences)
                    {
                        if (fieldDefinitionReference.name.Equals(fieldDefinition.name))
                        {
                            object o = key.Item;
                            if (o is foreignKey)
                            {
                                foreignKey f = (foreignKey) o;
                                List<FieldIndex> indexes = GetForeignKeyIndexes(f);

                                // TODO: Is it possible to have reference to more than one AddmlFieldDefinition?
                                if (indexes.Count != 1)
                                {
                                    throw new AddmlDefinitionParseException(
                                        "foreignKey must reference exactly one fieldDefinitionReference. " + f);
                                }

                                FieldIndex index = indexes[0];

                                // Må puttes på til slutt!
                                return index;

                                /*
                                if (!_allFieldDefinitions.ContainsKey(index))
                                {
                                    return null;
                                }

                                return _allFieldDefinitions[index];
                                */
                            }
                        }
                    }
                }
            }

            return null;
        }

        private List<FieldIndex> GetForeignKeyIndexes(foreignKey foreignKey)
        {
            List<FieldIndex> indexes = new List<FieldIndex>();

            flatFileDefinitionReference flatFileDefinitionReference = foreignKey.flatFileDefinitionReference;
            foreach (
                recordDefinitionReference recordDefinitionReference in
                flatFileDefinitionReference.recordDefinitionReferences)
            {
                foreach (
                    fieldDefinitionReference fieldDefinitionReference in
                    recordDefinitionReference.fieldDefinitionReferences)
                {
                    indexes.Add(new FieldIndex(flatFileDefinitionReference, recordDefinitionReference,
                        fieldDefinitionReference));
                }
            }

            return indexes;
        }


        private bool IsPartOfPrimaryKey(recordDefinition recordDefinition, fieldDefinition fieldDefinition)
        {
            key[] keys = recordDefinition.keys;
            if (keys != null)
            {
                foreach (key key in keys)
                {
                    fieldDefinitionReference[] keyFieldDefinitionReferences = key.fieldDefinitionReferences;
                    foreach (fieldDefinitionReference fieldDefinitionReference in keyFieldDefinitionReferences)
                    {
                        if (fieldDefinitionReference.name.Equals(fieldDefinition.name))
                        {
                            object o = key.Item;
                            if (o is primaryKey)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private List<recordDefinition> GetRecordDefinitions(flatFileDefinition flatFileDefinition)
        {
            recordDefinition[] recordDefinitions = flatFileDefinition.recordDefinitions;
            Assert.AssertNotNull("recordDefinitions", recordDefinitions);

            return new List<recordDefinition>(recordDefinitions);
        }


        private string GetFileName(string definitionReference)
        {
            return GetProperty(definitionReference, "fileName");
        }

        private int? GetNumberOfRecords(string definitionReference)
        {
            string numberOfRecords = GetProperty(definitionReference, "numberOfRecords");
            return StringUtil.ToInt(numberOfRecords);
        }

        private string GetProperty(string definitionReference, params string[] propertyName)
        {
            PropertyIndex propertyIndex = new PropertyIndex(definitionReference, propertyName);

            if (_flatFileProperties.ContainsKey(propertyIndex))
            {
                return _flatFileProperties[propertyIndex];
            } else
            {
                return null;
            }
        }

        private flatFile[] GetFlatFiles()
        {
            var flatFiles = GetDataset().flatFiles;
            Assert.AssertNotNull("flatFiles", flatFiles);
            return flatFiles.flatFile;
        }

        private flatFileDefinition[] GetFlatFileDefinitions()
        {
            var flatFiles = GetDataset().flatFiles;
            Assert.AssertNotNull("flatFiles", flatFiles);

            var flatFileDefinitions = flatFiles.flatFileDefinitions;
            Assert.AssertNotNull("flatFileDefinitions", flatFileDefinitions);

            return flatFileDefinitions;
        }

        private dataset GetDataset()
        {
            dataset[] datasets = _addmlInfo.Addml.dataset;
            Assert.AssertNotNull("dataset", datasets);

            if (datasets.Length != 1)
            {
                throw new AddmlDefinitionParseException("dataset must contain exactly one element. Found " +
                                                        datasets.Length +
                                                        " elements");
            }

            return datasets[0];
        }
    }

    internal class PropertyIndex
    {
        private readonly string _definitionReference;
        private readonly List<string> _propertyNames;

        internal PropertyIndex(string definitionReference, List<string> propertyNames)
        {
            _definitionReference = definitionReference;
            _propertyNames = propertyNames;
        }

        internal PropertyIndex(string definitionReference, params string[] propertyNames)
        {
            _definitionReference = definitionReference;
            _propertyNames = new List<string>(propertyNames);
        }

        protected bool Equals(PropertyIndex other)
        {
            return _definitionReference == other._definitionReference
                && _propertyNames.SequenceEqual(other._propertyNames);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PropertyIndex)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hc = _definitionReference.GetHashCode();
                foreach (string pn in _propertyNames)
                {
                    hc = hc * 17 + pn.GetHashCode();
                }
                return hc;
            }
        }

        public override string ToString()
        {
            return _definitionReference + "/" + string.Join("/", _propertyNames);
        }

    }
}