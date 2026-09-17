using Object = UnityEngine.Object;

namespace GameFramework.Performance.Resources
{
    /// <summary>Default <see cref="IAssetHandle{T}"/>, returned by <see cref="ResourcesAssetProvider"/>.</summary>
    internal sealed class AssetHandle<T> : IAssetHandle<T> where T : Object
    {
        private readonly ResourcesAssetProvider _owner;
        private readonly string _key;
        private T _asset;
        private bool _released;

        internal AssetHandle(ResourcesAssetProvider owner, string key, T asset)
        {
            _owner = owner;
            _key = key;
            _asset = asset;
        }

        public T Asset => _released ? null : _asset;

        public bool IsLoaded => !_released && _asset != null;

        public void Release()
        {
            if (_released)
            {
                return;
            }

            _released = true;
            _owner?.ReleaseKey(_key);
            _asset = null;
        }
    }
}
