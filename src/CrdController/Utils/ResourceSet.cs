using k8s;
using k8s.Models;
using System.Collections.Generic;

namespace CrdController.Utils
{
    public class ResourceSet<T> where T: class, IMetadata<V1ObjectMeta>
    {
        private readonly Dictionary<string, T> _items = new Dictionary<string, T>();

        public void Add(T item)
        {
            _items.Add(item.Metadata.Name, item);
        }
        public void Remove(T item)
        {
            _items.Remove(item.Metadata.Name);
        }

        public void Update(T item)
        {
            if (_items.ContainsKey(item.Metadata.Name))
            {
                _items[item.Metadata.Name] = item;
            }
        }

        public T Find(string name)
        {
            if (_items.ContainsKey(name))
            {
                return _items[name];
            }
            return null;
        }

        public IEnumerable<T> Items => _items.Values;
    }
}
