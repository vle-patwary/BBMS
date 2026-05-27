// Controllers/DonationController.cs
using BBMS.Data;
using BBMS.Models;
using Microsoft.AspNetCore.Authorization;
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
        // record donation----connected with blood bag
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> CreateRecord(RecordDonation model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // ── 1. Set metadata ──
            model.CreatedAt = DateTime.Now;

            // ── 2. Calculate expiry ──
            DateTime expiryDate = model.DonationType switch
            {
                "Whole Blood" => model.DonationDate.AddDays(42),
                "Red Blood Cells" => model.DonationDate.AddDays(42),
                "Platelets" => model.DonationDate.AddDays(5),
                "Fresh Frozen Plasma" => model.DonationDate.AddMonths(12),
                "Granulocytes" => model.DonationDate.AddHours(24),
                _ => model.DonationDate.AddDays(42)
            };

            // ── 3. Generate BagCode ──
            var lastBag = _db.BloodBags
     .OrderByDescending(b => b.BagCode)
     .FirstOrDefault();

            int nextBagNum = 1;
            if (lastBag != null &&
                lastBag.BagCode.StartsWith("BAG-") &&
                int.TryParse(lastBag.BagCode.Substring(4), out int lastBagNum))
            {
                nextBagNum = lastBagNum + 1;
            }

            string bagCode = "BAG-" + nextBagNum.ToString("D4");

            // ── 4. Create BloodBag ──
            var bag = new BloodBag
            {
                BagCode = bagCode,
                BloodGroup = model.BloodGroup,
                Component = model.DonationType,
                VolumeML = model.Quantity,
                CollectionDate = model.DonationDate,
                ExpiryDate = expiryDate,
                DonorId = model.DonorId,
                Status = "Available",
                Notes = model.Remarks,
                CreatedAt = DateTime.Now
            };

            _db.BloodBags.Add(bag);
            model.BagCode = bagCode;

            // ── 5. Save donation ──
            _db.RecordDonations.Add(model);
            await _db.SaveChangesAsync();

            // ── 5b & 5c. Update DonateBlood + UserProfile ──
            var donateBloodRecord = _db.DonateBloods
                .FirstOrDefault(d => d.DonorId == model.DonorId);

            if (donateBloodRecord != null)
            {
                // Update DonateBlood
                donateBloodRecord.TotalDonations += 1;
                donateBloodRecord.LastDonationDate = model.DonationDate;

                // Update linked UserProfile
                var identityUser = _db.Users
                    .FirstOrDefault(u => u.Email.ToLower() == donateBloodRecord.Email.ToLower());

                if (identityUser != null)
                {
                    var userProfile = _db.UserProfiles
                        .FirstOrDefault(p => p.IdentityUserId == identityUser.Id);

                    if (userProfile != null)
                    {
                        userProfile.LastDonationDate = model.DonationDate;
                        userProfile.DonorStatus = "Not Available";
                    }
                }

                await _db.SaveChangesAsync();
            }

            // ── 6. Update BloodStock ──
            var stock = _db.BloodStocks
                .FirstOrDefault(s => s.BloodGroup == model.BloodGroup);

            if (stock != null)
            {
                stock.Units = _db.BloodBags
                    .Count(b => b.BloodGroup == model.BloodGroup
                             && b.Status == "Available");
                stock.Status = stock.Units >= 30 ? "sufficient"
                             : stock.Units >= 15 ? "low"
                             : "critical";
                stock.LastUpdated = DateTime.Now;
            }
            else
            {
                _db.BloodStocks.Add(new BloodStock
                {
                    BloodGroup = model.BloodGroup,
                    Units = 1,
                    Status = "critical",
                    CollectionDate = model.DonationDate,
                    LastUpdated = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Donation recorded! Bag ID: {bagCode}";
            TempData["DonationType"] = model.DonationType;
            TempData["Quantity"] = model.Quantity;
            return RedirectToAction("CreateRecord", new { donorId = model.DonorId });
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