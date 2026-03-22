namespace BankInsight.API.Entities;

public enum AccessScopeType
{
    All = 1,
    BranchOnly = 2,
    RegionOnly = 3,
    AssignedPortfolioOnly = 4,
    OwnRecordsOnly = 5
}
