
namespace UnityExtensions
{
    public interface ICloneable<out T>
    {
        T Clone();
    }


    public interface ICopyable<in T>
    {
        void Copy(T target);
    }


    public interface INamed
    {
        string name { get; }
    }


    public interface ISettable<in T>
    {
        void Set(T arg);
    }


    public interface ISettable<in T1, in T2>
    {
        void Set(T1 arg1, T2 arg2);
    }


    public interface ISettable<in T1, in T2, in T3>
    {
        void Set(T1 arg1, T2 arg2, T3 arg3);
    }


    public interface ISettable<in T1, in T2, in T3, in T4>
    {
        void Set(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    }
}