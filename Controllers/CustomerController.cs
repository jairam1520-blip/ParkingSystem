using ParkingSystem.Models;

using ParkingSystem.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using MailKit.Net.Smtp;
using ParkingSystem.Models.EmailModels;
using MimeKit;
using System.Net.Mail;

namespace EParkingSystem.Controllers
{
    public class CustomerController : Controller
    {
        //Dependency injected
        UserManager<ApplicationUser> _userManager;
        SignInManager<ApplicationUser> _signInManager;
        RoleManager<IdentityRole> _roleManager;

        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender; 
        


        public CustomerController(ApplicationDbContext db, UserManager<ApplicationUser> _userManager,
            SignInManager<ApplicationUser> _signInManager, RoleManager<IdentityRole> _roleManager,IEmailSender emailSender)
        {
            _db = db;
            this._userManager = _userManager;
            this._signInManager = _signInManager;
            this._roleManager = _roleManager;
            _emailSender = emailSender;
            
        }



        

        //Getting slots
        public IQueryable<Slot> GetTwoWheelerSlot()
        {
            return _db.Slots.Where(x => x.SlotType == "Two Wheeler");
        }

        public IActionResult CustomerHomePage()
        {
            return View();
        }



        //View All Booking
        [HttpGet]
        public IActionResult MyBooking()
        {
            var bookings = _db.Bookings.Where(x => x.UserId == _userManager.GetUserId(User)).Include(s => s.slot);
            return View(bookings);
        }


        //Delete booking
        [HttpGet]
        public IActionResult DeleteBooking(int id)
        {
            var booking = _db.Bookings.Find(id);
            var slot = _db.Slots.Find(booking.Sid);
            booking.slot = slot;

            if (booking != null)
            {
                return View(booking);
            }
            return NotFound();

        }

        [HttpPost]
        public IActionResult DeleteBookingPost(Booking b)
        {
            var booking = _db.Bookings.Find(b.Bid);

            if (booking != null)
            {
                _db.Bookings.Remove(booking);
                _db.SaveChanges();
                return RedirectToAction("MyBooking");
            }

            return NotFound();


        }



        //Create booking
        [HttpGet]
        public IActionResult NewBooking()
        {
            if (TempData["DateTimeError"] != null)
            {
                ViewBag.spanError = "Invalid date time choosed!";
                ViewBag.DateTimeError = "You can book parking for minimum one hour!";
            }

            ViewBag.UserId = _userManager.GetUserId(User);
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NewBookingPost(Booking model)
        {

            return RedirectToAction("CheckAvailibility", model);
        }

        //Check Availibility
        [HttpGet]
        public IActionResult CheckAvailibility(Booking model)
        {



            TempData["VehicleType"] = model.VehicleType;
            TempData["StartTime"] = model.StartDateTime;
            TempData["EndTime"] = model.EndDateTime;
            TempData["UserId"] = model.UserId;
            TimeSpan span = model.EndDateTime.Subtract(model.StartDateTime);


            if (span > TimeSpan.Zero)
            {

                if (model.VehicleType == Helper.FourWheeler)
                {
                    ViewBag.SlotPresent = _db.Slots.Where(x => x.SlotType == "Four Wheeler");
                }
                else if (model.VehicleType == Helper.TwoWheeler)
                {

                    ViewBag.SlotPresent = _db.Slots.Where(x => x.SlotType == "Two Wheeler");
                }
                ViewBag.VehicleType = model.VehicleType;



                var overlappedBooking = _db.Bookings.Where((x => x.StartDateTime == model.StartDateTime || x.EndDateTime == model.EndDateTime || (model.StartDateTime > x.StartDateTime && model.EndDateTime <= x.EndDateTime) || 
                (model.StartDateTime >= x.StartDateTime && model.StartDateTime < x.EndDateTime && x.VehicleType == model.VehicleType)));
                
                
                
                var overlappedSlotId = new List<int>();
                foreach (var slot in overlappedBooking)
                {
                    overlappedSlotId.Add((int)slot.Sid);
                }

                ViewBag.overlappedSlotId = overlappedSlotId;

                return View(new Slot());
            }
            else
            {
                TempData["DateTimeError"] = 1;
                return RedirectToAction("NewBooking");
            }





        }





        // Save booking
        [HttpPost]
        public IActionResult CheckAvailibility(Slot selectedSlot)
        {

            var startTime = (DateTime)TempData["StartTime"];
            var endTime = (DateTime)TempData["EndTime"];


            Booking confirmedBooking = new Booking();
            confirmedBooking.Sid = selectedSlot.Sid;
            confirmedBooking.StartDateTime = startTime;
            confirmedBooking.EndDateTime = endTime;
            confirmedBooking.UserId = _userManager.GetUserId(User);
            confirmedBooking.VehicleType = (string)TempData["VehicleType"];



            var span = endTime.Subtract(startTime);
            var minutes = span.TotalMinutes;


            confirmedBooking.BillAmount = Math.Truncate(minutes * 0.167);
            _db.Bookings.Add(confirmedBooking);
            _db.SaveChanges();


            return RedirectToAction("SendEmail", new RouteValueDictionary(confirmedBooking));
        }


        [HttpGet]
        public IActionResult ContactUs()
        {
            ContactUsModel model = new ContactUsModel();
            return View(model);
        }


        public IActionResult ContactUsPost(ContactUsModel model)
        {
            model.Id = 0;
            if (ModelState.IsValid)
            {
                _db.ContactUsModel.Add(model);
                _db.SaveChanges();

                return View();
            }
            ModelState.AddModelError(string.Empty, "Please enter valid details");
            return RedirectToAction("ContactUs",model);
        }
        public async Task<IActionResult> SendEmail(Booking booking)
        {
            // create email message
          
            var user = await _userManager.GetUserAsync(User);
            string emailTo = user.Email;

            var startTime = booking.StartDateTime;
            var endTime = booking.EndDateTime;
            var vehicleType = booking.VehicleType;
            var name = user.Name;

            var billAmount = booking.BillAmount;
            var slots = from s in _db.Slots where s.Sid == booking.Sid select s.SlotNumber;
            var slot = slots.FirstOrDefault();


            string subject = "Booking Confirmed";
            string body = "Congratulation your booking is confirmed." +Environment.NewLine +
                "Name:"+name+ Environment.NewLine +
                "Vehicle Type:" +vehicleType + Environment.NewLine +
                "Slot Number:"+slot+Environment.NewLine +
                "Start Time:" + startTime+ Environment.NewLine +
                "End Time:" +endTime+ Environment.NewLine +
                "Bill Amount:" +billAmount+ Environment.NewLine +
                "Thankyou for using our service." ;


          
          

            var message = new Message(new String[] {emailTo},subject,body);
            _emailSender.SendEmail(message);
            
            
            return RedirectToAction("MyBooking");
        }

        
    }
}
