using CustomerSupport.Mvc.Models.Api;
using CustomerSupport.Mvc.Models.ViewModels;
using CustomerSupport.Mvc.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CustomerSupport.Mvc.Controllers
{
    [Authorize]
    public sealed class FaqsController : Controller
    {
        private readonly IContentApi _api;
        private readonly ICatalogApi _catalog;

        public FaqsController(IContentApi api, ICatalogApi catalog)
        {
            _api = api;
            _catalog = catalog;
        }

        [HttpGet("/faqs")]
        public Task<IActionResult> Index([FromQuery]ContentSearch search)
        {
            return ListAsync(search,false);
        }

        [Authorize(Roles="Administrator"),HttpGet("/admin/faqs")]
        public Task<IActionResult> Manage([FromQuery]ContentSearch search)
        {
            return ListAsync(search,true);
        }

        private async Task<IActionResult> ListAsync(ContentSearch search,bool management)
        {
            var r=await _api.FaqsAsync(search);
            if (!r.Ok) TempData["Error"]=r.Message;
            return View("Index",new ContentListPage<Faq>{Search=search,Results=r.Data??new(),
                Products=(await _catalog.ProductsAsync()).Data??[],Management=management});
        }

        [HttpGet("/faqs/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var r=await _api.FaqAsync(id);
            if (!r.Ok||r.Data is null)return Redirect("/not-found");
            return View(r.Data);
        }

        [Authorize(Roles="Administrator"),HttpGet("/admin/faqs/new")]
        public async Task<IActionResult> Create()
        {
            return View("Form",await PopulateAsync(new ContentForm()));
        }

        [Authorize(Roles="Administrator"),HttpGet("/admin/faqs/{id:int}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var r=await _api.FaqAsync(id);
            if (!r.Ok||r.Data is null)return Redirect("/not-found");
            return View("Form",await PopulateAsync(new ContentForm{Id=id,Title=r.Data.Question,Content=r.Data.Answer,
                ProductId=r.Data.ProductId,IsActive=r.Data.IsActive}));
        }

        [Authorize(Roles="Administrator"),HttpPost("/admin/faqs/save")]
        public async Task<IActionResult> Save(ContentForm form)
        {
            if (ModelState.IsValid)
            {
                var r=await _api.SaveFaqAsync(form.Id>0?form.Id:null,form.Title,form.Content,form.ProductId,form.IsActive);
                if (r.Ok)
                {
                    TempData["Success"] = r.Message;
                    return RedirectToAction(nameof(Manage));
                }
                ModelState.AddModelError(string.Empty,r.Message);
            }
            return View("Form",await PopulateAsync(form));
        }

        [Authorize(Roles="Administrator"),HttpPost("/admin/faqs/{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var r=await _api.DeactivateFaqAsync(id);TempData[r.Ok?"Success":"Error"]=r.Message;
            return RedirectToAction(nameof(Manage));
        }

        private async Task<ContentForm> PopulateAsync(ContentForm form)
        {
            form.Products=(await _catalog.ProductsAsync()).Data??[];return form;
        }
    }
}
