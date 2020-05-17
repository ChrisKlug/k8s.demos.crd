using CrdController.Models;
using System.Collections.Generic;

namespace CrdController.Services
{
    public interface IFooList
    {
        void Add(Foo item);
        void Update(Foo item);
        void Remove(Foo item);
        Foo Find(string name);

        IEnumerable<Foo> Foos { get; }
    }
}