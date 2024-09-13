<%@ Page Title="Payment Options" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Payment.aspx.cs" Inherits="OB455_Booklist.Payment" %>
<%@ Register Assembly="Recaptcha.Web" Namespace="Recaptcha.Web.UI.Controls" TagPrefix="cc1" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">


    <script>
        <%--function OnValueChanged()
        {
           
            var checkedst = document.getElementById("<%= chktermuse.ClientID %>").checked;
            
            if (checkedst == true)
            {
                btnSubmitOrder.SetEnabled(true);
            }
            else
            {
                btnSubmitOrder.SetEnabled(false);
            }
        }--%>

      
    
        function validateForm() {
            document.getElementById('<%= lblPMErrors.ClientID %>').style.display = "none";
            var hddelval = document.getElementById("<%= hdDelMode.ClientID %>").value;
            //console.log('hddelval= ' + hddelval);
            if (hddelval == "1" || hddelval == "2" || hddelval == "4") {
                if (RB3.GetChecked() == false && RB4.GetChecked() == false) {
                    if (Page_ClientValidate()) {
                        var ccdata = document.getElementById("<%= cardnumber.ClientID %>").value;
                        var cvdata = document.getElementById("<%= cvv2.ClientID %>").value;

                        if ((ccdata == null || ccdata == "") || (cvdata == null || cvdata == "")) {
                            document.getElementById("<%= lblCCError.ClientID %>").innerHTML = "Please enter the credit card & cvv number";
                            document.getElementById('<%= btnSubmitOrder.ClientID %>').style.display = "block";
                            document.getElementById('<%= BtnLoader.ClientID %>').style.display = "none";

                            document.getElementById('<%= bakCheckout.ClientID %>').style.display = "block";
                            document.getElementById('<%= canOrderanch.ClientID %>').style.display = "block";

                            return false;
                        }
                        else {
                            document.getElementById('<%= btnSubmitOrder.ClientID %>').style.display = "none";
                            document.getElementById('<%= BtnLoader.ClientID %>').style.display = "block";

                            
                            document.getElementById('<%= bakCheckout.ClientID %>').style.display = "none";
                            document.getElementById('<%= canOrderanch.ClientID %>').style.display = "none";
                            return true;
                        }
                    }
                    else {
                        document.getElementById('<%= btnSubmitOrder.ClientID %>').style.display = "block";
                        document.getElementById('<%= BtnLoader.ClientID %>').style.display = "none";

                        
                        document.getElementById('<%= bakCheckout.ClientID %>').style.display = "block";
                        document.getElementById('<%= canOrderanch.ClientID %>').style.display = "block";
                        return false;
                    }
                }
                else {
                    document.getElementById('<%= btnSubmitOrder.ClientID %>').style.display = "none";
                    document.getElementById('<%= BtnLoader.ClientID %>').style.display = "block";

                    document.getElementById('<%= bakCheckout.ClientID %>').style.display = "none";
                    document.getElementById('<%= canOrderanch.ClientID %>').style.display = "none";
                    return true;
                }
            }
            else
            {
                 document.getElementById('<%= btnSubmitOrder.ClientID %>').style.display = "none";
                    document.getElementById('<%= BtnLoader.ClientID %>').style.display = "block";
                    return true;
            }
        }



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
                     var hideAfterpay = document.getElementById('<%= showAfterpay.ClientID %>');
                    hideAfterpay.style.display = 'none';
                }
            }
            //Afterpay
            if (RadioButtonvariable == "MainContent_RB5"){
                 if (s.GetChecked() == true) {
                    //alert("RadioButton1 checked");
                    var HidepaymentDiv = document.getElementById('<%= showhideCreditCard.ClientID %>');
                     HidepaymentDiv.style.display = 'none';
                     var showPaypallabel = document.getElementById('<%= showhidePaypal.ClientID %>');
                     showPaypallabel.style.display = 'none';
                     var hideAfterpay = document.getElementById('<%= showAfterpay.ClientID %>');
                    hideAfterpay.style.display = 'block';
                    var showZiplabel = document.getElementById('<%= showhideZip.ClientID %>');
                    showZiplabel.style.display = 'none';
                  
                }
            }
               
          
          //Ends Hide and Show Payment based on Value Done By Sandhya i.e on 19/09/2017
        } 


    </script>
    <style>
        #BtnLoader {
  position: relative;
  padding: 15px 40px;
  text-align: center;
}

    </style>

    <div id="SchoolHeader" class="container">
        <div class="row">
            <h1>Choose your payment</h1>
            <p>Please complete the payment steps to complete your order.</p>
        </div>
        <div class="row">
            <p>
                <br />
                <asp:Label ID="lblPMErrors" runat="server" style="display:none" CssClass="alert alert-danger text-center" />
            </p>
        </div>
    </div>
    <div id="SchoolContent">
        <div class="container content">
            <div class="row">
                <div class="col-lg-8">
                    <%-- Edit button to open checkout page with all values loaded--%>
                    <div class="row">
                        <div class="col-lg-6">
                            <h4>Your Details - <a id="edtDetails" runat="server">EDIT</a></h4>
                            <p>
                                <asp:Label ID="lblName" runat="server" Text=""></asp:Label>
                                <asp:Label ID="lblMobile" runat="server" Text=""></asp:Label>
                                <asp:Label ID="lblEmail" runat="server" Text=""></asp:Label>
                                <asp:Label ID="lblAddress" runat="server" Text=""></asp:Label>
                                <asp:Label ID="lblSuburb" runat="server" Text=""></asp:Label>
                                <asp:Label ID="lblPostCode" runat="server" Text=""></asp:Label>
                                <asp:Label ID="lblState" runat="server" Text=""></asp:Label>
                                <asp:HiddenField ID="hdDelMode" runat="server" />
                                <asp:HiddenField ID="hdModeName" runat="server" />
                            </p>
                        </div>
                        <div class="col-lg-6">
                            <h4>Delivery / Collection Details - <a id="edtDeliveryDetails" runat="server">CHANGE</a></h4>
                            <p>
                                <asp:Label ID="lblDelname" runat="server" Text=""></asp:Label>
                                <asp:Label ID="lblSchoolorStoreName" runat="server" Text=""></asp:Label>
                                <asp:Label ID="lblDelAddr" runat="server" Text=""></asp:Label>
                                <asp:Label ID="lblDelSuburb" runat="server" Text=""></asp:Label>
                                <asp:Label ID="lblDelPostCode" runat="server" Text=""></asp:Label>
                                <asp:Label ID="lblDelState" runat="server" Text=""></asp:Label>

                            </p>
                        </div>
                    </div>
                  <div class="form-group">
                       <asp:Label runat="server" AssociatedControlID="orderNotes" CssClass="control-label">Order Instructions/Notes</asp:Label>
                       <asp:TextBox runat="server" ID="orderNotes" CssClass="form-control" TextMode="MultiLine" placeholder="E.g., Delivery to be left if the house is unattended" />
                   </div>
                    <div id="BILLINGDETAILSforpayment" runat="server">
                        <%-- Show/hide based on checkout selection --%>
                        <h3>Billing Method</h3>
                        <p>Please select your preferred payment option.</p>
                        <%-- PAYPAL UPDATES  --%>
                        <div class="form-group required">
                            <div class="row">
                                <div class="col-xs-5">
                                    <label>Payment Type</label>
                                    <table>
                                        <tr>
                                            <td style="vertical-align:middle;" id="opt1" runat="server">
                                                <dx:ASPxRadioButton ID="RB1" runat="server" GroupName="paymentOption" ClientInstanceName="RB1">
                                                    <ClientSideEvents CheckedChanged="RadioCheck" />
                                                </dx:ASPxRadioButton>                                   
                                            </td>
                                            <td style="padding:0 15px" id="opt11" runat="server">
                                                <%--<asp:Image ID="Image1" runat="server" ImageUrl="~/images/visa.png" /></td>--%>
                                                <asp:Image ID="Image1" runat="server" ImageUrl="https://officebrandsapps.blob.core.windows.net/booklist/images/visa.png" /></td>


                                      




                                            <td style="vertical-align:middle;padding-left:5px" id="opt2" runat="server">
                                                  <dx:ASPxRadioButton ID="RB2" runat="server" GroupName="paymentOption" ClientInstanceName="RB2">
                                                    <ClientSideEvents CheckedChanged="RadioCheck" />
                                                </dx:ASPxRadioButton> 
                                            </td>
                                            <%--<td style="padding:0 15px" id="opt22" runat="server"><asp:Image ID="Image2" runat="server" ImageUrl="~/images/mastercard.png" /></td>--%>
                                            <td style="padding:0 15px" id="opt22" runat="server"><asp:Image ID="Image2" runat="server" ImageUrl="https://officebrandsapps.blob.core.windows.net/booklist/images/mastercard.png" /></td>

                                           <td style="vertical-align:middle;padding-left:5px" id="opt3" runat="server">
                                              <dx:ASPxRadioButton ID="RB3" runat="server" GroupName="paymentOption" ClientInstanceName="RB3">
                                                    <ClientSideEvents CheckedChanged="RadioCheck" />
                                                </dx:ASPxRadioButton> 
                                            </td>
                                           <%-- <td style="padding:0 15px" id="opt33" runat="server"><asp:Image ID="Image3" runat="server" ImageUrl="~/images/paypal.png" /></td>--%>
                                             <td style="padding:0 15px" id="opt33" runat="server"><asp:Image ID="Image3" runat="server" ImageUrl="https://officebrandsapps.blob.core.windows.net/booklist/images/paypal.png" /></td>
                                             <td style="vertical-align:middle;padding-left:5px" id="opt4" runat="server">
                                              <dx:ASPxRadioButton ID="RB4" runat="server" GroupName="paymentOption" ClientInstanceName="RB4">
                                                    <ClientSideEvents CheckedChanged="RadioCheck" />
                                                </dx:ASPxRadioButton> 
                                            </td>
                                            <%--<td style="padding:0 15px" id="opt44" runat="server"><asp:Image ID="Image4" runat="server" ImageUrl="~/images/zip.png" /></td>--%>
                                            <td style="padding:0 15px" id="opt44" runat="server"><asp:Image ID="Image4" runat="server" ImageUrl="https://officebrandsapps.blob.core.windows.net/booklist/images/zip.png" /></td>


                                            <td style="vertical-align:middle;" id="opt5" runat="server">
                                                  <dx:ASPxRadioButton ID="RB5" runat="server" GroupName="paymentOption" ClientInstanceName="RB5">
                                                      <ClientSideEvents CheckedChanged="RadioCheck" />
                                                  </dx:ASPxRadioButton>                                   
                                                   </td>
                                              <td style="padding:0 15px" id="Td2" runat="server">
                                                <%--<afterpay />--%>
                                                <asp:Label ID="LabelVisa" runat="server" Text="Afterpay" Font-Bold="True" Font-Size="Large" style="border: 0.5px solid #999; padding: 5px; padding: 13px 8px" />
                                            </td>
                                        </tr>
                                    </table>
                                </div>
                            </div>
                        </div>

                        <%-- PAYPAL UPDATES /SHOW HIDE IF PAYPAL SELECTED  --%>
                        <div runat="server" id="showhidePaypal" style="display:none"> 
                            <hr />
                            <p class="blue-text">To complete your <b>PayPal</b> checkout, please click the Submit Order button. You will then be transferred to a secure <b>PayPal</b> payment portal.</p>
                        </div>

                           <%-- ZipPayment UPDATES /SHOW HIDE IF Xip SELECTED  --%>
                        <div runat="server" id="showhideZip" style="display:none"> 
                            <hr />
                            <p class="blue-text">Click Submit Order to be securely sent to zip payment where you will finalise your purchage.</p>
                        </div>

                        <div runat="server" id="showAfterpay" style="display:none"> 
                             <hr />
                             <p class="blue-text">Afterpay Payment System...</p>
                        </div>




                        <p class="text-danger">
                            <asp:Literal runat="server" ID="ErrorMessage" />
                        </p>
                        <div runat="server" id="showhideCreditCard">
                            <hr />
                        <div class="CreditCardDiv">
                        <div class="form-group required">
                            <label for="cardholdername">Card Holder Name</label>
                            <asp:TextBox runat="server" ID="cardholdername" CssClass="form-control" Text="" TextMode="SingleLine" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="cardholdername" CssClass="text-danger" ErrorMessage="Please enter the card holder's name." Display="Dynamic" />
                        </div>
                        <div class="form-group required">
                            <div class="row">
                                <div class="col-md-8">
                                    <label for="cardnumber">Card Number</label>
                                    <asp:TextBox runat="server" ID="cardnumber" CssClass="form-control" TextMode="SingleLine" MaxLength="50" data-eway-encrypt-name="EWAY_CARDNUMBER" />
                                    <asp:Label runat="server" ID="lblCCError" Text="" CssClass="text-danger"></asp:Label>
                                    <%--<asp:RequiredFieldValidator runat="server" ControlToValidate="cardnumber" CssClass="text-danger" ErrorMessage="Please enter the card number." Display="Dynamic" />--%>
                                </div>
                                <div class="col-md-4">
                                    <label>CVV2 Number</label>
                                    <asp:TextBox runat="server" ID="cvv2" MaxLength="3" CssClass="form-control" EnableViewState="false" TextMode="Password" data-eway-encrypt-name="EWAY_CARDCVN" />
                                    <%--<asp:RequiredFieldValidator runat="server" ControlToValidate="cvv2" CssClass="text-danger" ErrorMessage="Please enter the CVV2 number." Display="Dynamic" />--%>
                                    <a runat="server" href="#" data-toggle="modal" data-target="#ShowCreditCardImg">What is CVV2? </a>
                                </div>
                            </div>
                        </div>
                        <div class="form-group required">
                            <div class="row">
                                <div class="col-xs-4">
                                    <label>Expiry Month</label>
                                    <asp:DropDownList ID="Month" runat="server" CssClass="form-control">
                                        <asp:ListItem  Text="" Value="0"></asp:ListItem>
                                        <asp:ListItem  Text="01" Value="01"></asp:ListItem>
                                        <asp:ListItem Text="02" Value="02"></asp:ListItem>
                                        <asp:ListItem Text="03" Value="03">03</asp:ListItem>
                                        <asp:ListItem Text="04" Value="04">04</asp:ListItem>
                                        <asp:ListItem Text="05" Value="05">05</asp:ListItem>
                                        <asp:ListItem Text="06" Value="06">06</asp:ListItem>
                                        <asp:ListItem Text="07" Value="07">07</asp:ListItem>
                                        <asp:ListItem Text="08" Value="08">08</asp:ListItem>
                                        <asp:ListItem Text="09" Value="09">09</asp:ListItem>
                                        <asp:ListItem Text="10" Value="10">10</asp:ListItem>
                                        <asp:ListItem Text="11" Value="11">11</asp:ListItem>
                                        <asp:ListItem Text="12" Value="12">12</asp:ListItem>
                                    </asp:DropDownList>
                                    <asp:RequiredFieldValidator InitialValue="0" ID="Req_Month" Display="Dynamic"  runat="server" ControlToValidate="Month" Text="Please enter the expiry Month" ErrorMessage="ErrorMessage" CssClass="text-danger"></asp:RequiredFieldValidator>
                                </div>
                                <div class="col-xs-4">
                                    <label>Expiry Year</label>
                                    <asp:DropDownList ID="Year" runat="server" CssClass="form-control">
                                    </asp:DropDownList>
                                    <asp:RequiredFieldValidator InitialValue="0" ID="Req_ID" Display="Dynamic"  runat="server" ControlToValidate="Year" Text="Please enter the expiry year" ErrorMessage="ErrorMessage" CssClass="text-danger"></asp:RequiredFieldValidator>
                                </div>
                            </div>
                        </div>
                        </div>
                        <div class="form-group required">
                            <label for="exampleInputPassword1">Billing Address</label>

                            <asp:TextBox runat="server" ID="BillingAddress" CssClass="form-control" TextMode="SingleLine" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="BillingAddress" CssClass="text-danger" ErrorMessage="Please enter the billing address." Display="Dynamic" />
                        </div>
                        <div class="form-group required">
                            <div class="row">
                                <div class="col-xs-4">
                                    <label>Suburb</label>
                                    <asp:TextBox runat="server" ID="BillingSuburb" CssClass="form-control" TextMode="SingleLine" />
                                    <asp:RequiredFieldValidator runat="server" ControlToValidate="BillingSuburb" CssClass="text-danger" ErrorMessage="Please enter the billing suburb." Display="Dynamic" />
                                </div>
                                <div class="col-xs-4">
                                    <label>Postcode</label>
                                    <asp:TextBox runat="server" ID="BillingPostcode" CssClass="form-control" TextMode="SingleLine" MaxLength="4" />
                                    <asp:RequiredFieldValidator runat="server" ControlToValidate="BillingPostcode" CssClass="text-danger" ErrorMessage="Please enter the billing postcode." Display="Dynamic" />
                                    <%-- <asp:RegularExpressionValidator runat="server" ControlToValidate="BillingPostcode" ValidationExpression="^[0-9]{4}$" ErrorMessage="Please enter a vaild postcode." CssClass="text-danger"></asp:RegularExpressionValidator>                                  --%>
                                </div>
                                <div class="col-xs-4">
                                    <label>State</label>
                                    <asp:DropDownList ID="ddlState" runat="server" DataTextField="State" DataValueField="ID" DataSourceID="odsState" CssClass="form-control">
                                    </asp:DropDownList>
                                    <asp:ObjectDataSource ID="odsState" SelectMethod="Populate_States" runat="server" TypeName="OB455_Booklist.Biz.ProductBiz" OldValuesParameterFormatString="original_{0}"></asp:ObjectDataSource>
                                </div>
                            </div>
                        </div>
                        </div>

                    </div>

                    <div id="BILLINGDETAILSforpayinstore" runat="server" visible="false" class="alert alert-info">
                        <h3>Payment Instore</h3>
                        <p>You have selected to pay instore and will be required to make the final payment to receive your ordered items upon collection. You will receive notification once your order is ready for collection.</p>
                    </div>

                    <div id="BILLINGDETAILSforpayinschool" runat="server" visible="false" class="alert alert-info">
                        <h3>Payment at School</h3>
                        <p>You have selected to pay on collection day at your selected school and will be required to make the final payment to receive your ordered items. The school will contact you with further details regarding collection day.</p>
                    </div>

                </div>
                <div class="col-lg-4">
                    <div class="SchoolDetailsDiv" style="border-color: #efefef;">
                        <p>Please review all your details before submitting your order. Depending on the type of collection option you have selected, you may receive further instructions via email.</p>
                        <p>Your Final Order summary can be found below. <span style="line-height: normal; margin-top: 8px; display: block; font-size: .85em; color: #999;"><strong>Please note:</strong> If you cancel this order, it will remove all record and you will be required to place another order.</span></p>
                    </div>
                    <div class="greyBox">
                        <h3>Final Order Summary</h3>

                        <%-- Update to use repeater. --%>

                        <table class="table">
                            <asp:Repeater ID="Repeater1" runat="server">
                                <HeaderTemplate></HeaderTemplate>
                                <ItemTemplate>
                                    <tr class="blue-txt">
                                        <td>
                                            <asp:Label runat="server" ID="lblProduct" Text='<%# Eval("Name") %>'></asp:Label>
                                        </td>
                                        <td style="text-align: right;">
                                            <asp:Label runat="server" ID="lblQty" Text='<%# Eval("Total","{0:c2}") %>' CssClass=""></asp:Label>
                                        </td>
                                    </tr>
                                </ItemTemplate>
                                <FooterTemplate>
                                    <asp:Panel ID="pnlDelivery" runat="server">
                                        <tr>
                                            <td>Delivery</td>
                                            <td class="text-right">+<asp:Label ID="lblDeliveryChrgs" runat="server" /></td>
                                        </tr>
                                    </asp:Panel>
                                    <tr>
                                        <td style="border-bottom: 3px solid #ddd; border-top: 3px solid #ddd;"><strong>Subtotal</strong></td>
                                        <td class="text-right" style="border-bottom: 3px solid #ddd; border-top: 3px solid #ddd;"><strong>
                                            <asp:Label ID="lblTotal" runat="server" /></strong></td>
                                    </tr>
                                    <asp:Panel ID="pnlEarly" runat="server">
                                        <tr>
                                            <td>Early Bird Discount</td>
                                            <td class="text-right">-
                                                <asp:Label ID="lblEarlyBirdDiscount" runat="server" /></td>
                                        </tr>
                                    </asp:Panel>
                                    <asp:Panel ID="pnlLateFee" runat="server">
                                        <tr>
                                            <td>Processing Fee</td>
                                            <td class="text-right">+
                                                <asp:Label ID="lblLateFee" runat="server" /></td>
                                        </tr>
                                    </asp:Panel>
                                    <asp:Panel ID="pnlGST" runat="server">
                                        <tr>
                                            <td style="font-style: italic;"><small>(GST included)</small></td>
                                            <td class="text-right" style="font-style: italic;"><small>(<asp:Label ID="lblGSTIncl" runat="server" />)</small></td>
                                        </tr>
                                    </asp:Panel>
                                    <tr>
                                        <td style="background-color: #dfdfdf;"><strong>Grand Total (Inc GST)</strong></td>
                                        <td class="text-right" style="background-color: #dfdfdf;"><strong>
                                            <asp:Label ID="lblGrandTotal" runat="server" /></strong></td>
                                        
                                    </tr>
                                </FooterTemplate>
                            </asp:Repeater>
                        </table>

                        <%--OnClick="OnValueChanged()"--%>

                        <%--<div class="checkbox">
                            <label><asp:CheckBox runat="server" ID="chktermuse" AutoPostBack="False" Text="I agree with the" CssClass="AcceptedAgreement" /></label>
                            <label><a data-toggle="modal" data-target=".termsuse-modal-lg">terms use</a></label>
                            <br />
                            <asp:CustomValidator runat="server" ID="CheckBoxRequired" EnableClientScript="true" ForeColor="#b94a48" ClientValidationFunction="CheckBoxRequired_ClientValidate"> You must agree to the terms and conditions.</asp:CustomValidator>
                            </div>--%>
                        <%--<a runat="server" href="~/Order-confirmation" class="btn btn-success btn-lg btn-block">Submit Order <span class="glyphicon glyphicon-chevron-right"></span></a>--%>

                        <%-- Button to trigger validation of all Billing Method fields --%>
                        <%--  <dx:ASPxButton ID="btnSubmitOrder" OnClick="btnSubmitOrder_Click" ClientInstanceName="btnSubmitOrder" runat="server" Text="Submit Order" CssClass="btn btn-success btn-lg btn-block" Native="true">
                        </dx:ASPxButton>--%>
                        <asp:Button ID="btnSubmitOrder" runat="server" Text="Submit Order" OnClick="btnSubmitOrder_Click" CssClass="btn btn-success btn-lg btn-block" OnClientClick="return validateForm()"></asp:Button>
                        <button id="BtnLoader" runat="server" class="btn btn-success btn-lg btn-block" disabled="disabled" style="display:none;"><img id="loaderimg" alt=" " runat="server" src=".../Images/loader.gif" width="50" height="50" />&nbsp; &nbsp; Processing..</button>

                        <br />
                        <!-- Render recaptcha API script This is unfinished reCaptcha feature that needs to be done-->
                        <cc1:RecaptchaApiScript ID="RecaptchaApiScript1" runat="server" />
                        <!-- Render recaptcha widget -->
                        <cc1:RecaptchaWidget ID="Recaptcha1" runat="server" RenderApiScript="False" />
                    </div>
                    <div class="greyBoxHalf">
                        <div class="row">
                            <div class="col-sm-6">
                                <a runat="server" id="bakCheckout" class="btn btn-primary btn-block"><span class="glyphicon glyphicon-chevron-left"></span>&nbsp;Back </a>
                            </div>
                            <div class="col-sm-6">
                                <a runat="server" id="canOrderanch" href="#" data-toggle="modal" data-target="#cancelOrderModal" class="btn btn-default">Cancel Order </a>
                                <%--<dx:ASPxButton ID="CancelOr" runat="server" CssClass="btn btn-link btn-block" Text="Cancel Order" OnClick="CancelOr_Click" CausesValidation="false" Native="true"></dx:ASPxButton>--%>
                            </div>
                        </div>



                    </div>
                </div>
            </div>
        </div>
    </div>
    <%-- DEVTEAM: THIS IS FOR CANCEL ORDER POPUP --%>
    <%-- This code needs to be added to the cancel button to open this popup:
        data-toggle="modal" data-target=".termsuse-modal-lg"   --%>
    <div class="modal fade termsuse-modal-lg" id="cancelOrderModal" tabindex="-1" role="dialog" aria-labelledby="mytermsuseLabel">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                    <h4 class="modal-title" id="mytermsuseLabel">Cancel Order?</h4>
                </div>
                <div class="modal-body">
                    <p>Cancelling this order will remove this order from the order process and you will be returned to the homepage.</p>
                    <p>Do you wish to proceed?</p>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-primary" data-dismiss="modal">Close</button>
                    <%-- DEVTEAM: THIS BUTTON CANCELS THE ORDER and returns user to SCHOOL LANDING PAGE --%>
                    <%--<button type="button" class="btn btn-default" data-dismiss="modal">Cancel Order</button>--%>
                    <dx:ASPxButton CssClass="btn btn-primary" ID="btnCancelOrder" OnClick="CancelOr_Click" ClientInstanceName="btnCancelOrder" runat="server" Native="true" Text="Cancel Order" AutoPostBack="false" ValidationGroup="V1">
                    </dx:ASPxButton>
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade termsuse-modal-lg" id="ShowCreditCardImg" tabindex="-1" role="dialog" aria-labelledby="Cvvimg">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                    <h4 class="modal-title" id="Cvvimg">CVV?</h4>
                </div>
                <div class="modal-body">
                    <p style="text-align: center">
                        <asp:Image ID="credImg" runat="server" AlternateText="Credit Card Image" ImageAlign="Middle" />
                    </p>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-primary" data-dismiss="modal">Close</button>
                    <%-- DEVTEAM: THIS BUTTON CANCELS THE ORDER and returns user to SCHOOL LANDING PAGE --%>
                    <%--<button type="button" class="btn btn-default" data-dismiss="modal">Cancel Order</button>--%>
                    <%--<dx:ASPxButton CssClass="btn btn-primary" ID="ASPxButton1" OnClick="CancelOr_Click" ClientInstanceName="btnca" runat="server" Native="true" Text="Cancel Order" AutoPostBack="false" ValidationGroup="V1">                        
                    </dx:ASPxButton>--%>
                </div>
            </div>
        </div>
    </div>


    <asp:HiddenField ID="hdStudentCount" runat="server" />
     <asp:HiddenField ID="Hdgrandtotal" runat="server" />
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="plccContent" runat="server">
    <script src="https://secure.ewaypayments.com/scripts/eCrypt.min.js"></script>
</asp:Content>
