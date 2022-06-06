using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Sylvan.Data.Csv;

namespace Fast.ML.WebApp.Utils;

public static class FileUtils
{
    private const string UploadsDirectoryName = "upload";

    private static readonly HashSet<string> ExcelFileExtensions = new()
    {
        ".xlsx"
    };

    private const string PostfixSearchPattern = "-*.*";

    public static string GetUserFolder(string webRootPath, int userId) => 
        Path.Combine(webRootPath, UploadsDirectoryName, $"{userId:d10}");
    
    public static string GetFilePath(string path, string fileName) => 
        Path.Combine(path, fileName);

    public static string GetFileNamePrefix(string sessionId, DateTime dateTime) =>
        $"{sessionId}-{DateTimeUtils.GetDateString(dateTime)}";

    public static string GetFileExtension(string fileName) => 
        Path.GetExtension(fileName);

    public static bool IsExcelFile(string fileName) => 
        ExcelFileExtensions.Contains(GetFileExtension(fileName));

    public static string GetServerFileName(
        string fileNamePrefix, 
        string fileExtension,
        bool isPrediction = false) => 
        $"{fileNamePrefix}-{Convert.ToInt32(isPrediction)}{fileExtension}";

    public static FileInfo[] FindSessionFiles(
        string webRootPath, 
        int userId, 
        string prefix) =>
        new DirectoryInfo(GetUserFolder(webRootPath, userId))
            .GetFiles(prefix + PostfixSearchPattern)
            .OrderBy(file => file.Name)
            .ToArray();
    
    public static string[] GetHeaderFromCsvFile(string filePath, char delimiter, int index)
    {
        using var textReader = new StreamReader(filePath);
        var csvReaderOptions = new CsvDataReaderOptions
        {
            Delimiter = delimiter
        };

        var csvReader = CsvDataReader.Create(textReader, csvReaderOptions);
        return csvReader.GetColumnSchema()
            .Select(column => column.ColumnName).ToArray()[index..];
    }

    public static string[] GetHeaderFromExcelFile(string filePath, int index)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();
        var firstRow = worksheet.Rows().First().Cells();

        return firstRow
            .Select(column => column.CachedValue.ToString()).ToArray()[index..];
    }
    
    public static Dictionary<string, string> LoadAlgorithmsFromJsonFile(
        string webRootPath, 
        string fileName)
    {
        var filePath = GetFilePath(webRootPath, fileName);
        var jsonLines = File.ReadAllLines(filePath);

        var algorithmsSettings = new Dictionary<string, string>();

        foreach (var jsonLine in jsonLines)
        {
            var name = System.Text.Json
                .JsonDocument.Parse(jsonLine).RootElement.GetProperty("name").GetString();
            var settings = System.Text.Json
                .JsonDocument.Parse(jsonLine).RootElement.GetProperty("settings").ToString();
            algorithmsSettings.Add(name!, settings);
        }

        return algorithmsSettings;
    }
}