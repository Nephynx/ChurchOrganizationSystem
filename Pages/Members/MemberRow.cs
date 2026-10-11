
namespace ChurchOrganizationSystem.Pages.Members;

public class MemberRow
{
    public int MemberId { get; set; }
    public string MembershipNumber { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = "";
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime DateJoined { get; set; }
    public string MembershipStatus { get; set; } = "";
}
