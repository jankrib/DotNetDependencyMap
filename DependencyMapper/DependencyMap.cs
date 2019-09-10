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

        private static IEnumerable<Assembly> GetAssembliesInSameDir(Assembly assembly)
        {
            string path = Path.GetDirectoryName(assembly.Location);

            foreach (string dll in Directory.GetFiles(path, "*.dll"))
                yield return Assembly.LoadFrom(dll);
        }

        private static Node Load(Assembly assembly, Dictionary<string, Node> nodes, List<Edge> edges)
        {
            var node = new Node(assembly.GetName().Name);
            nodes.Add(assembly.GetName().Name, node);

            foreach (var dependencyName in assembly.GetReferencedAssemblies())
            {
                if (!nodes.TryGetValue(dependencyName.Name, out Node dependency))
                {
                    var dependencyAssembly = Assembly.Load(dependencyName);
                    dependency = Load(dependencyAssembly, nodes, edges);
                }

                edges.Add(new Edge(node, dependency));
            }

            return node;
        }

        public static DependencyMap CreateFrom(Assembly assembly)
        {
            var ass = GetAssembliesInSameDir(assembly).ToImmutableDictionary(x => x.FullName);

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler((o, a) =>
            {
                return ass[a.Name];
            });








            var nodes = new[]
            {
                new Node("System.Runtime.CompilerServices.Unsafe")
            }.ToDictionary(x => x.Name);

            var edges = new List<Edge>();


            Load(assembly, nodes, edges);

            return new DependencyMap(nodes.Values.ToImmutableArray(), edges.ToImmutableArray());
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
        public Node(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override bool Equals(object obj)
        {
            return obj is Node node &&
                   Name == node.Name;
        }

        public override int GetHashCode()
        {
            return 558414575 + EqualityComparer<string>.Default.GetHashCode(Name);
        }
    }
}
