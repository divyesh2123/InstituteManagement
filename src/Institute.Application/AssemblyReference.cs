using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Institute.ArchitectureTests")]

namespace Institute.Application;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
