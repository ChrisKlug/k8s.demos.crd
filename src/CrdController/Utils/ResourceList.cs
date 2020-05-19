using k8s;
using k8s.Models;
using System.Collections.Generic;

namespace CrdController.Utils
{
    public class ResourceList<T> : IKubernetesObject<V1ListMeta>, IKubernetesObject, IMetadata<V1ListMeta>, IItems<T>
    {
        public string ApiVersion { get; set; }
        public IList<T> Items { get; set; }
        public string Kind { get; set; }
        public V1ListMeta Metadata { get; set; }
    }
}
