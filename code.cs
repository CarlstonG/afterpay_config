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

         var data = new
         {
             amount = new { amount = "100.00", currency = "AUD" },
             consumer = new { email = "john.doe@example.com", givenNames = "John", surname = "Doe", phoneNumber = "1234567890" },
             billing = new { name = "John Doe", line1 = "123 Main St", city = "Sample City", postcode = "12345", countryCode = "AU" },
             shipping = new { name = "John Doe", line1 = "123 Main St", city = "Sample City", postcode = "12345", countryCode = "AU" },
             merchant = new
             {
               //  redirectConfirmUrl = "http://localhost:64212/paperchase/Order/Payment/Success",
                // redirectCancelUrl = "http://localhost:64212/paperchase/Order/Payment/Cancel"
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


  /*Start Afterpay logic*/
 try
 {
     // Clear any previous error messages and show loader
     lblPMErrors.Text = string.Empty;
     btnSubmitOrder.Style.Add("display", "none");
     BtnLoader.Style.Add("display", "block");
     lblPMErrors.Style.Add("display", "none");

     // Bypass other processes and focus on testing the Afterpay API call
     string response = await CreateAfterpayCharge();

     // Log the response in the console for testing
     Console.WriteLine("API Response: " + response);

     // Display the API response on the webpage (optional)
     lblPMErrors.Text = "API Response: " + response;
     lblPMErrors.Style.Add("display", "block");

     // Optionally, you can also deserialize the response to check its structure
     dynamic jsonResponse = JsonConvert.DeserializeObject(response);

     if (jsonResponse != null && jsonResponse.redirectCheckoutUrl != null)
     {
         // Log the redirect URL
         Console.WriteLine("Redirect URL: " + jsonResponse.redirectCheckoutUrl.ToString());

         // Optionally display the URL on the webpage
         lblPMErrors.Text += "<br/>Redirect URL: " + jsonResponse.redirectCheckoutUrl.ToString();
     }
     else
     {
         lblPMErrors.Text += "<br/>Error: Invalid response from Afterpay.";
     }
 }
 catch (Exception ex)
 {
     // Handle any errors and display them
     lblPMErrors.Text = "An error occurred: " + ex.Message;
     lblPMErrors.Style.Add("display", "block");
 }
 finally
 {
     // Restore the button and hide the loader after processing
     btnSubmitOrder.Style.Add("display", "block");
     BtnLoader.Style.Add("display", "none");
 }

 /*End Afterpay logic*/


 private async Task HandleAfterpayLogic()
{
    try
    {
        string response = await CreateAfterpayCharge();
        dynamic jsonResponse = JsonConvert.DeserializeObject(response);

        if (jsonResponse != null && jsonResponse.redirectConfirmUrl != null)
        {
            // Redirect to Afterpay confirmation page
            HttpContext.Current.Response.Redirect(jsonResponse.redirectConfirmUrl.ToString());
        }
        else
        {
            HttpContext.Current.Session["PMErrors"] = "Error creating Afterpay checkout.";
        }
    }
    catch (Exception ex)
    {
        HttpContext.Current.Session["PMErrors"] = "An error occurred.";
    }
}




// 
function RadioCheck(s, e) {
     document.getElementById('<%= lblPMErrors.ClientID %>').style.display = "none";
    //Hide and Show Payment based on Value Done By Sandhya i.e on 19/09/2017
    var RadioButtonvariable = s.name;
   // console.log(s.name);
    if (RadioButtonvariable == "MainContent_RB1" || RadioButtonvariable == "MainContent_RB2") {
        if (s.GetChecked() == true) {
            //alert("RadioButton1 checked");
            var HidepaymentDiv = document.getElementById('<%= showhideCreditCard.ClientID %>');
            HidepaymentDiv.style.display = 'block';

            var showPaypallabel = document.getElementById('<%= showhidePaypal.ClientID %>');
            showPaypallabel.style.display = 'none';
             var showZiplabel = document.getElementById('<%= showhideZip.ClientID %>');
            showZiplabel.style.display = 'none';
        }
    }

    if (RadioButtonvariable == "MainContent_RB3")
    {
         if (s.GetChecked() == true) {
            //alert("RadioButton1 checked");
            var HidepaymentDiv = document.getElementById('<%= showhideCreditCard.ClientID %>');
             HidepaymentDiv.style.display = 'none';
             var showPaypallabel = document.getElementById('<%= showhidePaypal.ClientID %>');
             showPaypallabel.style.display = 'block';
               var showZiplabel = document.getElementById('<%= showhideZip.ClientID %>');
            showZiplabel.style.display = 'none';
        }
    }


     if (RadioButtonvariable == "MainContent_RB4")
    {
         if (s.GetChecked() == true) {
            //alert("RadioButton1 checked");
            var HidepaymentDiv = document.getElementById('<%= showhideCreditCard.ClientID %>');
             HidepaymentDiv.style.display = 'none';
             var showPaypallabel = document.getElementById('<%= showhidePaypal.ClientID %>');
             showPaypallabel.style.display = 'none';
               var showZiplabel = document.getElementById('<%= showhideZip.ClientID %>');
            showZiplabel.style.display = 'block';
        }
    }

  //Ends Hide and Show Payment based on Value Done By Sandhya i.e on 19/09/2017
} 


// logic
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

//  code

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
