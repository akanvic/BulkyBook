using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        [BindProperty]
        public OrderDetailsVM OrderVM { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {
            OrderVM = new OrderDetailsVM()
            {
                OrderHeader = _unitOfWork.OrderHeader.FirstOrDefault(c => c.Id == id,
                                                      IncludeProperties: "ApplicationUser"),
                OrderDetails = _unitOfWork.OrderDetails.GetAll(c => c.OrderId == id, IncludeProperties: "Product")
            };
            return View(OrderVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Details")]
        public IActionResult Details(string stripeToken)
        {
            var orderHeader = _unitOfWork.OrderHeader.FirstOrDefault(c => c.Id == OrderVM.OrderHeader.Id, 
                                                      IncludeProperties: "ApplicationUser");
            if(stripeToken != null)
            {
                //process the payment
                var options = new ChargeCreateOptions
                {
                    Amount = Convert.ToInt32(orderHeader.OrderTotal*100),
                    Currency = "usd",
                    Description = "Order ID" + orderHeader.Id,
                    Source = stripeToken
                };
                var service = new ChargeService();
                var charge = service.Create(options); //This is the actual line that will make the call to create the transaction

                if (charge.BalanceTransactionId == null)  //TRansaction ID that is returned once a transaction is maid
                {
                    orderHeader.PaymentStatus = SD.PaymentStatusRejected;
                }
                else
                {
                    orderHeader.TransactionId = charge.BalanceTransactionId;
                }
                if (charge.Status.ToLower() == "succeeded")
                {
                    orderHeader.PaymentStatus = SD.PaymentStatusApproved;
                    orderHeader.PaymentDate = DateTime.Now;
                }

                _unitOfWork.Save();
            }
            return RedirectToAction(nameof(Details), new { id = orderHeader.Id });
        }

        [Authorize(Roles = SD.Role_Admin+ "," +SD.Role_Employee)]
        public IActionResult StartProcessing(int id)
        {
            var orderHeader = _unitOfWork.OrderHeader.FirstOrDefault(c => c.Id == id);
            orderHeader.OrderStatus = SD.StatusInProcess;
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.FirstOrDefault(c => c.Id == OrderVM.OrderHeader.Id);
            orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder(int id)
        {
            var orderHeader = _unitOfWork.OrderHeader.FirstOrDefault(c => c.Id == id);

            //We want refund if only the status was approved for the initial payment because if the status was for a delayed company we dont want to process a refund for them
            if(orderHeader.PaymentStatus == SD.StatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Amount = Convert.ToInt32(orderHeader.OrderTotal * 100),
                    Reason = RefundReasons.RequestedByCustomer,
                    Charge = orderHeader.TransactionId
                };
                var service = new RefundService();
                var refund = service.Create(options);

                orderHeader.OrderStatus = SD.StatusCancelled;
                orderHeader.PaymentStatus = SD.StatusCancelled;
            }
            else
            {
                orderHeader.OrderStatus = SD.StatusCancelled;
                orderHeader.PaymentStatus = SD.StatusCancelled;
            }
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        #region APICALLS
        [HttpGet]
        public IActionResult GetOrderList(string status)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            IEnumerable<OrderHeader> orderHeaderList;

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orderHeaderList = _unitOfWork.OrderHeader.GetAll(IncludeProperties: "ApplicationUser");
            }
            else
            {
                orderHeaderList = _unitOfWork.OrderHeader.GetAll(
                                c=>c.ApplicationUserId == claim.Value,
                                IncludeProperties: "ApplicationUser");
            }

            //We changing the list based in the status
            switch (status)
            {
                case "pending":
                    orderHeaderList = orderHeaderList.Where(c => c.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;

                case "inprocess":
                    orderHeaderList = orderHeaderList.Where(c => c.OrderStatus == SD.StatusApproved ||
                                                            c.OrderStatus==SD.StatusInProcess ||
                                                            c.OrderStatus==SD.StatusPending);
                    break;

                case "completed":
                    orderHeaderList = orderHeaderList.Where(c => c.OrderStatus == SD.StatusShipped);
                    break;

                case "rejected":
                    orderHeaderList = orderHeaderList.Where(c => c.OrderStatus == SD.StatusCancelled ||
                                                         c.OrderStatus == SD.StatusRefunded ||
                                                         c.OrderStatus == SD.PaymentStatusRejected);
                    break;

                default:
                    break;
            }
            return Json(new { data = orderHeaderList });

        }
        #endregion
    }
}