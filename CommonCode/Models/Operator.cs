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
        public List<string> ReplicasUris { get; set; }

        public Operator(Tuple<string, int> id, string pcs, string input, int port, Tuple<string, string> routing,List<string> myreplicas)
        {
            ReplicasUris = myreplicas;
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
            List<string> innerTup = new List<string>();
            innerTup.Add((++CurrentCount).ToString());
            tuples.Add(innerTup);
            return tuples;
        }
    }

    [Serializable]
    public class OperatorUniq : OperatorSpec
    {
        private int FieldNumber { get; set; }
        private HashSet<string> Set { get; set; }

        public OperatorUniq(int fieldNumb)
        {
            FieldNumber = fieldNumb - 1;
            Set = new HashSet<string>();
        }

        public override List<List<string>> processTuple(List<string> tuple)
        {
            List<List<string>> tuples = new List<List<string>>();
            // Returns true if item is not in set
            if (Set.Add(tuple[FieldNumber]))
            {
                tuples.Add(tuple);
            }
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

    [Serializable]
    public class OperatorFilter : OperatorSpec
    {
        private delegate bool CondFunc(List<string> tuple);
        private CondFunc Filter { get; set; }
        private int Field { get; set; }
        private string Value { get; set; }
        public OperatorFilter(int field, string condition, string value)
        {
            Field = field - 1;
            Value = value;
            // SUCH LAMBDA, MUCH WOW
            switch (condition)
            {
                case "=":
                    Filter = (x) => { return x[Field] == Value; };
                    break;
                case "<":
                    Filter = (x) => { return x[Field].CompareTo(Value) < 0; };
                    break;
                case ">":
                    Filter = (x) => { return x[Field].CompareTo(Value) > 0; };
                    break;
                default:
                    throw new Exception("Invalid Operator condition");
            }
        }

        public override List<List<string>> processTuple(List<string> tuple)
        {
            List<List<string>> tuples = new List<List<string>>();
            if (Filter(tuple))
            {
                tuples.Add(tuple);
            }
            return tuples;
        }
    }

    [Serializable]
    public class OperatorCustom : OperatorSpec
    {
        private string Dll { get; set; }
        private string ClassName { get; set; }
        private string MethodName { get; set; }
        private Object ClassInstance { get; set; }
        private MethodInfo ClassMethod { get; set; }

        public OperatorCustom(string dll, string className, string method)
        {
            Dll = dll;
            ClassName = className;
            MethodName = method;
        }

        public override List<List<string>> processTuple(List<string> tuple)
        {
            // Need to make this here, because whatever is in the DLL might not be serializable,
            // so it cannot be passed on the TCPChannel. ¯\_(ツ)_/¯
            if (ClassInstance == null && ClassMethod == null)
            {
                Assembly ADLL = Assembly.LoadFile(AppDomain.CurrentDomain.BaseDirectory + Dll);
                Type target = null;
                foreach (Type type in ADLL.GetTypes())
                {
                    if (type.IsClass && type.Name.Contains(ClassName))
                    {
                        target = type;
                        break;
                    }
                }

                ClassInstance = Activator.CreateInstance(target);
                ClassMethod = target.GetMethod(MethodName);
            }

            IList<IList<string>> result = (IList<IList<string>>)ClassMethod.Invoke(ClassInstance, new object[] { tuple });

            List<List<string>> output = new List<List<string>>();
            foreach (IList<string> tup in result)
            {
                output.Add(new List<string>(tup.AsEnumerable()));
            }
            return output;
        }
    }
}