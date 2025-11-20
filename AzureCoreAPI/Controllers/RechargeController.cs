using AzureCoreAPI.Model;
using AzureCoreAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using System.Text.Json;

namespace AzureCoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RechargeController : ControllerBase
    {
        private readonly IServiceBusSender _sender;
        private readonly ICosmosDbService _cosmosDbService;

        public RechargeController(IServiceBusSender sender, ICosmosDbService cosmosDbService)
        {
            _sender = sender;
            _cosmosDbService = cosmosDbService;
        }

        [HttpPost("recharge")]
        public async Task<IActionResult> Send(string phoneNumber, int amount)
        {
            await _sender.SendMessageAsync(JsonSerializer.Serialize(new { phoneNumber, amount }).ToString());
            return Ok("Message sent to Azure Service Bus Queue");
        }

        [HttpGet("getRecharge")]
        public async Task<IActionResult> GetRecharge()
        {
            var jsonResult = await _sender.PeekMessagesAsync();

            var result = new List<PaymentRequest>();

            // Step 2: Deserialize each inner JSON string
            foreach (var item in jsonResult)
            {
                var obj = JsonSerializer.Deserialize<PaymentRequest>(item);
                result.Add(obj);
            }

            return Ok(result);
        }

        /// <summary>
        /// This Endpoint will store all recharge based on validation and upload in cosmos db
        /// </summary>
        /// <param name="payments"></param>
        /// <returns></returns>
        [HttpPost("validateRecharges")]
        public async Task<IActionResult> ValidateRecharge([FromBody] List<PaymentRequest> payments)
        {
            if (payments == null || payments.Count == 0)
                return BadRequest("No payment data received.");
            try
            {
                foreach (var item in payments)
                {
                    if (item.isSelected)
                        item.validationMessage = "SUCCESS";
                    else
                        item.validationMessage = "FAILED";

                    await _cosmosDbService.AddItemAsync(item);
                }
            }
            catch (Exception ex) 
            {
            
            }

            return Ok(new
            {
                payments
            });
        }

        [HttpGet("CheckRecharge")]
        public async Task<IActionResult> CheckRecharge(string phoneNumber)
        {
            try
            {
                var result = await _cosmosDbService.GetByPhoneNumberAsync(phoneNumber);
                if (result == null)
                    return NotFound();

                return Ok(result);
            }
            catch (Exception ex) 
            { 
                return BadRequest(ex);
            }
        }

    }
}
