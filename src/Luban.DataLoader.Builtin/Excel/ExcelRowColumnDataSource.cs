using Luban.Core.DataLoader;
using Luban.Core.Datas;
using Luban.Core.Defs;
using Luban.Core.Types;
using Luban.Core.Utils;
using Luban.DataLoader.Builtin.DataVisitors;

namespace Luban.DataLoader.Builtin.Excel;

[DataLoader("xls", "xlsx", "xlm", "xlsm", "csv")]
public class ExcelRowColumnDataSource : DataLoaderBase
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly List<RowColumnSheet> _sheets = new List<RowColumnSheet>();


    public override void Load(DefTable table, string rawUrl, string sheetName, Stream stream)
    {
        s_logger.Trace("{filename} {sheet}", rawUrl, sheetName);
        RawUrl = rawUrl;


        foreach (RawSheet rawSheet in SheetLoadUtil.LoadRawSheets(rawUrl, sheetName, stream))
        {
            var sheet = new RowColumnSheet(rawUrl, sheetName);
            sheet.Load(rawSheet);
            _sheets.Add(sheet);
        }

        if (_sheets.Count == 0)
        {
            throw new Exception($"excel:{rawUrl} 不包含有效的单元薄(有效单元薄的A0单元格必须是##).");
        }
    }

    public void Load(params RawSheet[] rawSheets)
    {
        foreach (RawSheet rawSheet in rawSheets)
        {
            var sheet = new RowColumnSheet("__intern__", rawSheet.TableName);
            sheet.Load(rawSheet);
            _sheets.Add(sheet);
        }
    }

    public RawSheetTableDefInfo LoadTableDefInfo(string rawUrl, string sheetName, Stream stream)
    {
        return SheetLoadUtil.LoadSheetTableDefInfo(rawUrl, sheetName, stream);
    }

    public override List<Record> ReadMulti(DefTable table, TBean type)
    {
        var datas = new List<Record>();
        foreach (var sheet in _sheets)
        {
            try
            {
                foreach (var r in sheet.GetRows())
                {
                    TitleRow row = r.Row;
                    string tagStr = r.Tag;
                    if (DataUtil.IsIgnoreTag(tagStr))
                    {
                        continue;
                    }
                    var data = (DBean)type.Apply(SheetDataCreator.Ins, sheet, row);
                    datas.Add(new Record(data, sheet.RawUrl, DataUtil.ParseTags(tagStr)));
                }
            }
            catch (DataCreateException dce)
            {
                dce.OriginDataLocation = sheet.RawUrl;
                throw;
            }
            catch (Exception e)
            {
                throw new Exception($"sheet:{sheet.Name}", e);
            }
        }
        return datas;
    }

    public override Record ReadOne(DefTable table, TBean type)
    {
        throw new Exception($"excel不支持单例读取模式");
    }
}