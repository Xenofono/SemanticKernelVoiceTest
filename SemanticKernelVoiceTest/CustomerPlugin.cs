using Azure;
using Azure.Communication.Email;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelVoiceTest;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public List<int> OrderIds { get; set; }
}

public class CustomerPlugin
{
    private readonly Dictionary<int, Customer> _customersById;
    private readonly Dictionary<string, Customer> _customersByEmail;
    private readonly string _emailConnectionString;

	public CustomerPlugin(string emailConnectionString)
	{
		// Hard-coded dictionary of customers
		_customersById = new Dictionary<int, Customer>
			{
				{ 123, new Customer { Id = 123, Name = "Kristoffer", Email = "kristoffer.nasstrom@gmail.com", OrderIds = new List<int> { 1001, 1002, 1003, 1004, 1005 } } },
				{ 999, new Customer { Id = 999, Name = "Robbin", Email = "r.hoglin@gmail.com", OrderIds = new List<int>() } },
				{ 500, new Customer { Id = 500, Name = "Adam", Email = "kristoffer.nasstrom@hayppgroup.com", OrderIds = new List<int>{ 1010 } } }
			};

		_customersByEmail = _customersById.Values.ToDictionary(c => c.Email, c => c);
		_emailConnectionString = emailConnectionString;
	}

	[KernelFunction("get_customer_by_id")]
    [Description("gets a customer by id if it exists")]
    [return: Description("A customer")]
    public Customer? GetUserById(int id)
    {
        _customersById.TryGetValue(id, out var customer);
        return customer;
    }
    [KernelFunction("get_customer_by_email")]
    [Description("gets a customer by email if it exists")]
    [return: Description("A customer")]
    public Customer? GetUserByEmail(string email)
    {
        _customersByEmail.TryGetValue(email, out var customer);
        return customer;
    }
	[KernelFunction("create_new_user")]
	[Description("creates a new user")]
	[return: Description("the newly created user")]
	public Customer? CreateUser(string name, string email)
    {
        var newUser = new Customer { Name = name, Email = email, OrderIds = [], Id = 600 };
        _customersById[600] = newUser;
        return newUser;
    }

	[KernelFunction("create_support_ticket_and_email_user")]
	[Description("creates a support ticket and emails the user a summary of the conversation")]
	[return: Description("the support ticket")]
	public string CreateSupportTicketAndEmailSummaryOfConversation(string summaryOfConversation, string userEmail)
	{
		string guid = Guid.NewGuid().ToString();

		string connectionString = _emailConnectionString;
		var emailClient = new EmailClient(connectionString);


		EmailSendOperation emailSendOperation = emailClient.Send(
			WaitUntil.Completed,
			senderAddress: "DoNotReply@cebf8315-3b3b-4459-9cb6-dfd228120cf1.azurecomm.net",
			recipientAddress: userEmail,
			subject: $"Support ticket {guid} created",
			htmlContent: $"<html><p>{summaryOfConversation}</p></html>",
			plainTextContent: $"{{summaryOfConversation");

        return guid;
	}
}
