using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FastDelegate;

namespace Common {
    public class DynamicDispatcher {
        private class Node {
            public readonly JaggedAutoDictionary<Type, Node> Children = new JaggedAutoDictionary<Type, Node>(x => new Node());
            public Func<Object, Object[], Object> DelegateFunc;
        }

        readonly Node _rootNode = new Node();

        static void BuildTree(Node node, Func<Object, Object[], Object> delegateFunc, IEnumerable<Type> parameterTypes) {
            if (!parameterTypes.Any()) {
                node.DelegateFunc = delegateFunc;
            } else {
                BuildTree(node.Children[parameterTypes.First()], delegateFunc, parameterTypes.Skip(1));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public DynamicDispatcher() {
            var method = new StackTrace(1).GetFrame(0).GetMethod();
            var containingType = method.DeclaringType;
            Debug.Assert(containingType != null, "containingType != null");
            foreach (var candidate in containingType.GetMethods().Where(x => x.Name == method.Name && x.GetParameters().Count() == method.GetParameters().Count())) {
                BuildTree(_rootNode, candidate.Bind(), candidate.GetParameters().Select(x => x.ParameterType));
            }
        }

        public TReturn Dispatch<TReturn>(Object instance, params Object[] parameters) {
            Node currentNode = _rootNode;
            foreach (var parameter in parameters) {
                currentNode = currentNode.Children[parameter.GetType()];
            }
            return (TReturn)currentNode.DelegateFunc(instance, parameters);
        }
    }
}
