using ForumApp.Data;
using ForumApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ForumApp.Controllers
{
    [Route("api/Category")]
    [ApiController]
    public class CategoryApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public CategoryApiController(ApplicationDbContext context)
        {
            _context = context;
        }


        //get -> vraca JSON listu svih kategorija (glavne i potkategorije)
        [HttpGet]
        public async Task<ActionResult<List<Category>>> DohvatiSve()
        {
            var sve = await _context.Categories
                .Include(c => c.Subcategories)
                .ToListAsync();

            return Ok(sve);
        }

        // get -> vraca jednu kategoriju
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Category>> DohvatiJednu(int id)
        {
            var jednaKategorija = await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.Subcategories)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (jednaKategorija == null)
                return NotFound();

            return Ok(jednaKategorija);
        }

        //post -> kreira novu kategoriju, ocekuje json
        [HttpPost]
        public async Task<ActionResult<Category>> KreirajNovu([FromBody] Category input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
            {
                ModelState.AddModelError("Name", "Naziv kategorije je obaveyan.");
                return BadRequest(ModelState);
            }

            var nova = new Category
            {
                Name = input.Name.Trim(),
                ParentCategoryId = input.ParentCategoryId
            };

            _context.Categories.Add(nova);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(DohvatiJednu), new { id = nova.CategoryId }, nova);
        }


        //put -> radi update postojece kategorije, takodje ocekuje json
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Azuriraj(int id, [FromBody] Category input)
        {
            if (id != input.CategoryId)
                return BadRequest("URL id se ne podudara s CategoryId u teluu.");

            if (string.IsNullOrWhiteSpace(input.Name))
            {
                ModelState.AddModelError("Name", "Name je obavezno polje.");
                return BadRequest(ModelState);
            }

            var kategorijaIzBaze = await _context.Categories.FindAsync(id);
            if (kategorijaIzBaze == null)
                return NotFound();

            kategorijaIzBaze.Name = input.Name.Trim();
            kategorijaIzBaze.ParentCategoryId = input.ParentCategoryId;

            _context.Entry(kategorijaIzBaze).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }


        //delete -> brise kategoriju, ako ima potkategorije vraca BadReq ili bilo sta moze i konflikt ili tako nesto...
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Obrisi(int id)
        {
            var cat = await _context.Categories
                .Include(c => c.Subcategories)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (cat == null)
                return NotFound();

            if (cat.Subcategories != null && cat.Subcategories.Count > 0)
            {
                return BadRequest("Ne mozes da brises kategoriju koja ima potkategorije.");
            }

            _context.Categories.Remove(cat);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

}
