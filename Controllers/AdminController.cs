using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using ParkingSystem.Models.EmailModels;

namespace EParkingSystem.Controllers
{
    public class AdminController : Controller
    {
        UserManager<ApplicationUser> _userManager;
        SignInManager<ApplicationUser> _signInManager;
        RoleManager<IdentityRole> _roleManager;

        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> _userManager,
            SignInManager<ApplicationUser> _signInManager, RoleManager<IdentityRole> _roleManager,IEmailSender emailSender)
        {
            _db = db;
            this._userManager = _userManager;
            this._signInManager = _signInManager;
            this._roleManager = _roleManager;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {

            var bookingCount =  _db.Bookings.Where(x => x.StartDateTime >= DateTime.Today).Count();
            var users = from user in _db.Users select user; 
            var userCount = users.Count();
            var slotCount = _db.Slots.Count();
            AdminDashBoardModel model = new AdminDashBoardModel();
            model.BookingCount=bookingCount;
            model.UserCount=userCount;
            model.SlotCount=slotCount;
            return View(model);
        }
        public IActionResult ViewAllBooking()
        {
            var bookings = _db.Bookings.Include(s => s.slot).ToList();
            return View(bookings);
        }

        [HttpGet]
        public IActionResult ViewAllUsers()
        {
            var users = (from user in _db.Users select new User(user.Name, user.Email)).ToList();

            return View(users);
        }
        public IActionResult ViewAllSlots()
        {

            var slots = _db.Slots;
            return View(slots);
        }
        [HttpGet]
        public IActionResult CreateSlot()
        {
            return View();
        }
        [HttpPost]
        public IActionResult CreateSlot(Slot slot)
        {
            if (ModelState.IsValid)
            {
                _db.Slots.Add(slot);
                _db.SaveChanges();
                return RedirectToAction("ViewAllSlots");
            }
            return NotFound();
        }
        public IActionResult EditSlot(int id)
        {
            var slot = _db.Slots.Find(id);
            return View(slot);
        }

        public IActionResult EditSlotPost(Slot slot)
        {
            if (ModelState.IsValid)
            {
                _db.Slots.Update(slot);
                _db.SaveChanges();
                return RedirectToAction("ViewAllSlots");
            }
            return NotFound();
        }
        public IActionResult DeleteSlot(int id)
        {
            var slot = _db.Slots.Find(id);
            return View(slot);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteSlotPost(Slot slot)
        {

            var slotfetched = _db.Slots.Find(slot.Sid);
            if(slotfetched != null)
            {
                _db.Remove(slotfetched);
                _db.SaveChanges();
                return RedirectToAction("ViewAllSlots");

            }
            return NotFound();
        }

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
                return RedirectToAction("ViewAllBooking");
            }

            return NotFound();


        }

        public IActionResult GeAllMessages()
        {
            var messeges = _db.ContactUsModel;
            return View(messeges);
        }

        public IActionResult DeleteMessagePost(ContactUsModel model)
        {
            var message = _db.ContactUsModel.Find(model.Id);
            if (message != null)
            {
                _db.ContactUsModel.Remove(message);
                _db.SaveChanges();
                return RedirectToAction("GeAllMessages");
            }
            return NotFound();
        }

        public IActionResult Reply(ContactUsModel model)
        {
            var msgFetched = _db.ContactUsModel.Find(model.Id);
            var emailTo = msgFetched.Email;
            string subject = "From E-Parking System team";
            string body = "Dear customer," + Environment.NewLine +
                "Thankyou for contacting us,how can we help you?";




            var message = new Message(new String[] { emailTo }, subject, body);
            _emailSender.SendEmail(message);
            return RedirectToAction("Index");
        }
    }
}
