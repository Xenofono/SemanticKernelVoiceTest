using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelVoiceTest;
public class Order
{
    public int OrderId { get; set; }
    public bool IsDelivered { get; set; }
    public List<string> DeliveryHistory { get; set; } 
    public decimal TotalSum { get; set; }
}

public class OrderPlugin
{
    private readonly Dictionary<int, Order> _ordersById;
    private readonly Dictionary<int, List<Order>> _ordersByCustomerId;

    public OrderPlugin()
    {
        // Hard-coded dictionary of orders
        _ordersById = new Dictionary<int, Order>
            {
                { 1001, new Order { OrderId = 1001, IsDelivered = true, DeliveryHistory = new List<string> { "Order placed", "Shipped", "In Transit - Ompaketeras i Jönköping", "Delivered" }, TotalSum = 99.99m } },
                { 1002, new Order { OrderId = 1002, IsDelivered = false, DeliveryHistory = new List<string> { "Order placed", "Shipped", "In Transit - Inväntar utkörning i Västberga" }, TotalSum = 49.99m } },
                { 1003, new Order { OrderId = 1003, IsDelivered = true, DeliveryHistory = new List<string> { "Order placed", "Shipped", "Delivered" }, TotalSum = 29.99m } },
                { 1004, new Order { OrderId = 1004, IsDelivered = true, DeliveryHistory = new List<string> { "Order placed", "Shipped", "Delivered" }, TotalSum = 79.99m } },
                { 1005, new Order { OrderId = 1005, IsDelivered = false, DeliveryHistory = new List<string> { "Order placed" }, TotalSum = 59.99m } }
            };

        // Hard-coded dictionary of orders by customer ID
        _ordersByCustomerId = new Dictionary<int, List<Order>>
            {
                { 123, new List<Order> { _ordersById[1001], _ordersById[1002], _ordersById[1003], _ordersById[1004], _ordersById[1005] } },
                { 999, new List<Order>() } // No orders for this customer
            };
    }
    [KernelFunction("get_order_by_id")]
    [Description("gets an order if it exists")]
    [return: Description("An order")]
    public Order? GetOrderById(int orderId)
    {
        _ordersById.TryGetValue(orderId, out var order);
        return order;
    }
    [KernelFunction("get_all_customer_orders")]
    [Description("gets all orders if they exists")]
    [return: Description("A list of orders")]
    public List<Order>? GetOrdersByCustomerId(int customerId)
    {
        _ordersByCustomerId.TryGetValue(customerId, out var orders);
        return orders ?? new List<Order>();
    }
}