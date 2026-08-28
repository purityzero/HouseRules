using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

public static class ProjectVerificationCommands
{
    private const string TABLE_ROOT = "Assets/Resources/Table";

    [CliCommand("verify-compile", "Fail unless the Unity Editor is idle and the latest script compilation succeeded")]
    public static object VerifyCompile()
    {
        if (EditorApplication.isCompiling == true)
            throw new InvalidOperationException("Unity Editor is still compiling scripts.");

        if (EditorUtility.scriptCompilationFailed == true)
            throw new InvalidOperationException("The latest Unity script compilation failed. Check the Editor console.");

        return new
        {
            success = true,
            compiling = false,
            message = "Latest Unity script compilation succeeded."
        };
    }

    [CliCommand("verify-tables", "Validate every Resources/Table CSV against its matching Record type")]
    public static object VerifyTables()
    {
        List<string> errors = new List<string>();
        string[] assetGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { TABLE_ROOT });
        List<string> csvPaths = assetGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        if (csvPaths.Count == 0)
            throw new InvalidOperationException($"No CSV table files found under {TABLE_ROOT}.");

        Dictionary<string, Type> recordTypes = TypeCache.GetTypesDerivedFrom<Record>()
            .Where(type => type.IsAbstract == false)
            .GroupBy(type => type.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        for (int i = 0; i < csvPaths.Count; ++i)
            ValidateTable(csvPaths[i], recordTypes, errors);

        if (errors.Count > 0)
            throw new InvalidOperationException("Table verification failed:\n" + string.Join("\n", errors));

        return new
        {
            success = true,
            tableCount = csvPaths.Count,
            message = $"Validated {csvPaths.Count} CSV table(s)."
        };
    }

    private static void ValidateTable(string _assetPath, Dictionary<string, Type> _recordTypes, List<string> _errors)
    {
        string tableName = Path.GetFileNameWithoutExtension(_assetPath);
        string recordName = tableName.EndsWith("Table", StringComparison.Ordinal)
            ? tableName.Substring(0, tableName.Length - "Table".Length) + "Record"
            : tableName + "Record";

        if (_recordTypes.TryGetValue(recordName, out Type recordType) == false)
        {
            _errors.Add($"[{tableName}] Matching record type '{recordName}' was not found.");
            return;
        }

        TextAsset csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(_assetPath);
        if (csvAsset == null)
        {
            _errors.Add($"[{tableName}] Failed to load {_assetPath} as TextAsset.");
            return;
        }

        string[] lines = csvAsset.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            _errors.Add($"[{tableName}] CSV is empty.");
            return;
        }

        string[] headers = lines[0].Split(',');
        if (headers.Length > 0)
            headers[0] = headers[0].TrimStart('\uFEFF');

        Dictionary<string, FieldInfo> fields = recordType
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(field => field.Name, field => field, StringComparer.OrdinalIgnoreCase);
        HashSet<string> seenHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int column = 0; column < headers.Length; ++column)
        {
            string header = headers[column];
            if (string.IsNullOrWhiteSpace(header) == true)
            {
                _errors.Add($"[{tableName}] Header column {column + 1} is empty.");
                continue;
            }

            if (seenHeaders.Add(header) == false)
                _errors.Add($"[{tableName}] Header '{header}' is duplicated.");

            if (fields.TryGetValue(header, out FieldInfo field) == false)
            {
                _errors.Add($"[{tableName}] Header '{header}' has no public field on {recordName}.");
                continue;
            }

            if (string.Equals(header, field.Name, StringComparison.Ordinal) == false)
                _errors.Add($"[{tableName}] Header '{header}' must match field casing '{field.Name}'.");
        }

        FieldInfo[] declaredFields = recordType.GetFields(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        for (int i = 0; i < declaredFields.Length; ++i)
        {
            if (seenHeaders.Contains(declaredFields[i].Name) == false)
                _errors.Add($"[{tableName}] Field '{declaredFields[i].Name}' is missing from the CSV header.");
        }

        for (int row = 1; row < lines.Length; ++row)
        {
            string[] values = lines[row].Split(',');
            if (values.Length != headers.Length)
            {
                _errors.Add($"[{tableName}] Row {row + 1} has {values.Length} value(s); expected {headers.Length}.");
                continue;
            }

            for (int column = 0; column < headers.Length; ++column)
            {
                if (fields.TryGetValue(headers[column], out FieldInfo field) == false)
                    continue;

                if (CanConvert(values[column], field.FieldType) == false)
                {
                    _errors.Add(
                        $"[{tableName}] Row {row + 1}, column '{headers[column]}' value " +
                        $"'{values[column]}' cannot convert to {field.FieldType.Name}.");
                }
            }
        }
    }

    private static bool CanConvert(string _value, Type _fieldType)
    {
        if (_fieldType == typeof(string))
            return true;

        try
        {
            if (_fieldType.IsEnum == true)
            {
                object parsed = Enum.Parse(_fieldType, _value, true);
                return Enum.IsDefined(_fieldType, parsed);
            }

            if (_fieldType == typeof(float))
            {
                float.Parse(_value, NumberStyles.Float, CultureInfo.InvariantCulture);
                return true;
            }

            Convert.ChangeType(_value, _fieldType, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
