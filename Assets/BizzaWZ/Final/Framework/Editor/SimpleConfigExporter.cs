#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using cfg;
using UnityEditor;
using UnityEngine;

public static class SimpleConfigExporter
{
    private const string OutputRelativePath = "Assets/Game/Resources/ConfigAssets/TableBin/Table01";

    [MenuItem("工具/表格/导出简易配置表")]
    public static void ExportAll()
    {
        try
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new InvalidOperationException("Unable to resolve Unity project root.");
            }

            var dataRoot = Path.GetFullPath(Path.Combine(projectRoot, "..", "_Data", "Data"));
            var outputRoot = Path.GetFullPath(Path.Combine(projectRoot, OutputRelativePath.Replace('/', Path.DirectorySeparatorChar)));

            var languageWorkbook = ExcelWorkbook.Load(Path.Combine(dataRoot, "D-多语言.xlsx"));
            var langNumWorkbook = ExcelWorkbook.Load(Path.Combine(dataRoot, "D-多语言数字.xlsx"));
            var textureWorkbook = ExcelWorkbook.Load(Path.Combine(dataRoot, "M-网赚图标.xlsx"));
            var globalWorkbook = ExcelWorkbook.Load(Path.Combine(dataRoot, "Q-全局.xlsx"));
            var taskWorkbook = ExcelWorkbook.Load(Path.Combine(dataRoot, "R-任务表.xlsx"));

            Directory.CreateDirectory(outputRoot);
            ExportLanguage(languageWorkbook.GetSheet("LanguageConfig"), Path.Combine(outputRoot, "tbllanguage.bytes"));
            ExportLangNum(langNumWorkbook.GetSheetAt(0), Path.Combine(outputRoot, "tbllangnum.bytes"));
            ExportCommonTexture(textureWorkbook.GetSheet("Common"), Path.Combine(outputRoot, "tblcommonwztexture.bytes"));
            ExportCountryTexture(textureWorkbook.GetSheet("Country"), Path.Combine(outputRoot, "tblwzcountrytexture.bytes"));
            ExportGlobal(globalWorkbook.GetSheetAt(0), Path.Combine(outputRoot, "tblglobal.bytes"));
            ExportDailyTasks(taskWorkbook.GetSheetAt(0), Path.Combine(outputRoot, "tbldailytaskconfig.bytes"));
            ExportActivityTasks(taskWorkbook.GetSheetAt(1), Path.Combine(outputRoot, "tblactivitytaskconfig.bytes"));

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log($"[SimpleConfigExporter] Export completed. Output: {OutputRelativePath}");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("简易导表失败", exception.Message, "确定");
        }
    }

    private static void ExportLanguage(ExcelSheet sheet, string outputPath)
    {
        var languageColumns = new List<KeyValuePair<int, string>>();
        for (var column = 2; column < sheet.ColumnCount; column++)
        {
            var language = sheet.Get(2, column);
            if (!string.IsNullOrEmpty(language))
            {
                languageColumns.Add(new KeyValuePair<int, string>(column, language));
            }
        }

        var rows = new List<LanguageConfig>();
        for (var row = 4; row < sheet.RowCount; row++)
        {
            if (IsDisabledRow(sheet.Get(row, 0)))
            {
                continue;
            }

            var id = sheet.Get(row, 1);
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            var config = new LanguageConfig
            {
                Id = id,
                Dict = new Dictionary<string, string>(),
            };

            for (var i = 0; i < languageColumns.Count; i++)
            {
                var languageColumn = languageColumns[i];
                var value = sheet.Get(row, languageColumn.Key);
                if (value != null)
                {
                    config.Dict.Add(languageColumn.Value, value);
                }
            }

            rows.Add(config);
        }

        WriteTable(outputPath, "tbllanguage", writer =>
        {
            SimpleConfigBinary.WriteCount(writer, rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                SimpleConfigBinary.WriteString(writer, row.Id);
                SimpleConfigBinary.WriteCount(writer, row.Dict.Count);
                foreach (var pair in row.Dict)
                {
                    SimpleConfigBinary.WriteString(writer, pair.Key);
                    SimpleConfigBinary.WriteString(writer, pair.Value);
                }
            }
        });

        Debug.Log($"[SimpleConfigExporter] tbllanguage: {rows.Count} rows");
    }

    private static void ExportLangNum(ExcelSheet sheet, string outputPath)
    {
        var rows = new List<LangNumConfig>();
        for (var row = 4; row < sheet.RowCount; row++)
        {
            if (IsDisabledRow(sheet.Get(row, 0)) || string.IsNullOrEmpty(sheet.Get(row, 1)))
            {
                continue;
            }

            var config = new LangNumConfig
            {
                Lang = sheet.Get(row, 1),
                NumList = new List<int>(),
                TextList = new List<string>(),
            };

            for (var column = 2; column <= 4; column++)
            {
                var value = sheet.Get(row, column);
                if (!string.IsNullOrEmpty(value))
                {
                    config.NumList.Add(ParseInt(value, sheet, row, column));
                }
            }

            for (var column = 5; column <= 7; column++)
            {
                var value = sheet.Get(row, column);
                if (!string.IsNullOrEmpty(value))
                {
                    config.TextList.Add(value);
                }
            }

            rows.Add(config);
        }

        WriteTable(outputPath, "tbllangnum", writer =>
        {
            SimpleConfigBinary.WriteCount(writer, rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                SimpleConfigBinary.WriteString(writer, row.Lang);
                SimpleConfigBinary.WriteCount(writer, row.NumList.Count);
                for (var j = 0; j < row.NumList.Count; j++)
                {
                    writer.Write(row.NumList[j]);
                }

                SimpleConfigBinary.WriteCount(writer, row.TextList.Count);
                for (var j = 0; j < row.TextList.Count; j++)
                {
                    SimpleConfigBinary.WriteString(writer, row.TextList[j]);
                }
            }
        });

        Debug.Log($"[SimpleConfigExporter] tbllangnum: {rows.Count} rows");
    }

    private static void ExportCommonTexture(ExcelSheet sheet, string outputPath)
    {
        var rows = new List<TblCommonWzTextureConfig>();
        for (var row = 3; row < sheet.RowCount; row++)
        {
            if (IsDisabledRow(sheet.Get(row, 0)) || string.IsNullOrEmpty(sheet.Get(row, 1)))
            {
                continue;
            }

            rows.Add(new TblCommonWzTextureConfig
            {
                Name = sheet.Get(row, 1),
                Path = sheet.Get(row, 2),
            });
        }

        WriteTable(outputPath, "tblcommonwztexture", writer =>
        {
            SimpleConfigBinary.WriteCount(writer, rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                SimpleConfigBinary.WriteString(writer, rows[i].Name);
                SimpleConfigBinary.WriteString(writer, rows[i].Path);
            }
        });

        Debug.Log($"[SimpleConfigExporter] tblcommonwztexture: {rows.Count} rows");
    }

    private static void ExportCountryTexture(ExcelSheet sheet, string outputPath)
    {
        var rows = new List<TblWzCountryTextureConfig>();
        for (var row = 4; row < sheet.RowCount; row++)
        {
            if (IsDisabledRow(sheet.Get(row, 0)) || string.IsNullOrEmpty(sheet.Get(row, 1)))
            {
                continue;
            }

            rows.Add(new TblWzCountryTextureConfig
            {
                Name = sheet.Get(row, 1),
                BRPath = sheet.Get(row, 2),
                IDPath = sheet.Get(row, 3),
                USPath = sheet.Get(row, 4),
            });
        }

        WriteTable(outputPath, "tblwzcountrytexture", writer =>
        {
            SimpleConfigBinary.WriteCount(writer, rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                SimpleConfigBinary.WriteString(writer, rows[i].Name);
                SimpleConfigBinary.WriteString(writer, rows[i].BRPath);
                SimpleConfigBinary.WriteString(writer, rows[i].IDPath);
                SimpleConfigBinary.WriteString(writer, rows[i].USPath);
            }
        });

        Debug.Log($"[SimpleConfigExporter] tblwzcountrytexture: {rows.Count} rows");
    }

    private static void ExportGlobal(ExcelSheet sheet, string outputPath)
    {
        var languageInfos = new List<StringFloat>();
        var fieldValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var row = 2; row < sheet.RowCount; row++)
        {
            var fieldName = sheet.Get(row, 0);
            if (string.Equals(fieldName, "languageInfos", StringComparison.OrdinalIgnoreCase) ||
                (string.IsNullOrEmpty(fieldName) && languageInfos.Count > 0))
            {
                var languageValue = sheet.Get(row, 3);
                if (!string.IsNullOrEmpty(languageValue))
                {
                    languageInfos.Add(ParseStringFloat(languageValue, sheet, row, 3));
                }

                continue;
            }

            if (IsDisabledRow(fieldName) || string.IsNullOrEmpty(fieldName))
            {
                continue;
            }

            fieldValues[fieldName] = sheet.Get(row, 3);
        }

        var comboTips = new List<IntIntString>();
        foreach (var token in SplitList(GetField(fieldValues, "comboTips"), '|'))
        {
            comboTips.Add(ParseIntIntString(token, sheet, 14, 3));
        }

        var config = new GlobalConfig
        {
            LanguageInfos = languageInfos,
            ComboTips = comboTips,
            ComboCd = ParseFloat(GetField(fieldValues, "comboCd"), sheet, 15, 3),
            FlyToAreaSpeed = ParseFloat(GetField(fieldValues, "flyToAreaSpeed"), sheet, 16, 3),
            MoneyItemId = ParseInt(GetField(fieldValues, "moneyItemId"), sheet, 17, 3),
            DefaultDollarNum = ParseFloat(GetField(fieldValues, "defaultDollarNum"), sheet, 18, 3),
            AdDollarNum = ParseFloat(GetField(fieldValues, "adDollarNum"), sheet, 19, 3),
            AdDollarPoss = ParseInt(GetField(fieldValues, "adDollarPoss"), sheet, 20, 3),
            AdDollarMinCd = ParseFloat(GetField(fieldValues, "adDollarMinCd"), sheet, 21, 3),
            AdDollarMaxTimes = ParseInt(GetField(fieldValues, "adDollarMaxTimes"), sheet, 22, 3),
            MultDollarPoss = ParseInt(GetField(fieldValues, "multDollarPoss"), sheet, 23, 3),
            MultDollarRange = ParseVector2(GetField(fieldValues, "multDollarRange"), sheet, 24, 3),
            InterAdPoss = ParseInt(GetField(fieldValues, "interAdPoss"), sheet, 25, 3),
            InterAdMaxTimes = ParseInt(GetField(fieldValues, "interAdMaxTimes"), sheet, 26, 3),
            FlowDollarNum = ParseFloat(GetField(fieldValues, "flowDollarNum"), sheet, 27, 3),
            DollarRange = ParseVector2(GetField(fieldValues, "dollarRange"), sheet, 28, 3),
            CombineCoinNum = ParseInt(GetField(fieldValues, "combineCoinNum"), sheet, 29, 3),
            AdCoinNum = ParseInt(GetField(fieldValues, "adCoinNum"), sheet, 30, 3),
            FragmentDestroyDuration = ParseFloat(GetField(fieldValues, "fragmentDestroyDuration"), sheet, 31, 3),
            PropUseMaxTimes = ParseInt(GetField(fieldValues, "propUseMaxTimes"), sheet, 32, 3),
            ReviveMaxTimes = ParseInt(GetField(fieldValues, "reviveMaxTimes"), sheet, 33, 3),
        };

        WriteTable(outputPath, "tblglobal", writer =>
        {
            SimpleConfigBinary.WriteCount(writer, 1);
            SimpleConfigBinary.WriteCount(writer, config.LanguageInfos.Count);
            for (var i = 0; i < config.LanguageInfos.Count; i++)
            {
                SimpleConfigBinary.WriteString(writer, config.LanguageInfos[i].Key);
                writer.Write(config.LanguageInfos[i].FloatVal);
            }

            SimpleConfigBinary.WriteCount(writer, config.ComboTips.Count);
            for (var i = 0; i < config.ComboTips.Count; i++)
            {
                writer.Write(config.ComboTips[i].X);
                writer.Write(config.ComboTips[i].Y);
                SimpleConfigBinary.WriteString(writer, config.ComboTips[i].Id);
            }

            writer.Write(config.ComboCd);
            writer.Write(config.FlyToAreaSpeed);
            writer.Write(config.MoneyItemId);
            writer.Write(config.DefaultDollarNum);
            writer.Write(config.AdDollarNum);
            writer.Write(config.AdDollarPoss);
            writer.Write(config.AdDollarMinCd);
            writer.Write(config.AdDollarMaxTimes);
            writer.Write(config.MultDollarPoss);
            WriteVector2(writer, config.MultDollarRange);
            writer.Write(config.InterAdPoss);
            writer.Write(config.InterAdMaxTimes);
            writer.Write(config.FlowDollarNum);
            WriteVector2(writer, config.DollarRange);
            writer.Write(config.CombineCoinNum);
            writer.Write(config.AdCoinNum);
            writer.Write(config.FragmentDestroyDuration);
            writer.Write(config.PropUseMaxTimes);
            writer.Write(config.ReviveMaxTimes);
        });

        Debug.Log("[SimpleConfigExporter] tblglobal: 1 row");
    }

    private static void ExportDailyTasks(ExcelSheet sheet, string outputPath)
    {
        var rows = new List<DailyTaskConfig>();
        for (var row = 4; row < sheet.RowCount; row++)
        {
            if (IsDisabledRow(sheet.Get(row, 0)) || string.IsNullOrEmpty(sheet.Get(row, 1)))
            {
                continue;
            }

            rows.Add(new DailyTaskConfig
            {
                Id = sheet.Get(row, 1),
                RefreshDays = ParseInt(sheet.Get(row, 3), sheet, row, 3),
                Description = sheet.Get(row, 4),
                CompleteConditions = ParseEnum<E_AllTaskType>(sheet.Get(row, 5), sheet, row, 5),
                ConditionValues = ParseInt(sheet.Get(row, 6), sheet, row, 6),
                RewardsList = ParseItemEntries(sheet.Get(row, 7), sheet, row, 7),
                AdsFinish = ParseBool(sheet.Get(row, 8)),
            });
        }

        WriteTable(outputPath, "tbldailytaskconfig", writer =>
        {
            SimpleConfigBinary.WriteCount(writer, rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                SimpleConfigBinary.WriteString(writer, row.Id);
                writer.Write(row.RefreshDays);
                SimpleConfigBinary.WriteString(writer, row.Description);
                writer.Write((int)row.CompleteConditions);
                writer.Write(row.ConditionValues);
                WriteItemEntries(writer, row.RewardsList);
                writer.Write(row.AdsFinish);
            }
        });

        Debug.Log($"[SimpleConfigExporter] tbldailytaskconfig: {rows.Count} rows");
    }

    private static void ExportActivityTasks(ExcelSheet sheet, string outputPath)
    {
        var rows = new List<ActivityTaskConfig>();
        for (var row = 4; row < sheet.RowCount; row++)
        {
            if (IsDisabledRow(sheet.Get(row, 0)) || string.IsNullOrEmpty(sheet.Get(row, 1)))
            {
                continue;
            }

            rows.Add(new ActivityTaskConfig
            {
                Id = sheet.Get(row, 1),
                RefreshDays = ParseInt(sheet.Get(row, 2), sheet, row, 2),
                Item = ParseEnum<E_ItemType>(sheet.Get(row, 3), sheet, row, 3),
                Number = ParseInt(sheet.Get(row, 4), sheet, row, 4),
                RewardsList = ParseItemEntries(sheet.Get(row, 5), sheet, row, 5),
            });
        }

        WriteTable(outputPath, "tblactivitytaskconfig", writer =>
        {
            SimpleConfigBinary.WriteCount(writer, rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                SimpleConfigBinary.WriteString(writer, row.Id);
                writer.Write(row.RefreshDays);
                writer.Write((int)row.Item);
                writer.Write(row.Number);
                WriteItemEntries(writer, row.RewardsList);
            }
        });

        Debug.Log($"[SimpleConfigExporter] tblactivitytaskconfig: {rows.Count} rows");
    }

    private static void WriteItemEntries(BinaryWriter writer, List<ItemEntry> rows)
    {
        SimpleConfigBinary.WriteCount(writer, rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            writer.Write((int)rows[i].Type);
            writer.Write(rows[i].Count);
        }
    }

    private static void WriteVector2(BinaryWriter writer, vector2 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
    }

    private static void WriteTable(string outputPath, string tableName, Action<BinaryWriter> writeContent)
    {
        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                SimpleConfigBinary.WriteHeader(writer, tableName);
                writeContent(writer);
            }

            File.WriteAllBytes(outputPath, stream.ToArray());
        }
    }

    private static bool IsDisabledRow(string value)
    {
        return !string.IsNullOrEmpty(value) && value.StartsWith("##", StringComparison.Ordinal);
    }

    private static string GetField(Dictionary<string, string> values, string name)
    {
        return values.TryGetValue(name, out var value) ? value : null;
    }

    private static IEnumerable<string> SplitList(string value, char separator)
    {
        if (string.IsNullOrEmpty(value))
        {
            yield break;
        }

        var values = value.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < values.Length; i++)
        {
            yield return values[i].Trim();
        }
    }

    private static List<ItemEntry> ParseItemEntries(string value, ExcelSheet sheet, int row, int column)
    {
        var result = new List<ItemEntry>();
        foreach (var token in SplitList(value, ';'))
        {
            var values = token.Split(new[] { ',' }, 2);
            if (values.Length != 2)
            {
                throw InvalidCell(sheet, row, column, $"Invalid ItemEntry: {token}");
            }

            result.Add(new ItemEntry
            {
                Type = ParseEnum<E_ItemType>(values[0].Trim(), sheet, row, column),
                Count = ParseFloat(values[1].Trim(), sheet, row, column),
            });
        }

        return result;
    }

    private static StringFloat ParseStringFloat(string value, ExcelSheet sheet, int row, int column)
    {
        var values = value.Split(new[] { ',' }, 2);
        if (values.Length != 2)
        {
            throw InvalidCell(sheet, row, column, $"Invalid StringFloat: {value}");
        }

        return new StringFloat(values[0].Trim(), ParseFloat(values[1].Trim(), sheet, row, column));
    }

    private static IntIntString ParseIntIntString(string value, ExcelSheet sheet, int row, int column)
    {
        var values = value.Split(new[] { ',' }, 3);
        if (values.Length != 3)
        {
            throw InvalidCell(sheet, row, column, $"Invalid IntIntString: {value}");
        }

        return new IntIntString(
            ParseInt(values[0].Trim(), sheet, row, column),
            ParseInt(values[1].Trim(), sheet, row, column),
            values[2].Trim());
    }

    private static vector2 ParseVector2(string value, ExcelSheet sheet, int row, int column)
    {
        var values = value.Split(new[] { ',' }, 2);
        if (values.Length != 2)
        {
            throw InvalidCell(sheet, row, column, $"Invalid vector2: {value}");
        }

        return new vector2(
            ParseFloat(values[0].Trim(), sheet, row, column),
            ParseFloat(values[1].Trim(), sheet, row, column));
    }

    private static T ParseEnum<T>(string value, ExcelSheet sheet, int row, int column) where T : struct
    {
        if (Enum.TryParse(value, ignoreCase: true, out T result))
        {
            return result;
        }

        throw InvalidCell(sheet, row, column, $"Invalid {typeof(T).Name}: {value}");
    }

    private static int ParseInt(string value, ExcelSheet sheet, int row, int column)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return Convert.ToInt32(number);
        }

        throw InvalidCell(sheet, row, column, $"Invalid int: {value}");
    }

    private static float ParseFloat(string value, ExcelSheet sheet, int row, int column)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0f;
        }

        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return number;
        }

        throw InvalidCell(sheet, row, column, $"Invalid float: {value}");
    }

    private static bool ParseBool(string value)
    {
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
    }

    private static Exception InvalidCell(ExcelSheet sheet, int row, int column, string message)
    {
        return new InvalidDataException($"{sheet.Name} cell ({row + 1},{column + 1}): {message}");
    }

    private sealed class ExcelWorkbook
    {
        private readonly Dictionary<string, ExcelSheet> sheets;
        private readonly List<ExcelSheet> orderedSheets;

        private ExcelWorkbook(Dictionary<string, ExcelSheet> sheets, List<ExcelSheet> orderedSheets)
        {
            this.sheets = sheets;
            this.orderedSheets = orderedSheets;
        }

        public static ExcelWorkbook Load(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Excel file not found.", path);
            }

            using (var archive = ZipFile.OpenRead(path))
            {
                var sharedStrings = ReadSharedStrings(archive);
                var workbook = LoadXml(archive, "xl/workbook.xml");
                var relationships = LoadXml(archive, "xl/_rels/workbook.xml.rels");
                var relationshipMap = relationships.Root
                    .Elements(XName.Get("Relationship", "http://schemas.openxmlformats.org/package/2006/relationships"))
                    .ToDictionary(
                        element => (string)element.Attribute("Id"),
                        element => NormalizeEntryPath((string)element.Attribute("Target")));

                var result = new Dictionary<string, ExcelSheet>(StringComparer.OrdinalIgnoreCase);
                var orderedResult = new List<ExcelSheet>();
                var spreadsheetNamespace = XNamespace.Get("http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                var relationshipNamespace = XNamespace.Get("http://schemas.openxmlformats.org/officeDocument/2006/relationships");
                foreach (var sheetElement in workbook.Root.Element(spreadsheetNamespace + "sheets").Elements(spreadsheetNamespace + "sheet"))
                {
                    var name = (string)sheetElement.Attribute("name");
                    var relationshipId = (string)sheetElement.Attribute(relationshipNamespace + "id");
                    var entryPath = relationshipMap[relationshipId];
                    var sheet = ReadSheet(archive, entryPath, name, sharedStrings);
                    result.Add(name, sheet);
                    orderedResult.Add(sheet);
                }

                return new ExcelWorkbook(result, orderedResult);
            }
        }

        public ExcelSheet GetSheet(string name)
        {
            if (!sheets.TryGetValue(name, out var sheet))
            {
                throw new InvalidDataException($"Excel sheet not found: {name}");
            }

            return sheet;
        }

        public ExcelSheet GetSheetAt(int index)
        {
            return orderedSheets[index];
        }

        private static Dictionary<int, string> ReadSharedStrings(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            var result = new Dictionary<int, string>();
            if (entry == null)
            {
                return result;
            }

            var document = LoadXml(entry);
            var spreadsheetNamespace = XNamespace.Get("http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            var index = 0;
            foreach (var stringItem in document.Root.Elements(spreadsheetNamespace + "si"))
            {
                result[index++] = string.Concat(stringItem.Descendants(spreadsheetNamespace + "t").Select(element => element.Value));
            }

            return result;
        }

        private static ExcelSheet ReadSheet(ZipArchive archive, string entryPath, string name, Dictionary<int, string> sharedStrings)
        {
            var document = LoadXml(archive, entryPath);
            var spreadsheetNamespace = XNamespace.Get("http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            var result = new ExcelSheet(name);
            var rowIndex = 0;
            foreach (var rowElement in document.Root.Descendants(spreadsheetNamespace + "row"))
            {
                var rowNumber = ParseReferenceNumber((string)rowElement.Attribute("r"));
                if (rowNumber <= 0)
                {
                    rowNumber = rowIndex + 1;
                }

                rowIndex = rowNumber;
                foreach (var cellElement in rowElement.Elements(spreadsheetNamespace + "c"))
                {
                    var reference = (string)cellElement.Attribute("r");
                    var column = GetColumnIndex(reference);
                    var type = (string)cellElement.Attribute("t");
                    var valueElement = cellElement.Element(spreadsheetNamespace + "v");
                    var value = valueElement?.Value;

                    if (type == "s" && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stringIndex))
                    {
                        sharedStrings.TryGetValue(stringIndex, out value);
                    }
                    else if (type == "inlineStr")
                    {
                        value = string.Concat(cellElement.Descendants(spreadsheetNamespace + "t").Select(element => element.Value));
                    }
                    else if (type == "b")
                    {
                        value = value == "1" ? "true" : "false";
                    }

                    result.Set(rowNumber - 1, column, value);
                }
            }

            return result;
        }

        private static XDocument LoadXml(ZipArchive archive, string entryPath)
        {
            var entry = archive.GetEntry(entryPath);
            if (entry == null)
            {
                throw new InvalidDataException($"Excel XML entry not found: {entryPath}");
            }

            return LoadXml(entry);
        }

        private static XDocument LoadXml(ZipArchiveEntry entry)
        {
            using (var stream = entry.Open())
            {
                return XDocument.Load(stream);
            }
        }

        private static string NormalizeEntryPath(string target)
        {
            target = (target ?? string.Empty).Replace('\\', '/').TrimStart('/');
            return target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase) ? target : "xl/" + target;
        }

        private static int GetColumnIndex(string reference)
        {
            if (string.IsNullOrEmpty(reference))
            {
                return 0;
            }

            var index = 0;
            for (var i = 0; i < reference.Length && char.IsLetter(reference[i]); i++)
            {
                index = index * 26 + (char.ToUpperInvariant(reference[i]) - 'A' + 1);
            }

            return index - 1;
        }

        private static int ParseReferenceNumber(string reference)
        {
            if (string.IsNullOrEmpty(reference))
            {
                return 0;
            }

            var index = 0;
            while (index < reference.Length && !char.IsDigit(reference[index]))
            {
                index++;
            }

            return index < reference.Length && int.TryParse(reference.Substring(index), out var value) ? value : 0;
        }
    }

    private sealed class ExcelSheet
    {
        private readonly List<List<string>> rows = new List<List<string>>();

        public ExcelSheet(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public int RowCount => rows.Count;

        public int ColumnCount => rows.Count == 0 ? 0 : rows.Max(row => row.Count);

        public string Get(int row, int column)
        {
            return row >= 0 && row < rows.Count && column >= 0 && column < rows[row].Count ? rows[row][column] : null;
        }

        public void Set(int row, int column, string value)
        {
            while (rows.Count <= row)
            {
                rows.Add(new List<string>());
            }

            while (rows[row].Count <= column)
            {
                rows[row].Add(null);
            }

            rows[row][column] = value;
        }
    }
}
#endif
