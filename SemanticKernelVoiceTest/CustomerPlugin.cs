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

    public CustomerPlugin()
    {
        // Hard-coded dictionary of customers
        _customersById = new Dictionary<int, Customer>
            {
                { 123, new Customer { Id = 123, Name = "Kristoffer", Email = "kristoffer.nasstrom@gmail.com", OrderIds = new List<int> { 1001, 1002, 1003, 1004, 1005 } } },
                { 999, new Customer { Id = 999, Name = "Robbin", Email = "r.hoglin@gmail.com", OrderIds = new List<int>() } }
            };

        _customersByEmail = _customersById.Values.ToDictionary(c => c.Email, c => c);
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
}
