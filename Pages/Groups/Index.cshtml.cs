
using System.ComponentModel.DataAnnotations;
using MySqlConnector;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChurchOrganizationSystem.Pages.Groups;

public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;

    public IndexModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public List<GroupRow> Groups { get; set; } = new();

    [BindProperty]
    public GroupInput Input { get; set; } = new();

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    private string ConnectionString =>
        _configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Database connection string is missing.");

    public async Task OnGetAsync()
    {
        await LoadGroupsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadGroupsAsync();
            return Page();
        }

        try
        {
            await using var connection =
                new MySqlConnection(ConnectionString);

            await connection.OpenAsync();

            const string sql = @"
                INSERT INTO church_groups
                    (group_name, description, leader_name)
                VALUES
                    (@groupName, @description, @leaderName);";

            await using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@groupName", Input.GroupName.Trim());

            command.Parameters.AddWithValue(
                "@description",
                string.IsNullOrWhiteSpace(Input.Description)
                    ? DBNull.Value
                    : Input.Description.Trim());

            command.Parameters.AddWithValue(
                "@leaderName",
                string.IsNullOrWhiteSpace(Input.LeaderName)
                    ? DBNull.Value
                    : Input.LeaderName.Trim());

            await command.ExecuteNonQueryAsync();

            TempData["SuccessMessage"] = "Group added successfully.";

            return RedirectToPage();
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            ModelState.AddModelError(
                "Input.GroupName",
                "A group with this name already exists.");
        }
        catch (MySqlException)
        {
            ErrorMessage =
                "Unable to save the group. Please check your database connection.";
        }

        await LoadGroupsAsync();
        return Page();
    }

    private async Task LoadGroupsAsync()
    {
        Groups.Clear();

        await using var connection =
            new MySqlConnection(ConnectionString);

        await connection.OpenAsync();

        const string sql = @"
            SELECT group_id, group_name, description,
                   leader_name, created_at
            FROM church_groups
            ORDER BY group_name;";

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            Groups.Add(new GroupRow
            {
                GroupId = reader.GetInt32("group_id"),
                GroupName = reader.GetString("group_name"),
                Description = reader.IsDBNull(
                    reader.GetOrdinal("description"))
                    ? null
                    : reader.GetString("description"),
                LeaderName = reader.IsDBNull(
                    reader.GetOrdinal("leader_name"))
                    ? null
                    : reader.GetString("leader_name"),
                CreatedAt = reader.GetDateTime("created_at")
            });
        }
    }

    public class GroupRow
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = "";
        public string? Description { get; set; }
        public string? LeaderName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GroupInput
    {
        [Required(ErrorMessage = "Group name is required.")]
        [StringLength(100)]
        [Display(Name = "Group Name")]
        public string GroupName { get; set; } = "";

        [StringLength(255)]
        public string? Description { get; set; }

        [StringLength(150)]
        [Display(Name = "Leader Name")]
        public string? LeaderName { get; set; }
    }
}
