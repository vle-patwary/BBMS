using System.Linq;
using Rotativa.AspNetCore;
using BBMS.Data;
using BBMS.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using YourApp.Models;

namespace BBMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index() => View();

        // ─── DONATE ───────────────────────────────────────────
        [HttpGet]
        public IActionResult Donate() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Donate(DonateBlood donor)
        {
            if (ModelState.IsValid)
            {
                int count = _db.DonateBloods.Count();
                donor.DonorId = "DNR-" + (count + 1).ToString("D4");
                donor.CreatedAt = DateTime.Now;
                _db.DonateBloods.Add(donor);
                _db.SaveChanges();
                return RedirectToAction("DonationSuccess");
            }
            return View(donor);
        }

        public IActionResult DonationSuccess() => View();

        // ─── BLOOD REQUEST ────────────────────────────────────
        [HttpGet]
        public IActionResult RequestBlood() => View(new BloodRequest());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestBlood(BloodRequest model)
        {
            if (!ModelState.IsValid)
                return View(model);

            int count = _db.BloodRequests.Count();
            model.InvoiceNumber = "INV-" + (count + 1).ToString("D6");
            model.RequestDate = DateTime.Now;
            model.Status = "Pending";

            // DO NOT override Quantity here — it comes from the form
            if (model.Quantity <= 0)
                model.Quantity = 1;

            model.TotalCost = model.Quantity * 165;

            _db.BloodRequests.Add(model);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Your blood request has been submitted successfully!";
            return RedirectToAction("BloodRequests");
        }

        [HttpGet]
        public IActionResult RequestSuccess(int id)
        {
            var request = _db.BloodRequests.Find(id);
            if (request == null) return NotFound();
            return View(request);
        }

        // ─── BLOOD REQUESTS LIST ──────────────────────────────
        [HttpGet]
        public IActionResult BloodRequests(string search, string status)
        {
            var query = _db.BloodRequests.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(r =>
                    r.RequesterName.Contains(search) ||
                    r.BloodGroup.Contains(search));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            var list = query.OrderByDescending(r => r.RequestDate).ToList();

            ViewBag.TotalRequests = _db.BloodRequests.Count();
            ViewBag.Pending = _db.BloodRequests.Count(r => r.Status == "Pending");
            ViewBag.Approved = _db.BloodRequests.Count(r => r.Status == "Approved");
            ViewBag.Rejected = _db.BloodRequests.Count(r => r.Status == "Rejected");
            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveRequest(int id)
        {
            var r = _db.BloodRequests.Find(id);
            if (r != null) { r.Status = "Approved"; _db.SaveChanges(); }
            return RedirectToAction("BloodRequests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectRequest(int id)
        {
            var r = _db.BloodRequests.Find(id);
            if (r != null) { r.Status = "Rejected"; _db.SaveChanges(); }
            return RedirectToAction("BloodRequests");
        }

        [HttpGet]
        public IActionResult EditRequest(int id)
        {
            var r = _db.BloodRequests.Find(id);
            if (r == null) return NotFound();
            if (r.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Only pending requests can be edited.";
                return RedirectToAction("BloodRequests");
            }
            return View(r);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditRequest(BloodRequest model)
        {
            var r = _db.BloodRequests.Find(model.Id);
            if (r == null) return NotFound();
            if (r.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Only pending requests can be edited.";
                return RedirectToAction("BloodRequests");
            }

            r.RequesterName = model.RequesterName;
            r.BloodGroup = model.BloodGroup;
            r.RequiredDate = model.RequiredDate;
            r.Hospital = model.Hospital;
            r.ContactNumber = model.ContactNumber;
            r.Notes = model.Notes;
            r.Quantity = model.Quantity <= 0 ? 1 : model.Quantity;
            r.TotalCost = r.Quantity * 165;

            _db.SaveChanges();
            TempData["SuccessMessage"] = "Request updated successfully.";
            return RedirectToAction("BloodRequests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteRequest(int id)
        {
            var r = _db.BloodRequests.Find(id);
            if (r != null && r.Status == "Pending")
            {
                _db.BloodRequests.Remove(r);
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Request deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Only pending requests can be deleted.";
            }
            return RedirectToAction("BloodRequests");
        }

        [HttpGet]
        public IActionResult DownloadInvoice(int id)
        {
            var request = _db.BloodRequests.Find(id);
            if (request == null) return NotFound();

            return new ViewAsPdf("InvoicePdf", request)
            {
                FileName = "Invoice-" + request.InvoiceNumber + ".pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(15, 15, 15, 15)
            };
        }

        // ═══════════════════════════════════════════════════════
        // ─── STAFF CRUD ───────────────────────────────────────
        // ═══════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult ManageStaff(string search, string role, string status)
        {
            var query = _db.Staffs.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s =>
                    s.Name.Contains(search) ||
                    s.Email.Contains(search) ||
                    s.Contact.Contains(search));

            if (!string.IsNullOrEmpty(role))
                query = query.Where(s => s.Role == role);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);

            ViewBag.TotalStaff = _db.Staffs.Count();
            ViewBag.ActiveStaff = _db.Staffs.Count(s => s.Status == "Active");
            ViewBag.OnLeave = _db.Staffs.Count(s => s.Status == "On Leave");
            ViewBag.Admins = _db.Staffs.Count(s => s.Role == "Admin");
            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.Status = status;

            return View(query.OrderByDescending(s => s.JoinDate).ToList());
        }

        [HttpGet]
        public IActionResult AddStaff() => View(new Staff());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddStaff(Staff model)
        {
            if (_db.Staffs.Any(s => s.Email == model.Email))
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            if (model.JoinDate == default)
                model.JoinDate = DateTime.Now;

            _db.Staffs.Add(model);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Staff member added successfully!";
            return RedirectToAction("ManageStaff");
        }

        [HttpGet]
        public IActionResult EditStaff(int id)
        {
            var staff = _db.Staffs.Find(id);
            if (staff == null) return NotFound();
            return View(staff);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditStaff(Staff model)
        {
            var staff = _db.Staffs.Find(model.Id);
            if (staff == null) return NotFound();

            if (_db.Staffs.Any(s => s.Email == model.Email && s.Id != model.Id))
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            staff.Name = model.Name;
            staff.Email = model.Email;
            staff.Contact = model.Contact;
            staff.Role = model.Role;
            staff.Status = model.Status;
            staff.JoinDate = model.JoinDate;

            if (!string.IsNullOrWhiteSpace(model.Password))
                staff.Password = model.Password;

            _db.SaveChanges();
            TempData["SuccessMessage"] = "Staff member updated successfully!";
            return RedirectToAction("ManageStaff");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteStaff(int id)
        {
            var staff = _db.Staffs.Find(id);
            if (staff != null)
            {
                _db.Staffs.Remove(staff);
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Staff member deleted.";
            }
            return RedirectToAction("ManageStaff");
        }

        // ═══════════════════════════════════════════════════════
        // ─── HOSPITAL CRUD ────────────────────────────────────
        // ═══════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult ManageHospital(string search)
        {
            var query = _db.Hospitals.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(h =>
                    h.Name.Contains(search) ||
                    h.Address.Contains(search) ||
                    h.Contact.Contains(search) ||
                    h.Email.Contains(search));

            ViewBag.TotalHospitals = _db.Hospitals.Count();
            ViewBag.ActiveHospitals = _db.Hospitals.Count(h => h.Status == "Active");
            ViewBag.TotalCities = _db.Hospitals
                                         .Where(h => h.Address != null && h.Address != "")
                                         .Select(h => h.Address)
                                         .Distinct()
                                         .Count();
            ViewBag.Search = search;

            return View(query.OrderBy(h => h.Name).ToList());
        }

        [HttpGet]
        public IActionResult AddHospital() => View(new Hospital());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddHospital(Hospital model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (_db.Hospitals.Any(h => h.Email == model.Email))
            {
                ModelState.AddModelError("Email", "A hospital with this email already exists.");
                return View(model);
            }

            model.Status = string.IsNullOrEmpty(model.Status) ? "Active" : model.Status;

            _db.Hospitals.Add(model);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Hospital added successfully!";
            return RedirectToAction("ManageHospital");
        }

        [HttpGet]
        public IActionResult EditHospital(int id)
        {
            var hospital = _db.Hospitals.Find(id);
            if (hospital == null) return NotFound();
            return View(hospital);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditHospital(Hospital model)
        {
            var hospital = _db.Hospitals.Find(model.Id);
            if (hospital == null) return NotFound();

            if (_db.Hospitals.Any(h => h.Email == model.Email && h.Id != model.Id))
            {
                ModelState.AddModelError("Email", "A hospital with this email already exists.");
                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            hospital.Name = model.Name;
            hospital.Email = model.Email;
            hospital.Contact = model.Contact;
            hospital.Address = model.Address;
            hospital.Status = model.Status;

            _db.SaveChanges();
            TempData["SuccessMessage"] = "Hospital updated successfully!";
            return RedirectToAction("ManageHospital");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteHospital(int id)
        {
            var hospital = _db.Hospitals.Find(id);
            if (hospital != null)
            {
                _db.Hospitals.Remove(hospital);
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Hospital deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Hospital not found.";
            }
            return RedirectToAction("ManageHospital");
        }


        // ─── OTHER PAGES ──────────────────────────────────────
        public new IActionResult User() => View();
        public IActionResult Staff() => View();
        public IActionResult Privacy() => View();
        public IActionResult Admin() => View();
        public IActionResult AddCollection() => View();
        public IActionResult TrackRequest() => View();
        public IActionResult ViewCollection() => View();
        public IActionResult ViewDonation() => View();
        public IActionResult ManageBloodStock() => View();
        public IActionResult CheckRequest() => View();
        public IActionResult AdminReports()
        {
            var donors = _db.DonateBloods
                .OrderByDescending(d => d.CreatedAt)
                .ToList();

            ViewBag.DonorList = donors;
            ViewBag.TotalDonors = donors.Count;
            ViewBag.PendingRequests = _db.BloodRequests.Count(r => r.Status == "Pending");
            ViewBag.ActiveStaff = _db.Staffs.Count(s => s.Status == "Active");
            ViewBag.TotalHospitals = _db.Hospitals.Count();

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    }
}