using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUBETREKWPFV1.Classes
{
    public class NextTrain
    {
        public string line {  get; set; }
        public string fromstation { get; set; }

        public string selected { get ; set; }   

        public DateTime time { get; set; }
    }
}
