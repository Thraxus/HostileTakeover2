using HostileTakeover2.Thraxus.Common.BaseClasses;

namespace HostileTakeover2.Thraxus.Models
{
    public class ExampleModelWithEventLog : BaseLoggingClass
    {
        public void ExampleOfClassWritingToOwnersLog()
        {
            WriteGeneral("ExampleOfClassWritingToOwnersLog", "Some Message");
        }
    }
}