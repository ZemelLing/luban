namespace Luban.DataSources.Excel;

class RawSheet
{
    public Title Title { get; set; }

    public string TableName { get; set; }

    public List<List<Cell>> Cells { get; set; }
}