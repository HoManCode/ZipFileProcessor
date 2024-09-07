namespace ZipFileProcessor.Services.Validator;

public interface IXmlValidator
{
    bool ValidateXml(string xmlFilePath, string xsdFilePath);
}