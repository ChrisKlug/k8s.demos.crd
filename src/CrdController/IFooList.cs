using CrdController.Models;
using System.Collections.Generic;

namespace CrdController
{
    public interface IFooList
    {
        void Add(Foo item);
        void Remove(Foo item);

        IEnumerable<Foo> Foos { get; }
    }
}