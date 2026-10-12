using CustomerSupport.Mvc.Models.ViewModels;
using CustomerSupport.Mvc.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CustomerSupport.Mvc.Controllers
{
    [Authorize(Roles="Administrator")]
    public sealed class TicketCategoriesController : Controller
    {
        private readonly ICatalogApi _api;

        public TicketCategoriesController(ICatalogApi api)
        {
            _api = api;
        }

        [HttpGet("/admin/ticket-categories")]
        public async Task<IActionResult> Index()
        {
            var result=await _api.CategoriesAsync(all:true);
            if (!result.Ok) TempData["Error"]=result.Message;
            return View(result.Data??[]);
        }

        [HttpGet("/admin/ticket-categories/new")]
        public IActionResult Create()
        {
            return View("Form",new CatalogForm());
        }

        [HttpGet("/admin/ticket-categories/{id:int}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var r=await _api.CategoryAsync(id);
            if (!r.Ok||r.Data is null)return Redirect("/not-found");
            return View("Form",new CatalogForm { Id=id,Name=r.Data.Name,Description=r.Data.Description,IsActive=r.Data.IsActive });
        }

        [HttpPost("/admin/ticket-categories/save")]
        public async Task<IActionResult> Save(CatalogForm form)
        {
            if (!ModelState.IsValid)return View("Form",form);
            var result=await _api.SaveCategoryAsync(form.Id>0?form.Id:null,form.Name,form.Description,form.IsActive);
            if (!result.Ok)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View("Form", form);
            }
            TempData["Success"]=result.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
