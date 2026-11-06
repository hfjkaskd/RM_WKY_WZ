using System;
using System.Text;
using Whitebox.Foundation;
public static class LocalBoosterInventoryHarness
{
    public static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            string raw=line=="~"?null:Encoding.UTF8.GetString(Convert.FromBase64String(line));
            var bits=OriginalLocalAccountLoad.ParseUnlimitedBoosterInventory(raw);
            byte[] bytes=new byte[(bits.Length+7)/8];bits.CopyTo(bytes,0);
            Console.WriteLine(bits.Length+":"+Convert.ToBase64String(bytes));
        }
    }
}
