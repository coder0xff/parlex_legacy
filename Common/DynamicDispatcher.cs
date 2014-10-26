using System;
using System.Collections.Concurrent.More;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using FastDelegate;

namespace Common {
    public class DynamicDispatcher {
        private readonly Node _rootNode = new Node();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public DynamicDispatcher() {
            MethodBase method = new StackTrace(1).GetFrame(0).GetMethod();
            Type containingType = method.DeclaringType;
            Debug.Assert(containingType != null, "containingType != null");
            foreach (MethodInfo candidate in containingType.GetMethods().Where(x => x.Name == method.Name && x.GetParameters().Count() == method.GetParameters().Count())) {
                BuildTree(_rootNode, candidate.Bind(), candidate.GetParameters().Select(x => x.ParameterType));
            }
        }

        private static void BuildTree(Node node, Func<Object, Object[], Object> delegateFunc, IEnumerable<Type> parameterTypes) {
            if (!parameterTypes.Any()) {
                node.DelegateFunc = delegateFunc;
            } else {
                BuildTree(node.Children[parameterTypes.First()], delegateFunc, parameterTypes.Skip(1));
            }
        }

        public TReturn Dispatch<TReturn>(Object instance, params Object[] parameters) {
            Node currentNode = _rootNode;
            foreach (object parameter in parameters) {
                currentNode = currentNode.Children[parameter.GetType()];
            }
            return (TReturn)currentNode.DelegateFunc(instance, parameters);
        }

        private class Node {
            public readonly JaggedAutoDictionary<Type, Node> Children = new JaggedAutoDictionary<Type, Node>(x => new Node());
            public Func<Object, Object[], Object> DelegateFunc;
        }
    }
}