// Controllers/DonationController.cs
using BBMS.Data;
using BBMS.Models;
using Microsoft.AspNetCore.Mvc;

namespace BBMS.Controllers
{
    public class DonationController : Controller
    {
        private readonly AppDbContext _db;

        public DonationController(AppDbContext db)
        {
            _db = db;
        }

        // ─── RECORD DONATION (Donor List) ─────────────────────
        public IActionResult RecordDonation()
        {
            var donors = _db.DonateBloods
                .OrderByDescending(d => d.CreatedAt)
                .ToList();

            return View(donors);
        }

        // ─── CREATE RECORD GET (Pre-filled Form) ──────────────
        public IActionResult CreateRecord(string donorId)
        {
            var donor = _db.DonateBloods
                .FirstOrDefault(d => d.DonorId == donorId);

            if (donor == null)
                return NotFound();

            var record = new RecordDonation
            {
                DonorId = donor.DonorId,
                DonorName = donor.FullName,
                Gender = donor.Gender,
                ContactNumber = donor.ContactNumber,
                Email = donor.Email,
                BloodGroup = donor.BloodGroup,
                DonationDate = DateTime.Now
            };

            return View(record);
        }

        // ─── CREATE RECORD POST (Save to DB) ──────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateRecord(RecordDonation record)
        {
            if (ModelState.IsValid)
            {
                record.CreatedAt = DateTime.Now;
                _db.RecordDonations.Add(record);

                // Update LastDonationDate and TotalDonations in DonateBloods table
                var donor = _db.DonateBloods
                    .FirstOrDefault(d => d.DonorId == record.DonorId);

                if (donor != null)
                {
                    donor.LastDonationDate = record.DonationDate;
                    donor.TotalDonations += 1; // ← increment total donations
                    _db.DonateBloods.Update(donor);
                }

                _db.SaveChanges();
                return RedirectToAction("DonationHistory");
            }
            return View(record);
        }

        // ─── DONATION HISTORY ─────────────────────────────────
        public IActionResult DonationHistory(
            string search, string bloodGroup,
            string status, int page = 1)
        {
            int pageSize = 10;

            var all = _db.RecordDonations.ToList();

            var query = all.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(r =>
                    r.DonorName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    r.DonorId.Contains(search));

            if (!string.IsNullOrEmpty(bloodGroup))
                query = query.Where(r => r.BloodGroup == bloodGroup);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            var filtered = query
                .OrderByDescending(r => r.DonationDate)
                .ToList();

            int total = filtered.Count;

            var records = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Stats
            ViewBag.TotalRecords = all.Count;
            ViewBag.Available = all.Count(r => r.Status == "Available");
            ViewBag.Processing = all.Count(r => r.Status == "Processing");
            ViewBag.TotalML = all.Sum(r => r.Quantity);

            // Pagination
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.From = total == 0 ? 0 : (page - 1) * pageSize + 1;
            ViewBag.To = Math.Min(page * pageSize, total);

            // Filter state
            ViewBag.Search = search;
            ViewBag.BloodGroup = bloodGroup;
            ViewBag.Status = status;

            return View(records);
        }
    }
}