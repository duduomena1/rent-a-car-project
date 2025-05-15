using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fnPayment.Models
{
    internal class PaymentModel
    {
        public string Name { get; set; }
        public string Mail { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public string RentTime { get; set; }
        public DateTime Date { get; set; }
        public string id { get { return Guid.NewGuid().ToString(); } }
        public string idPayment { get { return Guid.NewGuid().ToString(); } }

        public string Status { get; set; }

        public DateTime? DateApproval { get; set; }
    }
}
