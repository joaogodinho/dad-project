using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dad_project
{
    class Operator
    {
        public string Id { get; set; }
        public string[] Input { get; set; }
        public int RepFact { get; set; }
        public string Routing { get; set; }
        public string[] Addresses { get; set; }
        public OperatorSpec Operation { get; set; }

        public Operator(string id, string[] input, int rep, string routing, string[] addr, OperatorSpec opspec)
        {
            Id = id;
            Input = input;
            RepFact = rep;
            Routing = routing;
            Addresses = addr;
            Operation = opspec;
        }
    }

    public abstract class OperatorSpec
    {
        public string Type { get; set; }
    }

    public class OperatorUniq : OperatorSpec
    {
        public int Field { get; set; }
        public OperatorUniq(int field)
        {
            Type = "UNIQ";
            Field = field;
        }
    }

    public class OperatorCount : OperatorSpec
    {
        public OperatorCount()
        {
            Type = "COUNT";
        }
    }

    public class OperatorDup: OperatorSpec
    {
        public OperatorDup()
        {
            Type = "DUP";
        }
    }

    public class OperatorFilter: OperatorSpec
    {
        public int Field { get; set; }
        public string Condition { get; set; }
        public string Value { get; set; }
        public OperatorFilter(int field, string condition, string value)
        {
            Type = "FILTER";
            Field = field;
            Condition = condition;
            Value = value;
        }
    }

    public class OperatorCustom: OperatorSpec
    {
        public string Dll { get; set; }
        public string Class { get; set; }
        public string Method { get; set; }
        public OperatorCustom(string dll, string className, string method)
        {
            Type = "CUSTOM";
            Dll = dll;
            Class = className;
            Method = method;
        }
    }
}
