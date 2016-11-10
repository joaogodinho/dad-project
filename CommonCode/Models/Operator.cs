using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonCode.Models
{
    public class Operator
    {
        string id { get; set; }
        List<string> down_ips { get; set; }
        int port { get; set; }
        Tuple<string, string> routing { get; set; }
        OperatorSpec spec { get; set; }

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
        public OperatorFilter(int field, string condition)
        {
            Field = field;
            Condition = condition;
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
