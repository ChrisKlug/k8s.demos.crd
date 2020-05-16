using CrdController.Models;
using System.Collections.Generic;
using System.Linq;

namespace CrdController
{
    public class FooList : IFooList
    {
        private readonly List<Foo> _foos = new List<Foo>();

        public void Add(Foo item)
        {
            _foos.Add(item);
        }
        public void Remove(Foo item)
        {
            _foos.Remove(_foos.First(x => x.Metadata.Name == item.Metadata.Name));
        }

        public IEnumerable<Foo> Foos => _foos;
    }
}
