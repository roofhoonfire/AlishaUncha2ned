// UIInputGate.cs
public static class UIInputGate
{
    private static int _lock;
    public static bool IsLocked => _lock > 0;
    public static void Push() { _lock++; }
    public static void Pop() { if (_lock > 0) _lock--; }
    public static void Clear() { _lock = 0; }
}
