
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;

namespace ChurchOrganizationSystem.Pages.Categories;

public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;

    public IndexModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public List<CategoryRow> Categories { get; set; } = new();

    [BindProperty]
    public CategoryInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadCategoriesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return Page();
        }

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
                INSERT INTO member_categories
                    (category_name, description)
                VALUES
                    (@name, @description);
                """;

            await using var command =
                new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@name", Input.CategoryName.Trim());

            command.Parameters.AddWithValue(
                "@description",
                string.IsNullOrWhiteSpace(Input.Description)
                    ? DBNull.Value
                    : Input.Description.Trim());

            await command.ExecuteNonQueryAsync();

            TempData["SuccessMessage"] = "Category added successfully.";

            return RedirectToPage();
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            ModelState.AddModelError(
                "Input.CategoryName",
                "This category already exists.");

            await LoadCategoriesAsync();
            return Page();
        }
        catch (MySqlException)
        {
            ErrorMessage =
                "Could not save the category. Check your database connection.";

            await LoadCategoriesAsync();
            return Page();
        }
    }

    private async Task LoadCategoriesAsync()
    {
        Categories.Clear();

        string connectionString =
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "The database connection string is missing.");

        await using var connection =
            new MySqlConnection(connectionString);

        await connection.OpenAsync();

        const string sql = """
            SELECT category_id, category_name, description, created_at
            FROM member_categories
            ORDER BY category_name;
            """;

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            Categories.Add(new CategoryRow
            {
                CategoryId = reader.GetInt32("category_id"),
                CategoryName = reader.GetString("category_name"),
                Description = reader["description"] as string,
                CreatedAt = reader.GetDateTime("created_at")
            });
        }

        SuccessMessage = TempData["SuccessMessage"] as string;
    }
}

public class CategoryRow
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CategoryInput
{
    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100)]
    [Display(Name = "Category Name")]
    public string CategoryName { get; set; } = "";

    [StringLength(255)]
    public string? Description { get; set; }
}
