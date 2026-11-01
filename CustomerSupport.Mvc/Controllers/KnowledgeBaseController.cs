using CustomerSupport.Mvc.Models.Api;
using CustomerSupport.Mvc.Models.ViewModels;
using CustomerSupport.Mvc.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CustomerSupport.Mvc.Controllers
{
    [Authorize]
    public sealed class KnowledgeBaseController : Controller
    {
        private readonly IContentApi _api;
        private readonly ICatalogApi _catalog;

        public KnowledgeBaseController(IContentApi api, ICatalogApi catalog)
        {
            _api = api;
            _catalog = catalog;
        }

        [HttpGet("/knowledge-base")]
        public Task<IActionResult> Index([FromQuery]ContentSearch search)
        {
            return ListAsync(search,false);
        }

        [Authorize(Roles="Administrator"),HttpGet("/admin/knowledge-base")]
        public Task<IActionResult> Manage([FromQuery]ContentSearch search)
        {
            return ListAsync(search,true);
        }

        private async Task<IActionResult> ListAsync(ContentSearch search,bool management)
        {
            var result=await _api.ArticlesAsync(search);
            if (!result.Ok) TempData["Error"]=result.Message;
            return View("Index",new ContentListPage<KnowledgeArticle>{Search=search,Results=result.Data??new(),
                Products=(await _catalog.ProductsAsync()).Data??[],Management=management});
        }

        [HttpGet("/knowledge-base/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var result=await _api.ArticleAsync(id);
            if (!result.Ok||result.Data is null)return Redirect("/not-found");
            return View(result.Data);
        }

        [Authorize(Roles="Administrator"),HttpGet("/admin/knowledge-base/new")]
        public async Task<IActionResult> Create()
        {
            return View("Form",await PopulateAsync(new ContentForm()));
        }

        [Authorize(Roles="Administrator"),HttpGet("/admin/knowledge-base/{id:int}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var r=await _api.ArticleAsync(id);
            if (!r.Ok||r.Data is null)return Redirect("/not-found");
            return View("Form",await PopulateAsync(new ContentForm{Id=id,Title=r.Data.Title,Content=r.Data.Content,
                ProductId=r.Data.ProductId,IsActive=r.Data.IsActive,IsPublished=r.Data.IsPublished}));
        }

        [Authorize(Roles="Administrator"),HttpPost("/admin/knowledge-base/save")]
        public async Task<IActionResult> Save(ContentForm form)
        {
            if (ModelState.IsValid)
            {
                var r=await _api.SaveArticleAsync(form.Id>0?form.Id:null,form.Title,form.Content,form.ProductId,form.IsPublished,form.IsActive);
                if (r.Ok)
                {
                    TempData["Success"] = r.Message;
                    return RedirectToAction(nameof(Manage));
                }
                ModelState.AddModelError(string.Empty,r.Message);
            }
            return View("Form",await PopulateAsync(form));
        }

        [Authorize(Roles="Administrator"),HttpPost("/admin/knowledge-base/{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var r=await _api.DeactivateArticleAsync(id);TempData[r.Ok?"Success":"Error"]=r.Message;
            return RedirectToAction(nameof(Manage));
        }

        private async Task<ContentForm> PopulateAsync(ContentForm form)
        {
            form.Products=(await _catalog.ProductsAsync()).Data??[];return form;
        }
    }
}
