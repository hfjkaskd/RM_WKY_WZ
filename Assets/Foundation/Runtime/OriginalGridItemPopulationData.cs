namespace Whitebox.Foundation
{
    // Shared decoded item access. The complete ILevelDataProvider extends
    // this contract; fixed first-board data does not pretend to be a parser.
    public interface IOriginalGridItemPopulationData
    {
        int GetItemAtCell(int x, int y);
        int GetItemOriginalIdAtCell(int x, int y);
    }
}
