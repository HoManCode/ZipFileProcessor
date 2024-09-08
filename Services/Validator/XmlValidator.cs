
using System.Xml;
using System.Xml.Schema;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ZipFileProcessor.Services.Validator;

public class XmlValidator : IValidator
{
    public bool Validate(System.IO.Compression.ZipArchiveEntry xmlFile, string schemaFilePath)
    {
        Log.Information("********************Starting Xml Validate method********************");
        
        var isValid = true;

        
        var schema = new XmlSchemaSet();
        schema.Add(null, schemaFilePath); 

        
        var settings = new XmlReaderSettings();
        settings.ValidationType = ValidationType.Schema;
        settings.Schemas = schema;
        
        using var xmlStream = xmlFile.Open();
        using var reader = XmlReader.Create(xmlStream, settings);
        try
        {
            while (reader.Read()) { } 
            Log.Information("XML is valid.");
        }
        catch (XmlException ex)
        {
            Log.Error("XML Exception: {ex.Message}",ex.Message);
            isValid = false;
        }

        return isValid;
    }
}