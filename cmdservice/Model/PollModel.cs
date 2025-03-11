using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmdservice.Model
{
    internal class PollModel
    {
        public class Root
        {
            public int id { get; set; }
            public string name { get; set; }
            public string value { get; set; }
            public bool active { get; set; }
        }
    }
}
