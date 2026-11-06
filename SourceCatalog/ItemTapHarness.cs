using System;using Whitebox.Foundation;
namespace Whitebox.Foundation{
public struct OriginalExplodeData{}
public partial class OriginalBoardItem{}
public class OriginalSpecialBoardItem:OriginalBoardItem{}
public sealed partial class OriginalRocketBoardItem:OriginalSpecialBoardItem{}
public sealed partial class OriginalTntBoardItem:OriginalSpecialBoardItem{}
}
class Verify{static void Main(){var item=new OriginalBoardItem();Console.WriteLine(item.GetMoveType+","+(item.IsTapDisabled?1:0)+","+(item.Tap(default(OriginalExplodeData))?1:0));item.IsTapDisabled=true;Console.WriteLine(item.IsTapDisabled?1:0);item.IsTapDisabled=false;Console.WriteLine(item.IsTapDisabled?1:0);foreach(var special in new OriginalBoardItem[]{new OriginalRocketBoardItem(),new OriginalTntBoardItem()}){Console.WriteLine(special.GetMoveType);try{special.Tap(default(OriginalExplodeData));throw new Exception("Unrestored concrete tap silently succeeded.");}catch(NotSupportedException){Console.WriteLine("pending");}}}}
