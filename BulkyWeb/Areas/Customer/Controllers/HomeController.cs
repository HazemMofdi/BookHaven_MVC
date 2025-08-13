using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Bulky.Models;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Bulky.Utility;

namespace BookWeb.Areas.Customer.Controllers
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

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if(claim != null)
            {
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
                .GetAll(c => c.UserId == claim.Value).Count());
            }
            var prodList = _unitOfWork.Product.GetAll(includeProperties: "Category,ProductImages");
            return View(prodList);
        }
        [HttpGet]
        public IActionResult Details(int Id)
        {
            var Cart = new ShoppingCart()
            {
                Product = _unitOfWork.Product.Get(p => p.Id == Id, "Category,ProductImages"),
                Count = 1,
                ProductId = Id
            };
            return View(Cart);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart Cart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            Cart.UserId = userId;

            ShoppingCart CartFromDb = _unitOfWork.ShoppingCart
                .Get(c => c.UserId == userId && c.ProductId == Cart.ProductId);

            if(CartFromDb != null)
            {
                CartFromDb.Count += Cart.Count;
                _unitOfWork.ShoppingCart.Update(CartFromDb);
                _unitOfWork.Save();
                
                TempData["Success"] = "Cart Updated Successfully";
            }
            else
            {
                _unitOfWork.ShoppingCart.Add(Cart);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
                .GetAll(c => c.UserId == userId).Count());
                TempData["Success"] = "Added To Cart Successfully";
            }

            return RedirectToAction("Index");
            
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
