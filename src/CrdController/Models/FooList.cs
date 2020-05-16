using k8s;
using k8s.Models;
using System.Collections.Generic;

namespace CrdController.Models
{
    public class FooList : IKubernetesObject<V1ListMeta>, IKubernetesObject, IMetadata<V1ListMeta>, IItems<Foo>
    {
        public FooList() { }

        public string ApiVersion { get; set; }
        public IList<Foo> Items { get; set; }
        public string Kind { get; set; }
        public V1ListMeta Metadata { get; set; }
    }
}
