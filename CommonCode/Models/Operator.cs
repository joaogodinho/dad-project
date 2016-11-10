using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonCode.Models
{
    public class Operator
    {
        public string Id { get; set; }
        public string Input { get; set; }
        public List<Uri> DownIps = new List<Uri>();
        public int Port { get; set; }
        public Tuple<string, string> Routing { get; set; }
        public OperatorSpec Spec { get; set; }

    }

    public abstract class OperatorSpec
    {
        public abstract string[] processTuple(string[] tuple);
    }

    public class OperatorCount : OperatorSpec
    {
        public override string[] processTuple(string[] tuple)
        {
            throw new NotImplementedException();
        }
    }

    public class OperatorUniq : OperatorSpec
    {
        public int FieldNumber { get; set; }
        public OperatorUniq(int fieldNumb)
        {
            FieldNumber = fieldNumb;
        }
        public override string[] processTuple(string[] tuple)
        {
            throw new NotImplementedException();
        }
    }

    public class OperatorDup : OperatorSpec
    {
        public override string[] processTuple(string[] tuple)
        {
            return tuple;
        }
    }

    public class OperatorFilter : OperatorSpec
    {
        public int Field { get; set; }
        public string Condition { get; set; }
        public string Value { get; set; }
        public OperatorFilter(int field, string condition, string value)
        {
            Field = field;
            Condition = condition;
            Value = value;
        }
        public override string[] processTuple(string[] tuple)
        {
            throw new NotImplementedException();
        }
    }

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

        public override string[] processTuple(string[] tuple)
        {
            throw new NotImplementedException();
        }
    }
}
