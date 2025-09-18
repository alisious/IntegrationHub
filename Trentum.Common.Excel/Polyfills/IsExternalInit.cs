// Polyfills/IsExternalInit.cs
#if NETSTANDARD2_0 || NET472 || NET48
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill dla 'init' i 'record' na TFM < .NET 5.
    /// Wystarczy istnienie typu — pusta definicja jest OK.
    /// </summary>
    public static class IsExternalInit { }
}
#endif
