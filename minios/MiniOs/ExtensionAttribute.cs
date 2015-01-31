using System;

namespace System.Runtime.CompilerServices
{
    // framework hacking
    // by implementing this attribute you can use the known extension method declaration :)
    [AttributeUsage(AttributeTargets.Assembly |
        AttributeTargets.Class |
        AttributeTargets.Method)]
    public sealed class ExtensionAttribute : Attribute
    { }
}

