using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace backup
{
    class KQResponse
    {
        public string error { get; set; }


        public string message { get; set; }


        public string cmd { get; set; }


        public int rowEffected { get; set; }
        public Dictionary<string, string> paras { get; set; }

        public DataTable data { get; set; }

        public KQResponse()
        {
            error = "";
            message = "";
            rowEffected = 0;
            data = new DataTable();
            paras = new Dictionary<string, string>();

        }

    }
}
