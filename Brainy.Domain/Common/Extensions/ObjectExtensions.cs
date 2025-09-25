namespace Brainy.Domain.Common.Extensions;

public static class ObjectExtensions
{
    public static T GetPropertyByName<T>(this object obj, string name)
    {
        return (T)obj.GetType().GetProperty(name)!.GetValue(obj, null)!;
    }

    public static void SetPropertyByName(this object obj, string name, object value)
    {
        obj.GetType().GetProperty(name)!.SetValue(obj, value, null);
    }
}
