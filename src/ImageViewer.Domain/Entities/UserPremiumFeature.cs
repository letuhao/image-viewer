using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// User premium feature entity - represents user subscriptions to premium features
/// </summary>
public class UserPremiumFeature : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

    [BsonElement("premiumFeatureId")]
    public ObjectId PremiumFeatureId { get; private set; }

    [BsonElement("status")]
    public string Status { get; private set; } = "Active"; // Active, Inactive, Suspended, Cancelled, Expired, Trial

    [BsonElement("subscriptionType")]
    public string SubscriptionType { get; private set; } = "Subscription"; // Subscription, Trial, Gift, Promotional

    [BsonElement("startDate")]
    public DateTime StartDate { get; private set; }

    [BsonElement("endDate")]
    public DateTime? EndDate { get; private set; }

    [BsonElement("trialStartDate")]
    public DateTime? TrialStartDate { get; private set; }

    [BsonElement("trialEndDate")]
    public DateTime? TrialEndDate { get; private set; }

    [BsonElement("isTrial")]
    public bool IsTrial { get; private set; } = false;

    [BsonElement("isAutoRenew")]
    public bool IsAutoRenew { get; private set; } = true;

    [BsonElement("price")]
    public decimal Price { get; private set; } = 0;

    [BsonElement("currency")]
    public string Currency { get; private set; } = "USD";

    [BsonElement("billingPeriod")]
    public string BillingPeriod { get; private set; } = "Monthly";

    [BsonElement("paymentMethod")]
    public string? PaymentMethod { get; private set; }

    [BsonElement("transactionId")]
    public string? TransactionId { get; private set; }

    [BsonElement("lastPaymentDate")]
    public DateTime? LastPaymentDate { get; private set; }

    [BsonElement("nextPaymentDate")]
    public DateTime? NextPaymentDate { get; private set; }

    [BsonElement("cancelledAt")]
    public DateTime? CancelledAt { get; private set; }

    [BsonElement("cancellationReason")]
    public string? CancellationReason { get; private set; }

    [BsonElement("suspendedAt")]
    public DateTime? SuspendedAt { get; private set; }

    [BsonElement("suspensionReason")]
    public string? SuspensionReason { get; private set; }

    [BsonElement("reactivatedAt")]
    public DateTime? ReactivatedAt { get; private set; }

    [BsonElement("usage")]
    public Dictionary<string, object> Usage { get; private set; } = new();

    [BsonElement("limits")]
    public Dictionary<string, object> Limits { get; private set; } = new();

    [BsonElement("features")]
    public List<string> Features { get; private set; } = new();

    [BsonElement("benefits")]
    public List<string> Benefits { get; private set; } = new();

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("notes")]
    public string? Notes { get; private set; }

    [BsonElement("createdBy")]
    public ObjectId? CreatedBy { get; private set; }

    [BsonElement("giftFromUserId")]
    public ObjectId? GiftFromUserId { get; private set; }

    [BsonElement("promotionalCode")]
    public string? PromotionalCode { get; private set; }

    [BsonElement("discountAmount")]
    public decimal DiscountAmount { get; private set; } = 0;

    [BsonElement("totalPaid")]
    public decimal TotalPaid { get; private set; } = 0;

    [BsonElement("refundAmount")]
    public decimal RefundAmount { get; private set; } = 0;

    // Navigation properties
    [BsonIgnore]
    public User User { get; private set; } = null!;

    [BsonIgnore]
    public PremiumFeature PremiumFeature { get; private set; } = null!;

    [BsonIgnore]
    public User? Creator { get; private set; }

    [BsonIgnore]
    public User? GiftFromUser { get; private set; }

    // Private constructor for EF Core
    private UserPremiumFeature() { }

    public static UserPremiumFeature Create(ObjectId userId, ObjectId premiumFeatureId, DateTime startDate, decimal price, string currency = "USD", string billingPeriod = "Monthly", ObjectId? createdBy = null, bool isTrial = false)
    {
        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));

        DateTime? endDate = billingPeriod switch
        {
            "Monthly" => startDate.AddMonths(1),
            "Quarterly" => startDate.AddMonths(3),
            "Yearly" => startDate.AddYears(1),
            "Lifetime" => (DateTime?)null,
            _ => startDate.AddMonths(1)
        };

        var subscription = new UserPremiumFeature
        {
            UserId = userId,
            PremiumFeatureId = premiumFeatureId,
            StartDate = startDate,
            EndDate = endDate,
            Price = price,
            Currency = currency,
            BillingPeriod = billingPeriod,
            CreatedBy = createdBy,
            IsTrial = isTrial,
            IsAutoRenew = !isTrial,
            Status = isTrial ? "Trial" : "Active",
            SubscriptionType = isTrial ? "Trial" : "Subscription",
            TotalPaid = 0,
            RefundAmount = 0,
            Usage = new Dictionary<string, object>(),
            Limits = new Dictionary<string, object>(),
            Features = new List<string>(),
            Benefits = new List<string>(),
            Metadata = new Dictionary<string, object>()
        };

        if (isTrial)
        {
            subscription.TrialStartDate = startDate;
            subscription.TrialEndDate = startDate.AddDays(7); // Default 7-day trial
        }

        subscription.SetNextPaymentDate();
        return subscription;
    }

    public void UpdateStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status cannot be empty", nameof(status));

        Status = status;
        UpdateTimestamp();
    }

    public void SetTrial(bool isTrial, DateTime? trialStartDate = null, DateTime? trialEndDate = null)
    {
        IsTrial = isTrial;
        
        if (isTrial)
        {
            TrialStartDate = trialStartDate ?? DateTime.UtcNow;
            TrialEndDate = trialEndDate ?? TrialStartDate.Value.AddDays(7);
            Status = "Trial";
            SubscriptionType = "Trial";
        }
        else
        {
            TrialStartDate = null;
            TrialEndDate = null;
            if (Status == "Trial")
            {
                Status = "Active";
            }
        }
        
        UpdateTimestamp();
    }

    public void SetAutoRenew(bool isAutoRenew)
    {
        IsAutoRenew = isAutoRenew;
        UpdateTimestamp();
    }

    public void UpdatePricing(decimal price, string? currency = null, string? billingPeriod = null)
    {
        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));

        Price = price;
        
        if (!string.IsNullOrEmpty(currency))
            Currency = currency;
            
        if (!string.IsNullOrEmpty(billingPeriod))
            BillingPeriod = billingPeriod;
            
        SetNextPaymentDate();
        UpdateTimestamp();
    }

    public void SetPaymentInfo(string? paymentMethod, string? transactionId)
    {
        PaymentMethod = paymentMethod;
        TransactionId = transactionId;
        UpdateTimestamp();
    }

    public void RecordPayment(DateTime paymentDate)
    {
        LastPaymentDate = paymentDate;
        TotalPaid += Price;
        SetNextPaymentDate();
        UpdateTimestamp();
    }

    public void SetNextPaymentDate()
    {
        if (IsTrial && TrialEndDate.HasValue)
        {
            NextPaymentDate = TrialEndDate.Value;
        }
        else if (LastPaymentDate.HasValue)
        {
            NextPaymentDate = BillingPeriod switch
            {
                "Monthly" => LastPaymentDate.Value.AddMonths(1),
                "Quarterly" => LastPaymentDate.Value.AddMonths(3),
                "Yearly" => LastPaymentDate.Value.AddYears(1),
                "Lifetime" => null,
                _ => LastPaymentDate.Value.AddMonths(1)
            };
        }
        else
        {
            NextPaymentDate = EndDate;
        }
    }

    public void Cancel(string reason, ObjectId? cancelledBy = null)
    {
        Status = "Cancelled";
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        IsAutoRenew = false;
        NextPaymentDate = null;
        UpdateTimestamp();
    }

    public void Suspend(string reason, ObjectId? suspendedBy = null)
    {
        Status = "Suspended";
        SuspendedAt = DateTime.UtcNow;
        SuspensionReason = reason;
        UpdateTimestamp();
    }

    public void Reactivate(ObjectId? reactivatedBy = null)
    {
        Status = "Active";
        ReactivatedAt = DateTime.UtcNow;
        SuspendedAt = null;
        SuspensionReason = null;
        SetNextPaymentDate();
        UpdateTimestamp();
    }

    public void SetGift(ObjectId giftFromUserId, string? notes = null)
    {
        GiftFromUserId = giftFromUserId;
        SubscriptionType = "Gift";
        Notes = notes;
        UpdateTimestamp();
    }

    public void SetPromotional(string promotionalCode, decimal discountAmount)
    {
        PromotionalCode = promotionalCode;
        DiscountAmount = discountAmount;
        SubscriptionType = "Promotional";
        UpdateTimestamp();
    }

    public void AddRefund(decimal refundAmount)
    {
        if (refundAmount < 0)
            throw new ArgumentException("Refund amount cannot be negative", nameof(refundAmount));

        RefundAmount += refundAmount;
        UpdateTimestamp();
    }

    public void AddUsage(string key, object value)
    {
        Usage[key] = value;
        UpdateTimestamp();
    }

    public void SetLimits(Dictionary<string, object> limits)
    {
        Limits = limits ?? new Dictionary<string, object>();
        UpdateTimestamp();
    }

    public void AddFeature(string feature)
    {
        if (string.IsNullOrWhiteSpace(feature))
            throw new ArgumentException("Feature cannot be empty", nameof(feature));

        if (!Features.Contains(feature))
        {
            Features.Add(feature);
            UpdateTimestamp();
        }
    }

    public void RemoveFeature(string feature)
    {
        Features.Remove(feature);
        UpdateTimestamp();
    }

    public void AddBenefit(string benefit)
    {
        if (string.IsNullOrWhiteSpace(benefit))
            throw new ArgumentException("Benefit cannot be empty", nameof(benefit));

        if (!Benefits.Contains(benefit))
        {
            Benefits.Add(benefit);
            UpdateTimestamp();
        }
    }

    public void RemoveBenefit(string benefit)
    {
        Benefits.Remove(benefit);
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdateTimestamp();
    }

    public bool IsActive()
    {
        return Status == "Active" || Status == "Trial";
    }

    public bool IsExpired()
    {
        return EndDate.HasValue && EndDate.Value <= DateTime.UtcNow;
    }

    public bool IsInTrial()
    {
        return IsTrial && TrialEndDate.HasValue && TrialEndDate.Value > DateTime.UtcNow;
    }

    public bool IsTrialExpired()
    {
        return IsTrial && TrialEndDate.HasValue && TrialEndDate.Value <= DateTime.UtcNow;
    }

    public bool NeedsPayment()
    {
        return NextPaymentDate.HasValue && NextPaymentDate.Value <= DateTime.UtcNow && Status == "Active";
    }

    public TimeSpan GetRemainingTime()
    {
        if (EndDate.HasValue)
        {
            var remaining = EndDate.Value - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
        return TimeSpan.Zero;
    }

    public TimeSpan GetTrialRemainingTime()
    {
        if (TrialEndDate.HasValue)
        {
            var remaining = TrialEndDate.Value - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
        return TimeSpan.Zero;
    }

    public decimal GetNetRevenue()
    {
        return TotalPaid - RefundAmount;
    }
}
