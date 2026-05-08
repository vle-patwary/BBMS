using BBMS.Models;
using YourApp.Models;

namespace BBMS.Models
{
    public class UserDashboardViewModel
    {
        public string Name { get; set; }
        public string BloodGroup { get; set; }
        public string DonorStatus { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public int TotalDonations { get; set; }
        public string LastDonationDate { get; set; }
        public List<BloodRequest> MyRequests { get; set; } = new();
        public List<DonateBlood> MyDonations { get; set; } = new();
    }
}