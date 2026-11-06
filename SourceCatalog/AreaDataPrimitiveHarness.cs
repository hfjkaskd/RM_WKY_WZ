using System;
using Whitebox.Foundation;
public static class AreaDataPrimitiveHarness
{
    public static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            var p=line.Split(',');int length=int.Parse(p[1]),mask=int.Parse(p[2]),value=0,other=0,index=mask;string output="";bool failed=false;
            try
            {
                if(p[0]=="G")
                {byte[] data=length<0?null:new byte[length];if(data!=null)for(int i=0;i<length;i++)data[i]=(byte)(i*17+3);value=OriginalAreaDataParser.GetNextInt(data,ref index);output=value+","+index;}
                else
                {
                    OriginalAreaTaskData[] tasks=length<0?null:new OriginalAreaTaskData[length];
                    if(tasks!=null)for(int i=0;i<length;i++)tasks[i]=new OriginalAreaTaskData(i+11,(mask&(1<<i))!=0);
                    var area=new OriginalUserAreaData(null){AreaId=int.Parse(p[3]),ChestStatus=int.Parse(p[4]),Tasks=tasks};
                    if(p[0]=="B"){value=area.GetTaskValue();other=OriginalAreaDataParser.ConvertAreaTaskArrayToTaskData(tasks);output=value+","+other;}
                    else output=area.EncodeAsString();
                }
            }
            catch(Exception){failed=true;output=p[0]=="G"?value+","+index:p[0]=="B"?value+","+other:"";}
            Console.WriteLine((failed?1:0)+"#"+output);
        }
    }
}
