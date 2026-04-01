namespace VBAnalyzer.Capturing;

/// <summary>
/// DotNetNuke.ComponentModel.ComponentFactoryの簡易版。
/// DataProviderインスタンスの登録と取得を提供する。
/// </summary>
public static class ComponentFactory
{
    private static readonly Dictionary<Type, object> _components = new();

    /// <summary>
    /// コンポーネントを登録する。
    /// </summary>
    public static void RegisterComponentInstance<T>(T instance) where T : class
    {
        _components[typeof(T)] = instance ?? throw new ArgumentNullException(nameof(instance));
    }

    /// <summary>
    /// 登録されたコンポーネントを取得する。
    /// </summary>
    public static T GetComponent<T>() where T : class
    {
        if (_components.TryGetValue(typeof(T), out var component))
        {
            return (T)component;
        }
        throw new InvalidOperationException($"コンポーネント {typeof(T).Name} が登録されていません。");
    }

    /// <summary>
    /// 登録を全てクリアする。テストのクリーンアップ用。
    /// </summary>
    public static void Clear()
    {
        _components.Clear();
    }
}
