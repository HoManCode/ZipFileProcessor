# ZIP File Processor Console App

## Overview
The ZIP File Processor Console App is a C# application designed to process and validate ZIP files. It checks if the ZIP files are valid by confirming they contain only specific file types and a structured party.XML file. If the ZIP file is valid, it extracts the files to a designated folder and sends a notification to the administrator. If invalid, an error notification is sent.

The application is configurable and logs all operations for troubleshooting and auditing purposes.

## Features

- Validates ZIP files: Ensures the ZIP contains only XLS, XLSX, DOC, DOCX, PDF, MSG, and image files, along with an XML file (party.XML) conforming to a provided XSD schema.
- Extracts files: Extracts valid files to a designated folder with the naming convention [applicationno]-[guid].
- Email Notifications: Sends an email notification to an administrator when processing completes or when an error occurs.
- Configurable: Settings such as administrator email, file paths, and supported file types can be customized via appsettings.json.
- Logging: Logs all actions, including success, errors, and detailed file processing information.