namespace HostileTakeover2.Thraxus.DataTypes
{
    public struct StructExample
    {
        public int SomeNumber;

        //public StructExample()
        //{
        // avoid a ctor, the mod profiler hates them.  Pretend this isn't really a struct if you know real programming
        //}

        public override string ToString()
        {
            return $"It helps to override the ToString method so you can get an accurate log representation of the struct. {SomeNumber}";
        }
    }
}