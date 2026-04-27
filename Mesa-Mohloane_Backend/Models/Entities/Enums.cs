namespace Mesa_Mohloane_Backend.Models.Entities;

public enum UserRole
{
    Citizen = 1,
    Contractor = 2,
    Admin = 3,
    Auditor = 4
}

public enum IncidentStatus
{
    Pending = 0,
    Reported = 1,
    Verified = 2,
    Published = 3,
    Assigned = 4,
    InProgress = 5,
    Completed = 6,
    Closed = 7,
    Rejected = 8
}

public enum TenderStatus
{
    Submitted = 1,
    UnderReview = 2,
    Approved = 3,
    Rejected = 4,
    Withdrawn = 5
}

public enum AssignmentStatus
{
    Assigned = 1,
    Started = 2,
    Completed = 3,
    AwaitingApproval = 4,
    Approved = 5,
    Closed = 6
}

public enum InvoiceStatus
{
    Submitted = 1,
    Validated = 2,
    Approved = 3,
    Flagged = 4,
    Disbursed = 5,
    Rejected = 6
}

public enum PaymentStatus
{
    Initiated = 1,
    Approved = 2,
    Disbursed = 3,
    Failed = 4
}

public enum NotificationType
{
    IncidentSubmitted = 1,
    IncidentVerified = 2,
    TenderSubmitted = 3,
    AssignmentCreated = 4,
    WorkCompleted = 5,
    InvoiceSubmitted = 6,
    InvoiceApproved = 7,
    PaymentStatusChanged = 8
}

public enum AuditActionType
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
    Approved = 4,
    Rejected = 5,
    Login = 6,
    Logout = 7,
    Create = 8,
    Update = 9,
    Delete = 10,
    Approve = 11,
    Reject = 12,
    Assign = 13,
    Submit = 14,
    Complete = 15,
    Verify = 16,
    Rate = 17,
    Other = 18
}

public enum TenderLineItemCategory
{
    Labor = 1,
    Materials = 2,
    Equipment = 3,
    Transport = 4,
    Other = 5
}
