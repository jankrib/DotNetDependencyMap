using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DependencyMapper
{
    public class DependencyMap
    {
        public DependencyMap(ImmutableArray<Node> nodes, ImmutableArray<Edge> edges)
        {
            Nodes = nodes;
            Edges = edges;
        }

        public ImmutableArray<Edge> Edges { get; }
        public ImmutableArray<Node> Nodes { get; }

        private static IEnumerable<Assembly> GetAssembliesInSameDir(string fileName)
        {
            string path = Path.GetDirectoryName(fileName);

            foreach (string dll in Directory.GetFiles(path, "*.dll"))
                yield return Assembly.LoadFrom(dll);
        }

        private static void Load(Node node, Dictionary<AssemblyName, Node> nodes, List<Edge> edges, Func<AssemblyName, Node> nodeFactory)
        {
            try
            {
                var assembly = Assembly.Load(node.Name);
                foreach (var dependencyName in assembly.GetReferencedAssemblies())
                {
                    if (!nodes.TryGetValue(dependencyName, out Node dependency))
                    {
                        dependency = nodeFactory(dependencyName);
                        nodes[dependencyName] = dependency;
                    }

                    dependency.Level = Math.Max(dependency.Level, node.Level + 1);

                    edges.Add(new Edge(node, dependency));
                }
            }
            catch (Exception ex)
            {
                node.Error = ex;
            }
        }

        public static DependencyMap CreateFrom(string filename)
        {
            var assemblyName = AssemblyName.GetAssemblyName(filename);

            var ass = GetAssembliesInSameDir(filename).ToImmutableDictionary(x => x.FullName);

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler((o, a) =>
            {
                return ass[a.Name];
            });


            //var nodes = new[]
            //{
            //    new Node("System.Runtime.CompilerServices.Unsafe")
            //}.ToDictionary(x => x.Name);


            var nodes = new Dictionary<AssemblyName, Node>(new AssemblyNameComparer());
            var edges = new List<Edge>();
            var unloaded = new Queue<Node>();

            var node = new Node(assemblyName);
            nodes.Add(node.Name, node);
            unloaded.Enqueue(node);

            while (unloaded.Count > 0)
            {
                var n = unloaded.Dequeue();
                Load(n, nodes, edges, an =>
                {
                    var nn = new Node(an);
                    unloaded.Enqueue(nn);
                    return nn;
                });
            }

            return new DependencyMap(nodes.Values.ToImmutableArray(), edges.ToImmutableArray());
        }

        private class AssemblyNameComparer : IEqualityComparer<AssemblyName>
        {
            public int Compare(AssemblyName x, AssemblyName y)
            {
                var c = x.Name.CompareTo(y.Name);

                if (c == 0)
                {
                    c = x.Version.CompareTo(y.Version);
                }

                return c;
            }

            public bool Equals(AssemblyName x, AssemblyName y)
            {
                return x.Name.Equals(y.Name) && x.Version.Equals(y.Version);
            }

            public int GetHashCode(AssemblyName obj)
            {
                return obj.Name.GetHashCode() + obj.Version.GetHashCode();
            }
        }
    }




    public class Edge
    {
        public Edge(Node from, Node to)
        {
            From = from;
            To = to;
        }

        public Node From { get; }
        public Node To { get; }
    }

    public class Node
    {
        public Node(AssemblyName name)
        {
            Name = name;
        }
        public Exception Error { get; set; }

        public int Level { get; set; } = 0;

        public AssemblyName Name { get; }

        public override bool Equals(object obj)
        {
            return obj is Node node &&
                   Name == node.Name;
        }

        public override int GetHashCode()
        {
            return 558414575 + Name.GetHashCode();
        }
    }
}
