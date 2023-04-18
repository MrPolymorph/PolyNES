namespace PolyNES.Extensions;

public static class BooleanExtensions
{
    public static int ToInt(this bool val)
    {
        return val ? 1 : 0;
    }
}