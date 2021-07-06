using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace zedcrest.api.Models.Shared
{
    public class ActionResponseEntity<T>
    {
        public ActionResponseEntity()
        {
            Data = new List<T>();
            Status = false;
        }
        public bool Status { get; set; }
        public string Message { get; set; }
        public List<T> Data { get; set; }
    }
}