
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;

namespace ChurchOrganizationSystem.Pages.Members;
public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;

    public IndexModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public List<MemberRow> Members { get; set; } = new();
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; } = "";
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
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
                WHERE membership_number LIKE @search
                   OR first_name LIKE @search
                   OR last_name LIKE @search
                ORDER BY member_id DESC;
                """;

            await using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@search",
                $"%{SearchTerm.Trim()}%"
            );

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Members.Add(new MemberRow
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
                });
            }
        }
        catch (Exception)
        {
            ErrorMessage =
                "Could not load members. Check your MySQL server, " +
                "connection settings, and database table.";
        }
    }
}
