namespace Bizza.UserInformationVerification
{
    public interface IAppIdentityProvider
    {
        string AppId { get; }
        string PackageName { get; }
        string UnityRegionCode { get; }
    }

    public interface IAuthLogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);
    }
}
