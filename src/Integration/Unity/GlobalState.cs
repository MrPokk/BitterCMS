using BitterCMS.Utility.Interfaces;

public static class GlobalState
{
    private static IRoot _root;

    public static void SetRoot(IRoot root)
    {
        if (_root == null || _root != root)
            _root = root;
    }
    public static T GetRoot<T>() where T : class, IRoot
    {
        return _root as T;
    }
}
