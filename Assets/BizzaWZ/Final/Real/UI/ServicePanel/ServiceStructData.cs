#if BIZZA_REAL_WITHDRAW
using System;

[Serializable]
public struct ChatInfo
{
    public Spokesperson spokesperson; // 发言人
    public string time; // 聊天时间
    public string chatcontent; // 聊天内容
    // public ServiceType serviceType; // 服务类型

    public ChatInfo(Spokesperson spokesperson, string chatcontent, string time)
    {
        this.spokesperson = spokesperson;
        this.time = time;
        this.chatcontent = chatcontent;
        //this.serviceType = serviceType;
    }

    public void Clear()
    {
        spokesperson = Spokesperson.Issue;
        time = "";
        chatcontent = "";
    }

    public bool IsEqual(string time, string chatcontent)
    {
        // bool timeEqual = string.Equals(this.time, time);
        bool chatcontentEqual = string.Equals(this.chatcontent, chatcontent);
        return  chatcontentEqual; // timeEqual &&
    }
}
  [Obfuz.ObfuzIgnore]
public enum Spokesperson
{
    Issue = 2,
    Player = 1,
}
  [Obfuz.ObfuzIgnore]
public enum ServiceType
{
    Default,
    Custom
}
#endif
