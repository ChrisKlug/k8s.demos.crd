using k8s;
using k8s.Models;

namespace CrdController.Models
{
    public class Foo : IKubernetesObject<V1ObjectMeta>, IKubernetesObject, IMetadata<V1ObjectMeta>
    {
        public const string Group = "demos.fearofoblivion.com";
        public const string Version = "v1";
        public const string Plural = "foos";
        public const string Singular = "foo";

        public string ApiVersion { get; set; }
        public string Kind { get; set; }
        public V1ObjectMeta Metadata { get; set; }
        public FooSpec Spec { get; set; }
        public string Status { get; set; }

        public class FooSpec
        {
            public string Value1 { get; set; }
            public string Value2 { get; set; }
            public int Value3 { get; set; }
            public int Value4 { get; set; }
        }
    }
}
