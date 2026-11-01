using CustomerSupport.Mvc.Models.ViewModels;
using CustomerSupport.Mvc.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CustomerSupport.Mvc.Controllers
{
    [Authorize(Roles="Administrator")]
    public sealed class ProductsController : Controller
    {
        private readonly ICatalogApi _api;

        public ProductsController(ICatalogApi api)
        {
            _api = api;
        }

        [HttpGet("/admin/products")]
        public async Task<IActionResult> Index()
        {
            var response=await _api.ProductsAsync(all:true);
            if (!response.Ok) TempData["Error"]=response.Message;
            return View(response.Data??[]);
        }

        [HttpGet("/admin/products/new")]
        public IActionResult Create()
        {
            return View("Form",new CatalogForm());
        }

        [HttpGet("/admin/products/{id:int}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var r=await _api.ProductAsync(id);
            if (!r.Ok||r.Data is null) return Redirect("/not-found");
            return View("Form",new CatalogForm { Id=id,Name=r.Data.Name,Description=r.Data.Description,IsActive=r.Data.IsActive });
        }

        [HttpPost("/admin/products/save")]
        public async Task<IActionResult> Save(CatalogForm form)
        {
            if (!ModelState.IsValid) return View("Form",form);
            var result=await _api.SaveProductAsync(form.Id>0?form.Id:null,form.Name,form.Description,form.IsActive);
            if (!result.Ok)
            {
                ModelState.AddModelError(string.Empty,result.Message);return View("Form",form);
            }
            TempData["Success"]=result.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
