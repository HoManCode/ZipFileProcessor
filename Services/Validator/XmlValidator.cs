
using System.Xml;
using System.Xml.Schema;
using Microsoft.Extensions.Logging;

namespace ZipFileProcessor.Services.Validator;

public class XmlValidator : IValidator
{
    private readonly ILogger<XmlValidator> _logger;
    
    public XmlValidator(ILogger<XmlValidator> logger)
    {
        _logger = logger;
    }
    public bool Validate(string xmlFilePath, string schemaFilePath)
    {
        bool isValid = true;

        try
        {
            if (!File.Exists(xmlFilePath)) throw new FileNotFoundException($"XML file '{xmlFilePath}' not found.");
            if (!File.Exists(schemaFilePath)) throw new FileNotFoundException($"XSD file '{schemaFilePath}' not found.");
            
            var schema = new XmlSchemaSet();
            schema.Add(null, schemaFilePath);
            
            var settings = new XmlReaderSettings();
            settings.Schemas.Add(schema);
            settings.ValidationType = ValidationType.Schema;
            
            settings.ValidationEventHandler += (sender, e) =>
            {
                isValid = false;
                _logger.LogError("file did not find error: {e.Message}", e.Message);
            };

            using var reader = XmlReader.Create(xmlFilePath, settings);
            while (reader.Read()) { }
        }
        catch (XmlSchemaException ex)
        {
            _logger.LogError("Schema error: {ex.Message}", ex.Message);
            return false;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError("file did not find error: {ex.Message}", ex.Message);
            return false;
        }
        catch (XmlException ex)
        {
            _logger.LogError("Xml error: {ex.Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError("Unexpected error: {ex.Message}", ex.Message);
            return false;
        }

        return isValid;
    }
}