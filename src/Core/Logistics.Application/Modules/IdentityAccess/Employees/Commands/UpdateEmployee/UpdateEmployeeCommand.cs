using Logistics.Application.Abstractions;
using Logistics.Domain.Primitives.Enums;
using Logistics.Domain.Primitives.ValueObjects;

namespace Logistics.Application.Modules.IdentityAccess.Employees.Commands;

public class UpdateEmployeeCommand : ICommand
{
    public Guid UserId { get; set; }
    public string? Role { get; set; }
    public decimal? Salary { get; set; }
    public SalaryType? SalaryType { get; set; }
    public EmployeeStatus? Status { get; set; }
    public Address? Address { get; set; }
    public bool UpdateAssignedDispatcher { get; set; }
    public Guid? AssignedDispatcherId { get; set; }
    public bool UpdateLoadFinder { get; set; }
    public bool IsLoadFinderEnabled { get; set; }
    public DateTime? LoadFinderExpiresAt { get; set; }
}
