﻿using CommonCode.Interfaces;
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
        public abstract string[] processTuple(string[] tuple);
    }
    
    [Serializable]
    public class OperatorCount : OperatorSpec
    {
        public int CurrentCount = 0;
        public override string[] processTuple(string[] tuple)
        {
            int count = Interlocked.Increment(ref CurrentCount);
            return new string[] { count.ToString() };
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
            FieldNumber = fieldNumb;
            Hits = new List<string>();
        }
        public override string[] processTuple(string[] tuple)
        {
            if (Hits.Contains(tuple[FieldNumber]))
                return null;
            else Hits.Add(tuple[FieldNumber]);
            return tuple;

        }
    }

    [Serializable]
    public class OperatorDup : OperatorSpec
    {
        public override string[] processTuple(string[] tuple)
        {
            return tuple;
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
            Field = field;
            Condition = condition;
            Value = value;
        }
        public override string[] processTuple(string[] tuple)
        {
            return IsFilterAMatch(tuple, Field, Condition, Value) ? tuple : null;
        }

        private bool IsFilterAMatch(string[] tuple, int field, string condition, string compareTo)
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

        public override string[] processTuple(string[] tuple)
        {
            var DLL = Assembly.LoadFile(Dll);
            var type = Type.GetType(ClassName);
            var c = Activator.CreateInstance(Type.GetType(ClassName));
            var method = type.GetMethod(Method);
            var result = method.Invoke(c, new object[] { @"Hello" });
            return result as string[];
        }
    }
}