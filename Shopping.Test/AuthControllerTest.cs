using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shopping.Controllers;
using Shopping.ViewModels;
using Shopping.Context;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

public class AuthControllerTests
{
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private DbContextOptions<ShoppingDb> CreateDbContextOptions()
    {
        return new DbContextOptionsBuilder<ShoppingDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task Login_InvalidEmail_ReturnsViewWithError()
    {
        var options = CreateDbContextOptions();
        using (var context = new ShoppingDb(options))
        {
            var controller = new AuthController(context, _cache, _config);

            var model = new LoginViewModel
            {
                Email = "invalid@example.com",
                PassWord = "password123"
            };

            var result = await controller.Login(model) as ViewResult;

            Assert.NotNull(result);
            Assert.True(controller.ModelState.ContainsKey("Email"));
        }
    }

  

}




