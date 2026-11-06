using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public class OriginalGridCell { public int Id; }
    public class OriginalBoardItem { }
    public class OriginalMatchBoardItem : OriginalBoardItem { public byte NativeMatchGroupId; }
    public interface IOriginalMatchRandomSource { byte GetRandomItemFromListBasedOnDifficulty(List<byte> values); }
}
class Verify
{
    static OriginalGridCell Cell(int id) => id < 0 ? null : new OriginalGridCell { Id=id };
    static string Format(OriginalMatchCells cells)
    {
        var values=new List<int>{cells.Count};
        for(int i=0;i<14;i++){var cell=cells.Get(i);values.Add(cell==null?-1:cell.Id);}
        return string.Join(",",values);
    }
    static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            var p=line.Split('#');var h=Array.ConvertAll(p[0].Split(','),int.Parse);var cells=new OriginalMatchCells();
            for(int i=0;i<h[1];i++)cells.Add(Cell(i));
            if(h[0]==1)
            {
                var random=new OriginalLevelRandomSource(h[2]);
                for(int i=0;i<h[3];i++)random.ShuffleCellsBasedOnDifficulty(cells);
                int next=random.DifficultyNext(0,1000000);
                Console.WriteLine(Format(cells)+":"+random.DifficultySeedGenerationCount+","+next);continue;
            }
            var output=new List<string>();
            foreach(var part in p[1].Split(';'))
            {
                var v=Array.ConvertAll(part.Split(','),int.Parse);string result="-";
                if(v[0]==0){var cell=cells.Get(v[1]);result=(cell==null?-1:cell.Id).ToString();}
                else if(v[0]==1)cells.Set(v[1],Cell(v[2]));
                else cells.Add(Cell(v[2]));
                output.Add(result+":"+Format(cells));
            }
            Console.WriteLine(string.Join("|",output));
        }
    }
}
