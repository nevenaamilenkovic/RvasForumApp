using ForumApp.Data;
using ForumApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ForumApp.Controllers
{
    [Authorize(Roles = "Admin,Moderator")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.ParentCategories = new SelectList(await _context.Categories.Where(c=>c.ParentCategoryId==null)
                .ToListAsync(), "CategoryId", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category model)
        {
            if (ModelState.IsValid)
            {
                //pod komentarom zato sto sada ne unosimo samo naziv za kategoriju
                //var c = new Category()
                //{
                //    Name = model.Name
                //};

                _context.Categories.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            //dodato zato sto ukoliko nisu validni podaci ponovo se ucitava forma za dodavanje/kreiranje sa ponudjenom listom roditeljskih kategorija
            ViewBag.ParentCategories = new SelectList(await _context.Categories.Where(c=>c.ParentCategoryId==null)
                .ToListAsync(), "CategoryId", "Name");
            return View(model);
        }

        public async Task<IActionResult> Index()
        {
            //inkludovane potkategorije i kategorije radi hijerarhijskog prikaza koja potkategorija pripada kojoj kategoriji
            var kategorije = await _context.Categories
                .Include(c=>c.Subcategories)
                .Where(c=>c.ParentCategoryId == null)
                .ToListAsync();
            return View(kategorije);
        }


        public async Task<IActionResult> Edit(int id)
        {
            var c = await _context.Categories.FindAsync(id);

            if (c == null) return NotFound();

            //dodato radi promene, ako se menja potkategorija prikazivace se izbor za promenu roditeljske kategorije
            if(c.ParentCategoryId == null)
            {
                ViewBag.ParentCategories = null;
            }
            else
            {

                // SelectList sa kategorijama koje nisu trenutna kategorija
                ViewBag.ParentCategories = new SelectList(
                    await _context.Categories.Where(category=>category.ParentCategoryId == null && category.CategoryId!=id).ToListAsync(),
                    "CategoryId",
                    "Name",
                    c.ParentCategoryId // Podesi trenutno odabranu roditeljsku kategoriju
                );
            }


            return View(c);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category model)
        {
            if (id != model.CategoryId) return NotFound();

            if (ModelState.IsValid)
            {
                var kategorija = await _context.Categories.FindAsync(id);

                if (kategorija == null) return NotFound();

                kategorija.Name = model.Name;
                //dodato ako se azurira potkategorija
                kategorija.ParentCategoryId = model.ParentCategoryId; // Izmena roditeljske kategorije

                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.ParentCategories = new SelectList(
                await _context.Categories.Where(cat => cat.CategoryId != id).ToListAsync(),
                "CategoryId",
                "Name",
                model.ParentCategoryId
            );

            return View(model);
        }

        [Authorize(Roles = "Admin")]

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _context.Categories
                .Include(c => c.Subcategories)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (c == null) return NotFound();

            bool imaPotkategorije = await _context.Categories.AnyAsync(c => c.ParentCategoryId == id);

            if (imaPotkategorije) return View("DeleteRestricted", c);


            _context.Categories.Remove(c);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            
            var c = await _context.Categories
                //da bi se prikazao naziv ukoliko se gledaju detalji za potkategoriju i koja je njena roditeljska kategorija
                   .Include(c => c.Subcategories)
                   .Include(c=>c.ParentCategory)
                   .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (c == null) return NotFound();


            return View(c);
        }


        //da vrati view s AJAXom za test
        [HttpGet]
        public IActionResult Upravljaj()
        {
            return View();
        }
    }
}
