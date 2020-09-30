using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BulkyBook.Models.ViewModels;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Http;

namespace BulkyBook.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly IUnitOfWork _unitOfWork;
        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var products = _unitOfWork.Product.GetAll(IncludeProperties: "Category,CoverType");

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if(claim != null)
            {
                //Were getting the number of shopping cart items of a logged in application user
                var count = _unitOfWork.ShoppingCart
                    .GetAll(c => c.ApplicationUserId == claim.Value)
                    .ToList().Count();

                HttpContext.Session.SetInt32(SD.ssShoppingCart, count);
            }
            return View(products);
        }

        public IActionResult Details(int id)
        {
            var products = _unitOfWork.Product.
                FirstOrDefault(c => c.Id == id, IncludeProperties: "Category,CoverType");
            ShoppingCart cartObj = new ShoppingCart()
            {
                Product = products,
                ProductId = products.Id
            };

            return View(cartObj);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCart cartObject)
        {
            cartObject.Id = 0;
            if (ModelState.IsValid)
            {
                //WE will add to the cart
                //Then we have to find the ID of the Logged in user
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                cartObject.ApplicationUserId = claim.Value;

                var cartFromDb = _unitOfWork.ShoppingCart.FirstOrDefault(
                    c => c.ApplicationUserId == cartObject.ApplicationUserId && c.ProductId == cartObject.ProductId // it gets an application user with a cart object and a product id that has been added to a cart
                    , IncludeProperties: "Product"
                    );

                if(cartFromDb == null)
                {
                    //no record exist in the database for that product for this user
                    _unitOfWork.ShoppingCart.Add(cartObject);
                }
                else
                {
                    //We are updating our cart object with the one we have in our DB;
                    cartFromDb.Count += cartObject.Count;
                    _unitOfWork.ShoppingCart.Update(cartFromDb);
                }
                _unitOfWork.Save();

                //We want to store the user shopping cart objects in a session
                var count = _unitOfWork.ShoppingCart.
                    GetAll(c => c.ApplicationUserId == cartObject.ApplicationUserId)
                    .ToList().Count();

                //The reason why we added the SetObject extension method is that i gives us the flexibility to store different types of object in our session whether it is an integer or a whole cart object
                //Asp.net core session only supports integer which is our count
                HttpContext.Session.SetInt32(SD.ssShoppingCart, count);

                return RedirectToAction(nameof(Index));
            }
            else
            {
                var products = _unitOfWork.Product.
                    FirstOrDefault(c => c.Id == cartObject.ProductId, IncludeProperties: "Category,CoverType");
                ShoppingCart cartObj = new ShoppingCart()
                {
                    Product = products,
                    ProductId = products.Id
                };

                return View(cartObj);
            }
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
