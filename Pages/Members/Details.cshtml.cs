using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;


namespace ChurchOrganizationSystem.Pages.Members;

public class DetailsModel : PageModel
{
    private readonly IConfiguration _configuration;

    public DetailsModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public MemberRow? Member { get; set; }

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

        Member = new MemberRow
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
}
