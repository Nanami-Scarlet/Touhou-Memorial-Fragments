using System;

[AttributeUsage(AttributeTargets.Class)]
public class BindAttribute : Attribute
{
    public string Path { get; }
    public int Priority { get; }

    public BindAttribute(string path, int priority)
    {
        Path = path;
        Priority = priority;
    }
}
