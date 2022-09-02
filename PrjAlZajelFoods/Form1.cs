using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Configuration;
using Newtonsoft.Json;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Xml;
using System.IO;
using System.Collections;
using PrjAlZajelFoods.Classes;

namespace PrjAlZajelFoods
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //GetData();
            this.Opacity = 0;
            this.ShowInTaskbar = false;

            string Interval = ConfigurationManager.AppSettings["Interval"];
            string sInt_Interval = Interval;
            //TimeSpan now = DateTime.Now.TimeOfDay;
            int interval = Convert.ToInt32(sInt_Interval) * 60 * 1000;
            timer1.Interval = interval;
            timer1.Enabled = true;
            timer1.Tick += new EventHandler(timer1_Tick);

            this.Opacity = 0;
            this.ShowInTaskbar = false;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                GetData2();
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
        }

        int SIVal = 1;
        int SRVal = 1;
        int CashVal = 1;
        int PDCVal = 1;
        int ExpenseVal = 1;
        int PayVal = 1;
        static string Content = ConfigurationManager.AppSettings["Content"];
        public void GetData()
        {
            //Old Method of posting sequence. first posting all invoice. if all posted without any failure then cash rec. then pdc, then sales return then payment.If any of the posting got error, the whole cycle will stop there.
            GetExternalData clsd = new GetExternalData();

            DataSet dsSI = clsd.GetExSales(10); //For Both Types 10 and 11 given as 10 //Sales Invoices
            {
                #region If Section
                SIVal = PostingSI(dsSI);
                if (SIVal > 0)
                {
                    DataSet dsCash = clsd.GetExCash(1);//Cash
                    if (dsCash.Tables[0].Rows.Count > 0)
                    {
                        CashVal = PostingCash(dsCash);
                    }
                    if (CashVal > 0)
                    {
                        DataSet dsPDC = clsd.GetExCash(2);//PDC
                        if (dsPDC.Tables[0].Rows.Count > 0)
                        {
                            PDCVal = PostingPDC(dsPDC);
                        }
                        if (PDCVal > 0)
                        {
                            DataSet dsSR = clsd.GetExSales(12); // Sales Returns
                            if (dsSR.Tables[0].Rows.Count > 0)
                            {
                                SRVal = PostingReturn(dsSR);
                            }
                            if (SRVal > 0)
                            {
                                DataSet dsPayment = clsd.GetExCash(3);//Payment
                                if (dsPayment.Tables[0].Rows.Count > 0)
                                {
                                    PayVal = PostingCashPayment(dsPayment);
                                }
                                if (PayVal > 0)
                                {
                                    DataSet dsEEV = clsd.GetExEEV(1);//EEV
                                    if (dsEEV.Tables[0].Rows.Count > 0)
                                    {
                                        ExpenseVal = PostingEEV(dsEEV);
                                    }
                                }
                            }
                        }

                    }

                }
                #endregion
            }
           
        }
        public void GetData2()
        {
            //New method of posting order. The posting sequence is determining as per the document date.
            try
            {
                GetExternalData clsd = new GetExternalData();
                DataSet ds = clsd.GetAllTrans();
                int _postStatus = 1;
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        BL_Registry.SetLog("_postStatus = " + _postStatus.ToString());
                        if (_postStatus > 0)
                        {
                            DataSet dst = new DataSet();
                            if (Convert.ToInt32(dr["DocType"]) == 11 || Convert.ToInt32(dr["DocType"]) == 10)
                            {
                                //salesinv
                                dst = clsd.GetInv(dr["DocumentNumber"].ToString());
                                _postStatus = PostingSI(dst);
                            }
                            else if (Convert.ToInt32(dr["DocType"]) == 12)
                            {
                                //salesret
                                dst = clsd.GetRet(dr["DocumentNumber"].ToString());
                                _postStatus = PostingReturn(dst);
                            }
                            else if (Convert.ToInt32(dr["DocType"]) == 22)
                            {
                                //pdc
                                dst = clsd.GetPDC(dr["DocumentNumber"].ToString());
                                _postStatus = PostingPDC(dst);
                            }
                            else if (Convert.ToInt32(dr["DocType"]) == 21)
                            {
                                //cashrec
                                dst = clsd.GetCashRec(dr["DocumentNumber"].ToString());
                                _postStatus = PostingCash(dst);
                            }
                            else if (Convert.ToInt32(dr["DocType"]) == 23)
                            {
                                //payment
                                dst = clsd.GetPay(dr["DocumentNumber"].ToString());
                                _postStatus = PostingCashPayment(dst);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                BL_Registry.SetLogError(ex.Message);
            }
        }
        public string getServiceLink()
        {
            XmlDocument xmlDoc = new XmlDocument();
            string strFileName = "";
            string sAppPath = BL_Configdata.Focus8Path;
            strFileName = sAppPath + "\\ERPXML\\ServerSettings.xml";

            xmlDoc.Load(strFileName);
            XmlNodeList nodeList = xmlDoc.DocumentElement.SelectNodes("/ServSetting/MasterServer/ServerName");
            string strValue;
            XmlNode node = nodeList[0];
            if (node != null)
                strValue = node.InnerText;
            else
                strValue = "";
            return strValue;
        }
        string DoLogin()
        {
            HashData objHashRequest = new HashData();
            Hashtable objHash = new Hashtable();
            try
            {
                int compId = BL_Configdata.Focus8CompID;
                //string code= ConfigurationSettings.AppSettings["CompanyCode"];
                objHash.Add("Username", BL_Configdata.UserName);
                objHash.Add("password", BL_Configdata.Password);
                objHash.Add("CompanyId", compId);
                List<Hashtable> lstHash = new List<Hashtable>();
                lstHash.Add(objHash);
                objHashRequest.data = lstHash;
                string sContent = JsonConvert.SerializeObject(objHashRequest);
                using (var client = new WebClient())
                {
                    client.Headers.Add("Content-Type", "application/json");
                    //client.Headers.Add("fSessionId", cls.fnSessionId());
                    string sUrl = "http://" + getServiceLink() + "/Focus8API/login";
                    string strResponse = client.UploadString(sUrl, sContent);
                    HashData objHashResponse = JsonConvert.DeserializeObject<HashData>(strResponse);
                    //    clsConnection.Log("fSessionId - " + objHashResponse.data[0]["fSessionId"].ToString(), "LogGeneva");
                    return (objHashResponse.data[0]["fSessionId"].ToString());
                }
            }
            catch (Exception ex)
            {
                //  clsConnection.Log("Dologin - " + ex.ToString(), "LogGeneva");
                MessageBox.Show(ex.ToString());
            }
            return "";
        }
        public string GetSessionId(int CompId)
        {
            string sSessionId = "";
            try
            {
                string strServer = BL_Configdata.ServerAPIIP;//getServiceLink();
                BL_Registry.SetLog("strServer" + strServer);
                int ccode = CompId;
                string User_Name = BL_Configdata.UserName;
                string Password = BL_Configdata.Password;

                BL_Registry.SetLog("ccode" + ccode);
                BL_Registry.SetLog("User_Name" + User_Name);
                BL_Registry.SetLog("Password" + Password);

                BL_Registry.SetLog("Session Link: " + strServer + " / Login");
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(strServer + "/Login");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = "{" + "\"data\": [{" + "\"Username\":\"" + User_Name + "\"," + "\"password\":\"" + Password + "\"," + "\"CompanyId\":\"" + ccode + "\"}]}";
                    BL_Registry.SetLog("Session json: " + json);
                    streamWriter.Write(json);
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                StreamReader Updatereader = new StreamReader(httpResponse.GetResponseStream());
                string Udtcontent = Updatereader.ReadToEnd();

                JObject odtbj = JObject.Parse(Udtcontent);
                Temperatures Updtresult = JsonConvert.DeserializeObject<Temperatures>(Udtcontent);
                if (Updtresult.Result == 1)
                {
                    sSessionId = Updtresult.Data[0].FSessionId;
                }


                return sSessionId;
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            return sSessionId;
        }
        public class LoginInfo
        {
            public int iEmpId { get; set; }
            public int iLogId { get; set; }
            public int iUserId { get; set; }
            public string sLoginName { get; set; }
            public string sSession { get; set; }
        }
        public class Result
        {
            public object arrTransIds { get; set; }
            public int lResult { get; set; }
            public string sValue { get; set; }
        }
        public class LstMasterTypeSearchBy
        {
            public bool bSearchBy { get; set; }
            public int iMasterTypeId { get; set; }
        }
        public class UserLoginResult
        {
            public LoginInfo LoginInfo { get; set; }
            public Result Result { get; set; }
            public List<LstMasterTypeSearchBy> lstMasterTypeSearchBy { get; set; }
            public List<object> lstWMSMenus { get; set; }
        }
        public class RootObject
        {
            public UserLoginResult UserLoginResult { get; set; }
        }
        public partial class Temperatures
        {
            [JsonProperty("data")]
            public Datum[] Data { get; set; }

            [JsonProperty("url")]
            public Uri Url { get; set; }

            [JsonProperty("result")]
            public long Result { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }
        }
        public partial class Datum
        {
            [JsonProperty("fSessionId")]
            public string FSessionId { get; set; }
        }

        //Posting Methods
        string sessionID = "";
        GetExternalData cls = new GetExternalData();

        public int PostingStockTransactions(DataSet ds)
        {
            int compId = BL_Configdata.Focus8CompID;
            sessionID = GetSessionId(compId);//DoLogin();//GetSessionId(compId);
            BL_Registry.SetLog("sessionID" + sessionID);
            string Message = "";
            var val = 0;
            try
            {
                var distinctValues = ds.Tables[0].AsEnumerable().Select(_r => _r.Field<string>("DocumentNumber")).Distinct();
                foreach (var dist in distinctValues)
                {
                    string str = "DocumentNumber='" + dist + "'";
                    DataRow[] dr = ds.Tables[0].Select(str);
                    var dt = dr.FirstOrDefault();
                    var DocNo = dt["DocumentNumber"].ToString();
                    int date = BL_Registry.GetDateToInt(Convert.ToDateTime(dt["TransactionDate"].ToString()));
                    int DefAcc = GetAcctName(5378);
                    int WareIdso = GetMasterId(dt["SourceWarehouse"].ToString(), 1);
                    #region Header
                    Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "PartyAC", DefAcc },
                             { "Company Master",GetMasterId(dt["Branch"].ToString(),8) },
                             { "Outlet", WareIdso },
                             { "Currency", "7"},
                             { "ExchangeRate", "1" },
                         };
                    #endregion

                    #region Body
                    List<Hashtable> body = new List<Hashtable>();

                    foreach (var data in dr)
                    {
                        int iBodyCnt = 0;

                        int BatchChk = cls.BatchCheck(data["Batch"].ToString());
                        if (BatchChk == -1)
                        {
                            BL_Registry.SetLog("Batch is not available for this TransactionNumber " + DocNo);
                            break;
                        }
                        int ItemId = GetMasterId(data["Product"].ToString(), 4);
                        int unitId = GetMasterId(data["ProductUnit"].ToString(), 5);
                        #region BatchQuery
                        //DataSet dsBatch = cls.GetBatchData(data["Batch"].ToString(), ItemId, WareIdso, date, data["ProductUnit"].ToString());
                        DataSet dsBatch = cls.GetBatchData(data["Batch"].ToString(), ItemId, WareIdso, date, 1, "");
                        decimal dval = Convert.ToDecimal(data["Quantity"]);
                        decimal qty = Convert.ToDecimal(data["Quantity"]);

                        //We have added new logic to stop posting partial items/qty
                        decimal batchquantity = 0;
                        decimal staggingqty = Convert.ToDecimal(data["Quantity"]);
                        for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                        {
                            batchquantity = batchquantity + Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]);
                        }
                        if (batchquantity < staggingqty)
                        {
                            BL_Registry.SetLog("Batch (" + data["Batch"].ToString() + ") is not available for this TransactionNumber " + DocNo);
                            break;
                        }

                        if (dsBatch != null && dsBatch.Tables.Count > 0 && dsBatch.Tables[0].Rows.Count > 0)
                        {
                            for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                            {
                                if (Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]) >= Convert.ToDecimal(data["Quantity"]))
                                {
                                    iBodyCnt++;
                                    dval = 0;
                                    break;
                                }
                                else
                                {
                                    dval = dval - Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]);
                                    iBodyCnt++;
                                    if (dval <= 0) { break; }
                                }
                            }
                        }
                        else
                        {
                            BL_Registry.SetLog("Batch (" + data["Batch"].ToString() + ") is not available for this TransactionNumber " + DocNo);
                            break;
                        }
                        #endregion
                        if (iBodyCnt > 0)
                        {
                            for (int c = 0; c < iBodyCnt; c++)
                            {
                                int DefaultBaseUnit = Convert.ToInt32(dsBatch.Tables[0].Rows[c]["iDefaultBaseUnit"]);

                                decimal batchqty = (Convert.ToDecimal(dsBatch.Tables[0].Rows[c]["BatchQty"]));
                                decimal remainingQty = (batchqty - qty);
                                if (remainingQty < 0)
                                {
                                    qty = Math.Round(batchqty, 4);
                                }
                                // decimal Gross = qty * Convert.ToDecimal(data["Price"]);
                                Hashtable row = new Hashtable
                         {
                             { "Item__Id", ItemId},//data["ItemCode"]
                             { "Description", data["Notes"]},
                             { "StockAC__Id",GetStockAc(data["Product"].ToString())},
                             { "Unit__Id",unitId},// data["ItemUOM"]
                             { "Quantity", qty },
                             //{ "Rate", rate},
                             //{ "Batch",data["Batch"] }
                             { "Batch", data["Batch"] + "^" + dsBatch.Tables[0].Rows[c]["iBatchId"]},
                         };
                                body.Add(row);

                                if (remainingQty < 0)
                                {
                                    qty = -(remainingQty);
                                }
                            }
                        }
                    }
                    #endregion
                    if (body.Count() > 0)
                    {
                        string baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                        string Url = baseUrl + "/Transactions/Vouchers/Load Out";

                        var postingData = new PostingData();
                        postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                        string sContent = JsonConvert.SerializeObject(postingData);
                        string err = "";

                        var response = Focus8API.Post(Url, sContent, sessionID, ref err);
                        #region Response
                        if (response != null)
                        {
                            var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                            if (responseData.result == -1)
                            {

                                Message = "Load Out Posting Failed" + "\n";
                                BL_Registry.SetLog("Load Out Posted Failed with DocNo: " + DocNo);
                                val = 0;
                            }
                            else
                            {
                                Message = "Load Out  Posted Successfully" + "\n";
                                BL_Registry.SetLog("Load Out Posted Success with DocNo: " + DocNo);
                                string UpSql = $@"update StockTransaction set PostStatus=1 where DocumentNumber='{DocNo}'";
                                int res = cls.Update(UpSql);
                                if (res == 1)
                                {
                                    BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                }

                                #region LoadIn
                                #region HeaderSection
                                int InDefAcc = GetAcctName(2050);
                                Hashtable header1 = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "PartyAC", InDefAcc },
                             { "Company Master",GetMasterId(dt["Branch"].ToString(),8) },
                             { "Outlet",GetMasterId(dt["DestinationWarehouse"].ToString(),1)  },
                             { "Currency", "7"},
                             { "ExchangeRate", "1" },
                         };
                                #endregion

                                #region BodySection
                                List<Hashtable> body1 = new List<Hashtable>();

                                foreach (var data in dr)
                                {
                                    GetTranID(dt["DocumentNumber"].ToString(), GetMasterId(data["Product"].ToString(), 4), 1);
                                    int MfgDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["MfgDate"].ToString()));
                                    int ExpDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["ExpiryDate"].ToString()));

                                    Hashtable row = new Hashtable
                         {
                             { "Item", GetMasterId(data["Product"].ToString(), 4)},
                             { "Description", data["Notes"]},
                             { "PurchaseAC",  GetStockAc(data["Product"].ToString())},
                             { "Unit",  GetMasterId(data["ProductUnit"].ToString(), 5)},
                             { "Quantity", data["Quantity"] },
                             { "L-Load Out", TransID },
                             //{ "Rate",  data["ItemUOM"]},
                             //{ "Gross",  data["ItemUOM"]},
                             { "Batch",  data["Batch"]},
                             { "MfgDate",  MfgDate},
                             { "ExpDate",  ExpDate},
                         };
                                    body1.Add(row);
                                }
                                #endregion

                                baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                                Url = baseUrl + "/Transactions/Vouchers/Load In";

                                var postingData1 = new PostingData();
                                postingData1.data.Add(new Hashtable { { "Header", header1 }, { "Body", body1 } });
                                string sContent1 = JsonConvert.SerializeObject(postingData1);
                                err = "";
                                var response1 = Focus8API.Post(Url, sContent1, sessionID, ref err);
                                #region ResponseData
                                if (response1 != null)
                                {
                                    var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                                    if (responseData1.result == -1)
                                    {

                                        Message = "Load In Posting Failed" + "\n";
                                        BL_Registry.SetLogError("Load In Posted Failed with DocNo: " + DocNo);
                                        val = 0;
                                    }
                                    else
                                    {
                                        Message = "Load In  Posted Successfully" + "\n";
                                        BL_Registry.SetLog("Load In Posted Success with DocNo: " + DocNo);
                                        string UpdSql = $@"update StockTransaction set PostStatus=1 where DocumentNumber='{DocNo}'";
                                        int res1 = cls.Update(UpdSql);
                                        if (res1 == 1)
                                        {
                                            BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                        }
                                    }
                                }
                                #endregion
                                #endregion
                            }
                        }
                        #endregion
                    }
                }
                return val;
            }
            catch (Exception e)
            {
                BL_Registry.SetLog(e.ToString());
                return -1;
            }
        }

        public int CommonPosting(DataSet ds)
        {
            BL_Registry.SetLog("Entered CommonPOsting");
            bool outLoop = false;
            int compId = BL_Configdata.Focus8CompID;
            sessionID = GetSessionId(compId);//DoLogin();//GetSessionId(compId);
            BL_Registry.SetLog("GetSessionId" + sessionID);
            string Message = "";
            string Url = "";
            string baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
            var val = 0;
            try
            {
                var distinctValues = ds.Tables[0].AsEnumerable().Select(_r => _r.Field<string>("DocumentNumber")).Distinct();
                foreach (var dist in distinctValues)
                {
                    string str = "DocumentNumber='" + dist + "'";
                    DataRow[] dr = ds.Tables[0].Select(str);
                    var dt = dr.FirstOrDefault();
                    var DocNo = dt["DocumentNumber"].ToString();
                    int date = BL_Registry.GetDateToInt(Convert.ToDateTime(dt["TransactionDate"].ToString()));
                    #region Load Request
                    if (dt["Screen"].ToString() == "LoadRequest")
                    {
                        #region CheckCondition
                        int DefAcc = GetAcctName(5378);
                        int WareIdso = GetMasterId(dt["SourceWarehouse"].ToString(), 1);
                        int BranchId = GetMasterId(dt["Branch"].ToString(), 8);

                        if (WareIdso == -1)
                        {
                            BL_Registry.SetLogError("Outlet '" + dt["SourceWarehouse"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        if (BranchId == -1)
                        {
                            BL_Registry.SetLogError("Branch '" + dt["Branch"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        #endregion

                        #region Header
                        Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "PartyAC", DefAcc },
                             { "Company Master",BranchId },
                             { "Outlet", WareIdso },
                             { "Currency", "7"},
                             { "ExchangeRate", "1" },
                         };
                        #endregion

                        #region Body
                        List<Hashtable> body = new List<Hashtable>();

                        foreach (var data in dr)
                        {
                            int iBodyCnt = 0;

                            int BatchChk = cls.BatchCheck(data["Batch"].ToString());
                            if (BatchChk == -1)
                            {
                                BL_Registry.SetLogError("Batch is not available for this TransactionNumber " + DocNo);
                                outLoop = true;
                                break;
                            }
                            int ItemId = GetMasterId(data["Product"].ToString(), 4);
                            if (ItemId == -1)
                            {
                                BL_Registry.SetLogError("Item '" + data["Product"].ToString() + "' Not Found with DocNo: " + DocNo);
                                body = new List<Hashtable>();
                                outLoop = true;
                                break;
                            }
                            int unitId = GetMasterId(data["ProductUnit"].ToString(), 5);
                            if (unitId == -1)
                            {
                                BL_Registry.SetLogError("Unit '" + data["ProductUnit"].ToString() + "' Not Found with DocNo: " + DocNo);
                                body = new List<Hashtable>();
                                outLoop = true;
                                break;
                            }
                            #region BatchQuery
                            //DataSet dsBatch = cls.GetBatchData(data["Batch"].ToString(), ItemId, WareIdso, date, data["ProductUnit"].ToString());
                            DataSet dsBatch = cls.GetBatchData(data["Batch"].ToString(), ItemId, WareIdso, date, 1, "");
                            decimal dval = Convert.ToDecimal(data["Quantity"]);
                            decimal qty = Convert.ToDecimal(data["Quantity"]);

                            //We have added new logic to stop posting partial items/qty
                            decimal batchquantity = 0;
                            decimal staggingqty = Convert.ToDecimal(data["Quantity"]);
                            for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                            {
                                batchquantity = batchquantity + Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]);
                            }
                            if (batchquantity < staggingqty)
                            {
                                BL_Registry.SetLogError("Batch (" + data["Batch"].ToString() + ") is not available for this TransactionNumber " + DocNo);
                                outLoop = true;
                                break;
                            }

                            if (dsBatch != null && dsBatch.Tables.Count > 0 && dsBatch.Tables[0].Rows.Count > 0)
                            {
                                for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                                {
                                    if (Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]) >= Convert.ToDecimal(data["Quantity"]))
                                    {
                                        iBodyCnt++;
                                        dval = 0;
                                        break;
                                    }
                                    else
                                    {
                                        dval = dval - Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]);
                                        iBodyCnt++;
                                        if (dval <= 0) { break; }
                                    }
                                }
                            }
                            else
                            {
                                BL_Registry.SetLogError("Batch (" + data["Batch"].ToString() + ") is not available for this TransactionNumber " + DocNo);
                                outLoop = true;
                                break;
                            }
                            #endregion
                            if (iBodyCnt > 0)
                            {
                                for (int c = 0; c < iBodyCnt; c++)
                                {
                                    int DefaultBaseUnit = Convert.ToInt32(dsBatch.Tables[0].Rows[c]["iDefaultBaseUnit"]);

                                    decimal batchqty = (Convert.ToDecimal(dsBatch.Tables[0].Rows[c]["BatchQty"]));
                                    decimal remainingQty = (batchqty - qty);
                                    if (remainingQty < 0)
                                    {
                                        qty = Math.Round(batchqty, 4);
                                    }
                                    // decimal Gross = qty * Convert.ToDecimal(data["Price"]);
                                    int StockAc = GetStockAc(data["Product"].ToString());
                                    if (StockAc == -1)
                                    {
                                        BL_Registry.SetLogError("StockAc not mapped to '" + data["Product"].ToString() + "' with DocNo: " + DocNo);
                                        body = new List<Hashtable>();
                                        outLoop = true;
                                        break;
                                    }
                                    Hashtable row = new Hashtable
                         {
                             { "Item__Id", ItemId},//data["ItemCode"]
                             { "Description", data["Notes"]},
                             { "StockAC__Id",StockAc},
                             { "Unit__Id",unitId},// data["ItemUOM"]
                             { "Quantity", qty },
                             //{ "Rate", rate},
                             //{ "Batch",data["Batch"] }
                             { "Batch", data["Batch"] + "^" + dsBatch.Tables[0].Rows[c]["iBatchId"]},
                         };
                                    body.Add(row);

                                    if (remainingQty < 0)
                                    {
                                        qty = -(remainingQty);
                                    }
                                }
                            }
                        }
                        #endregion
                        if (body.Count() > 0)
                        {
                            string LObaseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                            string LOUrl = LObaseUrl + "/Transactions/Vouchers/Load Out";

                            var postingData = new PostingData();
                            postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                            string sContent = JsonConvert.SerializeObject(postingData);
                            string err = "";

                            var response = Focus8API.Post(LOUrl, sContent, sessionID, ref err);
                            #region Response
                            if (response != null)
                            {
                                var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                                if (responseData.result == -1)
                                {

                                    Message = "Load Out Posting Failed" + "\n";
                                    BL_Registry.SetLogError("Load Out Posted Failed with DocNo: " + DocNo);
                                    val = 0;
                                }
                                else
                                {
                                    Message = "Load Out  Posted Successfully" + "\n";
                                    BL_Registry.SetLog("Load Out Posted Success with DocNo: " + DocNo);
                                    string UpSql = $@"update StockTransaction set PostStatus=1 where DocumentNumber='{DocNo}'";
                                    int res = cls.Update(UpSql);
                                    if (res == 1)
                                    {
                                        BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                    }

                                    #region LoadIn
                                    #region HeaderSection
                                    int InDefAcc = GetAcctName(2050);
                                    Hashtable header1 = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "PartyAC", InDefAcc },
                             { "Company Master",GetMasterId(dt["Branch"].ToString(),8) },
                             { "Outlet",GetMasterId(dt["DestinationWarehouse"].ToString(),1)  },
                             { "Currency", "7"},
                             { "ExchangeRate", "1" },
                         };
                                    #endregion

                                    #region BodySection
                                    List<Hashtable> body1 = new List<Hashtable>();

                                    foreach (var data in dr)
                                    {
                                        GetTranID(dt["DocumentNumber"].ToString(), GetMasterId(data["Product"].ToString(), 4), 1);
                                        int MfgDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["MfgDate"].ToString()));
                                        int ExpDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["ExpiryDate"].ToString()));

                                        int ItemId = GetMasterId(data["Product"].ToString(), 4);
                                        int StockAc = GetStockAc(data["Product"].ToString());
                                        int UnitId = GetMasterId(data["ProductUnit"].ToString(), 5);

                                        Hashtable row = new Hashtable
                         {
                             { "Item",ItemId },
                             { "Description", data["Notes"]},
                             { "PurchaseAC", StockAc },
                             { "Unit", UnitId },
                             { "Quantity", data["Quantity"] },
                             { "L-Load Out", TransID },
                             //{ "Rate",  data["ItemUOM"]},
                             //{ "Gross",  data["ItemUOM"]},
                             { "Batch",  data["Batch"]},
                             { "MfgDate",  MfgDate},
                             { "ExpDate",  ExpDate},
                         };
                                        body1.Add(row);
                                    }
                                    #endregion

                                    baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                                    Url = baseUrl + "/Transactions/Vouchers/Load In";

                                    var postingData1 = new PostingData();
                                    postingData1.data.Add(new Hashtable { { "Header", header1 }, { "Body", body1 } });
                                    string sContent1 = JsonConvert.SerializeObject(postingData1);
                                    err = "";
                                    var response1 = Focus8API.Post(Url, sContent1, sessionID, ref err);
                                    #region ResponseData
                                    if (response1 != null)
                                    {
                                        var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                                        if (responseData1.result == -1)
                                        {

                                            Message = "Load In Posting Failed" + "\n";
                                            BL_Registry.SetLogError("Load In Posted Failed with DocNo: " + DocNo);
                                            val = 0;
                                        }
                                        else
                                        {
                                            Message = "Load In  Posted Successfully" + "\n";
                                            BL_Registry.SetLog("Load In Posted Success with DocNo: " + DocNo);
                                            string UpdSql = $@"update StockTransaction set PostStatus=1 where DocumentNumber='{DocNo}'";
                                            int res1 = cls.Update(UpdSql);
                                            if (res1 == 1)
                                            {
                                                BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                            }
                                        }
                                    }
                                    #endregion
                                    #endregion
                                }
                            }
                            #endregion
                        }
                    }
                    #endregion

                    #region Stock Transfer from Van
                    else if (dt["Screen"].ToString() == "Transfer")
                    {
                        #region CheckCondition
                        GetDefAcc(5377);
                        //getting Masters ID

                        int WareId = GetMasterId(dt["SourceWarehouse"].ToString(), 1);
                        if (WareId == -1)
                        {
                            BL_Registry.SetLogError("Outlet '" + dt["SourceWarehouse"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int DWareId = GetMasterId(dt["DestinationWarehouse"].ToString(), 1);
                        if (DWareId == -1)
                        {
                            BL_Registry.SetLogError("Outlet '" + dt["DestinationWarehouse"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int BranchId = GetMasterId(dt["Branch"].ToString(), 8);
                        if (BranchId == -1)
                        {
                            BL_Registry.SetLogError("Company Master '" + dt["Branch"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int CurId = GetMasterId("AED", 3);
                        if (CurId == -1)
                        {
                            BL_Registry.SetLogError("Currency Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        #endregion

                        #region Header
                        Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "PartyAC", iDefAcc2 },
                             { "Company Master",BranchId},
                             { "Outlet",WareId },
                             { "Currency", CurId},
                             { "ExchangeRate", "1" },
                         };
                        #endregion

                        #region Body
                        List<Hashtable> body = new List<Hashtable>();

                        foreach (var data in dr)
                        {
                            int BatchChk = cls.BatchCheck(data["Batch"].ToString());
                            if (BatchChk == 0)
                            {
                                BL_Registry.SetLogError("Batch is not available for this TransactionNumber " + DocNo);
                                outLoop = true;
                                break;
                            }

                            int ItemId = GetMasterId(data["Product"].ToString(), 4);
                            if (ItemId == 0)
                            {
                                BL_Registry.SetLogError("Product '" + data["Product"].ToString() + "' Not Found with DocNo: " + DocNo);
                                outLoop = true;
                                break;
                            }
                            int unitId = GetMasterId(data["ProductUnit"].ToString(), 5);
                            if (unitId == 0)
                            {
                                BL_Registry.SetLogError("ProductUnit '" + data["ProductUnit"].ToString() + "' Not Found with DocNo: " + DocNo);
                                outLoop = true;
                                break;
                            }
                            int StockAc = GetStockAc(data["Product"].ToString());
                            if (StockAc == 0)
                            {
                                BL_Registry.SetLogError("Stock Account not mapped for Product '" + data["Product"].ToString() + "' with DocNo: " + DocNo);
                                outLoop = true;
                                break;
                            }
                            #region BatchQuery
                            //DataSet dsBatch = cls.GetBatchData(data["Batch"].ToString(), ItemId, WareId, date, data["ProductUnit"].ToString());
                            DataSet dsBatch = cls.GetBatchData(data["Batch"].ToString(), ItemId, WareId, date, 1, "");
                            decimal dval = Convert.ToDecimal(data["Quantity"]);
                            decimal qty = Convert.ToDecimal(data["Quantity"]);
                            int iBodyCnt = 0;

                            //We have added new logic to stop posting partial items/qty
                            decimal batchquantity = 0;
                            decimal staggingqty = Convert.ToDecimal(data["Quantity"]);
                            for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                            {
                                batchquantity = batchquantity + Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]);
                            }
                            if (batchquantity < staggingqty)
                            {
                                BL_Registry.SetLogError("Batch (" + data["Batch"].ToString() + ") is not available for this TransactionNumber " + DocNo);
                                outLoop = true;
                                break;
                            }

                            if (dsBatch != null && dsBatch.Tables.Count > 0 && dsBatch.Tables[0].Rows.Count > 0)
                            {
                                for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                                {
                                    if (Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]) >= Convert.ToDecimal(data["Quantity"]))
                                    {
                                        iBodyCnt++;
                                        dval = 0;
                                        break;
                                    }
                                    else
                                    {
                                        dval = dval - Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]);
                                        iBodyCnt++;
                                        if (dval <= 0) { break; }
                                    }
                                }
                            }
                            else
                            {
                                BL_Registry.SetLogError("Batch (" + data["Batch"].ToString() + ") is not available for this TransactionNumber " + DocNo);
                                outLoop = true;
                                break;
                            }
                            #endregion
                            if (iBodyCnt > 0)
                            {
                                for (int c = 0; c < iBodyCnt; c++)
                                {
                                    int DefaultBaseUnit = Convert.ToInt32(dsBatch.Tables[0].Rows[c]["iDefaultBaseUnit"]);

                                    decimal batchqty = (Convert.ToDecimal(dsBatch.Tables[0].Rows[c]["BatchQty"]));
                                    decimal remainingQty = (batchqty - qty);
                                    if (remainingQty < 0)
                                    {
                                        qty = Math.Round(batchqty, 4);
                                    }
                                    // decimal Gross = qty * Convert.ToDecimal(data["Price"]);
                                    Hashtable row = new Hashtable
                         {
                             { "Item__Id", ItemId},//data["ItemCode"]
                             { "Description", data["Notes"]},
                             { "StockAC__Id",StockAc},
                             { "Unit__Id",unitId},// data["ItemUOM"]
                             { "Quantity", qty },
                             //{ "Rate", rate},
                             //{ "Batch",data["Batch"] }
                             { "Batch", data["Batch"] + "^" + dsBatch.Tables[0].Rows[c]["iBatchId"]},
                         };
                                    body.Add(row);

                                    if (remainingQty < 0)
                                    {
                                        qty = -(remainingQty);
                                    }
                                }
                            }
                        }
                        #endregion
                        if (body.Count() > 0)
                        {
                            string TobaseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                            string ToUrl = TobaseUrl + "/Transactions/Vouchers/Transfer Out";

                            var postingData = new PostingData();
                            postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                            string sContent = JsonConvert.SerializeObject(postingData);
                            string err = "";

                            var response = Focus8API.Post(ToUrl, sContent, sessionID, ref err);
                            #region Response
                            if (response != null)
                            {
                                var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                                if (responseData.result == -1)
                                {
                                    Message = "Transfer Out Posting Failed" + "\n";
                                    BL_Registry.SetLogError("Transfer Out Posted Failed with DocNo: " + DocNo);
                                    //val = 0;
                                }
                                else
                                {
                                    Message = "Transfer Out Posted Successfully" + "\n";
                                    BL_Registry.SetLog("Transfer Out Posted Success with DocNo: " + DocNo);
                                    string UpSql = $@"update StockTransaction set PostStatus=1 where DocumentNumber='{DocNo}'";
                                    int res = cls.Update(UpSql);
                                    if (res == 1)
                                    {
                                        BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                    }

                                    #region TransferIn
                                    #region HeaderSection
                                    int InDefAcc = GetAcctName(2049);

                                    Hashtable header1 = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "PartyAC", InDefAcc },
                             { "Company Master",BranchId },
                             { "Outlet",DWareId  },
                             { "Currency", CurId},
                             { "ExchangeRate", "1" },
                         };
                                    #endregion

                                    #region BodySection
                                    List<Hashtable> body1 = new List<Hashtable>();

                                    foreach (var data in dr)
                                    {
                                        GetTranID(dt["DocumentNumber"].ToString(), GetMasterId(data["Product"].ToString(), 4), 1);
                                        int MfgDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["MfgDate"].ToString()));
                                        int ExpDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["ExpiryDate"].ToString()));

                                        Hashtable row = new Hashtable
                         {
                             { "Item", GetMasterId(data["Product"].ToString(), 4)},
                             { "Description", data["Notes"]},
                             { "PurchaseAC",  GetStockAc(data["Product"].ToString())},
                             { "Unit",  GetMasterId(data["ProductUnit"].ToString(), 5)},
                             { "Quantity", data["Quantity"] },
                             { "L-Transfer Out", TransID },
                             //{ "Rate",  data["ItemUOM"]},
                             //{ "Gross",  data["ItemUOM"]},
                             { "Batch",  data["Batch"]},
                             { "MfgDate",  MfgDate},
                             { "ExpDate",  ExpDate},
                         };
                                        body1.Add(row);
                                    }
                                    #endregion

                                    baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                                    Url = baseUrl + "/Transactions/Vouchers/Transfer In";

                                    var postingData1 = new PostingData();
                                    postingData1.data.Add(new Hashtable { { "Header", header1 }, { "Body", body1 } });
                                    string sContent1 = JsonConvert.SerializeObject(postingData1);
                                    err = "";
                                    var response1 = Focus8API.Post(Url, sContent1, sessionID, ref err);
                                    #region ResponseData
                                    if (response1 != null)
                                    {
                                        var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                                        if (responseData1.result == -1)
                                        {

                                            Message = "Transfer In Posting Failed" + "\n";
                                            BL_Registry.SetLog("Transfer In Posted Failed with DocNo: " + DocNo);
                                            val = 0;
                                        }
                                        else
                                        {
                                            Message = "Transfer In  Posted Successfully" + "\n";
                                            BL_Registry.SetLog("Transfer In Posted Success with DocNo: " + DocNo);
                                            string UpdSql = $@"update StockTransaction set PostStatus=1 where DocumentNumber='{DocNo}'";
                                            int res1 = cls.Update(UpdSql);
                                            if (res1 == 1)
                                            {
                                                BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                            }
                                        }
                                    }
                                    #endregion
                                    #endregion
                                }
                            }
                            #endregion
                        }
                    }
                    #endregion

                    #region Stock offload from Van
                    else if (dt["Screen"].ToString() == "OffLoad")
                    {
                        #region CheckConditions
                        GetDefAcc(5379);
                        //getting Masters ID
                        int WareId = GetMasterId(dt["SourceWarehouse"].ToString(), 1);
                        if (WareId == -1)
                        {
                            BL_Registry.SetLogError("Outlet '" + dt["SourceWarehouse"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int DWareId = GetMasterId(dt["DestinationWarehouse"].ToString(), 1);
                        if (DWareId == -1)
                        {
                            BL_Registry.SetLogError("Outlet '" + dt["DestinationWarehouse"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int BranchId = GetMasterId(dt["Branch"].ToString(), 8);
                        if (BranchId == -1)
                        {
                            BL_Registry.SetLogError("Company Master '" + dt["Branch"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int CurId = GetMasterId("AED", 3);
                        if (CurId == -1)
                        {
                            BL_Registry.SetLogError("Currency Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        #endregion

                        #region Header
                        Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "PartyAC", iDefAcc2 },
                             { "Company Master",BranchId},
                             { "Outlet",WareId },
                             { "Currency", CurId},
                             { "ExchangeRate", "1" },
                         };
                        #endregion

                        #region Body
                        List<Hashtable> body = new List<Hashtable>();

                        foreach (var data in dr)
                        {
                            int BatchChk = cls.BatchCheck(data["Batch"].ToString());
                            if (BatchChk == 0)
                            {
                                BL_Registry.SetLogError("Batch is not available for this TransactionNumber " + DocNo);
                                outLoop = true;
                                break;
                            }
                            int ItemId = GetMasterId(data["Product"].ToString(), 4);
                            if (ItemId == 0)
                            {
                                BL_Registry.SetLogError("Product '" + data["Product"].ToString() + "' Not Found with DocNo: " + DocNo);
                                outLoop = true;
                                break;
                            }
                            int unitId = GetMasterId(data["ProductUnit"].ToString(), 5);
                            if (unitId == 0)
                            {
                                BL_Registry.SetLogError("ProductUnit '" + data["ProductUnit"].ToString() + "' Not Found with DocNo: " + DocNo);
                                outLoop = true;
                                break;
                            }
                            int StockAc = GetStockAc(data["Product"].ToString());
                            if (StockAc == 0)
                            {
                                BL_Registry.SetLogError("Stock Account not mapped for Product '" + data["Product"].ToString() + "' with DocNo: " + DocNo);
                                outLoop = true;
                                break;
                            }
                            #region BatchQuery
                            //DataSet dsBatch = cls.GetBatchData(data["Batch"].ToString(), ItemId, WareId, date, data["ProductUnit"].ToString());
                            DataSet dsBatch = cls.GetBatchData(data["Batch"].ToString(), ItemId, WareId, date, 1, "");
                            decimal dval = Convert.ToDecimal(data["Quantity"]);
                            decimal qty = Convert.ToDecimal(data["Quantity"]);
                            int iBodyCnt = 0;

                            //We have added new logic to stop posting partial items/qty
                            decimal batchquantity = 0;
                            decimal staggingqty = Convert.ToDecimal(data["Quantity"]);
                            for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                            {
                                batchquantity = batchquantity + Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]);
                            }
                            if (batchquantity < staggingqty)
                            {
                                BL_Registry.SetLogError("Batch (" + data["Batch"].ToString() + ") is not available for this TransactionNumber " + DocNo);
                                outLoop = true;
                                break;
                            }

                            if (dsBatch != null && dsBatch.Tables.Count > 0 && dsBatch.Tables[0].Rows.Count > 0)
                            {
                                for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                                {
                                    if (Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]) >= Convert.ToDecimal(data["Quantity"]))
                                    {
                                        iBodyCnt++;
                                        dval = 0;
                                        break;
                                    }
                                    else
                                    {
                                        dval = dval - Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]);
                                        iBodyCnt++;
                                        if (dval <= 0) { break; }
                                    }
                                }
                            }
                            else
                            {
                                BL_Registry.SetLogError("Batch (" + data["Batch"].ToString() + ") is not available for this TransactionNumber " + DocNo);
                                outLoop = true;
                                break;
                            }
                            #endregion
                            if (iBodyCnt > 0)
                            {
                                for (int c = 0; c < iBodyCnt; c++)
                                {
                                    int DefaultBaseUnit = Convert.ToInt32(dsBatch.Tables[0].Rows[c]["iDefaultBaseUnit"]);

                                    decimal batchqty = (Convert.ToDecimal(dsBatch.Tables[0].Rows[c]["BatchQty"]));
                                    decimal remainingQty = (batchqty - qty);
                                    if (remainingQty < 0)
                                    {
                                        qty = Math.Round(batchqty, 4);
                                    }
                                    // decimal Gross = qty * Convert.ToDecimal(data["Price"]);
                                    Hashtable row = new Hashtable
                         {
                             { "Item__Id", ItemId},//data["ItemCode"]
                             { "Description", data["Notes"]},
                             { "StockAC__Id",StockAc},
                             { "Unit__Id",unitId},// data["ItemUOM"]
                             { "Quantity", qty },
                             //{ "Rate", rate},
                             //{ "Batch",data["Batch"] }
                             { "Batch", data["Batch"] + "^" + dsBatch.Tables[0].Rows[c]["iBatchId"]},
                         };
                                    body.Add(row);

                                    if (remainingQty < 0)
                                    {
                                        qty = -(remainingQty);
                                    }
                                }
                            }
                        }
                        #endregion
                        if (body.Count() > 0)
                        {
                            string OLbaseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                            string OLUrl = OLbaseUrl + "/Transactions/Vouchers/Off Load Out";

                            var postingData = new PostingData();
                            postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                            string sContent = JsonConvert.SerializeObject(postingData);
                            string err = "";

                            var response = Focus8API.Post(OLUrl, sContent, sessionID, ref err);
                            #region Response
                            if (response != null)
                            {
                                var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                                if (responseData.result == -1)
                                {
                                    Message = "Off Load Out Posting Failed" + "\n";
                                    BL_Registry.SetLogError("Off Load Out Posted Failed with DocNo: " + DocNo);
                                    //val = 0;
                                }
                                else
                                {
                                    Message = "Off Load Out Posted Successfully" + "\n";
                                    BL_Registry.SetLog("Off Load Out Posted Success with DocNo: " + DocNo);
                                    string UpSql = $@"update StockTransaction set PostStatus=1 where DocumentNumber='{DocNo}'";
                                    int res = cls.Update(UpSql);
                                    if (res == 1)
                                    {
                                        BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                    }

                                    #region OffLoadIn
                                    #region HeaderSection
                                    int InDefAcc = GetAcctName(2051);

                                    Hashtable header1 = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "PartyAC", InDefAcc },
                             { "Company Master",BranchId },
                             { "Outlet",DWareId  },
                             { "Currency", CurId},
                             { "ExchangeRate", "1" },
                         };
                                    #endregion

                                    #region BodySection
                                    List<Hashtable> body1 = new List<Hashtable>();

                                    foreach (var data in dr)
                                    {
                                        GetTranID(dt["DocumentNumber"].ToString(), GetMasterId(data["Product"].ToString(), 4), 1);
                                        int MfgDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["MfgDate"].ToString()));
                                        int ExpDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["ExpiryDate"].ToString()));

                                        int ItemId = GetMasterId(data["Product"].ToString(), 4);
                                        if (ItemId == -1)
                                        {
                                            BL_Registry.SetLogError("Outlet '" + dt["Product"].ToString() + "' Not Found with DocNo: " + DocNo);
                                            outLoop = true;
                                            break;
                                        }
                                        int StockAcId = GetStockAc(data["Product"].ToString());
                                        if (StockAcId == -1)
                                        {
                                            BL_Registry.SetLogError("Account not mapped to Item '" + dt["Product"].ToString() + "' with DocNo: " + DocNo);
                                            outLoop = true;
                                            break;
                                        }
                                        int UnitId = GetMasterId(data["ProductUnit"].ToString(), 5);
                                        if (UnitId == -1)
                                        {
                                            BL_Registry.SetLogError("Unit '" + dt["ProductUnit"].ToString() + "' Not Found with DocNo: " + DocNo);
                                            outLoop = true;
                                            break;
                                        }

                                        Hashtable row = new Hashtable
                         {
                             { "Item",ItemId },
                             { "Description", data["Notes"]},
                             { "PurchaseAC", StockAcId },
                             { "Unit", UnitId },
                             { "Quantity", data["Quantity"] },
                             { "L-Off Load Out", TransID },
                             //{ "Rate",  data["ItemUOM"]},
                             //{ "Gross",  data["ItemUOM"]},
                             { "Batch",  data["Batch"]},
                             { "MfgDate",  MfgDate},
                             { "ExpDate",  ExpDate},
                         };
                                        body1.Add(row);
                                    }
                                    #endregion

                                    baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                                    Url = baseUrl + "/Transactions/Vouchers/Off Load In";

                                    var postingData1 = new PostingData();
                                    postingData1.data.Add(new Hashtable { { "Header", header1 }, { "Body", body1 } });
                                    string sContent1 = JsonConvert.SerializeObject(postingData1);
                                    err = "";
                                    var response1 = Focus8API.Post(Url, sContent1, sessionID, ref err);
                                    #region ResponseData
                                    if (response1 != null)
                                    {
                                        var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                                        if (responseData1.result == -1)
                                        {

                                            Message = "Off Load In Posting Failed" + "\n";
                                            BL_Registry.SetLogError("Off Load In Posted Failed with DocNo: " + DocNo);
                                            val = 0;
                                        }
                                        else
                                        {
                                            Message = "Off Load In  Posted Successfully" + "\n";
                                            BL_Registry.SetLog("Off Load In Posted Success with DocNo: " + DocNo);
                                            string UpdSql = $@"update StockTransaction set PostStatus=1 where DocumentNumber='{DocNo}'";
                                            int res1 = cls.Update(UpdSql);
                                            if (res1 == 1)
                                            {
                                                BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                            }
                                        }
                                    }
                                    #endregion
                                    #endregion
                                }
                            }
                            #endregion
                        }
                    }
                    #endregion

                    #region Invoice Van Sales
                    else if (dt["Screen"].ToString() == "Sales Invoices")
                    {
                        #region CheckConditions
                        int Duedate = date;// BL_Registry.GetDateToInt(Convert.ToDateTime(dt["DueDate"].ToString()));
                        int CurId = GetMasterId(dt["Currency"].ToString(), 3);
                        if (CurId == -1)
                        {
                            BL_Registry.SetLogError("Currency '" + dt["Currency"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int custAcId = GetMasterId(dt["Customer"].ToString(), 6);
                        if (custAcId == -1)
                        {
                            BL_Registry.SetLogError("Customer '" + dt["Customer"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int OutletId = GetMasterId(dt["SourceWarehouse"].ToString(), 17);//Outlet
                        if (OutletId == -1)
                        {
                            BL_Registry.SetLogError("Outlet '" + dt["SourceWarehouse"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int CompanyMasterId = GetMasterId(dt["Branch"].ToString(), 8); //Companymaster
                        if (CompanyMasterId == -1)
                        {
                            BL_Registry.SetLogError("Company '" + dt["Branch"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int DriverId = GetMasterId(dt["Driver"].ToString(), 9);
                        if (DriverId == -1)
                        {
                            BL_Registry.SetLogError("Driver '" + dt["Driver"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int EmployeeId = GetMasterId(dt["Employee"].ToString(), 10);
                        if (EmployeeId == -1)
                        {
                            BL_Registry.SetLogError("Employee '" + dt["Employee"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int HelperId = GetMasterId(dt["Helper"].ToString(), 11);
                        if (HelperId == -1)
                        {
                            BL_Registry.SetLogError("Helper '" + dt["Helper"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int JurisId = GetMasterId(dt["Jurisdiction"].ToString(), 12);
                        if (JurisId == -1)
                        {
                            BL_Registry.SetLogError("Jurisdiction '" + dt["Jurisdiction"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int PosId = GetMasterId(dt["PlaceOfSupply"].ToString(), 13);
                        if (PosId == -1)
                        {
                            BL_Registry.SetLogError("PlaceOfSupply '" + dt["PlaceOfSupply"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        #endregion

                        #region Header
                        Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "CustomerAC", custAcId },
                             { "DueDate",Duedate },
                             { "Currency", CurId },
                             { "Company Master", CompanyMasterId },
                             { "Outlet", OutletId },
                             { "Employee", EmployeeId },
                             { "Driver", DriverId },
                             { "Helper", HelperId },
                             { "Jurisdiction", JurisId },
                             { "Place of supply", PosId },
                             { "LPO_No", dt["LPONumber"].ToString()},
                             { "Driver_Mobile_No", dt["DriverMobileNumber"].ToString() },
                         };
                        #endregion

                        List<Hashtable> body = new List<Hashtable>();
                        #region Body
                        foreach (var data in dr)
                        {
                            int BatchChk = cls.BatchCheck(data["Batch"].ToString());
                            if (BatchChk == 0)
                            {
                                BL_Registry.SetLogError("Batch is not available for this TransactionNumber " + DocNo);
                                outLoop = true;
                                break;
                                //continue;
                            }
                            else
                            {
                                int WareId = GetMasterId(dt["SourceWarehouse"].ToString(), 17);//SourceWarehouse
                                int ItemId = GetMasterId(data["Product"].ToString(), 4);
                                if (ItemId == 0)
                                {
                                    BL_Registry.SetLogError("Product '" + data["Product"].ToString() + "' Not Found with DocNo: " + DocNo);
                                    outLoop = true;
                                    break;
                                }
                                int unitId = GetMasterId(data["ProductUnit"].ToString(), 5);
                                if (unitId == 0)
                                {
                                    BL_Registry.SetLogError("ProductUnit '" + data["ProductUnit"].ToString() + "' Not Found with DocNo: " + DocNo);
                                    outLoop = true;
                                    break;
                                }
                                int TaxCode = GetMasterId(data["TaxCode"].ToString(), 2);
                                if (TaxCode == 0)
                                {
                                    BL_Registry.SetLogError("TaxCode '" + data["TaxCode"].ToString() + "' Not Found with DocNo: " + DocNo);
                                    outLoop = true;
                                    break;
                                }
                                int SalesAc = GetSalesAc(data["Product"].ToString());
                                if (SalesAc == 0)
                                {
                                    BL_Registry.SetLogError("SalesAc not mapped to Item '" + data["Product"].ToString() + "' with DocNo: " + DocNo);
                                    outLoop = true;
                                    break;
                                }
                                //string TaxCode = "EX";
                                //if (taxamt > 0)
                                //{
                                //    TaxCode = "SR";
                                //}
                                #region BatchQuery

                                //DataSet dsBatch = cls.GetBatchData(data["Batch"].ToString(), ItemId, WareId, date, data["ProductUnit"].ToString());
                                DataSet dsBatch = cls.GetBatchData(data["Batch"].ToString(), ItemId, WareId, date, 1, "");
                                decimal dval = Convert.ToDecimal(data["Quantity"]);
                                decimal qty = Convert.ToDecimal(data["Quantity"]);
                                int iBodyCnt = 0;

                                //We have added new logic to stop posting partial items/qty
                                decimal batchquantity = 0;
                                decimal staggingqty = Convert.ToDecimal(data["Quantity"]);
                                for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                                {
                                    batchquantity = batchquantity + Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]);
                                }
                                if (batchquantity < staggingqty)
                                {
                                    BL_Registry.SetLogError("Batch (" + data["Batch"].ToString() + ") is not available for this TransactionNumber " + DocNo);
                                    outLoop = true;
                                    break;
                                }

                                if (dsBatch != null && dsBatch.Tables.Count > 0 && dsBatch.Tables[0].Rows.Count > 0)
                                {
                                    for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                                    {
                                        if (Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]) >= Convert.ToDecimal(data["Quantity"]))
                                        {
                                            iBodyCnt++;
                                            dval = 0;
                                            break;
                                        }
                                        else
                                        {
                                            dval = dval - Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]);
                                            iBodyCnt++;
                                            if (dval <= 0) { break; }
                                        }
                                    }
                                }
                                else
                                {
                                    BL_Registry.SetLogError("Batch (" + data["Batch"].ToString() + ") is not available for this TransactionNumber " + DocNo);
                                    outLoop = true;
                                    break;
                                }
                                #endregion
                                if (iBodyCnt > 0)
                                {
                                    for (int c = 0; c < iBodyCnt; c++)
                                    {
                                        int DefaultBaseUnit = Convert.ToInt32(dsBatch.Tables[0].Rows[c]["iDefaultBaseUnit"]);

                                        decimal batchqty = (Convert.ToDecimal(dsBatch.Tables[0].Rows[c]["BatchQty"]));
                                        decimal remainingQty = (batchqty - qty);
                                        if (remainingQty < 0)
                                        {
                                            qty = Math.Round(batchqty, 2);
                                        }
                                        //  decimal Gross = qty * Convert.ToDecimal(data["Price"]);

                                        #region Body
                                        decimal rate = Convert.ToDecimal(data["SellingPrice"]);
                                        decimal gross = qty * rate;
                                        decimal TotalDisc = (gross * Convert.ToDecimal(data["DiscPerc"])) / 100;
                                        Hashtable row = new Hashtable
                         {
                             { "Item", ItemId},
                             { "Description", data["Notes"].ToString()},
                             { "TaxCode__Id", TaxCode},
                             { "SalesAC__Id", SalesAc},//need to change iDefAcc2
                             { "Unit__Id",  unitId},
                             { "Actual Quantity", data["ActualQuantity"] },
                             { "FOC Quantity", data["FOCQuantity"] },
                             { "Quantity", qty},
                             { "Selling Price", data["SellingPrice"] },
                             { "Discount %", data["DiscPerc"] },
                             { "Total Discount", TotalDisc },
                             { "Add Charges", data["AddCharges"] },
                             { "VAT", data["Vat"] },
                             { "Batch", data["Batch"] + "^" + dsBatch.Tables[0].Rows[c]["iBatchId"]},
                                    };
                                        #endregion
                                        body.Add(row);

                                        if (remainingQty < 0)
                                        {
                                            qty = -(remainingQty);
                                        }
                                    }
                                }
                            }
                        }

                        #endregion
                        if (body.Count() > 0)
                        {
                            string SIbaseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                            string SIUrl = SIbaseUrl + "/Transactions/Vouchers/Sales Invoice - VAN";

                            var postingData = new PostingData();
                            postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                            string sContent = JsonConvert.SerializeObject(postingData);
                            string err = "";

                            var response = Focus8API.Post(SIUrl, sContent, sessionID, ref err);
                            #region Response
                            if (response != null)
                            {
                                var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                                if (responseData.result == -1)
                                {

                                    Message = "Sales Invoice - VAN Posting Failed" + "\n";
                                    BL_Registry.SetLog("Sales Invoice - VAN Posted Failed with DocNo: " + DocNo);
                                    BL_Registry.SetLog2(response + "\n Sales Invoice - VAN Posted Failed with DocNo: " + DocNo);
                                    //val = 0;
                                }
                                else
                                {
                                    Message = "Sales Invoice - VAN  Posted Successfully" + "\n";
                                    BL_Registry.SetLog("Sales Invoice - VAN Posted Success with DocNo: " + DocNo);
                                    string UpSql = $@"update Invoice set PostStatus=1 where DocumentNumber='{DocNo}'";
                                    int res = cls.Update(UpSql);
                                    if (res == 1)
                                    {
                                        BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                    #endregion

                    #region Invoice Return Van Sales
                    else if (dt["Screen"].ToString() == "Sales Return")
                    {
                        #region CheckConditions
                        int BranchId = GetMasterId(dt["Branch"].ToString(), 8); //Companymaster
                        if (BranchId == -1)
                        {
                            BL_Registry.SetLogError("Company '" + dt["Branch"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int CurId = GetMasterId(dt["Currency"].ToString(), 3);
                        if (CurId == -1)
                        {
                            BL_Registry.SetLogError("Currency '" + dt["Currency"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int custAcId = GetMasterId(dt["Customer"].ToString(), 6);
                        if (custAcId == -1)
                        {
                            BL_Registry.SetLogError("Customer '" + dt["Customer"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int WareId = GetMasterId(dt["SourceWarehouse"].ToString(), 17);//Outlet
                        if (WareId == -1)
                        {
                            BL_Registry.SetLogError("Outlet '" + dt["SourceWarehouse"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int DriverId = GetMasterId(dt["Driver"].ToString(), 9);
                        if (DriverId == -1)
                        {
                            BL_Registry.SetLogError("Driver '" + dt["Driver"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int EmployeeId = GetMasterId(dt["Employee"].ToString(), 10);
                        if (EmployeeId == -1)
                        {
                            BL_Registry.SetLogError("Employee '" + dt["Employee"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int HelperId = GetMasterId(dt["Helper"].ToString(), 11);
                        if (HelperId == -1)
                        {
                            BL_Registry.SetLogError("Helper '" + dt["Helper"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int JurisId = GetMasterId(dt["Jurisdiction"].ToString(), 12);
                        if (JurisId == -1)
                        {
                            BL_Registry.SetLogError("Jurisdiction '" + dt["Jurisdiction"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int PosId = GetMasterId(dt["PlaceOfSupply"].ToString(), 13);
                        if (PosId == -1)
                        {
                            BL_Registry.SetLogError("PlaceOfSupply '" + dt["PlaceOfSupply"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        #endregion

                        #region Header
                        Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "CustomerAC", custAcId },
                             { "Currency", CurId },
                             { "Company Master", BranchId },
                             { "Outlet", WareId },
                             { "Employee", EmployeeId },
                             { "Driver", DriverId },
                             { "Helper", HelperId },
                             { "Jurisdiction", JurisId },
                             { "Place of supply", PosId },
                             { "LPO_No", dt["LPONumber"].ToString()},
                             { "Driver_Mobile_No", dt["DriverMobileNumber"].ToString() },
                         };
                        #endregion

                        List<Hashtable> body = new List<Hashtable>();
                        #region Body
                        foreach (var data in dr)
                        {
                            int BatchChk = cls.BatchCheck(data["Batch"].ToString());
                            if (BatchChk == 0)
                            {
                                BL_Registry.SetLogError("Batch is not available for this TransactionNumber " + DocNo);
                                outLoop = true;
                                continue;
                            }

                            int ItemId = GetMasterId(data["Product"].ToString(), 4);
                            if (ItemId == 0)
                            {
                                BL_Registry.SetLogError("Product '" + data["Product"].ToString() + "' Not Found with DocNo: " + DocNo);
                                outLoop = true;
                                break;
                            }
                            int unitId = GetMasterId(data["ProductUnit"].ToString(), 5);
                            if (unitId == 0)
                            {
                                BL_Registry.SetLogError("ProductUnit '" + data["ProductUnit"].ToString() + "' Not Found with DocNo: " + DocNo);
                                outLoop = true;
                                break;
                            }
                            int TaxCode = GetMasterId(data["TaxCode"].ToString(), 2);
                            if (TaxCode == 0)
                            {
                                BL_Registry.SetLogError("TaxCode '" + data["TaxCode"].ToString() + "' Not Found with DocNo: " + DocNo);
                                outLoop = true;
                                break;
                            }
                            int SalesAc = GetSalesAc(data["Product"].ToString());
                            if (SalesAc == 0)
                            {
                                BL_Registry.SetLogError("SalesAc not mapped to Item '" + data["Product"].ToString() + "' with DocNo: " + DocNo);
                                outLoop = true;
                                break;
                            }

                            int MfgDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["MfgDate"].ToString()));
                            int ExpDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["ExpiryDate"].ToString()));
                            //string TaxCode = "EX";
                            //if (taxamt > 0)
                            //{
                            //    TaxCode = "SR";
                            //}
                            Hashtable row = new Hashtable
                         {
                             { "Item", ItemId},
                             //{ "Description", GetItemDes(ItemId)},
                             { "Description", data["Notes"].ToString()},
                             { "TaxCode", TaxCode},
                             { "SalesAC", SalesAc},
                             { "Unit",  unitId},
                             { "Actual Quantity", data["ActualQuantity"] },
                             { "FOC Quantity", data["FOCQuantity"] },
                             { "Quantity", data["Quantity"] },
                             { "Selling Price", data["SellingPrice"] },
                             //{ "Rate", data["Price"] },
                             //{ "Gross", data["LineAmount"] },
                             { "Discount %", data["DiscPerc"] },
                             //{ "Input Discount Amt", data["LineDiscount"] },
                             { "Add Charges", data["AddCharges"] },
                             { "VAT", data["Vat"] },
                             { "Batch", data["Batch"] },
                             { "MfgDate",MfgDate },
                             { "ExpDate", ExpDate },
                         };
                            body.Add(row);
                        }

                        #endregion
                        if (body.Count() > 0)
                        {
                            string SRbaseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                            string SRUrl = SRbaseUrl + "/Transactions/Vouchers/Sales Return - VAN";

                            var postingData = new PostingData();
                            postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                            string sContent = JsonConvert.SerializeObject(postingData);
                            string err = "";

                            var response = Focus8API.Post(SRUrl, sContent, sessionID, ref err);
                            #region Response
                            if (response != null)
                            {
                                var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                                if (responseData.result == -1)
                                {

                                    Message = "Sales Return - VAN Posting Failed" + "\n";
                                    BL_Registry.SetLogError("Sales Return - VAN Posted Failed with DocNo: " + DocNo);
                                    BL_Registry.SetLog2(response + "\n Sales Return - VAN Posted Failed with DocNo: " + DocNo);
                                    //val = 0;
                                }
                                else
                                {
                                    Message = "Sales Return - VAN  Posted Successfully" + "\n";
                                    BL_Registry.SetLog("Sales Return - VAN Posted Success with DocNo: " + DocNo);
                                    string UpSql = $@"update Invoice set PostStatus=1 where DocumentNumber='{DocNo}'";
                                    int res = cls.Update(UpSql);
                                    if (res == 1)
                                    {
                                        BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                    #endregion

                    #region Cash Receipt Van Sales
                    else if (dt["Screen"].ToString() == "Cash")
                    {
                        #region CheckConditions
                        int CashBankAc = GetMasterId(dt["Salesman"].ToString(), 14);
                        if (CashBankAc == -1)
                        {
                            BL_Registry.SetLogError("CashBankAc for Employee '" + dt["Salesman"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int BranchId = GetMasterId(dt["Branch"].ToString(), 8);
                        if (BranchId == -1)
                        {
                            BL_Registry.SetLogError("Branch '" + dt["Branch"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int OutletId = GetMasterId(dt["VanNumber"].ToString(), 17);
                        if (OutletId == -1)
                        {
                            BL_Registry.SetLogError("VanNumber '" + dt["VanNumber"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int EmployeeId = GetMasterId(dt["Salesman"].ToString(), 10);
                        if (EmployeeId == -1)
                        {
                            BL_Registry.SetLogError("Salesman '" + dt["Salesman"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int CurId = GetMasterId("AED", 3);
                        if (CurId == -1)
                        {
                            BL_Registry.SetLogError("Currency 'AED' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        #endregion

                        #region Header
                        Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "CashBankAC",CashBankAc },
                             { "Currency",CurId },
                             { "ExchangeRate",dt["ExchangeRate"].ToString() },
                             { "Company Master", BranchId},
                             { "Employee", EmployeeId },
                             { "Outlet",OutletId } //InvoiceNumber                       
                         };
                        #endregion

                        List<Hashtable> body = new List<Hashtable>();

                        #region Body
                        foreach (var data in dr)
                        {
                            int Account = GetMasterId(data["Customer"].ToString(), 6);
                            if (Account == -1)
                            {
                                BL_Registry.SetLogError("Customer '" + data["Customer"].ToString() + "' Not Found with DocNo: " + DocNo);
                                outLoop = true;
                                break;
                            }

                            GetIRef(data["Reference"].ToString());
                            Hashtable row = new Hashtable();
                            if (iref != 0)
                            {
                                Hashtable billRef = new Hashtable();
                                billRef.Add("aptag", 0);
                                billRef.Add("CustomerId", Account);
                                billRef.Add("Amount", data["Amount"]);
                                billRef.Add("BillNo", "");
                                billRef.Add("reftype", 2);
                                billRef.Add("mastertypeid", 0);
                                billRef.Add("Reference", "SINV:" + data["Reference"].ToString());
                                billRef.Add("artag", 0);
                                billRef.Add("ref", iref);
                                billRef.Add("tag", 0);

                                row = new Hashtable
                         {
                             { "Account", Account},
                             { "Amount",data["Amount"]},
                             { "Reference",billRef},
                             { "sRemarks",data["Remarks"]}
                         };
                            }
                            else
                            {
                                row = new Hashtable
                         {
                             { "Account", Account},
                             { "Amount",data["Amount"]},
                             { "Reference", ""},
                             { "sRemarks",data["Remarks"]}
                         };
                            }
                            body.Add(row);
                        }

                        #endregion
                        if (body.Count() > 0)
                        {
                            string CashbaseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                            string CashUrl = CashbaseUrl + "/Transactions/Vouchers/Cash Receipts VAN";

                            var postingData = new PostingData();
                            postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                            string sContent = JsonConvert.SerializeObject(postingData);
                            string err = "";

                            var response = Focus8API.Post(CashUrl, sContent, sessionID, ref err);
                            #region Response
                            if (response != null)
                            {
                                var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                                if (responseData.result == -1)
                                {
                                    Message = "Cash Receipts VAN Posting Failed" + "\n";
                                    BL_Registry.SetLogError("Cash Receipts VAN Posted Failed with DocNo: " + DocNo);
                                    val = 0;
                                }
                                else
                                {
                                    Message = "Cash Receipts VAN Posted Successfully" + "\n";
                                    BL_Registry.SetLog("Cash Receipts VAN Posted Success with DocNo: " + DocNo);
                                    string UpSql = $@"update Receipt set PostStatus=1 where DocumentNumber='{DocNo}'";
                                    int res = cls.Update(UpSql);
                                    if (res == 1)
                                    {
                                        BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                    #endregion

                    #region PDC Receipt Van Sales
                    else if (dt["Screen"].ToString() == "PDC")
                    {
                        #region CheckConditions
                        int Mdate = BL_Registry.GetDateToInt(Convert.ToDateTime(dt["MaturityDate"].ToString()));
                        //getting Masters ID                    

                        int CashBankAc = GetMasterId(dt["Salesman"].ToString(), 16);
                        if (CashBankAc == -1)
                        {
                            BL_Registry.SetLogError("CashBankAc for Employee '" + dt["Salesman"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int BranchId = GetMasterId(dt["Branch"].ToString(), 8);
                        if (BranchId == -1)
                        {
                            BL_Registry.SetLogError("Branch '" + dt["Branch"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int OutletId = GetMasterId(dt["VanNumber"].ToString(), 17);
                        if (OutletId == -1)
                        {
                            BL_Registry.SetLogError("VanNumber '" + dt["VanNumber"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int EmployeeId = GetMasterId(dt["Salesman"].ToString(), 10);
                        if (EmployeeId == -1)
                        {
                            BL_Registry.SetLogError("Salesman '" + dt["Salesman"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int CurId = GetMasterId("AED", 3);
                        if (CurId == -1)
                        {
                            BL_Registry.SetLogError("Currency 'AED' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        #endregion

                        #region Header
                        Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "CashBankAC",CashBankAc },
                             { "Currency",CurId },
                             { "ExchangeRate",dt["ExchangeRate"].ToString() },
                             { "Company Master", BranchId},
                             { "Employee", EmployeeId },
                             { "Outlet",OutletId }, //InvoiceNumber                       
                             { "sChequeNo", dt["ChequeNumber"].ToString() },
                         };
                        #endregion

                        List<Hashtable> body = new List<Hashtable>();

                        #region Body
                        foreach (var data in dr)
                        {
                            int Account = GetMasterId(data["Customer"].ToString(), 6);
                            if (Account == -1)
                            {
                                BL_Registry.SetLogError("Customer '" + data["Customer"].ToString() + "' Not Found with DocNo: " + DocNo);
                                outLoop = true;
                                break;
                            }
                            GetIRef(data["Reference"].ToString());
                            Hashtable row = new Hashtable();
                            if (iref != 0)
                            {
                                Hashtable billRef = new Hashtable();
                                billRef.Add("aptag", 0);
                                billRef.Add("CustomerId", Account);
                                billRef.Add("Amount", data["Amount"]);
                                billRef.Add("BillNo", "");
                                billRef.Add("reftype", 2);
                                billRef.Add("mastertypeid", 0);
                                billRef.Add("Reference", "SINV:" + data["Reference"].ToString());
                                billRef.Add("artag", 0);
                                billRef.Add("ref", iref);
                                billRef.Add("tag", 0);

                                row = new Hashtable
                         {
                             { "Account", Account},
                             { "Amount",data["Amount"]},
                             { "Reference",billRef},
                             { "sRemarks",data["Remarks"]}
                         };
                            }
                            else
                            {
                                row = new Hashtable
                         {
                             { "Account", Account},
                             { "Amount",data["Amount"]},
                             { "Reference", ""},
                             { "sRemarks",data["Remarks"]}
                         };
                            }
                            body.Add(row);
                        }

                        #endregion
                        if (body.Count() > 0)
                        {
                            string PDCbaseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                            string PDCUrl = PDCbaseUrl + "/Transactions/Vouchers/Post Dated Receipt VAN";

                            var postingData = new PostingData();
                            postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                            string sContent = JsonConvert.SerializeObject(postingData);
                            string err = "";

                            var response = Focus8API.Post(PDCUrl, sContent, sessionID, ref err);
                            #region Response
                            if (response != null)
                            {
                                var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                                if (responseData.result == -1)
                                {
                                    Message = "Post Dated Receipt VAN Posting Failed" + "\n";
                                    BL_Registry.SetLogError("Post Dated Receipt VAN Posted Failed with DocNo: " + DocNo);
                                    val = 0;
                                }
                                else
                                {
                                    Message = "Post Dated Receipt VAN Posted Successfully" + "\n";
                                    BL_Registry.SetLog("Post Dated Receipt VAN Posted Success with DocNo: " + DocNo);
                                    string UpSql = $@"update Receipt set PostStatus=1 where DocumentNumber='{DocNo}'";
                                    int res = cls.Update(UpSql);
                                    if (res == 1)
                                    {
                                        BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                    #endregion

                    #region EEV
                    else if (dt["Screen"].ToString() == "EEV")
                    {
                        #region CheckConditions
                        int CurId = GetMasterId("AED", 3);
                        int CashBankAc = GetMasterId(dt["VanNumber"].ToString(), 14);
                        int BranchId = GetMasterId(dt["Branch"].ToString(), 8);
                        int OutletId = GetMasterId(dt["VanNumber"].ToString(), 17);
                        int EmployeeId = GetMasterId(dt["Salesman"].ToString(), 10);
                        #endregion

                        #region Header
                        Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "CashBankAC",CashBankAc },
                             { "Currency", CurId},
                             { "ExchangeRate",dt["ExchangeRate"].ToString() },
                             { "Company Master", BranchId},
                             { "Outlet", OutletId },
                             { "Employee", EmployeeId },
                         };
                        #endregion

                        List<Hashtable> body = new List<Hashtable>();

                        #region Body
                        foreach (var data in dr)
                        {
                            int ExpenseCatId = GetMasterId(data["ExpenseCategory"].ToString(), 18);
                            int Account = GetMasterId(data["ExpenseCategory"].ToString(), 15);
                            if (Account == -1)
                            {
                                BL_Registry.SetLogError("ExpenseCategory '" + data["ExpenseCategory"].ToString() + "' Not Found with DocNo: " + DocNo);
                                outLoop = true;
                                break;
                            }

                            Hashtable row = new Hashtable();

                            row = new Hashtable
                         {
                             { "Expense Category", ExpenseCatId},
                             { "Account", Account},
                             { "Amount",data["Amount"]},
                             { "sRemarks",data["Remarks"]}
                         };
                            body.Add(row);
                        }

                        #endregion
                        if (body.Count() > 0)
                        {
                            string EEVbaseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                            string EEVUrl = EEVbaseUrl + "/Transactions/Vouchers/Expense Entry Van";

                            var postingData = new PostingData();
                            postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                            string sContent = JsonConvert.SerializeObject(postingData);
                            string err = "";

                            var response = Focus8API.Post(EEVUrl, sContent, sessionID, ref err);
                            #region Response
                            if (response != null)
                            {
                                var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                                if (responseData.result == -1)
                                {
                                    Message = "Expense Entry Van Posting Failed" + "\n";
                                    BL_Registry.SetLog("Expense Entry Van Posted Failed with DocNo: " + DocNo);
                                    val = 0;
                                }
                                else
                                {
                                    Message = "Expense Entry Van Posted Successfully" + "\n";
                                    BL_Registry.SetLog("Expense Entry Van Posted Success with DocNo: " + DocNo);
                                    string UpSql = $@"update Expense set PostStatus=1 where DocumentNumber='{DocNo}'";
                                    int res = cls.Update(UpSql);
                                    if (res == 1)
                                    {
                                        BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                    #endregion

                    if (outLoop == true)
                    {
                        BL_Registry.SetLogError("Came out of Loop");
                        break;
                    }
                }
                return val;
            }
            catch (Exception e)
            {
                BL_Registry.SetLog(e.ToString());
                return -1;
            }
        }

        public int Posting(DataSet ds)
        {
            bool outLoop = false;
            int compId = BL_Configdata.Focus8CompID;
            sessionID = GetSessionId(compId);//DoLogin();//GetSessionId(compId);

            string Message = "";
            var val = 0;
            try
            {
                var distinctValues = ds.Tables[0].AsEnumerable().Select(_r => _r.Field<string>("DocumentNumber")).Distinct();
                foreach (var dist in distinctValues)
                {
                    string str = "DocumentNumber='" + dist + "'";
                    DataRow[] dr = ds.Tables[0].Select(str);
                    var dt = dr.FirstOrDefault();
                    var DocNo = dt["DocumentNumber"].ToString();
                    int date = BL_Registry.GetDateToInt(Convert.ToDateTime(dt["TransactionDate"].ToString()));
                    int DefAcc = GetAcctName(5378);
                    int WareIdso = GetMasterId(dt["SourceWarehouse"].ToString(), 1);
                    int BranchId = GetMasterId(dt["Branch"].ToString(), 8);

                    if (WareIdso == -1)
                    {
                        BL_Registry.SetLogError("Outlet '" + dt["SourceWarehouse"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    if (BranchId == -1)
                    {
                        BL_Registry.SetLogError("Branch '" + dt["Branch"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    #region Header
                    Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "PartyAC", DefAcc },
                             { "Company Master",BranchId },
                             { "Outlet", WareIdso },
                             { "Currency", "7"},
                             { "ExchangeRate", "1" },
                         };
                    #endregion

                    #region Body
                    List<Hashtable> body = new List<Hashtable>();

                    foreach (var data in dr)
                    {
                        int iBodyCnt = 0;

                        int BatchChk = cls.BatchCheck(data["Batch"].ToString());
                        if (BatchChk == -1)
                        {
                            BL_Registry.SetLogError("Batch is not available for this TransactionNumber " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int ItemId = GetMasterId(data["Product"].ToString(), 4);
                        if (ItemId == -1)
                        {
                            BL_Registry.SetLogError("Item '" + data["Product"].ToString() + "' Not Found with DocNo: " + DocNo);
                            body = new List<Hashtable>();
                            outLoop = true;
                            break;
                        }
                        int unitId = GetMasterId(data["ProductUnit"].ToString(), 5);
                        if (unitId == -1)
                        {
                            BL_Registry.SetLogError("Unit '" + data["ProductUnit"].ToString() + "' Not Found with DocNo: " + DocNo);
                            body = new List<Hashtable>();
                            outLoop = true;
                            break;
                        }
                        #region BatchQuery
                        //DataSet dsBatch = cls.GetBatchData(data["Batch"].ToString(), ItemId, WareIdso, date, data["ProductUnit"].ToString());
                        DataSet dsBatch = cls.GetBatchData(data["Batch"].ToString(), ItemId, WareIdso, date, 1, "");
                        decimal dval = Convert.ToDecimal(data["Quantity"]);
                        decimal qty = Convert.ToDecimal(data["Quantity"]);

                        //We have added new logic to stop posting partial items/qty
                        decimal batchquantity = 0;
                        decimal staggingqty = Convert.ToDecimal(data["Quantity"]);
                        for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                        {
                            batchquantity = batchquantity + Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]);
                        }
                        if (batchquantity < staggingqty)
                        {
                            BL_Registry.SetLogError("Batch (" + data["Batch"].ToString() + ") is not available for this TransactionNumber " + DocNo);
                            break;
                        }

                        if (dsBatch != null && dsBatch.Tables.Count > 0 && dsBatch.Tables[0].Rows.Count > 0)
                        {
                            for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                            {
                                if (Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]) >= Convert.ToDecimal(data["Quantity"]))
                                {
                                    iBodyCnt++;
                                    dval = 0;
                                    break;
                                }
                                else
                                {
                                    dval = dval - Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]);
                                    iBodyCnt++;
                                    if (dval <= 0) { break; }
                                }
                            }
                        }
                        else
                        {
                            BL_Registry.SetLogError("Batch (" + data["Batch"].ToString() + ") is not available for this TransactionNumber " + DocNo);
                            break;
                        }
                        #endregion
                        if (iBodyCnt > 0)
                        {
                            for (int c = 0; c < iBodyCnt; c++)
                            {
                                int DefaultBaseUnit = Convert.ToInt32(dsBatch.Tables[0].Rows[c]["iDefaultBaseUnit"]);

                                decimal batchqty = (Convert.ToDecimal(dsBatch.Tables[0].Rows[c]["BatchQty"]));
                                decimal remainingQty = (batchqty - qty);
                                if (remainingQty < 0)
                                {
                                    qty = Math.Round(batchqty, 4);
                                }
                                // decimal Gross = qty * Convert.ToDecimal(data["Price"]);
                                int StockAc = GetStockAc(data["Product"].ToString());
                                if (StockAc == -1)
                                {
                                    BL_Registry.SetLogError("StockAc not mapped to '" + data["Product"].ToString() + "' with DocNo: " + DocNo);
                                    body = new List<Hashtable>();
                                    break;
                                }
                                Hashtable row = new Hashtable
                         {
                             { "Item__Id", ItemId},//data["ItemCode"]
                             { "Description", data["Notes"]},
                             { "StockAC__Id",StockAc},
                             { "Unit__Id",unitId},// data["ItemUOM"]
                             { "Quantity", qty },
                             //{ "Rate", rate},
                             //{ "Batch",data["Batch"] }
                             { "Batch", data["Batch"] + "^" + dsBatch.Tables[0].Rows[c]["iBatchId"]},
                         };
                                body.Add(row);

                                if (remainingQty < 0)
                                {
                                    qty = -(remainingQty);
                                }
                            }
                        }
                    }
                    #endregion
                    if (body.Count() > 0)
                    {
                        string baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                        string Url = baseUrl + "/Transactions/Vouchers/Load Out";

                        var postingData = new PostingData();
                        postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                        string sContent = JsonConvert.SerializeObject(postingData);
                        string err = "";

                        var response = Focus8API.Post(Url, sContent, sessionID, ref err);
                        #region Response
                        if (response != null)
                        {
                            var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                            if (responseData.result == -1)
                            {

                                Message = "Load Out Posting Failed" + "\n";
                                BL_Registry.SetLogError("Load Out Posted Failed with DocNo: " + DocNo);
                                val = 0;
                            }
                            else
                            {
                                Message = "Load Out  Posted Successfully" + "\n";
                                BL_Registry.SetLog("Load Out Posted Success with DocNo: " + DocNo);
                                string UpSql = $@"update StockTransaction set PostStatus=1 where DocumentNumber='{DocNo}'";
                                int res = cls.Update(UpSql);
                                if (res == 1)
                                {
                                    BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                }

                                #region LoadIn
                                #region HeaderSection
                                int InDefAcc = GetAcctName(2050);
                                Hashtable header1 = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "PartyAC", InDefAcc },
                             { "Company Master",GetMasterId(dt["Branch"].ToString(),8) },
                             { "Outlet",GetMasterId(dt["DestinationWarehouse"].ToString(),1)  },
                             { "Currency", "7"},
                             { "ExchangeRate", "1" },
                         };
                                #endregion

                                #region BodySection
                                List<Hashtable> body1 = new List<Hashtable>();

                                foreach (var data in dr)
                                {
                                    GetTranID(dt["DocumentNumber"].ToString(), GetMasterId(data["Product"].ToString(), 4), 1);
                                    int MfgDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["MfgDate"].ToString()));
                                    int ExpDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["ExpiryDate"].ToString()));

                                    Hashtable row = new Hashtable
                         {
                             { "Item", GetMasterId(data["Product"].ToString(), 4)},
                             { "Description", data["Notes"]},
                             { "PurchaseAC",  GetStockAc(data["Product"].ToString())},
                             { "Unit",  GetMasterId(data["ProductUnit"].ToString(), 5)},
                             { "Quantity", data["Quantity"] },
                             { "L-Load Out", TransID },
                             //{ "Rate",  data["ItemUOM"]},
                             //{ "Gross",  data["ItemUOM"]},
                             { "Batch",  data["Batch"]},
                             { "MfgDate",  MfgDate},
                             { "ExpDate",  ExpDate},
                         };
                                    body1.Add(row);
                                }
                                #endregion

                                baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                                Url = baseUrl + "/Transactions/Vouchers/Load In";

                                var postingData1 = new PostingData();
                                postingData1.data.Add(new Hashtable { { "Header", header1 }, { "Body", body1 } });
                                string sContent1 = JsonConvert.SerializeObject(postingData1);
                                err = "";
                                var response1 = Focus8API.Post(Url, sContent1, sessionID, ref err);
                                #region ResponseData
                                if (response1 != null)
                                {
                                    var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                                    if (responseData1.result == -1)
                                    {

                                        Message = "Load In Posting Failed" + "\n";
                                        BL_Registry.SetLogError("Load In Posted Failed with DocNo: " + DocNo);
                                        val = 0;
                                    }
                                    else
                                    {
                                        Message = "Load In  Posted Successfully" + "\n";
                                        BL_Registry.SetLog("Load In Posted Success with DocNo: " + DocNo);
                                        string UpdSql = $@"update StockTransaction set PostStatus=1 where DocumentNumber='{DocNo}'";
                                        int res1 = cls.Update(UpdSql);
                                        if (res1 == 1)
                                        {
                                            BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                        }
                                    }
                                }
                                #endregion
                                #endregion
                            }
                        }
                        #endregion
                    }
                    if (outLoop == true)
                    {
                        BL_Registry.SetLogError("Came out of Loop");
                        break;
                    }
                }
                return val;
            }
            catch (Exception e)
            {
                BL_Registry.SetLogError(e.ToString());
                return -1;
            }
        }

        public int PostingOff(DataSet ds)
        {
            bool outLoop = false;
            int compId = BL_Configdata.Focus8CompID;
            sessionID = GetSessionId(compId);//DoLogin();//GetSessionId(compId);

            string Message = "";
            var val = 0;
            try
            {
                var distinctValues = ds.Tables[0].AsEnumerable().Select(_r => _r.Field<string>("DocumentNumber")).Distinct();
                foreach (var dist in distinctValues)
                {
                    string str = "DocumentNumber='" + dist + "'";
                    DataRow[] dr = ds.Tables[0].Select(str);
                    var dt = dr.FirstOrDefault();
                    var DocNo = dt["DocumentNumber"].ToString();
                    int date = BL_Registry.GetDateToInt(Convert.ToDateTime(dt["TransactionDate"].ToString()));
                    GetDefAcc(5379);
                    //getting Masters ID
                    int WareId = GetMasterId(dt["SourceWarehouse"].ToString(), 1);
                    if (WareId == -1)
                    {
                        BL_Registry.SetLogError("Outlet '" + dt["SourceWarehouse"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int DWareId = GetMasterId(dt["DestinationWarehouse"].ToString(), 1);
                    if (DWareId == -1)
                    {
                        BL_Registry.SetLogError("Outlet '" + dt["DestinationWarehouse"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int BranchId = GetMasterId(dt["Branch"].ToString(), 8);
                    if (BranchId == -1)
                    {
                        BL_Registry.SetLogError("Company Master '" + dt["Branch"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int CurId = GetMasterId("AED", 3);
                    if (CurId == -1)
                    {
                        BL_Registry.SetLogError("Currency Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    #region Header
                    Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "PartyAC", iDefAcc2 },
                             { "Company Master",BranchId},
                             { "Outlet",WareId },
                             { "Currency", CurId},
                             { "ExchangeRate", "1" },
                         };
                    #endregion

                    #region Body
                    List<Hashtable> body = new List<Hashtable>();

                    foreach (var data in dr)
                    {
                        int BatchChk = cls.BatchCheck(data["Batch"].ToString());
                        if (BatchChk == 0)
                        {
                            BL_Registry.SetLog("Batch is not available for this TransactionNumber " + DocNo);
                            break;
                        }
                        int ItemId = GetMasterId(data["Product"].ToString(), 4);
                        if (ItemId == 0)
                        {
                            BL_Registry.SetLogError("Product '" + data["Product"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int unitId = GetMasterId(data["ProductUnit"].ToString(), 5);
                        if (unitId == 0)
                        {
                            BL_Registry.SetLogError("ProductUnit '" + data["ProductUnit"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int StockAc = GetStockAc(data["Product"].ToString());
                        if (StockAc == 0)
                        {
                            BL_Registry.SetLogError("Stock Account not mapped for Product '" + data["Product"].ToString() + "' with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        #region BatchQuery
                        //DataSet dsBatch = cls.GetBatchData(data["Batch"].ToString(), ItemId, WareId, date, data["ProductUnit"].ToString());
                        DataSet dsBatch = cls.GetBatchData(data["Batch"].ToString(), ItemId, WareId, date, 1, "");
                        decimal dval = Convert.ToDecimal(data["Quantity"]);
                        decimal qty = Convert.ToDecimal(data["Quantity"]);
                        int iBodyCnt = 0;

                        //We have added new logic to stop posting partial items/qty
                        decimal batchquantity = 0;
                        decimal staggingqty = Convert.ToDecimal(data["Quantity"]);
                        for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                        {
                            batchquantity = batchquantity + Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]);
                        }
                        if (batchquantity < staggingqty)
                        {
                            BL_Registry.SetLogError("Batch (" + data["Batch"].ToString() + ") is not available for this TransactionNumber " + DocNo);
                            outLoop = true;
                            break;
                        }

                        if (dsBatch != null && dsBatch.Tables.Count > 0 && dsBatch.Tables[0].Rows.Count > 0)
                        {
                            for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                            {
                                if (Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]) >= Convert.ToDecimal(data["Quantity"]))
                                {
                                    iBodyCnt++;
                                    dval = 0;
                                    break;
                                }
                                else
                                {
                                    dval = dval - Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]);
                                    iBodyCnt++;
                                    if (dval <= 0) { break; }
                                }
                            }
                        }
                        else
                        {
                            BL_Registry.SetLog("Batch (" + data["Batch"].ToString() + ") is not available for this TransactionNumber " + DocNo);
                            outLoop = true;
                            break;
                        }
                        #endregion
                        if (iBodyCnt > 0)
                        {
                            for (int c = 0; c < iBodyCnt; c++)
                            {
                                int DefaultBaseUnit = Convert.ToInt32(dsBatch.Tables[0].Rows[c]["iDefaultBaseUnit"]);

                                decimal batchqty = (Convert.ToDecimal(dsBatch.Tables[0].Rows[c]["BatchQty"]));
                                decimal remainingQty = (batchqty - qty);
                                if (remainingQty < 0)
                                {
                                    qty = Math.Round(batchqty, 4);
                                }
                                // decimal Gross = qty * Convert.ToDecimal(data["Price"]);
                                Hashtable row = new Hashtable
                         {
                             { "Item__Id", ItemId},//data["ItemCode"]
                             { "Description", data["Notes"]},
                             { "StockAC__Id",StockAc},
                             { "Unit__Id",unitId},// data["ItemUOM"]
                             { "Quantity", qty },
                             //{ "Rate", rate},
                             //{ "Batch",data["Batch"] }
                             { "Batch", data["Batch"] + "^" + dsBatch.Tables[0].Rows[c]["iBatchId"]},
                         };
                                body.Add(row);

                                if (remainingQty < 0)
                                {
                                    qty = -(remainingQty);
                                }
                            }
                        }
                    }
                    #endregion
                    if (body.Count() > 0)
                    {
                        string baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                        string Url = baseUrl + "/Transactions/Vouchers/Off Load Out";

                        var postingData = new PostingData();
                        postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                        string sContent = JsonConvert.SerializeObject(postingData);
                        string err = "";

                        var response = Focus8API.Post(Url, sContent, sessionID, ref err);
                        #region Response
                        if (response != null)
                        {
                            var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                            if (responseData.result == -1)
                            {
                                Message = "Off Load Out Posting Failed" + "\n";
                                BL_Registry.SetLogError("Off Load Out Posted Failed with DocNo: " + DocNo);
                                //val = 0;
                            }
                            else
                            {
                                Message = "Off Load Out Posted Successfully" + "\n";
                                BL_Registry.SetLog("Off Load Out Posted Success with DocNo: " + DocNo);
                                string UpSql = $@"update StockTransaction set PostStatus=1 where DocumentNumber='{DocNo}'";
                                int res = cls.Update(UpSql);
                                if (res == 1)
                                {
                                    BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                }

                                #region OffLoadIn
                                #region HeaderSection
                                int InDefAcc = GetAcctName(2051);

                                Hashtable header1 = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "PartyAC", InDefAcc },
                             { "Company Master",BranchId },
                             { "Outlet",DWareId  },
                             { "Currency", CurId},
                             { "ExchangeRate", "1" },
                         };
                                #endregion

                                #region BodySection
                                List<Hashtable> body1 = new List<Hashtable>();

                                foreach (var data in dr)
                                {
                                    GetTranID(dt["DocumentNumber"].ToString(), GetMasterId(data["Product"].ToString(), 4), 1);
                                    int MfgDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["MfgDate"].ToString()));
                                    int ExpDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["ExpiryDate"].ToString()));

                                    int ItemId = GetMasterId(data["Product"].ToString(), 4);
                                    if (ItemId == -1)
                                    {
                                        BL_Registry.SetLogError("Outlet '" + dt["Product"].ToString() + "' Not Found with DocNo: " + DocNo);
                                        outLoop = true;
                                        break;
                                    }
                                    int StockAcId = GetStockAc(data["Product"].ToString());
                                    if (StockAcId == -1)
                                    {
                                        BL_Registry.SetLogError("Account not mapped to Item '" + dt["Product"].ToString() + "' with DocNo: " + DocNo);
                                        outLoop = true;
                                        break;
                                    }
                                    int UnitId = GetMasterId(data["ProductUnit"].ToString(), 5);
                                    if (UnitId == -1)
                                    {
                                        BL_Registry.SetLogError("Unit '" + dt["ProductUnit"].ToString() + "' Not Found with DocNo: " + DocNo);
                                        outLoop = true;
                                        break;
                                    }

                                    Hashtable row = new Hashtable
                         {
                             { "Item",ItemId },
                             { "Description", data["Notes"]},
                             { "PurchaseAC", StockAcId },
                             { "Unit", UnitId },
                             { "Quantity", data["Quantity"] },
                             { "L-Off Load Out", TransID },
                             //{ "Rate",  data["ItemUOM"]},
                             //{ "Gross",  data["ItemUOM"]},
                             { "Batch",  data["Batch"]},
                             { "MfgDate",  MfgDate},
                             { "ExpDate",  ExpDate},
                         };
                                    body1.Add(row);
                                }
                                #endregion

                                baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                                Url = baseUrl + "/Transactions/Vouchers/Off Load In";

                                var postingData1 = new PostingData();
                                postingData1.data.Add(new Hashtable { { "Header", header1 }, { "Body", body1 } });
                                string sContent1 = JsonConvert.SerializeObject(postingData1);
                                err = "";
                                var response1 = Focus8API.Post(Url, sContent1, sessionID, ref err);
                                #region ResponseData
                                if (response1 != null)
                                {
                                    var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                                    if (responseData1.result == -1)
                                    {

                                        Message = "Off Load In Posting Failed" + "\n";
                                        BL_Registry.SetLogError("Off Load In Posted Failed with DocNo: " + DocNo);
                                        val = 0;
                                    }
                                    else
                                    {
                                        Message = "Off Load In  Posted Successfully" + "\n";
                                        BL_Registry.SetLog("Off Load In Posted Success with DocNo: " + DocNo);
                                        string UpdSql = $@"update StockTransaction set PostStatus=1 where DocumentNumber='{DocNo}'";
                                        int res1 = cls.Update(UpdSql);
                                        if (res1 == 1)
                                        {
                                            BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                        }
                                    }
                                }
                                #endregion
                                #endregion
                            }
                        }
                        #endregion
                    }
                    if (outLoop == true)
                    {
                        BL_Registry.SetLogError("Came out of Loop");
                        break;
                    }
                }
                return val;
            }
            catch (Exception e)
            {
                BL_Registry.SetLog(e.ToString());
                return -1;
            }
        }

        public int PostingTransfer(DataSet ds)
        {
            GetExternalData clsd = new GetExternalData();
            bool outLoop = false;
            int compId = BL_Configdata.Focus8CompID;
            sessionID = GetSessionId(compId);//DoLogin();//GetSessionId(compId);

            string Message = "";
            var val = 0;
            try
            {
                var distinctValues = ds.Tables[0].AsEnumerable().Select(_r => _r.Field<string>("DocumentNumber")).Distinct();
                foreach (var dist in distinctValues)
                {
                    string str = "DocumentNumber='" + dist + "'";
                    DataRow[] dr = ds.Tables[0].Select(str);
                    var dt = dr.FirstOrDefault();
                    var DocNo = dt["DocumentNumber"].ToString();
                    int date = BL_Registry.GetDateToInt(Convert.ToDateTime(dt["TransactionDate"].ToString()));
                    GetDefAcc(5377);
                    //getting Masters ID

                    int WareId = GetMasterId(dt["SourceWarehouse"].ToString(), 1);
                    if (WareId == -1)
                    {
                        BL_Registry.SetLogError("Outlet '" + dt["SourceWarehouse"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int DWareId = GetMasterId(dt["DestinationWarehouse"].ToString(), 1);
                    if (DWareId == -1)
                    {
                        BL_Registry.SetLogError("Outlet '" + dt["DestinationWarehouse"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int BranchId = GetMasterId(dt["Branch"].ToString(), 8);
                    if (BranchId == -1)
                    {
                        BL_Registry.SetLogError("Company Master '" + dt["Branch"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int CurId = GetMasterId("AED", 3);
                    if (CurId == -1)
                    {
                        BL_Registry.SetLogError("Currency Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    #region Header
                    Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "PartyAC", iDefAcc2 },
                             { "Company Master",BranchId},
                             { "Outlet",WareId },
                             { "Currency", CurId},
                             { "ExchangeRate", "1" },
                         };
                    #endregion

                    #region Body
                    List<Hashtable> body = new List<Hashtable>();

                    foreach (var data in dr)
                    {
                        int BatchChk = cls.BatchCheck(data["Batch"].ToString());
                        if (BatchChk == 0)
                        {
                            BL_Registry.SetLogError("Batch is not available for this TransactionNumber " + DocNo);
                            break;
                        }

                        int ItemId = GetMasterId(data["Product"].ToString(), 4);
                        if (ItemId == 0)
                        {
                            BL_Registry.SetLogError("Product '" + data["Product"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int unitId = GetMasterId(data["ProductUnit"].ToString(), 5);
                        if (unitId == 0)
                        {
                            BL_Registry.SetLogError("ProductUnit '" + data["ProductUnit"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int StockAc = GetStockAc(data["Product"].ToString());
                        if (StockAc == 0)
                        {
                            BL_Registry.SetLogError("Stock Account not mapped for Product '" + data["Product"].ToString() + "' with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        #region BatchQuery
                        //DataSet dsBatch = cls.GetBatchData(data["Batch"].ToString(), ItemId, WareId, date, data["ProductUnit"].ToString());
                        DataSet dsBatch = cls.GetBatchData(data["Batch"].ToString(), ItemId, WareId, date, 1, "");
                        decimal dval = Convert.ToDecimal(data["Quantity"]);
                        decimal qty = Convert.ToDecimal(data["Quantity"]);
                        int iBodyCnt = 0;

                        //We have added new logic to stop posting partial items/qty
                        decimal batchquantity = 0;
                        decimal staggingqty = Convert.ToDecimal(data["Quantity"]);
                        for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                        {
                            batchquantity = batchquantity + Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]);
                        }
                        if (batchquantity < staggingqty)
                        {
                            BL_Registry.SetLogError("Batch (" + data["Batch"].ToString() + ") is not available for this TransactionNumber " + DocNo);
                            break;
                        }

                        if (dsBatch != null && dsBatch.Tables.Count > 0 && dsBatch.Tables[0].Rows.Count > 0)
                        {
                            for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                            {
                                if (Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]) >= Convert.ToDecimal(data["Quantity"]))
                                {
                                    iBodyCnt++;
                                    dval = 0;
                                    break;
                                }
                                else
                                {
                                    dval = dval - Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]);
                                    iBodyCnt++;
                                    if (dval <= 0) { break; }
                                }
                            }
                        }
                        else
                        {
                            BL_Registry.SetLogError("Batch (" + data["Batch"].ToString() + ") is not available for this TransactionNumber " + DocNo);
                            break;
                        }
                        #endregion
                        if (iBodyCnt > 0)
                        {
                            for (int c = 0; c < iBodyCnt; c++)
                            {
                                int DefaultBaseUnit = Convert.ToInt32(dsBatch.Tables[0].Rows[c]["iDefaultBaseUnit"]);

                                decimal batchqty = (Convert.ToDecimal(dsBatch.Tables[0].Rows[c]["BatchQty"]));
                                decimal remainingQty = (batchqty - qty);
                                if (remainingQty < 0)
                                {
                                    qty = Math.Round(batchqty, 4);
                                }
                                // decimal Gross = qty * Convert.ToDecimal(data["Price"]);
                                Hashtable row = new Hashtable
                         {
                             { "Item__Id", ItemId},//data["ItemCode"]
                             { "Description", data["Notes"]},
                             { "StockAC__Id",StockAc},
                             { "Unit__Id",unitId},// data["ItemUOM"]
                             { "Quantity", qty },
                             //{ "Rate", rate},
                             //{ "Batch",data["Batch"] }
                             { "Batch", data["Batch"] + "^" + dsBatch.Tables[0].Rows[c]["iBatchId"]},
                         };
                                body.Add(row);

                                if (remainingQty < 0)
                                {
                                    qty = -(remainingQty);
                                }
                            }
                        }
                    }
                    #endregion
                    if (body.Count() > 0)
                    {
                        string baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                        string Url = baseUrl + "/Transactions/Vouchers/Transfer Out";

                        var postingData = new PostingData();
                        postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                        string sContent = JsonConvert.SerializeObject(postingData);
                        string err = "";

                        var response = Focus8API.Post(Url, sContent, sessionID, ref err);
                        #region Response
                        if (response != null)
                        {
                            var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                            if (responseData.result == -1)
                            {
                                Message = "Transfer Out Posting Failed" + "\n";
                                BL_Registry.SetLogError("Transfer Out Posted Failed with DocNo: " + DocNo);
                                //val = 0;
                            }
                            else
                            {
                                Message = "Transfer Out Posted Successfully" + "\n";
                                BL_Registry.SetLog("Transfer Out Posted Success with DocNo: " + DocNo);
                                string UpSql = $@"update StockTransaction set PostStatus=1 where DocumentNumber='{DocNo}'";
                                int res = cls.Update(UpSql);
                                if (res == 1)
                                {
                                    BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                }

                                #region TransferIn
                                #region HeaderSection
                                int InDefAcc = GetAcctName(2049);

                                Hashtable header1 = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "PartyAC", InDefAcc },
                             { "Company Master",BranchId },
                             { "Outlet",DWareId  },
                             { "Currency", CurId},
                             { "ExchangeRate", "1" },
                         };
                                #endregion

                                #region BodySection
                                List<Hashtable> body1 = new List<Hashtable>();

                                foreach (var data in dr)
                                {
                                    GetTranID(dt["DocumentNumber"].ToString(), GetMasterId(data["Product"].ToString(), 4), 1);
                                    int MfgDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["MfgDate"].ToString()));
                                    int ExpDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["ExpiryDate"].ToString()));

                                    Hashtable row = new Hashtable
                         {
                             { "Item", GetMasterId(data["Product"].ToString(), 4)},
                             { "Description", data["Notes"]},
                             { "PurchaseAC",  GetStockAc(data["Product"].ToString())},
                             { "Unit",  GetMasterId(data["ProductUnit"].ToString(), 5)},
                             { "Quantity", data["Quantity"] },
                             { "L-Transfer Out", TransID },
                             //{ "Rate",  data["ItemUOM"]},
                             //{ "Gross",  data["ItemUOM"]},
                             { "Batch",  data["Batch"]},
                             { "MfgDate",  MfgDate},
                             { "ExpDate",  ExpDate},
                         };
                                    body1.Add(row);
                                }
                                #endregion

                                baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                                Url = baseUrl + "/Transactions/Vouchers/Transfer In";

                                var postingData1 = new PostingData();
                                postingData1.data.Add(new Hashtable { { "Header", header1 }, { "Body", body1 } });
                                string sContent1 = JsonConvert.SerializeObject(postingData1);
                                err = "";
                                var response1 = Focus8API.Post(Url, sContent1, sessionID, ref err);
                                #region ResponseData
                                if (response1 != null)
                                {
                                    var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                                    if (responseData1.result == -1)
                                    {

                                        Message = "Transfer In Posting Failed" + "\n";
                                        BL_Registry.SetLog("Transfer In Posted Failed with DocNo: " + DocNo);
                                        val = 0;
                                    }
                                    else
                                    {
                                        Message = "Transfer In  Posted Successfully" + "\n";
                                        BL_Registry.SetLog("Transfer In Posted Success with DocNo: " + DocNo);
                                        string UpdSql = $@"update StockTransaction set PostStatus=1 where DocumentNumber='{DocNo}'";
                                        int res1 = cls.Update(UpdSql);
                                        if (res1 == 1)
                                        {
                                            BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                        }
                                    }
                                }
                                #endregion
                                #endregion
                            }
                        }
                        #endregion
                    }
                    if (outLoop == true)
                    {
                        BL_Registry.SetLogError("Came out of Loop");
                        break;
                    }
                }
                return val;
            }
            catch (Exception e)
            {
                BL_Registry.SetLog(e.ToString());
                return -1;
            }
        }

        public int PostingSI(DataSet ds)
        {
            GetExternalData clsd = new GetExternalData();
            bool outLoop = false;
            int compId = BL_Configdata.Focus8CompID;
            sessionID = GetSessionId(compId);//DoLogin();//GetSessionId(compId);

            string Message = "";
            var val = 1;
            try
            {
                var distinctValues = ds.Tables[0].AsEnumerable().Select(_r => _r.Field<string>("DocumentNumber")).Distinct();
                foreach (var dist in distinctValues)
                {
                    outLoop = false;
                    val = 1;
                    string str = "DocumentNumber='" + dist + "'";
                    DataRow[] dr = ds.Tables[0].Select(str);
                    var dt = dr.FirstOrDefault();
                    var DocNo = dt["DocumentNumber"].ToString();
                    
                    int date = BL_Registry.GetDateToInt(Convert.ToDateTime(dt["InvoiceDate"].ToString()));
                    int Duedate = BL_Registry.GetDateToInt(Convert.ToDateTime(dt["InvoiceDate"].ToString()));
                    int CurId = GetMasterId(dt["Currency"].ToString(), 3);
                    if (CurId == -1)
                    {
                        BL_Registry.SetLogError("Master Currency '" + dt["Currency"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int custAcId = GetMasterId(dt["Customer"].ToString(), 6);
                    if (custAcId == -1)
                    {
                        BL_Registry.SetLogError("Master Account '" + dt["Customer"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        outLoop = true;
                        break;
                    }

                    //int WareId = GetMasterId(dt["Outlet"].ToString(), 17);//Outlet
                    //if (WareId == -1)
                    //{
                    //    BL_Registry.SetLogError("Outlet '" + dt["Outlet"].ToString() + "' Not Found with DocNo: " + DocNo);
                    //    outLoop = true;
                    //    break;
                    //}
                    int OutletId = GetMasterId(dt["SourceWarehouse"].ToString(), 17);//Outlet
                    if (OutletId == -1)
                    {
                        BL_Registry.SetLogError("Master Warehouse '" + dt["SourceWarehouse"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int CompanyMasterId = GetMasterId(dt["Company"].ToString(), 8); //Companymaster
                    if (CompanyMasterId == -1)
                    {
                        BL_Registry.SetLogError("Master Division '" + dt["Company"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int DriverId = GetMasterId(dt["Driver"].ToString(), 9);
                    if (DriverId == -1)
                    {
                        BL_Registry.SetLogError("Master Driver '" + dt["Driver"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int EmployeeId = GetMasterId(dt["Employee"].ToString(), 10);
                    if (EmployeeId == -1)
                    {
                        BL_Registry.SetLogError("Master Employee '" + dt["Employee"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int HelperId = GetMasterId(dt["Helper"].ToString(), 11);
                    if (HelperId == -1)
                    {
                        BL_Registry.SetLogError("Master Helper '" + dt["Helper"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int JurisId = GetMasterId(dt["Jurisdiction"].ToString(), 12);
                    if (JurisId == -1)
                    {
                        BL_Registry.SetLogError("Master Jurisdiction '" + dt["Jurisdiction"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int PosId = GetMasterId(dt["PlaceOfSupply"].ToString(), 13);
                    if (PosId == -1)
                    {
                        BL_Registry.SetLogError("Master PlaceOfSupply '" + dt["PlaceOfSupply"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        outLoop = true;
                        break;
                    }

                    #region Header
                    Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "CustomerAC", custAcId },
                             { "DueDate",Duedate },
                             { "Currency", CurId },
                             { "Company Master__Id", CompanyMasterId },
                             { "Outlet__Id", OutletId },
                             { "Employee", EmployeeId },
                             { "Driver", DriverId },
                             { "Helper", HelperId },
                             { "Jurisdiction", JurisId },
                             { "Place of supply", PosId },
                             { "LPO_No", dt["LPONumber"].ToString()},
                             { "Driver_Mobile_No", dt["DriverMobileNumber"].ToString() },
                         };
                    #endregion

                    List<Hashtable> body = new List<Hashtable>();
                    #region Body
                    foreach (var data in dr)
                    {
                        int WareId = GetMasterId(dt["SourceWarehouse"].ToString(), 17);//SourceWarehouse
                        int ItemId = GetMasterId(data["Product"].ToString(), 4);
                        if (ItemId == 0)
                        {
                            BL_Registry.SetLogError("Master Product '" + data["Product"].ToString() + "' Not Exist with Transaction : " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int unitId = GetMasterId(data["ProductUnit"].ToString(), 5);
                        if (unitId == 0)
                        {
                            BL_Registry.SetLogError("Master ProductUnit '" + data["ProductUnit"].ToString() + "' Not Exist with Transaction : " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int TaxCode = GetMasterId(data["TaxCode"].ToString(), 2);
                        if (TaxCode == 0)
                        {
                            BL_Registry.SetLogError("Master TaxCode '" + data["TaxCode"].ToString() + "' Not Exist with Transaction : " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int SalesAc = GetSalesAc(data["Product"].ToString());
                        if (SalesAc == 0)
                        {
                            BL_Registry.SetLogError("SalesAc not mapped to Item '" + data["Product"].ToString() + "' with Transaction : " + DocNo);
                            outLoop = true;
                            break;
                        }
                        decimal FinalQty = 0;
                        string Batch = "";
                        if (data["Batch"].ToString() == "")
                        {
                            FinalQty = Convert.ToDecimal(data["Qty"]);
                            Hashtable row = new Hashtable
                            {
                                { "Item", ItemId},
                                { "Description", GetItemDes(ItemId)},
                                //{ "Description", data["Description"].ToString()},
                                { "TaxCode__Id", TaxCode},
                                { "SalesAC__Id", SalesAc},//need to change iDefAcc2
                                { "Unit__Id",  unitId},
                                { "Actual Quantity", data["ActualQuantity"] },
                                { "FOC Quantity", data["FOCQuantity"] },
                                { "Quantity", FinalQty},
                                { "Selling Price", data["SellingPrice"] },
                                //{ "Discount %", data["DiscPerc"] },
                                { "Input Discount Amt", data["DiscPerc"] },
                                //{ "Total Discount", TotalDisc },
                                { "Add Charges", data["AddCharges"] },
                                { "VAT", data["Vat"] },
                                { "Batch", Batch},
                            };
                            body.Add(row);
                        }
                        else
                        {
                            BL_Registry.SetLog("Expiry Date = " + data["ExpiryDate"].ToString());
                            int Exdate =  data["ExpiryDate"].ToString().Trim() == "01-01-1900 12:00:00 AM" ? 0: BL_Registry.GetDateToInt(Convert.ToDateTime(data["ExpiryDate"].ToString()));
                            int BatchChk = 0;
                            if (data["BatchId"].ToString() == "" || data["BatchId"] == null || data["BatchId"].ToString().ToLower() == "null")
                            {
                                BatchChk = clsd.BatchCheck3(data["Batch"].ToString(), Exdate.ToString(), date.ToString());
                            }
                            else
                            {
                                BatchChk = clsd.BatchCheck2(data["Batch"].ToString(), Exdate.ToString(), date.ToString(), data["BatchId"].ToString());
                            }
                            
                            if (BatchChk == 0)
                            {
                                BL_Registry.SetLog("Cannot Find  Batch No '" + data["Batch"].ToString() + "' for TransactionNumber " + DocNo);
                                break;
                            }
                            else
                            {

                                #region BatchQuery
                                DataSet dsBatch;
                                if (data["BatchId"].ToString() == "" || data["BatchId"] == null || data["BatchId"].ToString().ToLower() == "null")
                                {
                                     dsBatch = cls.GetBatchData3(data["Batch"].ToString(), ItemId, WareId, date, Exdate, data["ProductUnit"].ToString());
                                }
                                else
                                {
                                     dsBatch = cls.GetBatchData2(data["Batch"].ToString(), ItemId, WareId, date, Exdate, data["ProductUnit"].ToString(), data["BatchId"].ToString());
                                }
                                decimal dval = Convert.ToDecimal(data["Qty"]);
                                decimal qty = Convert.ToDecimal(data["Qty"]);
                                int iBodyCnt = 0;

                                //We have added new logic to stop posting partial items/qty
                                decimal batchquantity = 0;
                                decimal basebatchquantity = 0;

                                decimal staggingqty = Convert.ToDecimal(data["Qty"]);
                                for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                                {
                                    batchquantity = batchquantity + Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BatchQty"]);
                                    basebatchquantity = basebatchquantity + Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BaseBatchQty"]);
                                }
                                if (basebatchquantity < staggingqty)
                                {
                                    BL_Registry.SetLogError("Cannot Find  Batch No '" + data["Batch"].ToString() + "' for TransactionNumber " + DocNo + " and Product :" + data["Product"].ToString());
                                    outLoop = true;
                                    break;
                                }

                                if (dsBatch != null && dsBatch.Tables.Count > 0 && dsBatch.Tables[0].Rows.Count > 0)
                                {
                                    for (int P = 0; P < dsBatch.Tables[0].Rows.Count; P++)
                                    {
                                        if (Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BaseBatchQty"]) >= Convert.ToDecimal(data["Qty"]))
                                        {
                                            iBodyCnt++;
                                            dval = 0;
                                            break;
                                        }
                                        else
                                        {
                                            dval = dval - Convert.ToDecimal(dsBatch.Tables[0].Rows[P]["BaseBatchQty"]);
                                            iBodyCnt++;
                                            if (dval <= 0) { break; }
                                        }
                                    }
                                }
                                else
                                {
                                    BL_Registry.SetLogError("Cannot Find  Batch No '" + data["Batch"].ToString() + "' for TransactionNumber " + DocNo + " and Product :" + data["Product"].ToString());
                                    //BL_Registry.SetLogError("Batch (" + data["Batch"].ToString() + ") is not available for this TransactionNumber " + DocNo);
                                    outLoop = true;
                                    break;
                                }
                                #endregion
                                if (iBodyCnt > 0)
                                {
                                    decimal remainingQty = 0;//for storing the intemediate balance qty inside the batches loop
                                    decimal totdisc = 0;
                                    for (int c = 0; c < iBodyCnt; c++)
                                    {
                                        int DefaultBaseUnit = Convert.ToInt32(dsBatch.Tables[0].Rows[c]["iDefaultBaseUnit"]);

                                        decimal batchqty = (Convert.ToDecimal(dsBatch.Tables[0].Rows[c]["BaseBatchQty"]));


                                        remainingQty = (qty - batchqty);
                                        if (remainingQty >= 0)
                                        {
                                            qty = remainingQty;
                                            batchqty = Math.Round(batchqty, 2);
                                        }
                                        else if (remainingQty < 0)
                                        {

                                            batchqty = Math.Round((batchqty + remainingQty), 2);
                                        }

                                        //decimal Gross = qty * Convert.ToDecimal(data["Price"]);

                                        decimal rate = Convert.ToDecimal(data["SellingPrice"]);
                                        decimal gross = batchqty * rate;
                                        decimal TotalDisc = (gross * Convert.ToDecimal(data["DiscPerc"])) / 100;
                                        FinalQty = batchqty;
                                        Batch = data["Batch"] + "^" + dsBatch.Tables[0].Rows[c]["iBatchId"];


                                        #region Discount Calculation
                                        // if the item have discount, it will be distributed to each batch based on the qty with the formaula, (discount*item qty)/ batchqty. The discount has to be rounded to 2 decimal

                                        decimal dis = Math.Round((Convert.ToDecimal(data["DiscPerc"]) * FinalQty) / Convert.ToDecimal(data["Qty"]), 2);
                                        totdisc = totdisc + dis;
                                        if (iBodyCnt == c + 1)
                                        {
                                            #region FinalDiscount
                                            //if there is any mismatch in the value of total discount that need to be adjusted in the last line of item.
                                            if (Convert.ToDecimal(data["DiscPerc"]) != totdisc)
                                            {
                                                decimal dis_adj = 0;
                                                dis_adj = Convert.ToDecimal(data["DiscPerc"]) - totdisc;
                                                dis = dis + dis_adj;
                                            }
                                            #endregion
                                        }

                                        #endregion

                                        #region Batcharr
                                        // as per the new issue, passing batch along with batch id and batch no and qty -- 25/08/2022-- Rosmin
                                        Hashtable batcharr = new Hashtable();
                                        batcharr.Add("BatchId", dsBatch.Tables[0].Rows[c]["iBatchId"]);
                                        batcharr.Add("BatchNo", data["Batch"].ToString());
                                        batcharr.Add("Qty", FinalQty);

                                        #endregion Batcharr
                                        if (data["BatchId"].ToString() == "")
                                        {
                                            Hashtable row = new Hashtable
                                         {
                                             { "Item", ItemId},
                                             { "Description", GetItemDes(ItemId)},
                                             //{ "Description", data["Description"].ToString()},
                                             { "TaxCode__Id", TaxCode},
                                             { "SalesAC__Id", SalesAc},//need to change iDefAcc2
                                             { "Unit__Id",  unitId},
                                             { "Actual Quantity", data["ActualQuantity"] },
                                             { "FOC Quantity", data["FOCQuantity"] },
                                             { "Quantity", FinalQty},
                                             { "Selling Price", data["SellingPrice"] },
                                             //{ "Discount %", data["DiscPerc"] },
                                             { "Input Discount Amt", dis },
                                             //{ "Total Discount", TotalDisc },
                                             { "Add Charges", data["AddCharges"] },
                                             { "VAT", data["Vat"] },
                                             { "Batch", data["Batch"].ToString()},
                                        };
                                            body.Add(row);
                                        }
                                        else
                                        {
                                            Hashtable row = new Hashtable
                                         {
                                             { "Item", ItemId},
                                             { "Description", GetItemDes(ItemId)},
                                             //{ "Description", data["Description"].ToString()},
                                             { "TaxCode__Id", TaxCode},
                                             { "SalesAC__Id", SalesAc},//need to change iDefAcc2
                                             { "Unit__Id",  unitId},
                                             { "Actual Quantity", data["ActualQuantity"] },
                                             { "FOC Quantity", data["FOCQuantity"] },
                                             { "Quantity", FinalQty},
                                             { "Selling Price", data["SellingPrice"] },
                                             //{ "Discount %", data["DiscPerc"] },
                                             { "Input Discount Amt", dis },
                                             //{ "Total Discount", TotalDisc },
                                             { "Add Charges", data["AddCharges"] },
                                             { "VAT", data["Vat"] },
                                             { "Batch", batcharr},
                                        };
                                            body.Add(row);
                                        }
                                    }
                                }
                            }
                        }
                        if (outLoop)
                        {
                            val = 0;
                            break;
                        }
                    }
                    if (outLoop)
                    {
                        val = 0;
                        break;
                        //continue;
                    }
                    #endregion
                    if (body.Count() > 0)
                    {

                        string baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                        string Url = baseUrl + "/Transactions/Vouchers/Sales Invoice - VAN";
                        BL_Registry.SetLog("Sales Invoice - VAN Url: " + Url);
                        var postingData = new PostingData();
                        postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                        string sContent = JsonConvert.SerializeObject(postingData);
                        BL_Registry.SetLog("Sales Invoice - VAN sContent: " + sContent);
                        if (Content == "true")
                        {
                            BL_Registry.SetContentLogError("Sales Invoice - VAN sContent:--  " + sContent);
                        }
                        string err = "";

                        var response = Focus8API.Post(Url, sContent, sessionID, ref err);
                        #region Response
                        if (response != null)
                        {
                            var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                            if (responseData.result == -1)
                            {
                                Message = "Sales Invoice - VAN Posting Failed" + "\n";
                                BL_Registry.SetLog("Sales Invoice - VAN Posted Failed with DocNo: " + DocNo);
                                BL_Registry.SetLog2(response + "\n Sales Invoice - VAN Posted Failed with DocNo: " + DocNo);
                                BL_Registry.SetLogError("Sales Invoice - VAN Posted Failed  with DocNo: " + DocNo + ". Response Error: " + responseData.message);
                                BL_Registry.SetLogError("Sales Invoice - VAN Posted Failed  with DocNo: " + DocNo + ". Response data: " + responseData.data);
                                val = 0;
                            }
                            else
                            {
                                Message = "Sales Invoice - VAN  Posted Successfully" + "\n";
                                BL_Registry.SetLog("Sales Invoice - VAN Posted Success with DocNo: " + DocNo);
                                string UpSql = $@"update Invoice set PostStatus=1 where DocumentNumber='{DocNo}'";
                                int res = cls.Update(UpSql);
                                if (res == 1)
                                {
                                    BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                }
                                val = 1;
                            }
                        }
                        #endregion
                    }
                    if (outLoop == true)
                    {
                        val = 0;
                        BL_Registry.SetLogError("Came out of Loop");
                        break;
                    }
                }
                return val;
            }
            catch (Exception e)
            {
                BL_Registry.SetLog(e.ToString());
                return -1;
            }
        }

        public int PostingReturn(DataSet ds)
        {
            GetExternalData clsd = new GetExternalData();
            bool outLoop = false;
            int compId = BL_Configdata.Focus8CompID;
            sessionID = GetSessionId(compId);//DoLogin();//GetSessionId(compId);

            string Message = "";
            var val = 1;
            try
            {
                var distinctValues = ds.Tables[0].AsEnumerable().Select(_r => _r.Field<string>("DocumentNumber")).Distinct();
                foreach (var dist in distinctValues)
                {
                    val = 1;
                    string str = "DocumentNumber='" + dist + "'";
                    DataRow[] dr = ds.Tables[0].Select(str);
                    var dt = dr.FirstOrDefault();
                    var DocNo = dt["DocumentNumber"].ToString();

                    int date = BL_Registry.GetDateToInt(Convert.ToDateTime(dt["InvoiceDate"].ToString()));
                    int BranchId = GetMasterId(dt["Company"].ToString(), 8); //Companymaster
                    if (BranchId == -1)
                    {
                        BL_Registry.SetLogError("Master Division '" + dt["Company"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Company '" + dt["Company"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int CurId = GetMasterId(dt["Currency"].ToString(), 3);
                    if (CurId == -1)
                    {
                        BL_Registry.SetLogError("Master Currency '" + dt["Currency"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Currency '" + dt["Currency"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int custAcId = GetMasterId(dt["Customer"].ToString(), 6);
                    if (custAcId == -1)
                    {
                        BL_Registry.SetLogError("Master Account '" + dt["Customer"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Customer '" + dt["Customer"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int WareId = GetMasterId(dt["SourceWarehouse"].ToString(), 17);//Outlet
                    if (WareId == -1)
                    {
                        BL_Registry.SetLogError("Master Warehouse '" + dt["SourceWarehouse"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Outlet '" + dt["SourceWarehouse"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int DriverId = GetMasterId(dt["Driver"].ToString(), 9);
                    if (DriverId == -1)
                    {
                        BL_Registry.SetLogError("Master Driver '" + dt["Driver"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Driver '" + dt["Driver"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int EmployeeId = GetMasterId(dt["Employee"].ToString(), 10);
                    if (EmployeeId == -1)
                    {
                        BL_Registry.SetLogError("Master Employee '" + dt["Employee"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Employee '" + dt["Employee"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int HelperId = GetMasterId(dt["Helper"].ToString(), 11);
                    if (HelperId == -1)
                    {
                        BL_Registry.SetLogError("Master Helper '" + dt["Helper"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Helper '" + dt["Helper"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int JurisId = GetMasterId(dt["Jurisdiction"].ToString(), 12);
                    if (JurisId == -1)
                    {
                        BL_Registry.SetLogError("Master Jurisdiction '" + dt["Jurisdiction"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Jurisdiction '" + dt["Jurisdiction"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int PosId = GetMasterId(dt["PlaceOfSupply"].ToString(), 13);
                    if (PosId == -1)
                    {
                        BL_Registry.SetLogError("Master PlaceOfSupply '" + dt["PlaceOfSupply"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("PlaceOfSupply '" + dt["PlaceOfSupply"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }

                    #region Header
                    Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "CustomerAC", custAcId },
                             { "Currency", CurId },
                             { "Company Master__Id", BranchId },
                             { "Outlet__Id", WareId },
                             { "Employee", EmployeeId },
                             { "Driver", DriverId },
                             { "Helper", HelperId },
                             { "Jurisdiction", JurisId },
                             { "Place of supply", PosId },
                             { "LPO_No", dt["LPONumber"].ToString()},
                             { "Driver_Mobile_No", dt["DriverMobileNumber"].ToString() },
                         };
                    #endregion

                    List<Hashtable> body = new List<Hashtable>();
                    #region Body
                    foreach (var data in dr)
                    {
                        //int BatchChk = clsd.BatchCheck(data["Batch"].ToString());
                        //if (BatchChk == 0)
                        //{
                        //    BL_Registry.SetLogError("Cannot Find  Batch No '" + data["Batch"].ToString() + "' for TransactionNumber " + DocNo);
                        //    //BL_Registry.SetLogError("Batch is not available for this TransactionNumber " + DocNo);
                        //    continue;
                        //}

                        int ItemId = GetMasterId(data["Product"].ToString(), 4);
                        if (ItemId == 0)
                        {
                            BL_Registry.SetLogError("Master Product '" + data["Product"].ToString() + "' Not Exist with Transaction : " + DocNo);
                            //BL_Registry.SetLogError("Product '" + data["Product"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int unitId = GetMasterId(data["ProductUnit"].ToString(), 5);
                        if (unitId == 0)
                        {
                            BL_Registry.SetLogError("Master ProductUnit '" + data["ProductUnit"].ToString() + "' Not Exist with Transaction : " + DocNo);
                            //BL_Registry.SetLogError("ProductUnit '" + data["ProductUnit"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int TaxCode = GetMasterId(data["TaxCode"].ToString(), 2);
                        if (TaxCode == 0)
                        {
                            BL_Registry.SetLogError("Master TaxCode '" + data["TaxCode"].ToString() + "' Not Exist with Transaction : " + DocNo);
                            //BL_Registry.SetLogError("TaxCode '" + data["TaxCode"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int SalesAc = GetSalesAc(data["Product"].ToString());
                        if (SalesAc == 0)
                        {
                            BL_Registry.SetLogError("SalesAc not mapped to Item '" + data["Product"].ToString() + "' with Transaction : " + DocNo);
                            //BL_Registry.SetLogError("SalesAc not mapped to Item '" + data["Product"].ToString() + "' with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }


                        int MfgDate = 0;
                        int ExpDate = 0;
                        if (data["MfgDate"].ToString() == "01-01-1900 00:00:00")
                        {
                            MfgDate = 0;
                        }
                        else
                        {
                            MfgDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["MfgDate"].ToString()));
                        }

                        if (data["ExpiryDate"].ToString() == "01-01-1900 00:00:00")
                        {
                            ExpDate = 0;
                        }
                        else
                        {
                            ExpDate = BL_Registry.GetDateToInt(Convert.ToDateTime(data["ExpiryDate"].ToString()));
                        }

                        //string TaxCode = "EX";
                        //if (taxamt > 0)
                        //{
                        //    TaxCode = "SR";
                        //}

                        List<Hashtable> LinkList = new List<Hashtable>();
                        DataSet TransDs;
                        BL_Registry.SetLogError(data["BatchId"].ToString());
                        if (data["BatchId"].ToString() == "" || data["BatchId"] == null || data["BatchId"].ToString().ToLower() == "null")
                        {
                            TransDs = GetReturnDSTranID2(data["LPONumber"].ToString(), ItemId, data["Batch"].ToString(), 1);
                        }
                        else
                        {
                            TransDs = GetReturnDSTranID(data["LPONumber"].ToString(), ItemId, data["Batch"].ToString(), 1, data["BatchId"].ToString());
                        }
                            
                        if (TransDs.Tables[0].Rows.Count != 0)
                        {
                            TransID = Convert.ToInt32(TransDs.Tables[0].Rows[0]["ibodyid"]);
                            int LinkID = Convert.ToInt32(TransDs.Tables[0].Rows[0]["iLinkId"]);
                            decimal UsedValue = Convert.ToDecimal(TransDs.Tables[0].Rows[0]["penvalue"]);

                            Hashtable LinkRef = new Hashtable
                         {
                             { "BaseTransactionId", TransID},
                             { "VoucherType", "3335"},
                             { "VoucherNo", "sinv:" + TransDs.Tables[0].Rows[0]["svoucherno"].ToString()},
                             { "UsedValue", UsedValue},
                             { "LinkId",  LinkID},
                             { "RefId", TransID }
                         };

                            LinkList.Add(LinkRef);
                        }
                        //else
                        //{
                        //    val = 0;
                        //    BL_Registry.SetLogError("Reference Not found");
                        //    outLoop = true;
                        //    break;
                        //}

                        List<Hashtable> RefList = new List<Hashtable>();

                        DataSet DsRef = GetIRefDs(data["LPONumber"].ToString());
                        if (DsRef.Tables[0].Rows.Count != 0)
                        {
                            iref = Convert.ToInt32(DsRef.Tables[0].Rows[0]["RefId"]);
                            #region BillRef
                            if (iref != 0)
                            {
                                string reference = DsRef.Tables[0].Rows[0]["VNo"] + ":" + DsRef.Tables[0].Rows[0]["Date"].ToString();
                                decimal qty = Convert.ToDecimal(data["Qty"]);
                                decimal RefQuantity = Convert.ToDecimal(DsRef.Tables[0].Rows[0]["Quantity"]);
                                decimal CalcValue = Convert.ToDecimal(DsRef.Tables[0].Rows[0]["fOrigNet"]) / RefQuantity;
                                decimal Amount = CalcValue * qty;

                                Hashtable billRef = new Hashtable();
                                billRef.Add("aptag", 0);
                                billRef.Add("CustomerId", custAcId);
                                billRef.Add("Amount", Amount);
                                billRef.Add("BillNo", "");
                                billRef.Add("reftype", 2);
                                billRef.Add("mastertypeid", 0);
                                billRef.Add("Reference", reference);
                                billRef.Add("artag", 0);
                                billRef.Add("ref", iref);
                                billRef.Add("tag", 0);
                                RefList.Add(billRef);
                            }
                            else
                            {
                                val = 0;
                                BL_Registry.SetLogError("Reference Not found");
                                outLoop = true;
                                break;
                            }
                            #endregion
                        }
                        #region Batcharr
                        // as per the new issue, passing batch along with batch id and batch no and qty -- 25/08/2022-- Rosmin
                        Hashtable batcharr = new Hashtable();
                        batcharr.Add("BatchId", data["BatchId"].ToString());
                        batcharr.Add("BatchNo", data["Batch"].ToString());
                        batcharr.Add("Qty", Convert.ToDecimal(data["ActualQuantity"].ToString()) + Convert.ToDecimal(data["FOCQuantity"].ToString()));
                        #endregion Batcharr

                        Hashtable row = new Hashtable();
                        if (iref != 0)
                        {
                            if (data["BatchId"].ToString() == "")
                            {
                                row = new Hashtable()
                         {
                             { "Item", ItemId},
                             { "Description", GetItemDes(ItemId)},
                             //{ "Description", data["Description"].ToString()},
                             { "TaxCode", TaxCode},
                             { "SalesAC", SalesAc},
                             { "Unit",  unitId},
                             { "Actual Quantity", data["ActualQuantity"] },
                             { "FOC Quantity", data["FOCQuantity"] },
                             { "Quantity", Convert.ToDecimal(data["ActualQuantity"].ToString())+ Convert.ToDecimal(data["FOCQuantity"].ToString()) },
                             { "L-Sales Invoice - VAN", LinkList },
                             { "Selling Price", data["SellingPrice"] },
                             //{ "Rate", data["Price"] },
                             //{ "Gross", data["LineAmount"] },
                             //{ "Discount %", data["DiscPerc"] },
                             { "Input Discount Amt", Convert.ToDecimal(data["DiscPerc"])*-1 },
                             //{ "Input Discount Amt", data["LineDiscount"] },
                             { "Add Charges", data["AddCharges"] },
                             { "VAT", data["Vat"] },
                             { "Batch", data["Batch"].ToString() },
                             { "MfgDate",MfgDate },
                             { "ExpDate", ExpDate },
                             { "Reference", RefList },
                         };
                            }
                            else
                            {
                                row = new Hashtable()
                         {
                             { "Item", ItemId},
                             { "Description", GetItemDes(ItemId)},
                             //{ "Description", data["Description"].ToString()},
                             { "TaxCode", TaxCode},
                             { "SalesAC", SalesAc},
                             { "Unit",  unitId},
                             { "Actual Quantity", data["ActualQuantity"] },
                             { "FOC Quantity", data["FOCQuantity"] },
                             { "Quantity", Convert.ToDecimal(data["ActualQuantity"].ToString())+ Convert.ToDecimal(data["FOCQuantity"].ToString()) },
                             { "L-Sales Invoice - VAN", LinkList },
                             { "Selling Price", data["SellingPrice"] },
                             //{ "Rate", data["Price"] },
                             //{ "Gross", data["LineAmount"] },
                             //{ "Discount %", data["DiscPerc"] },
                             { "Input Discount Amt", Convert.ToDecimal(data["DiscPerc"])*-1 },
                             //{ "Input Discount Amt", data["LineDiscount"] },
                             { "Add Charges", data["AddCharges"] },
                             { "VAT", data["Vat"] },
                             { "Batch", batcharr },
                             { "MfgDate",MfgDate },
                             { "ExpDate", ExpDate },
                             { "Reference", RefList },
                         };
                            }
                        }
                        else
                        {
                            if (data["BatchId"].ToString() == "")
                            {
                                row = new Hashtable()
                         {
                             { "Item", ItemId},
                             //{ "Description", GetItemDes(ItemId)},
                             { "Description", data["Description"].ToString()},
                             { "TaxCode", TaxCode},
                             { "SalesAC", SalesAc},
                             { "Unit",  unitId},
                             { "Actual Quantity", data["ActualQuantity"] },
                             { "FOC Quantity", data["FOCQuantity"] },
                             //{ "Quantity", data["Qty"] },
                             { "L-Sales Invoice - VAN", LinkList },
                             { "Selling Price", data["SellingPrice"] },
                             //{ "Rate", data["Price"] },
                             //{ "Gross", data["LineAmount"] },
                             { "Discount %", data["DiscPerc"] },
                             //{ "Input Discount Amt", data["LineDiscount"] },
                             { "Add Charges", data["AddCharges"] },
                             { "VAT", data["Vat"] },
                             { "Batch", data["Batch"].ToString() },
                             { "MfgDate",MfgDate },
                             { "ExpDate", ExpDate },
                         };
                            }
                            else
                            {
                                row = new Hashtable()
                         {
                             { "Item", ItemId},
                             //{ "Description", GetItemDes(ItemId)},
                             { "Description", data["Description"].ToString()},
                             { "TaxCode", TaxCode},
                             { "SalesAC", SalesAc},
                             { "Unit",  unitId},
                             { "Actual Quantity", data["ActualQuantity"] },
                             { "FOC Quantity", data["FOCQuantity"] },
                             //{ "Quantity", data["Qty"] },
                             { "L-Sales Invoice - VAN", LinkList },
                             { "Selling Price", data["SellingPrice"] },
                             //{ "Rate", data["Price"] },
                             //{ "Gross", data["LineAmount"] },
                             { "Discount %", data["DiscPerc"] },
                             //{ "Input Discount Amt", data["LineDiscount"] },
                             { "Add Charges", data["AddCharges"] },
                             { "VAT", data["Vat"] },
                             { "Batch", batcharr },
                             { "MfgDate",MfgDate },
                             { "ExpDate", ExpDate },
                         };
                            }
                        }
                        body.Add(row);
                    }

                    #endregion

                    if (body.Count() > 0)
                    {
                        string baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                        string Url = baseUrl + "/Transactions/Vouchers/Sales Return - VAN";

                        var postingData = new PostingData();
                        postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                        string sContent = JsonConvert.SerializeObject(postingData);
                        BL_Registry.SetLog("Content : " + sContent);
                        if (Content == "true")
                        {
                            BL_Registry.SetContentLogError("Sales Return - VAN sContent:--  " + sContent);
                        }
                        string err = "";

                        var response = Focus8API.Post(Url, sContent, sessionID, ref err);
                        #region Response
                        if (response != null)
                        {
                            var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                            if (responseData.result == -1)
                            {

                                Message = "Sales Return - VAN Posting Failed" + "\n";
                                BL_Registry.SetLogError("Sales Return - VAN Posted Failed with DocNo: " + DocNo);
                                BL_Registry.SetLog2(response + "\n Sales Return - VAN Posted Failed with DocNo: " + DocNo);
                                BL_Registry.SetLogError("Sales Return - VAN Posted Posted Failed  with DocNo: " + DocNo + ". Response Error: " + responseData.message);
                                val = 0;
                            }
                            else
                            {
                                Message = "Sales Return - VAN  Posted Successfully" + "\n";
                                BL_Registry.SetLog("Sales Return - VAN Posted Success with DocNo: " + DocNo);
                                string UpSql = $@"update Invoice set PostStatus=1 where DocumentNumber='{DocNo}'";
                                int res = cls.Update(UpSql);
                                if (res == 1)
                                {
                                    BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                }
                                val = 1;
                            }
                        }
                        #endregion
                    }
                    if (outLoop == true)
                    {
                        BL_Registry.SetLogError("Came out of Loop");
                        break;
                    }
                }
                return val;
            }
            catch (Exception e)
            {
                BL_Registry.SetLog(e.ToString());
                return -1;
            }
        }

        public int PostingCash(DataSet ds)
        {
            BL_Registry.SetLog("Entered PostingCash");
            GetExternalData clsd = new GetExternalData();
            int compId = BL_Configdata.Focus8CompID;
            sessionID = GetSessionId(compId);//DoLogin();//GetSessionId(compId);
            bool outLoop = false;
            string Message = "";
            var val = 1;
            try
            {
                var distinctValues = ds.Tables[0].AsEnumerable().Select(_r => _r.Field<string>("DocumentNumber")).Distinct();
                foreach (var dist in distinctValues)
                {
                    val = 1;
                    string str = "DocumentNumber='" + dist + "'";
                    DataRow[] dr = ds.Tables[0].Select(str);
                    BL_Registry.SetLog("dr count = " + dr.Length);
                    var dt = dr.FirstOrDefault();
                    var DocNo = dt["DocumentNumber"].ToString();
                    BL_Registry.SetLog("docno = " + DocNo);
                    int date = BL_Registry.GetDateToInt(Convert.ToDateTime(dt["PaymentDate"].ToString()));
                    //getting Masters ID                    
                    int CashBankAc = GetMasterId(dt["Salesman"].ToString(), 14);
                    if (CashBankAc == -1)
                    {
                        BL_Registry.SetLogError("Master Account '" + dt["Salesman"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("VanNumber '" + dt["VanNumber"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int BranchId = GetMasterId(dt["Branch"].ToString(), 8);
                    if (BranchId == -1)
                    {
                        BL_Registry.SetLogError("Master Branch '" + dt["Branch"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Branch '" + dt["Branch"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int OutletId = GetMasterId(dt["VanNumber"].ToString(), 17);
                    if (OutletId == -1)
                    {
                        BL_Registry.SetLogError("Master Outlet '" + dt["VanNumber"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("VanNumber '" + dt["VanNumber"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int EmployeeId = GetMasterId(dt["Salesman"].ToString(), 10);
                    if (EmployeeId == -1)
                    {
                        BL_Registry.SetLogError("Master Salesman '" + dt["Salesman"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Salesman '" + dt["Salesman"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int CurId = GetMasterId("AED", 3);
                    if (CurId == -1)
                    {
                        BL_Registry.SetLogError("Master Currency 'AED' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Currency 'AED' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int Account = GetMasterId(dt["Customer"].ToString(), 6);
                    if (Account == -1)
                    {
                        BL_Registry.SetLogError("Master Customer Account '" + dt["Customer"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Customer '" + data["Customer"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    #region Header
                    Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "CashBankAC",CashBankAc },
                             { "Currency",CurId },
                             { "ExchangeRate",dt["ExchangeRate"].ToString() },
                             { "Company Master__Id", BranchId},
                             { "Employee", EmployeeId },
                             { "Outlet__Id",OutletId } //InvoiceNumber                       
                         };
                    #endregion

                    List<Hashtable> body = new List<Hashtable>();
                    bool BodyLoopBreaks = false;
                    #region Body
                    foreach (var data in dr)
                    {
                        string str1 = $@"EXEC pCore_CommonSp @Operation=InvoiceChecking, @p2= '{data["Reference"].ToString()}'";
                        DataSet ds1 = clsd.getFn(str1);
                        decimal InvAmt = 0;
                        if (ds1 != null)
                        {
                            if (ds1.Tables.Count > 0)
                            {
                                if (ds1.Tables[0].Rows.Count > 0)
                                {
                                    if (ds1.Tables[0].Rows[0][0].ToString() == "0")
                                    {
                                        BL_Registry.SetLog("Invoice Not Found");
                                        BodyLoopBreaks = true;
                                        //DataSet ids = clsd.GetExSales2(data["Reference"].ToString());
                                        //int a = PostingSI(ids);
                                        //if (a > 0)
                                        //{
                                        //    BL_Registry.SetLog("Invoice POsted Sucessfully");
                                        //}
                                        //else
                                        //{
                                        //    BL_Registry.SetLog("Invoice Posting Failed");
                                        //    break;
                                        //}
                                    }
                                    else if (ds1.Tables[0].Rows[0][0].ToString() == "1")
                                    {
                                        BL_Registry.SetLog("Invoice Found. Post Receipt");
                                    }
                                    else if (Convert.ToInt32(ds1.Tables[0].Rows[0][0]) == -1)
                                    {
                                        BL_Registry.SetLog("Invoice Found and it is Adjusted");
                                        //BodyLoopBreaks = true;
                                    }
                                    else
                                    {
                                        BL_Registry.SetLog("Invoice Found and it is Partially Adjusted");
                                        InvAmt = Convert.ToDecimal(ds1.Tables[0].Rows[0][0].ToString());
                                        if (InvAmt < Convert.ToDecimal(data["Amount"].ToString()))
                                        {
                                            BL_Registry.SetLog("Invoice Found but the receipt amount is greater than invoice amount");
                                            //BodyLoopBreaks = true;
                                        }
                                        else
                                        {
                                            BL_Registry.SetLog("Invoice is Partially Adjusted.Post Receipt");
                                        }
                                    }
                                }
                                else
                                {
                                    BL_Registry.SetLog("NOT ds1.Tables[0].Rows.Count > 0");
                                    BodyLoopBreaks = true;
                                }
                            }
                            else
                            {
                                BL_Registry.SetLog("NOT ds1.Tables.Count > 0");
                                BodyLoopBreaks = true;
                            }
                        }
                        else
                        {
                            BL_Registry.SetLog("NOT ds1 != null");
                            BodyLoopBreaks = true;
                        }
                        if (BodyLoopBreaks)
                        {
                            break;
                        }
                        GetIRef(data["Reference"].ToString());
                        Hashtable row = new Hashtable();
                        if (iref != 0)
                        {
                            Hashtable billRef = new Hashtable();
                            billRef.Add("aptag", 0);
                            billRef.Add("CustomerId", Account);
                            billRef.Add("Amount", data["Amount"]);
                            billRef.Add("BillNo", "");
                            billRef.Add("reftype", 2);
                            billRef.Add("mastertypeid", 0);
                            billRef.Add("Reference", "SINV:" + data["Reference"].ToString());
                            billRef.Add("artag", 0);
                            billRef.Add("ref", iref);
                            billRef.Add("tag", 0);

                            row = new Hashtable
                         {
                             { "Account", Account},
                             { "Amount",data["Amount"]},
                             { "Reference",billRef},
                             { "sRemarks",data["Remarks"]}
                         };
                        }
                        else
                        {
                            val = 0;
                            BL_Registry.SetLogError("Reference Not found");
                            outLoop = true;
                            break;
                            //   row = new Hashtable
                            //{
                            //    { "Account", Account},
                            //    { "Amount",data["Amount"]},
                            //    { "Reference", ""},
                            //    { "sRemarks",data["Remarks"]}
                            //};
                        }
                        body.Add(row);
                    }

                    #endregion
                    if (body.Count() > 0)
                    {
                        string baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                        string Url = baseUrl + "/Transactions/Vouchers/Cash Receipts VAN";

                        var postingData = new PostingData();
                        postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                        string sContent = JsonConvert.SerializeObject(postingData);
                        BL_Registry.SetLog("Content : " + sContent);
                        if (Content == "true")
                        {
                            BL_Registry.SetContentLogError("Cash Receipts VAN sContent:--  " + sContent);
                        }
                        string err = "";

                        #region Response
                        var response = Focus8API.Post(Url, sContent, sessionID, ref err);
                        if (response != null)
                        {
                            var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                            if (responseData.result == -1)
                            {
                                Message = "Cash Receipts VAN Posting Failed" + "\n";
                                BL_Registry.SetLogError("Cash Receipts VAN Posted Failed with DocNo: " + DocNo);
                                BL_Registry.SetLogError("Cash Receipts VAN Posted Failed  with DocNo: " + DocNo + "Response Error: " + responseData.message);
                                val = 0;
                            }
                            else
                            {
                                Message = "Cash Receipts VAN Posted Successfully" + "\n";
                                BL_Registry.SetLog("Cash Receipts VAN Posted Success with DocNo: " + DocNo);
                                string UpSql = $@"update Receipt set PostStatus=1 where DocumentNumber='{DocNo}'";
                                int res = cls.Update(UpSql);
                                if (res == 1)
                                {
                                    BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                }
                                val = 1;
                            }
                        }
                        #endregion
                    }

                    if (outLoop == true)
                    {
                        val = 0;
                        BL_Registry.SetLogError("Came out of Loop");
                        break;
                    }
                }
                return val;
            }
            catch (Exception e)
            {
                BL_Registry.SetLog(e.ToString());
                return -1;
            }
        }

        public int PostingCashPayment(DataSet ds)
        {
            BL_Registry.SetLog("Entered PostingCashPayment");
            GetExternalData clsd = new GetExternalData();
            int compId = BL_Configdata.Focus8CompID;
            sessionID = GetSessionId(compId);//DoLogin();//GetSessionId(compId);
            bool outLoop = false;
            string Message = "";
            var val = 1;
            try
            {
                var distinctValues = ds.Tables[0].AsEnumerable().Select(_r => _r.Field<string>("DocumentNumber")).Distinct();
                foreach (var dist in distinctValues)
                {
                    val = 1;
                    string str = "DocumentNumber='" + dist + "'";
                    DataRow[] dr = ds.Tables[0].Select(str);
                    var dt = dr.FirstOrDefault();
                    var DocNo = dt["DocumentNumber"].ToString();
                    int date = BL_Registry.GetDateToInt(Convert.ToDateTime(dt["PaymentDate"].ToString()));
                    //getting Masters ID                    
                    int CashBankAc = GetMasterId(dt["Salesman"].ToString(), 14);
                    if (CashBankAc == -1)
                    {
                        BL_Registry.SetLogError("Master Account '" + dt["Salesman"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("VanNumber '" + dt["VanNumber"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int BranchId = GetMasterId(dt["Branch"].ToString(), 8);
                    if (BranchId == -1)
                    {
                        BL_Registry.SetLogError("Master Branch '" + dt["Branch"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Branch '" + dt["Branch"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int OutletId = GetMasterId(dt["VanNumber"].ToString(), 17);
                    if (OutletId == -1)
                    {
                        BL_Registry.SetLogError("Master Outlet '" + dt["VanNumber"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("VanNumber '" + dt["VanNumber"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int EmployeeId = GetMasterId(dt["Salesman"].ToString(), 10);
                    if (EmployeeId == -1)
                    {
                        BL_Registry.SetLogError("Master Salesman '" + dt["Salesman"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Salesman '" + dt["Salesman"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int CurId = GetMasterId("AED", 3);
                    if (CurId == -1)
                    {
                        BL_Registry.SetLogError("Master Currency 'AED' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Currency 'AED' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }

                    #region Header
                    Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "CashBankAC",CashBankAc },
                             { "Currency",CurId },
                             { "ExchangeRate",dt["ExchangeRate"].ToString() },
                             { "Company Master__Id", BranchId},
                             { "Employee", EmployeeId },
                             { "Outlet__Id",OutletId } //InvoiceNumber                       
                         };
                    #endregion

                    List<Hashtable> body = new List<Hashtable>();

                    #region Body
                    foreach (var data in dr)
                    {
                        int Account = GetMasterId(data["Customer"].ToString(), 6);
                        if (Account == -1)
                        {
                            BL_Registry.SetLogError("Master Customer Account '" + dt["Customer"].ToString() + "' Not Exist with Transaction : " + DocNo);
                            //BL_Registry.SetLogError("Customer '" + data["Customer"].ToString() + "' Not Found with DocNo: " + DocNo);
                            outLoop = true;
                            break;
                        }
                        BL_Registry.SetLog("PostingCashPayment Reference = " + data["Reference"].ToString());
                        GetIRef(data["Reference"].ToString());
                        BL_Registry.SetLog("PostingCashPayment iref = " + iref);
                        Hashtable row = new Hashtable();
                        if (iref != 0)
                        {
                            Hashtable billRef = new Hashtable();
                            billRef.Add("aptag", 0);
                            billRef.Add("CustomerId", Account);
                            billRef.Add("Amount", data["Amount"]);
                            billRef.Add("BillNo", "");
                            billRef.Add("reftype", 2);
                            billRef.Add("mastertypeid", 0);
                            billRef.Add("Reference", "SRTV:" + data["Reference"].ToString());
                            billRef.Add("artag", 0);
                            billRef.Add("ref", iref);
                            billRef.Add("tag", 0);

                            row = new Hashtable
                         {
                             { "Account", Account},
                             { "Amount",data["Amount"]},
                             { "Reference",billRef},
                             { "sRemarks",data["Remarks"]}
                         };
                        }
                        else
                        {
                            val = 0;
                            BL_Registry.SetLogError("Reference Not found");
                            outLoop = true;
                            break;
                            //   row = new Hashtable
                            //{
                            //    { "Account", Account},
                            //    { "Amount",data["Amount"]},
                            //    { "Reference", ""},
                            //    { "sRemarks",data["Remarks"]}
                            //};
                        }
                        body.Add(row);
                    }

                    #endregion
                    if (body.Count() > 0)
                    {
                        string baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                        string Url = baseUrl + "/Transactions/Vouchers/Cash Payment VAN";

                        var postingData = new PostingData();
                        postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                        string sContent = JsonConvert.SerializeObject(postingData);
                        BL_Registry.SetLog("Content : " + sContent);
                        if (Content == "true")
                        {
                            BL_Registry.SetContentLogError("Cash Payment VAN sContent:--  " + sContent);
                        }
                        string err = "";

                        #region Response
                        var response = Focus8API.Post(Url, sContent, sessionID, ref err);
                        if (response != null)
                        {
                            var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                            if (responseData.result == -1)
                            {
                                Message = "Cash Payment VAN Posting Failed" + "\n";
                                BL_Registry.SetLogError("Cash Payment VAN Posted Failed with DocNo: " + DocNo);
                                BL_Registry.SetLogError("Cash Payment VAN Posted Failed  with DocNo: " + DocNo + "Response Error: " + responseData.message);
                                val = 0;
                            }
                            else
                            {
                                Message = "Cash Payment VAN Posted Successfully" + "\n";
                                BL_Registry.SetLog("Cash Payment VAN Posted Success with DocNo: " + DocNo);
                                string UpSql = $@"update Receipt set PostStatus=1 where DocumentNumber='{DocNo}'";
                                int res = cls.Update(UpSql);
                                if (res == 1)
                                {
                                    BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                                }
                                val = 1;
                            }
                        }
                        #endregion
                    }
                    if (outLoop == true)
                    {
                        BL_Registry.SetLogError("Came out of Loop");
                        break;
                    }
                }
                return val;
            }
            catch (Exception e)
            {
                BL_Registry.SetLog(e.ToString());
                return -1;
            }
        }

        public int PostingPDC(DataSet ds)
        {
            BL_Registry.SetLog("Entered PostingPDC");
            GetExternalData clsd = new GetExternalData();
            int compId = BL_Configdata.Focus8CompID;
            bool outLoop = false;
            sessionID = GetSessionId(compId);//DoLogin();//GetSessionId(compId);
            BL_Registry.SetLog("PostingPDC sessionID" + sessionID);
            string Message = "";
            var val = 1;
            try
            {
                DataView view = new DataView(ds.Tables[0]);
                DataTable distinctValues = view.ToTable(true, "rno");
                foreach (DataRow drw in distinctValues.Rows)
                {

                    val = 1;
                    int rno = Convert.ToInt32(drw["rno"]);
                    BL_Registry.SetLog("PostingPDC rno" + rno);
                    DataRow[] result = ds.Tables[0].Select("rno = " + rno);
                    var dt = result[0];
                    var DocNo = "Customer = " + dt["Customer"].ToString() + ", ChequeNo = " + dt["ChequeNumber"].ToString();
                    BL_Registry.SetLog("PostingPDC DocNo" + DocNo);
                    int date = BL_Registry.GetDateToInt(Convert.ToDateTime(dt["PaymentDate"].ToString()));
                    int Mdate = BL_Registry.GetDateToInt(Convert.ToDateTime(dt["MaturityDate"].ToString()));
                    //getting Masters ID                    

                    int CashBankAc = GetMasterId(dt["Salesman"].ToString(), 16);
                    if (CashBankAc == -1)
                    {
                        BL_Registry.SetLogError("Master Account '" + dt["Salesman"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("VanNumber '" + dt["VanNumber"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int BranchId = GetMasterId(dt["Branch"].ToString(), 8);
                    if (BranchId == -1)
                    {
                        BL_Registry.SetLogError("Master Branch '" + dt["Branch"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Branch '" + dt["Branch"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int OutletId = GetMasterId(dt["VanNumber"].ToString(), 17);
                    if (OutletId == -1)
                    {
                        BL_Registry.SetLogError("Master Account '" + dt["VanNumber"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("VanNumber '" + dt["VanNumber"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int EmployeeId = GetMasterId(dt["Salesman"].ToString(), 10);
                    if (EmployeeId == -1)
                    {
                        BL_Registry.SetLogError("Master Salesman '" + dt["Salesman"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Salesman '" + dt["Salesman"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int CurId = GetMasterId("AED", 3);
                    if (CurId == -1)
                    {
                        BL_Registry.SetLogError("Master Currency 'AED' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Currency 'AED' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int Account = GetMasterId(dt["Customer"].ToString(), 6);
                    if (Account == -1)
                    {
                        BL_Registry.SetLogError("Master Customer Account '" + dt["Customer"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("Customer '" + data["Customer"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    string DocNosStr = "";
                    List<Hashtable> Mainbill = new List<Hashtable>();
                    List<Hashtable> body = new List<Hashtable>();
                    bool BodyLoopBreaks = false;
                    Hashtable row = new Hashtable();
                    decimal AmtSum = 0;
                    string remarks = "";
                    foreach (DataRow data in result)
                    {
                        #region Body
                        DocNosStr = DocNosStr + data["DocumentNumber"].ToString().Trim() + ",";
                        AmtSum = AmtSum + Convert.ToDecimal(data["Amount"].ToString());
                        remarks = data["Remarks"].ToString().Trim();
                        string str1 = $@"EXEC pCore_CommonSp @Operation=InvoiceChecking, @p2= '{data["Reference"].ToString()}'";
                        DataSet ds1 = clsd.getFn(str1);
                        decimal InvAmt = 0;
                        if (ds1 != null)
                        {
                            if (ds1.Tables.Count > 0)
                            {
                                if (ds1.Tables[0].Rows.Count > 0)
                                {
                                    if (ds1.Tables[0].Rows[0][0].ToString() == "0")
                                    {
                                        BL_Registry.SetLog("Invoice Not Found");
                                        BodyLoopBreaks = true;
                                        //DataSet ids = clsd.GetExSales2(data["Reference"].ToString());
                                        //int a = PostingSI(ids);
                                        //if (a > 0)
                                        //{
                                        //    continue;
                                        //}
                                        //else
                                        //{
                                        //    BL_Registry.SetLog("Invoice Posting Failed");
                                        //    break;
                                        //}
                                    }
                                    else if (ds1.Tables[0].Rows[0][0].ToString() == "1")
                                    {
                                        BL_Registry.SetLog("Invoice Found. Post PDC");
                                    }
                                    else if (ds1.Tables[0].Rows[0][0].ToString() == "-1")
                                    {
                                        BL_Registry.SetLog("Invoice Found and it is Adjusted");
                                        //BodyLoopBreaks = true; //01/09/2022
                                    }
                                    else
                                    {
                                        BL_Registry.SetLog("Invoice Found and it is Partially Adjusted");
                                        InvAmt = Convert.ToDecimal(ds1.Tables[0].Rows[0][0].ToString());
                                        if (InvAmt < Convert.ToDecimal(data["Amount"].ToString()))
                                        {
                                            BL_Registry.SetLog("Invoice Found but the receipt amount is greater than invoice amount");
                                            //BodyLoopBreaks = true; //01/09/2022
                                        }
                                        else
                                        {
                                            BL_Registry.SetLog("Invoice is Partially Adjusted.Post Receipt");
                                        }
                                    }
                                }
                                else
                                {
                                    BL_Registry.SetLog("NOT ds1.Tables[0].Rows.Count > 0");
                                    BodyLoopBreaks = true;
                                }
                            }
                            else
                            {
                                BL_Registry.SetLog("NOT ds1.Tables.Count > 0");
                                BodyLoopBreaks = true;
                            }
                        }
                        else
                        {
                            BL_Registry.SetLog("NOT ds1 != null");
                            BodyLoopBreaks = true;
                        }
                        if (BodyLoopBreaks)
                        {
                            break;
                        }
                        GetIRef(data["Reference"].ToString());
                        if (iref != 0)
                        {
                            Hashtable billRef = new Hashtable();
                            billRef.Add("aptag", 0);
                            billRef.Add("CustomerId", Account);
                            billRef.Add("Amount", data["Amount"]);
                            billRef.Add("BillNo", "");
                            billRef.Add("reftype", 2);
                            billRef.Add("mastertypeid", 0);
                            billRef.Add("Reference", "SINV:" + data["Reference"].ToString());
                            billRef.Add("artag", 0);
                            billRef.Add("ref", iref);
                            billRef.Add("tag", 0);
                            Mainbill.Add(billRef);
                        }

                        #endregion
                    }
                    if (Mainbill.Count > 0)
                    {

                        row = new Hashtable
                         {
                             { "Account", Account},
                             { "Amount",AmtSum},
                             { "Reference",Mainbill},
                             { "sRemarks",remarks}
                         };
                    }
                    else
                    {
                        val = 0;
                        BL_Registry.SetLogError("Reference Not found");
                        outLoop = true;
                        break;
                        //row = new Hashtable
                        // {
                        //     { "Account", Account},
                        //     { "Amount",AmtSum},
                        //     { "Reference", ""},
                        //     { "sRemarks",remarks}
                        // };
                    }
                    body.Add(row);
                    DocNosStr = DocNosStr.Remove(DocNosStr.Length - 1);
                    BL_Registry.SetLog("Header Start");
                    #region Header
                    Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "sNarration",DocNosStr },
                             { "CashBankAC",CashBankAc },
                             { "Currency",CurId },
                             { "ExchangeRate",dt["ExchangeRate"].ToString() },
                             { "Company Master__Id", BranchId},
                             { "Employee", EmployeeId },
                             { "Outlet__Id",OutletId }, //InvoiceNumber  
                         { "sChequeNo", dt["ChequeNumber"].ToString() },
                         { "MaturityDate", Mdate },
                         };
                    #endregion
                    BL_Registry.SetLog("Header End");



                    if (body.Count() > 0)
                    {
                        string baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                        string Url = baseUrl + "/Transactions/Vouchers/Post Dated Receipt VAN";

                        var postingData = new PostingData();
                        postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                        string sContent = JsonConvert.SerializeObject(postingData);
                        BL_Registry.SetLog("Content : " + sContent);
                        if (Content == "true")
                        {
                            BL_Registry.SetContentLogError("Post Dated Receipt VAN sContent:--  " + sContent);
                        }
                        string err = "";

                        var response = Focus8API.Post(Url, sContent, sessionID, ref err);
                        #region Response
                        if (response != null)
                        {
                            var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                            if (responseData.result == -1)
                            {
                                Message = "Post Dated Receipt VAN Posting Failed" + "\n";
                                BL_Registry.SetLogError("Post Dated Receipt VAN Posted Failed with DocNo: " + DocNo);
                                BL_Registry.SetLogError("Post Dated Receipt VAN Posted Failed  with DocNo: " + DocNo + "Response Error: " + responseData.message);
                                val = 0;
                            }
                            else
                            {
                                Message = "Post Dated Receipt VAN Posted Successfully" + "\n";
                                BL_Registry.SetLog("Post Dated Receipt VAN Posted Success with DocNo: " + DocNo);
                                foreach (DataRow drw2 in result)
                                {
                                    string dno = drw2["DocumentNumber"].ToString();
                                    string UpSql = $@"update Receipt set PostStatus=1 where DocumentNumber='{dno}'";
                                    int res = cls.Update(UpSql);
                                    if (res == 1)
                                    {
                                        BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + dno);
                                        val = 1;
                                    }
                                    else
                                    {
                                        val = 0;
                                        BL_Registry.SetLogError("Updating PostStatus got Fail");
                                        outLoop = false;
                                        break;
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                    if (outLoop == true)
                    {
                        BL_Registry.SetLogError("Came out of Loop");
                        break;
                    }
                }
                return val;
            }
            catch (Exception e)
            {
                BL_Registry.SetLog(e.ToString());
                return -1;
            }
        }

        public int PostingEEV(DataSet ds)
        {
            GetExternalData clsd = new GetExternalData();
            int compId = BL_Configdata.Focus8CompID;
            sessionID = GetSessionId(compId);//DoLogin();//GetSessionId(compId);
            bool outLoop = false;
            string Message = "";
            var val = 1;
            try
            {
                var distinctValues = ds.Tables[0].AsEnumerable().Select(_r => _r.Field<string>("DocumentNumber")).Distinct();
                foreach (var dist in distinctValues)
                {
                    val = 1;
                    string str = "DocumentNumber='" + dist + "'";
                    DataRow[] dr = ds.Tables[0].Select(str);
                    var dt = dr.FirstOrDefault();
                    var DocNo = dt["DocumentNumber"].ToString();
                    int date = BL_Registry.GetDateToInt(Convert.ToDateTime(dt["ExpenseDate"].ToString()));
                    //GetDefAcc(5891);
                    //getting Masters ID                    
                    int CurId = GetMasterId("AED", 3);
                    if (CurId == -1)
                    {
                        BL_Registry.SetLogError("Master Currency 'AED' Not Exist with Transaction : " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int CashBankAc = GetMasterId(dt["Salesman"].ToString(), 14);
                    if (CashBankAc == -1)
                    {
                        BL_Registry.SetLogError("Master Account '" + dt["Salesman"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        //BL_Registry.SetLogError("VanNumber '" + dt["VanNumber"].ToString() + "' Not Found with DocNo: " + DocNo);
                        outLoop = true;
                        break;
                    }
                    //int CashBankAc = GetMasterId(dt["VanNumber"].ToString(), 14);
                    //if (CashBankAc == -1)
                    //{
                    //    BL_Registry.SetLogError("Master Account '" + dt["VanNumber"].ToString() + "' Not Exist with Transaction : " + DocNo);
                    //    outLoop = true;
                    //    break;
                    //}
                    int BranchId = GetMasterId(dt["Branch"].ToString(), 19);
                    if (BranchId == -1)
                    {
                        BL_Registry.SetLogError("Master Branch '" + dt["Branch"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int OutletId = GetMasterId(dt["VanNumber"].ToString(), 1);
                    if (OutletId == -1)
                    {
                        BL_Registry.SetLogError("Master Outlet '" + dt["VanNumber"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        outLoop = true;
                        break;
                    }
                    int EmployeeId = GetMasterId(dt["Salesman"].ToString(), 10);
                    if (EmployeeId == -1)
                    {
                        BL_Registry.SetLogError("Master Employee '" + dt["Salesman"].ToString() + "' Not Exist with Transaction : " + DocNo);
                        outLoop = true;
                        break;
                    }

                    #region Header
                    Hashtable header = new Hashtable
                         {
                             { "Date", date },
                             { "DocNo",dt["DocumentNumber"].ToString() },
                             { "CashBankAC",CashBankAc },
                             { "Currency", CurId},
                             { "ExchangeRate",dt["ExchangeRate"].ToString() },
                             { "Division", BranchId},
                             { "Warehouse", OutletId },
                             { "Employee", EmployeeId },
                         };
                    #endregion

                    List<Hashtable> body = new List<Hashtable>();

                    #region Body
                    foreach (var data in dr)
                    {
                        int ExpenseCatId = GetMasterId(data["ExpenseCategory"].ToString(), 18);
                        if (ExpenseCatId == -1)
                        {
                            BL_Registry.SetLogError("Master ExpenseCategory '" + dt["ExpenseCategory"].ToString() + "' Not Exist with Transaction : " + DocNo);
                            outLoop = true;
                            break;
                        }
                        int Account = GetMasterId(data["ExpenseCategory"].ToString(), 15);
                        if (Account == -1)
                        {
                            BL_Registry.SetLogError("Master Account '" + dt["ExpenseCategory"].ToString() + "' Not Exist with Transaction : " + DocNo);
                            outLoop = true;
                            break;
                        }

                        Hashtable row = new Hashtable();

                        row = new Hashtable
                         {
                             { "Expense Category", ExpenseCatId},
                             { "Account", Account},
                             { "Amount",data["Amount"]},
                             { "sRemarks",data["Remarks"]}
                         };
                        body.Add(row);
                    }

                    #endregion
                    string baseUrl = ConfigurationManager.AppSettings["Server_API_IP"];
                    string Url = baseUrl + "/Transactions/Vouchers/Expense Entry Van";

                    var postingData = new PostingData();
                    postingData.data.Add(new Hashtable { { "Header", header }, { "Body", body } });
                    string sContent = JsonConvert.SerializeObject(postingData);
                    if (Content == "true")
                    {
                        BL_Registry.SetContentLogError("Expense Entry Van sContent:--  " + sContent);
                    }
                    string err = "";

                    var response = Focus8API.Post(Url, sContent, sessionID, ref err);
                    #region Response
                    if (response != null)
                    {
                        var responseData = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                        if (responseData.result == -1)
                        {
                            Message = "Expense Entry Van Posting Failed" + "\n";
                            BL_Registry.SetLog("Expense Entry Van Posted Failed with DocNo: " + DocNo);
                            BL_Registry.SetLogError("Expense Entry Van Posted Failed  with DocNo: " + DocNo + "Response Error: " + responseData.message);
                            val = 0;
                        }
                        else
                        {
                            Message = "Expense Entry Van Posted Successfully" + "\n";
                            BL_Registry.SetLog("Expense Entry Van Posted Success with DocNo: " + DocNo);
                            string UpSql = $@"update Expense set PostStatus=1 where DocumentNumber='{DocNo}'";
                            int res = cls.Update(UpSql);
                            if (res == 1)
                            {
                                BL_Registry.SetLog("Updated PostStatus values to 1 - DocNo: " + DocNo);
                            }
                            val = 1;
                        }
                    }
                    #endregion
                }
                return val;
            }
            catch (Exception e)
            {
                BL_Registry.SetLog(e.ToString());
                return -1;
            }
        }
        //Getting Data from Database
        int iDefAcc, iDefAcc2 = 0, CashAcc = 0;
        public void GetDefAcc(int ID)
        {
            GetExternalData clsd = new GetExternalData();
            DataSet ds = clsd.GetDefAccounts(ID);
            if (ds.Tables[0] != null)
            {
                if (ds.Tables[0].Rows.Count != 0)
                {
                    iDefAcc = Convert.ToInt32(ds.Tables[0].Rows[0]["iDefAcc"]);
                    iDefAcc2 = Convert.ToInt32(ds.Tables[0].Rows[0]["iDefACc2"]);
                }
            }
        }

        public void GetCPID(int ID)
        {
            GetExternalData clsd = new GetExternalData();
            string str = $@"
                            select CashAccount from mCore_Salesman s
                            join muCore_Salesman mu on mu.iMasterId = s.iMasterId
                            where s.iMasterId = {ID}";
            DataSet ds = clsd.GetData(str);
            if (ds.Tables[0] != null)
            {
                if (ds.Tables[0].Rows.Count != 0)
                {
                    CashAcc = Convert.ToInt32(ds.Tables[0].Rows[0]["CashAccount"]);
                }
            }
        }

        public DataSet GetReturnDSTranID(string DocNo, int ID, string BatchNo, int Type,string BatchId)
        {
            GetExternalData clsd = new GetExternalData();
            #region Default Acc
            DataSet ds = new DataSet();

            int LinkPathId = 0;
            string sql = "";

            string Lsql = $@"select ilinkpathid from vmCore_Links_0 with (ReadUnCommitted)  where BaseVoucherId=3335 and LinkVoucherId=1796 group by ilinkpathid,Basevoucherid";
            DataSet lds = clsd.GetData(Lsql);
            for (int i = 0; i < lds.Tables[0].Rows.Count; i++)
            {
                LinkPathId = Convert.ToInt32(lds.Tables[0].Rows[i]["ilinkpathid"]);
            }

            sql = $@"select svoucherno,iLinkId, (fvalue-linkvalue)penvalue,iproduct,ibodyid from  (select h.svoucherno,iLinkId,fvalue,
                    i.iproduct,i.ibodyid, (select isnull(sum(fvalue),0) from tcore_links_0 tl1 where  tl1.bbase=0 
                    and tl1.ilinkid=tl.ilinkid and tl1.irefid=tl.itransactionid)linkvalue from tcore_header_0 h with (ReadUnCommitted) 
                    join tcore_data_0 d with (ReadUnCommitted) on d.iheaderid=h.iheaderid  join tcore_indta_0 i with (ReadUnCommitted) on i.ibodyid=d.ibodyid
                    join tcore_links_0 tl with (ReadUnCommitted) on tl.itransactionid=d.itransactionid  
                    join tcore_headerdata3335_0 uh with (ReadUnCommitted) on uh.iheaderid=d.iheaderid 
                    left join tCore_Batch_0 b on b.iBodyId=d.iBodyId
                    join tcore_data3335_0 ub with (ReadUnCommitted) on ub.ibodyid=d.ibodyid where tl.ilinkid={LinkPathId}
                    and tl.bbase=1 and h.bsuspended=0  and h.sVoucherNo='{DocNo}' and iProduct={ID} and (iBatchId={BatchId} or '{BatchId}'='{BatchId}')
                    and (sBatchNo='{BatchNo}' or '{BatchNo}'='{BatchNo}'))a where (fvalue-linkvalue)>0";
            BL_Registry.SetLog(sql);
            ds = clsd.GetData(sql);
            return ds;
            #endregion
        }
        public DataSet GetReturnDSTranID2(string DocNo, int ID, string BatchNo, int Type)
        {
            GetExternalData clsd = new GetExternalData();
            #region Default Acc
            DataSet ds = new DataSet();

            int LinkPathId = 0;
            string sql = "";

            string Lsql = $@"select ilinkpathid from vmCore_Links_0 with (ReadUnCommitted)  where BaseVoucherId=3335 and LinkVoucherId=1796 group by ilinkpathid,Basevoucherid";
            DataSet lds = clsd.GetData(Lsql);
            for (int i = 0; i < lds.Tables[0].Rows.Count; i++)
            {
                LinkPathId = Convert.ToInt32(lds.Tables[0].Rows[i]["ilinkpathid"]);
            }

            sql = $@"select svoucherno,iLinkId, (fvalue-linkvalue)penvalue,iproduct,ibodyid from  (select h.svoucherno,iLinkId,fvalue,
                    i.iproduct,i.ibodyid, (select isnull(sum(fvalue),0) from tcore_links_0 tl1 where  tl1.bbase=0 
                    and tl1.ilinkid=tl.ilinkid and tl1.irefid=tl.itransactionid)linkvalue from tcore_header_0 h with (ReadUnCommitted) 
                    join tcore_data_0 d with (ReadUnCommitted) on d.iheaderid=h.iheaderid  join tcore_indta_0 i with (ReadUnCommitted) on i.ibodyid=d.ibodyid
                    join tcore_links_0 tl with (ReadUnCommitted) on tl.itransactionid=d.itransactionid  
                    join tcore_headerdata3335_0 uh with (ReadUnCommitted) on uh.iheaderid=d.iheaderid 
                    left join tCore_Batch_0 b on b.iBodyId=d.iBodyId
                    join tcore_data3335_0 ub with (ReadUnCommitted) on ub.ibodyid=d.ibodyid where tl.ilinkid={LinkPathId}
                    and tl.bbase=1 and h.bsuspended=0  and h.sVoucherNo='{DocNo}' and iProduct={ID} 
                    and (sBatchNo='{BatchNo}' or '{BatchNo}'='{BatchNo}'))a where (fvalue-linkvalue)>0";
            BL_Registry.SetLog(sql);
            ds = clsd.GetData(sql);
            return ds;
            #endregion
        }

        int TransID = 0, TransQty = 0;
        public void GetTranID(string DocNo, int ID, int Type)
        {
            string str = "";
            GetExternalData clsd = new GetExternalData();
            if (Type == 1)
            {
                str = $@"
                                                       
                            select iProduct,abs(fQuantity) Qty,d.iTransactionId from tCore_Header_0 h
                            join tCore_Data_0 d on d.iHeaderId=h.iHeaderId
                            join tCore_Indta_0 i on i.iBodyId=d.iBodyId
                            join mCore_Product p on iProduct=p.iMasterId
                            where h.sVoucherNo='{DocNo}' and iProduct={ID}
                            ";
            }
            else
            {
                str = $@"                                                       
                            select iProduct,abs(fQuantity) Qty,d.iTransactionId from tCore_Header_0 h
                            join tCore_Data_0 d on d.iHeaderId=h.iHeaderId
                            join tCore_Indta_0 i on i.iBodyId=d.iBodyId
                            join mCore_Product p on iProduct=p.iMasterId
                            where h.sVoucherNo='{DocNo}'
                            ";
            }

            DataSet ds = clsd.GetData(str);
            if (ds.Tables[0] != null)
            {
                if (ds.Tables[0].Rows.Count != 0)
                {
                    TransID = Convert.ToInt32(ds.Tables[0].Rows[0]["iTransactionId"]);
                    TransQty = Convert.ToInt32(ds.Tables[0].Rows[0]["Qty"]);
                }
            }
        }

        public decimal GetBRate(string Batch)
        {
            decimal BRate = 0;
            GetExternalData clsd = new GetExternalData();
            DataSet ds = clsd.GetBatchRate(Batch);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                BRate = Convert.ToInt32(ds.Tables[0].Rows[0]["BatchRate"]);
            }

            return BRate;
        }

        public int GetMasterId(string Para, int id)
        {
            int MasterId = -1;
            try
            {
                GetExternalData clsd = new GetExternalData();
                DataSet ds = new DataSet();

                string str = "";

                if (id == 1)
                {
                    str = $"select iMasterId from mCore_Warehouse where sCode='{Para}' and iStatus<>5";
                    ds = clsd.GetData(str);
                }
                if (id == 2)
                {
                    str = $"select iMasterId from mCore_TaxCode where sCode='{Para}' and iStatus<>5";
                    ds = clsd.GetData(str);
                }
                if (id == 3)
                {
                    str = $"select iCurrencyId from mCore_Currency where sCode='{Para}'";
                    ds = clsd.GetData(str);
                }
                if (id == 4)
                {
                    str = $"select iMasterId from mCore_Product where sCode='{Para}' and iStatus<>5";
                    ds = clsd.GetData(str);
                }
                if (id == 5)
                {
                    str = $"select iMasterId from mCore_Units where sCode='{Para}' and iStatus<>5";
                    ds = clsd.GetData(str);
                }
                if (id == 6)
                {
                    str = $"select iMasterId from mCore_Account where sCode ='{Para}' and iStatus<>5";
                    ds = clsd.GetData(str);
                }
                if (id == 7)
                {
                    str = $"select PlaceOfSupply from muCore_Account_VAT_Settings where iMasterId='{Para}'";
                    ds = clsd.GetData(str);
                }
                if (id == 8)
                {
                    str = $"select iMasterId from mCore_CompanyMaster where sCode ='{Para}' and iStatus<>5";
                    ds = clsd.GetData(str);
                }
                if (id == 9)
                {
                    str = $"select ISNULL(iMasterId,0) iMasterId from mCore_Driver where sCode ='{Para}' and iStatus<>5";
                    ds = clsd.GetData(str);
                }
                if (id == 10)
                {
                    str = $"select iMasterId from mPay_Employee where sCode ='{Para}' and iStatus<>5";
                    ds = clsd.GetData(str);
                }
                if (id == 11)
                {
                    str = $"select ISNULL(iMasterId,0) iMasterId from mCore_Helper where sCode ='{Para}' and iStatus<>5";
                    ds = clsd.GetData(str);
                }
                if (id == 12)
                {
                    str = $"select iMasterId from mCore_Jurisdiction where sCode ='{Para}' and iStatus<>5";
                    ds = clsd.GetData(str);
                }
                if (id == 13)
                {
                    str = $"select iMasterId from mCore_PlaceOfSupply where sCode ='{Para}' and iStatus<>5";
                    ds = clsd.GetData(str);
                }
                if (id == 14)
                {
                    str = $"select CashAccountVAN from vmPay_Employee where sCode ='{Para}' and iStatus<>5";
                    ds = clsd.GetData(str);
                }
                if (id == 16) //PDC
                {
                    str = $"select PDCAccountVAN from vmPay_Employee where sCode ='{Para}' and iStatus<>5";
                    ds = clsd.GetData(str);
                }
                if (id == 15)
                {
                    str = $"select ExpenseAccount from vmCore_ExpenseCategory where sCode ='{Para}' and iStatus<>5";
                    ds = clsd.GetData(str);
                }
                if (id == 18)
                {
                    str = $"select iMasterId from mCore_ExpenseCategory where sCode ='{Para}' and iStatus<>5";
                    ds = clsd.GetData(str);
                }
                if (id == 19)
                {
                    str = $"select iMasterId from mCore_Division where sCode ='{Para}' and iStatus<>5";
                    ds = clsd.GetData(str);
                }
                if (id == 17)
                {
                    str = $"select iMasterId from mPos_Outlet where sCode ='{Para}' and iStatus<>5";
                    ds = clsd.GetData(str);
                }
                if (ds.Tables[0] != null)
                {
                    if (ds.Tables[0].Rows.Count != 0)
                    {
                        if (id == 3)
                        {
                            MasterId = Convert.ToInt32(ds.Tables[0].Rows[0]["iCurrencyId"]);
                        }
                        else if (id == 7)
                        {
                            MasterId = Convert.ToInt32(ds.Tables[0].Rows[0]["PlaceOfSupply"]);
                        }
                        else if (id == 14)
                        {
                            MasterId = Convert.ToInt32(ds.Tables[0].Rows[0]["CashAccountVAN"]);
                        }
                        else if (id == 15)
                        {
                            MasterId = Convert.ToInt32(ds.Tables[0].Rows[0]["ExpenseAccount"]);
                        }
                        else if (id == 16)
                        {
                            MasterId = Convert.ToInt32(ds.Tables[0].Rows[0]["PDCAccountVAN"]);
                        }
                        else
                        {
                            MasterId = Convert.ToInt32(ds.Tables[0].Rows[0]["iMasterId"]);
                        }

                    }
                }

                return MasterId;
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            return MasterId;
        }

        public decimal GetRefAmount(string Reference)
        {
            decimal RefAmt = 0;
            GetExternalData clsd = new GetExternalData();
            DataSet ds = clsd.GetRefAmt(Reference);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                RefAmt = Convert.ToInt32(ds.Tables[0].Rows[0]["RefAmt"]);
            }

            return RefAmt;
        }

        public string GetItemDes(int MId)
        {
            string sName = "";
            try
            {
                GetExternalData clsd = new GetExternalData();
                DataSet ds = new DataSet();

                string str = $"select sName from mCore_Product where iMasterId={MId}";
                ds = clsd.GetData(str);

                if (ds.Tables[0] != null)
                {
                    if (ds.Tables[0].Rows.Count != 0)
                    {
                        sName = (ds.Tables[0].Rows[0]["sName"]).ToString();
                    }
                }

                return sName;
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            return sName;
        }

        public string chkInvNo(string vNo)
        {
            string vNumber = "";
            try
            {
                GetExternalData clsd = new GetExternalData();
                DataSet ds = new DataSet();

                string str = $"select sVoucherNo from tCore_Header_0 where iVoucherType=3338 and sVoucherNo='{vNo}'";
                ds = clsd.GetData(str);

                if (ds.Tables[0] != null)
                {
                    if (ds.Tables[0].Rows.Count != 0)
                    {
                        vNumber = (ds.Tables[0].Rows[0]["sVoucherNo"]).ToString();
                    }
                }

                return vNumber;
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            return vNumber;
        }

        public string GetAccName(int MId)
        {
            string sName = "";
            try
            {
                GetExternalData clsd = new GetExternalData();
                DataSet ds = new DataSet();

                string str = $"select sName from mCore_Account where iMasterId={MId}";
                ds = clsd.GetData(str);

                if (ds.Tables[0] != null)
                {
                    if (ds.Tables[0].Rows.Count != 0)
                    {
                        sName = (ds.Tables[0].Rows[0]["sName"]).ToString();
                    }
                }

                return sName;
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            return sName;
        }

        public int GetAcctName(int Vtype)
        {
            int iMasterId = -1;
            try
            {
                GetExternalData clsd = new GetExternalData();
                DataSet ds = new DataSet();

                string str = $"select iDefACc2 from cCore_Vouchers_0 where iVoucherType={Vtype}";
                ds = clsd.GetData(str);

                if (ds.Tables[0] != null)
                {
                    if (ds.Tables[0].Rows.Count != 0)
                    {
                        iMasterId = Convert.ToInt32(ds.Tables[0].Rows[0]["iDefACc2"]);
                    }
                }

                return iMasterId;
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            return iMasterId;
        }

        public int GetStockAc(string Item)
        {
            int iMasterId = -1;
            try
            {
                GetExternalData clsd = new GetExternalData();
                DataSet ds = new DataSet();

                string str = $"select iCostofExcessStockAC from vmCore_Product where sCode='{Item}'";
                ds = clsd.GetData(str);

                if (ds.Tables[0] != null)
                {
                    if (ds.Tables[0].Rows.Count != 0)
                    {
                        iMasterId = Convert.ToInt32(ds.Tables[0].Rows[0]["iCostofExcessStockAC"]);
                    }
                }

                return iMasterId;
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            return iMasterId;
        }

        public int GetItemStockAc(string Item)
        {
            int iMasterId = -1;
            try
            {
                GetExternalData clsd = new GetExternalData();
                DataSet ds = new DataSet();

                string str = $"select iStocksAccount from vmCore_Product where sCode='{Item}'";
                ds = clsd.GetData(str);

                if (ds.Tables[0] != null)
                {
                    if (ds.Tables[0].Rows.Count != 0)
                    {
                        iMasterId = Convert.ToInt32(ds.Tables[0].Rows[0]["iStocksAccount"]);
                    }
                }

                return iMasterId;
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            return iMasterId;
        }

        public int GetSalesAc(string Item)
        {
            int iMasterId = -1;
            int ParentId = 0;
            try
            {
                GetExternalData clsd = new GetExternalData();
                DataSet ds = new DataSet();

                string str = $"select iSalesAccount,ISNULL(iParentId,0)iParentId from mCore_Product p join muCore_Product_OtherDetails pd on p.iMasterId = pd.iMasterId  and iStatus<>5 join mCore_ProductTreeDetails ptd on p.iMasterId = ptd.iMasterId where sCode='{Item}'";
                ds = clsd.GetData(str);

                if (ds.Tables[0] != null)
                {
                    if (ds.Tables[0].Rows.Count != 0)
                    {
                        iMasterId = Convert.ToInt32(ds.Tables[0].Rows[0]["iSalesAccount"]);
                        ParentId = Convert.ToInt32(ds.Tables[0].Rows[0]["iParentId"]);
                    }
                }
                if (iMasterId == 0)
                {
                    str = $"select iSalesAccount,ISNULL(iParentId,0)iParentId from mCore_Product p join muCore_Product_OtherDetails pd on p.iMasterId = pd.iMasterId  and iStatus<>5 join mCore_ProductTreeDetails ptd on p.iMasterId = ptd.iMasterId where p.iMasterId ={ParentId}";
                    ds = clsd.GetData(str);

                    if (ds.Tables[0] != null)
                    {
                        if (ds.Tables[0].Rows.Count != 0)
                        {
                            iMasterId = Convert.ToInt32(ds.Tables[0].Rows[0]["iSalesAccount"]);
                            ParentId = Convert.ToInt32(ds.Tables[0].Rows[0]["iParentId"]);
                        }
                    }

                    if (iMasterId == 0)
                    {
                        str = $"select iSalesAccount,ISNULL(iParentId,0)iParentId from mCore_Product p join muCore_Product_OtherDetails pd on p.iMasterId = pd.iMasterId  and iStatus<>5 join mCore_ProductTreeDetails ptd on p.iMasterId = ptd.iMasterId where p.iMasterId ={ParentId}";
                        ds = clsd.GetData(str);

                        if (ds.Tables[0] != null)
                        {
                            if (ds.Tables[0].Rows.Count != 0)
                            {
                                iMasterId = Convert.ToInt32(ds.Tables[0].Rows[0]["iSalesAccount"]);
                                ParentId = Convert.ToInt32(ds.Tables[0].Rows[0]["iParentId"]);
                            }
                        }

                        if (iMasterId == 0)
                        {
                            str = $"select iSalesAccount,ISNULL(iParentId,0)iParentId from mCore_Product p join muCore_Product_OtherDetails pd on p.iMasterId = pd.iMasterId  and iStatus<>5 join mCore_ProductTreeDetails ptd on p.iMasterId = ptd.iMasterId where p.iMasterId ={ParentId}";
                            ds = clsd.GetData(str);

                            if (ds.Tables[0] != null)
                            {
                                if (ds.Tables[0].Rows.Count != 0)
                                {
                                    iMasterId = Convert.ToInt32(ds.Tables[0].Rows[0]["iSalesAccount"]);
                                    ParentId = Convert.ToInt32(ds.Tables[0].Rows[0]["iParentId"]);
                                }
                            }

                            if (iMasterId == 0)
                            {
                                str = $"select iSalesAccount,ISNULL(iParentId,0)iParentId from mCore_Product p join muCore_Product_OtherDetails pd on p.iMasterId = pd.iMasterId  and iStatus<>5 join mCore_ProductTreeDetails ptd on p.iMasterId = ptd.iMasterId where p.iMasterId ={ParentId}";
                                ds = clsd.GetData(str);

                                if (ds.Tables[0] != null)
                                {
                                    if (ds.Tables[0].Rows.Count != 0)
                                    {
                                        iMasterId = Convert.ToInt32(ds.Tables[0].Rows[0]["iSalesAccount"]);
                                        ParentId = Convert.ToInt32(ds.Tables[0].Rows[0]["iParentId"]);
                                    }
                                }

                                if (iMasterId == 0)
                                {
                                    str = $"select iSalesAccount,ISNULL(iParentId,0)iParentId from mCore_Product p join muCore_Product_OtherDetails pd on p.iMasterId = pd.iMasterId  and iStatus<>5 join mCore_ProductTreeDetails ptd on p.iMasterId = ptd.iMasterId where p.iMasterId ={ParentId}";
                                    ds = clsd.GetData(str);

                                    if (ds.Tables[0] != null)
                                    {
                                        if (ds.Tables[0].Rows.Count != 0)
                                        {
                                            iMasterId = Convert.ToInt32(ds.Tables[0].Rows[0]["iSalesAccount"]);
                                            ParentId = Convert.ToInt32(ds.Tables[0].Rows[0]["iParentId"]);
                                        }
                                    }

                                    if (iMasterId == 0)
                                    {
                                        str = $"select iSalesAccount,ISNULL(iParentId,0)iParentId from mCore_Product p join muCore_Product_OtherDetails pd on p.iMasterId = pd.iMasterId  and iStatus<>5 join mCore_ProductTreeDetails ptd on p.iMasterId = ptd.iMasterId where p.iMasterId ={ParentId}";
                                        ds = clsd.GetData(str);

                                        if (ds.Tables[0] != null)
                                        {
                                            if (ds.Tables[0].Rows.Count != 0)
                                            {
                                                iMasterId = Convert.ToInt32(ds.Tables[0].Rows[0]["iSalesAccount"]);
                                                ParentId = Convert.ToInt32(ds.Tables[0].Rows[0]["iParentId"]);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return iMasterId;
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            return iMasterId;
        }
        int iref = 0;
        decimal OrigAmt = 0;
        decimal Quantity = 0;
        string Vnovt = "";
        public void GetIRef(string RefNo)
        {
            try
            {
                GetExternalData clsd = new GetExternalData();
                DataSet ds = new DataSet();

                string str = $@"EXEC Proc_LN_Vnd_Blk_Pnd '{RefNo}'";
                ds = clsd.GetData(str);

                if (ds.Tables[0] != null)
                {
                    if (ds.Tables[0].Rows.Count != 0)
                    {
                        iref = Convert.ToInt32(ds.Tables[0].Rows[0]["RefId"]);
                        Vnovt = ds.Tables[0].Rows[0]["VNo"].ToString();
                        OrigAmt = Convert.ToDecimal(ds.Tables[0].Rows[0]["fOrigNet"]);
                        Quantity = Convert.ToDecimal(ds.Tables[0].Rows[0]["Quantity"]);
                    }
                }

            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
        }

        public DataSet GetIRefDs(string RefNo)
        {
            GetExternalData clsd = new GetExternalData();
            #region Default Acc
            DataSet ds = new DataSet();

            string sql = $@"EXEC Proc_LN_Vnd_Blk_Pnd '{RefNo}'";

            ds = clsd.GetData(sql);
            return ds;
            #endregion
        }
    }


    public class HashData
    {
        public string url { get; set; }
        public List<Hashtable> data { get; set; }
        public int result { get; set; }
        public string message { get; set; }
    }
}
