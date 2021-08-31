using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ML_Interpretation_Engine.Source.Requests
{
    public class User
    {
        public int id { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string role { get; set; }
        public string token { get; set; }
    }
}
