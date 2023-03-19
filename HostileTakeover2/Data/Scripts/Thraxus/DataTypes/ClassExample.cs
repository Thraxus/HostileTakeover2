namespace HostileTakeover2.Thraxus.DataTypes
{
    public class ClassExample
    {
        private readonly int _someNumber;

        public ClassExample(int someNumber)
        {   // unlike a struct, classes should always have a ctor unless there is a very good reason not to
            _someNumber = someNumber;
        }

        public override string ToString()
        {
            return $"It helps to override the ToString method so you can get an accurate log representation of the struct. {_someNumber}";
        }
    }
}