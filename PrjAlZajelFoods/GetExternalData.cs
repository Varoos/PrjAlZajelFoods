using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using PrjAlZajelFoods.Classes;

namespace PrjAlZajelFoods
{
    public class GetExternalData
    {
        static string ESerName = ConfigurationManager.AppSettings["ExternalServerName"];
        static string EDBName = ConfigurationManager.AppSettings["ExternalDBName"];
        static string EUID = ConfigurationManager.AppSettings["ExternalUserName"];
        static string EPWD = ConfigurationManager.AppSettings["ExternalPassword"];
        static string IntegratedSecurity = ConfigurationManager.AppSettings["Integrated"];

        static string connection = $"data source={ESerName};initial catalog={EDBName};User ID={EUID};Password={EPWD};integrated security={IntegratedSecurity}";
        SqlConnection conn = new SqlConnection(connection);

        public DataSet GetCommonData()
        {
            BL_Registry.SetLog(ESerName);
            BL_Registry.SetLog(EDBName);
            BL_Registry.SetLog(EUID);
            BL_Registry.SetLog(EPWD);
            BL_Registry.SetLog(connection);
            BL_Registry.SetLog("SqlConnection" + conn);
            DataSet dst = new DataSet();
            try
            {
                #region Common Extrenal Data
                string sql = "";
                sql = $@"select * from(
                        SELECT sh.DocumentNumber,InvoiceDate [TransactionDate],'Sales Invoices' Screen,Sequence,sh.InvoiceType [TransactionType],Branch,Warehouse SourceWarehouse,'' DestinationWarehouse,
                        si.Product,ISNULL(Note,'') Notes,ProductUnit,ActualQuantity+FOCQuantity [Quantity],Batch,ISNULL(MfgDate,0)MfgDate,ISNULL(ExpiryDate,0) ExpiryDate,
                        Customer,Currency,ExchangeRate,DueDate,Van [Outlet],sh.Salesman [Employee],ISNULL(Driver,0) Driver,ISNULL(Helper,0) Helper,ISNULL(LPONumber,'') LPONumber,
                        ISNULL(DriverMobileNumber,'') DriverMobileNumber,Jurisdiction,PlaceOfSupply, TaxCode,ActualQuantity,FOCQuantity,si.UnitPrice [SellingPrice],
                        si.Discount [DiscPerc],AddCharges,TaxValue [Vat],'' MaturityDate,'' ChequeNumber,'' VanNumber,'' Salesman,0 Amount,'' Reference,'' Remarks,0 ExpenseCategory
                        FROM Invoice sh join InvoiceLineItems si on sh.DocumentNumber=si.Invoice
                        where sh.InvoiceType in(10,11) and sh.PostStatus=0 --order by sh.Sequence 
                        union all
                        SELECT sh.DocumentNumber,InvoiceDate [TransactionDate],'Sales Return' Screen,Sequence,sh.InvoiceType [TransactionType],Branch,Warehouse SourceWarehouse,'' DestinationWarehouse,
                        si.Product,ISNULL(Note,'') Notes,ProductUnit,ActualQuantity+FOCQuantity [Quantity],Batch,ISNULL(MfgDate,0)MfgDate,ISNULL(ExpiryDate,0) ExpiryDate,
                        Customer,Currency,ExchangeRate,DueDate,Van [Outlet],sh.Salesman [Employee],ISNULL(Driver,0) Driver,ISNULL(Helper,0) Helper,ISNULL(LPONumber,'') LPONumber,
                        ISNULL(DriverMobileNumber,'') DriverMobileNumber,Jurisdiction,PlaceOfSupply, TaxCode,ActualQuantity,FOCQuantity,si.UnitPrice [SellingPrice],
                        si.Discount [DiscPerc],AddCharges,TaxValue [Vat],'' MaturityDate,'' ChequeNumber,'' VanNumber,'' Salesman,0 Amount,'' Reference,'' Remarks,0 ExpenseCategory
                        FROM Invoice sh join InvoiceLineItems si on sh.DocumentNumber=si.Invoice
                        where sh.InvoiceType in(12) and sh.PostStatus=0 --order by sh.Sequence 
                        union all 
                        select DocumentNumber,PaymentDate [TransactionDate],'Cash' Screen,(select max(Sequence)+1 from Invoice) Sequence,PaymentType [TransactionType],Branch,'' SourceWarehouse,'' DestinationWarehouse,
                        '' Product,'' Notes,'' ProductUnit,0 [Quantity],'' Batch,0 MfgDate,0 ExpiryDate,
                        Customer,Currency,ExchangeRate,0 DueDate,'' [Outlet],'' [Employee],'' Driver,'' Helper,'' LPONumber,
                        '' DriverMobileNumber,'' Jurisdiction,'' PlaceOfSupply,'' TaxCode,0 ActualQuantity,0 FOCQuantity,0 [SellingPrice],
                        0 [DiscPerc],0 AddCharges,0 [Vat],ISNULL(MaturityDate,'') MaturityDate,ISNULL(ChequeNumber,'') ChequeNumber,
                        ISNULL(VanNumber,'') VanNumber,Salesman,Amount,Reference,Remarks,0 ExpenseCategory from Receipt where PaymentType=21 and PostStatus=0
                        union all 
                        select DocumentNumber,PaymentDate [TransactionDate],'PDC' Screen,(select max(Sequence)+2 from Invoice)  Sequence,PaymentType [TransactionType],Branch,'' SourceWarehouse,'' DestinationWarehouse,
                        '' Product,'' Notes,'' ProductUnit,0 [Quantity],'' Batch,0 MfgDate,0 ExpiryDate,
                        Customer,Currency,ExchangeRate,0 DueDate,'' [Outlet],'' [Employee],'' Driver,'' Helper,'' LPONumber,
                        '' DriverMobileNumber,'' Jurisdiction,'' PlaceOfSupply,'' TaxCode,0 ActualQuantity,0 FOCQuantity,0 [SellingPrice],
                        0 [DiscPerc],0 AddCharges,0 [Vat],ISNULL(MaturityDate,'') MaturityDate,ISNULL(ChequeNumber,'') ChequeNumber,
                        ISNULL(VanNumber,'') VanNumber,Salesman,Amount,Reference,Remarks,0 ExpenseCategory from Receipt where PaymentType=22 and PostStatus=0
                        union all
                        select DocumentNumber,ExpenseDate [TransactionDate],'EEV' Screen,(select max(Sequence)+3 from Invoice)  Sequence,31 [TransactionType],ISNULL(Branch,'') Branch,'' SourceWarehouse,'' DestinationWarehouse,
                        '' Product,'' Notes,'' ProductUnit,0 [Quantity],'' Batch,0 MfgDate,0 ExpiryDate,
                        '' Customer,Currency,ExchangeRate,0 DueDate,'' [Outlet],'' [Employee],'' Driver,'' Helper,'' LPONumber,
                        '' DriverMobileNumber,'' Jurisdiction,'' PlaceOfSupply,'' TaxCode,0 ActualQuantity,0 FOCQuantity,0 [SellingPrice],
                        0 [DiscPerc],0 AddCharges,0 [Vat],'' MaturityDate,'' ChequeNumber,
                        ISNULL(VanNumber,'') VanNumber,Salesman,Amount,'' Reference,Remarks,ExpenseCategory from Expense where PostStatus=0
                        )c  order by Sequence,TransactionDate";

                try
                {
                    conn.Open();
                }
                catch (Exception ex)
                {
                    BL_Registry.SetLog(ex.ToString());
                }
                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                da.Fill(ds);
                dst = ds;
                conn.Close();
                return dst;

                #endregion
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            return dst;
        }

        public DataSet GetStockTransaction()
        {
            #region Stock Transactions
            string sql = "";
            sql = $@"select 'LoadRequest' Screen,DocumentNumber,TransactionDate,Branch,SourceWarehouse,DestinationWarehouse,Product,ISNULL(Notes,'') Notes,ProductUnit,Quantity,
                ISNULL(Batch,'') Batch,ISNULL(MfgDate,0)MfgDate,ISNULL(ExpiryDate,0) ExpiryDate 
                from StockTransaction where TransactionType=2 and PostStatus=0
                union all
                select 'Transfer' Screen,DocumentNumber,TransactionDate,Branch,SourceWarehouse,DestinationWarehouse,Product,ISNULL(Notes,'') Notes,ProductUnit,Quantity,
                ISNULL(Batch,'') Batch,ISNULL(MfgDate,0)MfgDate,ISNULL(ExpiryDate,0) ExpiryDate 
                from StockTransaction where TransactionType=6 and PostStatus=0
                union all
                select 'OffLoad' Screen,DocumentNumber,TransactionDate,Branch,SourceWarehouse,DestinationWarehouse,Product,ISNULL(Notes,'') Notes,ProductUnit,Quantity,
                ISNULL(Batch,'') Batch,ISNULL(MfgDate,0)MfgDate,ISNULL(ExpiryDate,0) ExpiryDate 
                from StockTransaction where TransactionType=3 and PostStatus=0 ";

            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            DataSet dst = ds;
            conn.Close();
            return dst;

            #endregion 
        }
        public DataSet GetExLoadRequest(int val)
        {
            //Getting External Load request and Offload and Transafer data
            #region LoadRequest and Offload and Transfer
            string sql = "";
            if (val == 2)
            {
                sql = $@"
                select DocumentNumber,TransactionDate,Branch,SourceWarehouse,DestinationWarehouse,Product,ISNULL(Notes,'') Notes,ProductUnit,Quantity,
                ISNULL(Batch,'') Batch,ISNULL(MfgDate,0)MfgDate,ISNULL(ExpiryDate,0) ExpiryDate 
                from StockTransaction where TransactionType=2 and PostStatus=0  -- Load Request";
            }

            if (val == 3)
            {
                sql = $@"
                select DocumentNumber,TransactionDate,Branch,SourceWarehouse,DestinationWarehouse,Product,ISNULL(Notes,'') Notes,ProductUnit,Quantity,
                ISNULL(Batch,'') Batch,ISNULL(MfgDate,0)MfgDate,ISNULL(ExpiryDate,0) ExpiryDate 
                from StockTransaction where TransactionType=3 and PostStatus=0  -- OffLoad ";
            }

            if (val == 6)
            {
                sql = $@"
                select DocumentNumber,TransactionDate,Branch,SourceWarehouse,DestinationWarehouse,Product,ISNULL(Notes,'') Notes,ProductUnit,Quantity,
                ISNULL(Batch,'') Batch,ISNULL(MfgDate,0)MfgDate,ISNULL(ExpiryDate,0) ExpiryDate 
                from StockTransaction where TransactionType=6 and PostStatus=0  -- Transfer ";
            }

            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            DataSet dst = ds;
            conn.Close();
            return dst;

            #endregion 
        }

        public DataSet GetExSales(int val)
        {
            //Getting External Sales Invoice  and return Data
            #region Sales Extrenal Data
            string sql = "";
            if (val == 10)
            {
                sql = $@"
                SELECT sh.DocumentNumber,InvoiceDate,Customer,Currency,ExchangeRate,Branch [Company],DueDate,Warehouse SourceWarehouse,Van [Outlet],sh.Salesman [Employee],ISNULL(Driver,0) Driver,
                ISNULL(Helper,0) Helper,ISNULL(LPONumber,'') LPONumber,
                ISNULL(DriverMobileNumber,'') DriverMobileNumber,Jurisdiction,PlaceOfSupply,
                si.Product,ISNULL(Note,'') Description,TaxCode,ProductUnit,ActualQuantity,FOCQuantity,ActualQuantity+FOCQuantity [Qty],si.UnitPrice [SellingPrice],
                si.Discount [DiscPerc],AddCharges,TaxValue [Vat],Batch,ISNULL(MfgDate,0)MfgDate,ISNULL(ExpiryDate,0) ExpiryDate,Sequence
                FROM Invoice sh join InvoiceLineItems si on sh.DocumentNumber=si.Invoice
                where sh.InvoiceType in(10,11) and sh.PostStatus=0 order by Sequence,InvoiceDate  --10,11-sales";
            }

            if (val == 12)
            {
                sql = $@"
                SELECT sh.DocumentNumber,InvoiceDate,Customer,Currency,ExchangeRate,Branch [Company],DueDate,Warehouse SourceWarehouse,Van [Outlet],sh.Salesman [Employee],ISNULL(Driver,0) Driver,
                ISNULL(Helper,0) Helper,ISNULL(LPONumber,'') LPONumber,
                ISNULL(DriverMobileNumber,'') DriverMobileNumber,Jurisdiction,PlaceOfSupply,
                si.Product,ISNULL(Note,'') Description,TaxCode,ProductUnit,ActualQuantity,FOCQuantity,ActualQuantity+FOCQuantity [Qty],si.UnitPrice [SellingPrice],
                si.Discount [DiscPerc],AddCharges,TaxValue [Vat],Batch,ISNULL(MfgDate,0)MfgDate,ISNULL(ExpiryDate,0) ExpiryDate,Sequence
                FROM Invoice sh join InvoiceLineItems si on sh.DocumentNumber=si.Invoice
                where sh.InvoiceType in(12) and sh.PostStatus=0 order by Sequence,InvoiceDate  -- 12 return ";
            }

            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            DataSet dst = ds;
            conn.Close();
            return dst;

            #endregion
        }

        public DataSet GetExSales2(string dno)
        {
            //Getting External Sales Invoice  and return Data
            #region Sales Extrenal Data
            string sql = "";
            sql = $@"
                SELECT sh.DocumentNumber,InvoiceDate,Customer,Currency,ExchangeRate,Branch [Company],DueDate,Warehouse SourceWarehouse,Van [Outlet],sh.Salesman [Employee],ISNULL(Driver,0) Driver,
                ISNULL(Helper,0) Helper,ISNULL(LPONumber,'') LPONumber,
                ISNULL(DriverMobileNumber,'') DriverMobileNumber,Jurisdiction,PlaceOfSupply,
                si.Product,ISNULL(Note,'') Description,TaxCode,ProductUnit,ActualQuantity,FOCQuantity,ActualQuantity+FOCQuantity [Qty],si.UnitPrice [SellingPrice],
                si.Discount [DiscPerc],AddCharges,TaxValue [Vat],Batch,ISNULL(MfgDate,0)MfgDate,ISNULL(ExpiryDate,0) ExpiryDate,Sequence
                FROM Invoice sh join InvoiceLineItems si on sh.DocumentNumber=si.Invoice
                where sh.InvoiceType in(10,11) and sh.DocumentNumber = '{dno}'
                --and sh.PostStatus=0 order by Sequence,InvoiceDate  --10,11-sales";

            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            DataSet dst = ds;
            conn.Close();
            return dst;

            #endregion
        }

        public DataSet GetExCash(int val)
        {
            //Getting External Cash Data
            #region Cash Extrenal Data
            string sql = "";
            if (val == 1)
            {
                //Cash
                sql = $@"
                    select DocumentNumber,(select max(Sequence)+1 from Invoice) Sequence,PaymentDate,Currency,ExchangeRate,ISNULL(MaturityDate,'') MaturityDate,ISNULL(ChequeNumber,'') ChequeNumber,
                    Branch,ISNULL(VanNumber,'') VanNumber,Salesman,Customer,Amount,Reference,Remarks from Receipt where PostStatus=0 and PaymentType=21 and Amount>0  order by Sequence,PaymentDate";
            }

            else if (val == 2)
            {
                //PDC
                //sql = $@"
                //select dbo.fn_dno1(Customer,ChequeNumber,MaturityDate)  DocumentNumber,null Sequence,min(PaymentDate) PaymentDate,Currency,ExchangeRate,ISNULL(MaturityDate,'') MaturityDate,ISNULL(ChequeNumber,'') ChequeNumber,Branch,ISNULL(VanNumber,'') VanNumber,Salesman,Customer,sum(Amount) Amount,
                //dbo.fn_Ref(Customer,ChequeNumber,MaturityDate)  Reference,Remarks
                //from Receipt 
                //where PostStatus=0 and PaymentType=22 and Amount>0  
                //group by Customer,ChequeNumber,MaturityDate,Salesman,Branch,VanNumber,Currency,ExchangeRate,Remarks
                //order by Sequence,PaymentDate ";
                sql = $@"
                    select DocumentNumber,(select max(Sequence)+1 from Invoice) Sequence,cast(PaymentDate as date) PaymentDate,Currency,ExchangeRate,ISNULL(MaturityDate,'') MaturityDate,ISNULL(ChequeNumber,'') ChequeNumber, Branch,ISNULL(VanNumber,'') VanNumber,Salesman,Customer,Amount,Reference,Remarks,rank() over (order by Customer,ChequeNumber,MaturityDate,Salesman  desc) rno from Receipt where PostStatus=0 and PaymentType=22 and Amount>0  order by Sequence,PaymentDate";
            }
            else if (val == 3)
            {
                //Payment
                sql = $@"
              
                    select DocumentNumber,(select max(Sequence)+1 from Invoice) Sequence,PaymentDate,Currency,ExchangeRate,ISNULL(MaturityDate,'') MaturityDate,ISNULL(ChequeNumber,'') ChequeNumber,
                    Branch,ISNULL(VanNumber,'') VanNumber,Salesman,Customer,abs(Amount) Amount,Reference,Remarks from Receipt where PostStatus=0 and Amount<0 order by Sequence,PaymentDate";
            }
            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            DataSet dst = ds;
            conn.Close();
            return dst;

            #endregion
        }

        

        public DataSet GetExEEV(int val)
        {
            //Getting External EEV Data
            #region EEV External Data
            string sql = "";
            if (val == 1)
            {
                //EEV
                sql = $@"
              
                    select DocumentNumber,(select max(Sequence)+3 from Invoice)  Sequence,ExpenseDate,Currency,ExchangeRate,ISNULL(Branch,'') Branch,
                    ISNULL(VanNumber,'') VanNumber,Salesman,ExpenseCategory,Amount,Remarks from Expense where PostStatus=0  order by Sequence, ExpenseDate";
            }

            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            DataSet dst = ds;
            conn.Close();
            return dst;

            #endregion
        }

        //Get Data from Focus Database Sql server 
        static string FSerName = ConfigurationManager.AppSettings["FocusServerName"];
        static string FDB = ConfigurationManager.AppSettings["FocusDBName"];
        static string FSQLUID = ConfigurationManager.AppSettings["FocusUserName"];
        static string FSQLPWD = ConfigurationManager.AppSettings["FocusPassword"];

        static string Fconnection = $"data source={FSerName};initial catalog={FDB};User ID={FSQLUID};Password={FSQLPWD};integrated security={IntegratedSecurity}";

        SqlConnection con = new SqlConnection(Fconnection);

        public DataSet GetDefAccounts(int ID)
        {
            //Getting Def Account
            #region Default Acc
            string sql = "";
            if (ID == 3338)
            {
                sql = $@"select iDefAcc,iDefACc2 from cCore_Vouchers_0  WHERE iVoucherType=3338  --5231 (stock trnsfer) 2954 (123)";
            }
            else if (ID == 4615)
            {
                sql = $@"select iDefAcc, iDefACc2 from cCore_Vouchers_0 WHERE iVoucherType = 4615--4615 cash";
            }
            else if (ID == 5891)
            {
                sql = $@"select iDefAcc, iDefACc2 from cCore_Vouchers_0 WHERE iVoucherType = 5891--5891 PDC";
            }
            else
            {
                sql = $@"select iDefAcc,iDefACc2 from cCore_Vouchers_0  WHERE iVoucherType=5379  --5231 (stock trnsfer) 2954 (123)";
            }
            try
            {
                con.Open();
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            SqlCommand cmd = new SqlCommand(sql, con);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            DataSet dst = ds;
            conn.Close();
            return dst;
            #endregion
        }

        public DataSet GetBatchRate(string Batch)
        {
            //Getting Batch Rate
            #region Batch Rate
            string sql = "";
            sql = $@"select isnull(sum(fRate)/count(frate),0) BatchRate from tCore_Batch_0 where sBatchNo='{Batch}' and fRate>0  --batchrate";

            try
            {
                con.Open();
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            SqlCommand cmd = new SqlCommand(sql, con);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            DataSet dst = ds;
            conn.Close();
            return dst;
            #endregion
        }
        public DataSet getFn(string sql)
        {
            try
            {
                con.Open();
            
            SqlCommand cmd = new SqlCommand(sql, con);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            DataSet dst = ds;
            con.Close();
            return dst;
            }
            catch (Exception ex)
            {
                con.Close();
                BL_Registry.SetLog(ex.ToString());
                return null;
            }
        }
        public DataSet GetRefAmt(string RefNo)
        {
            //Getting Batch Rate
            #region Batch Rate
            string sql = "";
            sql = $@"select abs(r.mAmount)RefAmt from tCore_Header_0 h join tCore_Data_0 d on h.iHeaderId=d.iHeaderId join tCore_Refrn_0 r on r.iBodyId=d.iBodyId where sVoucherNo='{RefNo}'";

            try
            {
                con.Open();
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            SqlCommand cmd = new SqlCommand(sql, con);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            DataSet dst = ds;
            conn.Close();
            return dst;
            #endregion
        }
        public int BatchCheck(string Batch)
        {
            int Bat = 0;
            //Getting Batch Check
            #region Batch Check
            string sql = "";
            // sql = $@"select distinct sBatchNo from tCore_Batch_0 where sBatchNo='{Batch}'";
            sql = $@"select top 1 sBatchNo,iBatchId from tCore_Batch_0 where sBatchNo='{Batch}' order by ibodyid desc";

            try
            {
                con.Open();
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            SqlCommand cmd = new SqlCommand(sql, con);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            DataSet dst = ds;
            con.Close();
            if (dst != null && dst.Tables.Count > 0 && dst.Tables[0].Rows.Count > 0)
            {
                Bat = Convert.ToInt32(dst.Tables[0].Rows[0]["iBatchId"]);
            }
            return Bat;

            #endregion
        }
        public int BatchCheck2(string Batch,string ExpDt,string TransDt)
        {
            int Bat = 0;
            //Getting Batch Check
            #region Batch Check
            string sql = "";
            // sql = $@"select distinct sBatchNo from tCore_Batch_0 where sBatchNo='{Batch}'";
            sql = $@"if('{ExpDt}' = '0')
            begin
            select top 1 sBatchNo,iBatchId from tCore_Batch_0 where sBatchNo='{Batch}' order by ibodyid desc
            end
            else if(cast(dbo.IntToDate('{ExpDt}') as date)<(cast(dbo.IntToDate('{TransDt}') as date)))
            begin
            select top 1 * from tCore_Batch_0 where sBatchNo='{Batch}' and iExpiryDate={ExpDt} order by iBodyId desc
            end
            else
            begin
            select top 1 sBatchNo,iBatchId from tCore_Batch_0 where sBatchNo='{Batch}' order by ibodyid desc
            end";

            try
            {
                con.Open();
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            SqlCommand cmd = new SqlCommand(sql, con);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            DataSet dst = ds;
            con.Close();
            if (dst != null && dst.Tables.Count > 0 && dst.Tables[0].Rows.Count > 0)
            {
                Bat = Convert.ToInt32(dst.Tables[0].Rows[0]["iBatchId"]);
            }
            return Bat;

            #endregion
        }
        public DataSet GetBatchData(string Batch, int ItemId, int WhId, int tDate, int ExpiryDt,string TransUnit)
        {
            //Getting Batch Data
            #region Batch Data
            string sql = "";
            //We have changed mcore_Warehouse to mPos_Outlet as per Murtaza confirmed
            sql = $@"exec pCore_GettingBatchList @ProdId={ItemId},@TransDt={tDate},@OutletId={WhId},@BatchNo='{Batch}',@ExpiryDT={ExpiryDt},@Unit='{TransUnit}'";
            //sql = $@"declare @salesdate int
            //            select @salesdate='{tDate}'
            //            declare @salesunit int
            //            select @salesunit=iMasterId from mCore_Units where sCode='{SUnit}' and iStatus=0
            //            select *,dbo.IntToDate(dbo.getbatchpurdate(sCode,sBatchNo,iBatchId))  bdate from
            //            (
            //            select i.iProduct[iheaderid],p.sName,p.sCode,iDefaultBaseUnit,iBatchId,sBatchNo,
            //            case when isnull((select top 1 fXFactor from mCore_UnitConversion where iProductId=i.iProduct and iBaseUnitId=p.iDefaultBaseUnit and iUnitId=@salesunit),0)=0 then
            //            sum(i.fQuantityInBase) 
            //            else
            //            sum(i.fQuantityInBase)/(select top 1 fXFactor from mCore_UnitConversion where iProductId=i.iProduct and iBaseUnitId=p.iDefaultBaseUnit and iUnitId=@salesunit) end 
            //            batchqty,iInvTag
            //            from tCore_Header_0 h
            //            inner join tCore_Data_0 d on h.iHeaderId=d.iHeaderId
            //            inner join tCore_Indta_0 i on d.iBodyId=i.iBodyId
            //            inner join tCore_Batch_0 b on b.iBodyId=d.iBodyId
            //            inner join vmCore_Product p on p.iMasterId=i.iProduct
            //            inner join mPos_Outlet wh on wh.iMasterId=d.iInvTag
            //            where p.iMasterId={ItemId} and wh.iMasterId={WhId} and
            //            h.bUpdateStocks=1 and h.bSuspended=0 and bUpdateStocks=1 and bSuspendUpdateStocks=0  and sBatchNo='{Batch}'  and 
            //           dbo.getbatchpurdate(p.sCode,sBatchNo,iBatchId) <=@salesdate

            //            group by b.sBatchNo,iBatchId,iProduct,p.sName,p.sCode,iDefaultBaseUnit,iInvTag,iFaTag)t
            //            group by iheaderid,sBatchNo,sname,scode,iDefaultBaseUnit,iBatchId,batchqty,iInvTag having batchqty > 0
            //            order by min(dbo.getbatchpurdate(sCode,sBatchNo,iBatchId))";

            try
            {
                con.Open();
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            SqlCommand cmd = new SqlCommand(sql, con);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            DataSet dst = ds;
            con.Close();
            return dst;
            #endregion
        }

        public DataSet GetData(string Query)
        {
            try
            {
                con.Open();
            }
            catch (Exception ex)
            {
                BL_Registry.SetLog(ex.ToString());
            }
            SqlCommand cmd = new SqlCommand(Query, con);
            //BL_Registry.SetLog(Fconnection);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            DataSet dst = ds;
            con.Close();
            return dst;
        }

        public int Update(string Vouc)
        {
            int result = 0;
            using (SqlConnection connect = new SqlConnection(connection))
            {
                string sql = $"{Vouc}";
                using (SqlCommand command = new SqlCommand(sql, connect))
                {
                    try
                    {
                        connect.Open();
                    }
                    catch (Exception ex)
                    {
                        BL_Registry.SetLog(ex.ToString());
                    }                   
                    result = command.ExecuteNonQuery();
                    connect.Close();
                }
            }
            return result;
        }
    }
}
