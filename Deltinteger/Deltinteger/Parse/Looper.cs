﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class Looper
    {
        private readonly List<Element> _actions = new List<Element>();
        private readonly Rule _rule;
        private int _insertAtIndex = 0;
        public bool Used { get; private set; }

        private readonly List<VariableChase> _chases = new List<VariableChase>();

        public Looper(bool global)
        {
            _rule = new Rule(Constants.INTERNAL_ELEMENT + "Chase", global ? RuleEvent.OngoingGlobal : RuleEvent.OngoingPlayer, Team.All, PlayerSelector.All);
            _actions.Add(A_Wait.MinimumWait);
            _actions.Add(Element.Part<A_Loop>());
        }

        public void AddActions(Element[] actions)
        {
            _actions.InsertRange(_insertAtIndex, actions);
            _insertAtIndex += actions.Length;
            Used = true;
        }

        public Rule Finalize()
        {
            _rule.Actions = _actions.ToArray();
            return _rule;
        }

        public VariableChase GetChaseData(IndexedVar var, Translate context)
        {
            var existingChaseData = _chases.FirstOrDefault(cd => cd.Var == var);
            if (existingChaseData != null)
                return existingChaseData;
            
            IndexedVar destination = context.VarCollection.AssignVar(null, $"'{var.Name}' chase destination", var.IsGlobal);
            IndexedVar rate        = context.VarCollection.AssignVar(null, $"'{var.Name}' chase duration"   , var.IsGlobal);

            VariableChase newChaseData = new VariableChase(var, destination, rate);
            _chases.Add(newChaseData);

            AddActions(GetChaseActions(var, destination, rate));

            return newChaseData;
        }

        private static Element[] GetChaseActions(IndexedVar var, IndexedVar destination, IndexedVar rate)
        {
            Element rateAdjusted = Element.Part<V_Multiply>(rate.GetVariable(), new V_Number(Constants.MINIMUM_WAIT));

            Element distance = Element.Part<V_DistanceBetween>(var.GetVariable(), destination.GetVariable());

            Element ratio = Element.Part<V_Divide>(rateAdjusted, distance);

            Element delta = Element.Part<V_Subtract>(destination.GetVariable(), var.GetVariable());

            Element result = Element.TernaryConditional(
                new V_Compare(distance, Operators.GreaterThan, rateAdjusted),
                Element.Part<V_Add>(var.GetVariable(), Element.Part<V_Multiply>(ratio, delta)),
                destination.GetVariable()
            );

            Element[] setVar = var.SetVariable(result);

            return setVar;
        }
    }

    public class VariableChase
    {
        public readonly IndexedVar Var;
        public readonly IndexedVar Destination;
        public readonly IndexedVar Rate;

        public VariableChase(IndexedVar var, IndexedVar destination, IndexedVar rate)
        {
            Var = var;
            Destination = destination;
            Rate = rate;
        }
    }
}
