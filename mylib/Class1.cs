using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mylib
{
    public class Class1
    {

        public IList<IList<string>> HelloWorld(IList<IList<string>> tuple)
        {
            Console.WriteLine("Hello!");
            return tuple;
        }

    }
}
