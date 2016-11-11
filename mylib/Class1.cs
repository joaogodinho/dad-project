using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mylib
{
    public class Class1
    {

        public IList<string> HelloWorld(IList<string> tuple)
        {
            Console.WriteLine("Hello!");
            return tuple;
        }

    }
}
