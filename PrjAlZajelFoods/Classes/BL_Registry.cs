using Focus.Common.DataStructs;
using Focus.DatabaseFactory;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrjAlZajelFoods.Classes
{
    public class BL_Registry
    {
        string error = "";
        public static int GetQueryExe(string strSelQry, int CompId, ref string error)
        {
            try
            {
                //  DataSet ds;
                Database obj = DatabaseWrapper.GetDatabase(CompId);
                System.Data.Common.DbConnection dbcon = obj.CreateConnection();
                //dbcon.ConnectionString = obj.ConnectionString;
                System.Data.Common.DbCommand cmd = dbcon.CreateCommand();

                // return (obj.ExecuteDataSet(CommandType.Text, strSelQry));


                cmd.CommandTimeout = 0;
                cmd.CommandText = strSelQry;
                int Result = obj.ExecuteNonQuery(cmd);
                cmd.Dispose();
                dbcon.Dispose();
                return Result;
            }
            catch (Exception e)
            {
                error = e.Message;
                FConvert.LogFile("AlZajel.log", "err : " + "[" + System.DateTime.Now + "] - GetData :" + error + "---" + strSelQry);
                return 0;
            }
        }
        public static int GetDateToInt(DateTime dt)
        {
            int val;
            val = Convert.ToInt16(dt.Year) * 65536 + Convert.ToInt16(dt.Month) * 256 + Convert.ToInt16(dt.Day);
            return val;
        }

        public static Date GetIntToDate(int iDate)
        {
            try
            {
                return (new Date(iDate, CalendarType.Gregorean));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public int GetVouchertype(string InvName, int CompId)
        {
            string strsql = "Select iVouchertype,sName,sabbr from ccore_vouchers_0 where sabbr='" + InvName + "'";
            DataSet Dst2 = BL_Registry.GetData1(strsql, CompId, ref error);

            if (Dst2 != null && Dst2.Tables.Count > 0 && Dst2.Tables[0].Rows.Count > 0)
            {
                return Convert.ToInt32(Dst2.Tables[0].Rows[0]["iVouchertype"]);
            }
            else
            {
                return 0;
            }
        }
        public static int GetVoucherType(string Screen, int compid)
        {
            string query = default(string);
            string strerr = default(string);
            int strvtype = 0;
            query = "Select iVouchertype from cCore_Vouchers_0 where sAbbr='" + Screen + "'";
            DataSet dst = new DataSet();
            dst = GetData1(query, compid, ref strerr);
            if (dst != null)
            {
                if (dst.Tables.Count != 0)
                {
                    if (dst.Tables[0].Rows.Count != 0)
                    {
                        strvtype = Convert.ToInt32(dst.Tables[0].Rows[0]["iVouchertype"]);
                    }

                }
            }
            return strvtype;
        }

        public static DataSet GetData1(string strSelQry, int CompId, ref string error)
        {
            try
            {
                BL_Registry.SetLog(strSelQry);
                DataSet ds;
                Database obj = Focus.DatabaseFactory.DatabaseWrapper.GetDatabase(CompId);
                System.Data.Common.DbConnection dbcon = obj.CreateConnection();
                //dbcon.ConnectionString = obj.ConnectionString;
                System.Data.Common.DbCommand cmd = dbcon.CreateCommand();

                // return (obj.ExecuteDataSet(CommandType.Text, strSelQry));


                cmd.CommandTimeout = 0;
                cmd.CommandText = strSelQry;
                ds = obj.ExecuteDataSet(cmd);
                cmd.Dispose();
                dbcon.Dispose();
                return ds;
            }
            catch (Exception e)
            {
                error = e.Message;
                FConvert.LogFile("AlZajel.log", "err : " + "[" + System.DateTime.Now + "] - GetData :" + error + "---" + strSelQry);
                return null;
            }
        }
        public class Repository
        {
            public string GetConnectionString(int companyId, ref string logText)
            {
                var cs = "";
                try
                {
                    Database _db = DatabaseWrapper.GetDatabase(companyId);
                    return _db.ConnectionString;
                }
                catch (Exception e)
                {
                    logText += $"Error :: Origin : GetConnectionString \n Message : {e.Message} \n";
                    return null;
                }

            }
        }
        internal static DataSet GetData1(string retrievequery, int cid, ref object error)
        {
            throw new NotImplementedException();
        }


        public static void SetLog(string content)
        {
            StreamWriter objSw = null;
            try
            {
                string sFilePath = AppDomain.CurrentDomain.BaseDirectory + "AlZajel-" + DateTime.Now.ToString("dd-MM-yyyy") + ".txt";
                //string sFilePath = System.IO.Path.GetTempPath() + "CleancoLog" + DateTime.Now.Date.ToString("ddMMyyyy") + ".txt";
                objSw = new StreamWriter(sFilePath, true);
                objSw.WriteLine(DateTime.Now.ToString() + " " + content + Environment.NewLine);
            }
            catch (Exception ex)
            {
                //SetLog("Error -" + ex.Message);
            }
            finally
            {
                if (objSw != null)
                {
                    objSw.Flush();
                    objSw.Dispose();
                }
            }
        }

        public static void SetLog2(string content)
        {
            StreamWriter objSw = null;
            try
            {
                string sFilePath = AppDomain.CurrentDomain.BaseDirectory + "AlZajelLog-" + DateTime.Now.ToString("dd-MM-yyyy") + ".txt";
                //string sFilePath = System.IO.Path.GetTempPath() + "CleancoLog" + DateTime.Now.Date.ToString("ddMMyyyy") + ".txt";
                objSw = new StreamWriter(sFilePath, true);
                objSw.WriteLine(DateTime.Now.ToString() + " " + content + Environment.NewLine);
            }
            catch (Exception ex)
            {
                //SetLog("Error -" + ex.Message);
            }
            finally
            {
                if (objSw != null)
                {
                    objSw.Flush();
                    objSw.Dispose();
                }
            }
        }

        public static void SetLogError(string content)
        {
            StreamWriter objSw = null;
            try
            {
                string sFilePath = AppDomain.CurrentDomain.BaseDirectory + "AlZajelErrorLog-" + DateTime.Now.ToString("dd-MM-yyyy") + ".txt";
                //string sFilePath = System.IO.Path.GetTempPath() + "CleancoLog" + DateTime.Now.Date.ToString("ddMMyyyy") + ".txt";
                objSw = new StreamWriter(sFilePath, true);
                objSw.WriteLine(DateTime.Now.ToString() + " " + content + Environment.NewLine);
            }
            catch (Exception ex)
            {
                //SetLog("Error -" + ex.Message);
            }
            finally
            {
                if (objSw != null)
                {
                    objSw.Flush();
                    objSw.Dispose();
                }
            }
        }

        public static void SetContentLogError(string content)
        {
            StreamWriter objSw = null;
            try
            {
                string sFilePath = AppDomain.CurrentDomain.BaseDirectory + "AlZajelContentLog-" + DateTime.Now.ToString("dd-MM-yyyy") + ".txt";
                //string sFilePath = System.IO.Path.GetTempPath() + "CleancoLog" + DateTime.Now.Date.ToString("ddMMyyyy") + ".txt";
                objSw = new StreamWriter(sFilePath, true);
                objSw.WriteLine(DateTime.Now.ToString() + " " + content + Environment.NewLine);
            }
            catch (Exception ex)
            {
                //SetLog("Error -" + ex.Message);
            }
            finally
            {
                if (objSw != null)
                {
                    objSw.Flush();
                    objSw.Dispose();
                }
            }
        }
    }
}
