using Coalesce.Starter.Vue.Data.Models;

namespace Coalesce.Starter.Vue.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        Widget widget = new()
        {
            Name = "Test Widget",
            Category = WidgetCategory.Whizbangs,
        };

        Assert.Equal("Test Widget", widget.Name);
        Assert.Equal(WidgetCategory.Whizbangs, widget.Category);
    }
}