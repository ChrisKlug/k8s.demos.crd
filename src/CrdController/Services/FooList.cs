using CrdController.Models;
using System.Collections.Generic;

namespace CrdController.Services
{
    public class FooList : IFooList
    {
        private readonly Dictionary<string, Foo> _foos = new Dictionary<string, Foo>();

        public void Add(Foo item)
        {
            _foos.Add(item.Metadata.Name, item);
        }
        public void Remove(Foo item)
        {
            _foos.Remove(item.Metadata.Name);
        }

        public void Update(Foo item)
        {
            if (_foos.ContainsKey(item.Metadata.Name))
            {
                _foos[item.Metadata.Name] = item;
            }
        }

        public Foo Find(string name)
        {
            if (_foos.ContainsKey(name))
            {
                return _foos[name];
            }
            return null;
        }

        public IEnumerable<Foo> Foos => _foos.Values;
    }
}
