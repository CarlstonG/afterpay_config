using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AfterpayTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PaymentController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Content("Welcome to PaymentController");
        }

        [HttpPost("CreateAfterpayCharge")]
        public async Task<IActionResult> CreateAfterpayCharge()
        {
            string username = "45776";
            string password = "4a9a9ec0e2a37646a94765e61d716fb3619849a977d6672bd1f9cdfb82f4d172515f9030a595dfac2bfe7fe96a3e480a42ff81dfcd1c2b582dccc64ca6bdb462";
            string url = "https://global-api-sandbox.afterpay.com/v2/checkouts";

            using (var client = new HttpClient())
            {
                  var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                  client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var chargeData = new
                {
                    amount = new
                    {
                        amount = "100.00",
                        currency = "AUD"
                    },
                    consumer = new
                    {
                        email = "john.doe@example.com",
                        givenNames = "John",
                        surname = "Doe",
                        phoneNumber = "1234567890"
                    },
                    billing = new
                    {
                        name = "John Doe",
                        line1 = "123 Main St",
                        line2 = "",
                        area1 = "",
                        region = "",
                        postcode = "12345",
                        countryCode = "AU",
                        phoneNumber = "1234567890"
                    },
                    shipping = new
                    {
                        name = "John Doe",
                        line1 = "123 Main St",
                        line2 = "",
                        area1 = "",
                        region = "",
                        postcode = "12345",
                        countryCode = "AU",
                        phoneNumber = "1234567890"
                    },
                    merchant = new
                    {
                        redirectConfirmUrl = "https://localhost:5001/success",
                        redirectCancelUrl = "https://localhost:5001/cancel",
                        popupOriginUrl = "https://localhost:5001",
                        name = "Your Merchant Name"
                    },
                    items = new[]
                    {
                        new
                        {
                            name = "Sample Item",
                            sku = "123456",
                            quantity = 1,
                            pageUrl = "https://localhost:5001",
                            imageUrl = "https://localhost:5001/image.jpg",
                            price = new
                            {
                                amount = "100.00",
                                currency = "AUD"
                            },
                            categories = new[]
                            {
                                new[] { "Sample Category" }
                            },
                            estimatedShipmentDate = "2024-09-10"
                        }
                    },
                    courier = new
                    {
                        shippedAt = "2024-09-10T00:00:00Z",
                        name = "Sample Courier",
                        tracking = "TRACK1234567890",
                        priority = "STANDARD"
                    },
                    taxAmount = new
                    {
                        amount = "10.00",
                        currency = "AUD"
                    },
                    shippingAmount = new
                    {
                        amount = "10.00",
                        currency = "AUD"
                    },
                    discounts = new[]
                    {
                        new
                        {
                            displayName = "Sample Discount",
                            amount = new
                            {
                                amount = "10.00",
                                currency = "AUD"
                            }
                        }
                    },
                    description = "Sample description"
                };

                var jsonData = JsonConvert.SerializeObject(chargeData);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync(url, content);
                    var responseData = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        // Log or handle error details
                        return StatusCode((int)response.StatusCode, responseData);
                    }

                    return Ok(responseData);
                }
                catch (Exception ex)
                {
                    // Log or handle exception
                    return StatusCode(500, "Internal server error");
                }
            }
        }
    }
}
