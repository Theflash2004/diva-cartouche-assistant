using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using AssistantArsef.Core;

namespace AssistantArsef;

internal sealed record ExcelInspection(
    string WorkbookPath,
    string SheetName,
    int HeaderRow,
    IReadOnlyDictionary<string, int> Columns,
    IReadOnlyList<string> ClasserOptions,
    int LastUsedColumn);

internal sealed record ExcelAppendResult(bool Added, bool AlreadyExists, string Message);

internal static class ExcelDocumentService
{
    private static readonly string[] RequiredColumns = ["Codification", "Document", "Domain", "Version", "Date", "Classer"];

    public static ExcelInspection Inspect(string workbookPath)
    {
        if (!File.Exists(workbookPath)) throw new FileNotFoundException("Le classeur sélectionné est introuvable.", workbookPath);
        dynamic? excel = null;
        dynamic? workbook = null;
        try
        {
            excel = StartExcel();
            workbook = excel.Workbooks.Open(workbookPath, false, true);
            var inspection = InspectWorkbook(workbook, workbookPath);
            return inspection ?? throw new InvalidOperationException(
                "Le classeur ne contient pas une feuille de registre exploitable. " +
                "Il faut au minimum les colonnes Document enregistré, Codification, Domaine, Version, Date et Lieu de classement.");
        }
        finally
        {
            CloseExcel(workbook, excel, false);
        }
    }

    public static ExcelAppendResult Append(string workbookPath, ArsefInput input, ArsefPlan plan, IReadOnlyList<string> classers)
    {
        dynamic? excel = null;
        dynamic? workbook = null;
        try
        {
            excel = StartExcel();
            workbook = excel.Workbooks.Open(workbookPath, false, false);
            ExcelInspection? inspection = InspectWorkbook(workbook, workbookPath)
                ?? throw new InvalidOperationException("Le classeur ne contient pas les colonnes nécessaires au registre.");

            var allowed = new HashSet<string>(inspection.ClasserOptions, StringComparer.OrdinalIgnoreCase);
            var selected = classers.Where(x => allowed.Contains(x.Trim())).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (selected.Length == 0)
                throw new InvalidOperationException("Sélectionnez au moins un lieu de classement existant dans la colonne « Lieu de classement ».");

            dynamic sheet = workbook.Worksheets[inspection.SheetName];
            try
            {
                if (ContainsCode(sheet, inspection, plan.Code))
                    return new ExcelAppendResult(false, true, "Cette codification existe déjà dans le registre : aucune ligne en double n'a été ajoutée.");

                var row = LastDataRow(sheet, inspection) + 1;
                CopyPreviousRowFormatting(sheet, row, inspection);
                Set(sheet, row, inspection.Columns["Codification"], plan.Code);
                Set(sheet, row, inspection.Columns["Document"], input.Title.Trim());
                Set(sheet, row, inspection.Columns["Domain"], ArsefRules.GetDomain(input.DomainCode).Label);
                Set(sheet, row, inspection.Columns["Version"], input.Version.Trim().StartsWith("v.", StringComparison.OrdinalIgnoreCase) ? input.Version.Trim() : "v." + input.Version.Trim());
                Set(sheet, row, inspection.Columns["Date"], input.ValidityDate.ToOADate());
                try { sheet.Cells[row, inspection.Columns["Date"]].NumberFormat = "dd/mm/yyyy"; } catch { }
                Set(sheet, row, inspection.Columns["Classer"], string.Join(" ; ", selected));

                if (inspection.Columns.TryGetValue("Type", out var typeColumn))
                    Set(sheet, row, typeColumn, ArsefRules.GetType(input.TypeCode).Label);
                if (inspection.Columns.TryGetValue("Author", out var authorColumn))
                    Set(sheet, row, authorColumn, input.Author.Trim());
                if (inspection.Columns.TryGetValue("Number", out var numberColumn))
                    Set(sheet, row, numberColumn, NextNumber(sheet, inspection, numberColumn));

                workbook.Save();
                return new ExcelAppendResult(true, false, "Le document a été ajouté au registre.");
            }
            finally
            {
                Release(sheet);
            }
        }
        finally
        {
            CloseExcel(workbook, excel, true);
        }
    }

    private static dynamic StartExcel()
    {
        var type = Type.GetTypeFromProgID("Excel.Application")
                   ?? throw new InvalidOperationException("Microsoft Excel n'est pas installé sur cet ordinateur.");
        dynamic excel = Activator.CreateInstance(type)!;
        excel.Visible = false;
        excel.DisplayAlerts = false;
        return excel;
    }

    private static ExcelInspection? InspectWorkbook(dynamic workbook, string path)
    {
        dynamic sheets = workbook.Worksheets;
        try
        {
            for (var index = 1; index <= (int)sheets.Count; index++)
            {
                dynamic sheet = sheets[index];
                try
                {
                    var inspection = InspectSheet(sheet, path);
                    if (inspection is not null) return inspection;
                }
                finally
                {
                    Release(sheet);
                }
            }
        }
        finally
        {
            Release(sheets);
        }

        return null;
    }

    private static ExcelInspection? InspectSheet(dynamic sheet, string path)
    {
        dynamic used = sheet.UsedRange;
        try
        {
            var rows = (int)used.Rows.Count;
            var columns = (int)used.Columns.Count;
            if (rows < 2 || columns < 2) return null;

            var values = used.Value2;
            var firstRow = (int)used.Row;
            var firstColumn = (int)used.Column;
            Dictionary<string, int>? found = null;
            var headerRow = 0;

            for (var relativeRow = 1; relativeRow <= Math.Min(rows, 15); relativeRow++)
            {
                var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (var relativeColumn = 1; relativeColumn <= columns; relativeColumn++)
                {
                    var header = CellText(values, relativeRow, relativeColumn, rows, columns);
                    if (!string.IsNullOrWhiteSpace(header)) headers[Normalize(header)] = firstColumn + relativeColumn - 1;
                }

                var mapped = MapColumns(headers);
                if (RequiredColumns.All(mapped.ContainsKey))
                {
                    found = mapped;
                    headerRow = firstRow + relativeRow - 1;
                    break;
                }
            }

            if (found is null) return null;
            var classerColumn = found["Classer"];
            var options = new List<string>();
            var lastRow = firstRow + rows - 1;
            for (var row = headerRow + 1; row <= lastRow; row++)
            {
                string value = CellValue((object)sheet, row, classerColumn);
                if (!string.IsNullOrWhiteSpace(value) && !options.Contains(value, StringComparer.OrdinalIgnoreCase)) options.Add(value);
            }

            return new ExcelInspection((Path.GetFullPath(path)), (string)sheet.Name, headerRow, found, options, firstColumn + columns - 1);
        }
        finally
        {
            Release(used);
        }
    }

    private static Dictionary<string, int> MapColumns(Dictionary<string, int> headers)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Add(headers, result, "Codification", "codification existante", "codification", "code");
        Add(headers, result, "Document", "document enregistre", "document", "titre", "intitule", "objet");
        Add(headers, result, "Type", "type");
        Add(headers, result, "Domain", "domaine");
        Add(headers, result, "Version", "numero de version", "version");
        Add(headers, result, "Date", "date de mise a jour", "date de validite", "date");
        Add(headers, result, "Classer", "lieu de classement", "classeur", "emplacement", "localisation", "dossier");
        Add(headers, result, "Author", "prepare par", "auteur", "responsable");
        Add(headers, result, "Number", "numero", "n");
        return result;
    }

    private static void Add(Dictionary<string, int> headers, Dictionary<string, int> result, string key, params string[] aliases)
    {
        foreach (var header in headers)
        foreach (var alias in aliases)
        {
            var normalizedAlias = Normalize(alias);
            if (header.Key.Equals(normalizedAlias, StringComparison.OrdinalIgnoreCase) ||
                (normalizedAlias.Length > 2 && header.Key.Contains(normalizedAlias, StringComparison.OrdinalIgnoreCase)))
            {
                result[key] = header.Value;
                return;
            }
        }
    }

    private static string Normalize(string value)
    {
        var form = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(form.Length);
        foreach (var character in form)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark)
                builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
        return string.Join(' ', builder.ToString().Split([' '], StringSplitOptions.RemoveEmptyEntries));
    }

    private static string CellText(object values, int row, int column, int rows, int columns)
    {
        object? value = values is Array array && rows > 1 && columns > 1 ? array.GetValue(row, column) : row == 1 && column == 1 ? values : null;
        return Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }

    private static bool ContainsCode(dynamic sheet, ExcelInspection inspection, string code)
    {
        var last = LastDataRow(sheet, inspection);
        var column = inspection.Columns["Codification"];
        for (var row = inspection.HeaderRow + 1; row <= last; row++)
        {
            var value = Convert.ToString(sheet.Cells[row, column].Value2, CultureInfo.InvariantCulture)?.Trim();
            if (string.Equals(value, code, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static int LastDataRow(dynamic sheet, ExcelInspection inspection)
    {
        var columns = inspection.Columns.Values.Distinct().ToArray();
        dynamic used = sheet.UsedRange;
        try
        {
            var lastPossible = (int)used.Row + (int)used.Rows.Count - 1;
            for (var row = lastPossible; row > inspection.HeaderRow; row--)
                if (columns.Any(column => !string.IsNullOrWhiteSpace(CellValue((object)sheet, row, column)))) return row;
            return inspection.HeaderRow;
        }
        finally
        {
            Release(used);
        }
    }

    private static int NextNumber(dynamic sheet, ExcelInspection inspection, int column)
    {
        var next = 1;
        var last = LastDataRow(sheet, inspection);
        for (var row = inspection.HeaderRow + 1; row <= last; row++)
        {
            int number;
            if (int.TryParse(CellValue((object)sheet, row, column), out number)) next = Math.Max(next, number + 1);
        }
        return next;
    }

    private static void CopyPreviousRowFormatting(dynamic sheet, int row, ExcelInspection inspection)
    {
        if (row <= inspection.HeaderRow + 1) return;
        try
        {
            var firstColumn = inspection.Columns.Values.Min();
            dynamic source = sheet.Range[sheet.Cells[row - 1, firstColumn], sheet.Cells[row - 1, inspection.LastUsedColumn]];
            dynamic target = sheet.Range[sheet.Cells[row, firstColumn], sheet.Cells[row, inspection.LastUsedColumn]];
            source.Copy();
            target.PasteSpecial(-4122);
            sheet.Application.CutCopyMode = false;
            Release(source);
            Release(target);
        }
        catch
        {
            // Formatting is helpful but never allowed to block the registry append.
        }
    }

    private static string CellValue(object sheetObject, int row, int column)
    {
        dynamic sheet = sheetObject;
        object raw = sheet.Cells[row, column].Value2;
        return Convert.ToString(raw, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }

    private static void Set(dynamic sheet, int row, int column, object value) => sheet.Cells[row, column].Value2 = value;

    private static void CloseExcel(dynamic? workbook, dynamic? excel, bool save)
    {
        if (workbook is not null)
        {
            try { workbook.Close(save); } catch { }
            Release(workbook);
        }
        if (excel is not null)
        {
            try { excel.Quit(); } catch { }
            Release(excel);
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private static void Release(object? value)
    {
        try { if (value is not null && Marshal.IsComObject(value)) Marshal.FinalReleaseComObject(value); } catch { }
    }
}
