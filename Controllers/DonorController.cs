// Controllers/DonorController.cs
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

            // Load all from DB first (needed for computed Eligibility property)
            var allDonors = _db.DonateBloods.ToList();

            // Apply filters
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

            // Paginate
            var donors = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Stats
            ViewBag.TotalDonors = allDonors.Count;
            ViewBag.EligibleDonors = allDonors.Count(d => d.Eligibility == "Eligible");
            ViewBag.DeferredDonors = allDonors.Count(d => d.Eligibility == "Deferred");

            // Pagination
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.From = total == 0 ? 0 : (page - 1) * pageSize + 1;
            ViewBag.To = Math.Min(page * pageSize, total);

            // Filter state
            ViewBag.Search = search;
            ViewBag.BloodGroup = bloodGroup;
            ViewBag.Eligibility = eligibility;

            return View(donors);
        }

        public IActionResult DeleteDonor(string id)
        {
            var donor = _db.DonateBloods.FirstOrDefault(d => d.DonorId == id);
            if (donor != null)
            {
                _db.DonateBloods.Remove(donor);
                _db.SaveChanges();
            }
            return RedirectToAction("ManageDonors");
        }

        public IActionResult DonorDetails(string id)
        {
            var donor = _db.DonateBloods.FirstOrDefault(d => d.DonorId == id);
            if (donor == null)
                return NotFound();

            return View(donor);
        }

        public IActionResult EditDonor(string id)
        {
            var donor = _db.DonateBloods.FirstOrDefault(d => d.DonorId == id);
            if (donor == null)
                return NotFound();

            return View(donor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditDonor(DonateBlood donor)
        {
            if (ModelState.IsValid)
            {
                _db.DonateBloods.Update(donor);
                _db.SaveChanges();
                return RedirectToAction("ManageDonors");
            }
            return View(donor);
        }

    }
}