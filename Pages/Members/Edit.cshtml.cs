
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;

namespace ChurchOrganizationSystem.Pages.Members;

public class EditModel : PageModel
{
    private readonly IConfiguration _configuration;

    public EditModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [BindProperty]
    public MemberEditInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        string connectionString =
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "The database connection string is missing.");

        await using var connection =
            new MySqlConnection(connectionString);

        await connection.OpenAsync();

        const string sql = """
            SELECT member_id, membership_number,
                   first_name, middle_name, last_name,
                   contact_number, email, address,
                   date_joined, membership_status
            FROM members
            WHERE member_id = @id;
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return NotFound();
        }

        Input = new MemberEditInput
        {
            MemberId = reader.GetInt32("member_id"),
            MembershipNumber = reader.GetString("membership_number"),
            FirstName = reader.GetString("first_name"),
            MiddleName = reader["middle_name"] as string,
            LastName = reader.GetString("last_name"),
            ContactNumber = reader["contact_number"] as string,
            Email = reader["email"] as string,
            Address = reader["address"] as string,
            DateJoined = reader.GetDateTime("date_joined"),
            MembershipStatus = reader.GetString("membership_status")
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        // Use the ID from the URL, not the submitted form.
        Input.MemberId = id;

        if (Input.MembershipStatus != "Active" &&
            Input.MembershipStatus != "Inactive")
        {
            ModelState.AddModelError(
                "Input.MembershipStatus",
                "Please select a valid membership status.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        string connectionString =
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "The database connection string is missing.");

        try
        {
            await using var connection =
                new MySqlConnection(connectionString);

            await connection.OpenAsync();

            const string sql = """
                UPDATE members
                SET membership_number = @membershipNumber,
                    first_name = @firstName,
                    middle_name = @middleName,
                    last_name = @lastName,
                    contact_number = @contactNumber,
                    email = @email,
                    address = @address,
                    date_joined = @dateJoined,
                    membership_status = @membershipStatus
                WHERE member_id = @id;
                """;

            await using var command =
                new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@membershipNumber", Input.MembershipNumber.Trim());
            command.Parameters.AddWithValue(
                "@firstName", Input.FirstName.Trim());
            command.Parameters.AddWithValue(
                "@middleName",
                string.IsNullOrWhiteSpace(Input.MiddleName)
                    ? DBNull.Value : Input.MiddleName.Trim());
            command.Parameters.AddWithValue(
                "@lastName", Input.LastName.Trim());
            command.Parameters.AddWithValue(
                "@contactNumber",
                string.IsNullOrWhiteSpace(Input.ContactNumber)
                    ? DBNull.Value : Input.ContactNumber.Trim());
            command.Parameters.AddWithValue(
                "@email",
                string.IsNullOrWhiteSpace(Input.Email)
                    ? DBNull.Value : Input.Email.Trim());
            command.Parameters.AddWithValue(
                "@address",
                string.IsNullOrWhiteSpace(Input.Address)
                    ? DBNull.Value : Input.Address.Trim());
            command.Parameters.AddWithValue(
                "@dateJoined", Input.DateJoined.Date);
            command.Parameters.AddWithValue(
                "@membershipStatus", Input.MembershipStatus);
            command.Parameters.AddWithValue("@id", id);

            int rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                // The record may have been deleted after the form opened.
                await using var checkCommand = new MySqlCommand(
                    "SELECT COUNT(*) FROM members WHERE member_id = @id;",
                    connection);

                checkCommand.Parameters.AddWithValue("@id", id);

                long exists = Convert.ToInt64(
                    await checkCommand.ExecuteScalarAsync());

                if (exists == 0)
                {
                    return NotFound();
                }
            }

            return RedirectToPage(
                "/Members/Details", new { id });
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            ModelState.AddModelError(
                "Input.MembershipNumber",
                "That membership number is already in use.");

            return Page();
        }
        catch (MySqlException)
        {
            ErrorMessage =
                "Could not update the member. Please check your database connection and try again.";

            return Page();
        }
    }
}

public class MemberEditInput
{
    public int MemberId { get; set; }

    [Required(ErrorMessage = "Membership number is required.")]
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

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(255)]
    public string? Address { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date Joined")]
    public DateTime DateJoined { get; set; }

    [Required]
    [Display(Name = "Membership Status")]
    public string MembershipStatus { get; set; } = "Active";
}
