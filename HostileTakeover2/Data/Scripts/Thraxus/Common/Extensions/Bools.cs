namespace HostileTakeover2.Thraxus.Common.Extensions
{
    public static class Bools
    {
        public static string ToSingleChar(this bool tf)
        {
            return tf ? "T" : "F";
        }
    }
}
