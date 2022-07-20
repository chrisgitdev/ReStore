using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class BasketController : BaseApiController
    {
        private readonly StoreContext _context;
        public BasketController(StoreContext context)
        {
            _context = context;

        }

        [HttpGet(Name = "GetBasket")]
        public async Task<ActionResult<BasketDto>> GetBasket()
        {
            var basket = await RetrieveBasket(GetBuyerId());

            if (basket == null) return NotFound();
            return basket.MapBasketToDto();
        }

        [HttpPost]
        public async Task<ActionResult<BasketDto>> AddItemToBasket(int productId, int quantity)
        {
            var basket = await RetrieveBasket(GetBuyerId());//get basket || create basket

            if (basket == null) basket = CreateBasket();

            var product = await _context.Products.FindAsync(productId);//get product

            if (product == null) return BadRequest(new ProblemDetails { Title = "Product Not Found" });

            basket.AddItem(product, quantity);//add item

            var result = await _context.SaveChangesAsync() > 0;//save changes

            if (result) return CreatedAtRoute("GetBasket", basket.MapBasketToDto());

            return BadRequest(new ProblemDetails { Title = "Problem saving item to basket" });
        }

        [HttpDelete]
        public async Task<ActionResult> RemoveBasketItem(int productId, int quantity)
        {
            var basket = await RetrieveBasket(GetBuyerId());//get basket

            if (basket == null) return NotFound();

            basket.RemoveItem(productId, quantity);//remove item or reduce quantity

            var result = await _context.SaveChangesAsync() > 0;//save changes

            if (result) return Ok();

            return BadRequest(new ProblemDetails { Title = "Problem removing item from basket" });
        }

        private async Task<Basket> RetrieveBasket(string buyerId)
        {
            if (string.IsNullOrEmpty(buyerId))
            {
                Response.Cookies.Delete("buyerId");
                return null;
            }

            return await _context.Baskets
                .Include(i => i.Items)
                .ThenInclude(p => p.Product)
                .FirstOrDefaultAsync(x => x.BuyerId == buyerId);
        }

        private string GetBuyerId()
        {
            return User.Identity?.Name ?? Request.Cookies["buyerId"];
        }

        private Basket CreateBasket()
        {
            var buyerId = User.Identity?.Name;
            if (string.IsNullOrEmpty(buyerId))
            {
                buyerId = Guid.NewGuid().ToString();
                var cookieOptions = new CookieOptions { IsEssential = true, Expires = DateTime.Now.AddDays(30) };
                Response.Cookies.Append("buyerId", buyerId, cookieOptions);
            }

            var basket = new Basket { BuyerId = buyerId };
            _context.Baskets.Add(basket);
            return basket;
        }
    }
}