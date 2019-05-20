using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using OverwatchParser;
using OverwatchParser.Elements;
using OverwatchParser.Parse;

namespace OverwatchParser
{
    public class Program
    {
        static Log InputLog = new Log("Input");

        static void Main(string[] args)
        {
            string file = args[0];

            string text = File.ReadAllText(file);
            string scriptName = Path.GetFileName(file);

            string workingDirectory = Environment.CurrentDirectory;
            string compiledDirectory = Path.Combine(workingDirectory, "compiled");

            string compiledName = scriptName + Constants.COMPILED_FILETYPE;

            Rule[] generatedRules = Parser.ParseText(text);
            Workshop workshop = new Workshop(generatedRules);

            Console.ReadLine();

            if (!Directory.Exists(compiledDirectory))
                Directory.CreateDirectory(compiledDirectory);

            Workshop prev = null;
            if (File.Exists(Path.Combine(compiledDirectory, compiledName)))
            {
                Stream stream = File.Open(Path.Combine(compiledDirectory, compiledName), FileMode.Open);

                var formatter = new BinaryFormatter();
                prev = formatter.Deserialize(stream) as Workshop;

                stream.Close();
            }

            List<Rule> previousRules = prev?.Rules.ToList();

            List<int> deleteRules = new List<int>();
            List<RuleAction> ruleActions = new List<RuleAction>();

            // Remove old rules
            if (previousRules != null)
                for (int i = 0; i < previousRules.Count; i++)
                    if (!generatedRules.Contains(previousRules[i]))
                    {
                        InputLog.Write($"Deleting rule \"{previousRules[i].Name}\".");
                        deleteRules.Add(i);
                        previousRules.RemoveAt(i);
                    }

            int numberOfRules = 0;

            for (int i = 0; i < generatedRules.Length; i++)
            {
                if (previousRules != null && generatedRules[i] == previousRules.ElementAtOrDefault(i))
                    return;

                var previousIndex = previousRules?.IndexOf(generatedRules[i]) ?? -1;

                if (previousIndex == -1)
                {
                    // Create new rule
                    InputLog.Write($"Creating rule \"{generatedRules[i].Name}\".");
                    ruleActions.Add(new RuleAction(generatedRules[i], i, true));
                }
                else if (previousIndex != i)
                {
                    // Move existing rule
                    InputLog.Write($"Moving rule \"{generatedRules[i].Name}\" from #{previousIndex} to #{i}.");
                    ruleActions.Add(new RuleAction(generatedRules[i], previousIndex, i));
                    numberOfRules++;
                }
                else
                {
                    InputLog.Write($"Doing nothing to rule \"{generatedRules[i].Name}\".");
                    ruleActions.Add(new RuleAction(generatedRules[i], i, false));
                    numberOfRules++;
                }
            }

            // Save workshop

            Console.ReadLine();

            InputSim.Press(Keys.Tab, Wait.Short);
            //InputSim.Repeat(Keys.Right, Wait.Short, 2);

            // Delete rules
            int selectedRule = -1;
            foreach (var remove in deleteRules)
            {
                selectedRule = RuleNav(selectedRule, remove);

                InputSim.Press(Keys.Space, Wait.Short);
                InputSim.Press(Keys.Tab, Wait.Short);
                InputSim.Press(Keys.Right, Wait.Short);
                InputSim.Press(Keys.Space, Wait.Long);

                selectedRule = -1;
            }

            // Move and add rules.
            int index = 0;
            foreach(var action in ruleActions)
            {
                if (action.RuleActionType == RuleActionType.Add)
                {
                    selectedRule = ResetRuleNav(selectedRule);

                    action.Rule.Input(numberOfRules, action.RuleIndex);
                    numberOfRules++;
                    action.Exists = true;

                    var conflicting = ruleActions.Where(v => v != null
                    && v.Exists
                    && action.NewIndex <= v.RuleIndex
                    && !ReferenceEquals(action, v));
                    foreach (var conflict in conflicting)
                    {
                        conflict.RuleIndex += 1;
                    }
                }
                if (action.RuleIndex != action.NewIndex)
                {
                    selectedRule = RuleNav(selectedRule, action.RuleIndex);

                    InputSim.Press(Keys.Left, Wait.Short, 2);
                    if (index < action.RuleIndex)
                    {
                        InputSim.Press(Keys.Space, Wait.Short, selectedRule - action.NewIndex);
                    }

                    InputSim.Press(Keys.Right, Wait.Short, 2);

                    selectedRule = index;

                    var conflicting = ruleActions.Where(v => v != null 
                    &&  action.NewIndex <= v.RuleIndex && v.RuleIndex <= action.RuleIndex
                    && !ReferenceEquals(action, v));
                    foreach (var conflict in conflicting)
                    {
                        conflict.RuleIndex += 1;
                    }

                    action.RuleIndex = action.NewIndex;
                }

                index++;
            }

            selectedRule = ResetRuleNav(selectedRule);

            Console.WriteLine("Done. Press enter to save state.");
            Console.ReadLine();

            Stream saveStream = File.Open(Path.Combine(compiledDirectory, compiledName), FileMode.Create);

            var saveFormatter = new BinaryFormatter();
            saveFormatter.Serialize(saveStream, workshop);

            saveStream.Close();

            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        static int RuleNav(int selectedRule, int toRule)
        {
            if (selectedRule == -1)
            {
                InputSim.Press(Keys.Down, Wait.Long);
                InputSim.Press(Keys.Left, Wait.Long);
                selectedRule = 0;
            }

            if (selectedRule < toRule)
                InputSim.Press(Keys.Down, Wait.Short, toRule - selectedRule);
            else if (selectedRule > toRule)
                InputSim.Press(Keys.Up, Wait.Short, selectedRule - toRule);

            return toRule;
        }

        static int ResetRuleNav(int selectedRule)
        {
            if (selectedRule != -1)
                InputSim.Press(Keys.Up, Wait.Short, selectedRule + 1);

            return -1;
        }
    }

    [Serializable]
    class Workshop : IEquatable<Workshop>
    {
        public Workshop(Rule[] rules)
        {
            Rules = rules;
        }
        public Rule[] Rules { get; set; }

        public bool Equals(Workshop other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (other.Rules.Length != Rules.Length)
                return false;

            for (int i = 0; i < Rules.Length; i++)
                if (!Rules[i].Equals(other.Rules[i]))
                    return false;

            return true;
        }
    }

    class RuleAction
    {
        public RuleAction(Rule rule, int ruleIndex, bool create)
        {
            Rule = rule;
            RuleIndex = ruleIndex;
            NewIndex = ruleIndex;

            if (create)
            {
                RuleActionType = RuleActionType.Add;
                Exists = false;
            }
            else
            {
                RuleActionType = RuleActionType.None;
                Exists = true;
            }
        }

        public RuleAction(Rule rule, int ruleIndex, int newIndex)
        {
            Rule = rule;
            RuleIndex = ruleIndex;
            NewIndex = newIndex;
            RuleActionType = RuleActionType.Move;
            Exists = true;
        }

        public Rule Rule { get; private set; }

        public int RuleIndex { get; set; }
        public int NewIndex { get; set; }

        public bool Exists { get; set; }

        public RuleActionType RuleActionType { get; private set; }
    }

    enum RuleActionType
    {
        None,
        Add,
        Move,
    }
}
