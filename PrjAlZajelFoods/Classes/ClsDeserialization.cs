using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrjAlZajelFoods.Classes
{
    public class ClsDeserialization
    {
        public class RootObject
        {
            public SaveVoucher2Result SaveVoucher2Result { get; set; }
        }

        public class SaveVoucher2Result
        {
            public List<string> arrTransIds { get; set; }
            public int lResult11 { get; set; }
            public string sValue { get; set; }
        }
        public class LoadVoucherResult
        {
            public List<List<object>> arrBody { get; set; }
            public object arrFooter { get; set; }
            public List<object> arrHeader { get; set; }
            public object arrsuspendBody { get; set; }
            public int iDefaultFloorId { get; set; }
            public int iLinkPath { get; set; }
            public int iLinkVoucherType { get; set; }
            public object lstAuthData { get; set; }
            public Result result { get; set; }
            public object sDefaultFloorName { get; set; }
            public object sGroupNo { get; set; }
            public object sSuspendDocNo { get; set; }
        }

        public class RootObjectLoad
        {
            public LoadVoucherResult LoadVoucherResult { get; set; }

        }
        public class Result
        {
            public int lResult { get; set; }
            public string sValue { get; set; }
        }
    }
}
