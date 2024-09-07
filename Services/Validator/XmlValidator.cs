
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

        try
        {
            if (!File.Exists(schemaFilePath)) throw new FileNotFoundException($"XSD file '{schemaFilePath}' not found.");
            
            var schema = new XmlSchemaSet();
            schema.Add(null, schemaFilePath);
            
            var settings = new XmlReaderSettings();
            settings.Schemas.Add(schema);
            settings.ValidationType = ValidationType.Schema;
            
            settings.ValidationEventHandler += (sender, e) =>
            {
                isValid = false;
                Log.Error("file did not find error: {e.Message}", e.Message);
            };
            
        }
        catch (XmlSchemaException ex)
        {
            Log.Error("Schema error: {ex.Message}", ex.Message);
            return false;
        }
        catch (FileNotFoundException ex)
        {
            Log.Error("file did not find error: {ex.Message}", ex.Message);
            return false;
        }
        catch (XmlException ex)
        {
            Log.Error("Xml error: {ex.Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error("Unexpected error: {ex.Message}", ex.Message);
            return false;
        }

        return isValid;
    }
}