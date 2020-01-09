using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using ContactManagerAPI.Models;
using Microsoft.Extensions.Configuration;
using ContactManagerAPI.Helpers;

namespace ContactManagerAPI.Functions
{
    public class CreateContact
    {
        private readonly ILogger _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _config;

        private Database contactDatabase;
        private Container contactContainer;

        public CreateContact(
            ILogger<CreateContact> logger,
            CosmosClient cosmosClient,
            IConfiguration config)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _config = config;

            contactDatabase = _cosmosClient.GetDatabase(_config[Settings.DATABASE_NAME]);
            contactContainer = contactDatabase.GetContainer(_config[Settings.CONTAINER_NAME]);

        }

        [FunctionName(nameof(CreateContact))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contacts")] HttpRequest req)
        {
            IActionResult returnValue = null;

            _logger.LogInformation("Creating a new contact");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var input = JsonConvert.DeserializeObject<Contact>(requestBody);

            var contact = new Contact
            {
                ContactId = Guid.NewGuid().ToString(),
                ContactName = new ContactName(input.ContactName.FirstName, input.ContactName.LastName),
                ContactBirthday = new ContactBirthday
                {
                    Birthday = input.ContactBirthday.Birthday
                },
                ContactAddress = new ContactAddress
                {
                    AddressLine1 = input.ContactAddress.AddressLine1,
                    AddressLine2 = input.ContactAddress.AddressLine2,
                    AddressCity = input.ContactAddress.AddressCity,
                    AddressState = input.ContactAddress.AddressState,
                    AddressZIPCode = input.ContactAddress.AddressZIPCode
                },
                ContactEmail = new ContactEmail
                {
                    Email = input.ContactEmail.Email
                },
                ContactPhone = new ContactPhone
                {
                    MobilePhone = input.ContactPhone.MobilePhone,
                    HomePhone = input.ContactPhone.HomePhone,
                    WorkPhone = input.ContactPhone.WorkPhone
                },
                ContactType = input.ContactType
            };

            try
            {
                ItemResponse<Contact> contactResponse = await contactContainer.CreateItemAsync(
                    contact,
                    new PartitionKey(contact.ContactType));         
                returnValue = new OkObjectResult(contactResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Creating new contact failed. Exception thrown: {ex.Message}");
                returnValue = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return returnValue;
        }
    }
}
