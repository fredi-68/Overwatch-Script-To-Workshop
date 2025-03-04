using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class UserMethod
    {
        public UserMethod(UserMethodNode node)
        {
            Name = node.Name;
            Block = node.Block;

            Parameters = new Parameter[node.Parameters.Length];
            for (int i = 0; i < Parameters.Length; i++)
            {
                Parameters[i] = new Parameter(node.Parameters[i], Elements.ValueType.Any, null);
            }

            //UserMethodCollection.Add(this);
        }

        public string Name { get; private set; }

        public BlockNode Block { get; private set; }

        public Parameter[] Parameters { get; private set; }

        //public static readonly List<UserMethod> UserMethodCollection = new List<UserMethod>();

        public override string ToString()
        {
            return Name + "(" + Parameter.ParameterGroupToString(Parameters) + ")";
        }

        public static UserMethod GetUserMethod(UserMethod[] methods, string name)
        {
            return methods.FirstOrDefault(um => um.Name == name);
        }

        public static CompletionItem[] CollectionCompletion(UserMethod[] methods)
        {
            return methods.Select(method => 
                new CompletionItem(method.ToString())
                {
                    kind = CompletionItem.Method
                }
            ).ToArray();
        }
    }

    public class MethodStack
    {
        public UserMethod UserMethod { get; private set; }
        public ParameterVar[] ParameterVars { get; private set; }
        public int ActionIndex { get; private set; }
        public Var Return { get; private set; }
        public Var ContinueSkipArray { get; private set; }

        public MethodStack(UserMethod userMethod, ParameterVar[] parameterVars, int actionIndex, Var @return, Var continueSkipArray)
        {
            UserMethod = userMethod;
            ParameterVars = parameterVars;
            ActionIndex = actionIndex;
            Return = @return;
            ContinueSkipArray = continueSkipArray;
        }
    }
}