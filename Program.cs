using Job_UpdateGR.Item;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfApprove.API2.Controllers.Utils;

namespace Job_UpdateGR
{
    class Program
    {
        public static void Log(String iText)
        {
            string pathlog = ItemConfig.LogFile;
            String logFolderPath = System.IO.Path.Combine(pathlog, DateTime.Now.ToString("yyyyMMdd"));

            if (!System.IO.Directory.Exists(logFolderPath))
            {
                System.IO.Directory.CreateDirectory(logFolderPath);
            }
            String logFilePath = System.IO.Path.Combine(logFolderPath, DateTime.Now.ToString("yyyyMMdd") + ".txt");

            try
            {
                using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(logFilePath, true))
                {
                    System.Text.StringBuilder sbLog = new System.Text.StringBuilder();

                    String[] listText = iText.Split('|').ToArray();

                    foreach (String s in listText)
                    {
                        sbLog.AppendLine($"[{DateTime.Now:HH:mm:ss}] {s}");
                    }

                    outfile.WriteLine(sbLog.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing log file: {ex.Message}");
            }
        }
        public static void LogError(String iText)
        {

            string pathlog = ItemConfig.LogFile;
            String logFolderPath = System.IO.Path.Combine(pathlog, DateTime.Now.ToString("yyyyMMdd"));

            if (!System.IO.Directory.Exists(logFolderPath))
            {
                System.IO.Directory.CreateDirectory(logFolderPath);
            }
            String logFilePath = System.IO.Path.Combine(logFolderPath, DateTime.Now.ToString("yyyyMMdd") + "LogError.txt");

            try
            {
                using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(logFilePath, true))
                {
                    System.Text.StringBuilder sbLog = new System.Text.StringBuilder();

                    String[] listText = iText.Split('|').ToArray();

                    foreach (String s in listText)
                    {
                        sbLog.AppendLine($"[{DateTime.Now:HH:mm:ss}] {s}");
                    }

                    outfile.WriteLine(sbLog.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing log file: {ex.Message}");
            }
        }
        static void Main(string[] args)
        {
            try
            {
                Log("====== Start Process ====== : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                Log(string.Format("Run batch as :{0}", System.Security.Principal.WindowsIdentity.GetCurrent().Name));
                DataconDataContext db = new DataconDataContext(ItemConfig.dbConnectionString);
                if (db.Connection.State == ConnectionState.Open)
                {
                    db.Connection.Close();
                    db.Connection.Open();
                }
                db.Connection.Open();
                db.CommandTimeout = 0;

                var lstmemo = Getdata_Update(db);
                Log("lstmemo: " + lstmemo.Count());
                Console.WriteLine("lstmemo: " + lstmemo.Count());
                if (lstmemo != null)
                {
                    UpdateData(lstmemo, db);
                }
                Log("Successfully: " + lstmemo.Count());
                Console.WriteLine("Successfully: " + lstmemo.Count());
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("Exit ERROR");
                LogError("ERROR");
                LogError("message: " + ex.Message);
                LogError("Exit ERROR");
            }
            finally
            {
                Log("====== End Process Process ====== : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            }
        }
        public static List<TRNMemo> Getdata_Update(DataconDataContext db)
        {
            List<TRNMemo> memos = new List<TRNMemo>();
            if (ItemConfig.Docno.ToLower().Contains("all") || ItemConfig.Docno.ToLower().Contains("feb") || ItemConfig.Docno.ToLower().Contains("edit"))
            {
                memos = db.TRNMemos.Where(x => x.TemplateId == Convert.ToInt32(ItemConfig.TemplateId) && x.ModifiedDate >= DateTime.Now.AddDays(ItemConfig.IntervalTimeDay)).ToList();
            }
            else
            {
                var splitmemo = ItemConfig.Docno.Split('|').ToList();
                foreach (var item in splitmemo)
                {
                    var memo = db.TRNMemos.Where(x => x.DocumentNo == item.Trim()).FirstOrDefault();
                    if (memo != null)
                    {
                        memos.Add(memo);
                    }
                }
            }
            return memos;
        }
        public static void UpdateData(List<TRNMemo> lstmemo, DataconDataContext db)
        {
            foreach (var item in lstmemo)
            {
                try
                {
                    #region Getdata
                    List<object> Ordered_ProductList = new List<object>();
                    JObject jsonAdvanceForm = JsonUtils.createJsonObject(item.MAdvancveForm);
                    JArray itemsArray = (JArray)jsonAdvanceForm["items"];
                    foreach (JObject jItems in itemsArray)
                    {
                        JArray jLayoutArray = (JArray)jItems["layout"];
                        if (jLayoutArray.Count >= 1)
                        {
                            JObject jTemplateL = (JObject)jLayoutArray[0]["template"];
                            JObject jData = (JObject)jLayoutArray[0]["data"];
                            if ((String)jTemplateL["label"] == "รายการสินค้าที่สั่งซื้อ")
                            {
                                foreach (JArray row in jData["row"])
                                {
                                    // ตรวจสอบว่าตำแหน่งที่ 1 (index = 1) ของ row มีค่าหรือไม่
                                    if (row.Count > 1 && row[1]?["value"] != null && !string.IsNullOrEmpty(row[1]["value"].ToString()))
                                    {
                                        List<object> rowObject = new List<object>();
                                        foreach (JObject items in row)
                                        {
                                            rowObject.Add(items["value"].ToString());
                                        }
                                        Ordered_ProductList.Add(rowObject);
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                    #region Updatedata
                    if (Ordered_ProductList.Count > 0)
                    {
                        string Value = string.Empty;
                        decimal SumtotalPrice = 0;
                        decimal SumPriceBeforeTax = 0;
                        for (int i = 0; i < Ordered_ProductList.Count; i++)
                        {
                            dynamic uitem = Ordered_ProductList[i];
                            string category = uitem[0];
                            string productCode = uitem[1];
                            string productName = uitem[2];
                            decimal unitPrice = 0;
                            if (ItemConfig.Docno.ToLower().Contains("feb"))
                            {
                                unitPrice = decimal.TryParse(uitem[3]?.ToString(), out decimal result3) ? Math.Round(result3, 2, MidpointRounding.AwayFromZero) : 0;
                                unitPrice = GetdataMSTCatagolyItemStock(productCode, db, unitPrice, item);
                                if (unitPrice == 0)
                                {
                                    LogError("---- MSTCatagolyItemStock Not have (ราคาสินค้าต่อหน่วย) ----");
                                    LogError("ProductCode : " + productCode + "|BeforePrice : " + unitPrice + "|DocumentNo : " + item.DocumentNo + "|Memoid : " + item.MemoId);
                                    continue;
                                }
                            }
                            else if (ItemConfig.Docno.ToLower().Contains("edit"))
                            {
                                var RefMemoPO = db.TRNReferenceDocs.Where(x => x.MemoID == item.MemoId).FirstOrDefault();
                                if (RefMemoPO != null)
                                {
                                    unitPrice = decimal.TryParse(uitem[3]?.ToString(), out decimal result3) ? Math.Round(result3, 2, MidpointRounding.AwayFromZero) : 0;
                                    Log("---- GR Ref PO ----");
                                    Log("ProductCode : " + productCode + "|DocumentNo : " + item.DocumentNo + "|MemoGR : " + item.MemoId + "|MemoPO : " + RefMemoPO.MemoRefDocID + "|BeforePrice GR : " + unitPrice);
                                    var memof_productCode = db.TRNMemoForms.Where(a => a.MemoId == RefMemoPO.MemoRefDocID && a.col_label == "รหัสสินค้า" && a.col_value == productCode).FirstOrDefault();
                                    if (memof_productCode != null)
                                    {
                                        var memof_unitPrice = db.TRNMemoForms.Where(a => a.MemoId == RefMemoPO.MemoRefDocID && a.row_index == memof_productCode.row_index && a.col_label == "ราคาสินค้าต่อหน่วย").Select(x => x.col_value).FirstOrDefault();
                                        unitPrice = decimal.TryParse(memof_unitPrice?.ToString(), out decimal tempPrice) ? Math.Round(tempPrice, 2, MidpointRounding.AwayFromZero) : 0;
                                        Log("AfterPrice GR : " + unitPrice);
                                        Log("-------------------");
                                    }
                                }
                                else
                                {
                                    unitPrice = decimal.TryParse(uitem[3]?.ToString(), out decimal result3) ? Math.Round(result3, 2, MidpointRounding.AwayFromZero) : 0;
                                }
                            }
                            else
                            {
                                unitPrice = decimal.TryParse(uitem[3]?.ToString(), out decimal result3) ? Math.Round(result3, 2, MidpointRounding.AwayFromZero) : 0;
                            }
                            decimal vatPerUnit = decimal.TryParse(uitem[4]?.ToString(), out decimal result4) ? Math.Round(result4, 2, MidpointRounding.AwayFromZero) : 0;
                            decimal unitPriceBeforeTax = decimal.TryParse(uitem[5]?.ToString(), out decimal result5) ? Math.Round(result5, 2, MidpointRounding.AwayFromZero) : 0;
                            string quantityStr = uitem[6];
                            string unit = uitem[7];
                            decimal totalPrice = decimal.TryParse(uitem[8]?.ToString(), out decimal result8) ? Math.Round(result8, 2, MidpointRounding.AwayFromZero) : 0;
                            string cancelledQty = uitem[9];
                            string missingQty = uitem[10];
                            string damagedQty = uitem[11];
                            string incompleteQty = uitem[12];
                            string totalQty = uitem[13];
                            string receivedQty = uitem[14];
                            decimal receivedTotalPrice = decimal.TryParse(uitem[15]?.ToString(), out decimal result15) ? Math.Round(result15, 2, MidpointRounding.AwayFromZero) : 0;

                            //calculator
                            int quantity = int.TryParse(quantityStr, out int qty) ? qty : 0;
                            int xreceivedQty = int.TryParse(receivedQty, out int aaa) ? aaa : 0;
                            decimal calcUnitPriceBeforeTax = Math.Round(unitPrice / 1.07m, 2, MidpointRounding.AwayFromZero);
                            decimal calcVatPerUnit = Math.Round(unitPrice - calcUnitPriceBeforeTax, 2, MidpointRounding.AwayFromZero);
                            decimal totalReceived = Math.Round((calcUnitPriceBeforeTax * quantity) * 1.07m, 2, MidpointRounding.AwayFromZero);
                            decimal totalReceived2 = Math.Round((calcUnitPriceBeforeTax * xreceivedQty) * 1.07m, 2, MidpointRounding.AwayFromZero);

                            //ราคารวมสินค้า , รวมเป็นเงินทั้งสิ้น
                            SumtotalPrice += totalReceived2;

                            //ราคาสินค้า (ก่อนภาษี)
                            decimal xPriceBeforeTax = Math.Round(calcUnitPriceBeforeTax * xreceivedQty, 2, MidpointRounding.AwayFromZero);
                            SumPriceBeforeTax += xPriceBeforeTax;

                            if (i > 0) { Value += ","; }
                            Value += $"[{{\"value\": \"{category.Replace("\"", "\\\"")}\"}},{{\"value\": \"{productCode.Replace("\"", "\\\"")}\"}},{{\"value\": \"{productName.Replace("\"", "\\\"")}\"}},{{\"value\": \"{unitPrice.ToString("0.00", CultureInfo.InvariantCulture)}\"}}" +
                                $",{{\"value\": \"{calcVatPerUnit.ToString("0.00", CultureInfo.InvariantCulture)}\"}},{{\"value\": \"{calcUnitPriceBeforeTax.ToString("0.00", CultureInfo.InvariantCulture)}\"}},{{\"value\": \"{quantityStr.Replace("\"", "\\\"")}\"}},{{\"value\": \"{unit.Replace("\"", "\\\"")}\"}}" +
                                $",{{\"value\": \"{totalReceived.ToString("0.00", CultureInfo.InvariantCulture)}\"}},{{\"value\": \"{cancelledQty}\"}},{{\"value\": \"{missingQty.Replace("\"", "\\\"")}\"}},{{\"value\": \"{damagedQty.Replace("\"", "\\\"")}\"}}" +
                                $",{{\"value\": \"{incompleteQty.Replace("\"", "\\\"")}\"}},{{\"value\": \"{totalQty.Replace("\"", "\\\"")}\"}},{{\"value\": \"{receivedQty.Replace("\"", "\\\"")}\"}},{{\"value\": \"{totalReceived2.ToString("0.00", CultureInfo.InvariantCulture)}\"}}]";
                        }
                        //ภาษีมูลค่าเพิ่ม , ภาษีมูลค่าเพิ่ม 7%
                        decimal SumVat = Math.Round(SumtotalPrice - SumPriceBeforeTax, 2, MidpointRounding.AwayFromZero);

                        foreach (JObject jItems in itemsArray)
                        {
                            string loglabel = string.Empty;
                            string logValue = string.Empty;
                            try
                            {
                                JArray jLayoutArray = (JArray)jItems["layout"];
                                if (jLayoutArray.Count >= 1)
                                {
                                    JObject jTemplateL = (JObject)jLayoutArray[0]["template"];
                                    JObject UpdatejDataL = (JObject)jLayoutArray[0]["data"];
                                    if ((String)jTemplateL["label"] == "รายการสินค้าที่สั่งซื้อ")
                                    {
                                        loglabel = "รายการสินค้าที่สั่งซื้อ";
                                        Value = $"[{Value}]";
                                        logValue = Value;
                                        UpdatejDataL.Remove("row");
                                        UpdatejDataL.Add("row", JArray.Parse(Value));
                                    }
                                    if (jLayoutArray.Count > 1)
                                    {
                                        JObject jTemplateR = (JObject)jLayoutArray[1]["template"];
                                        JObject UpdatejData = (JObject)jLayoutArray[1]["data"];
                                        if ((String)jTemplateR["label"] == "ราคารวมสินค้า")
                                        {
                                            loglabel = "ราคารวมสินค้า";
                                            UpdatejData["value"] = SumtotalPrice.ToString("0.00", CultureInfo.InvariantCulture);
                                        }
                                        if ((String)jTemplateR["label"] == "ภาษีมูลค่าเพิ่ม")
                                        {
                                            loglabel = "ภาษีมูลค่าเพิ่ม";
                                            UpdatejData["value"] = SumVat.ToString("0.00", CultureInfo.InvariantCulture);

                                        }
                                        if ((String)jTemplateR["label"] == "ราคาสินค้า (ก่อนภาษี)")
                                        {
                                            loglabel = "ราคาสินค้า (ก่อนภาษี)";
                                            UpdatejData["value"] = SumPriceBeforeTax.ToString("0.00", CultureInfo.InvariantCulture);
                                        }
                                        if ((String)jTemplateR["label"] == "ภาษีมูลค่าเพิ่ม 7%")
                                        {
                                            loglabel = "ภาษีมูลค่าเพิ่ม 7%";
                                            UpdatejData["value"] = SumVat.ToString("0.00", CultureInfo.InvariantCulture);
                                        }
                                        if ((String)jTemplateR["label"] == "รวมเป็นเงินทั้งสิ้น")
                                        {
                                            loglabel = "รวมเป็นเงินทั้งสิ้น";
                                            UpdatejData["value"] = SumtotalPrice.ToString("0.00", CultureInfo.InvariantCulture);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                if (!string.IsNullOrEmpty(logValue))
                                {
                                    LogError("Error Json : " + ex.Message + "|Docno : " + item.DocumentNo + "|Label : " + loglabel + "|Value : " + logValue);
                                }
                                else
                                {
                                    LogError("Error Json : " + ex.Message + "|Docno : " + item.DocumentNo + "|Label : " + loglabel);
                                }
                                continue;
                            }
                        }
                        string MAdvancveform = JsonConvert.SerializeObject(jsonAdvanceForm);
                        if (!string.IsNullOrEmpty(MAdvancveform))
                        {
                            TRNMemo objMemo = db.TRNMemos.Where(x => x.MemoId == item.MemoId).FirstOrDefault();
                            objMemo.MAdvancveForm = MAdvancveform;
                            db.SubmitChanges();
                            Log("UpdateData ♥♥ Done ♥♥ : " + item.DocumentNo);
                            Console.WriteLine("UpdateData ♥♥ Done ♥♥ : " + item.DocumentNo);
                            Log("------------------------------------------------------");
                        }
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    LogError("Error UpdateData : " + ex.Message + "|DocNo : " + item.DocumentNo);
                    LogError("------------------------------------------------------------------");
                    continue;
                }
            }
        }
        public static decimal GetdataMSTCatagolyItemStock(string productCode, DataconDataContext db, decimal unitPrice, TRNMemo item)
        {
            var checkunitPrice = db.MSTCatagolyItemStocks.Where(x => x.ItemID == productCode).FirstOrDefault();
            if (checkunitPrice != null)
            {
                Log("---- MSTCatagolyItemStock have (ราคาสินค้าต่อหน่วย) ----");
                Log("ProductCode : " + productCode + "|BeforePrice : " + unitPrice);
                unitPrice = Math.Round(checkunitPrice.ItemPrice, 2, MidpointRounding.AwayFromZero);
                Log("AfterPrice : " + unitPrice + "|DocumentNo : " + item.DocumentNo + "|Memoid : " + item.MemoId);
                return unitPrice;
            }
            return 0;
        }
    }
}
