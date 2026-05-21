using BBMS.Data;
using BBMS.Models;
using Microsoft.AspNetCore.Mvc;

namespace BBMS.Controllers
{
    public class DonorController : Controller
    {
        private readonly AppDbContext _db;

        public DonorController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult ManageDonors(
            string search, string bloodGroup,
            string eligibility, int page = 1)
        {
            int pageSize = 10;
            var allDonors = _db.DonateBloods.ToList();
            var query = allDonors.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(d =>
                    d.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    d.ContactNumber.Contains(search) ||
                    d.DonorId.Contains(search));

            if (!string.IsNullOrEmpty(bloodGroup))
                query = query.Where(d => d.BloodGroup == bloodGroup);

            if (!string.IsNullOrEmpty(eligibility))
                query = query.Where(d => d.Eligibility == eligibility);

            var filtered = query.OrderByDescending(d => d.CreatedAt).ToList();
            int total = filtered.Count;

            var donors = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalDonors = allDonors.Count;
            ViewBag.EligibleDonors = allDonors.Count(d => d.Eligibility == "Eligible");
            ViewBag.DeferredDonors = allDonors.Count(d => d.Eligibility == "Deferred");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.From = total == 0 ? 0 : (page - 1) * pageSize + 1;
            ViewBag.To = Math.Min(page * pageSize, total);
            ViewBag.Search = search;
            ViewBag.BloodGroup = bloodGroup;
            ViewBag.Eligibility = eligibility;

            return View(donors);
        }

        // ─── DELETE ───────────────────────────────────────────
        public IActionResult DeleteDonor(string id)
        {
            var donor = _db.DonateBloods.FirstOrDefault(d => d.DonorId == id);
            if (donor != null)
            {
                _db.DonateBloods.Remove(donor);
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Donor deleted successfully!";
            }
            return RedirectToAction("ManageDonors");
        }

        // ─── DETAILS ──────────────────────────────────────────
        public IActionResult DonorDetails(string id)
        {
            var donor = _db.DonateBloods.FirstOrDefault(d => d.DonorId == id);
            if (donor == null) return NotFound();
            return View(donor);
        }

        // ─── EDIT GET ─────────────────────────────────────────
        [HttpGet]
        public IActionResult EditDonor(string id)
        {
            var donor = _db.DonateBloods.FirstOrDefault(d => d.DonorId == id);
            if (donor == null) return NotFound();
            return View(donor);
        }

        // ─── EDIT POST ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditDonor(string DonorId, string FullName, string Email,
                                       string Gender, string BloodGroup,
                                       string ContactNumber, int Age, string Location)
        {
            // Find existing record by DonorId
            var existing = _db.DonateBloods
                              .FirstOrDefault(d => d.DonorId == DonorId);

            if (existing == null)
            {
                TempData["ErrorMessage"] = $"Donor '{DonorId}' not found.";
                return RedirectToAction("ManageDonors");
            }

            // Update only the fields from the form
            existing.FullName = FullName;
            existing.Email = Email;
            existing.Gender = Gender;
            existing.BloodGroup = BloodGroup;
            existing.ContactNumber = ContactNumber;
            existing.Age = Age;
            existing.Location = Location;

            _db.SaveChanges();

            TempData["SuccessMessage"] = "Donor updated successfully!";
            return RedirectToAction("ManageDonors");
        }
    }
}