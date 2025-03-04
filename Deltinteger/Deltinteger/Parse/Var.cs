using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Parse
{
    public class VarCollection
    {
        private int NextFreeGlobalIndex = 0;
        private int NextFreePlayerIndex = 0;

        public Variable UseVar = Variable.A;

        public int Assign(bool isGlobal)
        {
            if (isGlobal)
            {
                int index = NextFreeGlobalIndex;
                NextFreeGlobalIndex++;
                return index;
            }
            else
            {
                int index = NextFreePlayerIndex;
                NextFreePlayerIndex++;
                return index;
            }
        }

        public Var AssignVar(string name, bool isGlobal)
        {
            Var var = new Var("INTERNAL : " + name, isGlobal, UseVar, Assign(isGlobal));
            AllVars.Add(var);
            return var;
        }

        public DefinedVar AssignDefinedVar(ScopeGroup scopeGroup, bool isGlobal, string name, Range range)
        {
            DefinedVar var = new DefinedVar(scopeGroup, name, isGlobal, UseVar, Assign(isGlobal), range);
            AllVars.Add(var);
            return var;
        }

        public ParameterVar AssignParameterVar(List<Element> actions, ScopeGroup scopeGroup, bool isGlobal, string name, Range range)
        {
            ParameterVar var = new ParameterVar(actions, scopeGroup, name, isGlobal, UseVar, Assign(isGlobal), range);
            AllVars.Add(var);
            return var;
        }

        public readonly List<Var> AllVars = new List<Var>();
    }

    public class Var
    {
        public string Name { get; private set; }
        public bool IsGlobal { get; private set; }
        public Variable Variable { get; private set; }
        public int Index { get; private set; }

        private readonly IWorkshopTree VariableAsWorkshop; 

        public Var(string name, bool isGlobal, Variable variable, int index)
        {
            Name = name;
            IsGlobal = isGlobal;
            Variable = variable;
            VariableAsWorkshop = EnumData.GetEnumValue(Variable);
            Index = index;
        }

        public virtual Element GetVariable(Element targetPlayer = null)
        {
            return GetSub(targetPlayer ?? new V_EventPlayer());
        }

        private Element GetSub(Element targetPlayer)
        {
            if (Index != -1)
                return Element.Part<V_ValueInArray>(GetRoot(targetPlayer), new V_Number(Index));
            else
                return GetRoot(targetPlayer);
        }

        private Element GetRoot(Element targetPlayer)
        {
            if (IsGlobal)
                return Element.Part<V_GlobalVariable>(VariableAsWorkshop);
            else
                return Element.Part<V_PlayerVariable>(targetPlayer, VariableAsWorkshop);
        }

        public virtual Element SetVariable(Element value, Element targetPlayer = null, Element setAtIndex = null)
        {
            Element element;

            if (targetPlayer == null)
                targetPlayer = new V_EventPlayer();

            if (setAtIndex == null)
            {
                if (Index != -1)
                {
                    if (IsGlobal)
                        element = Element.Part<A_SetGlobalVariableAtIndex>(VariableAsWorkshop, new V_Number(Index), value);
                    else
                        element = Element.Part<A_SetPlayerVariableAtIndex>(targetPlayer, VariableAsWorkshop, new V_Number(Index), value);
                }
                else
                {
                    if (IsGlobal)
                        element = Element.Part<A_SetGlobalVariable>(VariableAsWorkshop, value);
                    else
                        element = Element.Part<A_SetPlayerVariable>(targetPlayer, VariableAsWorkshop, value);
                }
            }
            else
            {
                if (Index != -1)
                {
                    if (IsGlobal)
                        element = Element.Part<A_SetGlobalVariableAtIndex>(VariableAsWorkshop, new V_Number(Index), 
                            Element.Part<V_Append>(
                                Element.Part<V_Append>(
                                    Element.Part<V_ArraySlice>(GetVariable(targetPlayer), new V_Number(0), setAtIndex), 
                                    value),
                            Element.Part<V_ArraySlice>(GetVariable(targetPlayer), Element.Part<V_Add>(setAtIndex, new V_Number(1)), new V_Number(9999))));
                    else
                        element = Element.Part<A_SetPlayerVariableAtIndex>(targetPlayer, VariableAsWorkshop, new V_Number(Index),
                            Element.Part<V_Append>(
                                Element.Part<V_Append>(
                                    Element.Part<V_ArraySlice>(GetVariable(targetPlayer), new V_Number(0), setAtIndex),
                                    value),
                            Element.Part<V_ArraySlice>(GetVariable(targetPlayer), Element.Part<V_Add>(setAtIndex, new V_Number(1)), new V_Number(9999))));
                }
                else
                {
                    if (IsGlobal)
                        element = Element.Part<A_SetGlobalVariableAtIndex>(VariableAsWorkshop, setAtIndex, value);
                    else
                        element = Element.Part<A_SetPlayerVariableAtIndex>(targetPlayer, VariableAsWorkshop, setAtIndex, value);
                }
            }

            return element;

        }
    }

    public class DefinedVar : Var
    {
        /*
        public DefinedVar(ScopeGroup scopeGroup, DefinedNode node, VarCollection varCollection)
            : base(node.VariableName, node.IsGlobal, varCollection.UseVar, node.UseIndex ?? varCollection.Assign(node.IsGlobal))
        {
            if (scopeGroup.IsVar(node.VariableName))
                throw SyntaxErrorException.AlreadyDefined(node.VariableName, node.Range);

            scopeGroup.In(this);
        }
        */

        public DefinedVar(ScopeGroup scopeGroup, string name, bool isGlobal, Variable variable, int index, Range range)
            : base (name, isGlobal, variable, index)
        {
            if (scopeGroup.IsVar(name))
                throw SyntaxErrorException.AlreadyDefined(name, range);

            scopeGroup./* we're */ In(this) /* together! */;
        }
    }

    public class ParameterVar : DefinedVar
    {
        private static readonly IWorkshopTree bAsWorkshop = EnumData.GetEnumValue(Variable.B); // TODO: Remove when multidimensional temp var can be set.

        private readonly List<Element> Actions = new List<Element>();

        public ParameterVar(List<Element> actions, ScopeGroup scopeGroup, string name, bool isGlobal, Variable variable, int index, Range range)
            : base (scopeGroup, name, isGlobal, variable, index, range)
        {
            Actions = actions;
        }

        override public Element GetVariable(Element targetPlayer = null)
        {
            return Element.Part<V_LastOf>(base.GetVariable(targetPlayer));
        }

        override public Element SetVariable(Element value, Element targetPlayer = null, Element setAtIndex = null)
        {
            Actions.Add(
                Element.Part<A_SetGlobalVariable>(bAsWorkshop, base.GetVariable(targetPlayer))
                );
            
            Actions.Add(
                Element.Part<A_SetGlobalVariableAtIndex>(bAsWorkshop, 
                    Element.Part<V_Subtract>(Element.Part<V_CountOf>(Element.Part<V_GlobalVariable>(bAsWorkshop)), new V_Number(1)), value)
            );

            return base.SetVariable(Element.Part<V_GlobalVariable>(bAsWorkshop), targetPlayer);
        }

        public Element Push(Element value, Element targetPlayer = null)
        {
            Actions.Add(
                Element.Part<A_SetGlobalVariable>(bAsWorkshop, base.GetVariable(targetPlayer))
            );
            
            Actions.Add(
                Element.Part<A_SetGlobalVariableAtIndex>(bAsWorkshop, Element.Part<V_CountOf>(Element.Part<V_GlobalVariable>(bAsWorkshop)), value)
            );

            return base.SetVariable(Element.Part<V_GlobalVariable>(bAsWorkshop), targetPlayer);
        }

        public void Pop(Element targetPlayer = null)
        {
            Element get = base.GetVariable(targetPlayer);
            Actions.Add(base.SetVariable(
                Element.Part<V_ArraySlice>(
                    get,
                    new V_Number(0),
                    Element.Part<V_Subtract>(
                        Element.Part<V_CountOf>(get), new V_Number(1)
                    )
                ), targetPlayer
            ));
        }

        public Element DebugStack(Element targetPlayer = null)
        {
            return base.GetVariable(targetPlayer);
        }
    }
}