using BBMS.Data;
using BBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Rotativa.AspNetCore;
using YourApp.Models;

namespace BBMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(AppDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ─── HOME PAGE ────────────────────────────────────────
        public IActionResult Index()
        {
            var bloodStocks = _db.BloodStocks.OrderBy(s => s.BloodGroup).ToList();
            ViewBag.TotalDonors = _db.DonateBloods.Count();
            ViewBag.TotalGroups = _db.BloodStocks.Count();
            return View(bloodStocks);
        }

        // ─── DONATE ──────────────────────────────────────────
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

        // ─── BLOOD REQUEST POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestBlood(BloodRequest model)
        {
            if (!ModelState.IsValid)
                return View(model);

            int count = _db.BloodRequests.Count();
            model.InvoiceNumber = "INV-" + (count + 1).ToString("D6");
            model.RequestDate = DateTime.Now;
            model.Status = "Pending";

            if (model.Quantity <= 0)
                model.Quantity = 1;

            model.TotalCost = model.Quantity * 165;

            // ── Save the logged-in user's ID if authenticated ──
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var identityUser = await _userManager.GetUserAsync(User);
                if (identityUser != null)
                    model.IdentityUserId = identityUser.Id;
            }

            _db.BloodRequests.Add(model);
            await _db.SaveChangesAsync();

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
        [Authorize]
        public async Task<IActionResult> BloodRequests(string search, string status)
        {
            IQueryable<BloodRequest> query = _db.BloodRequests.AsQueryable();

            // Regular users only see their own requests by IdentityUserId
            if (User.IsInRole("User"))
            {
                var identityUser = await _userManager.GetUserAsync(User);
                query = query.Where(r => r.IdentityUserId == identityUser.Id);
            }

            // Admin/Staff can search and filter
            if (!User.IsInRole("User"))
            {
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(r =>
                        r.RequesterName.Contains(search) ||
                        r.BloodGroup.Contains(search));

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(r => r.Status == status);
            }

            var list = query.OrderByDescending(r => r.RequestDate).ToList();

            ViewBag.TotalRequests = list.Count;
            ViewBag.Pending = list.Count(r => r.Status == "Pending");
            ViewBag.Approved = list.Count(r => r.Status == "Approved");
            ViewBag.Rejected = list.Count(r => r.Status == "Rejected");
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.IsAdminOrStaff = User.IsInRole("Admin") || User.IsInRole("Staff");

            return View(list);
        }

        // ─── APPROVE REQUEST ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult ApproveRequest(int id)
        {
            var request = _db.BloodRequests.Find(id);
            if (request == null)
                return RedirectToAction("BloodRequests");

            request.Status = "Approved";

            // ── Find matching available bags (FEFO) but DON'T mark used yet ──
            var availableBags = _db.BloodBags
                .Where(b => b.BloodGroup == request.BloodGroup
                         && b.Status == "Available"
                         && b.ExpiryDate >= DateTime.Today)
                .OrderBy(b => b.ExpiryDate)
                .Take(request.Quantity)
                .ToList();

            // ── Reserve bags (not Used yet — just noted in allocation) ──
            var bagCodes = availableBags.Any()
                ? string.Join(", ", availableBags.Select(b => b.BagCode))
                : "No bags available";

            // ── Create BloodAllocation record ──
            var allocation = new BloodAllocation
            {
                BloodRequestId = request.Id,
                InvoiceNumber = request.InvoiceNumber,
                RequesterName = request.RequesterName,
                BloodGroup = request.BloodGroup,
                Hospital = request.Hospital ?? "—",
                ContactNumber = request.ContactNumber,
                AllocatedBagCodes = bagCodes,
                QuantityAllocated = availableBags.Count,
                Status = "Ready for Delivery",
                AllocatedAt = DateTime.Now
            };

            _db.BloodAllocations.Add(allocation);

            if (availableBags.Count < request.Quantity)
            {
                TempData["WarningMessage"] =
                    $"Approved but only {availableBags.Count} of " +
                    $"{request.Quantity} bag(s) available for {request.BloodGroup}.";
            }
            else
            {
                TempData["SuccessMessage"] =
                    $"Request approved. {availableBags.Count} bag(s) reserved. " +
                    $"Stock will reduce after delivery confirmation.";
            }

            _db.SaveChanges();
            return RedirectToAction("BloodRequests");
        }

        // ─── REQUEST REFUND (User) ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public IActionResult RequestRefund(int id)
        {
            var r = _db.BloodRequests.Find(id);
            if (r != null && r.Status == "Rejected" && r.RefundStatus == null)
            {
                r.RefundStatus = "Requested";
                r.RefundRequestedAt = DateTime.Now;
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Refund requested. Staff will process it shortly.";
            }
            return RedirectToAction("BloodRequests");
        }

        // ─── PROCESS REFUND (Admin/Staff) ─────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult ProcessRefund(int id)
        {
            var r = _db.BloodRequests.Find(id);
            if (r != null && r.RefundStatus == "Requested")
            {
                r.RefundStatus = "Processed";
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Refund marked as processed.";
            }
            return RedirectToAction("BloodRequests");
        }

        // ─── EDIT REQUEST ─────────────────────────────────────
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
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
        [Authorize(Roles = "Admin,Staff")]
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

        // ─── DELETE REQUEST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
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

        // ─── DOWNLOAD INVOICE ─────────────────────────────────
        [HttpGet]
        [Authorize]
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


        // ─── BLOOD DISTRIBUTION ───────────────────────────────

        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult BloodDistribution(string search, string status)
        {
            var query = _db.BloodAllocations.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(a =>
                    a.InvoiceNumber.Contains(search) ||
                    a.RequesterName.Contains(search) ||
                    a.BloodGroup.Contains(search) ||
                    a.AllocatedBagCodes.Contains(search));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);

            var list = query.OrderByDescending(a => a.AllocatedAt).ToList();

            ViewBag.TotalAllocations = _db.BloodAllocations.Count();
            ViewBag.ReadyCount = _db.BloodAllocations
                                        .Count(a => a.Status == "Ready for Delivery");
            ViewBag.DeliveredCount = _db.BloodAllocations
                                        .Count(a => a.Status == "Delivered");
            ViewBag.CancelledCount = _db.BloodAllocations
                                        .Count(a => a.Status == "Cancelled");

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> MarkDelivered(int id, string? notes)
        {
            var allocation = _db.BloodAllocations.Find(id);
            if (allocation == null || allocation.Status != "Ready for Delivery")
                return RedirectToAction("BloodDistribution");

            // ── Auto-detect logged-in staff name ──
            var identityUser = await _userManager.GetUserAsync(User);
            var staffName = _db.Staffs
                .FirstOrDefault(s => s.IdentityUserId == identityUser.Id)?.Name
                ?? identityUser.Email;

            allocation.Status = "Delivered";
            allocation.DeliveredAt = DateTime.Now;
            allocation.DeliveredBy = staffName;
            allocation.Notes = notes;

            // ── Mark each allocated bag as Used ──
            if (!string.IsNullOrEmpty(allocation.AllocatedBagCodes) &&
                allocation.AllocatedBagCodes != "No bags available")
            {
                var bagCodes = allocation.AllocatedBagCodes
                    .Split(',')
                    .Select(c => c.Trim())
                    .ToList();

                var bagsToUse = _db.BloodBags
                    .Where(b => bagCodes.Contains(b.BagCode) && b.Status == "Available")
                    .ToList();

                foreach (var bag in bagsToUse)
                    bag.Status = "Used";

                _db.SaveChanges();

                // ── Recount and update BloodStock ──
                var stock = _db.BloodStocks
                    .FirstOrDefault(s => s.BloodGroup == allocation.BloodGroup);

                if (stock != null)
                {
                    stock.Units = _db.BloodBags
                        .Count(b => b.BloodGroup == allocation.BloodGroup
                                 && b.Status == "Available");

                    stock.Status = stock.Units >= 30 ? "sufficient"
                                 : stock.Units >= 15 ? "low"
                                 : "critical";

                    stock.LastUpdated = DateTime.Now;
                }
            }

            _db.SaveChanges();

            TempData["SuccessMessage"] =
                $"Invoice {allocation.InvoiceNumber} marked as delivered by {staffName}. " +
                $"Stock reduced by {allocation.QuantityAllocated} unit(s).";

            return RedirectToAction("BloodDistribution");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult CancelAllocation(int id)
        {
            var allocation = _db.BloodAllocations.Find(id);
            if (allocation != null && allocation.Status == "Ready for Delivery")
            {
                allocation.Status = "Cancelled";
                _db.SaveChanges();
                TempData["SuccessMessage"] =
                    $"Allocation for {allocation.InvoiceNumber} cancelled.";
            }
            return RedirectToAction("BloodDistribution");
        }

        // ═══════════════════════════════════════════════════════
        // ─── STAFF CRUD ───────────────────────────────────────
        // ═══════════════════════════════════════════════════════

        [HttpGet]
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        public IActionResult AddStaff() => View(new Staff());

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddStaff(
            Staff model,
            string Password,
            [FromServices] UserManager<IdentityUser> userManager,
            [FromServices] RoleManager<IdentityRole> roleManager)
        {
            ModelState.Remove("Password");
            ModelState.Remove("IdentityUserId");

            if (!ModelState.IsValid) return View(model);

            if (_db.Staffs.Any(s => s.Email == model.Email))
            {
                ModelState.AddModelError("Email", "A staff member with this email already exists.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
            {
                ModelState.AddModelError("", "Password must be at least 6 characters.");
                return View(model);
            }

            if (model.Role != "Admin" && model.Role != "Staff")
            {
                ModelState.AddModelError("Role", "Role must be Admin or Staff.");
                return View(model);
            }

            var identityUser = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await userManager.CreateAsync(identityUser, Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            if (!await roleManager.RoleExistsAsync(model.Role))
                await roleManager.CreateAsync(new IdentityRole(model.Role));

            await userManager.AddToRoleAsync(identityUser, model.Role);

            model.IdentityUserId = identityUser.Id;

            if (model.JoinDate == default)
                model.JoinDate = DateTime.Now;

            _db.Staffs.Add(model);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Staff member added successfully!";
            return RedirectToAction("ManageStaff");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult EditStaff(int id)
        {
            var staff = _db.Staffs.Find(id);
            if (staff == null) return NotFound();
            return View(staff);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditStaff(
            Staff model,
            string? NewPassword,
            [FromServices] UserManager<IdentityUser> userManager,
            [FromServices] RoleManager<IdentityRole> roleManager)
        {
            ModelState.Remove("NewPassword");
            ModelState.Remove("IdentityUserId");

            if (!ModelState.IsValid) return View(model);

            var staff = _db.Staffs.Find(model.Id);
            if (staff == null) return NotFound();

            if (_db.Staffs.Any(s => s.Email == model.Email && s.Id != model.Id))
            {
                ModelState.AddModelError("Email", "Another staff member already has this email.");
                return View(model);
            }

            if (model.Role != "Admin" && model.Role != "Staff")
            {
                ModelState.AddModelError("Role", "Role must be Admin or Staff.");
                return View(model);
            }

            string oldRole = staff.Role;
            staff.Name = model.Name;
            staff.Email = model.Email;
            staff.Contact = model.Contact;
            staff.Role = model.Role;
            staff.Status = model.Status;

            if (!string.IsNullOrEmpty(staff.IdentityUserId))
            {
                var identityUser = await userManager.FindByIdAsync(staff.IdentityUserId);
                if (identityUser != null)
                {
                    if (identityUser.Email != model.Email)
                    {
                        identityUser.UserName = model.Email;
                        identityUser.Email = model.Email;
                        await userManager.UpdateAsync(identityUser);
                    }

                    if (oldRole != model.Role)
                    {
                        await userManager.RemoveFromRoleAsync(identityUser, oldRole);

                        if (!await roleManager.RoleExistsAsync(model.Role))
                            await roleManager.CreateAsync(new IdentityRole(model.Role));

                        await userManager.AddToRoleAsync(identityUser, model.Role);
                    }

                    if (!string.IsNullOrWhiteSpace(NewPassword) && NewPassword.Length >= 6)
                    {
                        var token = await userManager.GeneratePasswordResetTokenAsync(identityUser);
                        await userManager.ResetPasswordAsync(identityUser, token, NewPassword);
                    }
                }
            }

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Staff member updated successfully!";
            return RedirectToAction("ManageStaff");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteStaff(
            int id,
            [FromServices] UserManager<IdentityUser> userManager)
        {
            var staff = _db.Staffs.Find(id);
            if (staff != null)
            {
                if (!string.IsNullOrEmpty(staff.IdentityUserId))
                {
                    var identityUser = await userManager.FindByIdAsync(staff.IdentityUserId);
                    if (identityUser != null)
                        await userManager.DeleteAsync(identityUser);
                }

                _db.Staffs.Remove(staff);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Staff member deleted.";
            }
            return RedirectToAction("ManageStaff");
        }

        // ─── VIEW COLLECTION ──────────────────────────────────
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult ViewCollection()
        {
            var donors = _db.DonateBloods
                .OrderByDescending(d => d.CreatedAt)
                .ToList();
            return View(donors);
        }

        // ─── STAFF DASHBOARD ──────────────────────────────────
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult Staff()
        {
            var recentDonations = _db.RecordDonations
                .OrderByDescending(d => d.DonationDate)
                .Take(10)
                .ToList();

            ViewBag.RecentDonations = recentDonations;
            return View();
        }
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult ManageBloodBags(string search, string group, string status, string component)
        {
            // ── Auto-expire bags ──
            var now = DateTime.Now;
            var today = DateTime.Today;
            var sevenDaysLater = today.AddDays(7);

            var expiredBags = _db.BloodBags
                .Where(b => b.Status == "Available" && b.ExpiryDate < now)
                .ToList();

            foreach (var bag in expiredBags)
                bag.Status = "Expired";

            if (expiredBags.Any())
            {
                _db.SaveChanges();

                var affectedGroups = expiredBags.Select(b => b.BloodGroup).Distinct();
                foreach (var grp in affectedGroups)
                {
                    var stock = _db.BloodStocks.FirstOrDefault(s => s.BloodGroup == grp);
                    if (stock != null)
                    {
                        stock.Units = _db.BloodBags
                            .Count(b => b.BloodGroup == grp && b.Status == "Available");
                        stock.Status = stock.Units >= 30 ? "sufficient"
                                     : stock.Units >= 15 ? "low" : "critical";
                        stock.LastUpdated = today;
                    }
                }
                _db.SaveChanges();
            }

            // ── Query ──
            var query = _db.BloodBags.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(b => b.BagCode.Contains(search)
                                      || b.BloodGroup.Contains(search)
                                      || b.DonorId.Contains(search));

            if (!string.IsNullOrEmpty(group))
                query = query.Where(b => b.BloodGroup == group);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.Status == status);

            if (!string.IsNullOrEmpty(component))
                query = query.Where(b => b.Component == component);

            var list = query.OrderBy(b => b.ExpiryDate).ToList();

            // ── ViewBag stats — all calculated with plain DateTime comparisons ──
            ViewBag.TotalBags = _db.BloodBags.Count();
            ViewBag.Available = _db.BloodBags.Count(b => b.Status == "Available");
            ViewBag.ExpiringSoon = _db.BloodBags.Count(b => b.Status == "Available"
                                        && b.ExpiryDate >= today
                                        && b.ExpiryDate <= sevenDaysLater);
            ViewBag.ExpiredCount = _db.BloodBags.Count(b => b.Status == "Expired");
            ViewBag.UsedCount = _db.BloodBags.Count(b => b.Status == "Used");
            ViewBag.AutoExpired = expiredBags.Count;

            return View(list);
        }

        // ═══════════════════════════════════════════════════════
        // ─── HOSPITAL CRUD ────────────────────────────────────
        // ═══════════════════════════════════════════════════════

        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult ManageHospital(string search, string type, string status)
        {
            var query = _db.Hospitals.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(h =>
                    h.Name.Contains(search) ||
                    h.Location.Contains(search) ||
                    h.ContactPerson.Contains(search) ||
                    h.Phone.Contains(search));

            if (!string.IsNullOrEmpty(type))
                query = query.Where(h => h.Type == type);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(h => h.Status == status);

            ViewBag.TotalHospitals = _db.Hospitals.Count();
            ViewBag.ActiveHospitals = _db.Hospitals.Count(h => h.Status == "Active");
            ViewBag.PendingHospitals = _db.Hospitals.Count(h => h.Status == "Pending Approval");
            ViewBag.TotalUnits = _db.Hospitals.Sum(h => (int?)h.UnitsSupplied) ?? 0;
            ViewBag.TotalRequests = _db.Hospitals.Sum(h => (int?)h.Requests) ?? 0;
            ViewBag.Search = search;
            ViewBag.Type = type;
            ViewBag.Status = status;

            return View(query.OrderBy(h => h.Name).ToList());
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult AddHospital() => View(new Hospital());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult AddHospital(Hospital model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (!string.IsNullOrEmpty(model.Email) &&
                _db.Hospitals.Any(h => h.Email == model.Email))
            {
                ModelState.AddModelError("Email", "A hospital with this email already exists.");
                return View(model);
            }

            int count = _db.Hospitals.Count();
            model.HospitalCode = "HSP-" + (count + 1).ToString("D4");
            model.CreatedAt = DateTime.Now;
            model.Status = string.IsNullOrEmpty(model.Status) ? "Active" : model.Status;

            _db.Hospitals.Add(model);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Hospital added successfully!";
            return RedirectToAction("ManageHospital");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult EditHospital(int id)
        {
            var hospital = _db.Hospitals.Find(id);
            if (hospital == null) return NotFound();
            return View(hospital);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult EditHospital(Hospital model)
        {
            var hospital = _db.Hospitals.Find(model.Id);
            if (hospital == null) return NotFound();

            if (!string.IsNullOrEmpty(model.Email) &&
                _db.Hospitals.Any(h => h.Email == model.Email && h.Id != model.Id))
            {
                ModelState.AddModelError("Email", "A hospital with this email already exists.");
                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            hospital.Name = model.Name;
            hospital.Type = model.Type;
            hospital.Location = model.Location;
            hospital.ContactPerson = model.ContactPerson;
            hospital.Phone = model.Phone;
            hospital.Email = model.Email;
            hospital.Status = model.Status;
            hospital.Requests = model.Requests;
            hospital.UnitsSupplied = model.UnitsSupplied;
            hospital.Notes = model.Notes;

            _db.SaveChanges();
            TempData["SuccessMessage"] = "Hospital updated successfully!";
            return RedirectToAction("ManageHospital");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
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

        // ─── BLOOD STOCK ──────────────────────────────────────

        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult AddBloodStock() => View(new BloodStock());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult AddBloodStock(BloodStock model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // ── 1. Check if stock entry already exists for this blood group ──
            var existing = _db.BloodStocks
                .FirstOrDefault(s => s.BloodGroup == model.BloodGroup);

            if (existing != null)
            {
                TempData["ErrorMessage"] =
                    $"A stock entry for {model.BloodGroup} already exists. " +
                    $"Use Edit to update it.";
                return RedirectToAction("ManageBloodStock");
            }

            // ── 2. Create one BloodBag per unit entered ──
            for (int i = 0; i < model.Units; i++)
            {
                int bagCount = _db.BloodBags.Count();
                string bagCode = "BAG-" + (bagCount + 1).ToString("D4");

                _db.BloodBags.Add(new BloodBag
                {
                    BagCode = bagCode,
                    BloodGroup = model.BloodGroup,
                    Component = "Whole Blood",
                    VolumeML = 450,
                    CollectionDate = model.CollectionDate,
                    ExpiryDate = model.CollectionDate.AddDays(42),
                    DonorId = "MANUAL",
                    Status = "Available",
                    Notes = "Manually added by admin",
                    CreatedAt = DateTime.Now
                });

                // Save after each bag so Count() is always accurate for BagCode
                _db.SaveChanges();
            }

            // ── 3. Recount units from actual bags (single source of truth) ──
            int actualUnits = _db.BloodBags
                .Count(b => b.BloodGroup == model.BloodGroup
                         && b.Status == "Available");

            // ── 4. Save BloodStock with real count ──
            model.Units = actualUnits;
            model.Status = actualUnits >= 30 ? "sufficient"
                              : actualUnits >= 15 ? "low"
                              : "critical";
            model.LastUpdated = DateTime.Now;

            _db.BloodStocks.Add(model);
            _db.SaveChanges();

            TempData["SuccessMessage"] =
                $"{actualUnits} bag(s) created for {model.BloodGroup}. " +
                $"Stock entry added successfully!";
            return RedirectToAction("ManageBloodStock");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult ManageBloodStock(string search, string status)
        {
            var query = _db.BloodStocks.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s => s.BloodGroup.Contains(search));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);

            var list = query.OrderBy(s => s.BloodGroup).ToList();

            ViewBag.TotalUnits = _db.BloodStocks.Sum(s => (int?)s.Units) ?? 0;
            ViewBag.TotalGroups = _db.BloodStocks.Count();
            ViewBag.CriticalCount = _db.BloodStocks.Count(s => s.Status == "critical");
            ViewBag.SufficientCount = _db.BloodStocks.Count(s => s.Status == "sufficient");
            ViewBag.LowCount = _db.BloodStocks.Count(s => s.Status == "low");
            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult UpdateBloodStock(int id, int units, string status)
        {
            var stock = _db.BloodStocks.Find(id);
            if (stock == null)
                return RedirectToAction("ManageBloodStock");

            // ── How many available bags exist right now for this group ──
            int currentBags = _db.BloodBags
                .Count(b => b.BloodGroup == stock.BloodGroup
                         && b.Status == "Available");

            int difference = units - currentBags;

            if (difference > 0)
            {
                // ── Admin increased units — create extra bags ──
                for (int i = 0; i < difference; i++)
                {
                    int bagCount = _db.BloodBags.Count();
                    string bagCode = "BAG-" + (bagCount + 1).ToString("D4");

                    _db.BloodBags.Add(new BloodBag
                    {
                        BagCode = bagCode,
                        BloodGroup = stock.BloodGroup,
                        Component = "Whole Blood",
                        VolumeML = 450,
                        CollectionDate = DateTime.Today,
                        ExpiryDate = DateTime.Today.AddDays(42),
                        DonorId = "MANUAL",
                        Status = "Available",
                        Notes = "Manually adjusted by admin",
                        CreatedAt = DateTime.Now
                    });

                    _db.SaveChanges();
                }
            }
            else if (difference < 0)
            {
                // ── Admin decreased units — discard excess bags (oldest first) ──
                int toRemove = Math.Abs(difference);

                var bagsToDiscard = _db.BloodBags
                    .Where(b => b.BloodGroup == stock.BloodGroup
                             && b.Status == "Available"
                             && b.DonorId == "MANUAL")
                    .OrderBy(b => b.CollectionDate)
                    .Take(toRemove)
                    .ToList();

                foreach (var bag in bagsToDiscard)
                    bag.Status = "Discarded";

                _db.SaveChanges();
            }

            // ── Recount from actual bags — never trust the typed number ──
            int actualUnits = _db.BloodBags
                .Count(b => b.BloodGroup == stock.BloodGroup
                         && b.Status == "Available");

            stock.Units = actualUnits;
            stock.Status = actualUnits >= 30 ? "sufficient"
                              : actualUnits >= 15 ? "low"
                              : "critical";
            stock.LastUpdated = DateTime.Now;

            _db.SaveChanges();

            TempData["SuccessMessage"] =
                $"Stock updated! {actualUnits} available bag(s) for {stock.BloodGroup}.";
            return RedirectToAction("ManageBloodStock");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult DeleteBloodStock(int id)
        {
            var stock = _db.BloodStocks.Find(id);
            if (stock != null)
            {
                // ── Also discard all MANUAL bags for this group ──
                var manualBags = _db.BloodBags
                    .Where(b => b.BloodGroup == stock.BloodGroup
                             && b.Status == "Available"
                             && b.DonorId == "MANUAL")
                    .ToList();

                foreach (var bag in manualBags)
                    bag.Status = "Discarded";

                _db.BloodStocks.Remove(stock);
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Blood stock and associated manual bags deleted!";
            }
            return RedirectToAction("ManageBloodStock");
        }

        // ─── USER DASHBOARD ───────────────────────────────────
        [Authorize(Roles = "User")]
        public async Task<IActionResult> UserDashboard()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return RedirectToAction("LogIn", "Auth");

            var profile = _db.UserProfiles
                .FirstOrDefault(p => p.IdentityUserId == identityUser.Id);

            if (profile == null)
                return RedirectToAction("LogIn", "Auth");

            var myRequests = _db.BloodRequests
                .Where(r => r.IdentityUserId == identityUser.Id)
                .OrderByDescending(r => r.RequestDate)
                .Take(5)
                .ToList();

            var myDonations = _db.DonateBloods
                .OrderByDescending(d => d.CreatedAt)
                .Take(5)
                .ToList();

            var lastDonation = myDonations.FirstOrDefault();

            var viewModel = new UserDashboardViewModel
            {
                Name = profile.Name,
                BloodGroup = profile.BloodGroup,
                DonorStatus = profile.DonorStatus,
                Email = identityUser.Email ?? string.Empty,
                Phone = profile.Phone,
                Address = profile.Address,
                TotalDonations = myDonations.Count,
                LastDonationDate = lastDonation?.CreatedAt.ToString("dd-MM-yyyy") ?? "N/A",
                MyRequests = myRequests,
                MyDonations = myDonations
            };

            return View(viewModel);
        }

        // ─── OTHER PAGES ──────────────────────────────────────
        public IActionResult Privacy() => View();
        public IActionResult AddCollection() => View();
        public IActionResult ViewDonation() => View();
        public IActionResult CheckRequest() => View();

        // ─── ADMIN DASHBOARD ──────────────────────────────────
        [Authorize(Roles = "Admin")]
        public IActionResult Admin()
        {
            var bloodStocks = _db.BloodStocks
                .OrderBy(s => s.BloodGroup)
                .ToList();

            var today = DateTime.Today;
            var sevenDaysLater = today.AddDays(7);
            var threeDaysLater = today.AddDays(3);

            // Per-group bag summary for the dashboard table
            var bagSummary = _db.BloodBags
                .Where(b => b.Status == "Available")
                .GroupBy(b => b.BloodGroup)
                .Select(g => new
                {
                    BloodGroup = g.Key,
                    TotalBags = g.Count(),
                    ExpiringSoon = g.Count(b => b.ExpiryDate >= today
                                             && b.ExpiryDate <= sevenDaysLater),
                    Critical = g.Count(b => b.ExpiryDate >= today
                                             && b.ExpiryDate <= threeDaysLater),
                    NearestExpiry = g.Min(b => b.ExpiryDate)
                })
                .ToList();

            ViewBag.BagSummary = bagSummary;
            ViewBag.TotalExpiringSoon = _db.BloodBags.Count(b =>
                b.Status == "Available" &&
                b.ExpiryDate >= today &&
                b.ExpiryDate <= sevenDaysLater);
            ViewBag.TotalDonors = _db.DonateBloods.Count();
            ViewBag.TotalRequests = _db.BloodRequests.Count();
            ViewBag.PendingRequests = _db.BloodRequests.Count(r => r.Status == "Pending");
           

            ViewBag.CriticalBags = _db.BloodBags.Count(b =>
                b.Status == "Available" &&
                b.ExpiryDate >= today &&
                b.ExpiryDate <= threeDaysLater);

            ViewBag.ExpiredUnhandled = _db.BloodBags.Count(b =>
                b.Status == "Expired");
            return View(bloodStocks);
        }

        // ─── ADMIN REPORTS ────────────────────────────────────
        [Authorize(Roles = "Admin")]
        public IActionResult AdminReports()
        {
            var donors = _db.DonateBloods.OrderByDescending(d => d.CreatedAt).ToList();
            var bloodStocks = _db.BloodStocks.OrderBy(s => s.BloodGroup).ToList();
            var bloodRequests = _db.BloodRequests.OrderByDescending(r => r.RequestDate).ToList();
            var staffList = _db.Staffs.OrderByDescending(s => s.JoinDate).ToList();
            var hospitalList = _db.Hospitals.OrderBy(h => h.Name).ToList();

          
            // With this:
            ViewBag.BloodBagList = _db.BloodBags
                .OrderBy(b => b.ExpiryDate)
                .ToList();
            ViewBag.DonorList = donors;
            ViewBag.BloodStockList = bloodStocks;
            ViewBag.BloodRequestList = bloodRequests;
            ViewBag.StaffList = staffList;
            ViewBag.HospitalList = hospitalList;

            ViewBag.TotalDonors = donors.Count;
            ViewBag.TotalBloodUnits = bloodStocks.Sum(s => s.Units);
            ViewBag.PendingRequests = _db.BloodRequests.Count(r => r.Status == "Pending");
            ViewBag.ActiveStaff = _db.Staffs.Count(s => s.Status == "Active");
            ViewBag.TotalHospitals = _db.Hospitals.Count();

            return View();
        }
    }
}