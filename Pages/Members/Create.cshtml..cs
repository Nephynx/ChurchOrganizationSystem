
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;

namespace ChurchOrganizationSystem.Pages.Members;

public class CreateModel : PageModel
{
    private readonly IConfiguration _configuration;

    public CreateModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [BindProperty]
    public MemberInput Member { get; set; } = new()
    {
        DateJoined = DateTime.Today,
        MembershipStatus = "Active"
    };

    public string? ErrorMessage { get; set; }

    public class MemberInput
    {
        [Required]
        [StringLength(20)]
        [Display(Name = "Membership Number")]
        public string MembershipNumber { get; set; } = "";

        [Required]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";

        [StringLength(100)]
        [Display(Name = "Middle Name")]
        public string? MiddleName { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "";

        [StringLength(20)]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date Joined")]
        public DateTime? DateJoined { get; set; }

        [Required]
        [RegularExpression("Active|Inactive")]
        [Display(Name = "Membership Status")]
        public string MembershipStatus { get; set; } = "Active";
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            string connectionString =
                _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Database connection is not configured.");

            await using var connection =
                new MySqlConnection(connectionString);

            await connection.OpenAsync();

            const string sql = """
                INSERT INTO members (
                    membership_number,
                    first_name,
                    middle_name,
                    last_name,
                    contact_number,
                    email,
                    address,
                    date_joined,
                    membership_status
                )
                VALUES (
                    @number,
                    @first,
                    @middle,
                    @last,
                    @contact,
                    @email,
                    @address,
                    @dateJoined,
                    @status
                );
                """;

            await using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@number",
                Member.MembershipNumber.Trim());
            command.Parameters.AddWithValue("@first",
                Member.FirstName.Trim());
            command.Parameters.AddWithValue("@middle",
                (object?)Member.MiddleName?.Trim() ?? DBNull.Value);
            command.Parameters.AddWithValue("@last",
                Member.LastName.Trim());
            command.Parameters.AddWithValue("@contact",
                (object?)Member.ContactNumber?.Trim() ?? DBNull.Value);
            command.Parameters.AddWithValue("@email",
                (object?)Member.Email?.Trim() ?? DBNull.Value);
            command.Parameters.AddWithValue("@address",
                (object?)Member.Address?.Trim() ?? DBNull.Value);
            command.Parameters.AddWithValue("@dateJoined",
                Member.DateJoined!.Value.Date);
            command.Parameters.AddWithValue("@status",
                Member.MembershipStatus);

            await command.ExecuteNonQueryAsync();

            return RedirectToPage("/Members/Index");
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            ModelState.AddModelError(
                "Member.MembershipNumber",
                "That membership number already exists.");

            return Page();
        }
        catch (Exception)
        {
            ErrorMessage =
                "The member could not be saved. Check your database connection.";
            return Page();
        }
    }
}
