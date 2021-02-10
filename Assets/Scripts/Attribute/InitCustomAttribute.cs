using System;
using System.Reflection;

public class InitCustomAttribute : IInit
{
    public void Init()
    {
        InitData<BindAttribute>(BindUtil.Bind);
    }

    private void InitData<T>(Action<T, Type> callback) where T : Attribute
    {
        Assembly assembly = Assembly.GetAssembly(typeof(T));
        Type[] types = assembly.GetExportedTypes();

        foreach(Type type in types)
        {
            foreach(var attribute in Attribute.GetCustomAttributes(type))
            {
                if (attribute is T t)
                {
                    callback(t, type);
                }
            }
        }
    }
}
