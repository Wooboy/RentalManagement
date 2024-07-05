using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalManagement.Models;

public class PropertyController : Controller
{
    private readonly MainContext _context;

    public PropertyController(MainContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "資產清單";

        var properties = await _context.Property
            .Where(u => u.DeleteAt == null && u.ParentPropertyId == null)
            .ToListAsync();

        return View(properties);
    }

    public async Task<IActionResult> Detail(int id)
    {
        ViewData["Title"] = "資產 " + id.ToString() + " 詳細內容";

        var properties = await _context.Property.FirstOrDefaultAsync(u => u.Id == id);

        if (properties == null)
        {
            return NotFound();
        }

        return View(properties);
    }
}
