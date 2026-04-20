using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace YourProjectName.Models
{
    public class RequestBlood
    {
        public int Id { get; set; }

        [Required]
        public string PatientName { get; set; }

        [Required]
        public string BloodGroupNeeded { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime RequiredDate { get; set; }

        [Required]
        public string HospitalLocation { get; set; }

        [Required]
        [RegularExpression(@"^01[3-9]\d{8}$")]
        public string ContactNumber { get; set; }

        public string? Notes { get; set; }

        // Payment
        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        public string? MobilePaymentNumber { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalExpenseAmount { get; set; } = 165;

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public RequestStatus Status { get; set; } = RequestStatus.Pending;
    }

    public enum RequestStatus
    {
        Pending,
        Approved,
        Cancelled,
        Completed
    }

    public enum PaymentMethod
    {
        BKash,
        Nagad,
        Rocket,
        CashOnSite,
        BankTransfer
    }

    public enum PaymentStatus
    {
        Unpaid,
        Paid
    }
}