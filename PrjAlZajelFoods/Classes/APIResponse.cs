using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrjAlZajelFoods.Classes
{
    public class APIResponse
    {
        public class Data
        {
            public List<Hashtable> Body { get; set; }
            public Hashtable Header { get; set; }
            public List<Hashtable> Footer { get; set; }
        }

        public class Response
        {
            public string url { get; set; }
            public List<Data> data { get; set; }
            public int result { get; set; }
            public string message { get; set; }
        }
        public class PostResponse
        {
            public string url { get; set; }
            public List<Hashtable> data { get; set; }
            public int result { get; set; }
            public string message { get; set; }
        }
    }
}
