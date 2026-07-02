

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ABC_ECOMMERCE_POE.Data;
using ABC_ECOMMERCE_POE.Models;

namespace ABC_ECOMMERCE_POE.Controllers
{
    [Authorize(Roles = "Admin,Customer")]
    public class CartItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartItemsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

   
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var cartsWithItems = await _context.CartItems
                .Include(ci => ci.Product)
                .Include(ci => ci.Cart)
                    .ThenInclude(c => c.User)
                .OrderBy(ci => ci.Cart.User.Email)
                .ToListAsync();

            return View(cartsWithItems);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var cartItem = await _context.CartItems
                .Include(c => c.Cart)
                    .ThenInclude(u => u.User)
                .Include(c => c.Product)
                .FirstOrDefaultAsync(m => m.CartItemId == id);

            if (cartItem == null)
                return NotFound();

            return View(cartItem);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["CartId"] = new SelectList(_context.Carts, "CartId", "UserId");
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CartItemId,CartId,ProductId,Quantity")] CartItem cartItem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cartItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CartId"] = new SelectList(_context.Carts, "CartId", "UserId", cartItem.CartId);
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", cartItem.ProductId);
            return View(cartItem);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem == null)
                return NotFound();

            ViewData["CartId"] = new SelectList(_context.Carts, "CartId", "UserId", cartItem.CartId);
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", cartItem.ProductId);
            return View(cartItem);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CartItemId,CartId,ProductId,Quantity")] CartItem cartItem)
        {
            if (id != cartItem.CartItemId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cartItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CartItemExists(cartItem.CartItemId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CartId"] = new SelectList(_context.Carts, "CartId", "UserId", cartItem.CartId);
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", cartItem.ProductId);
            return View(cartItem);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var cartItem = await _context.CartItems
                .Include(c => c.Cart)
                    .ThenInclude(u => u.User)
                .Include(c => c.Product)
                .FirstOrDefaultAsync(m => m.CartItemId == id);

            if (cartItem == null)
                return NotFound();

            return View(cartItem);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddToCart(int productId)
        {
            var userId = _userManager.GetUserId(User);

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = productId,
                    Quantity = 1
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("MyCart");
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

            if (cartItem == null)
                return NotFound();

            if (quantity < 1)
                quantity = 1; // prevent zero or negative

            cartItem.Quantity = quantity;
            await _context.SaveChangesAsync();

            return RedirectToAction("MyCart");
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyCart()
        {
            var userId = _userManager.GetUserId(User);

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                ViewBag.Message = "Your cart is empty.";
                return View(new List<CartItem>());
            }

            return View(cart.CartItems.ToList());
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            var userId = _userManager.GetUserId(User);

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("MyCart");
            }

            var totalAmount = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity);

            var newOrder = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = "Pending",
                TotalAmount = totalAmount
            };

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your order has been placed successfully!";
            return RedirectToAction("Index", "Orders");
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var item = await _context.CartItems.FindAsync(id);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("MyCart");
        }

        private bool CartItemExists(int id)
        {
            return _context.CartItems.Any(e => e.CartItemId == id);
        }
    }
}

