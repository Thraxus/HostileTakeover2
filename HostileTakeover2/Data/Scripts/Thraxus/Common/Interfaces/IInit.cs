namespace HostileTakeover2.Thraxus.Common.Interfaces
{
    internal interface IInit<in T>
    {
        void Init(T init);
    }
}
