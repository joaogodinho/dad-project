using CommonCode.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommonCode.Models
{
    [Serializable]
    public class Operator
    {
        public Tuple<string, int> Id { get; set; }
        public string PCS { get; set; }
        public string Input { get; set; }
        public List<Uri> DownIps = new List<Uri>();
        public int Port { get; set; }
        public Tuple<string, string> Routing { get; set; }
        public OperatorSpec Spec { get; set; }
        public IReplica Replica { get; set; }

        public Operator(Tuple<string, int> id, string pcs, string input, int port, Tuple<string, string> routing)
        {
            Id = id;
            PCS = pcs;
            Input = input;
            Port = port;
            Routing = routing;
        }
    }

    [Serializable]
    public abstract class OperatorSpec
    {
        public abstract List<List<string>> processTuple(List<string> tuple);
    }
    
    [Serializable]
    public class OperatorCount : OperatorSpec
    {
        public int CurrentCount = 0;
        public override List<List<string>> processTuple(List<string> tuple)
        {
            List<List<string>> tuples = new List<List<string>>();
            int count = Interlocked.Increment(ref CurrentCount);
            Console.WriteLine(count);
            return tuples;
        }
    }

    // TODO Use HashSet instead of List
    [Serializable]
    public class OperatorUniq : OperatorSpec
    {
        public int FieldNumber { get; set; }
        public List<string> Hits { get; set; }

        public OperatorUniq(int fieldNumb)
        {
            FieldNumber = fieldNumb-1;
            Hits = new List<string>();
        }
        public override List<List<string>> processTuple(List<string> tuple)
        {
            List<List<string>> tuples = new List<List<string>>();
            if (Hits.Contains(tuple[FieldNumber]))
                return tuples;
            else Hits.Add(tuple[FieldNumber]);
            tuples.Add(tuple);
            return tuples;

        }
    }

    [Serializable]
    public class OperatorDup : OperatorSpec
    {
        public override List<List<string>> processTuple(List<string> tuple)
        {
            List<List<string>> tuples = new List<List<string>>();
            tuples.Add(tuple);
            return tuples;
        }
    }

    // TODO Define a lambda or whatever C# calls it to spare the switch
    [Serializable]
    public class OperatorFilter : OperatorSpec
    {
        public int Field { get; set; }
        public string Condition { get; set; }
        public string Value { get; set; }
        public OperatorFilter(int field, string condition, string value)
        {
            Field = field-1;
            Condition = condition;
            Value = value;
        }
        public override List<List<string>> processTuple(List<string> tuple)
        {
            List<List<string>> tuples = new List<List<string>>();
            if (IsFilterAMatch(tuple, Field, Condition, Value))
                tuples.Add(tuple);
            return tuples;
        }

        private bool IsFilterAMatch(List<string> tuple, int field, string condition, string compareTo)
        {
            switch (condition)
            {
                case "=": return tuple[field] == compareTo;
                case "<": return tuple[field].CompareTo(compareTo) < 0;
                case ">": return tuple[field].CompareTo(compareTo) > 0;
                default: return false;
            }
        }
    }

    // TODO Test this implementation
    [Serializable]
    public class OperatorCustom : OperatorSpec
    {
        public string Dll { get; set; }
        public string ClassName { get; set; }
        public string Method { get; set; }

        public OperatorCustom(string dll, string className, string method)
        {
            Dll = dll;
            ClassName = className;
            Method = method;
        }

        public override List<List<string>> processTuple(List<string> tuple)
        {
            IList<IList<string>> magicCombo = new List<IList<string>>();
            magicCombo.Add(tuple);
            var DLL = Assembly.LoadFile(AppDomain.CurrentDomain.BaseDirectory + Dll);
            Type target = null;
            foreach (Type lel in DLL.GetTypes())
            {
                if (lel.Name.Contains(ClassName)) { 
                    target = lel;break;
                }
            }
            var c = Activator.CreateInstance(target);
            var method = target.GetMethod(Method);

            IList<IList<string>> result = (IList < IList < string >>) method.Invoke(c, new object[] { magicCombo });
            List<List<string>> magicList = new List<List<string>>();
            foreach (var item in result)
            {
                List<string> element = new List<string>(item.AsEnumerable());
                magicList.Add(element);
            }
            return magicList;
        }
    }
}