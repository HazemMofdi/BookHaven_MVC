using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace BookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM orderVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int Id)
        {
            orderVM = new()
            {
                OrderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == Id, includeProperties: "User"),
                orderDetail = _unitOfWork.OrderDetail.GetAll(o=> o.OrderHeaderId == Id, includeProperties:"Product")
            };
            return View(orderVM);
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetails()
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(o => o.Id == orderVM.OrderHeader.Id);

            orderHeaderFromDb.Name = orderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = orderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.City = orderVM.OrderHeader.City;
            orderHeaderFromDb.State = orderVM.OrderHeader.State;
            orderHeaderFromDb.StreetAddress = orderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.PostalCode = orderVM.OrderHeader.PostalCode;
            if(!string.IsNullOrEmpty(orderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = orderVM.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(orderVM.OrderHeader.TrackinNumber))
            {
                orderHeaderFromDb.TrackinNumber = orderVM.OrderHeader.TrackinNumber;
            }
            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();
            TempData["Success"] = "Order Details Updated Successfully!";

            return RedirectToAction("Details", new {Id = orderHeaderFromDb.Id});
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(orderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["Success"] = "Order Details Updated Successfully!";
            return RedirectToAction("Details", new { Id = orderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == orderVM.OrderHeader.Id);
            orderHeader.TrackinNumber = orderVM.OrderHeader.TrackinNumber;
            orderHeader.Carrier = orderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;
            if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }

            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();
            TempData["Success"] = "Order Shipped Successfully!";
            return RedirectToAction("Details", new { Id = orderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == orderVM.OrderHeader.Id);
            if(orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions()
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };
                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
            }
            _unitOfWork.Save();
            TempData["Success"] = "Order Cancelled Successfully!";
            return RedirectToAction("Details", new { Id = orderVM.OrderHeader.Id });
        }


        [ActionName("Details")]
        [HttpPost]
        public IActionResult DetailsPayNow()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == orderVM.OrderHeader.Id, includeProperties: "User");

            var orderDetails = _unitOfWork.OrderDetail.GetAll
                (o => o.OrderHeaderId == orderVM.OrderHeader.Id, includeProperties: "Product");
            // Stripe Logic
            var domain = "https://localhost:7204/";

            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"Admin/Order/PaymentConfirmation?Id={orderVM.OrderHeader.Id}",
                CancelUrl = domain + $"Admin/Order/details?Id={orderVM.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in orderDetails)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = (long)(item.Price * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                };
                options.LineItems.Add(sessionLineItem);
            }



            var service = new SessionService();
            Session session = service.Create(options);


            _unitOfWork.OrderHeader.UpdateStripePaymentId
                (orderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            //PaymentIntentId is null because the PaymentIntentId is populated once the payment is successfull
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }



        public IActionResult PaymentConfirmation(int Id)
        {
            //                                                          The Navigation Prop in OrderHeader Class
            var orderHeader = _unitOfWork.OrderHeader.Get(oh => oh.Id == Id, includeProperties: "User");
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                // Oreder By a Company
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(Id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(Id, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }

            return View(Id);
        }



        #region APICalls
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> OrderHeaderList;
            

            if(User.IsInRole("Admin") || User.IsInRole("Employee"))
            {
                OrderHeaderList = _unitOfWork.OrderHeader.GetAll(includeProperties: "User");
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                OrderHeaderList = _unitOfWork.OrderHeader.GetAll(o=> o.UserId == userId, includeProperties: "User");
            }

            switch (status)
            {
                case "pending":
                    OrderHeaderList = OrderHeaderList.Where(o => o.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inProcess":
                    OrderHeaderList = OrderHeaderList.Where(o => o.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    OrderHeaderList = OrderHeaderList.Where(o => o.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    OrderHeaderList = OrderHeaderList.Where(o => o.OrderStatus == SD.StatusApproved);

                    break;
                default:
                    break;
            }


            return Json(new { data = OrderHeaderList });
        }

       
        #endregion


    }
}
