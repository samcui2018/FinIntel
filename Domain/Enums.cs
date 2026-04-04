namespace FinancialIntelligence.Api.Domain.Transactions;

public enum EntryDirection
{
    Unknown = 0,
    Debit = 1,   // money out
    Credit = 2   // money in
}

public enum TransactionClass
{
    Unknown = 0,
    Expense = 1,
    Income = 2,
    Refund = 3,
    Transfer = 4,
    CardPayment = 5,
    Payout = 6,
    Fee = 7,
    Adjustment = 8,
    Chargeback = 9,
    LoanPayment = 10,
    Payroll = 11
}

public enum SignConvention
{
    Unknown = 0,
    NegativeIsDebit = 1,
    PositiveIsDebit = 2,
    SeparateDebitCreditColumns = 3
}

public enum ConfidenceLevel
{
    Low = 1,
    Medium = 2,
    High = 3
}