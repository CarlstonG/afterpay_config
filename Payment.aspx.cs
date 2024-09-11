using eWAY.Rapid;
using eWAY.Rapid.Enums;
using eWAY.Rapid.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OB.Booklist.Services.Interfaces;
using OB.Booklist.Services.Services;
using OB455_Booklist.Admin.Reports;
using OB455_Booklist.AppCode;
using OB455_Booklist.Biz;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Routing;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.Serialization;

//for testing afterpay
using System.Net.Http;
using System.Web.Script.Serialization;



namespace OB455_Booklist
{
    public partial class Payment : Page
    {
        protected async void AfterpayButton_Click(object sender, EventArgs e)
        {
            try
            {
                afterpayButton.Style.Add("display", "none");
                BtnLoader.Style.Add("display", "block");
                lblPMErrors.Style.Add("display", "none");

                // Call your existing Afterpay charge method
                string response = await CreateAfterpayCharge();
                dynamic jsonResponse = JsonConvert.DeserializeObject(response);

                if (jsonResponse != null && jsonResponse.redirectConfirmUrl != null)
                {
                    // Redirect to Afterpay confirmation page
                    Response.Redirect(jsonResponse.redirectConfirmUrl.ToString());
                }
                else
                {
                    // Handle error if no redirect URL received
                    lblPMErrors.Text = "Error creating Afterpay checkout.";
                    lblPMErrors.Style.Add("display", "block");
                    afterpayButton.Style.Add("display", "block");
                    BtnLoader.Style.Add("display", "none");
                }
            }
            catch (Exception ex)
            {
                lblPMErrors.Text = "An error occurred during Afterpay processing.";
                lblPMErrors.Style.Add("display", "block");
                afterpayButton.Style.Add("display", "block");
                BtnLoader.Style.Add("display", "none");
                // Optionally log the exception here
            }
        }


        //afterpay method test
        // Example method for creating Afterpay charge
        public async Task<string> CreateAfterpayCharge()
        {


            string username = "45776";
            string password = "4a9a9ec0e2a37646a94765e61d716fb3619849a977d6672bd1f9cdfb82f4d172515f9030a595dfac2bfe7fe96a3e480a42ff81dfcd1c2b582dccc64ca6bdb462";

            string url = "https://global-api-sandbox.afterpay.com/v2/checkouts";

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30);

                var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

               // var serializer = new JavaScriptSerializer();
                var data = new
                {
                    amount = new { amount = "100.00", currency = "AUD" },
                    consumer = new { email = "john.doe@example.com", givenNames = "John", surname = "Doe", phoneNumber = "1234567890" },
                    billing = new { name = "John Doe", line1 = "123 Main St", city = "Sample City", postcode = "12345", countryCode = "AU" },
                    shipping = new { name = "John Doe", line1 = "123 Main St", city = "Sample City", postcode = "12345", countryCode = "AU" },
                    merchant = new
                    {
                        redirectConfirmUrl = "http://localhost:64212/paperchase/Order/Payment/Success",
                        redirectCancelUrl = "http://localhost:64212/paperchase/Order/Payment/Cancel"

                    }
            };

                var jsonData = JsonConvert.SerializeObject(data);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // Send the request
                var response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode(); // Throw an exception if the status code is not success

                return await response.Content.ReadAsStringAsync();
            }
        }



        //afterpay method end test

        PaymentBiz pbiz = new PaymentBiz();
        CartBiz biz = new CartBiz();
        BooklistBiz bkiz = new BooklistBiz();
        OrderBiz obiz = new OrderBiz();
        private string _Subject = "";
        private StringBuilder _Body = new StringBuilder();
        private readonly IBLSchoolService blSchoolService = new BLSchoolService();
        string ShoppingCartID, fName, lName, URLKeyword;
        int hasDelAddr;
        string EWAYAPIKEY, EWAYAPIPASSWORD, EWAYENCRYPTIONKEY, ERPCustomerID = string.Empty;
        DataSet dealerPaymentDetails;

        protected void Page_Init(object sender, EventArgs e)
        {
            string AzureConn = ConfigurationManager.ConnectionStrings["AzureStorageAccount"].ConnectionString;
            string AzureResource = ConfigurationManager.AppSettings["AzureResource"];
            CloudStorageAccount sa = CloudStorageAccount.Parse(AzureConn);
            CloudBlobClient bd = sa.CreateCloudBlobClient();
            CloudBlobContainer container = bd.GetContainerReference(AzureResource);
            container.CreateIfNotExists(BlobContainerPublicAccessType.Blob);

            if (!IsPostBack)
            {
                loaderimg.Src = container.Uri.ToString() + "/images/loader1.gif";
                DataSet DealerEwayDetails = GetDealerEwayDetails(out ERPCustomerID);
                if (DealerEwayDetails != null && DealerEwayDetails.Tables.Count > 0)
                {
                    if (DealerEwayDetails.Tables[0].Rows.Count > 0)
                    {
                        EWAYENCRYPTIONKEY = (!string.IsNullOrEmpty(Convert.ToString(DealerEwayDetails.Tables[0].Rows[0]["EWAYENCRYPTIONKEY"]))) ? Convert.ToString(DealerEwayDetails.Tables[0].Rows[0]["EWAYENCRYPTIONKEY"]) : string.Empty;
                        // Show or Hide Payment Options based on Payment Settings
                        ShowHidePaymentMethods(DealerEwayDetails.Tables[0]);
                    }
                }
                this.Page.Form.Attributes.Add("data-eway-encrypt-key", EWAYENCRYPTIONKEY);
                this.Page.Form.Attributes.Add("method", "POST");
            }
            else
            {

            }
        }
        public void ShowHidePaymentMethods(DataTable DealerPaymentDetails)
        {
            if ((!string.IsNullOrEmpty(Convert.ToString(DealerPaymentDetails.Rows[0]["PAYPALENDPOINT"]))) && (!string.IsNullOrEmpty(Convert.ToString(DealerPaymentDetails.Rows[0]["PAYPALUSERNAME"]))) && (!string.IsNullOrEmpty(Convert.ToString(DealerPaymentDetails.Rows[0]["PAYPALPASSWORD"]))) && (!string.IsNullOrEmpty(Convert.ToString(DealerPaymentDetails.Rows[0]["PAYPALSIGNATURE"]))))
            {
                RB3.Checked = true;
                opt3.Style.Add("display", "table-cell");
                opt33.Style.Add("display", "table-cell");
                showhidePaypal.Style.Add("display", "block");
                showhideCreditCard.Style.Add("display", "none");
            }
            else
            {
                RB3.Checked = false;
                showhidePaypal.Style.Add("display", "none");
                opt3.Style.Add("display", "none");
                opt33.Style.Add("display", "none");
            }
            if ((!string.IsNullOrEmpty(Convert.ToString(DealerPaymentDetails.Rows[0]["EWAYAPIKEY"]))) && (!string.IsNullOrEmpty(Convert.ToString(DealerPaymentDetails.Rows[0]["EWAYAPIPASSWORD"]))) && (!string.IsNullOrEmpty(Convert.ToString(DealerPaymentDetails.Rows[0]["EWAYENCRYPTIONKEY"]))))
            {
                RB1.Checked = true;
                RB3.Checked = false;
                opt1.Style.Add("display", "table-cell");
                opt11.Style.Add("display", "table-cell");
                opt2.Style.Add("display", "table-cell");
                opt22.Style.Add("display", "table-cell");
                showhidePaypal.Style.Add("display", "none");
                showhideCreditCard.Style.Add("display", "block");
            }
            else
            {
                RB1.Checked = false;
                showhideCreditCard.Style.Add("display", "none");
                opt1.Style.Add("display", "none");
                opt11.Style.Add("display", "none");
                opt2.Style.Add("display", "none");
                opt22.Style.Add("display", "none");
            }
            if (((!string.IsNullOrEmpty(Convert.ToString(DealerPaymentDetails.Rows[0]["ZIPMERCHANTID"]))) && (!string.IsNullOrEmpty(Convert.ToString(DealerPaymentDetails.Rows[0]["ZIPAPI"])))))
            {
                opt4.Style.Add("display", "table-cell");
                opt44.Style.Add("display", "table-cell");
            }
            else
            {
                opt4.Style.Add("display", "none");
                opt44.Style.Add("display", "none");
            }
            if ((!string.IsNullOrEmpty(Convert.ToString(Session["ChargeException"]))))
            {
                opt4.Style.Add("display", "table-cell");
                opt44.Style.Add("display", "table-cell");
                RB1.Checked = false;
                RB2.Checked = false;
                RB3.Checked = false;
                RB4.Checked = true;
            }

        }
        public void GeneratePaypalTransaction(string Token)
        {
            //generate paypal transaction ID Done by Sandhya i.e 19/9/2017.
            try
            {
                string PayerID = string.Empty;
                NVPAPICaller payPalCaller = new NVPAPICaller();
                NVPCodec decoder = new NVPCodec();
                string retMsg = string.Empty;
                string Username = string.Empty;
                string Pswd = string.Empty;
                string Signature = string.Empty;
                string EndPointUrl = string.Empty;
                string paypalTransactionID = "";
                DataSet DealerPaypalDetails = GetDealerEwayDetails(out ERPCustomerID);
                if (DealerPaypalDetails != null && DealerPaypalDetails.Tables.Count > 0)
                {
                    if (DealerPaypalDetails.Tables[0].Rows.Count > 0)
                    {
                        Username = (!string.IsNullOrEmpty(Convert.ToString(DealerPaypalDetails.Tables[0].Rows[0]["PAYPALUSERNAME"]))) ? Convert.ToString(DealerPaypalDetails.Tables[0].Rows[0]["PAYPALUSERNAME"]) : string.Empty;
                        Pswd = (!string.IsNullOrEmpty(Convert.ToString(DealerPaypalDetails.Tables[0].Rows[0]["PAYPALPASSWORD"]))) ? Convert.ToString(DealerPaypalDetails.Tables[0].Rows[0]["PAYPALPASSWORD"]) : string.Empty;
                        Signature = (!string.IsNullOrEmpty(Convert.ToString(DealerPaypalDetails.Tables[0].Rows[0]["PAYPALSIGNATURE"]))) ? Convert.ToString(DealerPaypalDetails.Tables[0].Rows[0]["PAYPALSIGNATURE"]) : string.Empty;
                        EndPointUrl = (!string.IsNullOrEmpty(Convert.ToString(DealerPaypalDetails.Tables[0].Rows[0]["PAYPALENDPOINT"]))) ? Convert.ToString(DealerPaypalDetails.Tables[0].Rows[0]["PAYPALENDPOINT"]) : string.Empty;
                        if (!string.IsNullOrEmpty(Pswd))
                        {
                            Pswd = FileHelper.Decrypt(Pswd);
                        }
                    }
                }
                bool ret = payPalCaller.GetCheckoutDetails(Token, ref PayerID, ref decoder, ref retMsg, Username, Pswd, Signature, EndPointUrl);
                if (ret)
                {
                    Session["payerId"] = PayerID;
                    string OrderiD = string.Empty;
                    OrderiD = InsertNextTransactionIdIntoBillingTable("PayPal", "P");

                    bool retFinal = payPalCaller.DoCheckoutPayment(decoder["AMT"].ToString(), Token, PayerID, ref decoder, ref retMsg, Username, Pswd, Signature, EndPointUrl, OrderiD);
                    if (retFinal)
                    {

                        // Retrieve PayPal Transation ID value.
                        paypalTransactionID = decoder["PAYMENTINFO_0_TRANSACTIONID"].ToString();
                        string GrandTotalAmount = string.Empty;
                        if (!string.IsNullOrEmpty(paypalTransactionID))
                        {
                            if (Session["GrandTotalsession"] != null)
                            {
                                if (!string.IsNullOrEmpty(Session["GrandTotalsession"].ToString()))
                                {
                                    GrandTotalAmount = Session["GrandTotalsession"].ToString();
                                }
                            }
                            SaveOrder(OrderiD);
                            ProcessOrderCofirmation(OrderiD, paypalTransactionID, '$' + GrandTotalAmount);
                            Session["token"] = "";
                        }
                        if (string.IsNullOrEmpty(paypalTransactionID))
                        {
                            lblPMErrors.Text = "There was a technical problem for processing order, please try after sometime Or contact our friendly staff for assistance.";
                            lblPMErrors.Style.Add("display", "block");
                        }
                    }
                    else
                    {
                        lblPMErrors.Text = retMsg;
                        lblPMErrors.Style.Add("display", "block");
                    }
                }
                else
                {
                    lblPMErrors.Text = retMsg;
                    lblPMErrors.Style.Add("display", "block");
                }
            }
            catch (Exception ex)
            {
                btnSubmitOrder.Style.Add("display", "block");
                BtnLoader.Style.Add("display", "none");

                bakCheckout.Style.Add("display", "block");
                canOrderanch.Style.Add("display", "block");

                throw ex;
            }
            //Ends generate paypal transaction ID Done by Sandhya i.e 19/9/2017
        }
        protected void DoneZipTransaction(string state, string OrderID, string ReceiptNumber, string zipProduct)
        {
            try
            {
                if (state == "captured")
                {
                    SaveOrder(OrderID);
                    ProcessZipOrderCofirmation(OrderID, ReceiptNumber, zipProduct);
                    Session["ZipState"] = null;
                    Session["receipt_number"] = null;
                    Session["ZipProduct"] = null;
                    Session["ChargeException"] = null;
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                btnSubmitOrder.Style.Add("display", "block");
                BtnLoader.Style.Add("display", "none");
                bakCheckout.Style.Add("display", "block");
                canOrderanch.Style.Add("display", "block");
                throw ex;
            }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                lblPMErrors.Style.Add("display", "none");

                if (RB3.Checked == true)
                {
                    opt3.Style.Add("display", "table-cell");
                    opt33.Style.Add("display", "table-cell");
                    showhidePaypal.Style.Add("display", "block");
                    showhideCreditCard.Style.Add("display", "none");
                    showhideZip.Style.Add("display", "none");
                }
                else if (RB2.Checked == true || RB1.Checked == true)
                {
                    opt1.Style.Add("display", "table-cell");
                    opt11.Style.Add("display", "table-cell");
                    opt2.Style.Add("display", "table-cell");
                    opt22.Style.Add("display", "table-cell");
                    showhidePaypal.Style.Add("display", "none");
                    showhideCreditCard.Style.Add("display", "block");
                    showhideZip.Style.Add("display", "none");
                }

                else if (RB4.Checked == true)
                {
                    opt4.Style.Add("display", "table-cell");
                    opt44.Style.Add("display", "table-cell");
                    showhidePaypal.Style.Add("display", "none");
                    showhideCreditCard.Style.Add("display", "none");
                    showhideZip.Style.Add("display", "block");
                }
                else
                {
                    opt1.Style.Add("display", "none");
                    opt11.Style.Add("display", "none");
                    opt2.Style.Add("display", "none");
                    opt22.Style.Add("display", "none");
                    opt3.Style.Add("display", "none");
                    opt33.Style.Add("display", "none");
                    opt4.Style.Add("display", "none");
                    opt44.Style.Add("display", "none");
                    showhidePaypal.Style.Add("display", "none");
                    showhideCreditCard.Style.Add("display", "none");
                    showhideZip.Style.Add("display", "none");
                }

                string shosti = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
                credImg.ImageUrl = shosti + "/Images/cvv2.jpg";
                System.Web.Routing.Route myRoute = RouteData.Route as System.Web.Routing.Route;
                if (myRoute != null)
                {
                    // Bind Url values
                    URLKeyword = Page.RouteData.Values["store"] == null ? "NULL" : Page.RouteData.Values["store"].ToString();

                    if (URLKeyword != "NULL")
                    {
                        //set route for edit button
                        RouteValueDictionary parameters =
                                    new RouteValueDictionary
                                                   {
                                            {"store", URLKeyword},
                                                   };
                        VirtualPathData vpd = RouteTable.Routes.GetVirtualPath(null, "rtStoreCheckout", parameters);
                        edtDetails.HRef = vpd.VirtualPath;
                        edtDeliveryDetails.HRef = vpd.VirtualPath;
                        // set route for Back button
                        RouteValueDictionary chkparameters =
                                   new RouteValueDictionary
                                                  {
                                            {"store", URLKeyword},
                                                  };
                        VirtualPathData vpdchk = RouteTable.Routes.GetVirtualPath(null, "rtStoreCheckout", parameters);
                        bakCheckout.HRef = vpdchk.VirtualPath;
                    }
                    else
                    {
                        Response.RedirectToRoute("rtError");
                    }
                    if (Session["ShoppingCartID"] != null)
                    {
                        if (!string.IsNullOrEmpty(Session["ShoppingCartID"].ToString()))
                        {
                            ShoppingCartID = Session["ShoppingCartID"].ToString();
                            int RetVal;
                            RetVal = biz.GetShoppingCount(ShoppingCartID);
                            if (RetVal == 0)
                            {
                                URLKeyword = Page.RouteData.Values["store"] == null ? "NULL" : Page.RouteData.Values["store"].ToString();

                                if (URLKeyword != "NULL")
                                {
                                    RouteValueDictionary parameters =
                                                   new RouteValueDictionary
                                                          {
                                                       {"store",URLKeyword  }
                                                          };
                                    Response.RedirectToRoute("rtStore", parameters);
                                }

                            }
                        }
                    }
                    else
                    {
                        URLKeyword = Page.RouteData.Values["store"] == null ? "NULL" : Page.RouteData.Values["store"].ToString();
                        if (URLKeyword != "NULL")
                        {
                            RouteValueDictionary parameters =
                                           new RouteValueDictionary
                                                  {
                                                       {"store",URLKeyword  }
                                                  };

                            Response.RedirectToRoute("rtStore", parameters);
                        }

                    }
                    // hide menus
                    HtmlContainerControl RegisterDiv;
                    RegisterDiv = (HtmlContainerControl)Master.FindControl("Register");
                    if (RegisterDiv != null)
                        RegisterDiv.Visible = false;
                    HtmlContainerControl LoginDiv;
                    LoginDiv = (HtmlContainerControl)Master.FindControl("Login");
                    if (LoginDiv != null)
                        LoginDiv.Visible = false;

                    if (!IsPostBack)
                    {
                        LoadCCYearData();
                        if (Session["ShoppingCartID"] != null)
                        {
                            if (!string.IsNullOrEmpty(Session["ShoppingCartID"].ToString()))
                            {
                                if (ddlState.Items.Count > 0)
                                {
                                    ddlState.Items.Insert(0, "State");
                                    ddlState.SelectedIndex = 0;
                                }
                                ShoppingCartID = Session["ShoppingCartID"].ToString();


                                if (Session["ShoppingCartID"] != null) //login user
                                {
                                    if (!string.IsNullOrEmpty(Session["ShoppingCartID"].ToString()))
                                    {
                                        if (!Session["ShoppingCartID"].ToString().Contains("Guest_")) // Registered user
                                        {
                                            //populate from UserRegistrations and delivery details table
                                            if (Session["UId"] != null)
                                            {
                                                if (!string.IsNullOrEmpty(Session["UId"].ToString()))
                                                {
                                                    UserBiz ubiz = new UserBiz();
                                                    DataSet dsCustBLDetails = ubiz.Get_UserInfo(Convert.ToInt32(Session["UId"]));
                                                    int hasDelAddr = -1;
                                                    if (dsCustBLDetails != null && dsCustBLDetails.Tables.Count > 0)
                                                    {
                                                        //Added CHanges populating the Address details from checkout page. i.e Sandhya 06/10/2017

                                                        BillingAddress.Text = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["Address"])) ? Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["Address"]) : string.Empty);
                                                        BillingPostcode.Text = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["PostCode"])) ? Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["PostCode"]) : string.Empty);
                                                        BillingSuburb.Text = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["Suburb"])) ? Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["Suburb"]) : string.Empty);
                                                        ddlState.SelectedValue = Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["StateId"]);

                                                        //Ends Added CHanges populating the Address details from checkout page. i.e Sandhya 06/10/2017

                                                        fName = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["FName"]))) ? Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["FName"]) : string.Empty;
                                                        lName = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["LName"]))) ? Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["LName"]) : string.Empty;
                                                        lblName.Text = fName + " " + lName + ", <br/> ";
                                                        lblMobile.Text = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["Mobile"]))) ? Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["Mobile"]) + "<br/>" : string.Empty;
                                                        lblEmail.Text = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["Email"]))) ? Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["Email"]) : string.Empty;
                                                        lblAddress.Text = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["Address"])) ? Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["Address"]) : string.Empty);
                                                        lblPostCode.Text = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["PostCode"])) ? Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["PostCode"]) : string.Empty);
                                                        lblSuburb.Text = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["Suburb"])) ? Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["Suburb"]) : string.Empty);
                                                        hdDelMode.Value = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["DelMode"])) ? Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["DelMode"]) : string.Empty);
                                                        hasDelAddr = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["HasDeliveryAddress"])) ? Convert.ToInt32(dsCustBLDetails.Tables[0].Rows[0]["HasDeliveryAddress"]) : -1);
                                                        hdModeName.Value = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["DisplayName"])) ? Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["DisplayName"]) : string.Empty);

                                                        int stateId = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[0].Rows[0]["StateId"])) ? Convert.ToInt32(dsCustBLDetails.Tables[0].Rows[0]["StateId"]) : 0);
                                                        if (stateId == 0)
                                                            lblState.Text = "";
                                                        else
                                                        {
                                                            DataSet dsState = ubiz.get_State_ById(stateId);
                                                            if (dsState != null && dsState.Tables.Count > 0)
                                                            {
                                                                lblState.Text = (!string.IsNullOrEmpty(Convert.ToString(dsState.Tables[0].Rows[0]["State"])) ? Convert.ToString(dsState.Tables[0].Rows[0]["State"]) : string.Empty);
                                                            }
                                                        }
                                                        if (hasDelAddr == 1) //del addr same as address
                                                        {
                                                            lblDelAddr.Text = lblAddress.Text;
                                                            lblDelPostCode.Text = lblPostCode.Text + ", ";
                                                            lblDelSuburb.Text = lblSuburb.Text + ", ";
                                                            lblDelState.Text = lblState.Text;
                                                            if (Convert.ToInt32(hdDelMode.Value) == 1)
                                                            {
                                                                lblDelAddr.Text += ",<br/> ";
                                                            }
                                                            else
                                                            {
                                                                lblDelAddr.Text += ", ";
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (Convert.ToInt32(hdDelMode.Value) == 1)
                                                            {
                                                                lblDelAddr.Text = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[1].Rows[0]["Address"])) ? Convert.ToString(dsCustBLDetails.Tables[1].Rows[0]["Address"]) + ",<br/> " : string.Empty);
                                                            }
                                                            else
                                                            {
                                                                lblDelAddr.Text = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[1].Rows[0]["Address"])) ? Convert.ToString(dsCustBLDetails.Tables[1].Rows[0]["Address"]) + ", " : string.Empty);
                                                            }
                                                            lblDelPostCode.Text = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[1].Rows[0]["PostCode"])) ? Convert.ToString(dsCustBLDetails.Tables[1].Rows[0]["PostCode"]) + ", " : string.Empty);
                                                            lblDelSuburb.Text = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[1].Rows[0]["Suburb"])) ? Convert.ToString(dsCustBLDetails.Tables[1].Rows[0]["Suburb"]) + ", " : string.Empty);
                                                            lblDelState.Text = (!string.IsNullOrEmpty(Convert.ToString(dsCustBLDetails.Tables[1].Rows[0]["State"])) ? Convert.ToString(dsCustBLDetails.Tables[1].Rows[0]["State"]) : string.Empty);
                                                        }

                                                    }
                                                    if (!string.IsNullOrEmpty(lblAddress.Text) || !string.IsNullOrEmpty(lblPostCode.Text) || !string.IsNullOrEmpty(lblSuburb.Text) || !string.IsNullOrEmpty(lblState.Text))
                                                    {
                                                        lblEmail.Text = lblEmail.Text + "<br/>";
                                                    }
                                                    else
                                                        lblEmail.Text = lblEmail.Text + "<br/>";
                                                    if (!string.IsNullOrEmpty(lblSuburb.Text))
                                                    {
                                                        if (!string.IsNullOrEmpty(lblAddress.Text))
                                                            lblAddress.Text = lblAddress.Text + ", ";
                                                    }
                                                    if (!string.IsNullOrEmpty(lblPostCode.Text))
                                                    {
                                                        if (!string.IsNullOrEmpty(lblSuburb.Text))
                                                            lblSuburb.Text = lblSuburb.Text + ", ";
                                                        else
                                                            lblAddress.Text = lblAddress.Text + ", ";
                                                    }
                                                    else
                                                    {
                                                        if (!string.IsNullOrEmpty(lblState.Text))
                                                            lblSuburb.Text = lblSuburb.Text + ", ";
                                                    }
                                                    if (!string.IsNullOrEmpty(lblState.Text))
                                                    {
                                                        if (!string.IsNullOrEmpty(lblPostCode.Text))
                                                            lblPostCode.Text = lblPostCode.Text + ",";
                                                    }
                                                    else
                                                    {

                                                    }
                                                }
                                            }
                                        }
                                        else if (Session["ShoppingCartID"].ToString().Contains("Guest_"))  // Guest user
                                        {
                                            ShoppingCartID = Session["ShoppingCartID"].ToString();
                                            DataSet dsGuest = pbiz.Populate_ConsumerDetails(ShoppingCartID);
                                            UserBiz ubiz = new UserBiz();
                                            if (dsGuest != null && dsGuest.Tables.Count > 0)
                                            {
                                                if (dsGuest.Tables[0].Rows.Count > 0)
                                                {
                                                    //Added CHanges populating the Address details from checkout page. i.e Sandhya 06/10/2017

                                                    BillingAddress.Text = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[0].Rows[0]["Address"])) ? Convert.ToString(dsGuest.Tables[0].Rows[0]["Address"]) : string.Empty);
                                                    BillingPostcode.Text = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[0].Rows[0]["PostCode"])) ? Convert.ToString(dsGuest.Tables[0].Rows[0]["PostCode"]) : string.Empty);
                                                    BillingSuburb.Text = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[0].Rows[0]["Suburb"])) ? Convert.ToString(dsGuest.Tables[0].Rows[0]["Suburb"]) : string.Empty);
                                                    ddlState.SelectedValue = Convert.ToString(dsGuest.Tables[0].Rows[0]["StateId"]);
                                                    fName = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[0].Rows[0]["FName"]))) ? Convert.ToString(dsGuest.Tables[0].Rows[0]["FName"]) : string.Empty;
                                                    lName = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[0].Rows[0]["LName"]))) ? Convert.ToString(dsGuest.Tables[0].Rows[0]["LName"]) : string.Empty;
                                                    lblMobile.Text = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[0].Rows[0]["Mobile"]))) ? Convert.ToString(dsGuest.Tables[0].Rows[0]["Mobile"]) + "<br/>" : string.Empty;
                                                    lblEmail.Text = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[0].Rows[0]["Email"]))) ? Convert.ToString(dsGuest.Tables[0].Rows[0]["Email"]) : string.Empty;
                                                    lblAddress.Text = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[0].Rows[0]["Address"])) ? Convert.ToString(dsGuest.Tables[0].Rows[0]["Address"]) : string.Empty);
                                                    lblPostCode.Text = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[0].Rows[0]["PostCode"])) ? Convert.ToString(dsGuest.Tables[0].Rows[0]["PostCode"]) : string.Empty);
                                                    lblSuburb.Text = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[0].Rows[0]["Suburb"])) ? Convert.ToString(dsGuest.Tables[0].Rows[0]["Suburb"]) : string.Empty);
                                                    lblName.Text = fName + " " + lName + ", <br/> ";
                                                    hasDelAddr = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[0].Rows[0]["HasDeliveryAddress"])) ? Convert.ToInt32(dsGuest.Tables[0].Rows[0]["HasDeliveryAddress"]) : -1);
                                                    hdModeName.Value = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[0].Rows[0]["DisplayName"])) ? Convert.ToString(dsGuest.Tables[0].Rows[0]["DisplayName"]) : string.Empty);
                                                    hdDelMode.Value = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[0].Rows[0]["DelMode"])) ? Convert.ToString(dsGuest.Tables[0].Rows[0]["DelMode"]) : string.Empty);

                                                    int stateId = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[0].Rows[0]["StateId"])) ? Convert.ToInt32(dsGuest.Tables[0].Rows[0]["StateId"]) : 0);
                                                    if (stateId == 0)
                                                        lblState.Text = "";
                                                    else
                                                    {
                                                        DataSet dsState = ubiz.get_State_ById(stateId);
                                                        if (dsState != null && dsState.Tables.Count > 0)
                                                        {
                                                            lblState.Text = (!string.IsNullOrEmpty(Convert.ToString(dsState.Tables[0].Rows[0]["State"])) ? Convert.ToString(dsState.Tables[0].Rows[0]["State"]) : string.Empty);
                                                        }
                                                    }
                                                    if (hasDelAddr == 1)
                                                    {
                                                        lblDelAddr.Text = lblAddress.Text;
                                                        lblDelPostCode.Text = lblPostCode.Text + ", ";
                                                        lblDelSuburb.Text = lblSuburb.Text + ", ";
                                                        lblDelState.Text = lblState.Text;
                                                        if (Convert.ToInt32(hdDelMode.Value) == 1)
                                                        {
                                                            lblDelAddr.Text += ",<br/> ";
                                                        }
                                                        else
                                                        {
                                                            lblDelAddr.Text += ", ";
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (dsGuest.Tables[1].Rows.Count > 0)
                                                        {
                                                            if (Convert.ToInt32(hdDelMode.Value) == 1)
                                                            {
                                                                lblDelAddr.Text = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[1].Rows[0]["DelAddress"])) ? Convert.ToString(dsGuest.Tables[1].Rows[0]["DelAddress"]) + ",<br/>" : string.Empty);
                                                            }
                                                            else
                                                            {
                                                                lblDelAddr.Text = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[1].Rows[0]["DelAddress"])) ? Convert.ToString(dsGuest.Tables[1].Rows[0]["DelAddress"]) + "," : string.Empty);
                                                            }
                                                            lblDelPostCode.Text = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[1].Rows[0]["DelPostCode"])) ? Convert.ToString(dsGuest.Tables[1].Rows[0]["DelPostCode"]) + ", " : string.Empty);
                                                            lblDelSuburb.Text = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[1].Rows[0]["DelSuburb"])) ? Convert.ToString(dsGuest.Tables[1].Rows[0]["DelSuburb"]) + ", " : string.Empty);
                                                            lblDelState.Text = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[1].Rows[0]["DelState"])) ? Convert.ToString(dsGuest.Tables[1].Rows[0]["DelState"]) : string.Empty);
                                                        }
                                                    }
                                                }
                                            }
                                            if (!string.IsNullOrEmpty(lblAddress.Text) || !string.IsNullOrEmpty(lblPostCode.Text) || !string.IsNullOrEmpty(lblSuburb.Text) || !string.IsNullOrEmpty(lblState.Text))
                                            {
                                                lblEmail.Text = lblEmail.Text + "<br/>";
                                            }
                                            else
                                                lblEmail.Text = lblEmail.Text + "<br/>";
                                            if (!string.IsNullOrEmpty(lblSuburb.Text))
                                            {
                                                if (!string.IsNullOrEmpty(lblAddress.Text))
                                                    lblAddress.Text = lblAddress.Text + ", ";
                                            }
                                            if (!string.IsNullOrEmpty(lblPostCode.Text))
                                            {
                                                if (!string.IsNullOrEmpty(lblSuburb.Text))
                                                    lblSuburb.Text = lblSuburb.Text + ", ";
                                                else
                                                    lblAddress.Text = lblAddress.Text + ", ";
                                            }
                                            else
                                            {
                                                if (!string.IsNullOrEmpty(lblState.Text))
                                                    lblSuburb.Text = lblSuburb.Text + ", ";
                                            }
                                            if (!string.IsNullOrEmpty(lblState.Text))
                                            {
                                                if (!string.IsNullOrEmpty(lblPostCode.Text))
                                                    lblPostCode.Text = lblPostCode.Text + ",";
                                            }
                                            else
                                            {

                                            }
                                        }

                                        BooklistBiz biz = new BooklistBiz();
                                        decimal decLateFee = 0, total = 0, decEBD = 0, DecFinalOrderTotal, dirDeliveryCost = 0m;

                                        DataSet dtDeliveryCost = biz.BL_GET_DELIVERYCHARGES_BY_YEARID(ShoppingCartID, 0);
                                        dirDeliveryCost = Convert.ToDecimal(dtDeliveryCost.Tables[0].Rows[0]["DirectDelivertCost"]);

                                        ProductBiz pdz = new ProductBiz();
                                        DataSet dsOrderSummary = pdz.get_OrderSummay_ByLoginId(ShoppingCartID);
                                        if (dsOrderSummary != null && dsOrderSummary.Tables.Count > 0)
                                        {
                                            Repeater1.DataSource = dsOrderSummary;
                                            Repeater1.DataBind();

                                            if (dsOrderSummary.Tables[0].Rows.Count > 0)
                                            {
                                                hdStudentCount.Value = Convert.ToString(Repeater1.Items.Count);

                                                total = dsOrderSummary.Tables[0].Select().Sum(p => Convert.ToDecimal(p["Total"]));
                                                //Delivery Rates for Direct Delivery Calculation
                                                if (!string.IsNullOrEmpty(hdStudentCount.Value) && hdDelMode.Value == "1")
                                                {
                                                    if (dirDeliveryCost > 0)
                                                    {
                                                        total = total + dirDeliveryCost;
                                                        (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblDeliveryChrgs") as Label).Text = "$" + string.Format("{0:0.00}", dirDeliveryCost);
                                                    }

                                                    else
                                                    {
                                                        (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("pnlDelivery") as Panel).Visible = false;
                                                    }
                                                }
                                                else
                                                {
                                                    (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("pnlDelivery") as Panel).Visible = false;
                                                }

                                                string subStr = String.Format("{0:0.00}", total);
                                                (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblTotal") as Label).Text = "$" + subStr;
                                                Session["GrandTotalsession"] = subStr.ToString();
                                            }
                                            if (dsOrderSummary.Tables[1].Rows.Count > 0)
                                            {
                                                // LateFee
                                                decLateFee = dsOrderSummary.Tables[1].Select().Sum(p => Convert.ToDecimal(p["LateFee"]));
                                                string LateFees = String.Format("{0:0.00}", decLateFee);
                                                if (decLateFee > 0)
                                                {
                                                    (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("pnlLateFee") as Panel).Visible = true;
                                                }
                                                else
                                                {
                                                    (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("pnlLateFee") as Panel).Visible = false;
                                                }

                                                (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblLateFee") as Label).Text = "$" + LateFees;
                                            }

                                            if (dsOrderSummary.Tables[2].Rows.Count > 0)
                                            {
                                                // Early Bird Discount
                                                decEBD = Convert.ToDecimal(dsOrderSummary.Tables[2].Rows[0]["FinalEarlybirdDiscount"]);
                                                string EBD = String.Format("{0:0.00}", decEBD);
                                                if (decEBD > 0)
                                                {
                                                    (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("pnlEarly") as Panel).Visible = true;
                                                }
                                                else
                                                {
                                                    (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("pnlEarly") as Panel).Visible = false;
                                                }

                                                (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblEarlyBirdDiscount") as Label).Text = "$" + EBD;

                                                // Final Order
                                                DecFinalOrderTotal = total - decEBD + decLateFee;
                                                string FinalOrderTotal = String.Format("{0:0.00}", DecFinalOrderTotal);
                                                (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblGrandTotal") as Label).Text = "$" + FinalOrderTotal;
                                                Session["GrandTotalsession"] = FinalOrderTotal.ToString();
                                                if (dsOrderSummary.Tables[0].Rows.Count > 0)
                                                {
                                                    // GST
                                                    decimal decGST = dsOrderSummary.Tables[0].Select().Sum(p => Convert.ToDecimal(p["GST"]));
                                                    decGST = (DecFinalOrderTotal - decGST) / 11;
                                                    string GST = String.Format("{0:0.00}", decGST);

                                                    if (decGST > 0)
                                                    {
                                                        (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("pnlGST") as Panel).Visible = true;
                                                    }
                                                    else
                                                    {
                                                        (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("pnlGST") as Panel).Visible = false;
                                                    }

                                                (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblGSTIncl") as Label).Text = "$" + GST;
                                                }
                                            }

                                            if (Convert.ToInt32(hdDelMode.Value) == 1)
                                            {
                                                lblSchoolorStoreName.Text = "";
                                                lblDelname.Text = fName + " " + lName + "<br/>";
                                                hdModeName.Value = fName + " " + lName;
                                            }
                                            else if (Convert.ToInt32(hdDelMode.Value) == 2 || (Convert.ToInt32(hdDelMode.Value) == 3))
                                            {
                                                if (Convert.ToInt32(hdDelMode.Value) == 3)
                                                {
                                                    BILLINGDETAILSforpayment.Visible = false;
                                                    BILLINGDETAILSforpayinschool.Visible = true;
                                                }

                                                SchoolBiz sbiz = new SchoolBiz();
                                                if (Session["SchoolID"] != null)
                                                {
                                                    if (!string.IsNullOrEmpty((Session["SchoolID"].ToString())))
                                                    {
                                                        DataTable dt = sbiz.Get_School_Data(Convert.ToInt32(Session["SchoolID"]));
                                                        if (dt != null)
                                                        {
                                                            lblSchoolorStoreName.Text = "Your order will be available for pickup at <br/><b>" + dt.Rows[0]["Name"] + "</b><br/>";
                                                            hdModeName.Value = dt.Rows[0]["Name"].ToString();
                                                        }
                                                    }
                                                }
                                            }

                                            else if ((Convert.ToInt32(hdDelMode.Value) == 4) || Convert.ToInt32(hdDelMode.Value) == 5)
                                            {
                                                if ((Convert.ToInt32(hdDelMode.Value) == 5))
                                                {
                                                    BILLINGDETAILSforpayment.Visible = false;
                                                    BILLINGDETAILSforpayinstore.Visible = true;
                                                }

                                                SchoolBiz sbiz = new SchoolBiz();
                                                if (Session["SchoolID"] != null)
                                                {
                                                    if (!string.IsNullOrEmpty((Session["SchoolID"].ToString())))
                                                    {
                                                        DataSet dsStore = sbiz.getStore(Convert.ToInt32(Session["SchoolID"]));
                                                        if (dsStore != null && dsStore.Tables.Count > 0)
                                                        {
                                                            //lblDelPostCode.Visible = false; //Commented By Sandhya i.e on 22/09/2017
                                                            lblSchoolorStoreName.Text = "Your order will be available for pickup at <br/><b>" + dsStore.Tables[0].Rows[0]["StoreName"] + "</b><br/>";
                                                            hdModeName.Value = dsStore.Tables[0].Rows[0]["StoreName"].ToString();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                //From PayPal Page To get Transaction ID Done by Sandhya i.e 19/9/2017.
                                if (Session["token"] != null)
                                {
                                    if (!string.IsNullOrEmpty(Session["token"].ToString()))
                                    {
                                        string PaypalToken = Session["token"].ToString();
                                        GeneratePaypalTransaction(PaypalToken);
                                    }
                                    else
                                    {
                                        btnSubmitOrder.Style.Add("display", "block");
                                        BtnLoader.Style.Add("display", "none");
                                    }
                                }
                                else
                                {
                                    btnSubmitOrder.Style.Add("display", "block");
                                    BtnLoader.Style.Add("display", "none");
                                }

                                // Checking ZiPCharges Error
                                if (Session["ChargeException"] != null)
                                {
                                    if (!string.IsNullOrEmpty(Session["ChargeException"].ToString()))
                                    {
                                        setErrorText(Convert.ToString(Session["ChargeException"]));
                                        lblPMErrors.Style.Add("display", "block");
                                        btnSubmitOrder.Style.Add("display", "block");
                                        BtnLoader.Style.Add("display", "none");
                                    }
                                }
                                //From ZipPayment Page To get ZipPayment State value
                                if (Session["ZipState"] != null && Session["receipt_number"] != null)
                                {
                                    if (!string.IsNullOrEmpty(Session["ZipState"].ToString()) && !string.IsNullOrEmpty(Session["ZipProduct"].ToString()) && !string.IsNullOrEmpty(Session["ZipOrderRef"].ToString()) && !string.IsNullOrEmpty(Session["receipt_number"].ToString()))
                                    {
                                        string ZipState = Session["ZipState"].ToString();
                                        string orderid = Convert.ToString(Session["ZipOrderRef"]);
                                        string receipt_number = Convert.ToString(Session["receipt_number"]);
                                        string zipProduct = Convert.ToString(Session["ZipProduct"]);
                                        DoneZipTransaction(ZipState, orderid, receipt_number, zipProduct);
                                    }
                                    else
                                    {
                                        btnSubmitOrder.Style.Add("display", "block");
                                        BtnLoader.Style.Add("display", "none");
                                    }
                                }
                                else
                                {
                                    btnSubmitOrder.Style.Add("display", "block");
                                    BtnLoader.Style.Add("display", "none");
                                }
                                // Ends From ZipPayment Page To get ZipPayment State value
                            }
                        }
                    }
                    // Bind Return URL
                    if (string.IsNullOrEmpty(Convert.ToString(Session["UId"])))
                    {
                        RouteValueDictionary parameters =
                                       new RouteValueDictionary
                                              {
                                                       {"store",URLKeyword  }
                                              };

                        VirtualPathData vpd =
                                RouteTable.Routes.GetVirtualPath(null, "rtStorePayment", parameters);

                        Session["ReturnPageUrl"] = vpd.VirtualPath;
                    }
                }
                else
                {
                    Response.RedirectToRoute("rtError");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void LoadCCYearData()
        {
            Year.Items.Clear();
            // Populate the year list with 7 years in advance from this year
            for (int i = 0; i < 15; i++)
            {
                Year.Items.Add(new ListItem((DateTime.Now.Year + i).ToString(), (DateTime.Now.Year + i).ToString()));
            }
            Year.Items.Insert(0, new ListItem("", "0"));
            if (Year.Items.Count > 0)
            {
                Year.SelectedIndex = 0;
            }
        }
        protected void Page_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();
            Helper hbiz = new Helper();
            if (Session["UId"] != null)
            {
                if (!string.IsNullOrEmpty(Session["UId"].ToString()))
                {
                    hbiz.LogExceptiontoDB(ex, Convert.ToString(Session["UId"]));
                }
            }
            else
                hbiz.LogExceptiontoDB(ex, Convert.ToString(ShoppingCartID));
            Server.ClearError();
            Response.RedirectToRoute("rtError");
        }
        private bool ValidateCardData()
        {
            if ((cardnumber.Text.Trim() == string.Empty || cvv2.Text.Trim() == string.Empty) && (Request.Form.Get("EWAY_CARDNUMBER") == null && Request.Form.Get("EWAY_CARDCVN") == null))
            {
                lblCCError.Visible = true;
                lblCCError.Text = "Please enter the credit card & cvv number.";
                return false;
            }
            return true;
        }
        //I copied this method
        protected void btnSubmitOrder_Click(object sender, EventArgs e)
        {
            try
            {
                lblPMErrors.Text = string.Empty;
                btnSubmitOrder.Style.Add("display", "none");
                BtnLoader.Style.Add("display", "block");
                lblPMErrors.Style.Add("display", "none");

                //Month must be greater than or equal to current month checking
                if (Month.SelectedItem.Text != "" && Year.SelectedItem.Text != "")
                {
                    int _Month = Convert.ToInt32(Month.SelectedItem.Value);
                    int _Year = Convert.ToInt32(Year.SelectedItem.Value);
                    if (_Year == DateTime.Now.Year)
                    {
                        if (_Month < DateTime.Now.Month)
                        {
                            lblPMErrors.Text = "Please select valid expiry month.";
                            lblPMErrors.Style.Add("display", "block");
                            btnSubmitOrder.Style.Add("display", "block");
                            BtnLoader.Style.Add("display", "none");
                            return;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(Session["StudentYear"].ToString()))
                {
                    string YearID = Session["StudentYear"].ToString();
                    ProductBiz biz = new ProductBiz();
                    DataSet dsDates = biz.get_ImportantDates(YearID);
                    DateTime dtOrderTimeInUTC = new DateTime();
                    DateTime dtUTCTime = new DateTime();
                    foreach (DataRow dr in dsDates.Tables[0].Rows)
                    {
                        if (dr["note"].ToString() == "Booklist Order Closes" && dr["TimeInUTC"].ToString().Trim() != "")
                        {
                            dtOrderTimeInUTC = Convert.ToDateTime(dr["TimeInUTC"].ToString());
                            dtUTCTime = Convert.ToDateTime(dr["UTCTimeNow"].ToString());
                            if (dtUTCTime > dtOrderTimeInUTC)
                            {
                                lblPMErrors.Style.Add("display", "block");
                                lblPMErrors.Text = "Order closed";
                                return;
                            }
                        }
                    }
                }

                string orderId = string.Empty;
                int ordTransactionID = 0;
                int ShoppingCartPrdCount = 0;

                if (Session["ShoppingCartID"] != null)
                {
                    if (!string.IsNullOrEmpty(Session["ShoppingCartID"].ToString()))
                    {
                        //get Shopping Cart count
                        CartBiz crtb = new CartBiz();
                        ShoppingCartPrdCount = crtb.GetShoppingCount(ShoppingCartID);
                    }
                }

                SchoolBiz scbz = new SchoolBiz();
                string GrandTotal = (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblGrandTotal") as Label).Text;

                if (ShoppingCartPrdCount > 0 && IsRecaptchaVerified())
                {
                    if (Convert.ToInt32(hdDelMode.Value) == 1 || Convert.ToInt32(hdDelMode.Value) == 2 || Convert.ToInt32(hdDelMode.Value) == 4)
                    {
                        if (RB1.Checked == true || RB2.Checked == true)
                        {
                            Session["TenderID"] = "eWay";
                            if (Page.IsValid)
                            {
                                if (ValidateCardData())
                                {
                                    orderId = InsertNextTransactionIdIntoBillingTable("eWay", "P"); // Default Status is 'P' means e
                                    ordTransactionID = ProcessPayment(GrandTotal, orderId);
                                    if (ordTransactionID > 0)
                                    {
                                        SaveOrder(orderId);
                                        ProcessOrderCofirmation(orderId, Convert.ToString(ordTransactionID), GrandTotal);
                                    }
                                    else if (ordTransactionID == 0)
                                    {
                                        orderId = string.Empty;
                                        string uIdMessage = string.Empty;
                                        if (Session["OrderID"] != null)
                                        {
                                            orderId = Session["OrderID"].ToString();
                                        }
                                        string statusMessage = "EwayTransactionFailedTransactionIdNotReturned";
                                        string logMessage = "OrderID:" + orderId + " Error from Eway: TransactionId == 0";
                                        if (Session["UId"] != null)
                                        {
                                            uIdMessage = "UId: [" + Session["UId"].ToString() + "] ";
                                        }
                                        if (Session["ShoppingCartID"] != null)
                                        {
                                            uIdMessage += " ShoppingCartID: [" + Session["ShoppingCartID"].ToString() + "]";
                                        }
                                        pbiz.Insert_Transaction_Log("NA", uIdMessage, statusMessage, logMessage, orderId, "NA");

                                        lblPMErrors.Text = "There was a technical problem for processing order, please try after sometime Or contact our friendly staff for assistance.";
                                        lblPMErrors.Style.Add("display", "block");
                                        btnSubmitOrder.Style.Add("display", "block");
                                        BtnLoader.Style.Add("display", "none");
                                    }
                                    else
                                    {
                                        btnSubmitOrder.Style.Add("display", "block");
                                        BtnLoader.Style.Add("display", "none");
                                    }
                                }
                                else
                                {
                                    btnSubmitOrder.Style.Add("display", "block");
                                    BtnLoader.Style.Add("display", "none");
                                }
                            }
                        }
                        else if (RB3.Checked == true)
                        {
                            Session["TenderID"] = "PayPal";
                            ProcessPaypalPayment(GrandTotal);
                        }
                        else if (RB4.Checked == true)
                        {
                            Session["TenderID"] = "Zip";
                            btnSubmitOrder.Style.Add("display", "none");
                            BtnLoader.Style.Add("display", "block");
                            orderId = InsertNextTransactionIdIntoBillingTable("Zip", "P");
                            ZipCheckOut(orderId);
                        }
                    }
                    else
                    {
                        Session["TenderID"] = "CASH";
                        btnSubmitOrder.Style.Add("display", "none");
                        BtnLoader.Style.Add("display", "block");
                        orderId = InsertNextTransactionIdIntoBillingTable("CASH", "A"); // 'A' for Approved
                        SaveOrder(orderId);
                        ProcessOrderCofirmation(orderId, Convert.ToString(ordTransactionID), GrandTotal);

                    }
                }
            }
            catch (Exception ex)
            {
                btnSubmitOrder.Style.Add("display", "block");
                BtnLoader.Style.Add("display", "none");
                throw ex;
            }
        }
        protected void ZipCheckOut(string orderId)
        {
            try
            {
                string ZipApi = string.Empty;
                string ZipMerchantID = string.Empty;
                DataSet dsSCartData = GetShoppingCartDataBYPlacedBylogin();
                DataSet DealerZipDetails = GetDealerEwayDetails(out ERPCustomerID);
                ZipApi = (!string.IsNullOrEmpty(Convert.ToString(DealerZipDetails.Tables[0].Rows[0]["ZIPAPI"]))) ? Convert.ToString(DealerZipDetails.Tables[0].Rows[0]["ZIPAPI"]) : string.Empty;
                ZipMerchantID = (!string.IsNullOrEmpty(Convert.ToString(DealerZipDetails.Tables[0].Rows[0]["ZIPMERCHANTID"]))) ? Convert.ToString(DealerZipDetails.Tables[0].Rows[0]["ZIPMERCHANTID"]) : string.Empty;

                if (Session["ShoppingCartID"] != null) //when user is login
                {
                    if (!string.IsNullOrEmpty(Session["ShoppingCartID"].ToString()))
                    {
                        if (!string.IsNullOrEmpty(ZipApi) && !string.IsNullOrEmpty(ZipMerchantID))
                        {
                            if (dsSCartData != null)
                            {
                                if (dsSCartData.Tables.Count > 1)
                                {
                                    if (dsSCartData.Tables[1].Rows.Count > 0)
                                    {
                                        GetTransactionID(orderId, dsSCartData, ZipMerchantID, ZipApi, Convert.ToString(Session["ShoppingCartID"]));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        protected bool IsRecaptchaVerified()
        {
            //return true;
            if (String.IsNullOrEmpty(Recaptcha1.Response))
            {
                lblPMErrors.Text = "Captcha cannot be empty.";
                lblPMErrors.Style.Add("display", "block");
                return false;
            }
            else
            {
                var result = Recaptcha1.Verify();

                if (result.Success)
                {
                    lblPMErrors.Text = string.Empty;
                    lblPMErrors.Style.Add("display", "none");
                    return true;
                }
                else
                {
                    lblPMErrors.Text = "Error(s): ";
                    lblPMErrors.Style.Add("display", "block");

                    foreach (var err in result.ErrorCodes)
                    {
                        lblPMErrors.Text = lblPMErrors.Text + err;
                    }

                    return false;
                }
            }
        }
        protected DataSet GetShoppingCartData()
        {
            DataSet dsCart = null;
            try
            {
                if (Session["ShoppingCartID"] != null) //when user is login
                {
                    if (!string.IsNullOrEmpty(Session["ShoppingCartID"].ToString()))
                    {
                        //get data from shopping cart table by placedbyid
                        CartBiz cbiz = new CartBiz();
                        ShoppingCartID = Session["ShoppingCartID"].ToString();
                        dsCart = cbiz.Populate_ProductsinCartbyID(ShoppingCartID, "", 2);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dsCart;
        }
        protected DataSet GetShoppingCartDataBYPlacedBylogin()
        {
            DataSet dsCart = null;
            try
            {
                if (Session["ShoppingCartID"] != null) //when user is login
                {
                    if (!string.IsNullOrEmpty(Session["ShoppingCartID"].ToString()))
                    {
                        //get data from shopping cart table by placedbyid
                        CartBiz cbiz = new CartBiz();
                        ShoppingCartID = Session["ShoppingCartID"].ToString();
                        dsCart = cbiz.Populate_ProductsinCartbyPlacedByLogin(ShoppingCartID);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dsCart;
        }
        protected void GetTransactionID(string orderId, DataSet dsSCartData, string ZipMerchantID, string ZipApi, string PlacedByLogin)
        {
            try
            {
                string TransactionID = null;
                object response = null;
                string requestUrl = "";
                string subTotal = string.Empty;
                string GSTTotal = string.Empty;
                string Delivery = string.Empty;
                string earlyBird = string.Empty;
                string lateFee = string.Empty;
                string GrandTotal = string.Empty;
                Session["ZipsubTotal"] = string.Empty;
                Session["ZipGSTTotal"] = string.Empty;
                Session["ZipDelivery"] = string.Empty;
                Session["ZipearlyBird"] = string.Empty;
                Session["ZiplateFee"] = string.Empty;
                Session["ZipGrandTotal"] = string.Empty;

                if (Repeater1.Items.Count > 0)
                {
                    if ((Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("pnlDelivery") as Panel).Visible == true)
                    {
                        Delivery = (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblDeliveryChrgs") as Label).Text;
                        if (Delivery.Contains('$'))
                            Delivery = Delivery.Remove(0, 1);

                        Session["ZipDelivery"] = Delivery;
                    }
                    if ((Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("pnlEarly") as Panel).Visible == true)
                    {
                        earlyBird = (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblEarlyBirdDiscount") as Label).Text;
                        if (earlyBird.Contains('$'))
                            earlyBird = earlyBird.Remove(0, 1);

                        Session["ZipearlyBird"] = earlyBird;
                    }

                    if ((Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("pnlLateFee") as Panel).Visible == true)
                    {
                        lateFee = (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblLateFee") as Label).Text;
                        if (lateFee.Contains('$'))
                            lateFee = lateFee.Remove(0, 1);

                        Session["ZiplateFee"] = lateFee;
                    }

                    if ((Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("pnlGST") as Panel).Visible == true)
                    {
                        GSTTotal = (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblGSTIncl") as Label).Text;
                        if (GSTTotal.Contains('$'))
                            GSTTotal = GSTTotal.Remove(0, 1);

                        Session["ZipGSTTotal"] = GSTTotal;
                    }

                    subTotal = (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblTotal") as Label).Text;

                    GrandTotal = (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblGrandTotal") as Label).Text;
                    if (GrandTotal.Contains('$'))
                        GrandTotal = GrandTotal.Remove(0, 1);

                    Session["ZipGrandTotal"] = GrandTotal;
                }

                List<OrderLines> OrderLines = new List<OrderLines>();
                DataSet ds = obiz.BL_GETORDERDETAILS_FORZIP(PlacedByLogin);
                if (ds != null)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        requestUrl = ConfigurationSettings.AppSettings["ZipChekout"].ToString();


                        shopper shopper = new shopper();

                        billing_address billing_address = new billing_address();
                        billing_address.line1 = Convert.ToString(ds.Tables[0].Rows[0]["line1"]);
                        billing_address.city = Convert.ToString(ds.Tables[0].Rows[0]["city"]);
                        billing_address.state = Convert.ToString(ds.Tables[0].Rows[0]["state"]);
                        billing_address.postal_code = Convert.ToString(ds.Tables[0].Rows[0]["postal_code"]);
                        billing_address.country = Convert.ToString(ds.Tables[0].Rows[0]["country"]);
                        billing_address.first_name = Convert.ToString(ds.Tables[0].Rows[0]["first_name"]);
                        billing_address.last_name = Convert.ToString(ds.Tables[0].Rows[0]["last_name"]);

                        shopper.first_name = Convert.ToString(ds.Tables[0].Rows[0]["first_name"]);
                        shopper.last_name = Convert.ToString(ds.Tables[0].Rows[0]["last_name"]);
                        shopper.email = Convert.ToString(ds.Tables[0].Rows[0]["email"]);
                        shopper.billing_address = billing_address;
                        order order = new order();
                        order.reference = Guid.NewGuid().ToString();
                        shipping shipping = new shipping();
                        address address = new address();
                        address.line1 = lblDelAddr.Text.Trim().Replace("<br/>", "").TrimEnd(',');
                        address.line2 = "";
                        address.city = lblDelSuburb.Text.Trim().Replace("<br/>", "").TrimEnd(','); ;
                        address.state = lblDelState.Text.Trim().Replace("<br/>", "").TrimEnd(','); ;
                        address.postal_code = lblDelPostCode.Text.Trim().Replace("<br/>", "").TrimEnd(','); ;
                        address.country = Convert.ToString(ds.Tables[0].Rows[0]["country"]);

                        Session["Shippingline1"] = address.line1;
                        Session["Shippingcity"] = address.city;
                        Session["Shippingstate"] = address.state;
                        Session["Shippingpostal_code"] = address.postal_code;
                        Session["Shippingcountry"] = address.country;
                        shipping.pickup = Convert.ToBoolean(ds.Tables[0].Rows[0]["pickup"]);
                        shipping.address = address;
                        List<items> items = new List<items>();
                        foreach (DataRow dtrow in dsSCartData.Tables[1].Rows)
                        {
                            items.Add(
                                   new items
                                   {
                                       name = dtrow["name"].ToString(),
                                       amount = Convert.ToDecimal(dtrow["amount"]),
                                       quantity = Convert.ToInt32(dtrow["quantity"]),
                                       type = dtrow["type"].ToString(),
                                       reference = dtrow["reference"].ToString(),
                                   }
                                   );
                        }
                        if (Delivery != string.Empty)
                        {
                            items.Add(
                                        new items
                                        {
                                            name = "Delivery",
                                            amount = Convert.ToDecimal(Delivery),
                                            quantity = 1,
                                            type = "sku",
                                            reference = ""
                                        }
                                        );
                        }

                        if (lateFee != string.Empty)
                        {
                            items.Add(
                                        new items
                                        {
                                            name = "Late Fee",
                                            amount = Convert.ToDecimal(lateFee),
                                            quantity = 1,
                                            type = "sku",
                                            reference = ""
                                        }
                                        );
                        }

                        if (earlyBird != string.Empty)
                        {
                            items.Add(
                                        new items
                                        {
                                            name = "Earlybird Discount",
                                            amount = -Convert.ToDecimal(earlyBird),
                                            quantity = 1,
                                            type = "discount",
                                            reference = ""
                                        }
                                        );
                        }

                        order.amount = Convert.ToDecimal(GrandTotal);
                        order.currency = "AUD";
                        order.shipping = shipping;
                        order.items = items;
                        string shost = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
                        string returnURL = shost + "/" + URLKeyword + "/Order/ZipCheckout";
                        string ZipURi = string.Empty;
                        string ZipMerReferenceNum = string.Empty;
                        config config = new config();
                        config.redirect_uri = returnURL;
                        metadata metadata = new metadata();
                        metadata.name1 = "";
                        object blorder = new
                        {
                            shopper,
                            order,
                            config,
                            metadata
                        };
                        string Exceptionmsg = null;
                        response = MakeRequest(requestUrl, blorder, orderId, ZipApi, ref Exceptionmsg);

                        if (response != null)
                        {
                            JObject obj = response as JObject;
                            ZipURi = obj["uri"].ToString();

                            ZipMerReferenceNum = obj["order"]["reference"].ToString();
                            //ZipMerReferenceNum = orderId;
                            Session["ZipOrderRef"] = string.Empty;
                            Session["ZipMerReferenceNum"] = string.Empty;
                            if (!string.IsNullOrEmpty(ZipMerReferenceNum))
                            {
                                Session["ZipMerReferenceNum"] = ZipMerReferenceNum;

                            }
                            if (!string.IsNullOrEmpty(ZipURi))
                            {
                                Session["ZipOrderRef"] = orderId;
                                string[] URIZIP = ZipURi.Split(new string[] { "&m=" }, StringSplitOptions.None);
                                if (URIZIP.Length > 1)
                                {
                                    if (URIZIP[1] == ZipMerchantID)
                                    {
                                        Response.Redirect(ZipURi, false);
                                    }
                                    else
                                    {
                                        OrderBiz orbiz = new OrderBiz();
                                        orbiz.BL_INSERT_ZIPEXCEPTION("/checkouts", "POST", null, null, null, null, null, orderId, "Error In Payment Page GetTransactionID Method: Return ZipURi is Not Match");
                                        lblPMErrors.Text = "Unfortunately we were unable to process your transaction using zip payment method. Please select another payment method for your purchase.";
                                        lblPMErrors.Style.Add("display", "block");
                                        btnSubmitOrder.Style.Add("display", "block");
                                        BtnLoader.Style.Add("display", "none");
                                    }
                                }
                                else
                                {
                                    OrderBiz orbiz = new OrderBiz();
                                    orbiz.BL_INSERT_ZIPEXCEPTION("/checkouts", "POST", null, null, null, null, null, orderId, "Error In Payment Page GetTransactionID Method: Return ZipURi is null");
                                    lblPMErrors.Text = "Unfortunately we were unable to process your transaction using zip payment method. Please select another payment method for your purchase.";
                                    lblPMErrors.Style.Add("display", "block");
                                    btnSubmitOrder.Style.Add("display", "block");
                                    BtnLoader.Style.Add("display", "none");
                                }
                            }
                            else
                            {

                                OrderBiz orbiz = new OrderBiz();
                                orbiz.BL_INSERT_ZIPEXCEPTION("/checkouts", "POST", null, null, null, null, null, orderId, "Error In Payment Page GetTransactionID Method: Return ZipURi is null");
                                lblPMErrors.Text = "Unfortunately we were unable to process your transaction using zip payment method. Please select another payment method for your purchase.";
                                lblPMErrors.Style.Add("display", "block");
                                btnSubmitOrder.Style.Add("display", "block");
                                BtnLoader.Style.Add("display", "none");
                            }

                        }
                        else if (Exceptionmsg != null)
                        {
                            setErrorText(Exceptionmsg);
                            lblPMErrors.Style.Add("display", "block");
                            btnSubmitOrder.Style.Add("display", "block");
                            BtnLoader.Style.Add("display", "none");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        protected void setErrorText(string Exceptionmsg)
        {

            if (Exceptionmsg.Contains("(40"))
            {
                lblPMErrors.Text = "Unfortunately we were unable to process your transaction using zip payment method. Please select another payment method for your purchase.";
            }
            else if (Exceptionmsg.Contains("(5"))
            {
                lblPMErrors.Text = "Server Error An error has occurred on zipMoney's end. Please select another payment method for your purchase.";
            }
            else
            {
                lblPMErrors.Text = Exceptionmsg;
            }
        }
        public static object MakeRequest(string requestUrl, object JSONRequest, string OrderID, string ZipApi, ref string Exceptionmsg)
        {
            OrderBiz orbiz = new OrderBiz();
            try
            {
                DateTime ReqStartTime = DateTime.Now;
                string ZipVersion = ConfigurationSettings.AppSettings["ZipVersion"].ToString();
                HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest;
                string sb = JsonConvert.SerializeObject(JSONRequest);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers.Add("zip-version", ZipVersion);
                request.Headers.Add("authorization", "Bearer " + ZipApi);
                request.Headers.Add("Idempotency-Key", OrderID);


                Byte[] bt = System.Text.Encoding.UTF8.GetBytes(sb);
                Stream st = request.GetRequestStream();
                st.Write(bt, 0, bt.Length);
                st.Close();

                // Inserting ZipRequest Details to BLZipLogException table with OrderID - checkouts Request
                orbiz.BL_INSERT_ZIPEXCEPTION("/checkouts", "POST", null, sb.ToString(), null, null, null, OrderID, null);


                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    Stream stream1 = response.GetResponseStream();
                    StreamReader sr = new StreamReader(stream1);
                    string strsb = sr.ReadToEnd();
                    object objResponse = JsonConvert.DeserializeObject(strsb);
                    DateTime ReqEndTime = DateTime.Now;
                    TimeSpan span = ReqEndTime - ReqStartTime;
                    int ms = (int)span.TotalSeconds;
                    Exceptionmsg = null;
                    string Resid = null;
                    if (objResponse != null)
                    {
                        JObject obj = objResponse as JObject;
                        var properties = obj.Properties();
                        foreach (var prop in properties)
                        {
                            string key = prop.Name;
                            object value = prop.Value;
                            if (prop.Name == "id")
                            {
                                Resid = Convert.ToString(prop.Value);
                                break;
                            }
                        }
                    }
                    // Updating ZipRequest Details to BLZipLogException table with OrderID - checkouts Response
                    orbiz.BL_INSERT_ZIPEXCEPTION("/checkouts", "POST", null, sb.ToString(), strsb, Convert.ToString(ms), Resid, OrderID, null);

                    return objResponse;
                }

            }
            catch (WebException wEx)
            {

                Exceptionmsg = wEx.Message;
                string DesearialiazeMessage = string.Empty;
                // Logging ReadySell unsynced Errors in Exception Table 
                if (wEx.Response != null)
                {
                    HttpWebResponse wr = (HttpWebResponse)wEx.Response;
                    Stream stream1 = wr.GetResponseStream();
                    StreamReader sr = new StreamReader(stream1);
                    string strsb = sr.ReadToEnd();
                    object objResponse = JsonConvert.DeserializeObject(strsb);
                    if (objResponse != null)
                    {
                        JObject obj = objResponse as JObject;
                        var properties = obj.Properties();
                        foreach (var prop in properties)
                        {
                            string key = prop.Name;
                            object value = prop.Value;
                            if (prop.Name == "error")
                            {
                                JObject ReStatus = prop.Value as JObject;
                                var restatusProperties = ReStatus.Properties();
                                foreach (var reprop in restatusProperties)
                                {
                                    string rekey = reprop.Name;
                                    object revalue = reprop.Value;

                                    if (reprop.Name == "message")
                                    {
                                        DesearialiazeMessage = Convert.ToString(reprop.Value);
                                    }
                                }
                            }
                        }
                    }
                }
                // Updating ZipRequest Details to BLZipLogException table with OrderID - checkouts Response
                orbiz.BL_INSERT_ZIPEXCEPTION("/checkouts", "POST", null, null, null, null, null, OrderID, "Error in WebException Block: " + Convert.ToString(wEx) + " DeserializeMessage: " + DesearialiazeMessage);
                return null;
            }
            catch (Exception ex)
            {
                Exceptionmsg = ex.Message;
                // Updating ZipRequest Details to BLZipLogException table with OrderID - checkouts Response
                orbiz.BL_INSERT_ZIPEXCEPTION("/checkouts", "POST", null, null, null, null, null, OrderID, "Error in Catch Block: " + Exceptionmsg);
                return null;
            }
        }

        private void ProcessZipOrderCofirmation(string orderId, string ReceiptNum, string zipProduct)
        {
            try
            {
                OrderBiz orderbiz = new OrderBiz();
                string PaymentMethod = zipProduct;
                //Update orderbilling Table not insert
                orderbiz.Insert_OrderBIlling(orderId, ReceiptNum, PaymentMethod);
                Session["OrderID"] = orderId;
                string GrandTotal = (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblGrandTotal") as Label).Text;
                MailConfrimation(orderId, ReceiptNum, GrandTotal);

                RouteValueDictionary parameters =
                       new RouteValueDictionary
                       {
                           { "store", URLKeyword }
                       };

                Response.RedirectToRoute("rtOrderConf", parameters);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void ProcessOrderCofirmation(string orderId, string ordTranId, string GrandTotal)
        {
            try
            {

                OrderBiz orderbiz = new OrderBiz();
                string PaymentMethod = string.Empty;
                if (Session["TenderID"] != null)
                {
                    if (!string.IsNullOrEmpty(Session["TenderID"].ToString()))
                    {
                        PaymentMethod = Session["TenderID"].ToString();
                    }
                }

                if (ordTranId != "0")
                {
                    //Update orderbilling Table not insert
                    orderbiz.Insert_OrderBIlling(orderId, ordTranId, PaymentMethod);
                    Session["OrderID"] = orderId;
                    MailConfrimation(orderId, ordTranId, GrandTotal);
                }
                else
                {
                    if (PaymentMethod.ToUpper().Trim() == "CASH")
                    {
                        Session["OrderID"] = orderId;
                    }

                    MailConfrimation(orderId, "0", GrandTotal);
                }

                //orderId = string.Empty;
                string uIdMessage = string.Empty;

                string statusMessage = "3:OrderConfirmationProcessed";
                string logMessage = "OrderID: [" + orderId + "] Payment method: [" + PaymentMethod + "] Order email confirmation sent.  ProcessOrderCofirmation()";
                if (Session["UId"] != null)
                {
                    uIdMessage = "UId: [" + Session["UId"].ToString() + "] ";
                }
                if (Session["ShoppingCartID"] != null)
                {
                    uIdMessage += " ShoppingCartID: [" + Session["ShoppingCartID"].ToString() + "]";
                }
                pbiz.Insert_Transaction_Log("NA", uIdMessage, statusMessage, logMessage, orderId, ordTranId);

                RouteValueDictionary parameters =
                       new RouteValueDictionary
                       {
                           { "store", URLKeyword }
                       };

                Response.RedirectToRoute("rtOrderConf", parameters);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private int ProcessPayment(string GrandTotal, string OrderId)
        {
            try
            {
                int pTransactionID = 0;
                string OrderRef = string.Empty;
                string OrderRefwithoutBL = string.Empty;
                StringBuilder sbPaymentErrors = new StringBuilder();
                string CustomGatewayCardNumber = Request.Form.Get("EWAY_CARDNUMBER");
                string CustomerGatewayCVN = Request.Form.Get("EWAY_CARDCVN");
                if (CustomGatewayCardNumber == null && CustomerGatewayCVN == null)
                {
                    return pTransactionID;
                }

                DataSet DealerEwayDetails = GetDealerEwayDetails(out ERPCustomerID);
                if (DealerEwayDetails != null && DealerEwayDetails.Tables[0].Rows.Count > 0)
                {
                    EWAYAPIKEY = (!string.IsNullOrEmpty(Convert.ToString(DealerEwayDetails.Tables[0].Rows[0]["EWAYAPIKEY"]))) ? Convert.ToString(DealerEwayDetails.Tables[0].Rows[0]["EWAYAPIKEY"]) : string.Empty;
                    EWAYAPIPASSWORD = (!string.IsNullOrEmpty(Convert.ToString(DealerEwayDetails.Tables[0].Rows[0]["EWAYAPIPASSWORD"]))) ? FileHelper.Decrypt(Convert.ToString(DealerEwayDetails.Tables[0].Rows[0]["EWAYAPIPASSWORD"])) : string.Empty;
                }

                string apiKey = EWAYAPIKEY;
                string password = EWAYAPIPASSWORD.Trim();
                string rapidEndpoint = ConfigurationSettings.AppSettings["AppEnvMode"].ToString();
                string removableChars = Regex.Escape(@"$");
                string pattern = "[" + removableChars + "]";
                string TrimDollar = Regex.Replace(GrandTotal, pattern, "");
                Decimal CalculateValue = Convert.ToDecimal(TrimDollar) * 100;

                IRapidClient ewayClient = RapidClientFactory.NewRapidClient(apiKey, password, rapidEndpoint);

                if (!string.IsNullOrEmpty(OrderId))
                {
                    OrderRef = OrderId;
                    OrderRefwithoutBL = OrderRef.Substring(2, OrderRef.Length - 2);
                }
                if (ewayClient != null)
                {
                    Transaction transaction = new Transaction()
                    {
                        PaymentDetails = new PaymentDetails()
                        {
                            TotalAmount = Convert.ToInt32(CalculateValue),
                            InvoiceNumber = OrderRef,
                            InvoiceDescription = OrderRefwithoutBL,
                            InvoiceReference = OrderRefwithoutBL,
                            CurrencyCode = "AUD"
                        },
                        Customer = new Customer()
                        {
                            Reference = ERPCustomerID,
                            FirstName = fName,
                            LastName = lName,
                            Mobile = lblMobile.Text.Trim().Replace("<br/>", string.Empty),
                            Email = lblEmail.Text.Trim().Replace("<br/>", string.Empty),
                            Address = new eWAY.Rapid.Models.Address()
                            {
                                Street1 = BillingAddress.Text.Trim(),
                                City = BillingSuburb.Text.Trim(),
                                State = ddlState.SelectedItem.Text.Trim(),
                                Country = "au",
                                PostalCode = BillingPostcode.Text.Trim()
                            },
                            CardDetails = new CardDetails()
                            {
                                Name = cardholdername.Text.Trim(),
                                Number = CustomGatewayCardNumber,
                                ExpiryMonth = Month.SelectedItem.Value.Trim(),
                                ExpiryYear = Year.SelectedItem.Value.Trim(),
                                CVN = CustomerGatewayCVN
                            }

                        },
                        TransactionType = TransactionTypes.Purchase
                    };

                    CreateTransactionResponse response = ewayClient.Create(PaymentMethod.Direct, transaction);

                    if (response.Errors != null)
                    {
                        sbPaymentErrors.Append("Transaction Failed:");
                        foreach (string errorCode in response.Errors)
                        {
                            sbPaymentErrors.AppendLine(RapidClientFactory.UserDisplayMessage(errorCode, "EN"));
                        }
                    }
                    else
                    {
                        if (response.TransactionStatus != null)
                        {
                            if (response.TransactionStatus.Status.GetValueOrDefault())
                            {
                                pTransactionID = response.TransactionStatus.TransactionID;
                                return pTransactionID;
                            }
                            else
                            {
                                if (response.TransactionStatus.ProcessingDetails.ResponseMessage != null)
                                {
                                    string[] errorCodes = response.TransactionStatus.ProcessingDetails.ResponseMessage.Split(new[] { ", " }, StringSplitOptions.None);

                                    if (errorCodes.Length > 0)
                                    {
                                        foreach (string errorCode in errorCodes)
                                        {
                                            sbPaymentErrors.AppendLine(RapidClientFactory.UserDisplayMessage(errorCode, "EN"));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (sbPaymentErrors.Length > 0)
                {
                    string orderId = string.Empty;
                    string uIdMessage = string.Empty;
                    if (Session["OrderID"] != null)
                    {
                        orderId = Session["OrderID"].ToString();
                    }
                    string statusMessage = "EwayTransactionFailed";
                    string logMessage = "OrderID:" + orderId + " Error from Eway: " + sbPaymentErrors.ToString();
                    if (Session["UId"] != null)
                    {
                        uIdMessage = "UId: [" + Session["UId"].ToString() + "] ";
                    }
                    if (Session["ShoppingCartID"] != null)
                    {
                        uIdMessage += " ShoppingCartID: [" + Session["ShoppingCartID"].ToString() + "]";
                    }
                    pbiz.Insert_Transaction_Log("NA", uIdMessage, statusMessage, logMessage, orderId, "NA");

                    lblPMErrors.Text = sbPaymentErrors.ToString();
                    lblPMErrors.Style.Add("display", "block");
                    return -1;
                }

                return pTransactionID;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }
        public void ProcessPaypalPayment(string GrandTotal)
        {
            try
            {
                string Username = string.Empty;
                string Pswd = string.Empty;
                string Signature = string.Empty;
                string EndPointUrl = string.Empty;
                DataSet DealerPaypalDetails = GetDealerEwayDetails(out ERPCustomerID);
                if (DealerPaypalDetails != null && DealerPaypalDetails.Tables.Count > 0)
                {
                    if (DealerPaypalDetails.Tables[0].Rows.Count > 0)
                    {
                        Username = (!string.IsNullOrEmpty(Convert.ToString(DealerPaypalDetails.Tables[0].Rows[0]["PAYPALUSERNAME"]))) ? Convert.ToString(DealerPaypalDetails.Tables[0].Rows[0]["PAYPALUSERNAME"]) : string.Empty;
                        Pswd = (!string.IsNullOrEmpty(Convert.ToString(DealerPaypalDetails.Tables[0].Rows[0]["PAYPALPASSWORD"]))) ? Convert.ToString(DealerPaypalDetails.Tables[0].Rows[0]["PAYPALPASSWORD"]) : string.Empty;
                        Signature = (!string.IsNullOrEmpty(Convert.ToString(DealerPaypalDetails.Tables[0].Rows[0]["PAYPALSIGNATURE"]))) ? Convert.ToString(DealerPaypalDetails.Tables[0].Rows[0]["PAYPALSIGNATURE"]) : string.Empty;
                        EndPointUrl = (!string.IsNullOrEmpty(Convert.ToString(DealerPaypalDetails.Tables[0].Rows[0]["PAYPALENDPOINT"]))) ? Convert.ToString(DealerPaypalDetails.Tables[0].Rows[0]["PAYPALENDPOINT"]) : string.Empty;
                        if (!string.IsNullOrEmpty(Pswd))
                        {
                            Pswd = FileHelper.Decrypt(Pswd);
                        }
                    }
                }
                string retMsg = "";
                string token = "";
                NVPAPICaller payPalCaller = new NVPAPICaller();
                bool ret = payPalCaller.ShortcutExpressCheckout(GrandTotal, ref token, ref retMsg, Username, Pswd, Signature, EndPointUrl, URLKeyword);
                if (ret)
                {
                    Session["token"] = token;
                    btnSubmitOrder.Style.Add("display", "none");
                    BtnLoader.Style.Add("display", "block");
                    Response.Redirect(retMsg, false);
                }
                else
                {
                    lblPMErrors.Text = retMsg;
                    lblPMErrors.Style.Add("display", "block");
                    btnSubmitOrder.Style.Add("display", "block");
                    BtnLoader.Style.Add("display", "none");
                }
            }
            catch (Exception ex)
            {
                lblPMErrors.Text = "There was a technical problem for processing order, please try after sometime Or contact our friendly staff for assistance.";
                lblPMErrors.Style.Add("display", "block");
                btnSubmitOrder.Style.Add("display", "block");
                BtnLoader.Style.Add("display", "none");
                throw ex;
            }
        }
        private string InsertNextTransactionIdIntoBillingTable(string PaymentMethod, string Status)
        {
            string orderId = string.Empty;
            try
            {
                DataSet dsOrderId = pbiz.get_NextOrderId(PaymentMethod, Status);
                if (dsOrderId != null && dsOrderId.Tables.Count > 0)
                {
                    orderId = (Convert.ToString(dsOrderId.Tables[0].Rows[0]["OrderId"]) != null) ? Convert.ToString(dsOrderId.Tables[0].Rows[0]["OrderId"]) : string.Empty;
                    string uIdMessage = string.Empty;
                    pbiz.Move_ShoppingCart_To_OrderDetailsTemp(orderId, ShoppingCartID, orderNotes.Text, Constants_Wrapper.Order_Processing);
                    string statusMessage = "1:TransactionStarted";
                    string logMessage = "OrderID: [" + orderId + "] PaymentMethod: [" + PaymentMethod + "] generated and Order Billing inserted. Shopping details inserted into BLORDERDETAILS_TEMPTRANS table. InsertNextTransactionIdIntoBillingTable() Status: [" + Status + "]";
                    if (Session["UId"] != null)
                    {
                        uIdMessage = "UId: [" + Session["UId"].ToString() + "] ";
                    }
                    if (Session["ShoppingCartID"] != null)
                    {
                        uIdMessage += " ShoppingCartID: [" + Session["ShoppingCartID"].ToString() + "]";
                    }
                    pbiz.Insert_Transaction_Log("NA", uIdMessage, statusMessage, logMessage, orderId, "NA");
                    Session["OrderID"] = orderId;
                    //This records temp order details in BLORDERDETAILS_TEMPTRANS which gets deleted when records have been written to all related tables in a transaction

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return orderId;
        }
        private void SaveOrder(string orderId)
        {
            try
            {
                if (!string.IsNullOrEmpty(orderId))
                {
                    if (Session["ShoppingCartID"] != null) //when user is login
                    {
                        if (!string.IsNullOrEmpty(Session["ShoppingCartID"].ToString()))
                        {
                            //get data from shopping cart table by placedbyid
                            CartBiz cbiz = new CartBiz();
                            ShoppingCartID = Session["ShoppingCartID"].ToString();
                            pbiz.Move_ShoppingCart_To_OrderDetails(orderId, ShoppingCartID, orderNotes.Text, Constants_Wrapper.Order_Processing);

                            //orderId = string.Empty;
                            string uIdMessage = string.Empty;

                            string statusMessage = "2:ShoppingCartMovedToOrderTables";
                            string logMessage = "OrderID:" + orderId + " Order details moved to order related tables from cart. Temp order details should be deleted";
                            if (Session["UId"] != null)
                            {
                                uIdMessage = "UId: [" + Session["UId"].ToString() + "] ";
                            }
                            if (Session["ShoppingCartID"] != null)
                            {
                                uIdMessage += " ShoppingCartID: [" + Session["ShoppingCartID"].ToString() + "]";
                            }
                            pbiz.Insert_Transaction_Log("NA", uIdMessage, statusMessage, logMessage, orderId, "NA");
                        }
                    }

                    if (!string.IsNullOrEmpty(hdDelMode.Value) && !string.IsNullOrEmpty(hdModeName.Value))
                    {
                        lblEmail.Text = lblEmail.Text.Replace("<br/>", "");
                        lblMobile.Text = lblMobile.Text.Replace("<br/>", "");
                        lblDelAddr.Text = lblDelAddr.Text.Replace("<br/>", "");
                        lblDelSuburb.Text = lblDelSuburb.Text.Replace("<br/>", "");
                        lblDelPostCode.Text = lblDelPostCode.Text.Replace("<br/>", "");
                        lblDelState.Text = lblDelState.Text.Replace("<br/>", "");
                        lblEmail.Text = lblEmail.Text.Replace(",", "");
                        lblMobile.Text = lblMobile.Text.Replace(",", "");
                        lblDelAddr.Text = lblDelAddr.Text.Replace(",", "");
                        lblDelSuburb.Text = lblDelSuburb.Text.Replace(",", "");
                        lblDelPostCode.Text = lblDelPostCode.Text.Replace(",", "");
                        lblDelState.Text = lblDelState.Text.Replace(",", "");
                        pbiz.insert_Order_Delivery_Details(hdModeName.Value, lblDelAddr.Text, lblDelSuburb.Text,
                        lblDelPostCode.Text, lblEmail.Text, lblMobile.Text, orderId, Convert.ToInt32(hdDelMode.Value), lblDelState.Text);
                    }

                    int st = 0;
                    //Insert Billing Details
                    if (ddlState.SelectedValue == "")
                    {
                        st = 0;
                    }
                    else
                    {
                        st = Convert.ToInt32(ddlState.SelectedValue);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void MailConfrimation(string orderId, string TransactionID, string TotalAmount)
        {
            string AzureConn = ConfigurationManager.ConnectionStrings["AzureStorageAccount"].ConnectionString;
            string AzureResource = ConfigurationManager.AppSettings["AzureResource"];
            CloudStorageAccount sa = CloudStorageAccount.Parse(AzureConn);
            CloudBlobClient bd = sa.CreateCloudBlobClient();
            CloudBlobContainer container = bd.GetContainerReference(AzureResource);
            container.CreateIfNotExists(BlobContainerPublicAccessType.Blob);

            try
            {

                string LoginGuestorUser = string.Empty;
                string emaiIDl = string.Empty;
                string PlacedByLoginId = string.Empty;
                string schoolName = string.Empty; string studentName = string.Empty; string year = string.Empty;
                if (!ShoppingCartID.Contains("Guest_"))
                {
                    if (Session["UId"] != null)
                    {
                        if (!string.IsNullOrEmpty(Session["UId"].ToString()))
                        {
                            UserBiz ubiz = new UserBiz();
                            DataSet dsConsumer = ubiz.Get_UserInfo(Convert.ToInt32(Session["UId"]));
                            if (dsConsumer != null && dsConsumer.Tables.Count > 0)
                            {
                                string firstName = (!string.IsNullOrEmpty(Convert.ToString(dsConsumer.Tables[0].Rows[0]["FName"]))) ? Convert.ToString(dsConsumer.Tables[0].Rows[0]["FName"]) : string.Empty;
                                emaiIDl = (!string.IsNullOrEmpty(Convert.ToString(dsConsumer.Tables[0].Rows[0]["Email"]))) ? Convert.ToString(dsConsumer.Tables[0].Rows[0]["Email"]) : string.Empty;
                                LoginGuestorUser = emaiIDl;
                                emaiIDl = emaiIDl + "," + firstName;
                            }
                        }
                    }
                }
                else if (ShoppingCartID.Contains("Guest_"))
                {
                    DataSet dsGuest = pbiz.Populate_ConsumerDetails(ShoppingCartID);
                    if (dsGuest != null && dsGuest.Tables.Count > 0)
                    {
                        LoginGuestorUser = ShoppingCartID;
                        emaiIDl = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[0].Rows[0]["Email"]))) ? Convert.ToString(dsGuest.Tables[0].Rows[0]["Email"]) : string.Empty;
                        string fName = (!string.IsNullOrEmpty(Convert.ToString(dsGuest.Tables[0].Rows[0]["FName"]))) ? Convert.ToString(dsGuest.Tables[0].Rows[0]["FName"]) : string.Empty;
                        emaiIDl = emaiIDl + "," + fName;
                    }
                }

                string[] str = emaiIDl.Split(',');
                string name = string.Empty;
                if (str.Length > 1)
                {
                    PlacedByLoginId = str[0];
                    name = str[1];
                }

                SendEmailHelper seh = new SendEmailHelper();
                string Product, Quantity, Price, comment, _To = "";
                string bodytr = string.Empty;
                int noOfStdnts = 0;
                OrderBiz obiz = new OrderBiz();
                DataSet dsOrderSummary = obiz.Populate_StudentDetails_ByOrderId(orderId);
                if (dsOrderSummary != null && dsOrderSummary.Tables.Count > 0)
                {
                    //Set no of Students in Order
                    noOfStdnts = dsOrderSummary.Tables[0].Rows.Count;
                    string StoreInfo = string.Empty;
                    string StorePhoneNumber = string.Empty;
                    string StoreName = string.Empty;
                    string customOrderMsg = String.Empty;
                    string brand = string.Empty;
                    string FromAddress = string.Empty;
                    bool XMLAttachment = false;

                    _To = PlacedByLoginId;
                    _Subject = "Order Confirmation - Your Order with Booklist " + orderId + " has been successfully placed!";
                    _Body.Clear();
                    if (Session["SchoolID"] != null)
                    {
                        if (!string.IsNullOrEmpty((Session["SchoolID"].ToString())))
                        {
                            SchoolBiz sbiz = new SchoolBiz();
                            DataTable dt = sbiz.Get_School_Data(Convert.ToInt32(Session["SchoolID"]));
                            string img = string.Empty;
                            string BackGround = string.Empty;

                            string shost = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
                            if (dt != null && dt.Rows.Count > 0)
                            {
                                schoolName = (!string.IsNullOrEmpty(Convert.ToString(dt.Rows[0]["Name"]))) ? Convert.ToString(dt.Rows[0]["Name"]) : string.Empty;
                                customOrderMsg = (!string.IsNullOrEmpty(Convert.ToString(dt.Rows[0]["CustomMessage"]))) ? Convert.ToString(dt.Rows[0]["CustomMessage"]) : string.Empty;
                                if (dt.Rows[0]["Image"] != null && dt.Rows[0]["isDynamicPath"].ToString() == "0")
                                    //img = container.Uri.ToString() + "/SchoolLogo/" + dt.Rows[0]["Image"].ToString();
                                    img = dt.Rows[0]["Image"].ToString();
                                else
                                    img = dt.Rows[0]["Image"].ToString();

                                _Body.Append("<table style='width:100%;'><tr><td style='font-family:Arial,sans-serif;vertical-align:middle;text-align:center;' align='center'><center>");
                                _Body.Append("<table><tr><td><img src=" + img.ToString() + " alt='schoollogo' style='height:100px;' height='100'></td>" +
                                            "<td><h2 style='font-family:Arial,sans-serif;'>&nbsp;" + dt.Rows[0]["Name"] + "</h2></td></tr></table></center></td></tr></table>");
                                DataSet dtDealerDetails = sbiz.GET_URLKeyWord_ByDealerNo(Convert.ToInt32(dt.Rows[0]["DealerNo"]));
                                if (dtDealerDetails != null && dtDealerDetails.Tables[0].Rows.Count > 0)
                                {
                                    brand = (!string.IsNullOrEmpty(Convert.ToString(dtDealerDetails.Tables[0].Rows[0]["Brand"]))) ? Convert.ToString(dtDealerDetails.Tables[0].Rows[0]["Brand"]) : string.Empty;
                                    if (brand == "Office National")
                                    {
                                        BackGround = "<tr style='background:rgb(0, 44, 117);color:#fff;'>";
                                    }
                                    else if (brand == "Office Products Depot")
                                    {
                                        BackGround = "<tr style='background:#5c707c;color:#fff;'>";
                                    }
                                    else if (brand == "O-NET")
                                    {
                                        BackGround = "<tr style='background:rgb(0, 44, 117);color:#fff;'>";
                                    }

                                    bodytr = BackGround;
                                    StoreName = dtDealerDetails.Tables[0].Rows[0]["DealerName"].ToString();
                                    StoreInfo = dtDealerDetails.Tables[0].Rows[0]["Address"].ToString() + ", " + dtDealerDetails.Tables[0].Rows[0]["Location"].ToString() + ", " + dtDealerDetails.Tables[0].Rows[0]["State"].ToString() + ", " + dtDealerDetails.Tables[0].Rows[0]["Postcode"].ToString();
                                    StorePhoneNumber = dtDealerDetails.Tables[0].Rows[0]["Phone"].ToString();
                                }
                                if (dtDealerDetails.Tables[1].Rows.Count > 0)
                                {
                                    FromAddress = dtDealerDetails.Tables[1].Rows[0]["Email"].ToString();
                                    XMLAttachment = Convert.ToBoolean(dtDealerDetails.Tables[1].Rows[0]["XMLAttachment"].ToString());
                                }
                            }
                        }
                    }

                    _Body.Append("<p class=MsoNormal></p><div style='mso-element:para-border-div;border:none;border-bottom:dashed gray 1.0pt;mso-border-bottom-alt:dashed gray .75pt;padding:0cm 0cm 0cm 0cm'>" +
                                    "<h2 style='border:none;mso-border-bottom-alt:dashed gray .75pt;padding:0cm;mso-padding-alt:0cm 0cm 0cm 0cm; text-align:center;'><span style='font-size:16.5pt;color:#D2100D;font-family: Arial, sans-serif;'>" +
                                    "Thank you for your order " + name + ",</span></h2></div>");

                    _Body.Append("<div><p class=MsoNormal style='background:#FFF4EA;font-family: Arial, sans-serif;'>&nbsp;Your order number is <strong><span style='font-family:'Calibri',sans-serif'>#" + orderId + "</span></strong>. A summary of your order is shown below." +
                                      "</p></div><p class=MsoNormal></p>");
                    if (!string.IsNullOrEmpty(customOrderMsg))
                    {
                        _Body.Append("<div><p style='font-family: Arial, sans-serif;width:100%;'>" + customOrderMsg + "</p></div><p class=MsoNormal></p>");
                    }

                    string GetPaymentMethod = paymentmethod();
                    if (!string.IsNullOrEmpty(GetPaymentMethod))
                    {
                        _Body.Append("<h3 style='font-family:Arial,sans-serif;'>Payment Method : " + GetPaymentMethod + "</h3>");
                    }

                    _Body.Append("<table class=MsoNormalTable border='0' cellspacing='0' cellpadding='0' width='100%' style='width:100.0%;mso-cellspacing:0cm;mso-yfti-tbllook:1184;mso-padding-alt:0cm 0cm 0cm 0cm'> " +
                            "<tr style='mso-yfti-irow:0;mso-yfti-firstrow:yes;mso-yfti-lastrow:yes'><td width='33%' valign='top' style='width:33.0%;padding:0cm 0cm 0cm 0cm' id=Left><h3><span style='font-family:Arial,sans-serif'> Delivery &#47; Collection Address " +
                            "</span></h3><div>");

                    _Body.Append("<p class=MsoNormal><strong><span style='font-size:9.0pt;font-family:Arial,sans-serif'></span></strong><span style='font-size:9.0pt;font-family:Arial,sans-serif'>" +
                        "" + lblDelname.Text + " " + lblSchoolorStoreName.Text + "" + lblDelAddr.Text + " " + lblDelSuburb.Text + "  " + lblDelState.Text + "  " + lblDelPostCode.Text + "Australia.</span></p></div></td>");

                    //EDIT by ALEX NEW 3rd COLUMN - updated other columns width to reflect. Devteam to update dynamic text with parentname/mobile/email
                    _Body.Append("<td width='33%' valign='top' style='width:33.0%;padding:0cm 0cm 0cm 0cm' id=Right><h3><span style='font-family:Arial,sans-serif'> Ordered By" +
                                 "</span></h3><div>");
                    _Body.Append("<p class=MsoNormal><strong><span style='font-size:9.0pt;font-family:Arial,sans-serif'></span></strong><span style='font-size:9.0pt;font-family:Arial,sans-serif'>" +
                        //UPDATE THESE LABELS TO SHOW PARENT NAME, MOBILE, EMail address.
                        "<b>" + UppercaseFirst(lblName.Text).Replace(",", string.Empty) + "</b> " + lblMobile.Text.Replace(",", string.Empty) + "<br/>" + lblEmail.Text + "</span></p></div></td>");

                    _Body.Append("<td width='33%' valign='top' style='width:33.0%;padding:0cm 0cm 0cm 0cm' id=Right><h3><span style='font-family:Arial,sans-serif'> Details &#47; Instructions" +
                                 "</span></h3><div>");

                    if (hdDelMode.Value == "1")
                    {
                        _Body.Append("<p class=MsoNormal><strong><span style='font-size:9.0pt;font-family:Arial,sans-serif'> Your order will be processed and sent to the delivery address as requested.<br>" +
                      "If you have any questions, you can speak to one of our friendly staff on " + StorePhoneNumber + ".</span></strong><br>");
                    }

                    if (hdDelMode.Value == "4" || hdDelMode.Value == "5")
                    {
                        string checkoutStorePaymentMessage = string.Empty;
                        if (hdDelMode.Value == "5")
                        {
                            if (Session["CheckoutNotes"] != null)
                            {
                                if (!string.IsNullOrEmpty((Session["CheckoutNotes"].ToString())))
                                {
                                    checkoutStorePaymentMessage = Session["CheckoutNotes"].ToString();
                                }
                            }
                        }

                        _Body.Append("<strong><span style='font-size:9.0pt;font-family:Arial,sans-serif'> Your order will be processed for collection in store.<br>" +
                                     checkoutStorePaymentMessage + " <br>" +
                                     "If you have any questions, you can call us " + StorePhoneNumber + ".</span></strong><br>");

                    }

                    if (hdDelMode.Value == "2" || hdDelMode.Value == "3")
                    {
                        _Body.Append("<strong><span style='font-size:9.0pt;font-family:Arial,sans-serif'> Your order will be processed and will be available for collection at your school.<br>" +
                               "If you have any questions, please contact your school administrator.</span></strong>");

                    }

                    if (!string.IsNullOrEmpty(orderNotes.Text))
                    {
                        _Body.Append("<strong><span style='font-size:9.0pt;font-family:Arial,sans-serif'> " + orderNotes.Text + "</span></strong>");
                    }

                    _Body.Append("</p></div></td></tr></table>");

                    _Body.Append("<h3 style='font-family:Arial,sans-serif;'>Order summary details for your reference: </h3>");

                    for (int i = 0; i < dsOrderSummary.Tables[0].Rows.Count; i++)
                    {
                        DataSet DsProducts = obiz.Populate_OrderDetails(LoginGuestorUser, dsOrderSummary.Tables[0].Rows[i]["stid"].ToString(), 1);
                        _Body.Append("<table class=MsoNormalTable border='0' cellspacing='0' cellpadding='0' width='100%' style='width:100.0%;mso-cellspacing:0cm;mso-yfti-tbllook:1184;mso-padding-alt:2.0pt 2.0pt 2.0pt 2.0pt'>");
                        string StudentName = dsOrderSummary.Tables[0].Rows[i]["Name"].ToString();

                        string customStudentIdString = String.Empty;
                        if (!dsOrderSummary.Tables[0].Rows[i].IsNull("CustomStudentId"))
                        {
                            customStudentIdString = dsOrderSummary.Tables[0].Rows[i]["CustomStudentId"].ToString();
                        }

                        if (!string.IsNullOrEmpty(customStudentIdString.Trim()))
                        {
                            customStudentIdString = " ID: " + customStudentIdString.Trim();
                        }

                        int schoolId = Convert.ToInt32(Session["SchoolID"]);
                        string gender = dsOrderSummary.Tables[0].Rows[i]["Gender"] as string;

                        switch (gender)
                        {
                            case "M":
                                gender = "Male";
                                break;
                            case "F":
                                gender = "Female";
                                break;
                            case "O":
                                gender = blSchoolService.GetAdditionalGenderNameBySchoolId(schoolId);
                                break;
                            default:
                                gender = string.Empty;
                                break;
                        }

                        if (blSchoolService.IsGenderEnabled(schoolId))
                            studentName = $"{StudentName}{customStudentIdString} Gender: {gender}";
                        else
                            studentName = $"{StudentName}{customStudentIdString}";

                        _Body.Append(
                      "" + bodytr + "<th style = 'font-family: Arial, sans-serif;line-height:1.428571429;vertical-align: top;text-align: left;font-weight: bold;border-bottom: 1px solid #dddddd;padding:20px 10px;'colspan ='3'>" + studentName + "</th></tr>");
                        if (DsProducts != null)
                        {
                            if (DsProducts.Tables[0].Rows.Count != 0)
                            {
                                for (int h = 0; h < DsProducts.Tables[0].Rows.Count; h++)
                                {
                                    string catalgouenmae = DsProducts.Tables[0].Rows[h]["CatalogueName"].ToString();
                                    string catalgoueID = DsProducts.Tables[0].Rows[h]["BLCatalogueId"].ToString();
                                    string YearName = DsProducts.Tables[0].Rows[h]["yearname"].ToString();
                                    year = DsProducts.Tables[0].Rows[h]["yearname"].ToString();
                                    string yearandcat = YearName + "( " + catalgouenmae + ")";
                                    bool MandatoryCheck = Convert.ToBoolean(DsProducts.Tables[0].Rows[h]["Mandatory"].ToString());
                                    bool hidePrice = Convert.ToBoolean(DsProducts.Tables[0].Rows[h]["HidePrice"].ToString());
                                    DataSet dsectionlistProducts = obiz.Populate_OrderDetails(LoginGuestorUser, dsOrderSummary.Tables[0].Rows[i]["stid"].ToString(), Convert.ToInt32(catalgoueID));
                                    decimal PackagePrice = dsectionlistProducts.Tables[2].Select().Sum(p => Convert.ToDecimal(p["BLPrice"]) * Convert.ToInt32(p["BLMinQTY"]));
                                    PackagePrice = Convert.ToDecimal(PackagePrice);
                                    if (hidePrice == true)
                                    {
                                        _Body.Append("<tr style='background: rgb(105, 105, 105); color:#fff;'><th style='font-family: Arial, sans-serif;line-height:1.428571429;vertical-align: top;text-align: left;font-weight: bold;border-bottom: 1px solid #dddddd;padding:7px 10px;' colspan='2';>" + yearandcat + "</th>");
                                        _Body.Append("<th style = 'font-family: Arial, sans-serif;line-height:1.428571429;vertical-align: top;text-align: right;font-weight: bold;border-bottom: 1px solid #dddddd;padding:7px 10px;'>Package Price: " + "$" + PackagePrice + "</th>");
                                    }
                                    else
                                    {
                                        _Body.Append("<tr style='background: rgb(105, 105, 105); color:#fff;'><th style='font-family: Arial, sans-serif;line-height:1.428571429;vertical-align: top;text-align: left;font-weight: bold;border-bottom: 1px solid #dddddd;padding:7px 10px;' colspan='3';>" + yearandcat + "</th>");
                                    }
                                    _Body.Append("</tr>");
                                    if (dsectionlistProducts != null && dsectionlistProducts.Tables[0].Rows.Count != 0)
                                    {
                                        _Body.Append("<tr><th style='font-family: Arial, sans-serif;background-color: #efefef;text-align:left;padding:8px;line-height:1.428571429;vertical-align:top;border-top:1px solid #dddddd;'>Product</th><th style='font-family: Arial, sans-serif;text-align: center;background-color: #efefef;padding:8px;line-height:1.428571429;vertical-align:top;border-top:1px solid #dddddd;'>QTY</th>");
                                        if (hidePrice == false)
                                            _Body.Append("<th style='font-family: Arial, sans-serif;text-align: right;background-color: #efefef;padding:8px;line-height:1.428571429;vertical-align:top;border-top:1px solid #dddddd;'>Total Price</th>");
                                        else
                                            _Body.Append("<th style='font-family: Arial, sans-serif;text-align: right;background-color: #efefef;padding:8px;line-height:1.428571429;vertical-align:top;border-top:1px solid #dddddd;'></th>");
                                        _Body.Append("</tr>");

                                        for (int j = 0; j < dsectionlistProducts.Tables[2].Rows.Count; j++)
                                        {
                                            int prdGroupId = (!string.IsNullOrEmpty(Convert.ToString(dsectionlistProducts.Tables[2].Rows[j]["BLPrdGroupId"]))) ? Convert.ToInt32(dsectionlistProducts.Tables[2].Rows[j]["BLPrdGroupId"]) : 0;
                                            Product = dsectionlistProducts.Tables[2].Rows[j]["BLProductName"].ToString();
                                            if (prdGroupId > 0)
                                            {
                                                ProductBiz biz = new ProductBiz();
                                                DataSet dsGroupDetails = biz.Get_Group_Details(prdGroupId);
                                                if (dsGroupDetails != null && dsGroupDetails.Tables.Count > 0)
                                                {
                                                    string prdName = Product;
                                                    Product = Convert.ToString(dsGroupDetails.Tables[0].Rows[0]["BLGroupName"]);
                                                    if (!string.IsNullOrEmpty(prdName))
                                                    {
                                                        Product = Product + " - " + prdName;
                                                    }
                                                }
                                            }
                                            Quantity = dsectionlistProducts.Tables[2].Rows[j]["BLMinQTY"].ToString();
                                            Price = dsectionlistProducts.Tables[2].Rows[j]["BLPrice"].ToString();
                                            comment = dsectionlistProducts.Tables[2].Rows[j]["Comment"].ToString();
                                            string GenerateProductList = GenerateGridOrder(Product, comment, Quantity, Price, hidePrice);
                                            _Body.Append(GenerateProductList);
                                        }
                                    }
                                }
                            }
                        }
                        _Body.Append("<tr style='margin-bottom:1em;'>");
                        _Body.Append("</tr>");
                        _Body.Append("</table>");

                    }

                    DataSet OrderSummary = obiz.Populate_OrderDetails_ByOrderId(orderId);
                    if (OrderSummary != null && OrderSummary.Tables.Count > 0)
                    {
                        string subTotal = string.Empty;
                        string GSTTotal = string.Empty;
                        string Delivery = string.Empty;
                        string earlyBird = string.Empty;
                        string lateFee = string.Empty;

                        if (Repeater1.Items.Count > 0)
                        {
                            if ((Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("pnlDelivery") as Panel).Visible == true)
                            {
                                Delivery = (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblDeliveryChrgs") as Label).Text;
                            }
                            if ((Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("pnlEarly") as Panel).Visible == true)
                            {
                                earlyBird = (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblEarlyBirdDiscount") as Label).Text;
                            }

                            if ((Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("pnlLateFee") as Panel).Visible == true)
                            {
                                lateFee = (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblLateFee") as Label).Text;
                            }

                            if ((Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("pnlGST") as Panel).Visible == true)
                            {
                                GSTTotal = (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblGSTIncl") as Label).Text;
                            }

                            subTotal = (Repeater1.Controls[Repeater1.Controls.Count - 1].Controls[0].FindControl("lblTotal") as Label).Text;

                        }

                        string subStr = String.Format("{0:0.00}", subTotal);
                        string GSTStrF = String.Format("{0:0.00}", GSTTotal);
                        //code added for calculations             

                        if (!string.IsNullOrEmpty(Delivery))
                        {
                            Delivery = "+" + Delivery;
                            _Body.Append(OrderSummaryTable("Delivery", Delivery));
                        }
                        _Body.Append(OrderSummaryTable("Subtotal", subTotal));
                        if (!string.IsNullOrEmpty(earlyBird))
                        {
                            earlyBird = "-" + earlyBird;
                            _Body.Append(OrderSummaryTable("Discount", earlyBird));
                        }
                        if (!string.IsNullOrEmpty(lateFee))
                        {
                            lateFee = "+" + lateFee;
                            _Body.Append(OrderSummaryTable("ProcessingFee", lateFee));
                        }
                        if (!string.IsNullOrEmpty(GSTStrF))
                        {
                            GSTStrF = "<small>(" + GSTStrF + ")</small>";
                            _Body.Append(OrderSummaryTable("<small>(GST included)</small>", GSTStrF));
                        }
                        _Body.Append(OrderSummaryTable("Grand Total (Inc GST)", TotalAmount));
                        _Body.Append("<hr>");
                    }

                    string host = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);

                    if (brand == "Office National")
                    {
                        host = container.Uri.ToString() + "/images/office-national-logo.png";
                    }
                    else if (brand == "Office Products Depot")
                    {
                        host = container.Uri.ToString() + "/images/officeproductsdepot-logo.png";
                    }
                    else if (brand == "O-NET")
                    {
                        host = container.Uri.ToString() + "/images/O-NET-logo.png";
                    }

                    _Body.Append("<table style ='width:100%;'><tr><td style='font-family:Arial,sans-serif; vertical-align: middle;text-align:center;'align='center'><center><table>" +
                      "<tr><td><img src=" + host.ToString() + " alt='Brandlogo' style ='width:145px;height:41px;vertical-align:middle;' width='145' height='41'></td>" +
                    "<td><span style='font-size:12.0pt;font-family:Arial,sans-serif;'>&nbsp;<strong>" + StoreName + "</strong> - " + StorePhoneNumber + " - " + StoreInfo + " </span ></td></tr></table>" +
                    "</center></td></tr></table>");

                    // get bcc email from db  
                    string sandbox = ConfigurationManager.AppSettings["AppEnvMode"];
                    string bcc = string.Empty;
                    List<string> emailList = new List<string>();

                    DataSet dsEmail = new DataSet();

                    if (!string.IsNullOrEmpty(Session["SchoolID"]?.ToString()))
                    {
                        dsEmail = obiz.get_Dealer_EmailId(Convert.ToInt32(Session["SchoolID"]));

                        string schoolAdminEmail = blSchoolService.GetEnabledSchoolAdminEmail(Convert.ToInt32(Session["SchoolID"])).Result;
                        if (!string.IsNullOrEmpty(schoolAdminEmail))
                            emailList.Add(schoolAdminEmail?.Trim());
                    }

                    if (dsEmail != null && dsEmail.Tables[0].Rows.Count > 0)
                    {
                        string dealerEmail = dsEmail.Tables[0].Rows[0]["Email"] as string;

                        if (!string.IsNullOrEmpty(dealerEmail))
                            emailList.Add(dealerEmail.Trim());
                    }

                    //if (sandbox.ToUpper() == "SANDBOX")
                    //{

                    //}
                    //else
                    //{
                    //    //this is for live
                    //    bcc = bcc + "," + _BCC;
                    //}

                    //Add BCC email if configured in db AppSetting
                    if (AppSetting.GetInstance().IsSupportAdminNotificationOn())
                    {
                        string adminEmailBCC = AppSetting.GetInstance().OrderConfirmationEmailNotificationBCC();

                        if (!string.IsNullOrEmpty(adminEmailBCC))
                            emailList.Add(adminEmailBCC.Trim());
                    }

                    bcc = string.Join(";", emailList);

                    if (XMLAttachment == false)
                    {
                        seh.SetOrderConfirmation(PlacedByLoginId, _Body, _Subject, bcc, schoolName, studentName, year);
                        seh.SendEmail(FromAddress);
                    }
                    else
                    {
                        // Generate XML file
                        GenerateXMLfile(orderId);
                        DataTable dtOrderMails = new DataTable();
                        dtOrderMails.Columns.AddRange(new DataColumn[1] {
                        new DataColumn("Email",typeof(string)) });
                        dtOrderMails.Rows.Add(PlacedByLoginId);
                        dtOrderMails.Rows.Add(FromAddress);
                        MemoryStream[] xmlMemoryStream = new MemoryStream[noOfStdnts];
                        string[] xmlContent = new string[noOfStdnts];
                        string stdfname = string.Empty;
                        string xmlfilename = string.Empty;

                        for (int i = 0; i < noOfStdnts; i++)
                        {
                            stdfname = Convert.ToString(dsOrderSummary.Tables[0].Rows[i]["stFirstName"]);
                            stdfname = stdfname.ToUpper().Trim().Replace(" ", "_");
                            xmlfilename = "OrderDetail_" + orderId.Substring(2, orderId.Length - 2) + "_" + stdfname + ".xml";  // remove BL from Orderid                            

                            CloudBlobDirectory BlobDirectory = container.GetDirectoryReference("XML");
                            var di = BlobDirectory.ListBlobs();
                            CloudBlockBlob cloudBlockBlob = container.GetBlockBlobReference($"XML/{xmlfilename}");

                            string Streamreader = "";
                            ContentType contentType = new ContentType();
                            using (var stream = cloudBlockBlob.OpenRead())
                            {
                                using (StreamReader stream_reader = new StreamReader(stream))
                                {
                                    stream.Seek(0, SeekOrigin.Begin);
                                    Streamreader = stream_reader.ReadToEnd();
                                }
                            }
                            contentType.Name = xmlfilename;
                            byte[] byteArray = Encoding.UTF8.GetBytes(Streamreader);
                            MemoryStream mStream = new MemoryStream(byteArray);
                            xmlMemoryStream[i] = mStream;
                            xmlContent[i] = contentType.Name.ToString();
                        }

                        //Using Parallel Multi-Threading send multiple bulk email.
                        Parallel.ForEach(dtOrderMails.AsEnumerable(), row =>
                        {
                            SendMails(seh, row["Email"].ToString(), _Body, bcc, schoolName, studentName, year, xmlMemoryStream, xmlContent, FromAddress);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Helper hbiz = new Helper();
                if (Session["UId"] != null)
                {
                    if (!string.IsNullOrEmpty(Session["UId"].ToString()))
                    {
                        hbiz.LogExceptiontoDB(ex, "MailConfrimation()| " + orderId + Convert.ToString(Session["UId"]));
                    }
                }
                else
                    hbiz.LogExceptiontoDB(ex, "MailConfrimation()| " + orderId + Convert.ToString(ShoppingCartID));
            }
        }
        protected void SendMails(SendEmailHelper seh, string toEmail, StringBuilder Body, string BCC, string schoolName, string studentName, string year, MemoryStream[] xmlstream, string[] xmlcontent, string FromAddress)
        {
            try
            {
                seh.SetOrderConfirmationMailWithAttachment(toEmail, Body, BCC, schoolName, studentName, year, FromAddress, xmlstream, xmlcontent);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        string OrderSummaryTable(string Label, string Price)
        {
            StringBuilder OrderSummary = new StringBuilder();
            try
            {
                OrderSummary.Append("<table class=MsoNormalTable border='0' cellspacing='0' cellpadding='0' width='100%' style='width:100.0%;mso-cellspacing:0cm;mso-yfti-tbllook:1184;mso-padding-alt:2.0pt 2.0pt 2.0pt 2.0pt' id=CheckoutCartGrid>" +
               "<tr style='mso-yfti-irow:0;mso-yfti-firstrow:yes;mso-yfti-lastrow:yes'><td style='padding:2.0pt 2.0pt 2.0pt 2.0pt'><p class=MsoNormal align=right style='text-align:right'><strong><span style='font-size:9.0pt;font-family:Arial,sans-serif'>" +
               "" + Label + ":</span></strong><span style='font-size:9.0pt;font-family:Arial,sans-serif'></span></p></td>" +
               "<td width='100' style='width:75.0pt;padding:2.0pt 2.0pt 2.0pt 2.0pt'><p class=MsoNormal align=right style='text-align:right'><strong>" +
               "<span style='font-size:9.0pt;font-family:Arial,sans-serif;'>" + Price + "</span></strong><span style='font-size:9.0pt;font-family:Arial,sans-serif'>" +
               "</span></p></td></tr></table>");
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return OrderSummary.ToString();
        }
        string paymentmethod()
        {
            string PaymentType = string.Empty;
            if (Session["TenderID"] != null)
            {
                if (!string.IsNullOrEmpty(Session["TenderID"].ToString()))
                {
                    if (Session["TenderID"].ToString() == "PayPal")
                    {
                        PaymentType = "PayPal";
                    }
                    if (Session["TenderID"].ToString() == "eWay")
                    {
                        PaymentType = "eWAY";
                    }
                    if (Session["TenderID"].ToString().ToLower().Contains("zip"))
                    {
                        PaymentType = Session["TenderID"].ToString();
                    }
                    else
                    {
                        if (Convert.ToInt32(hdDelMode.Value) == 3)
                        {
                            PaymentType = "Pay and collect at School";
                        }
                        if (Convert.ToInt32(hdDelMode.Value) == 5)
                        {
                            PaymentType = "Pay and collect in Store";
                        }
                    }
                }
            }
            return PaymentType;

        }
        string GenerateGridOrder(string Product, string Comment, string Quantity, string Price, bool HidePrice)
        {
            StringBuilder retval = new StringBuilder();
            try
            {
                string normalTDHeaderleft = "<td style='font-size:12px;font-family:Arial,sans-serif;padding:8px;line-height:1.428571429;vertical-align:top;text-align:left;border-bottom:1px solid #dddddd;'>";
                string normalTDHeadercenter = "<td style='font-size:12px;font-family:Arial,sans-serif;padding:8px;line-height:1.428571429;vertical-align:top;text-align:center;border-bottom:1px solid #dddddd;'>";
                string normalTDHeaderright = "<td style='font-size:12px;font-family:Arial,sans-serif;padding:8px;line-height:1.428571429;vertical-align:top;text-align:right;border-bottom:1px solid #dddddd;'>";

                retval.Append("<tr>");
                retval.Append(normalTDHeaderleft); //Product
                retval.Append("<b>" + Product + "</b>");
                retval.Append("<br/>");
                retval.Append("<span style='font-size:.85em;'>");
                retval.Append(Comment);
                retval.Append("</span>");
                retval.Append("</td>");
                retval.Append(normalTDHeadercenter); //Quantity
                retval.Append(Quantity);
                retval.Append("</td>");

                if (HidePrice == false)
                {
                    decimal QtyDecimal = Convert.ToDecimal(Quantity);
                    decimal TotalPrice = QtyDecimal * Convert.ToDecimal(Price);
                    string FinalPrice = String.Format("{0:c2}", TotalPrice);

                    retval.Append(normalTDHeaderright);
                    retval.Append(FinalPrice);          //Price
                    retval.Append("</td>");
                }
                if (HidePrice == true)
                {
                    retval.Append(normalTDHeaderright);
                    //HIDDEN PRICE COLUMN
                    retval.Append("</td>");
                }

                retval.Append("</tr>");
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return retval.ToString();
        }
        protected void CancelOr_Click(object sender, EventArgs e)
        {
            // Once Click on CancelOrder Delete respective rows in student and shooping cart table (i.e Sandhya 27/07/2017)
            try
            {
                if (Session["ShoppingCartID"] != null)
                {
                    if (!string.IsNullOrEmpty(Session["ShoppingCartID"].ToString()))
                    {
                        ShoppingCartID = Session["ShoppingCartID"].ToString();
                        if (!ShoppingCartID.Contains("Guest_"))
                        {
                            biz.DeleteStudentandShoopingCart(ShoppingCartID);
                        }
                        else
                        {
                            biz.DeleteStudentandShoopingCart(ShoppingCartID);
                        }
                        RouteValueDictionary Parameters =
                         new RouteValueDictionary
                         {
                                            {"store", URLKeyword }

                         };

                        VirtualPathData vpd =
                        RouteTable.Routes.GetVirtualPath(null, "rtStore", Parameters);
                        Response.RedirectToRoute("rtStore", Parameters);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private DataSet GetDealerEwayDetails(out string ErpCustID)
        {
            int DealerNum = 0;
            if (!string.IsNullOrEmpty((Convert.ToString(Session["SchoolID"]))))
            {
                SchoolBiz sbiz = new SchoolBiz();

                DataTable dt = sbiz.Get_School_Data(Convert.ToInt32(Session["SchoolID"]));
                DealerNum = Convert.ToInt32(dt.Rows[0]["DealerNo"]);
                ErpCustID = (!string.IsNullOrEmpty(Convert.ToString(dt.Rows[0]["ERPCustomerID"]))) ? Convert.ToString(dt.Rows[0]["ERPCustomerID"]) : string.Empty;
                PaymentBiz pbiz = new PaymentBiz();
                dealerPaymentDetails = pbiz.populate_Eway_Payment_Details(DealerNum);
            }
            else
            {
                ErpCustID = string.Empty;
            }
            return dealerPaymentDetails;
        }
        protected void GenerateXMLfile(string OrderID)
        {
            try
            {
                BBS(OrderID);
            }
            catch (Exception ex)
            {

                Helper hbiz = new Helper();
                if (Session["UId"] != null)
                {
                    if (!string.IsNullOrEmpty(Session["UId"].ToString()))
                    {
                        hbiz.LogExceptiontoDB(ex, Convert.ToString(Session["UId"]));
                    }
                }
                else
                    hbiz.LogExceptiontoDB(ex, Convert.ToString(ShoppingCartID));
            }
        }
        protected void BBS(string orderid)
        {
            string TransactionID = string.Empty;
            try
            {
                OrderBiz obiz = new OrderBiz();
                DataSet dtDelieverypacs = bkiz.BL_GET_DELIVERYCHARGES_BY_YEARID(orderid, 2); // Getting all booklist details for all years that is pass 0
                decimal StudentDelchargesex = 0m;
                decimal StudentDelcharges = 0m;
                decimal DelchargesforEachStudent = 0m;
                DataTable DtbooklistId = new DataTable();

                if (dtDelieverypacs.Tables.Count > 0)
                {
                    DtbooklistId = dtDelieverypacs.Tables[0];
                }

                if (dtDelieverypacs.Tables.Count > 1)
                {
                    DelchargesforEachStudent = (Decimal.Round(Convert.ToDecimal(dtDelieverypacs.Tables[1].Rows[0]["DirectDelivertCost"]), 4));
                }

                int stcount = 65; // Ascii value is 'A'
                for (int i = 0; i < DtbooklistId.Rows.Count; i++)
                {
                    StudentDelcharges = DelchargesforEachStudent / 11;
                    StudentDelchargesex = DelchargesforEachStudent - StudentDelcharges;
                    BBSIntegration(orderid, Convert.ToInt32(DtbooklistId.Rows[i]["Stid"].ToString()), StudentDelcharges, StudentDelchargesex, Convert.ToChar(stcount));
                    stcount++;
                }
            }
            catch (Exception ex)
            {
                Helper hbiz = new Helper();
                if (Session["UId"] != null)
                {
                    if (!string.IsNullOrEmpty(Session["UId"].ToString()))
                    {
                        hbiz.LogExceptiontoDB(ex, Convert.ToString(Session["UId"]));
                    }
                }
                else
                    hbiz.LogExceptiontoDB(ex, Convert.ToString(ShoppingCartID));
            }
        }
        protected void BBSIntegration(string OrderId, int Stid, decimal stdelcharge, decimal stdelchargeEX, char OrdSuffix)
        {
            try
            {
                List<OrderLines> OrderLines = new List<OrderLines>();
                DataSet ds = obiz.BL_GetOrderDetails_ForBBS(OrderId, Stid);
                if (ds != null)
                {
                    if (ds.Tables.Count == 7)
                    {
                        if (ds.Tables[0].Rows.Count > 0 && ds.Tables[1].Rows.Count > 0 && ds.Tables[2].Rows.Count > 0 &&
                            ds.Tables[3].Rows.Count > 0 && ds.Tables[4].Rows.Count > 0 && ds.Tables[5].Rows.Count > 0 &&
                            ds.Tables[6].Rows.Count > 0)
                        {
                            Report_NC_PurchaseOrder RNPO = new Report_NC_PurchaseOrder();
                            // Set Control Area
                            ControlArea CA = new ControlArea();
                            Verb v = new Verb();
                            v.value = "Report";
                            Noun n = new Noun();
                            n.value = "NC_PurchaseOrder";
                            CA.Verb = v;
                            CA.Noun = n;
                            RNPO.ControlArea = CA;
                            // Set DataArea
                            SetDataArea(RNPO, ds, stdelcharge, stdelchargeEX, Stid, OrdSuffix);
                        }
                    }
                    else
                    {
                        Helper hbiz = new Helper();

                        if (Session["UId"] != null)
                        {
                            if (!string.IsNullOrEmpty(Session["UId"].ToString()))
                            {
                                hbiz.LogExceptiontoDB(null, OrderId + " | " + Stid + " | BBSIntegration() " + Convert.ToString(Session["UId"]));
                            }
                        }
                        else
                            hbiz.LogExceptiontoDB(null, OrderId + " | " + Stid + " | BBSIntegration() " + Convert.ToString(ShoppingCartID));

                    }
                }
            }
            catch (Exception ex)
            {
                Helper hbiz = new Helper();
                if (Session["UId"] != null)
                {
                    if (!string.IsNullOrEmpty(Session["UId"].ToString()))
                    {
                        hbiz.LogExceptiontoDB(ex, OrderId + " | " + Stid + " | BBSIntegration() " + Convert.ToString(Session["UId"]));
                    }
                }
                else
                    hbiz.LogExceptiontoDB(ex, OrderId + " | " + Stid + " | BBSIntegration() " + Convert.ToString(ShoppingCartID));
            }
        }
        protected void SetDataArea(Report_NC_PurchaseOrder RNPO, DataSet ds, decimal stdelcharge, decimal stdelchargeEX, int stid, char OrderSuffix)
        {
            try
            {
                decimal Delivery = 0m, DeliveryEx = 0m, Latefee = 0m, EBD = 0m, IncGSTTotal = 0m, GSTFreeTotal = 0m, GSTTOTAL = 0m, GrandTotal = 0m;

                if (Convert.ToString(ds.Tables[0].Rows[0]["DeliveryMode"]) == "1")
                {
                    DeliveryEx = stdelchargeEX;
                    Delivery = stdelcharge;

                }

                Latefee = (Decimal.Round(Convert.ToDecimal(ds.Tables[2].Rows[0]["UnitPriceExTax"]), 4) + Decimal.Round(Convert.ToDecimal(ds.Tables[2].Rows[0]["UnitTax"]), 4)) * (Convert.ToInt32(ds.Tables[2].Rows[0]["UnitQuantityOrdered"]));
                EBD = (Decimal.Round(Convert.ToDecimal(ds.Tables[3].Rows[0]["UnitPriceExTax"]), 4) + Decimal.Round(Convert.ToDecimal(ds.Tables[3].Rows[0]["UnitTax"]), 4)) * (Convert.ToInt32(ds.Tables[3].Rows[0]["UnitQuantityOrdered"]));
                //Get Exclude product cost from DB
                //GSTFREETOTAL
                GSTFreeTotal = (Decimal.Round(Convert.ToDecimal(ds.Tables[6].Rows[0]["GSTFreeTotal"]), 4));
                //ProuctTotal -GSTFREETotal 
                IncGSTTotal = (Decimal.Round(Convert.ToDecimal(ds.Tables[6].Rows[0]["IncGSTTotal"]), 4));

                GrandTotal = IncGSTTotal + DeliveryEx + Delivery + EBD + Latefee + GSTFreeTotal;

                if (IncGSTTotal > 0)
                {
                    GSTTOTAL = (Decimal.Round(Convert.ToDecimal((GrandTotal - GSTFreeTotal) / 11), 4));
                    //this is the Excluded gst cost
                    IncGSTTotal = (Decimal.Round(Convert.ToDecimal((GrandTotal - GSTTOTAL)), 4));
                }
                else
                {
                    IncGSTTotal = (Decimal.Round(GrandTotal, 4));
                    GSTTOTAL = (Decimal.Round(Convert.ToDecimal(0.0), 4));
                }
                DataArea dArea = new DataArea();
                ReportPO rPO = new ReportPO();
                ReportPOHeader rptPOHeader = new ReportPOHeader();
                rptPOHeader.OrderNumberByNC = ds.Tables[0].Rows[0]["OrderNumber"].ToString().Trim() + OrderSuffix;
                DateTimeReference dtRef = new DateTimeReference();
                dtRef.PlacedDate = ds.Tables[0].Rows[0]["CreateDatePart"].ToString();
                dtRef.PlacedTime = ds.Tables[0].Rows[0]["CreatedTimePart"].ToString();
                rptPOHeader.DateTimeReference = dtRef;
                TotalPriceInfo tPriceInfo = new TotalPriceInfo();
                tPriceInfo.currency = "AUD";
                tPriceInfo.TotalNetPrice = IncGSTTotal.ToString();
                TaxInfo[] ti = new TaxInfo[1];
                MonetaryAmount mo = new MonetaryAmount();
                mo.Value = GSTTOTAL.ToString();
                mo.currency = "AUD";
                ti[0] = new TaxInfo();
                ti[0].MonetaryAmount = mo;
                tPriceInfo.TaxInfo = ti;
                tPriceInfo.OrderTotalPayable = GrandTotal.ToString();
                rptPOHeader.TotalPriceInfo = tPriceInfo;

                if (DeliveryEx > 0 && ds.Tables[4].Rows.Count > 0)
                {
                    decimal DelPrice = 0.0m, DelTaxPrice = 0.0m;
                    DelPrice = (Decimal.Round(DeliveryEx, 4)) * Convert.ToInt32(ds.Tables[4].Rows[0]["UnitQuantityOrdered"]);
                    tPriceInfo.TotalShippingPrice = DelPrice.ToString();
                    DelTaxPrice = Decimal.Round(Convert.ToDecimal(Delivery), 4);
                    tPriceInfo.TotalTaxOnShippingPrice = DelTaxPrice.ToString();
                }
                else
                {
                    tPriceInfo.TotalShippingPrice = "0.00";
                    tPriceInfo.TotalTaxOnShippingPrice = "0.00";
                }


                tPriceInfo.Discount = Convert.ToString(-EBD);
                rptPOHeader.TotalPriceInfo = tPriceInfo;
                rptPOHeader.ShipStatus = "M";
                MerchantInfo moi = new MerchantInfo();
                moi = SetMerchantInfo(ds);
                rptPOHeader.MerchantInfo = moi;
                RequisitionerInfo rif = new RequisitionerInfo();
                BuyOrgInfo bor = new BuyOrgInfo();
                bor = SetBuyOrgInfo(ds);
                rptPOHeader.BuyOrgInfo = bor;
                rif = SetRequisitionerInfo(ds, stid);
                rptPOHeader.RequisitionerInfo = rif;
                string[] OrdCust = new string[3] { string.Empty, string.Empty, string.Empty };
                rptPOHeader.OrderCustomerField = OrdCust;
                UserData ud = new UserData();
                rptPOHeader.UserData = SetUserData(ds);
                rPO.ReportPOHeader = rptPOHeader;
                dArea.ReportPO = rPO;
                //Getting no of products in for Student in order
                int noOfProducts = ds.Tables[1].Rows.Count;
                ReportPOItem[] rptPOItem = new ReportPOItem[noOfProducts];
                for (int i = 0; i < ds.Tables[1].Rows.Count; i++)
                {
                    rptPOItem[i] = new ReportPOItem();
                    rptPOItem[i].ItemLineNumber = i.ToString();
                    rptPOItem[i].ItemNumberByNC = Convert.ToString(ds.Tables[1].Rows[i]["p1"]);
                    rptPOItem[i].ProductNumberByMerchant = Convert.ToString(ds.Tables[1].Rows[i]["ProductID"]);
                    ItemUnitPrice iUnitPrice = new ItemUnitPrice();
                    iUnitPrice.currency = "AUD";
                    iUnitPrice.Value = Convert.ToString(ds.Tables[1].Rows[i]["UnitPriceExTax"]);
                    rptPOItem[i].ItemUnitPrice = iUnitPrice;
                    rptPOItem[i].ItemProductQuantity = Convert.ToString(ds.Tables[1].Rows[i]["UnitQuantityOrdered"]);
                    rptPOItem[i].UnitOfMeasure = Convert.ToString(ds.Tables[1].Rows[i]["ProductUnitCode"]);
                    rptPOItem[i].ItemProductDescription = Convert.ToString(ds.Tables[1].Rows[i]["ProductName"]);

                    //* Parent Details Begin */
                    ShipToInfo shpInfo = new ShipToInfo();
                    string FirstName = Convert.ToString(ds.Tables[5].Rows[0]["FName"]);
                    string LastName = Convert.ToString(ds.Tables[5].Rows[0]["LName"]);
                    ContactPersonName cntPerName = new ContactPersonName();
                    ItemsChoiceType[] iTypes = new ItemsChoiceType[] { ItemsChoiceType.LastName, ItemsChoiceType.FirstName, ItemsChoiceType.MiddleName };
                    string[] custItems = new string[] { LastName, FirstName, string.Empty };
                    cntPerName.ItemsElementName = iTypes;
                    cntPerName.Items = custItems;
                    cntPerName.AlternateName = LastName;
                    shpInfo.ContactPersonName = cntPerName;
                    string CustAddress = Convert.ToString(ds.Tables[0].Rows[0]["CustomerAddressStreet1"]);
                    OB455_Booklist.AppCode.Address custAddress = new OB455_Booklist.AppCode.Address();
                    string[] custAddrLine = new string[] { CustAddress, string.Empty, string.Empty };
                    custAddress.AddressLine = custAddrLine;
                    custAddress.City = Convert.ToString(ds.Tables[0].Rows[0]["CustomerAddressSuburbName"]);
                    custAddress.State = Convert.ToString(ds.Tables[0].Rows[0]["CustomerState"]);
                    custAddress.Zip = Convert.ToString(ds.Tables[0].Rows[0]["CustomerAddressPostCode"]);
                    custAddress.Country = "AU";
                    shpInfo.Address = custAddress;
                    ContactInfo custContactInfo = new ContactInfo();
                    Telephone[] custTel = new Telephone[2];
                    custTel[0] = new Telephone();
                    custTel[0].type = TelephoneType.primary;
                    custTel[0].Value = Convert.ToString(ds.Tables[0].Rows[0]["CustomerPhone"]);
                    custTel[1] = new Telephone();
                    custTel[1].type = TelephoneType.secondary;
                    custTel[1].Value = string.Empty;
                    custContactInfo.Telephone = custTel;
                    Email[] custEmail = new Email[2];
                    custEmail[0] = new Email();
                    custEmail[0].type = EmailType.primary;
                    custEmail[0].Value = Convert.ToString(ds.Tables[0].Rows[0]["CustomerEmailAddress"]);
                    custEmail[1] = new Email();
                    custEmail[1].type = EmailType.secondary;
                    custEmail[1].Value = string.Empty;
                    custContactInfo.Email = custEmail;
                    custContactInfo.Fax = string.Empty;
                    shpInfo.ContactInfo = custContactInfo;
                    shpInfo.Comment = Convert.ToString(ds.Tables[0].Rows[0]["ShippingComment"]);
                    rptPOItem[i].ShipToInfo = shpInfo;
                    ShippingCarrierInfo custshpinto = new ShippingCarrierInfo();
                    custshpinto.Carrier = "Mail";
                    custshpinto.Method = "Mail";
                    rptPOItem[i].ShippingCarrierInfo = custshpinto;
                    rptPOItem[i].ShipStatus = "M";
                    DateTimeReference custDtred = new DateTimeReference();
                    custDtred.PlacedDate = ds.Tables[0].Rows[0]["CreateDatePart"].ToString();
                    custDtred.PlacedTime = ds.Tables[0].Rows[0]["CreatedTimePart"].ToString();
                    custDtred.LastUpdateDate = ds.Tables[0].Rows[0]["ModifiedDatePart"].ToString();
                    custDtred.LastUpdateTime = ds.Tables[0].Rows[0]["ModifiedtimePart"].ToString();
                    rptPOItem[i].DateTimeReference = custDtred;
                    //* Parent Details End */
                    UserDataField[] custUserDatafield = new UserDataField[1];
                    custUserDatafield[0] = new UserDataField();
                    custUserDatafield[0].name = "ItemTaxAmount";
                    custUserDatafield[0].Value = Convert.ToString(ds.Tables[1].Rows[i]["UnitTax"]);
                    rptPOItem[i].UserData = custUserDatafield;
                }
                rPO.ReportPOItem = rptPOItem;
                RNPO.DataArea = dArea;
                dArea.ReportPO = rPO;
                ExportXML(RNPO, ds.Tables[0].Rows[0]["OrderNumber"].ToString(), ds.Tables[0].Rows[0]["StFname"].ToString());
            }
            catch (Exception ex)
            {
                Helper hbiz = new Helper();
                if (Session["UId"] != null)
                {
                    if (!string.IsNullOrEmpty(Session["UId"].ToString()))
                    {
                        hbiz.LogExceptiontoDB(ex, stid + " | SetDataArea() " + Convert.ToString(Session["UId"]));
                    }
                }
                else
                    hbiz.LogExceptiontoDB(ex, stid + " | SetDataArea() " + Convert.ToString(ShoppingCartID));
            }
        }
        protected MerchantInfo SetMerchantInfo(DataSet ds)
        {
            try
            {
                MerchantInfo mio = new MerchantInfo();
                mio.OrgName = Convert.ToString(ds.Tables[0].Rows[0]["DealerName"]);
                OrgID[] orgId = new OrgID[1];
                orgId[0] = new OrgID();
                orgId[0].type = "NCInternal";
                orgId[0].Value = Convert.ToString(ds.Tables[0].Rows[0]["DealereStoreID"]);
                mio.OrgID = orgId;
                OB455_Booklist.AppCode.Address addr = new OB455_Booklist.AppCode.Address();
                string[] stAddrline = new string[] { Convert.ToString(ds.Tables[0].Rows[0]["DealerAddress"]), string.Empty };
                addr.AddressLine = stAddrline;
                addr.City = Convert.ToString(ds.Tables[0].Rows[0]["DealerCity"]);
                addr.State = Convert.ToString(ds.Tables[0].Rows[0]["DealerState"]);
                addr.Zip = Convert.ToString(ds.Tables[0].Rows[0]["DealerPostcode"]);
                addr.Country = Convert.ToString(ds.Tables[0].Rows[0]["DealerCountry"]);
                mio.Address = addr;
                ContactInfo cinf = new ContactInfo();
                Telephone[] tf = new Telephone[1];
                tf[0] = new Telephone();
                tf[0].type = TelephoneType.primary;
                tf[0].Value = Convert.ToString(ds.Tables[0].Rows[0]["DealerPhone"]);
                cinf.Telephone = tf;
                Email[] email = new Email[1];
                email[0] = new Email();
                email[0].type = EmailType.primary;
                email[0].Value = Convert.ToString(ds.Tables[0].Rows[0]["DealerEmail"]);
                cinf.Email = email;
                cinf.Fax = Convert.ToString(ds.Tables[0].Rows[0]["DealerFax"]);
                mio.ContactInfo = cinf;
                return mio;
            }
            catch (Exception ex)
            {
                Helper hbiz = new Helper();
                if (Session["UId"] != null)
                {
                    if (!string.IsNullOrEmpty(Session["UId"].ToString()))
                    {
                        hbiz.LogExceptiontoDB(ex, "SetMerchantInfo() " + Convert.ToString(Session["UId"]));
                    }
                }
                else
                    hbiz.LogExceptiontoDB(ex, "SetMerchantInfo() " + Convert.ToString(ShoppingCartID));
                return null;
            }
        }
        protected BuyOrgInfo SetBuyOrgInfo(DataSet ds)
        {
            try
            {
                BuyOrgInfo boi = new BuyOrgInfo();
                string ERPCustomerID = string.Empty;
                string orgName = string.Empty;

                if (!ds.Tables[0].Rows[0].IsNull("CustomerID"))
                {
                    ERPCustomerID = Convert.ToString(ds.Tables[0].Rows[0]["CustomerID"]);
                }


                string buyOrgInfoName = Convert.ToString(ds.Tables[0].Rows[0]["BuyOrgInfoName"]);
                if (string.IsNullOrEmpty(ERPCustomerID))
                {
                    orgName = buyOrgInfoName.ToUpper();
                }
                else
                {
                    orgName = ERPCustomerID.ToUpper() + " - " + buyOrgInfoName.ToUpper();
                }

                boi.OrgName = orgName;

                return boi;
            }
            catch (Exception ex)
            {
                Helper hbiz = new Helper();
                if (Session["UId"] != null)
                {
                    if (!string.IsNullOrEmpty(Session["UId"].ToString()))
                    {
                        hbiz.LogExceptiontoDB(ex, "SetBuyOrgInfo() " + Convert.ToString(Session["UId"]));
                    }
                }
                else
                    hbiz.LogExceptiontoDB(ex, "SetBuyOrgInfo() " + Convert.ToString(ShoppingCartID));
                return null;
            }
        }

        protected RequisitionerInfo SetRequisitionerInfo(DataSet ds, int stid)
        {
            try
            {
                RequisitionerInfo ri = new RequisitionerInfo();
                RequisitionerID[] reqId = new RequisitionerID[2];
                reqId[0] = new RequisitionerID();
                reqId[0].type = RequisitionerIDType.NCInternal;
                reqId[0].Value = stid.ToString();
                reqId[1] = new RequisitionerID();
                reqId[1].type = RequisitionerIDType.logon;
                reqId[1].Value = Convert.ToString(ds.Tables[5].Rows[0]["RequisitionerGuestorReg"]);
                ri.RequisitionerID = reqId;

                OB455_Booklist.AppCode.Address addr = new OB455_Booklist.AppCode.Address();
                string[] stAddrline = new string[] { Convert.ToString(ds.Tables[0].Rows[0]["CustomerAddressStreet1"]), string.Empty };
                addr.AddressLine = stAddrline;
                addr.City = Convert.ToString(ds.Tables[0].Rows[0]["CustomerAddressSuburbName"]);
                addr.State = Convert.ToString(ds.Tables[0].Rows[0]["CustomerState"]);
                addr.Zip = Convert.ToString(ds.Tables[0].Rows[0]["CustomerAddressPostCode"]);
                addr.Country = Convert.ToString(ds.Tables[0].Rows[0]["DealerCountry"]);
                ri.Address = addr;

                ContactInfo cinf = new ContactInfo();
                Telephone[] tf = new Telephone[1];
                tf[0] = new Telephone();
                tf[0].type = TelephoneType.primary;
                tf[0].Value = Convert.ToString(ds.Tables[0].Rows[0]["CustomerPhone"]);
                cinf.Telephone = tf;
                Email[] email = new Email[1];
                email[0] = new Email();
                email[0].type = EmailType.primary;
                email[0].Value = Convert.ToString(ds.Tables[0].Rows[0]["CustomerEmailAddress"]);
                cinf.Email = email;
                cinf.Fax = "";
                ri.ContactInfo = cinf;


                return ri;
            }
            catch (Exception ex)
            {
                Helper hbiz = new Helper();
                if (Session["UId"] != null)
                {
                    if (!string.IsNullOrEmpty(Session["UId"].ToString()))
                    {
                        hbiz.LogExceptiontoDB(ex, stid + " | SetRequisitionerInfo() " + Convert.ToString(Session["UId"]));
                    }
                }
                else
                    hbiz.LogExceptiontoDB(ex, stid + " SetRequisitionerInfo() " + Convert.ToString(ShoppingCartID));
                return null;
            }
        }
        protected PCardInfo SetPCardInfo(DataSet ds)
        {
            try
            {
                PCardInfo pi = new PCardInfo();
                MonetaryAmount monamt = new MonetaryAmount();
                monamt.currency = "AUD";
                monamt.Value = "0.0000";
                pi.MonetaryAmount = monamt;
                pi.CardType = "XXXXX";
                pi.CardNumber = "1111111111111111";
                pi.ExpirationDate = "2017/11/13 00:00:00 PM";
                return pi;
            }
            catch (Exception ex)
            {
                Helper hbiz = new Helper();
                if (Session["UId"] != null)
                {
                    if (!string.IsNullOrEmpty(Session["UId"].ToString()))
                    {
                        hbiz.LogExceptiontoDB(ex, "SetPCardInfo() " + Convert.ToString(Session["UId"]));
                    }
                }
                else
                    hbiz.LogExceptiontoDB(ex, "SetPCardInfo() " + Convert.ToString(ShoppingCartID));
                return null;
            }
        }
        protected UserDataField[] SetUserData(DataSet ds)
        {
            try
            {
                UserDataField[] ud = new UserDataField[8];
                ud[0] = new UserDataField();
                ud[0].name = "OrderDescription";
                if (Convert.ToString(ds.Tables[0].Rows[0]["paymentTransactionID"]) != "0")
                {
                    ud[0].Value = Convert.ToString(ds.Tables[0].Rows[0]["paymentTransactionID"]);
                }
                else
                {
                    ud[0].Value = string.Empty;
                }
                ud[1] = new UserDataField();
                ud[1].name = "CostCentre";
                ud[1].Value = string.Empty;
                ud[2] = new UserDataField();
                ud[2].name = "ShipInstruction";
                ud[2].Value = Convert.ToString(ds.Tables[0].Rows[0]["ExternalNotes"]);
                ud[3] = new UserDataField();
                ud[3].name = "PurchaseOrder";
                ud[3].Value = string.Empty;
                ud[4] = new UserDataField();
                ud[4].name = "DeliveryStoreId";
                ud[4].Value = Convert.ToString(ds.Tables[0].Rows[0]["DealereStoreID"]);
                ud[5] = new UserDataField();
                ud[5].name = "SiteId";
                if (ds.Tables[0].Rows[0]["SITEID"] != null)
                {
                    ud[5].Value = Convert.ToString(ds.Tables[0].Rows[0]["SITEID"]);
                }

                ud[6] = new UserDataField();
                ud[6].name = "PaymentMethod";
                ud[6].Value = paymentmethod();

                ud[7] = new UserDataField();
                ud[7].name = "CustomStudentId";
                if (ds.Tables[0].Rows[0]["CustomStudentId"] != null)
                {
                    ud[7].Value = Convert.ToString(ds.Tables[0].Rows[0]["CustomStudentId"]);
                }


                return ud;
            }
            catch (Exception ex)
            {
                Helper hbiz = new Helper();
                if (Session["UId"] != null)
                {
                    if (!string.IsNullOrEmpty(Session["UId"].ToString()))
                    {
                        hbiz.LogExceptiontoDB(ex, "SetUserData() " + Convert.ToString(Session["UId"]));
                    }
                }
                else
                    hbiz.LogExceptiontoDB(ex, "SetUserData() " + Convert.ToString(ShoppingCartID));
                return null;
            }
        }
        protected void ExportXML(Report_NC_PurchaseOrder rnpo, string OrderId, string stfname)
        {
            string AzureConn = ConfigurationManager.ConnectionStrings["AzureStorageAccount"].ConnectionString;
            string AzureResource = ConfigurationManager.AppSettings["AzureResource"];
            CloudStorageAccount sa = CloudStorageAccount.Parse(AzureConn);
            CloudBlobClient bd = sa.CreateCloudBlobClient();
            CloudBlobContainer container = bd.GetContainerReference(AzureResource);
            container.CreateIfNotExists(BlobContainerPublicAccessType.Blob);

            try
            {
                stfname = stfname.Trim().Replace(" ", "_");
                string filename = "OrderDetail_" + OrderId + "_" + stfname + ".xml";
                XmlSerializer serializer =
                new XmlSerializer(typeof(Report_NC_PurchaseOrder));
                string FolderPath = container.Uri.ToString() + "/XML" + "/";
                string FilePath = FolderPath + filename;
                CloudBlobDirectory BlobDirectory = container.GetDirectoryReference("XML");
                var di = BlobDirectory.ListBlobs();
                CloudBlockBlob cloudBlockBlob = container.GetBlockBlobReference($"XML/{filename}");
                XmlWriterSettings settings = new XmlWriterSettings()
                {
                    OmitXmlDeclaration = false,
                    Indent = true,
                    Encoding = Encoding.UTF8
                };
                XmlSerializerNamespaces xns = new XmlSerializerNamespaces();
                xns.Add(string.Empty, string.Empty);
                string dealerERPSystem = string.Empty;

                if (Session["ERPSystem"] != null)
                {
                    dealerERPSystem = Session["ERPSystem"].ToString();
                }

                if (!string.IsNullOrEmpty(dealerERPSystem) && dealerERPSystem.Equals("CLEAR"))
                {
                    xns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                    xns.Add("OBWEBORDER", "urn:Booklist v2.0");
                }
                else
                {
                    xns.Add(string.Empty, string.Empty);
                }

                var Stream = new MemoryStream();
                XmlWriter xWriter = XmlWriter.Create(Stream, settings);
                xWriter.WriteStartDocument(false);
                xWriter.WriteDocType("Report_NC_PurchaseOrder", null, "Report_NC_PO_10.dtd", null);
                serializer.Serialize(xWriter, rnpo, xns);
                StreamReader stream_reader = new StreamReader(Stream);
                Stream.Seek(0, SeekOrigin.Begin);
                string xmlString = stream_reader.ReadToEnd();
                xmlString = xmlString.Replace(":OBWEBORDER", "");
                xmlString = xmlString.Replace(":OBWEBORDER", "");
                using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlString)))
                {
                    cloudBlockBlob.UploadFromStream(stream);
                }

                xWriter.Close();
            }
            catch (Exception ex)
            {
                Helper hbiz = new Helper();
                if (Session["UId"] != null)
                {
                    if (!string.IsNullOrEmpty(Session["UId"].ToString()))
                    {
                        hbiz.LogExceptiontoDB(ex, OrderId + " | " + Convert.ToString(Session["UId"]));
                    }
                }
                else
                    hbiz.LogExceptiontoDB(ex, OrderId + " | " + Convert.ToString(ShoppingCartID));
            }
        }
        static string UppercaseFirst(string value)
        {
            char[] array = value.ToCharArray();
            // Handle the first letter in the string.
            if (array.Length >= 1)
            {
                if (char.IsLower(array[0]))
                {
                    array[0] = char.ToUpper(array[0]);
                }
            }
            // Scan through the letters, checking for spaces.
            // ... Uppercase the lowercase letters following spaces.
            for (int i = 1; i < array.Length; i++)
            {
                if (array[i - 1] == ' ')
                {
                    if (char.IsLower(array[i]))
                    {
                        array[i] = char.ToUpper(array[i]);
                    }
                }
            }
            return new string(array);
        }
    }
}