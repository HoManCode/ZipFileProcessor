namespace ZipFileProcessor.Services.Validator;

public interface IValidator
{
    bool Validate(string filePath, string xsdFilePath);
}